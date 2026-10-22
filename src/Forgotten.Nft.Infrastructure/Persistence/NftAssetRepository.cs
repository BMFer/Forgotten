using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed <see cref="INftAssetRepository"/>. Upsert replaces the stored
/// snapshot and its attributes within a single SaveChanges (spec §9.2 transactions).
/// </summary>
public sealed class NftAssetRepository : INftAssetRepository
{
    private readonly ForgottenDbContext _dbContext;

    public NftAssetRepository(ForgottenDbContext dbContext) => _dbContext = dbContext;

    public Task<NftAsset?> GetByAddressAsync(string blockchain, string assetAddress, CancellationToken cancellationToken) =>
        _dbContext.NftAssets
            .Include(a => a.Attributes)
            .FirstOrDefaultAsync(
                a => a.Blockchain == blockchain && a.AssetAddress == assetAddress,
                cancellationToken);

    public async Task UpsertAsync(NftAsset asset, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var existing = await _dbContext.NftAssets
            .Include(a => a.Attributes)
            .FirstOrDefaultAsync(
                a => a.Blockchain == asset.Blockchain && a.AssetAddress == asset.AssetAddress,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            asset.CreatedAt = now;
            asset.UpdatedAt = now;
            _dbContext.NftAssets.Add(asset);
        }
        else
        {
            existing.CollectionAddress = asset.CollectionAddress;
            existing.Name = asset.Name;
            existing.Symbol = asset.Symbol;
            existing.Description = asset.Description;
            existing.ImageUrl = asset.ImageUrl;
            existing.AnimationUrl = asset.AnimationUrl;
            existing.MetadataUri = asset.MetadataUri;
            existing.OwnerAddress = asset.OwnerAddress;
            existing.IsCompressed = asset.IsCompressed;
            existing.IsBurned = asset.IsBurned;
            existing.LastFetchedAt = asset.LastFetchedAt;
            existing.UpdatedAt = now;

            // Replace attributes wholesale.
            _dbContext.NftAttributes.RemoveRange(existing.Attributes);
            existing.Attributes = asset.Attributes
                .Select(a => new NftAttribute
                {
                    TraitType = a.TraitType,
                    TraitValue = a.TraitValue,
                    DisplayType = a.DisplayType,
                })
                .ToList();
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<NftAsset>> ListByCollectionsAsync(
        string blockchain, IReadOnlyCollection<string> collectionAddresses, int max, CancellationToken cancellationToken)
    {
        if (collectionAddresses.Count == 0)
        {
            return Array.Empty<NftAsset>();
        }

        var collections = collectionAddresses.ToList();

        // Stalest snapshots first so each sweep makes progress across the set.
        return await _dbContext.NftAssets
            .Where(a => a.Blockchain == blockchain
                && !a.IsBurned
                && a.CollectionAddress != null
                && collections.Contains(a.CollectionAddress))
            .OrderBy(a => a.LastFetchedAt)
            .Take(Math.Clamp(max, 1, 500))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateOwnerAsync(int nftAssetId, string? newOwnerAddress, DateTime observedAt, CancellationToken cancellationToken)
    {
        var asset = await _dbContext.NftAssets.FindAsync(new object[] { nftAssetId }, cancellationToken).ConfigureAwait(false);
        if (asset is null)
        {
            return;
        }

        asset.OwnerAddress = newOwnerAddress;
        asset.LastFetchedAt = observedAt;
        asset.UpdatedAt = observedAt;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
