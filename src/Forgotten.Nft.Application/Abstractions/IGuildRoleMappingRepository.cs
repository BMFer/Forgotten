using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>Persistence for per-guild collection→role mappings (spec §11).</summary>
public interface IGuildRoleMappingRepository
{
    Task<IReadOnlyList<GuildRoleMapping>> ListByGuildAsync(ulong guildId, CancellationToken cancellationToken);

    Task<IReadOnlyList<GuildRoleMapping>> ListAllAsync(CancellationToken cancellationToken);

    Task<GuildRoleMapping?> GetAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken);

    Task UpsertAsync(GuildRoleMapping mapping, CancellationToken cancellationToken);

    Task<bool> RemoveAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken);
}
