using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.DTOs;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.Enums;
using Forgotten.Nft.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Forgotten.Nft.Application.Services;

/// <summary>
/// Layered verification (spec §4.2/§4.3). Evaluates — as independent facts —
/// whether the asset exists (L1), is a supported NFT (L2), is on-chain
/// collection-verified (L3), and is approved in this guild's registry (L4).
/// Ownership (L5/L6) is reported as not-evaluated until Phases 3/5. Every check is
/// written to the audit log (spec §9). A provider failure is ProviderUnavailable,
/// never NotFound (spec §4.3, §16).
/// </summary>
public sealed class NftVerificationService : INftVerificationService
{
    private readonly ISolanaAssetProvider _provider;
    private readonly ICollectionRegistryService _registry;
    private readonly INftVerificationLogRepository _auditLog;
    private readonly ILogger<NftVerificationService> _logger;

    public NftVerificationService(
        ISolanaAssetProvider provider,
        ICollectionRegistryService registry,
        INftVerificationLogRepository auditLog,
        ILogger<NftVerificationService> logger)
    {
        _provider = provider;
        _registry = registry;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<NftVerificationResult> VerifyAsync(
        ulong guildId,
        string assetAddress,
        ulong? requestingUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (!SolanaAddress.TryParse(assetAddress, out var address))
        {
            var invalid = NftVerificationResult.Failure(
                assetAddress ?? string.Empty,
                NftVerificationStatus.Indeterminate,
                ErrorCodes.InvalidAssetAddress,
                "That is not a valid Solana asset address.");
            await AuditAsync(invalid, guildId, requestingUserId, cancellationToken).ConfigureAwait(false);
            return invalid;
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
            _logger.LogWarning(ex, "Provider {Provider} unavailable verifying {Asset}.", _provider.Name, address.Value);
            var unavailable = NftVerificationResult.Failure(
                address.Value,
                NftVerificationStatus.ProviderUnavailable,
                ErrorCodes.BlockchainProviderUnavailable,
                "The blockchain provider is temporarily unavailable. Please try again shortly.");
            await AuditAsync(unavailable, guildId, requestingUserId, cancellationToken).ConfigureAwait(false);
            return unavailable;
        }

        if (asset is null)
        {
            var notFound = NftVerificationResult.Failure(
                address.Value,
                NftVerificationStatus.NotFound,
                ErrorCodes.NftNotFound,
                "No asset was found at that address.");
            await AuditAsync(notFound, guildId, requestingUserId, cancellationToken).ConfigureAwait(false);
            return notFound;
        }

        var approved = false;
        string? collectionName = null;
        if (asset.CollectionAddress is not null)
        {
            var entry = await _registry.GetAsync(guildId, asset.CollectionAddress, cancellationToken).ConfigureAwait(false);
            approved = entry is { IsApproved: true };
            if (approved)
            {
                collectionName = entry!.DisplayName;
            }
        }

        var status = !asset.IsSupportedNft
            ? NftVerificationStatus.Unsupported
            : asset.IsBurned
                ? NftVerificationStatus.Burned
                : NftVerificationStatus.Verified;

        var result = new NftVerificationResult
        {
            AssetAddress = address.Value,
            AssetExists = true,
            IsSupportedNft = asset.IsSupportedNft,
            CollectionVerified = asset.IsSupportedNft && asset.CollectionVerified,
            CollectionApproved = asset.IsSupportedNft && approved,
            OwnerAddress = asset.OwnerAddress,
            OwnershipVerified = false,       // Phase 5
            MemberOwnershipVerified = false, // Phase 3
            VerificationStatus = status,
            HighestLevel = DetermineLevel(asset.IsSupportedNft, asset.CollectionVerified, approved),
            VerificationTimestamp = DateTimeOffset.UtcNow,
            Provider = asset.Provider,
            Name = asset.Name,
            CollectionAddress = asset.CollectionAddress,
            CollectionName = collectionName,
        };

        await AuditAsync(result, guildId, requestingUserId, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static NftVerificationLevel DetermineLevel(bool supported, bool collectionVerified, bool approved)
    {
        if (!supported)
        {
            return NftVerificationLevel.AssetExists;
        }

        if (approved)
        {
            return NftVerificationLevel.CollectionApproved;
        }

        return collectionVerified ? NftVerificationLevel.CollectionVerified : NftVerificationLevel.NftClassified;
    }

    private async Task AuditAsync(
        NftVerificationResult result, ulong guildId, ulong? userId, CancellationToken cancellationToken)
    {
        try
        {
            await _auditLog.AddAsync(
                new NftVerificationLog
                {
                    DiscordUserId = userId,
                    GuildId = guildId,
                    AssetAddress = result.AssetAddress,
                    VerificationType = NftVerificationType.Verify,
                    VerificationStatus = result.VerificationStatus,
                    Provider = result.Provider,
                    CheckedAt = DateTime.UtcNow,
                    ErrorCode = result.ErrorCode,
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write verification audit log for {Asset}.", result.AssetAddress);
        }
    }
}
