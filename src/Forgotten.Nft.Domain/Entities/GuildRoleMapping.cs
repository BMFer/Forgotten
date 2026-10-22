namespace Forgotten.Nft.Domain.Entities;

/// <summary>
/// Maps an approved collection to a Discord role for a guild (spec §11). Ownership
/// of a qualifying NFT grants the role; losing it removes the role — but only on a
/// confirmed ownership check, never because the provider was unavailable (spec §11).
/// </summary>
public class GuildRoleMapping
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    public string Blockchain { get; set; } = "solana";

    public string CollectionAddress { get; set; } = string.Empty;

    public ulong RoleId { get; set; }

    public ulong CreatedByDiscordUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
