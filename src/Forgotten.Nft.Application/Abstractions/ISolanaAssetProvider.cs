using Forgotten.Nft.Application.Common;
using Forgotten.Nft.Application.Models;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Abstraction over a Solana DAS-compatible asset provider (spec §5.1).
/// Implementations must be replaceable and must treat all provider responses as
/// untrusted until validated. A transient failure or timeout must surface as an
/// error distinct from "asset not found" (spec §4.3, §16).
/// </summary>
public interface ISolanaAssetProvider
{
    /// <summary>Short identifier of the underlying provider (for diagnostics/audit).</summary>
    string Name { get; }

    /// <summary>
    /// Fetches a single asset. Returns null only when the provider authoritatively
    /// reports the asset does not exist — never on timeout/transport failure (those throw).
    /// </summary>
    Task<SolanaAsset?> GetAssetAsync(string assetAddress, CancellationToken cancellationToken);

    /// <summary>Paged assets owned by a wallet (implemented in Phase 4).</summary>
    Task<PagedResult<SolanaAsset>> GetAssetsByOwnerAsync(
        string walletAddress, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Paged assets in a collection group (implemented in Phase 2/4).</summary>
    Task<PagedResult<SolanaAsset>> GetAssetsByCollectionAsync(
        string collectionAddress, int page, int pageSize, CancellationToken cancellationToken);
}
