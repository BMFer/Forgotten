using System.Text.Json;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Infrastructure.Configuration;
using Forgotten.Nft.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Infrastructure.Metadata;

/// <summary>
/// Resolves off-chain NFT metadata over HTTPS/IPFS/Arweave (spec §8.1) through an
/// SSRF-guarded HTTP client. Enforces scheme, content-type, redirect, size, and
/// time limits (spec §8.2). Any failure returns null so inspection degrades
/// gracefully rather than throwing (spec §5.4).
/// </summary>
public sealed class MetadataResolver : IMetadataResolver
{
    private static readonly JsonDocumentOptions DocOptions = new() { AllowTrailingCommas = true };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetadataOptions _options;
    private readonly ILogger<MetadataResolver> _logger;

    public MetadataResolver(
        IHttpClientFactory httpClientFactory,
        IOptions<MetadataOptions> options,
        ILogger<MetadataResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ResolvedMetadata?> ResolveAsync(string metadataUri, CancellationToken cancellationToken)
    {
        if (!TryNormalize(metadataUri, out var uri))
        {
            return null;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            var bytes = await FetchAsync(uri, timeoutCts.Token).ConfigureAwait(false);
            return bytes is null ? null : Parse(bytes);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Metadata resolution failed for {Uri}.", metadataUri);
            return null;
        }
    }

    private bool TryNormalize(string? input, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();

        if (input.StartsWith("ipfs://", StringComparison.OrdinalIgnoreCase))
        {
            var path = input["ipfs://".Length..].TrimStart('/');
            if (path.StartsWith("ipfs/", StringComparison.OrdinalIgnoreCase))
            {
                path = path["ipfs/".Length..];
            }

            input = _options.IpfsGateway.TrimEnd('/') + "/" + path;
        }
        else if (input.StartsWith("ar://", StringComparison.OrdinalIgnoreCase))
        {
            input = _options.ArweaveGateway.TrimEnd('/') + "/" + input["ar://".Length..].TrimStart('/');
        }

        return Uri.TryCreate(input, UriKind.Absolute, out uri!) && IsAllowedScheme(uri);
    }

    private static bool IsAllowedScheme(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;

    private async Task<byte[]?> FetchAsync(Uri uri, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(InfrastructureServiceCollectionExtensions.MetadataHttpClientName);

        for (var redirect = 0; redirect <= _options.MaxRedirects; redirect++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (IsRedirect(response.StatusCode))
            {
                var location = response.Headers.Location;
                if (location is null || redirect == _options.MaxRedirects)
                {
                    return null;
                }

                // Resolve relative redirects against the current URI; re-validate scheme.
                uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
                if (!IsAllowedScheme(uri))
                {
                    return null;
                }

                continue;
            }

            response.EnsureSuccessStatusCode();

            if (!IsAcceptableContentType(response.Content.Headers.ContentType?.MediaType))
            {
                return null;
            }

            return await ReadLimitedAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static bool IsRedirect(System.Net.HttpStatusCode code) =>
        (int)code is >= 300 and < 400;

    private static bool IsAcceptableContentType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return true; // many gateways omit/return generic types for JSON
        }

        if (mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
            mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
            mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private async Task<byte[]?> ReadLimitedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        var total = 0;

        while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > _options.MaxResponseBytes)
            {
                _logger.LogWarning("Metadata response exceeded {Max} bytes; discarding.", _options.MaxResponseBytes);
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static ResolvedMetadata Parse(byte[] json)
    {
        using var doc = JsonDocument.Parse(json, DocOptions);
        var root = doc.RootElement;

        return new ResolvedMetadata
        {
            Name = GetString(root, "name"),
            Symbol = GetString(root, "symbol"),
            Description = GetString(root, "description"),
            Image = GetString(root, "image"),
            AnimationUrl = GetString(root, "animation_url"),
            ExternalUrl = GetString(root, "external_url"),
            Attributes = ParseAttributes(root),
        };
    }

    private static IReadOnlyList<NftAttributeData> ParseAttributes(JsonElement root)
    {
        if (!root.TryGetProperty("attributes", out var attrs) || attrs.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<NftAttributeData>();
        }

        var list = new List<NftAttributeData>();
        foreach (var attr in attrs.EnumerateArray())
        {
            if (attr.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var traitType = GetString(attr, "trait_type");
            if (string.IsNullOrWhiteSpace(traitType))
            {
                continue;
            }

            var value = attr.TryGetProperty("value", out var v) ? ValueToString(v) : string.Empty;
            list.Add(new NftAttributeData(traitType!.Trim(), value, GetString(attr, "display_type")));
        }

        return list;
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string ValueToString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => string.Empty,
    };
}
