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
}
