using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DTOs;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Application.Services;

/// <summary>
/// Builds interactive NFT galleries (spec §6.2, §12). Owned NFTs are fetched across
/// the member's verified wallets with bounded provider paging — capped at
/// <see cref="NftEngineOptions.GalleryMaxItems"/> so a huge wallet never loads
/// wholesale (spec §6.2, §19) — cached briefly (spec §15), then filtered and paged
/// one NFT at a time. Navigation always checks session ownership (spec §12).
/// </summary>
public sealed class NftGalleryService : INftGalleryService
{
    private const string Blockchain = "solana";
    private static readonly TimeSpan OwnedCacheTtl = TimeSpan.FromSeconds(60);

    private readonly ISolanaAssetProvider _provider;
    private readonly IDiscordWalletConnectionRepository _connections;
    private readonly INftGallerySessionRepository _sessions;
    private readonly ICollectionRegistryService _registry;
    private readonly IMemoryCache _cache;
    private readonly NftEngineOptions _engineOptions;
    private readonly SolanaOptions _solanaOptions;
    private readonly ILogger<NftGalleryService> _logger;

    public NftGalleryService(
        ISolanaAssetProvider provider,
        IDiscordWalletConnectionRepository connections,
        INftGallerySessionRepository sessions,
        ICollectionRegistryService registry,
        IMemoryCache cache,
        IOptions<NftEngineOptions> engineOptions,
        IOptions<SolanaOptions> solanaOptions,
        ILogger<NftGalleryService> logger)
    {
        _provider = provider;
        _connections = connections;
        _sessions = sessions;
        _registry = registry;
        _cache = cache;
        _engineOptions = engineOptions.Value;
        _solanaOptions = solanaOptions.Value;
        _logger = logger;
    }

