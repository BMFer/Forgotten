using Forgotten.Nft.Application.DTOs;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Inspects a single Solana NFT: validates the address, queries the provider,
/// normalizes metadata, classifies the asset, persists a snapshot, and returns a
/// display-ready result (spec §4.1).
/// </summary>
public interface INftInspectionService
{
    /// <param name="refresh">When true, bypasses the cache and forces a fresh provider fetch (backs <c>/nft refresh</c>).</param>
    Task<NftInspectionResult> InspectAsync(
        string assetAddress,
        bool refresh = false,
        CancellationToken cancellationToken = default);
}
