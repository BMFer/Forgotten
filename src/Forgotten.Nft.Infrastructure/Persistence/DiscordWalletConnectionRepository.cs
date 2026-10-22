using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>EF Core-backed store for verified wallet connections (spec §9, §7.4).</summary>
public sealed class DiscordWalletConnectionRepository : IDiscordWalletConnectionRepository
{
    private readonly ForgottenDbContext _dbContext;

    public DiscordWalletConnectionRepository(ForgottenDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<DiscordWalletConnection>> ListActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken) =>
        await _dbContext.DiscordWalletConnections
            .Where(c => c.DiscordUserId == discordUserId && c.DisconnectedAt == null)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.VerifiedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<DiscordWalletConnection?> GetActiveByWalletAsync(string blockchain, string walletAddress, CancellationToken cancellationToken) =>
        _dbContext.DiscordWalletConnections
            .FirstOrDefaultAsync(
                c => c.Blockchain == blockchain && c.WalletAddress == walletAddress && c.DisconnectedAt == null,
                cancellationToken);

    public Task<DiscordWalletConnection?> GetAsync(ulong discordUserId, string blockchain, string walletAddress, CancellationToken cancellationToken) =>
        _dbContext.DiscordWalletConnections
            .FirstOrDefaultAsync(
                c => c.DiscordUserId == discordUserId && c.Blockchain == blockchain && c.WalletAddress == walletAddress,
                cancellationToken);

    public Task<int> CountActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken) =>
        _dbContext.DiscordWalletConnections
            .CountAsync(c => c.DiscordUserId == discordUserId && c.DisconnectedAt == null, cancellationToken);

    public async Task<IReadOnlyList<ulong>> ListActiveUserIdsAsync(int max, CancellationToken cancellationToken) =>
        await _dbContext.DiscordWalletConnections
            .Where(c => c.DisconnectedAt == null)
            .Select(c => c.DiscordUserId)
            .Distinct()
            .Take(Math.Clamp(max, 1, 1000))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertAsync(DiscordWalletConnection connection, CancellationToken cancellationToken)
    {
        if (connection.Id == 0)
        {
            _dbContext.DiscordWalletConnections.Add(connection);
        }
        else
        {
            _dbContext.DiscordWalletConnections.Update(connection);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
