using Discord.WebSocket;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Bot.Workers;

/// <summary>
/// Background ownership monitor + role reconciler (spec §10, §11). Runs on a
/// configurable interval, gated by <see cref="NftEngineOptions.EnableOwnershipMonitoring"/>.
/// Each cycle runs in its own DI scope; failures are logged and the loop continues
/// (failure recovery, spec §10.1). Cancellation stops it cleanly.
/// </summary>
public sealed class OwnershipMonitoringWorker : BackgroundService
{
    private const int MaxAssetsPerSweep = 100;

    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly NftEngineOptions _options;
    private readonly ILogger<OwnershipMonitoringWorker> _logger;

    public OwnershipMonitoringWorker(
        IServiceProvider services,
        DiscordSocketClient client,
        IOptions<NftEngineOptions> options,
        ILogger<OwnershipMonitoringWorker> logger)
    {
        _services = services;
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableOwnershipMonitoring)
        {
            _logger.LogInformation("Ownership monitoring is disabled (NftEngine:EnableOwnershipMonitoring=false).");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.OwnershipPollingMinutes));
        _logger.LogInformation("Ownership monitoring enabled; interval {Interval}.", interval);

        // Let the gateway connect before the first role reconciliation.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_client.ConnectionState == Discord.ConnectionState.Connected)
            {
                await RunCycleAsync(stoppingToken).ConfigureAwait(false);
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var monitor = scope.ServiceProvider.GetRequiredService<IOwnershipMonitor>();
            var reconciler = scope.ServiceProvider.GetRequiredService<IRoleReconciliationService>();

            var ownershipChanges = await monitor.RunSweepAsync(MaxAssetsPerSweep, cancellationToken).ConfigureAwait(false);
            var roleChanges = await reconciler.ReconcileAllAsync(cancellationToken).ConfigureAwait(false);

            if (ownershipChanges > 0 || roleChanges > 0)
            {
                _logger.LogInformation(
                    "Monitoring cycle: {Ownership} ownership change(s), {Roles} role change(s).",
                    ownershipChanges, roleChanges);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ownership monitoring cycle failed; will retry next interval.");
        }
    }
}
