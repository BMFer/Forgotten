using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DTOs;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.Enums;
using Forgotten.Nft.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Application.Services;

/// <summary>
/// Orchestrates NFT inspection (spec §4.1): validate → fetch → normalize →
/// classify → persist snapshot → return a display-ready result. Provider failures
/// are reported as <see cref="NftVerificationStatus.ProviderUnavailable"/> and are
/// never confused with a non-existent asset (spec §4.3, §16).
/// </summary>
public sealed class NftInspectionService : INftInspectionService
{
    private const string Blockchain = "solana";

    private readonly ISolanaAssetProvider _provider;
    private readonly IMetadataResolver _metadataResolver;
    private readonly INftAssetRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly SolanaOptions _options;
    private readonly ILogger<NftInspectionService> _logger;

    public NftInspectionService(
        ISolanaAssetProvider provider,
        IMetadataResolver metadataResolver,
        INftAssetRepository repository,
        IMemoryCache cache,
        IOptions<SolanaOptions> options,
        ILogger<NftInspectionService> logger)
    {
        _provider = provider;
        _metadataResolver = metadataResolver;
        _repository = repository;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<NftInspectionResult> InspectAsync(
        string assetAddress,
        bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        if (!SolanaAddress.TryParse(assetAddress, out var address))
        {
            return NftInspectionResult.Failure(
                assetAddress ?? string.Empty,
                NftVerificationStatus.Indeterminate,
                ErrorCodes.InvalidAssetAddress,
                "That is not a valid Solana asset address.");
        }

        var cacheKey = CacheKey(address.Value);
        if (!refresh && _cache.TryGetValue(cacheKey, out NftInspectionResult? cached) && cached is not null)
        {
            return cached with { ServedFromCache = true };
        }

        SolanaAsset? asset;
        try
        {
            asset = await _provider.GetAssetAsync(address.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Timeout / transport / rate-limit after retries: fail safe, never "not found" (spec §4.3, §16).
            _logger.LogWarning(ex, "Provider {Provider} unavailable inspecting {Asset}.", _provider.Name, address.Value);
            return NftInspectionResult.Failure(
                address.Value,
                NftVerificationStatus.ProviderUnavailable,
                ErrorCodes.BlockchainProviderUnavailable,
                "The blockchain provider is temporarily unavailable. Please try again shortly.");
        }

        if (asset is null)
        {
            return NftInspectionResult.Failure(
                address.Value,
                NftVerificationStatus.NotFound,
                ErrorCodes.NftNotFound,
                "No asset was found at that address.");
        }

        if (!asset.IsSupportedNft)
        {
            return new NftInspectionResult
            {
                AssetAddress = address.Value,
                Status = NftVerificationStatus.Unsupported,
                AssetExists = true,
                IsSupportedNft = false,
                InterfaceType = asset.InterfaceType,
                Provider = asset.Provider,
                RetrievedAt = asset.RetrievedAt,
                ErrorCode = ErrorCodes.UnsupportedAsset,
                ErrorMessage = "That asset exists but is not a supported NFT.",
            };
        }

        var enriched = await EnrichFromMetadataAsync(asset, cancellationToken).ConfigureAwait(false);
        var result = BuildResult(enriched);

        await PersistSnapshotAsync(enriched, cancellationToken).ConfigureAwait(false);

        if (result.Status is NftVerificationStatus.Verified or NftVerificationStatus.Burned)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(Math.Max(1, _options.CacheDurationSeconds)));
        }

        return result;
    }

