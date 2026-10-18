using Forgotten.Nft.Infrastructure.Configuration;
using Forgotten.Nft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Infrastructure.DependencyInjection;

/// <summary>
/// Registers the Infrastructure layer: the EF Core/SQLite context and the named
/// HTTP client used by the Solana provider (with the standard resilience handler:
/// timeout, retry with jitter, circuit breaker — spec §5.3). The provider
/// implementation itself is added in Phase 1.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Named <see cref="System.Net.Http.HttpClient"/> for Solana DAS/RPC calls.</summary>
    public const string SolanaHttpClientName = "solana";

    public static IServiceCollection AddForgottenInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            return new ForgottenConnectionInterceptor(options.BusyTimeoutMs);
        });

        services.AddDbContext<ForgottenDbContext>((sp, options) =>
        {
            var database = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlite(database.ConnectionString);
            options.AddInterceptors(sp.GetRequiredService<ForgottenConnectionInterceptor>());
        });

        services.AddHttpClient(SolanaHttpClientName)
            .AddStandardResilienceHandler();

        // Phase 1+ register ISolanaAssetProvider, metadata resolver, repositories here.
        return services;
    }
}
