using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Common;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Application.Services;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forgotten.Nft.UnitTests.Services;

public class OwnershipMonitorTests
{
    private const string Collection = "Coll1111111111111111111111111111111111111111";
    private const string Asset = "Asset111111111111111111111111111111111111111";

    private static (OwnershipMonitor monitor, MonitorAssetRepo assets, FakeHistoryRepo history, RecordingEventPublisher events) Build(
        string? storedOwner, string? freshOwner, bool providerThrows = false, bool providerNotFound = false)
    {
        var assets = new MonitorAssetRepo
        {
            Tracked = { new NftAsset { Id = 1, AssetAddress = Asset, CollectionAddress = Collection, OwnerAddress = storedOwner } },
        };
        var collections = new FakeCollectionRepository();
        collections.UpsertAsync(new NftCollection { GuildId = 1, Blockchain = "solana", CollectionAddress = Collection, IsApproved = true }, default);

        var provider = new MonitorProvider(freshOwner, providerThrows, providerNotFound);
        var history = new FakeHistoryRepo();
        var events = new RecordingEventPublisher();

        var monitor = new OwnershipMonitor(
            provider, assets, collections, history, events,
            new MemoryCache(new MemoryCacheOptions()), NullLogger<OwnershipMonitor>.Instance);
        return (monitor, assets, history, events);
    }

    [Fact]
    public async Task OwnerChanged_RecordsObservedHistoryAndPublishesEvent()
    {
        var (monitor, assets, history, events) = Build(storedOwner: "OwnerA", freshOwner: "OwnerB");

        var changes = await monitor.RunSweepAsync(50, default);

        Assert.Equal(1, changes);
        var entry = Assert.Single(history.Entries);
        Assert.Equal("OwnerA", entry.PreviousOwnerAddress);
        Assert.Equal("OwnerB", entry.NewOwnerAddress);
        Assert.Null(entry.TransactionSignature); // observed — never fabricated
        Assert.Equal(NftOwnershipHistory.ObservedSource, entry.Source);
        Assert.Contains(events.Published, e => e is NftOwnershipChangedEvent);
        Assert.Single(assets.OwnerUpdates);
    }

    [Fact]
    public async Task NoChange_RecordsNothing()
    {
        var (monitor, _, history, events) = Build(storedOwner: "OwnerA", freshOwner: "OwnerA");

        var changes = await monitor.RunSweepAsync(50, default);

        Assert.Equal(0, changes);
        Assert.Empty(history.Entries);
        Assert.Empty(events.Published);
    }

    [Fact]
    public async Task ProviderFailure_IsNotTreatedAsChange()
    {
        var (monitor, _, history, events) = Build(storedOwner: "OwnerA", freshOwner: null, providerThrows: true);

        var changes = await monitor.RunSweepAsync(50, default);

        Assert.Equal(0, changes);
        Assert.Empty(history.Entries);
        Assert.Empty(events.Published);
    }

    [Fact]
    public async Task NotFound_IsNotTreatedAsTransfer()
    {
        var (monitor, _, history, _) = Build(storedOwner: "OwnerA", freshOwner: null, providerNotFound: true);

        var changes = await monitor.RunSweepAsync(50, default);

        Assert.Equal(0, changes);
        Assert.Empty(history.Entries);
    }

    private sealed class MonitorProvider : ISolanaAssetProvider
    {
        private readonly string? _freshOwner;
        private readonly bool _throws;
        private readonly bool _notFound;

        public MonitorProvider(string? freshOwner, bool throws, bool notFound)
        {
            _freshOwner = freshOwner;
            _throws = throws;
            _notFound = notFound;
        }

        public string Name => "fake";

        public Task<SolanaAsset?> GetAssetAsync(string assetAddress, CancellationToken cancellationToken)
        {
            if (_throws)
            {
                throw new HttpRequestException("provider down");
            }

            if (_notFound)
            {
                return Task.FromResult<SolanaAsset?>(null);
            }

            return Task.FromResult<SolanaAsset?>(new SolanaAsset
            {
                AssetAddress = assetAddress,
                IsSupportedNft = true,
                OwnerAddress = _freshOwner,
                Provider = "fake",
                RetrievedAt = DateTimeOffset.UtcNow,
            });
        }

        public Task<PagedResult<SolanaAsset>> GetAssetsByOwnerAsync(string walletAddress, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(PagedResult<SolanaAsset>.Empty(page, pageSize));

        public Task<PagedResult<SolanaAsset>> GetAssetsByCollectionAsync(string collectionAddress, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(PagedResult<SolanaAsset>.Empty(page, pageSize));
    }

    private sealed class MonitorAssetRepo : INftAssetRepository
    {
        public List<NftAsset> Tracked { get; } = new();
        public List<(int Id, string? Owner)> OwnerUpdates { get; } = new();

        public Task<NftAsset?> GetByAddressAsync(string blockchain, string assetAddress, CancellationToken cancellationToken) =>
            Task.FromResult<NftAsset?>(null);

        public Task UpsertAsync(NftAsset asset, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<NftAsset>> ListByCollectionsAsync(
            string blockchain, IReadOnlyCollection<string> collectionAddresses, int max, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NftAsset>>(Tracked);

        public Task UpdateOwnerAsync(int nftAssetId, string? newOwnerAddress, DateTime observedAt, CancellationToken cancellationToken)
        {
            OwnerUpdates.Add((nftAssetId, newOwnerAddress));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHistoryRepo : INftOwnershipHistoryRepository
    {
        public List<NftOwnershipHistory> Entries { get; } = new();

        public Task AddAsync(NftOwnershipHistory entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingEventPublisher : IDomainEventPublisher
    {
        public List<IDomainEvent> Published { get; } = new();

        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            Published.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
