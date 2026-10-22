using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Persists inspected NFT snapshots (spec §9 "NftAssets"/"NftAttributes").
/// Upsert replaces the stored snapshot (including attributes) for an asset.
/// </summary>
public interface INftAssetRepository
{
    Task<NftAsset?> GetByAddressAsync(string blockchain, string assetAddress, CancellationToken cancellationToken);

    Task UpsertAsync(NftAsset asset, CancellationToken cancellationToken);

    /// <summary>Tracked (non-burned) snapshots in the given collections, bounded (for the ownership monitor).</summary>
    Task<IReadOnlyList<NftAsset>> ListByCollectionsAsync(
        string blockchain, IReadOnlyCollection<string> collectionAddresses, int max, CancellationToken cancellationToken);

    /// <summary>Updates only the owner + last-fetched fields of a stored snapshot.</summary>
    Task UpdateOwnerAsync(int nftAssetId, string? newOwnerAddress, DateTime observedAt, CancellationToken cancellationToken);
}
