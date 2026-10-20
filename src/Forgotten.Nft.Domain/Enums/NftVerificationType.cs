namespace Forgotten.Nft.Domain.Enums;

/// <summary>Kind of verification recorded in the audit log (spec §9 "NftVerificationLogs").</summary>
public enum NftVerificationType
{
    Inspect,
    Verify,
    Collection,
    Ownership,
    Eligibility,
}
