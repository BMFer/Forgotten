using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>Persistence for verified wallet connections (spec §9 "DiscordWalletConnections", §7.4).</summary>
public interface IDiscordWalletConnectionRepository
{
    Task<IReadOnlyList<DiscordWalletConnection>> ListActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken);

    /// <summary>Active link for a wallet regardless of owner — used to enforce the duplicate-claim policy (spec §7.4).</summary>
    Task<DiscordWalletConnection?> GetActiveByWalletAsync(string blockchain, string walletAddress, CancellationToken cancellationToken);

    Task<DiscordWalletConnection?> GetAsync(ulong discordUserId, string blockchain, string walletAddress, CancellationToken cancellationToken);

    Task<int> CountActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken);

    Task UpsertAsync(DiscordWalletConnection connection, CancellationToken cancellationToken);
}
