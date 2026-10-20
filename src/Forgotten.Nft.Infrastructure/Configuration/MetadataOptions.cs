using System.ComponentModel.DataAnnotations;

namespace Forgotten.Nft.Infrastructure.Configuration;

/// <summary>
/// Options for off-chain metadata resolution (spec §8). Metadata is untrusted:
/// responses are size- and time-bounded and fetched through an SSRF-guarded client.
/// </summary>
public sealed class MetadataOptions
{
    public const string SectionName = "Metadata";

    public string IpfsGateway { get; set; } = "https://ipfs.io/ipfs/";

    public string ArweaveGateway { get; set; } = "https://arweave.net/";

    [Range(1024, 50_000_000)]
    public int MaxResponseBytes { get; set; } = 2_000_000;

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 10;

    [Range(0, 10)]
    public int MaxRedirects { get; set; } = 3;
}
