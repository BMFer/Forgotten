using System.ComponentModel.DataAnnotations;

namespace Forgotten.Nft.Application.Configuration;

/// <summary>
/// Strongly-typed options for the NFT engine (spec §20).
/// Bound from the "NftEngine" configuration section and validated at startup.
/// </summary>
public sealed class NftEngineOptions
{
    public const string SectionName = "NftEngine";

    public bool Enabled { get; set; } = true;

    public bool AllowUniversalNftInspection { get; set; } = true;

    public bool RequireApprovedCollectionForRoles { get; set; } = true;

    public bool RequireVerifiedWalletForGallery { get; set; } = true;

    public bool EnableOwnershipMonitoring { get; set; } = true;

    [Range(1, 1440)]
    public int OwnershipPollingMinutes { get; set; } = 5;

    [Range(1, 1440)]
    public int GallerySessionMinutes { get; set; } = 15;

    [Range(1, 100)]
    public int DefaultGalleryPageSize { get; set; } = 1;

    [Range(1, 50)]
    public int MaximumLinkedWallets { get; set; } = 5;

    /// <summary>
    /// Upper bound on how many owned NFTs a gallery loads across a member's wallets,
    /// keeping memory bounded (spec §6.2, §19). If a member owns more, the gallery
    /// discloses that it is showing the first N.
    /// </summary>
    [Range(1, 1000)]
    public int GalleryMaxItems { get; set; } = 100;

    public bool EnablePowIntegration { get; set; } = false;
}
