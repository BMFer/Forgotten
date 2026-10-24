using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>EF Core-backed guild-scoped collection registry store (spec §9/§13).</summary>
public sealed class NftCollectionRepository : INftCollectionRepository
{
    private readonly ForgottenDbContext _dbContext;

    public NftCollectionRepository(ForgottenDbContext dbContext) => _dbContext = dbContext;

    public Task<NftCollection?> GetAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken) =>
        _dbContext.NftCollections.FirstOrDefaultAsync(
            c => c.GuildId == guildId && c.Blockchain == blockchain && c.CollectionAddress == collectionAddress,
            cancellationToken);

    public async Task<IReadOnlyList<NftCollection>> ListAsync(ulong guildId, string blockchain, CancellationToken cancellationToken) =>
        await _dbContext.NftCollections
            .Where(c => c.GuildId == guildId && c.Blockchain == blockchain)
            .OrderByDescending(c => c.IsApproved)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertAsync(NftCollection collection, CancellationToken cancellationToken)
    {
        if (collection.Id == 0)
        {
            _dbContext.NftCollections.Add(collection);
        }
        else
        {
            _dbContext.NftCollections.Update(collection);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.NftCollections
            .FirstOrDefaultAsync(
                c => c.GuildId == guildId && c.Blockchain == blockchain && c.CollectionAddress == collectionAddress,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return false;
        }

        _dbContext.NftCollections.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task<NftCollection?> GetDefaultAsync(ulong guildId, string blockchain, CancellationToken cancellationToken) =>
        _dbContext.NftCollections.FirstOrDefaultAsync(
            c => c.GuildId == guildId && c.Blockchain == blockchain && c.IsApproved && c.IsDefault,
            cancellationToken);

    public async Task<bool> SetDefaultAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken)
    {
        var guildCollections = await _dbContext.NftCollections
            .Where(c => c.GuildId == guildId && c.Blockchain == blockchain)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var target = guildCollections.FirstOrDefault(c => c.CollectionAddress == collectionAddress);
        if (target is null || !target.IsApproved)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        foreach (var c in guildCollections)
        {
            var shouldBeDefault = ReferenceEquals(c, target);
            if (c.IsDefault != shouldBeDefault)
            {
                c.IsDefault = shouldBeDefault;
                c.UpdatedAt = now;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<string>> ListApprovedCollectionAddressesAsync(string blockchain, CancellationToken cancellationToken) =>
        await _dbContext.NftCollections
            .Where(c => c.Blockchain == blockchain && c.IsApproved)
            .Select(c => c.CollectionAddress)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
