using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>Append-only security audit log for verification checks (spec §9 "NftVerificationLogs").</summary>
public interface INftVerificationLogRepository
{
    Task AddAsync(NftVerificationLog log, CancellationToken cancellationToken);

    Task<IReadOnlyList<NftVerificationLog>> GetRecentAsync(ulong guildId, int count, CancellationToken cancellationToken);
}
