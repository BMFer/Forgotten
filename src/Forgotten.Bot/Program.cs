using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forgotten.Bot;
using Forgotten.Bot.DependencyInjection;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DependencyInjection;
using Forgotten.Nft.Infrastructure.Configuration;
using Forgotten.Nft.Infrastructure.DependencyInjection;
using Forgotten.Nft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

// Diagnostic subcommand: `dotnet run -- inspect <assetAddress>` runs the real
// inspection pipeline against the configured provider and prints the result,
// without connecting to Discord. Useful for ops/verification.
if (args.Length >= 2 && string.Equals(args[0], "inspect", StringComparison.OrdinalIgnoreCase))
{
    await RunInspectAsync(builder.Configuration, args[1]);
    return;
}

builder.Services
    .AddForgottenApplication(builder.Configuration)
    .AddForgottenInfrastructure(builder.Configuration)
    .AddForgottenBot(builder.Configuration);

var host = builder.Build();

// Apply any pending EF Core migrations to the runtime database (spec §9). This is
// additive only — Migrate() never drops or recreates data — so the schema always
// matches the code without a manual `dotnet ef database update` step.
await MigrateDatabaseAsync(host.Services);

// Startup banner (spec §20 / Phase 0): report environment, DB target, and the
// security-relevant feature flags that are now in effect.
LogStartupBanner(host.Services, builder.Environment.EnvironmentName);

host.Run();

static async Task MigrateDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Forgotten.Startup");
    var dbContext = scope.ServiceProvider.GetRequiredService<ForgottenDbContext>();

    var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count == 0)
    {
        logger.LogInformation("Database schema is up to date; no migrations to apply.");
        return;
    }

    logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
    await dbContext.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied.");
}

static async Task RunInspectAsync(IConfiguration configuration, string address)
{
    var services = new ServiceCollection();
    services.AddLogging(b => b
        .AddSimpleConsole(o => o.SingleLine = true)
        .SetMinimumLevel(LogLevel.Information)
        .AddFilter("Forgotten.Nft.Infrastructure.Blockchain", LogLevel.Debug));
    services.AddForgottenApplication(configuration);
    services.AddForgottenInfrastructure(configuration);

    await using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<ForgottenDbContext>();
    await db.Database.MigrateAsync();

    var inspector = scope.ServiceProvider.GetRequiredService<INftInspectionService>();
    var result = await inspector.InspectAsync(address, refresh: true);

    var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    });
    Console.WriteLine(json);
}

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
