using Forgotten.Nft.Domain.Entities;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>Manages a member's verified wallet connections (spec §7.4).</summary>
public interface IWalletConnectionService
{
    Task<IReadOnlyList<DiscordWalletConnection>> ListAsync(ulong discordUserId, CancellationToken cancellationToken);

    /// <summary>Disconnects a wallet (marks it inactive). Returns false if no active link exists.</summary>
    Task<bool> DisconnectAsync(ulong discordUserId, string walletAddress, CancellationToken cancellationToken);

    /// <summary>Sets a wallet as the member's primary. Returns false if no active link exists.</summary>
    Task<bool> SetPrimaryAsync(ulong discordUserId, string walletAddress, CancellationToken cancellationToken);
}
