namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Applies Discord role changes (spec §11). Implementations must respect the role
/// hierarchy and the bot's permissions, must never assign privileged/administrator
/// roles, and should fail safe (no-op + log) when a change can't be made rather than
/// throwing. Returns false when the change was not applied.
/// </summary>
public interface IDiscordRoleService
{
    Task<bool> HasRoleAsync(ulong guildId, ulong userId, ulong roleId, CancellationToken cancellationToken);

    Task<bool> AssignRoleAsync(ulong guildId, ulong userId, ulong roleId, string reason, CancellationToken cancellationToken);

    Task<bool> RemoveRoleAsync(ulong guildId, ulong userId, ulong roleId, string reason, CancellationToken cancellationToken);
}
