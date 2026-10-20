using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Forgotten.Bot.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Bot;

/// <summary>
/// Hosted service that owns the Discord gateway connection lifecycle and registers
/// slash commands once the client is ready. In Phase 0 there are no command modules
/// yet; the registration pipeline is wired so later phases plug modules in without
/// re-plumbing. Command handlers must stay thin — business logic lives in the
/// Application layer (spec §3).
/// </summary>
public sealed class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly DiscordOptions _options;
    private readonly ILogger<DiscordBotService> _logger;

    public DiscordBotService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        IOptions<DiscordOptions> options,
        ILogger<DiscordBotService> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Log += OnLogAsync;
        _interactions.Log += OnLogAsync;
        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;
        _interactions.InteractionExecuted += OnInteractionExecutedAsync;

        await _client.LoginAsync(TokenType.Bot, _options.Token).ConfigureAwait(false);
        await _client.StartAsync().ConfigureAwait(false);

        // Keep the service alive until the host is shutting down.
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task OnReadyAsync()
    {
        // Discover interaction modules from this assembly (none in Phase 0).
        await _interactions.AddModulesAsync(typeof(DiscordBotService).Assembly, _services)
            .ConfigureAwait(false);

        if (_options.DevGuildId is ulong guildId)
        {
            await _interactions.RegisterCommandsToGuildAsync(guildId).ConfigureAwait(false);
            _logger.LogInformation(
                "FORGOTTEN ready as {User}. Commands registered to dev guild {GuildId}.",
                _client.CurrentUser?.Username, guildId);
        }
        else
        {
            await _interactions.RegisterCommandsGloballyAsync().ConfigureAwait(false);
            _logger.LogInformation(
                "FORGOTTEN ready as {User}. Commands registered globally.",
                _client.CurrentUser?.Username);
        }
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        // A DI scope per interaction so scoped services (DbContext, repositories,
        // inspection service) are isolated and disposed per command (default RunMode
        // is Sync, so the scope stays alive for the whole execution).
        _logger.LogInformation(
            "Interaction received: type={Type} id={Id} user={User}",
            interaction.Type, interaction.Id, interaction.User?.Id);

        using var scope = _services.CreateScope();
        try
        {
            var context = new SocketInteractionContext(_client, interaction);
            var result = await _interactions.ExecuteCommandAsync(context, scope.ServiceProvider).ConfigureAwait(false);

            // In RunMode.Sync, command failures are reported here rather than thrown.
            if (!result.IsSuccess)
            {
                _logger.LogError(
                    "Interaction {Id} failed: {Error} — {Reason}",
                    interaction.Id, result.Error, result.ErrorReason);

                if (!interaction.HasResponded)
                {
                    await interaction.RespondAsync(
                        $"⚠️ Command error ({result.Error}): {result.ErrorReason}",
                        ephemeral: true).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error executing interaction {Id}.", interaction.Id);
            if (!interaction.HasResponded)
            {
                try
                {
                    await interaction.RespondAsync("⚠️ Something went wrong handling that command.", ephemeral: true)
                        .ConfigureAwait(false);
                }
                catch (Exception responseEx)
                {
                    _logger.LogError(responseEx, "Failed to send error response for interaction {Id}.", interaction.Id);
                }
            }
        }
    }

    // With RunMode.Async the command runs on a separate task; failures (module
    // creation, parameter parsing, preconditions, or exceptions in the body) are
    // reported here rather than through ExecuteCommandAsync's result. Without this
    // handler those failures are swallowed and the interaction is never acknowledged.
    private async Task OnInteractionExecutedAsync(ICommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess)
        {
            return;
        }

        _logger.LogError(
            "Command '{Command}' failed: {Error} — {Reason}",
            commandInfo?.Name, result.Error, result.ErrorReason);

        try
        {
            var message = $"⚠️ Command error ({result.Error}): {result.ErrorReason}";
            if (context.Interaction.HasResponded)
            {
                await context.Interaction.FollowupAsync(message, ephemeral: true).ConfigureAwait(false);
            }
            else
            {
                await context.Interaction.RespondAsync(message, ephemeral: true).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report interaction error to the user.");
        }
    }

    private Task OnLogAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information,
        };

        _logger.Log(level, message.Exception, "[Discord/{Source}] {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Ready -= OnReadyAsync;
        _client.InteractionCreated -= OnInteractionCreatedAsync;
        _interactions.InteractionExecuted -= OnInteractionExecutedAsync;
        _client.Log -= OnLogAsync;
        _interactions.Log -= OnLogAsync;

        await _client.LogoutAsync().ConfigureAwait(false);
        await _client.StopAsync().ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
