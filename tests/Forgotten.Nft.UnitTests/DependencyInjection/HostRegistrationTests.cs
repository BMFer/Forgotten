using Forgotten.Nft.Application.DependencyInjection;
using Forgotten.Nft.Infrastructure.DependencyInjection;
using Forgotten.Nft.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forgotten.Nft.UnitTests.DependencyInjection;

/// <summary>
/// The composition root must produce a valid DI graph (Phase 0 Exit Gate).
/// Building with ValidateOnBuild + ValidateScopes catches missing dependencies
/// and captive-dependency (scoped-into-singleton) mistakes at build time.
/// </summary>
public class HostRegistrationTests
{
    private static IConfiguration MinimalValidConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Data Source=:memory:",
                ["Solana:DefaultPageSize"] = "20",
                ["Solana:MaxPageSize"] = "100",
            })
            .Build();

    [Fact]
    public void ApplicationAndInfrastructure_BuildAndValidate()
    {
        var configuration = MinimalValidConfiguration();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddForgottenApplication(configuration);
        services.AddForgottenInfrastructure(configuration);

        // Throws if any registration cannot be constructed or captures a scope.
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        Assert.NotNull(provider);
    }

    [Fact]
    public void DbContext_IsResolvableWithinAScope()
    {
        var configuration = MinimalValidConfiguration();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddForgottenApplication(configuration);
        services.AddForgottenInfrastructure(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ForgottenDbContext>();
        Assert.NotNull(context);
    }
}
