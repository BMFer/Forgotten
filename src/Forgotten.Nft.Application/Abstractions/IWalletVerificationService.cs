using Forgotten.Nft.Application.DTOs;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Drives the cryptographic wallet-verification workflow (spec §7.2). The session
/// token is a one-time capability bound to the requesting Discord account; only
/// someone holding it (via the ephemeral link) can complete verification for that
/// account (spec §7.3 session binding).
/// </summary>
public interface IWalletVerificationService
{
    /// <summary>Creates a short-lived session for a member and returns the secure URL to open.</summary>
    Task<WalletConnectSession> CreateSessionAsync(ulong discordUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Binds the connected wallet to the session and returns the exact message to sign.
    /// Returns null if the session is unknown, expired, or already consumed.
    /// </summary>
    Task<WalletChallengeResult?> IssueChallengeAsync(string sessionToken, string walletAddress, CancellationToken cancellationToken);

    /// <summary>Verifies a signature for a session and, on success, associates the wallet with the member.</summary>
    Task<WalletVerificationOutcome> VerifyAsync(string sessionToken, string walletAddress, byte[] signature, CancellationToken cancellationToken);
}
