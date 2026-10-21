using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Common;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DTOs;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Application.Services;
using Forgotten.Nft.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.UnitTests.Services;

public class NftGalleryServiceTests
{
    private const ulong Owner = 500UL;
    private const ulong Guild = 1UL;
    private const string Wallet = "So11111111111111111111111111111111111111112";

    private static SolanaAsset Nft(string address, string? collection = null, bool supported = true, bool burned = false) => new()
    {
        AssetAddress = address,
        InterfaceType = supported ? "V1_NFT" : "FungibleToken",
        IsSupportedNft = supported,
        IsBurned = burned,
        Name = address,
        CollectionAddress = collection,
        Provider = "fake",
        RetrievedAt = DateTimeOffset.UtcNow,
    };

    private static (NftGalleryService svc, FakeGallerySessions sessions) Build(
        IReadOnlyList<SolanaAsset> owned, bool hasWallet = true, int galleryMax = 100)
    {
        var provider = new FakeOwnerProvider(owned);
        var connections = new FakeWalletConnections(hasWallet
            ? new[] { new DiscordWalletConnection { DiscordUserId = Owner, WalletAddress = Wallet, Blockchain = "solana" } }
            : Array.Empty<DiscordWalletConnection>());
        var sessions = new FakeGallerySessions();

        var svc = new NftGalleryService(
            provider, connections, sessions,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new NftEngineOptions { GalleryMaxItems = galleryMax, GallerySessionMinutes = 15 }),
            Options.Create(new SolanaOptions { MaxPageSize = 100 }),
            NullLogger<NftGalleryService>.Instance);
        return (svc, sessions);
    }

    [Fact]
    public async Task NoVerifiedWallet_ReturnsNoWallet()
    {
        var (svc, _) = Build(Array.Empty<SolanaAsset>(), hasWallet: false);
        var view = await svc.CreateAsync(Owner, Guild, null, default);
        Assert.Equal(GalleryViewStatus.NoWallet, view.Status);
    }

    [Fact]
    public async Task EmptyWallet_ReturnsEmpty()
    {
        var (svc, _) = Build(Array.Empty<SolanaAsset>());
        var view = await svc.CreateAsync(Owner, Guild, null, default);
        Assert.Equal(GalleryViewStatus.Empty, view.Status);
    }

    [Fact]
    public async Task SingleNft_OkWithOnePage()
    {
        var (svc, _) = Build(new[] { Nft("AAAA1111111111111111111111111111111111111111") });
        var view = await svc.CreateAsync(Owner, Guild, null, default);

        Assert.Equal(GalleryViewStatus.Ok, view.Status);
        Assert.Equal(1, view.TotalCount);
        Assert.Equal(0, view.PageIndex);
    }

    [Fact]
    public async Task MultipleNfts_PaginateAndClamp()
    {
        var owned = Enumerable.Range(0, 3).Select(i => Nft($"Asset{i}1111111111111111111111111111111111111")).ToList();
        var (svc, _) = Build(owned);

        var first = await svc.CreateAsync(Owner, Guild, null, default);
        var second = await svc.GetPageAsync(first.SessionId, Owner, 1, refresh: false, default);
        var clamped = await svc.GetPageAsync(first.SessionId, Owner, 99, refresh: false, default);

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(1, second.PageIndex);
        Assert.Equal(2, clamped.PageIndex); // clamped to last
    }

    [Fact]
    public async Task CollectionFilter_LimitsResults()
    {
        var owned = new[]
        {
            Nft("Asset1111111111111111111111111111111111111A", collection: "CollA111111111111111111111111111111111111111"),
            Nft("Asset2111111111111111111111111111111111111B", collection: "CollB111111111111111111111111111111111111111"),
        };
        var (svc, _) = Build(owned);

        var view = await svc.CreateAsync(Owner, Guild, "CollA111111111111111111111111111111111111111", default);

        Assert.Equal(GalleryViewStatus.Ok, view.Status);
        Assert.Equal(1, view.TotalCount);
    }

    [Fact]
    public async Task OtherUser_CannotNavigateSession()
    {
        var (svc, _) = Build(new[] { Nft("AAAA1111111111111111111111111111111111111111") });
        var view = await svc.CreateAsync(Owner, Guild, null, default);

        var intruder = await svc.GetPageAsync(view.SessionId, requestingUserId: 999UL, 0, refresh: false, default);

        Assert.Equal(GalleryViewStatus.NotOwner, intruder.Status);
    }

    [Fact]
    public async Task ExpiredSession_ReturnsExpired()
    {
        var (svc, sessions) = Build(new[] { Nft("AAAA1111111111111111111111111111111111111111") });
        var view = await svc.CreateAsync(Owner, Guild, null, default);

        sessions.Items[view.SessionId].ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        var expired = await svc.GetPageAsync(view.SessionId, Owner, 0, refresh: false, default);
        Assert.Equal(GalleryViewStatus.Expired, expired.Status);
    }

    [Fact]
    public async Task OwnedSet_IsCappedAndDisclosed()
    {
        var owned = Enumerable.Range(0, 5).Select(i => Nft($"Asset{i}1111111111111111111111111111111111111")).ToList();
        var (svc, _) = Build(owned, galleryMax: 2);

        var view = await svc.CreateAsync(Owner, Guild, null, default);

        Assert.Equal(2, view.TotalCount);
        Assert.True(view.Truncated);
    }

    [Fact]
    public async Task FungibleAndBurned_AreExcluded()
    {
        var owned = new[]
        {
            Nft("Nft11111111111111111111111111111111111111111"),
            Nft("Fung1111111111111111111111111111111111111111", supported: false),
            Nft("Burn1111111111111111111111111111111111111111", burned: true),
        };
        var (svc, _) = Build(owned);

        var view = await svc.CreateAsync(Owner, Guild, null, default);

        Assert.Equal(1, view.TotalCount);
    }

    private sealed class FakeOwnerProvider : ISolanaAssetProvider
    {
        private readonly IReadOnlyList<SolanaAsset> _owned;

        public FakeOwnerProvider(IReadOnlyList<SolanaAsset> owned) => _owned = owned;

        public string Name => "fake";

        public Task<SolanaAsset?> GetAssetAsync(string assetAddress, CancellationToken cancellationToken) =>
            Task.FromResult<SolanaAsset?>(null);

        public Task<PagedResult<SolanaAsset>> GetAssetsByOwnerAsync(string walletAddress, int page, int pageSize, CancellationToken cancellationToken)
        {
            var items = _owned.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PagedResult<SolanaAsset>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = _owned.Count,
            });
        }

        public Task<PagedResult<SolanaAsset>> GetAssetsByCollectionAsync(string collectionAddress, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(PagedResult<SolanaAsset>.Empty(page, pageSize));
    }

    private sealed class FakeWalletConnections : IDiscordWalletConnectionRepository
    {
        private readonly IReadOnlyList<DiscordWalletConnection> _active;

        public FakeWalletConnections(IReadOnlyList<DiscordWalletConnection> active) => _active = active;

        public Task<IReadOnlyList<DiscordWalletConnection>> ListActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken) =>
            Task.FromResult(_active);

        public Task<DiscordWalletConnection?> GetActiveByWalletAsync(string blockchain, string walletAddress, CancellationToken cancellationToken) =>
            Task.FromResult<DiscordWalletConnection?>(null);

        public Task<DiscordWalletConnection?> GetAsync(ulong discordUserId, string blockchain, string walletAddress, CancellationToken cancellationToken) =>
            Task.FromResult<DiscordWalletConnection?>(null);

        public Task<int> CountActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken) =>
            Task.FromResult(_active.Count);

        public Task UpsertAsync(DiscordWalletConnection connection, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeGallerySessions : INftGallerySessionRepository
    {
        public Dictionary<int, NftGallerySession> Items { get; } = new();

        public Task AddAsync(NftGallerySession session, CancellationToken cancellationToken)
        {
            session.Id = Items.Count + 1;
            Items[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task<NftGallerySession?> GetAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.TryGetValue(id, out var s) ? s : null);

        public Task UpdateAsync(NftGallerySession session, CancellationToken cancellationToken)
        {
            Items[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}
