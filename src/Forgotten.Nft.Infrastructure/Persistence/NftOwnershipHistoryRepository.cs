using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>EF Core-backed append-only ownership history (spec §9).</summary>
public sealed class NftOwnershipHistoryRepository : INftOwnershipHistoryRepository
{
    private readonly ForgottenDbContext _dbContext;

    public NftOwnershipHistoryRepository(ForgottenDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(NftOwnershipHistory entry, CancellationToken cancellationToken)
    {
        _dbContext.NftOwnershipHistory.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
