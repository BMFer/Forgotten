using Forgotten.Nft.Application.DTOs;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Produces a layered verification result for an asset (spec §4.2/§4.3). The guild
/// context is required to evaluate registry approval (L4). Verification uses
/// sufficiently fresh provider data and never serves ownership from a stale cache
/// (spec §6.4, §15).
/// </summary>
public interface INftVerificationService
{
    Task<NftVerificationResult> VerifyAsync(
        ulong guildId,
        string assetAddress,
        ulong? requestingUserId = null,
        CancellationToken cancellationToken = default);
}
