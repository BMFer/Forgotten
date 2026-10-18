using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Application.Configuration;

/// <summary>
/// Cross-field validation for <see cref="SolanaOptions"/> that DataAnnotations
/// cannot express. Combined with ValidateOnStart this makes startup fail closed
/// on invalid security-/correctness-critical settings (spec §20).
/// </summary>
public sealed class SolanaOptionsValidator : IValidateOptions<SolanaOptions>
{
    public ValidateOptionsResult Validate(string? name, SolanaOptions options)
    {
        var failures = new List<string>();

        if (options.DefaultPageSize > options.MaxPageSize)
        {
            failures.Add(
                $"{nameof(options.DefaultPageSize)} ({options.DefaultPageSize}) must not exceed " +
                $"{nameof(options.MaxPageSize)} ({options.MaxPageSize}).");
        }

        if (!string.IsNullOrWhiteSpace(options.DasEndpoint) &&
            !IsHttpsOrHttp(options.DasEndpoint))
        {
            failures.Add($"{nameof(options.DasEndpoint)} must be a valid http(s) URL.");
        }

        if (!string.IsNullOrWhiteSpace(options.RpcEndpoint) &&
            !IsHttpsOrHttp(options.RpcEndpoint))
        {
            failures.Add($"{nameof(options.RpcEndpoint)} must be a valid http(s) URL.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static bool IsHttpsOrHttp(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
