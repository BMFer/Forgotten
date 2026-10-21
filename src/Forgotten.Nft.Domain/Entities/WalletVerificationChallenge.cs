using Forgotten.Nft.Domain.Enums;

namespace Forgotten.Nft.Domain.Entities;

/// <summary>
/// A short-lived, single-use wallet-verification challenge/session (spec §7.3, §9
/// "WalletVerificationChallenges").
///
/// Security notes on storage:
/// - <see cref="SessionTokenHash"/> stores only a SHA-256 hash of the URL token —
///   the token is the capability that lets a browser complete verification for this
///   Discord account, so the raw value is never persisted (spec §7.3).
/// - <see cref="Nonce"/> is stored raw because the exact signed message must be
///   reconstructed server-side at verification time; it is ephemeral, single-use,
///   and already visible to the signer, so it is not a stored secret.
/// </summary>
public class WalletVerificationChallenge
{
    public int Id { get; set; }

    public ulong DiscordUserId { get; set; }

    public string SessionTokenHash { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    /// <summary>The wallet the member chose to prove; bound once the browser connects a wallet.</summary>
    public string? WalletAddress { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public WalletVerificationStatus Status { get; set; } = WalletVerificationStatus.Pending;
}
