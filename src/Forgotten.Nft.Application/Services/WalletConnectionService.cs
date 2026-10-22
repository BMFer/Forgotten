using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.Events;
using Forgotten.Nft.Domain.ValueObjects;

namespace Forgotten.Nft.Application.Services;

/// <summary>Manages a member's verified wallet connections (spec §7.4).</summary>
public sealed class WalletConnectionService : IWalletConnectionService
{
    private const string Blockchain = "solana";

    private readonly IDiscordWalletConnectionRepository _connections;
    private readonly IDomainEventPublisher _events;

    public WalletConnectionService(IDiscordWalletConnectionRepository connections, IDomainEventPublisher events)
    {
        _connections = connections;
        _events = events;
    }

    public Task<IReadOnlyList<DiscordWalletConnection>> ListAsync(ulong discordUserId, CancellationToken cancellationToken) =>
        _connections.ListActiveByUserAsync(discordUserId, cancellationToken);

    public async Task<bool> DisconnectAsync(ulong discordUserId, string walletAddress, CancellationToken cancellationToken)
    {
        if (!SolanaAddress.TryParse(walletAddress, out var address))
        {
            return false;
        }

        var connection = await _connections
            .GetAsync(discordUserId, Blockchain, address.Value, cancellationToken)
            .ConfigureAwait(false);

        if (connection is null || !connection.IsActive)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        connection.DisconnectedAt = now;
        connection.IsPrimary = false;
        connection.UpdatedAt = now;
        await _connections.UpsertAsync(connection, cancellationToken).ConfigureAwait(false);

        await _events.PublishAsync(
            new WalletDisconnectedEvent(discordUserId, address.Value, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetPrimaryAsync(ulong discordUserId, string walletAddress, CancellationToken cancellationToken)
    {
        if (!SolanaAddress.TryParse(walletAddress, out var address))
        {
            return false;
        }

        var active = await _connections.ListActiveByUserAsync(discordUserId, cancellationToken).ConfigureAwait(false);
        var target = active.FirstOrDefault(c => string.Equals(c.WalletAddress, address.Value, StringComparison.Ordinal));
        if (target is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        foreach (var connection in active)
        {
            var shouldBePrimary = ReferenceEquals(connection, target);
            if (connection.IsPrimary != shouldBePrimary)
            {
                connection.IsPrimary = shouldBePrimary;
                connection.UpdatedAt = now;
                await _connections.UpsertAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }

        return true;
    }
}
