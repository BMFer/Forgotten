using System.Net.Http.Json;
using System.Text.Json;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Common;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Infrastructure.Blockchain.Das;
using Forgotten.Nft.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Infrastructure.Blockchain;

/// <summary>
/// <see cref="ISolanaAssetProvider"/> backed by a Metaplex DAS-compatible JSON-RPC
/// endpoint (spec §5.1). Uses the resilient named HTTP client (timeout/retry/circuit
/// breaker — spec §5.3). Responses are validated and normalized (spec §5.4). A
/// transport failure/timeout propagates as an exception so callers can report
/// "provider unavailable" distinctly from "not found" (spec §4.3, §16).
/// </summary>
public sealed class DasSolanaAssetProvider : ISolanaAssetProvider
{
    private static readonly HashSet<string> SupportedInterfaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "V1_NFT", "V1_PRINT", "LEGACY_NFT", "ProgrammableNFT", "MplCoreAsset",
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SolanaOptions _options;
    private readonly ILogger<DasSolanaAssetProvider> _logger;

    public DasSolanaAssetProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<SolanaOptions> options,
        ILogger<DasSolanaAssetProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "das";

    public async Task<SolanaAsset?> GetAssetAsync(string assetAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.DasEndpoint))
        {
            throw new InvalidOperationException("Solana DAS endpoint is not configured (Solana:DasEndpoint).");
        }

        var request = new
        {
            jsonrpc = "2.0",
            id = "forgotten",
            method = "getAsset",
            @params = new { id = assetAddress },
        };

        var client = _httpClientFactory.CreateClient(InfrastructureServiceCollectionExtensions.SolanaHttpClientName);

        using var httpResponse = await client
            .PostAsJsonAsync(_options.DasEndpoint, request, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        // Non-success after resilience retries is a provider failure, not "not found".
        httpResponse.EnsureSuccessStatusCode();

        var raw = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // Raw provider payloads are preserved only for diagnostics, size-limited (spec §5.4).
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("DAS getAsset raw response for {Address}: {Json}", assetAddress, Truncate(raw, 8000));
        }

        var payload = JsonSerializer.Deserialize<DasRpcResponse<DasAsset>>(raw, JsonOptions);

        if (payload?.Error is { } error)
        {
            if (IsNotFound(error))
            {
                return null;
            }

            throw new InvalidOperationException($"DAS error {error.Code}: {error.Message}");
        }

        return payload?.Result is { } asset ? Map(asset, assetAddress) : null;
    }

    public Task<PagedResult<SolanaAsset>> GetAssetsByOwnerAsync(
        string walletAddress, int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException("getAssetsByOwner is implemented in Phase 4.");

    public Task<PagedResult<SolanaAsset>> GetAssetsByCollectionAsync(
        string collectionAddress, int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException("getAssetsByGroup is implemented in Phase 2/4.");

    private SolanaAsset Map(DasAsset asset, string requestedAddress)
    {
        var collection = asset.Grouping?
            .FirstOrDefault(g => string.Equals(g.GroupKey, "collection", StringComparison.OrdinalIgnoreCase));

        var attributes = (asset.Content?.Metadata?.Attributes ?? new List<DasAttribute>())
            .Where(a => !string.IsNullOrWhiteSpace(a.TraitType))
            .Select(a => new NftAttributeData(a.TraitType!.Trim(), a.ValueAsString(), a.DisplayType))
            .ToList();

        var creators = (asset.Creators ?? new List<DasCreator>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Address))
            .Select(c => new SolanaCreator(c.Address!, c.Share, c.Verified))
            .ToList();

        return new SolanaAsset
        {
            AssetAddress = asset.Id ?? requestedAddress,
            InterfaceType = asset.Interface,
            IsSupportedNft = IsSupportedNft(asset),
            IsCompressed = asset.Compression?.Compressed ?? false,
            IsBurned = asset.Burnt ?? false,
            IsMutable = asset.Mutable,
            Name = NullIfBlank(asset.Content?.Metadata?.Name),
            Symbol = NullIfBlank(asset.Content?.Metadata?.Symbol),
            Description = NullIfBlank(asset.Content?.Metadata?.Description),
            ImageUrl = NullIfBlank(asset.Content?.Links?.Image),
            AnimationUrl = NullIfBlank(asset.Content?.Links?.AnimationUrl),
            ExternalUrl = NullIfBlank(asset.Content?.Links?.ExternalUrl),
            MetadataUri = NullIfBlank(asset.Content?.JsonUri),
            OwnerAddress = NullIfBlank(asset.Ownership?.Owner),
            CollectionAddress = NullIfBlank(collection?.GroupValue),
            CollectionVerified = collection?.Verified ?? false,
            TokenStandard = NullIfBlank(asset.Content?.Metadata?.TokenStandard),
            Supply = asset.TokenInfo?.Supply,
            Creators = creators,
            Attributes = attributes,
            Provider = Name,
            RetrievedAt = DateTimeOffset.UtcNow,
        };
    }

    private static bool IsSupportedNft(DasAsset asset)
    {
        var tokenStandard = asset.Content?.Metadata?.TokenStandard;
        if (!string.IsNullOrWhiteSpace(tokenStandard) &&
            tokenStandard.Contains("Fungible", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return asset.Interface is not null && SupportedInterfaces.Contains(asset.Interface);
    }

    private static bool IsNotFound(DasError error)
    {
        var message = error.Message ?? string.Empty;
        return message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("could not find", StringComparison.OrdinalIgnoreCase)
            || message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unable to find", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + $"…(+{value.Length - max} chars)";
}