    private async Task<SolanaAsset> EnrichFromMetadataAsync(SolanaAsset asset, CancellationToken cancellationToken)
    {
        var needsEnrichment =
            asset.Attributes.Count == 0 ||
            string.IsNullOrWhiteSpace(asset.ImageUrl) ||
            string.IsNullOrWhiteSpace(asset.Description);

        if (!needsEnrichment || string.IsNullOrWhiteSpace(asset.MetadataUri))
        {
            return asset;
        }

        var metadata = await _metadataResolver.ResolveAsync(asset.MetadataUri!, cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            return asset;
        }

        return new SolanaAsset
        {
            AssetAddress = asset.AssetAddress,
            InterfaceType = asset.InterfaceType,
            IsSupportedNft = asset.IsSupportedNft,
            IsCompressed = asset.IsCompressed,
            IsBurned = asset.IsBurned,
            IsMutable = asset.IsMutable,
            Name = asset.Name ?? metadata.Name,
            Symbol = asset.Symbol ?? metadata.Symbol,
            Description = asset.Description ?? metadata.Description,
            ImageUrl = asset.ImageUrl ?? metadata.Image,
            AnimationUrl = asset.AnimationUrl ?? metadata.AnimationUrl,
            ExternalUrl = asset.ExternalUrl ?? metadata.ExternalUrl,
            MetadataUri = asset.MetadataUri,
            OwnerAddress = asset.OwnerAddress,
            CollectionAddress = asset.CollectionAddress,
            CollectionVerified = asset.CollectionVerified,
            TokenStandard = asset.TokenStandard,
            Supply = asset.Supply,
            Creators = asset.Creators,
            Attributes = asset.Attributes.Count > 0 ? asset.Attributes : metadata.Attributes,
            Provider = asset.Provider,
            RetrievedAt = asset.RetrievedAt,
        };
    }

    private static NftInspectionResult BuildResult(SolanaAsset asset) => new()
    {
        AssetAddress = asset.AssetAddress,
        Status = asset.IsBurned ? NftVerificationStatus.Burned : NftVerificationStatus.Verified,
        AssetExists = true,
        IsSupportedNft = true,
        InterfaceType = asset.InterfaceType,
        Name = asset.Name,
        Symbol = asset.Symbol,
        Description = asset.Description,
        ImageUrl = asset.ImageUrl,
        AnimationUrl = asset.AnimationUrl,
        ExternalUrl = asset.ExternalUrl,
        MetadataUri = asset.MetadataUri,
        OwnerAddress = asset.OwnerAddress,
        CollectionAddress = asset.CollectionAddress,
        CollectionVerified = asset.CollectionVerified,
        TokenStandard = asset.TokenStandard,
        Supply = asset.Supply,
        IsCompressed = asset.IsCompressed,
        IsBurned = asset.IsBurned,
        IsMutable = asset.IsMutable,
        Creators = asset.Creators,
        Attributes = asset.Attributes,
        Provider = asset.Provider,
        RetrievedAt = asset.RetrievedAt,
    };

    private async Task PersistSnapshotAsync(SolanaAsset asset, CancellationToken cancellationToken)
    {
        try
        {
            var entity = new NftAsset
            {
                Blockchain = Blockchain,
                AssetAddress = asset.AssetAddress,
                CollectionAddress = asset.CollectionAddress,
                Name = asset.Name,
                Symbol = asset.Symbol,
                Description = asset.Description,
                ImageUrl = asset.ImageUrl,
                AnimationUrl = asset.AnimationUrl,
                MetadataUri = asset.MetadataUri,
                OwnerAddress = asset.OwnerAddress,
                IsCompressed = asset.IsCompressed,
                IsBurned = asset.IsBurned,
                LastFetchedAt = asset.RetrievedAt.UtcDateTime,
                Attributes = asset.Attributes
                    .Select(a => new NftAttribute
                    {
                        TraitType = a.TraitType,
                        TraitValue = a.TraitValue,
                        DisplayType = a.DisplayType,
                    })
                    .ToList(),
            };

            await _repository.UpsertAsync(entity, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Persistence is a snapshot/cache concern; never fail the inspection because of it.
            _logger.LogWarning(ex, "Failed to persist NFT snapshot for {Asset}.", asset.AssetAddress);
        }
    }

    private static string CacheKey(string address) => $"nft:inspect:{address}";
}
