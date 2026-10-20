using Forgotten.Nft.Application.Models;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Resolves off-chain token metadata from a URI (HTTPS/IPFS/Arweave — spec §8.1).
/// Implementations treat metadata as untrusted input and MUST enforce size/time
/// limits, scheme/content-type validation, redirect limits, and SSRF protection
/// (spec §8.2, §16.8). Resolution failures return null rather than throwing, so a
/// missing metadata document never blocks inspection.
/// </summary>
public interface IMetadataResolver
{
    Task<ResolvedMetadata?> ResolveAsync(string metadataUri, CancellationToken cancellationToken);
}
