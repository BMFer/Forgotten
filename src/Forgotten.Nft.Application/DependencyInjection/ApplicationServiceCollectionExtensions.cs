using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Application.DependencyInjection;

/// <summary>
/// Registers the Application layer: bound + validated options and (in later
/// phases) application services. Kept free of any Discord or infrastructure
/// dependency so it can be composed and unit-tested in isolation.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddForgottenApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<NftEngineOptions>()
            .Bind(configuration.GetSection(NftEngineOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SolanaOptions>()
            .Bind(configuration.GetSection(SolanaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<NftEngineOptions>, NftEngineOptionsValidator>();
        services.AddSingleton<IValidateOptions<SolanaOptions>, SolanaOptionsValidator>();

        services.AddMemoryCache();
        services.AddScoped<INftInspectionService, NftInspectionService>();
        services.AddScoped<INftVerificationService, NftVerificationService>();
        services.AddScoped<ICollectionRegistryService, CollectionRegistryService>();

        return services;
    }
}
