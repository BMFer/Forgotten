using System.Net;
using System.Net.Sockets;

namespace Forgotten.Nft.Infrastructure.Metadata;

/// <summary>
/// Server-Side Request Forgery protection for outbound metadata fetches (spec §8.2,
/// §16.8). Rejects loopback, private, link-local, unique-local, multicast, and
/// cloud-metadata destinations. Validation runs in the socket connect callback, so
/// it checks the <em>actual</em> IP being dialed on every connection — including
/// after redirects — which also defeats DNS-rebinding.
/// </summary>
public static class SsrfGuard
{
    /// <summary>Returns true if traffic to <paramref name="ip"/> must be blocked.</summary>
    public static bool IsBlocked(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return IsBlockedV4(ip.GetAddressBytes());
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return IsBlockedV6(ip);
        }

        // Unknown address family: fail closed.
        return true;
    }

    private static bool IsBlockedV4(byte[] b)
    {
        // 0.0.0.0/8 (this host / unspecified)
        if (b[0] == 0) return true;
        // 10.0.0.0/8 (private)
        if (b[0] == 10) return true;
        // 100.64.0.0/10 (CGNAT)
        if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return true;
        // 127.0.0.0/8 (loopback) — also covered by IsLoopback
        if (b[0] == 127) return true;
        // 169.254.0.0/16 (link-local, incl. 169.254.169.254 cloud metadata)
        if (b[0] == 169 && b[1] == 254) return true;
        // 172.16.0.0/12 (private)
        if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
        // 192.0.0.0/24 (IETF protocol assignments) and 192.168.0.0/16 (private)
        if (b[0] == 192 && b[1] == 0 && b[2] == 0) return true;
        if (b[0] == 192 && b[1] == 168) return true;
        // 198.18.0.0/15 (benchmarking)
        if (b[0] == 198 && (b[1] == 18 || b[1] == 19)) return true;
        // 224.0.0.0/4 (multicast) and 240.0.0.0/4 (reserved) + 255.255.255.255
        if (b[0] >= 224) return true;

        return false;
    }

    private static bool IsBlockedV6(IPAddress ip)
    {
        if (ip.IsIPv6LinkLocal || ip.IsIPv6Multicast || ip.IsIPv6SiteLocal)
        {
            return true;
        }

        var b = ip.GetAddressBytes();

        // :: unspecified
        if (Array.TrueForAll(b, x => x == 0)) return true;
        // fc00::/7 unique local address
        if ((b[0] & 0xFE) == 0xFC) return true;

        return false;
    }

    /// <summary>
    /// SocketsHttpHandler connect callback that resolves the target host and only
    /// connects to a non-blocked IP; throws (blocking the request) if none qualify.
    /// </summary>
    public static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var endPoint = context.DnsEndPoint;

        // Resolves literal IPs to themselves, so hard-coded-IP SSRF is also validated.
        var addresses = await Dns.GetHostAddressesAsync(endPoint.Host, cancellationToken).ConfigureAwait(false);
        var allowed = Array.FindAll(addresses, a => !IsBlocked(a));

        if (allowed.Length == 0)
        {
            throw new HttpRequestException($"Blocked by SSRF guard: '{endPoint.Host}' resolves only to restricted addresses.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(allowed, endPoint.Port, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
