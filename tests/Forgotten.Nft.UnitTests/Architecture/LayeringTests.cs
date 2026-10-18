using Forgotten.Nft.Domain;

namespace Forgotten.Nft.UnitTests.Architecture;

/// <summary>
/// Enforces the layering rule from spec §3: the Domain layer must not depend on
/// Discord.Net (or any presentation/infrastructure concern). This guards the rule
/// as the Domain assembly fills up in later phases.
/// </summary>
public class LayeringTests
{
    [Fact]
    public void Domain_DoesNotReferenceDiscordNet()
    {
        var referenced = typeof(DomainAssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(referenced, name =>
            name.Contains("Discord", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Domain_DoesNotReferenceEntityFrameworkCore()
    {
        var referenced = typeof(DomainAssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(referenced, name =>
            name.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
    }
}
