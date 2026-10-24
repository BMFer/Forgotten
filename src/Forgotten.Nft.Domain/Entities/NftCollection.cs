namespace Forgotten.Nft.Domain.Entities;

/// <summary>
/// An administrator-managed collection registry entry (spec §9 "NftCollections",
/// §13). Registry approval is scoped per Discord guild (spec §13) and is distinct
/// from a collection's on-chain verification status — the two must never be
/// conflated (spec §13). <see cref="GuildId"/> is added beyond the literal §9
/// column list to satisfy the §13 per-guild scoping requirement.
/// </summary>
public class NftCollection
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    public string Blockchain { get; set; } = "solana";

    public string CollectionAddress { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public bool IsApproved { get; set; }

    /// <summary>
    /// The guild's default collection — what <c>/nft gallery</c> shows first so members
    /// flex this server's collection without passing an address. At most one per guild;
    /// the first approved collection becomes the default automatically (spec §13).
    /// </summary>
    public bool IsDefault { get; set; }

    public ulong? ApprovedByDiscordUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
