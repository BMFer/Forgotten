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
}
