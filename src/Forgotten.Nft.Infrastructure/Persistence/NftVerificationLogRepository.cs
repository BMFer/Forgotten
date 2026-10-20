using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>EF Core-backed append-only verification audit log (spec §9).</summary>
public sealed class NftVerificationLogRepository : INftVerificationLogRepository
{
    private readonly ForgottenDbContext _dbContext;

    public NftVerificationLogRepository(ForgottenDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(NftVerificationLog log, CancellationToken cancellationToken)
    {
        _dbContext.NftVerificationLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<NftVerificationLog>> GetRecentAsync(ulong guildId, int count, CancellationToken cancellationToken) =>
        await _dbContext.NftVerificationLogs
            .Where(l => l.GuildId == guildId)
            .OrderByDescending(l => l.CheckedAt)
            .Take(Math.Clamp(count, 1, 100))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
