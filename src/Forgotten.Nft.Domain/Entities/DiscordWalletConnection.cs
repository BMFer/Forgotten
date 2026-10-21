namespace Forgotten.Nft.Domain.Entities;

/// <summary>
/// A cryptographically proven link between a Discord member and a Solana wallet
/// (spec §9 "DiscordWalletConnections", §7.4). A connection only exists after a
/// valid Ed25519 signature — never from a submitted address alone (spec §7.1).
/// </summary>
public class DiscordWalletConnection
{
    public int Id { get; set; }

    public ulong DiscordUserId { get; set; }

    public string WalletAddress { get; set; } = string.Empty;

    public string Blockchain { get; set; } = "solana";

    public bool IsPrimary { get; set; }

    public DateTime VerifiedAt { get; set; }

    public DateTime LastVerifiedAt { get; set; }

    /// <summary>Set when the member disconnects; a non-null value means the link is inactive.</summary>
    public DateTime? DisconnectedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive => DisconnectedAt is null;
}
