using Forgotten.Nft.Application.Models;

namespace Forgotten.Nft.Application.DTOs;

/// <summary>Outcome of a gallery render/navigation request.</summary>
public enum GalleryViewStatus
{
    /// <summary>A page is available (see <see cref="GalleryView.Item"/>).</summary>
    Ok,

    /// <summary>The member has a verified wallet but owns no (matching) NFTs.</summary>
    Empty,

    /// <summary>The member has no verified wallet (spec §6.2).</summary>
    NoWallet,

    /// <summary>The interaction came from someone who does not own the session (spec §12).</summary>
    NotOwner,

    /// <summary>The session no longer exists or has expired (spec §12).</summary>
    Expired,
}

/// <summary>A single-NFT gallery page plus navigation context (spec §6.2, §12).</summary>
public sealed record GalleryView
{
    public GalleryViewStatus Status { get; init; }

    public int SessionId { get; init; }

    public ulong OwnerUserId { get; init; }

    /// <summary>The NFT on the current page, or null when the gallery is empty.</summary>
    public SolanaAsset? Item { get; init; }

    /// <summary>Zero-based page index.</summary>
    public int PageIndex { get; init; }

    /// <summary>Count of NFTs in the (filtered) owned set that backs this gallery.</summary>
    public int TotalCount { get; init; }

    public string? CollectionFilter { get; init; }

    /// <summary>True when the owned set was capped (not every NFT was loaded) — disclose it in the UI.</summary>
    public bool Truncated { get; init; }

    public int PageNumber => PageIndex + 1;

    public static GalleryView Of(GalleryViewStatus status) => new() { Status = status };
}
