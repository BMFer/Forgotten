using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.UnitTests.Configuration;

/// <summary>
/// Security-critical configuration must fail closed (spec §20): invalid settings
/// throw when the options are materialized rather than being silently accepted.
/// </summary>
public class OptionsValidationTests
{
    private static IServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddForgottenApplication(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void ValidConfiguration_Materializes()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Solana:DefaultPageSize"] = "20",
            ["Solana:MaxPageSize"] = "100",
            ["NftEngine:OwnershipPollingMinutes"] = "5",
        });

        var solana = provider.GetRequiredService<IOptions<SolanaOptions>>().Value;
        var engine = provider.GetRequiredService<IOptions<NftEngineOptions>>().Value;

        Assert.Equal(20, solana.DefaultPageSize);
        Assert.Equal(5, engine.OwnershipPollingMinutes);
    }

    [Fact]
    public void DefaultPageSizeExceedingMax_FailsClosed()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Solana:DefaultPageSize"] = "500",
            ["Solana:MaxPageSize"] = "100",
        });

        var ex = Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<SolanaOptions>>().Value);

        Assert.Contains("DefaultPageSize", ex.Message);
    }

    [Fact]
    public void DataAnnotationRangeViolation_FailsClosed()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            // OwnershipPollingMinutes has [Range(1, 1440)]; 0 must be rejected.
            ["NftEngine:OwnershipPollingMinutes"] = "0",
        });

        Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<NftEngineOptions>>().Value);
    }

    [Fact]
    public void MalformedEndpointUrl_FailsClosed()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Solana:DasEndpoint"] = "not-a-url",
        });

        Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<SolanaOptions>>().Value);
    }
}
