namespace Forgotten.Nft.Domain.Enums;

/// <summary>
/// Overall verification outcome for an asset (spec §4.3). The full set is defined
/// now; Phase 1 produces the subset relevant to inspection
/// (Verified/Unsupported/NotFound/Burned/ProviderUnavailable/Indeterminate),
/// while collection/ownership-specific statuses are produced from Phase 2 onward.
/// </summary>
public enum NftVerificationStatus
{
    Verified,
    Unverified,
    Unsupported,
    NotFound,
    Burned,
    InvalidCollection,
    OwnershipMismatch,
    ProviderUnavailable,
    Indeterminate,
}
