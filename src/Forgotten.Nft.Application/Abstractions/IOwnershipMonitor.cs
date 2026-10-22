namespace Forgotten.Nft.Application.Abstractions;

/// <summary>
/// Re-checks ownership of tracked NFTs in approved collections, recording observed
/// changes and publishing events (spec §10). One sweep processes a bounded batch.
/// </summary>
public interface IOwnershipMonitor
{
    /// <summary>Runs one bounded ownership sweep; returns the number of observed changes recorded.</summary>
    Task<int> RunSweepAsync(int maxAssets, CancellationToken cancellationToken);
}

/// <summary>
/// Reconciles Discord roles against current NFT ownership for approved,
/// role-mapped collections (spec §11).
/// </summary>
public interface IRoleReconciliationService
{
    /// <summary>Reconciles every role-mapped guild; returns the number of role changes applied.</summary>
    Task<int> ReconcileAllAsync(CancellationToken cancellationToken);

    /// <summary>Reconciles a single member in a guild (e.g. right after they verify a wallet).</summary>
    Task ReconcileMemberAsync(ulong guildId, ulong discordUserId, CancellationToken cancellationToken);
}
