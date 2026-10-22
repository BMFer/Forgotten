using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Forgotten.Bot.Configuration;
using Forgotten.Bot.Embeds;
using Forgotten.Bot.Services;
using Forgotten.Bot.Workers;
using Forgotten.Nft.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forgotten.Bot.DependencyInjection;

/// <summary>
/// Registers the Discord presentation layer: the socket client, the interaction
/// framework, and the hosted service that manages the gateway connection.
/// </summary>
public static class BotServiceCollectionExtensions
{
    public static IServiceCollection AddForgottenBot(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DiscordOptions>()
            .Bind(configuration.GetSection(DiscordOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PresentationOptions>()
            .Bind(configuration.GetSection(PresentationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<CommandRateLimiter>();
        services.AddSingleton<NftEmbedFactory>();
        services.AddSingleton<IDiscordRoleService, DiscordRoleService>();

        services.AddSingleton(new DiscordSocketConfig
        {
            // Slash commands only need the Guilds intent in Phase 0.
            GatewayIntents = GatewayIntents.Guilds,
            LogLevel = LogSeverity.Info,
            AlwaysDownloadUsers = false,
        });

        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton(sp => new InteractionService(
            sp.GetRequiredService<DiscordSocketClient>(),
            new InteractionServiceConfig
            {
                LogLevel = LogSeverity.Info,
                // Sync so ExecuteCommandAsync awaits the whole command while the
                // per-interaction DI scope is still alive (handlers defer early, so
                // Discord is acknowledged within the 3s window regardless).
                DefaultRunMode = RunMode.Sync,
            }));

        services.AddHostedService<DiscordBotService>();
        services.AddHostedService<OwnershipMonitoringWorker>();

        return services;
    }
}
