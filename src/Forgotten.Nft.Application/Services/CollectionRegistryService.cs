using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.Events;
using Forgotten.Nft.Domain.ValueObjects;

namespace Forgotten.Nft.Application.Services;

/// <summary>
/// Per-guild collection approval registry (spec §13). Approval records the
/// approving administrator and timestamp and is independent of on-chain
/// verification. Addresses are validated/normalized before storage (spec §16.6).
/// </summary>
public sealed class CollectionRegistryService : ICollectionRegistryService
{
    private const string Blockchain = "solana";

    private readonly INftCollectionRepository _repository;
    private readonly IDomainEventPublisher _events;

    public CollectionRegistryService(INftCollectionRepository repository, IDomainEventPublisher events)
    {
        _repository = repository;
        _events = events;
    }

    public async Task<NftCollection> ApproveAsync(
        ulong guildId, string collectionAddress, string? displayName, ulong approvedByUserId, CancellationToken cancellationToken)
    {
        var address = Normalize(collectionAddress);
        var now = DateTime.UtcNow;

        var existing = await _repository.GetAsync(guildId, Blockchain, address, cancellationToken).ConfigureAwait(false);
        var entity = existing ?? new NftCollection
        {
            GuildId = guildId,
            Blockchain = Blockchain,
            CollectionAddress = address,
            CreatedAt = now,
        };

        // The first approved collection for a guild becomes its default automatically, so
        // members can flex "our collection" in /nft showcase with no extra admin step (spec §13).
        if (existing is null)
        {
            var currentDefault = await _repository.GetDefaultAsync(guildId, Blockchain, cancellationToken).ConfigureAwait(false);
            entity.IsDefault = currentDefault is null;
        }

        entity.IsApproved = true;
        entity.ApprovedByDiscordUserId = approvedByUserId;
        entity.ApprovedAt = now;
        entity.UpdatedAt = now;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            entity.DisplayName = displayName.Trim();
        }

        await _repository.UpsertAsync(entity, cancellationToken).ConfigureAwait(false);

        await _events.PublishAsync(
            new NftCollectionApprovedEvent(guildId, address, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
        return entity;
    }

    public Task<bool> RemoveAsync(ulong guildId, string collectionAddress, CancellationToken cancellationToken) =>
        _repository.RemoveAsync(guildId, Blockchain, Normalize(collectionAddress), cancellationToken);

    public Task<IReadOnlyList<NftCollection>> ListAsync(ulong guildId, CancellationToken cancellationToken) =>
        _repository.ListAsync(guildId, Blockchain, cancellationToken);

    public async Task<IReadOnlyList<NftCollection>> ListApprovedAsync(ulong guildId, CancellationToken cancellationToken)
    {
        var all = await _repository.ListAsync(guildId, Blockchain, cancellationToken).ConfigureAwait(false);
        return all.Where(c => c.IsApproved).ToList();
    }

    public async Task<NftCollection?> GetDefaultAsync(ulong guildId, CancellationToken cancellationToken)
    {
        var flagged = await _repository.GetDefaultAsync(guildId, Blockchain, cancellationToken).ConfigureAwait(false);
        if (flagged is not null)
        {
            return flagged;
        }

        // Fallback: a guild with exactly one approved collection flexes that one by default,
        // even if it predates the IsDefault flag.
        var approved = await ListApprovedAsync(guildId, cancellationToken).ConfigureAwait(false);
        return approved.Count == 1 ? approved[0] : null;
    }

    public Task<bool> SetDefaultAsync(ulong guildId, string collectionAddress, CancellationToken cancellationToken) =>
        _repository.SetDefaultAsync(guildId, Blockchain, Normalize(collectionAddress), cancellationToken);

    public Task<NftCollection?> GetAsync(ulong guildId, string collectionAddress, CancellationToken cancellationToken) =>
        _repository.GetAsync(guildId, Blockchain, Normalize(collectionAddress), cancellationToken);

    public async Task<bool> IsApprovedAsync(ulong guildId, string collectionAddress, CancellationToken cancellationToken)
    {
        var entity = await GetAsync(guildId, collectionAddress, cancellationToken).ConfigureAwait(false);
        return entity is { IsApproved: true };
    }

    private static string Normalize(string collectionAddress) =>
        SolanaAddress.TryParse(collectionAddress, out var address)
            ? address.Value
            : throw new ArgumentException($"'{collectionAddress}' is not a valid Solana collection address.", nameof(collectionAddress));
}
