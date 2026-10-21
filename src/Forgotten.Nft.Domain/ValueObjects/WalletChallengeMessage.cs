namespace Forgotten.Nft.Domain.ValueObjects;

/// <summary>
/// Builds the exact human-readable message a member signs to prove wallet control
/// (spec §7.3). The format is canonical and deterministic so the backend can
/// reconstruct and verify the precise signed bytes. The message is bound to the
/// domain, Discord account, wallet, nonce, and issue/expiry timestamps.
/// </summary>
public static class WalletChallengeMessage
{
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public static string Build(
        string domain,
        ulong discordUserId,
        string walletAddress,
        string nonce,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        return
            "FORGOTTEN Wallet Verification\n" +
            "\n" +
            $"Domain: {domain}\n" +
            $"Discord User ID: {discordUserId}\n" +
            $"Wallet Address: {walletAddress}\n" +
            $"Nonce: {nonce}\n" +
            $"Issued At: {issuedAt.ToUniversalTime().ToString(TimestampFormat)}\n" +
            $"Expires At: {expiresAt.ToUniversalTime().ToString(TimestampFormat)}\n" +
            "Purpose: Link Solana wallet";
    }
}
