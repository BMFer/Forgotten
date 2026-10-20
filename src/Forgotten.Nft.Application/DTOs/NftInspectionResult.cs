using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Domain.Enums;

namespace Forgotten.Nft.Application.DTOs;

/// <summary>
/// Outcome of inspecting a single asset (spec §4.1/§4.3). Carries normalized,
/// display-ready data plus the verification status and, on failure, a user-facing
/// error code/message. A provider failure is reported as
/// <see cref="NftVerificationStatus.ProviderUnavailable"/> — never as NotFound.
/// </summary>
public sealed record NftInspectionResult
{
    public required string AssetAddress { get; init; }

    public NftVerificationStatus Status { get; init; }

    public bool AssetExists { get; init; }

    public bool IsSupportedNft { get; init; }

    public string? InterfaceType { get; init; }

    public string? Name { get; init; }

    public string? Symbol { get; init; }

    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public string? AnimationUrl { get; init; }

    public string? ExternalUrl { get; init; }

    public string? MetadataUri { get; init; }

    public string? OwnerAddress { get; init; }

    public string? CollectionAddress { get; init; }

    /// <summary>On-chain grouping verification (spec §4.2 L3) — NOT registry approval (Phase 2).</summary>
    public bool CollectionVerified { get; init; }

    public string? TokenStandard { get; init; }

    public long? Supply { get; init; }

    public bool IsCompressed { get; init; }

    public bool IsBurned { get; init; }

    public bool? IsMutable { get; init; }

    public IReadOnlyList<SolanaCreator> Creators { get; init; } = Array.Empty<SolanaCreator>();

    public IReadOnlyList<NftAttributeData> Attributes { get; init; } = Array.Empty<NftAttributeData>();

    public string? Provider { get; init; }

    public DateTimeOffset RetrievedAt { get; init; }

    public bool ServedFromCache { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => ErrorCode is null;

    public static NftInspectionResult Failure(
        string assetAddress, NftVerificationStatus status, string errorCode, string errorMessage) =>
        new()
        {
            AssetAddress = assetAddress,
            Status = status,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            RetrievedAt = DateTimeOffset.UtcNow,
        };
}
