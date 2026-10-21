using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>EF Core-backed store for interactive gallery sessions (spec §9).</summary>
public sealed class NftGallerySessionRepository : INftGallerySessionRepository
{
    private readonly ForgottenDbContext _dbContext;

    public NftGallerySessionRepository(ForgottenDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(NftGallerySession session, CancellationToken cancellationToken)
    {
        _dbContext.NftGallerySessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<NftGallerySession?> GetAsync(int id, CancellationToken cancellationToken) =>
        _dbContext.NftGallerySessions.FindAsync(new object[] { id }, cancellationToken).AsTask();

    public async Task UpdateAsync(NftGallerySession session, CancellationToken cancellationToken)
    {
        _dbContext.NftGallerySessions.Update(session);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
