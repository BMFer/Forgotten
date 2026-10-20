using System.ComponentModel.DataAnnotations;

namespace Forgotten.Bot.Configuration;

/// <summary>
/// Discord presentation options: explorer links, a fallback artwork image, and a
/// per-user command cooldown (spec §6.1 explorer links, §8.3 placeholder, §16.10 rate limiting).
/// </summary>
public sealed class PresentationOptions
{
    public const string SectionName = "Presentation";

    public string ExplorerBaseUrl { get; set; } = "https://explorer.solana.com";

    /// <summary>Optional image shown when an NFT has no displayable artwork (spec §8.3). Null = no image.</summary>
    public string? PlaceholderImageUrl { get; set; }

    [Range(0, 60)]
    public int CommandCooldownSeconds { get; set; } = 3;
}
