namespace Forgotten.Nft.Domain.Entities;

/// <summary>
/// Tracks an interactive gallery browsing session (spec §9 "NftGallerySessions",
/// §12). The session is owned by a single member; component interactions must be
/// rejected for anyone else (spec §6.2, §12, §16.18). Navigation state (the page)
/// travels in the button custom-id to avoid a DB write per click (spec §9.2);
/// the stored <see cref="CurrentPage"/>/<see cref="CollectionFilter"/> change only
/// on filter toggles.
/// </summary>
public class NftGallerySession
{
    public int Id { get; set; }

    public ulong DiscordUserId { get; set; }

    public ulong GuildId { get; set; }

    /// <summary>Primary wallet at creation (informational); the view resolves all active wallets fresh.</summary>
    public string? WalletAddress { get; set; }

    public string? CollectionFilter { get; set; }

    public int CurrentPage { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
