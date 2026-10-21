namespace Forgotten.Nft.Domain.Enums;

/// <summary>Lifecycle of a wallet-verification challenge/session (spec §7).</summary>
public enum WalletVerificationStatus
{
    /// <summary>Created; awaiting a signed challenge.</summary>
    Pending,

    /// <summary>Signature verified and wallet associated.</summary>
    Verified,

    /// <summary>Expired before completion.</summary>
    Expired,

    /// <summary>Already used — single-use enforcement (replay protection).</summary>
    Consumed,

    /// <summary>Verification attempted and failed.</summary>
    Failed,
}
