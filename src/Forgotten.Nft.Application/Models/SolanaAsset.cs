namespace Forgotten.Nft.Application.Models;

/// <summary>
/// Normalized, provider-agnostic representation of an on-chain asset returned by
/// <see cref="Abstractions.ISolanaAssetProvider"/> (spec §5.4). Carries the full
/// §4.1 inspection richness. The source <see cref="Provider"/> and
/// <see cref="RetrievedAt"/> are always recorded. All text fields are untrusted
/// and must be sanitized before display (spec §8.2).
/// </summary>
public sealed class SolanaAsset
{
    public required string AssetAddress { get; init; }

    /// <summary>DAS "interface" value (e.g. V1_NFT, ProgrammableNFT, V1_PRINT, FungibleToken).</summary>
    public string? InterfaceType { get; init; }

    /// <summary>True when the asset is a supported NFT rather than a fungible/other token (spec §4.2 L2).</summary>
    public bool IsSupportedNft { get; init; }

    public bool IsCompressed { get; init; }

    public bool IsBurned { get; init; }

    public bool? IsMutable { get; init; }

    public string? Name { get; init; }

    public string? Symbol { get; init; }

    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public string? AnimationUrl { get; init; }

    public string? ExternalUrl { get; init; }

    public string? MetadataUri { get; init; }

    public string? OwnerAddress { get; init; }

    public string? CollectionAddress { get; init; }

    /// <summary>On-chain collection grouping verification flag (spec §4.2 L3). Not registry approval (that is Phase 2).</summary>
    public bool CollectionVerified { get; init; }

    public string? TokenStandard { get; init; }

    public long? Supply { get; init; }

    public IReadOnlyList<SolanaCreator> Creators { get; init; } = Array.Empty<SolanaCreator>();

    public IReadOnlyList<NftAttributeData> Attributes { get; init; } = Array.Empty<NftAttributeData>();

    public required string Provider { get; init; }

    public required DateTimeOffset RetrievedAt { get; init; }
}
