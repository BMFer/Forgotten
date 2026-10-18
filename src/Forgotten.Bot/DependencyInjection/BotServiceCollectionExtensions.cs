using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Forgotten.Bot.Configuration;
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
            new InteractionServiceConfig { LogLevel = LogSeverity.Info }));

        services.AddHostedService<DiscordBotService>();

        return services;
    }
}
