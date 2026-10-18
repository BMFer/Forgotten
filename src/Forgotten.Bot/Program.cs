using System.Reflection;
using Forgotten.Bot;
using Forgotten.Bot.DependencyInjection;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DependencyInjection;
using Forgotten.Nft.Infrastructure.Configuration;
using Forgotten.Nft.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Secrets (Discord token, API keys) come from user secrets / environment — never
// from committed files (spec §5.2, §16.11). Env vars use the "Section__Key" form,
// e.g. Discord__Token.
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

builder.Services
    .AddForgottenApplication(builder.Configuration)
    .AddForgottenInfrastructure(builder.Configuration)
    .AddForgottenBot(builder.Configuration);

var host = builder.Build();

// Startup banner (spec §20 / Phase 0): report environment, DB target, and the
// security-relevant feature flags that are now in effect.
LogStartupBanner(host.Services, builder.Environment.EnvironmentName);

host.Run();

static void LogStartupBanner(IServiceProvider services, string environment)
{
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Forgotten.Startup");
    var engine = services.GetRequiredService<IOptions<NftEngineOptions>>().Value;
    var database = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    var solana = services.GetRequiredService<IOptions<SolanaOptions>>().Value;

    logger.LogInformation(
        "FORGOTTEN NFT Engine starting | Env={Environment} | Engine.Enabled={Enabled} | " +
        "Database={ConnectionString} | Solana.Network={Network} | Solana.DasConfigured={DasConfigured} | " +
        "OwnershipMonitoring={Monitoring} | PowIntegration={Pow}",
        environment,
        engine.Enabled,
        database.ConnectionString,
        solana.Network,
        !string.IsNullOrWhiteSpace(solana.DasEndpoint),
        engine.EnableOwnershipMonitoring,
        engine.EnablePowIntegration);
}
