using System.Net;
using Forgotten.Nft.Infrastructure.Metadata;

namespace Forgotten.Nft.SecurityTests;

public class SsrfGuardTests
{
    [Theory]
    [InlineData("127.0.0.1")]          // loopback
    [InlineData("10.0.0.5")]           // private
    [InlineData("172.16.1.1")]         // private
    [InlineData("192.168.1.1")]        // private
    [InlineData("169.254.169.254")]    // cloud metadata (link-local)
    [InlineData("100.64.0.1")]         // CGNAT
    [InlineData("0.0.0.0")]            // unspecified
    [InlineData("224.0.0.1")]          // multicast
    [InlineData("::1")]                // IPv6 loopback
    [InlineData("fe80::1")]            // IPv6 link-local
    [InlineData("fc00::1")]            // IPv6 unique-local
    [InlineData("::ffff:127.0.0.1")]   // IPv4-mapped loopback
    [InlineData("::ffff:169.254.169.254")] // IPv4-mapped metadata
    public void IsBlocked_RejectsRestrictedAddresses(string ip)
    {
        Assert.True(SsrfGuard.IsBlocked(IPAddress.Parse(ip)));
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("104.18.0.1")]
    [InlineData("2606:4700:4700::1111")] // Cloudflare DNS IPv6 (public)
    public void IsBlocked_AllowsPublicAddresses(string ip)
    {
        Assert.False(SsrfGuard.IsBlocked(IPAddress.Parse(ip)));
    }
}
