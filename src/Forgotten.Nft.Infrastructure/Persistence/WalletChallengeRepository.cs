using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>EF Core-backed store for wallet-verification challenges (spec §9).</summary>
public sealed class WalletChallengeRepository : IWalletChallengeRepository
{
    private readonly ForgottenDbContext _dbContext;

    public WalletChallengeRepository(ForgottenDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(WalletVerificationChallenge challenge, CancellationToken cancellationToken)
    {
        _dbContext.WalletVerificationChallenges.Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<WalletVerificationChallenge?> GetBySessionTokenHashAsync(string sessionTokenHash, CancellationToken cancellationToken) =>
        _dbContext.WalletVerificationChallenges
            .FirstOrDefaultAsync(c => c.SessionTokenHash == sessionTokenHash, cancellationToken);

    public async Task UpdateAsync(WalletVerificationChallenge challenge, CancellationToken cancellationToken)
    {
        _dbContext.WalletVerificationChallenges.Update(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
