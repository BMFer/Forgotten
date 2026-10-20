using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Common;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.UnitTests.Services;

/// <summary>In-memory test doubles shared by the Phase 2 service tests.</summary>
internal sealed class FakeAssetProvider : ISolanaAssetProvider
{
    private readonly SolanaAsset? _asset;
    private readonly bool _throw;

    public FakeAssetProvider(SolanaAsset? asset = null, bool throwError = false)
    {
        _asset = asset;
        _throw = throwError;
    }

    public int CallCount { get; private set; }

    public string Name => "fake";

    public Task<SolanaAsset?> GetAssetAsync(string assetAddress, CancellationToken cancellationToken)
    {
        CallCount++;
        if (_throw)
        {
            throw new HttpRequestException("simulated provider failure");
        }

        return Task.FromResult(_asset);
    }

    public Task<PagedResult<SolanaAsset>> GetAssetsByOwnerAsync(
        string walletAddress, int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<PagedResult<SolanaAsset>> GetAssetsByCollectionAsync(
        string collectionAddress, int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult(PagedResult<SolanaAsset>.Empty(page, pageSize));
}

internal sealed class FakeCollectionRepository : INftCollectionRepository
{
    private readonly List<NftCollection> _items = new();

    private static string Key(ulong g, string b, string a) => $"{g}:{b}:{a}";

    public Task<NftCollection?> GetAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken) =>
        Task.FromResult(_items.FirstOrDefault(c =>
            Key(c.GuildId, c.Blockchain, c.CollectionAddress) == Key(guildId, blockchain, collectionAddress)));

    public Task<IReadOnlyList<NftCollection>> ListAsync(ulong guildId, string blockchain, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<NftCollection>>(
            _items.Where(c => c.GuildId == guildId && c.Blockchain == blockchain).ToList());

    public Task UpsertAsync(NftCollection collection, CancellationToken cancellationToken)
    {
        var existing = _items.FirstOrDefault(c =>
            Key(c.GuildId, c.Blockchain, c.CollectionAddress) == Key(collection.GuildId, collection.Blockchain, collection.CollectionAddress));
        if (existing is not null)
        {
            _items.Remove(existing);
        }
        else if (collection.Id == 0)
        {
            collection.Id = _items.Count + 1;
        }

        _items.Add(collection);
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken)
    {
        var existing = _items.FirstOrDefault(c =>
            Key(c.GuildId, c.Blockchain, c.CollectionAddress) == Key(guildId, blockchain, collectionAddress));
        if (existing is null)
        {
            return Task.FromResult(false);
        }

        _items.Remove(existing);
        return Task.FromResult(true);
    }
}

internal sealed class FakeVerificationLogRepository : INftVerificationLogRepository
{
    public List<NftVerificationLog> Entries { get; } = new();

    public Task AddAsync(NftVerificationLog log, CancellationToken cancellationToken)
    {
        Entries.Add(log);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NftVerificationLog>> GetRecentAsync(ulong guildId, int count, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<NftVerificationLog>>(
            Entries.Where(l => l.GuildId == guildId).OrderByDescending(l => l.CheckedAt).Take(count).ToList());
}
