using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Application.Configuration;

/// <summary>
/// Cross-field validation for <see cref="NftEngineOptions"/> (spec §20).
/// Security-critical combinations must fail closed rather than silently continue.
/// </summary>
public sealed class NftEngineOptionsValidator : IValidateOptions<NftEngineOptions>
{
    public ValidateOptionsResult Validate(string? name, NftEngineOptions options)
    {
        var failures = new List<string>();

        if (options.EnableOwnershipMonitoring && options.OwnershipPollingMinutes < 1)
        {
            failures.Add(
                $"{nameof(options.OwnershipPollingMinutes)} must be >= 1 when " +
                $"{nameof(options.EnableOwnershipMonitoring)} is true.");
        }

        if (options.DefaultGalleryPageSize < 1)
        {
            failures.Add($"{nameof(options.DefaultGalleryPageSize)} must be >= 1.");
        }

        if (options.MaximumLinkedWallets < 1)
        {
            failures.Add($"{nameof(options.MaximumLinkedWallets)} must be >= 1.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
