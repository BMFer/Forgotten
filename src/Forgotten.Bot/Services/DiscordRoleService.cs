using Discord;
using Discord.WebSocket;
using Forgotten.Nft.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Forgotten.Bot.Services;

/// <summary>
/// Applies role changes through Discord (spec §11). Fails safe: refuses privileged
/// roles (Administrator / Manage Roles / Manage Guild), respects the role hierarchy
/// (won't touch a role at or above the bot's highest role), and no-ops with a log
/// when a change can't be made rather than throwing. Every change carries an audit
/// reason (spec §11, §16.9/§16.14).
/// </summary>
public sealed class DiscordRoleService : IDiscordRoleService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordRoleService> _logger;

    public DiscordRoleService(DiscordSocketClient client, ILogger<DiscordRoleService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> HasRoleAsync(ulong guildId, ulong userId, ulong roleId, CancellationToken cancellationToken)
    {
        var member = await _client.Rest.GetGuildUserAsync(guildId, userId).ConfigureAwait(false);
        return member is not null && member.RoleIds.Contains(roleId);
    }

    public async Task<bool> AssignRoleAsync(ulong guildId, ulong userId, ulong roleId, string reason, CancellationToken cancellationToken)
    {
        if (!TryValidateRole(guildId, roleId, out _))
        {
            return false;
        }

        var member = await _client.Rest.GetGuildUserAsync(guildId, userId).ConfigureAwait(false);
        if (member is null || member.RoleIds.Contains(roleId))
        {
            return false;
        }

        await member.AddRoleAsync(roleId, new RequestOptions { AuditLogReason = $"FORGOTTEN: {reason}" }).ConfigureAwait(false);
        _logger.LogInformation("Assigned role {Role} to {User} in {Guild} ({Reason}).", roleId, userId, guildId, reason);
        return true;
    }

    public async Task<bool> RemoveRoleAsync(ulong guildId, ulong userId, ulong roleId, string reason, CancellationToken cancellationToken)
    {
        if (!TryValidateRole(guildId, roleId, out _))
        {
            return false;
        }

        var member = await _client.Rest.GetGuildUserAsync(guildId, userId).ConfigureAwait(false);
        if (member is null || !member.RoleIds.Contains(roleId))
        {
            return false;
        }

        await member.RemoveRoleAsync(roleId, new RequestOptions { AuditLogReason = $"FORGOTTEN: {reason}" }).ConfigureAwait(false);
        _logger.LogInformation("Removed role {Role} from {User} in {Guild} ({Reason}).", roleId, userId, guildId, reason);
        return true;
    }

    /// <summary>Guards hierarchy + privilege rules before any change (spec §11).</summary>
    private bool TryValidateRole(ulong guildId, ulong roleId, out SocketRole? role)
    {
        role = null;
        var guild = _client.GetGuild(guildId);
        if (guild is null)
        {
            _logger.LogWarning("Role op skipped: guild {Guild} not available.", guildId);
            return false;
        }

        role = guild.GetRole(roleId);
        if (role is null)
        {
            _logger.LogWarning("Role op skipped: role {Role} not found in guild {Guild}.", roleId, guildId);
            return false;
        }

        if (role.Permissions.Administrator || role.Permissions.ManageRoles || role.Permissions.ManageGuild)
        {
            _logger.LogWarning("Refusing to manage privileged role {Role} in guild {Guild}.", roleId, guildId);
            return false;
        }

        if (role.IsManaged || role.Position >= guild.CurrentUser.Hierarchy)
        {
            _logger.LogWarning("Role {Role} is above the bot's highest role (or managed) in guild {Guild}; cannot modify.", roleId, guildId);
            return false;
        }

        return true;
    }
}
