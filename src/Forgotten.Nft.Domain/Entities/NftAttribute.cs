namespace Forgotten.Nft.Domain.Entities;

/// <summary>
/// A single NFT trait (spec §9 "NftAttributes"). Trait values originate from
/// untrusted metadata and must be sanitized before display (spec §8.2).
/// </summary>
public class NftAttribute
{
    public int Id { get; set; }

    public int NftAssetId { get; set; }

    public string TraitType { get; set; } = string.Empty;

    public string TraitValue { get; set; } = string.Empty;

    public string? DisplayType { get; set; }

    public NftAsset? NftAsset { get; set; }
}
