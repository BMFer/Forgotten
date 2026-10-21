using System.Net;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Infrastructure.Blockchain;
using Forgotten.Nft.Infrastructure.Configuration;
using Forgotten.Nft.Infrastructure.Metadata;
using Forgotten.Nft.Infrastructure.Persistence;
using Forgotten.Nft.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Infrastructure.DependencyInjection;

/// <summary>
/// Registers the Infrastructure layer: EF Core/SQLite, the DAS asset provider and
/// its resilient HTTP client (timeout/retry/circuit breaker — spec §5.3), and the
/// SSRF-guarded metadata resolver (spec §8.2).
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public const string SolanaHttpClientName = "solana";
    public const string MetadataHttpClientName = "metadata";

    public static IServiceCollection AddForgottenInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<MetadataOptions>()
            .Bind(configuration.GetSection(MetadataOptions.SectionName))
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

        AddSolanaHttpClient(services, configuration);
        AddMetadataHttpClient(services);

        services.AddSingleton<ISolanaAssetProvider, DasSolanaAssetProvider>();
        services.AddSingleton<IMetadataResolver, MetadataResolver>();
        services.AddSingleton<ISignatureVerifier, Ed25519SignatureVerifier>();
        services.AddScoped<INftAssetRepository, NftAssetRepository>();
        services.AddScoped<INftCollectionRepository, NftCollectionRepository>();
        services.AddScoped<INftVerificationLogRepository, NftVerificationLogRepository>();
        services.AddScoped<IWalletChallengeRepository, WalletChallengeRepository>();
        services.AddScoped<IDiscordWalletConnectionRepository, DiscordWalletConnectionRepository>();

        return services;
    }

    private static void AddSolanaHttpClient(IServiceCollection services, IConfiguration configuration)
    {
        var solana = configuration.GetSection(SolanaOptions.SectionName).Get<SolanaOptions>() ?? new SolanaOptions();
        var attempt = TimeSpan.FromSeconds(Math.Max(1, solana.RequestTimeoutSeconds));
        var retries = Math.Clamp(solana.MaxRetries, 0, 10);
        var total = TimeSpan.FromSeconds(attempt.TotalSeconds * (retries + 1));

        services.AddHttpClient(SolanaHttpClientName)
            .AddStandardResilienceHandler(o =>
            {
                o.AttemptTimeout.Timeout = attempt;
                o.TotalRequestTimeout.Timeout = total;
                o.Retry.MaxRetryAttempts = retries;
                // Circuit-breaker sampling must be >= 2x the attempt timeout.
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(attempt.TotalSeconds * 2);
            });
    }

    private static void AddMetadataHttpClient(IServiceCollection services)
    {
        // Redirects are followed manually by the resolver so each hop is re-validated;
        // the SSRF guard runs in the connect callback to vet the real dialed IP.
        services.AddHttpClient(MetadataHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectCallback = SsrfGuard.ConnectAsync,
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            });
    }
}
