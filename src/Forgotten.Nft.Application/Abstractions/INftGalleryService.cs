using Forgotten.Nft.Application.DTOs;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Backs the interactive NFT gallery (spec §6.2, §12). Owned NFTs are fetched
/// across the member's verified wallets with bounded paging (never the whole
/// inventory — spec §6.2, §19) and cached briefly. Every navigation validates that
/// the requesting member owns the session.
/// </summary>
public interface INftGalleryService
{
    /// <summary>Creates a session for the member and returns the first page.</summary>
    Task<GalleryView> CreateAsync(ulong discordUserId, ulong guildId, string? collectionFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the requested page for an existing session. Returns
    /// <see cref="GalleryViewStatus.NotOwner"/> if <paramref name="requestingUserId"/> is not the
    /// session owner, or <see cref="GalleryViewStatus.Expired"/> if the session is gone/expired.
    /// </summary>
    Task<GalleryView> GetPageAsync(int sessionId, ulong requestingUserId, int page, bool refresh, CancellationToken cancellationToken);

    /// <summary>Toggles the collection filter to the current page's collection (or clears it) and returns page 0.</summary>
    Task<GalleryView> ToggleCollectionFilterAsync(int sessionId, ulong requestingUserId, int currentPage, CancellationToken cancellationToken);
}
