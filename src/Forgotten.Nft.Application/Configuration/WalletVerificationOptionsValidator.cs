using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Application.Configuration;

/// <summary>
/// Fails startup closed on insecure wallet-verification configuration (spec §7.3,
/// §16.12, §20): the public URL must be HTTPS (localhost may use HTTP for dev), and
/// the session must live at least as long as a challenge.
/// </summary>
public sealed class WalletVerificationOptionsValidator : IValidateOptions<WalletVerificationOptions>
{
    public ValidateOptionsResult Validate(string? name, WalletVerificationOptions options)
    {
        var failures = new List<string>();

        if (!Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri))
        {
            failures.Add($"{nameof(options.PublicBaseUrl)} must be an absolute URL.");
        }
        else
        {
            var isLocalhost = uri.IsLoopback;
            if (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && isLocalhost))
            {
                failures.Add($"{nameof(options.PublicBaseUrl)} must use HTTPS (plain HTTP is permitted only for localhost).");
            }
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
