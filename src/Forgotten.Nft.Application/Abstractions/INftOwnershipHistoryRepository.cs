using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>Append-only ownership observation history (spec §9 "NftOwnershipHistory").</summary>
public interface INftOwnershipHistoryRepository
{
    Task AddAsync(NftOwnershipHistory entry, CancellationToken cancellationToken);
}