    public async Task<GalleryView> CreateAsync(ulong discordUserId, ulong guildId, string? collectionFilter, CancellationToken cancellationToken)
    {
        var wallets = await _connections.ListActiveByUserAsync(discordUserId, cancellationToken).ConfigureAwait(false);
        if (wallets.Count == 0)
        {
            return GalleryView.Of(GalleryViewStatus.NoWallet);
        }

        // No explicit collection? Default to this server's collection so members flex "our"
        // collection without passing an address (spec §6.2, §13).
        var filter = string.IsNullOrWhiteSpace(collectionFilter) ? null : collectionFilter.Trim();
        if (filter is null && guildId != 0)
        {
            var def = await _registry.GetDefaultAsync(guildId, cancellationToken).ConfigureAwait(false);
            filter = def?.CollectionAddress;
        }

        var session = new NftGallerySession
        {
            DiscordUserId = discordUserId,
            GuildId = guildId,
            WalletAddress = wallets[0].WalletAddress,
            CollectionFilter = filter,
            CurrentPage = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(Math.Max(1, _engineOptions.GallerySessionMinutes)),
        };

        await _sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
        return await BuildViewAsync(session, 0, refresh: false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GalleryView> GetPageAsync(int sessionId, ulong requestingUserId, int page, bool refresh, CancellationToken cancellationToken)
    {
        var session = await LoadOwnedSessionAsync(sessionId, requestingUserId, cancellationToken).ConfigureAwait(false);
        return session.view ?? await BuildViewAsync(session.session!, page, refresh, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GalleryView> SetCollectionFilterAsync(
        int sessionId, ulong requestingUserId, string? collectionAddress, CancellationToken cancellationToken)
    {
        var loaded = await LoadOwnedSessionAsync(sessionId, requestingUserId, cancellationToken).ConfigureAwait(false);
        if (loaded.view is not null)
        {
            return loaded.view;
        }

        var session = loaded.session!;
        session.CollectionFilter = string.IsNullOrWhiteSpace(collectionAddress) ? null : collectionAddress.Trim();
        session.CurrentPage = 0;
        await _sessions.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
        return await BuildViewAsync(session, 0, refresh: false, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(NftGallerySession? session, GalleryView? view)> LoadOwnedSessionAsync(
        int sessionId, ulong requestingUserId, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null || session.ExpiresAt <= DateTime.UtcNow)
        {
            return (null, GalleryView.Of(GalleryViewStatus.Expired));
        }

        if (session.DiscordUserId != requestingUserId)
        {
            return (null, GalleryView.Of(GalleryViewStatus.NotOwner));
        }

        return (session, null);
    }

    private async Task<GalleryView> BuildViewAsync(NftGallerySession session, int page, bool refresh, CancellationToken cancellationToken)
    {
        var owned = await GetOwnedAsync(session.DiscordUserId, refresh, cancellationToken).ConfigureAwait(false);
        var choices = await GetCollectionChoicesAsync(session.GuildId, cancellationToken).ConfigureAwait(false);

        var items = session.CollectionFilter is { Length: > 0 } filter
            ? owned.Items.Where(a => string.Equals(a.CollectionAddress, filter, StringComparison.Ordinal)).ToList()
            : owned.Items;

        if (items.Count == 0)
        {
            return new GalleryView
            {
                Status = GalleryViewStatus.Empty,
                SessionId = session.Id,
                OwnerUserId = session.DiscordUserId,
                CollectionFilter = session.CollectionFilter,
                AvailableCollections = choices,
                Truncated = owned.Truncated,
            };
        }

        var index = Math.Clamp(page, 0, items.Count - 1);
        return new GalleryView
        {
            Status = GalleryViewStatus.Ok,
            SessionId = session.Id,
            OwnerUserId = session.DiscordUserId,
            Item = items[index],
            PageIndex = index,
            TotalCount = items.Count,
            CollectionFilter = session.CollectionFilter,
            AvailableCollections = choices,
            Truncated = owned.Truncated,
        };
    }

    private async Task<IReadOnlyList<CollectionChoice>> GetCollectionChoicesAsync(ulong guildId, CancellationToken cancellationToken)
    {
        if (guildId == 0)
        {
            return Array.Empty<CollectionChoice>();
        }

        var approved = await _registry.ListApprovedAsync(guildId, cancellationToken).ConfigureAwait(false);
        return approved
            .Select(c => new CollectionChoice(
                c.CollectionAddress,
                string.IsNullOrWhiteSpace(c.DisplayName) ? Shorten(c.CollectionAddress) : c.DisplayName!,
                c.IsDefault))
            .ToList();
    }

    private static string Shorten(string address) =>
        address.Length <= 10 ? address : $"{address[..4]}…{address[^4..]}";

    private async Task<OwnedSet> GetOwnedAsync(ulong discordUserId, bool refresh, CancellationToken cancellationToken)
    {
        var cacheKey = $"nft:gallery:owned:{discordUserId}";
        if (!refresh && _cache.TryGetValue(cacheKey, out OwnedSet? cached) && cached is not null)
        {
            return cached;
        }

        var wallets = await _connections.ListActiveByUserAsync(discordUserId, cancellationToken).ConfigureAwait(false);
        var cap = Math.Max(1, _engineOptions.GalleryMaxItems);
        var pageSize = Math.Clamp(50, 1, Math.Max(1, _solanaOptions.MaxPageSize));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var collected = new List<SolanaAsset>();
        var truncated = false;

        foreach (var wallet in wallets)
        {
            var page = 1;
            while (collected.Count < cap)
            {
                var result = await _provider
                    .GetAssetsByOwnerAsync(wallet.WalletAddress, page, pageSize, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var asset in result.Items)
                {
                    if (!asset.IsSupportedNft || asset.IsBurned || !seen.Add(asset.AssetAddress))
                    {
                        continue;
                    }

                    collected.Add(asset);
                    if (collected.Count >= cap)
                    {
                        truncated = true;
                        break;
                    }
                }

                if (result.Items.Count < pageSize)
                {
                    break; // last page for this wallet
                }

                page++;
            }

            if (collected.Count >= cap)
            {
                break;
            }
        }

        var owned = new OwnedSet(collected, truncated);
        _cache.Set(cacheKey, owned, OwnedCacheTtl);
        return owned;
    }

    private sealed record OwnedSet(IReadOnlyList<SolanaAsset> Items, bool Truncated);
}
