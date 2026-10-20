namespace Forgotten.Nft.Application.Models;

/// <summary>
/// Off-chain metadata resolved from a token URI (spec §8.1). Every field is
/// optional — do not assume an NFT has an image, description, or attributes
/// (spec §5.4, §8.1). Values are untrusted and must be sanitized before display.
/// </summary>
public sealed class ResolvedMetadata
{
    public string? Name { get; init; }

    public string? Symbol { get; init; }

    public string? Description { get; init; }

    public string? Image { get; init; }

    public string? AnimationUrl { get; init; }

    public string? ExternalUrl { get; init; }

    public IReadOnlyList<NftAttributeData> Attributes { get; init; } = Array.Empty<NftAttributeData>();
}
