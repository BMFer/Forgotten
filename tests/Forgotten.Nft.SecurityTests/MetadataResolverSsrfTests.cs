using System.Net;
using Forgotten.Nft.Infrastructure.Configuration;
using Forgotten.Nft.Infrastructure.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.SecurityTests;

/// <summary>
/// Exercises the real resolver + SSRF-guarded connect callback (no external
/// network): requests to restricted destinations are blocked at connect time and
/// surface as a null result (spec §8.2, §16.8).
/// </summary>
public class MetadataResolverSsrfTests
{
    private static MetadataResolver CreateResolver()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("metadata")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectCallback = SsrfGuard.ConnectAsync,
            });

        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        var options = Options.Create(new MetadataOptions { TimeoutSeconds = 5, MaxRedirects = 3 });
        return new MetadataResolver(factory, options, NullLogger<MetadataResolver>.Instance);
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")] // cloud metadata
    [InlineData("http://127.0.0.1/metadata.json")]            // loopback
    [InlineData("http://10.0.0.1/metadata.json")]             // private
    public async Task Resolve_BlockedForRestrictedHosts(string uri)
    {
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(uri, CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/x")]
    [InlineData("data:application/json,{}")]
    public async Task Resolve_RejectsNonHttpSchemes(string uri)
    {
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(uri, CancellationToken.None);

        Assert.Null(result);
    }
}
