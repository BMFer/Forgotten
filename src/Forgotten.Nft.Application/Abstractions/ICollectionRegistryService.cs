using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Administrator-managed, per-guild collection approval registry (spec §13).
/// Approval here is a server-administration decision and is independent of a
/// collection's on-chain verification status.
/// </summary>
public interface ICollectionRegistryService
{
    /// <summary>Approves (adds or updates) a collection for a guild, recording the approving admin.</summary>
    Task<NftCollection> ApproveAsync(
        ulong guildId, string collectionAddress, string? displayName, ulong approvedByUserId, CancellationToken cancellationToken);

    /// <summary>Removes a collection's registry entry for a guild. Returns false if it was not present.</summary>
    Task<bool> RemoveAsync(ulong guildId, string collectionAddress, CancellationToken cancellationToken);

    Task<IReadOnlyList<NftCollection>> ListAsync(ulong guildId, CancellationToken cancellationToken);

    /// <summary>Approved collections for a guild (what members can flex in the gallery).</summary>
    Task<IReadOnlyList<NftCollection>> ListApprovedAsync(ulong guildId, CancellationToken cancellationToken);

    Task<NftCollection?> GetAsync(ulong guildId, string collectionAddress, CancellationToken cancellationToken);

    Task<bool> IsApprovedAsync(ulong guildId, string collectionAddress, CancellationToken cancellationToken);

    /// <summary>
    /// The collection <c>/nft gallery</c> defaults to for a guild: the flagged default, or — as a
    /// fallback for guilds approved before defaults existed — the sole approved collection. Null if
    /// the guild has no (or ambiguously many) approved collections and none is flagged.
    /// </summary>
    Task<NftCollection?> GetDefaultAsync(ulong guildId, CancellationToken cancellationToken);

    /// <summary>Sets the guild's default collection. Returns false if it is not an approved collection.</summary>
    Task<bool> SetDefaultAsync(ulong guildId, string collectionAddress, CancellationToken cancellationToken);
}
