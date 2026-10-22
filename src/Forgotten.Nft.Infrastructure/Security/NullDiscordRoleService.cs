using Forgotten.Nft.Application.Abstractions;

namespace Forgotten.Nft.Infrastructure.Security;

/// <summary>
/// Default no-op <see cref="IDiscordRoleService"/> so the Application + Infrastructure
/// graph is self-contained for hosts without a Discord gateway (e.g. the wallet web
/// app, tests). The Bot composition root overrides this with a real implementation.
/// </summary>
public sealed class NullDiscordRoleService : IDiscordRoleService
{
    public Task<bool> HasRoleAsync(ulong guildId, ulong userId, ulong roleId, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> AssignRoleAsync(ulong guildId, ulong userId, ulong roleId, string reason, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> RemoveRoleAsync(ulong guildId, ulong userId, ulong roleId, string reason, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
