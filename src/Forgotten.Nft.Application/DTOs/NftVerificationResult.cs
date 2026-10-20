using Forgotten.Nft.Domain.Enums;

namespace Forgotten.Nft.Application.DTOs;

/// <summary>
/// Layered verification outcome (spec §4.3). Each fact is independent: a supported
/// NFT is not automatically collection-verified, approved, or owned. Phase 2
/// populates through L4 (approval); ownership fields are present but not evaluated
/// until Phases 3/5.
/// </summary>
public sealed record NftVerificationResult
{
    public required string AssetAddress { get; init; }

    public bool AssetExists { get; init; }

    public bool IsSupportedNft { get; init; }

    /// <summary>On-chain collection grouping verification (spec §4.2 L3).</summary>
    public bool CollectionVerified { get; init; }

    /// <summary>Present in this guild's approved registry (spec §4.2 L4) — distinct from on-chain verification.</summary>
    public bool CollectionApproved { get; init; }

    public string? OwnerAddress { get; init; }

    /// <summary>Authoritative wallet ownership (spec §4.2 L5) — not evaluated before Phase 5.</summary>
    public bool OwnershipVerified { get; init; }

    /// <summary>Requesting member proved control of the owning wallet (spec §4.2 L6) — not before Phase 3.</summary>
    public bool MemberOwnershipVerified { get; init; }

    public NftVerificationStatus VerificationStatus { get; init; }

    public NftVerificationLevel HighestLevel { get; init; }

    public DateTimeOffset VerificationTimestamp { get; init; }

    public string? Provider { get; init; }

    public string? Name { get; init; }

    public string? CollectionAddress { get; init; }

    /// <summary>Registry display name when the collection is approved.</summary>
    public string? CollectionName { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => ErrorCode is null;

    public static NftVerificationResult Failure(
        string assetAddress, NftVerificationStatus status, string errorCode, string errorMessage) =>
        new()
        {
            AssetAddress = assetAddress,
            VerificationStatus = status,
            HighestLevel = NftVerificationLevel.None,
            VerificationTimestamp = DateTimeOffset.UtcNow,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
        };
}
