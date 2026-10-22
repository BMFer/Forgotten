using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Common;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Application.Services;
using Forgotten.Nft.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.UnitTests.Services;

public class RoleReconciliationServiceTests
{
    private const ulong Guild = 1UL;
    private const ulong User = 50UL;
    private const ulong RoleId = 777UL;
    private const string Wallet = "Wallet11111111111111111111111111111111111111";
    private const string Collection = "Coll1111111111111111111111111111111111111111";

    private static (RoleReconciliationService svc, FakeRoleSvc roles) Build(
        IReadOnlyList<SolanaAsset>? owned, bool providerThrows = false, bool userHasRole = false)
    {
        var mappings = new FakeRoleMapRepo();
        mappings.Items.Add(new GuildRoleMapping { GuildId = Guild, Blockchain = "solana", CollectionAddress = Collection, RoleId = RoleId });

        var connections = new FakeReconConn(new[]
        {
            new DiscordWalletConnection { DiscordUserId = User, WalletAddress = Wallet, Blockchain = "solana" },
        });

        var provider = new ReconProvider(owned ?? Array.Empty<SolanaAsset>(), providerThrows);
        var roles = new FakeRoleSvc();
        if (userHasRole)
        {
            roles.Granted.Add((Guild, User, RoleId));
        }

        var svc = new RoleReconciliationService(
            mappings, connections, provider, roles,
            Options.Create(new NftEngineOptions { GalleryMaxItems = 100 }),
            Options.Create(new SolanaOptions { MaxPageSize = 100 }),
            NullLogger<RoleReconciliationService>.Instance);
        return (svc, roles);
    }

    private static SolanaAsset NftIn(string collection) => new()
    {
        AssetAddress = "A", IsSupportedNft = true, CollectionAddress = collection, Provider = "fake", RetrievedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task OwnsQualifyingNft_GrantsRole()
    {
        var (svc, roles) = Build(owned: new[] { NftIn(Collection) }, userHasRole: false);

        await svc.ReconcileMemberAsync(Guild, User, default);

        Assert.Contains(RoleId, roles.Assigned);
    }

    [Fact]
    public async Task NoLongerOwns_RemovesRole()
    {
        var (svc, roles) = Build(owned: new[] { NftIn("SomeOtherCollection1111111111111111111111111") }, userHasRole: true);

        await svc.ReconcileMemberAsync(Guild, User, default);

        Assert.Contains(RoleId, roles.Removed);
    }

    [Fact]
    public async Task ProviderUnavailable_DoesNotRemoveRole()
    {
        var (svc, roles) = Build(owned: null, providerThrows: true, userHasRole: true);

        await svc.ReconcileMemberAsync(Guild, User, default);

        Assert.Empty(roles.Removed); // grace: never strip on an outage
    }

    [Fact]
    public async Task OwnsAndAlreadyHasRole_NoChange()
    {
        var (svc, roles) = Build(owned: new[] { NftIn(Collection) }, userHasRole: true);

        await svc.ReconcileMemberAsync(Guild, User, default);

        Assert.Empty(roles.Assigned);
        Assert.Empty(roles.Removed);
    }

    private sealed class ReconProvider : ISolanaAssetProvider
    {
        private readonly IReadOnlyList<SolanaAsset> _owned;
        private readonly bool _throws;

        public ReconProvider(IReadOnlyList<SolanaAsset> owned, bool throws)
        {
            _owned = owned;
            _throws = throws;
        }

        public string Name => "fake";

        public Task<SolanaAsset?> GetAssetAsync(string assetAddress, CancellationToken cancellationToken) =>
            Task.FromResult<SolanaAsset?>(null);

        public Task<PagedResult<SolanaAsset>> GetAssetsByOwnerAsync(string walletAddress, int page, int pageSize, CancellationToken cancellationToken)
        {
            if (_throws)
            {
                throw new HttpRequestException("provider down");
            }

            // single page
            return Task.FromResult(new PagedResult<SolanaAsset>
            {
                Items = page == 1 ? _owned : Array.Empty<SolanaAsset>(),
                Page = page,
                PageSize = pageSize,
                TotalItems = _owned.Count,
            });
        }

        public Task<PagedResult<SolanaAsset>> GetAssetsByCollectionAsync(string collectionAddress, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(PagedResult<SolanaAsset>.Empty(page, pageSize));
    }

    private sealed class FakeRoleMapRepo : IGuildRoleMappingRepository
    {
        public List<GuildRoleMapping> Items { get; } = new();

        public Task<IReadOnlyList<GuildRoleMapping>> ListByGuildAsync(ulong guildId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GuildRoleMapping>>(Items.Where(m => m.GuildId == guildId).ToList());

        public Task<IReadOnlyList<GuildRoleMapping>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GuildRoleMapping>>(Items.ToList());

        public Task<GuildRoleMapping?> GetAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(m => m.GuildId == guildId && m.CollectionAddress == collectionAddress));

        public Task UpsertAsync(GuildRoleMapping mapping, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> RemoveAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class FakeReconConn : IDiscordWalletConnectionRepository
    {
        private readonly IReadOnlyList<DiscordWalletConnection> _active;

        public FakeReconConn(IReadOnlyList<DiscordWalletConnection> active) => _active = active;

        public Task<IReadOnlyList<DiscordWalletConnection>> ListActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken) =>
            Task.FromResult(_active);

        public Task<DiscordWalletConnection?> GetActiveByWalletAsync(string blockchain, string walletAddress, CancellationToken cancellationToken) =>
            Task.FromResult<DiscordWalletConnection?>(null);

        public Task<DiscordWalletConnection?> GetAsync(ulong discordUserId, string blockchain, string walletAddress, CancellationToken cancellationToken) =>
            Task.FromResult<DiscordWalletConnection?>(null);

        public Task<int> CountActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken) =>
            Task.FromResult(_active.Count);

        public Task<IReadOnlyList<ulong>> ListActiveUserIdsAsync(int max, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ulong>>(_active.Select(c => c.DiscordUserId).Distinct().ToList());

        public Task UpsertAsync(DiscordWalletConnection connection, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeRoleSvc : IDiscordRoleService
    {
        public HashSet<(ulong Guild, ulong User, ulong Role)> Granted { get; } = new();
        public List<ulong> Assigned { get; } = new();
        public List<ulong> Removed { get; } = new();

        public Task<bool> HasRoleAsync(ulong guildId, ulong userId, ulong roleId, CancellationToken cancellationToken) =>
            Task.FromResult(Granted.Contains((guildId, userId, roleId)));

        public Task<bool> AssignRoleAsync(ulong guildId, ulong userId, ulong roleId, string reason, CancellationToken cancellationToken)
        {
            Assigned.Add(roleId);
            Granted.Add((guildId, userId, roleId));
            return Task.FromResult(true);
        }

        public Task<bool> RemoveRoleAsync(ulong guildId, ulong userId, ulong roleId, string reason, CancellationToken cancellationToken)
        {
            Removed.Add(roleId);
            Granted.Remove((guildId, userId, roleId));
            return Task.FromResult(true);
        }
    }
}
