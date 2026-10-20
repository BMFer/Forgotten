namespace Forgotten.Nft.Domain.Enums;

/// <summary>
/// The distinct verification levels from spec §4.2. Levels are independent and
/// cumulative: a valid NFT (L2) is not automatically collection-verified (L3),
/// approved (L4), or owned (L5/L6). Phase 2 evaluates up to L4; L5–L7 arrive in
/// later phases.
/// </summary>
public enum NftVerificationLevel
{
    None = 0,
    AssetExists = 1,
    NftClassified = 2,
    CollectionVerified = 3,
    CollectionApproved = 4,
    WalletOwnership = 5,
    MemberOwnership = 6,
    ApplicationEligible = 7,
}
