using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>Persistence for wallet-verification challenges/sessions (spec §9).</summary>
public interface IWalletChallengeRepository
{
    Task AddAsync(WalletVerificationChallenge challenge, CancellationToken cancellationToken);

    Task<WalletVerificationChallenge?> GetBySessionTokenHashAsync(string sessionTokenHash, CancellationToken cancellationToken);

    Task UpdateAsync(WalletVerificationChallenge challenge, CancellationToken cancellationToken);
}
