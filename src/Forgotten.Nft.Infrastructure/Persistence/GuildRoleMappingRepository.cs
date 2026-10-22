using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>EF Core-backed store for per-guild collection→role mappings (spec §11).</summary>
public sealed class GuildRoleMappingRepository : IGuildRoleMappingRepository
{
    private readonly ForgottenDbContext _dbContext;

    public GuildRoleMappingRepository(ForgottenDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<GuildRoleMapping>> ListByGuildAsync(ulong guildId, CancellationToken cancellationToken) =>
        await _dbContext.GuildRoleMappings
            .Where(m => m.GuildId == guildId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<GuildRoleMapping>> ListAllAsync(CancellationToken cancellationToken) =>
        await _dbContext.GuildRoleMappings.ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<GuildRoleMapping?> GetAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken) =>
        _dbContext.GuildRoleMappings.FirstOrDefaultAsync(
            m => m.GuildId == guildId && m.Blockchain == blockchain && m.CollectionAddress == collectionAddress,
            cancellationToken);

    public async Task UpsertAsync(GuildRoleMapping mapping, CancellationToken cancellationToken)
    {
        if (mapping.Id == 0)
        {
            _dbContext.GuildRoleMappings.Add(mapping);
        }
        else
        {
            _dbContext.GuildRoleMappings.Update(mapping);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.GuildRoleMappings
            .FirstOrDefaultAsync(
                m => m.GuildId == guildId && m.Blockchain == blockchain && m.CollectionAddress == collectionAddress,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return false;
        }

        _dbContext.GuildRoleMappings.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
