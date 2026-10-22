using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Forgotten.Nft.Application.Services;

/// <summary>
/// Re-checks ownership of tracked NFTs in approved collections (spec §10). When the
/// observed owner differs from the stored snapshot it updates the snapshot, records
/// an <em>observed</em> history row (no fabricated transaction — spec §10.2),
/// publishes <see cref="NftOwnershipChangedEvent"/>, and invalidates the asset's
/// display cache (spec §15). Provider failures are skipped (not treated as a change)
/// and a "not found" result is never interpreted as a transfer.
/// </summary>
public sealed class OwnershipMonitor : IOwnershipMonitor
{
    private const string Blockchain = "solana";

    private readonly ISolanaAssetProvider _provider;
    private readonly INftAssetRepository _assets;
    private readonly INftCollectionRepository _collections;
    private readonly INftOwnershipHistoryRepository _history;
    private readonly IDomainEventPublisher _events;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OwnershipMonitor> _logger;

    public OwnershipMonitor(
        ISolanaAssetProvider provider,
        INftAssetRepository assets,
        INftCollectionRepository collections,
        INftOwnershipHistoryRepository history,
        IDomainEventPublisher events,
        IMemoryCache cache,
        ILogger<OwnershipMonitor> logger)
    {
        _provider = provider;
        _assets = assets;
        _collections = collections;
        _history = history;
        _events = events;
        _cache = cache;
        _logger = logger;
    }

    public async Task<int> RunSweepAsync(int maxAssets, CancellationToken cancellationToken)
    {
        var approved = await _collections.ListApprovedCollectionAddressesAsync(Blockchain, cancellationToken).ConfigureAwait(false);
        if (approved.Count == 0)
        {
            return 0;
        }

        var tracked = await _assets.ListByCollectionsAsync(Blockchain, approved, maxAssets, cancellationToken).ConfigureAwait(false);
        var changes = 0;

        foreach (var asset in tracked)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SolanaAssetSnapshot fresh;
            try
            {
                var result = await _provider.GetAssetAsync(asset.AssetAddress, cancellationToken).ConfigureAwait(false);
                if (result is null)
                {
                    continue; // authoritative "not found" — do not infer a transfer
                }

                fresh = new SolanaAssetSnapshot(result.OwnerAddress);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ownership check failed for {Asset}; skipping this cycle.", asset.AssetAddress);
                continue; // provider failure is never treated as an ownership change
            }

            if (string.Equals(asset.OwnerAddress, fresh.Owner, StringComparison.Ordinal))
            {
                continue; // no change — no event (duplicate-event prevention)
            }

            var observedAt = DateTime.UtcNow;
            await _assets.UpdateOwnerAsync(asset.Id, fresh.Owner, observedAt, cancellationToken).ConfigureAwait(false);
            await _history.AddAsync(
                new NftOwnershipHistory
                {
                    NftAssetId = asset.Id,
                    PreviousOwnerAddress = asset.OwnerAddress,
                    NewOwnerAddress = fresh.Owner,
                    ObservedAt = observedAt,
                    Source = NftOwnershipHistory.ObservedSource,
                    TransactionSignature = null,
                },
                cancellationToken).ConfigureAwait(false);

            _cache.Remove($"nft:inspect:{asset.AssetAddress}");

            await _events.PublishAsync(
                new NftOwnershipChangedEvent(asset.AssetAddress, asset.OwnerAddress, fresh.Owner, observedAt),
                cancellationToken).ConfigureAwait(false);

            changes++;
        }

        if (changes > 0)
        {
            _logger.LogInformation("Ownership sweep recorded {Changes} change(s) across {Count} tracked asset(s).", changes, tracked.Count);
        }

        return changes;
    }

    private readonly record struct SolanaAssetSnapshot(string? Owner);
}
