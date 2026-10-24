using System.ComponentModel.DataAnnotations;

namespace Forgotten.Bot.Configuration;

/// <summary>
/// Discord connection options. The bot token is a secret and must be supplied via
/// environment variable (<c>Discord__Token</c>), user secrets, or another secure
/// configuration provider — never committed to source control (spec §5.2, §16.11).
/// </summary>
public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    [Required(AllowEmptyStrings = false, ErrorMessage = "A Discord bot token is required to start the bot.")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Optional guild ID for fast, guild-scoped slash-command registration during
    /// development. When unset, commands register globally (slower propagation).
    /// </summary>
    public ulong? DevGuildId { get; set; }

    /// <summary>
    /// Optional "holders" role. When set, only members who hold this role may open
    /// <c>/nft showcase</c> and flex — making flexing a holders-only perk. Unset = open to all.
    /// </summary>
    public ulong? FlexRoleId { get; set; }
}
