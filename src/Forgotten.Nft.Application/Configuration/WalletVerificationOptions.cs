using System.ComponentModel.DataAnnotations;

namespace Forgotten.Nft.Application.Configuration;

/// <summary>
/// Wallet-verification options (spec §7). The verification site must be served over
/// HTTPS (spec §7.3, §16.12); <see cref="WalletVerificationOptionsValidator"/> enforces
/// that at startup (allowing plain-HTTP only for localhost dev, which browsers treat
/// as a secure context).
/// </summary>
public sealed class WalletVerificationOptions
{
    public const string SectionName = "WalletVerification";

    /// <summary>Public base URL of the wallet-verification web app, e.g. https://verify.example.com.</summary>
    public string PublicBaseUrl { get; set; } = "https://localhost:7443";

    /// <summary>Domain shown in the signed message; defaults to the host of <see cref="PublicBaseUrl"/>.</summary>
    public string? Domain { get; set; }

    /// <summary>
    /// TipLink Wallet Adapter client id (request from TipLink). When set, the
    /// verification page offers "Connect with TipLink" (Google sign-in wallet).
    /// Empty/null hides the TipLink option; injected wallets still work.
    /// </summary>
    public string? TipLinkClientId { get; set; }

    /// <summary>How long a verification session/challenge remains valid after creation.</summary>
    [Range(60, 86400)]
    public int SessionTtlSeconds { get; set; } = 900;

    [Range(1, 50)]
    public int MaximumLinkedWallets { get; set; } = 5;

    /// <summary>
    /// When true, a verified wallet may belong to only one active Discord identity at a
    /// time (spec §7.4 default policy).
    /// </summary>
    public bool OneWalletPerIdentity { get; set; } = true;

    public string EffectiveDomain =>
        !string.IsNullOrWhiteSpace(Domain)
            ? Domain!
            : Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var uri) ? uri.Host : PublicBaseUrl;
}
