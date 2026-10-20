namespace Forgotten.Nft.Domain.Entities;

/// <summary>
/// Persisted snapshot of an inspected NFT (spec §9 "NftAssets"). Columns mirror
/// the spec schema; richer inspection-only details (interface type, supply,
/// creators, mutability) live on the application-layer inspection result rather
/// than being persisted. A snapshot reflects what the provider reported at
/// <see cref="LastFetchedAt"/> and is never treated as proof of current ownership.
/// </summary>
public class NftAsset
{
    public int Id { get; set; }

    public string Blockchain { get; set; } = "solana";

    public string AssetAddress { get; set; } = string.Empty;

    public string? CollectionAddress { get; set; }

    public string? Name { get; set; }

    public string? Symbol { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? AnimationUrl { get; set; }

    public string? MetadataUri { get; set; }

    public string? OwnerAddress { get; set; }

    public bool IsCompressed { get; set; }

    public bool IsBurned { get; set; }

    public DateTime LastFetchedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<NftAttribute> Attributes { get; set; } = new List<NftAttribute>();
}
