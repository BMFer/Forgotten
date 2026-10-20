using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>Persistence for the guild-scoped collection registry (spec §9 "NftCollections").</summary>
public interface INftCollectionRepository
{
    Task<NftCollection?> GetAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken);

    Task<IReadOnlyList<NftCollection>> ListAsync(ulong guildId, string blockchain, CancellationToken cancellationToken);

    Task UpsertAsync(NftCollection collection, CancellationToken cancellationToken);

    Task<bool> RemoveAsync(ulong guildId, string blockchain, string collectionAddress, CancellationToken cancellationToken);
}
