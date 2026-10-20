namespace Forgotten.Nft.Application.Models;

/// <summary>Normalized NFT trait (untrusted — sanitize before display, spec §8.2).</summary>
public sealed record NftAttributeData(string TraitType, string TraitValue, string? DisplayType = null);
