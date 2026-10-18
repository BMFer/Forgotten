using System.ComponentModel.DataAnnotations;

namespace Forgotten.Nft.Application.Configuration;

/// <summary>
/// Solana provider options (spec §5.2). API keys must NOT live here — they are
/// supplied via environment variables, user secrets, or another secure provider
/// (spec §5.2, §16.11). This type only carries non-secret endpoint/tuning values.
/// </summary>
public sealed class SolanaOptions
{
    public const string SectionName = "Solana";

    public string Network { get; set; } = "mainnet-beta";

    /// <summary>DAS-compatible endpoint (spec §5.1). Not every RPC supports DAS.</summary>
    public string? DasEndpoint { get; set; }

    /// <summary>Standard Solana RPC endpoint, configured independently of DAS (spec §5.1).</summary>
    public string? RpcEndpoint { get; set; }

    [Range(1, 120)]
    public int RequestTimeoutSeconds { get; set; } = 15;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    [Range(1, 100)]
    public int DefaultPageSize { get; set; } = 20;

    [Range(1, 1000)]
    public int MaxPageSize { get; set; } = 100;

    [Range(0, 86400)]
    public int CacheDurationSeconds { get; set; } = 300;
}
