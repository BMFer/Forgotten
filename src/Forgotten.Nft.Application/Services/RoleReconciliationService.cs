using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Application.Services;

/// <summary>
/// Reconciles Discord roles against current NFT ownership for role-mapped, approved
/// collections (spec §11). Grants a role when the member owns a qualifying NFT and
/// removes it when they no longer do — but <b>never removes a role because the
/// provider was unavailable</b> (spec §11 grace). Role hierarchy/permission checks
/// live in the <see cref="IDiscordRoleService"/> implementation.
/// </summary>
public sealed class RoleReconciliationService : IRoleReconciliationService
{
    private const string Blockchain = "solana";
    private const int MaxUsersPerSweep = 100;

    private readonly IGuildRoleMappingRepository _roleMappings;
    private readonly IDiscordWalletConnectionRepository _connections;
    private readonly ISolanaAssetProvider _provider;
    private readonly IDiscordRoleService _roles;
    private readonly NftEngineOptions _engineOptions;
    private readonly SolanaOptions _solanaOptions;
    private readonly ILogger<RoleReconciliationService> _logger;

    public RoleReconciliationService(
        IGuildRoleMappingRepository roleMappings,
        IDiscordWalletConnectionRepository connections,
        ISolanaAssetProvider provider,
        IDiscordRoleService roles,
        IOptions<NftEngineOptions> engineOptions,
        IOptions<SolanaOptions> solanaOptions,
        ILogger<RoleReconciliationService> logger)
    {
        _roleMappings = roleMappings;
        _connections = connections;
        _provider = provider;
        _roles = roles;
        _engineOptions = engineOptions.Value;
        _solanaOptions = solanaOptions.Value;
        _logger = logger;
    }

    public async Task<int> ReconcileAllAsync(CancellationToken cancellationToken)
    {
        var mappings = await _roleMappings.ListAllAsync(cancellationToken).ConfigureAwait(false);
        if (mappings.Count == 0)
        {
            return 0;
        }

        var guilds = mappings.Select(m => m.GuildId).Distinct().ToList();
        var users = await _connections.ListActiveUserIdsAsync(MaxUsersPerSweep, cancellationToken).ConfigureAwait(false);

        var changes = 0;
        foreach (var guildId in guilds)
        {
            var guildMappings = mappings.Where(m => m.GuildId == guildId).ToList();
            foreach (var userId in users)
            {
                cancellationToken.ThrowIfCancellationRequested();
                changes += await ReconcileCoreAsync(guildId, userId, guildMappings, cancellationToken).ConfigureAwait(false);
            }
        }

        return changes;
    }

    public async Task ReconcileMemberAsync(ulong guildId, ulong discordUserId, CancellationToken cancellationToken)
    {
        var mappings = await _roleMappings.ListByGuildAsync(guildId, cancellationToken).ConfigureAwait(false);
        if (mappings.Count > 0)
        {
            await ReconcileCoreAsync(guildId, discordUserId, mappings, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int> ReconcileCoreAsync(
        ulong guildId, ulong userId, IReadOnlyList<GuildRoleMapping> mappings, CancellationToken cancellationToken)
    {
        var wallets = await _connections.ListActiveByUserAsync(userId, cancellationToken).ConfigureAwait(false);

        HashSet<string> ownedCollections;
        try
        {
            ownedCollections = await GetOwnedCollectionsAsync(wallets, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Provider unavailable: never strip roles on an outage (spec §11). Skip this member.
            _logger.LogWarning(ex, "Ownership lookup failed for user {User} in guild {Guild}; skipping (no role removal).", userId, guildId);
            return 0;
        }

        var changes = 0;
        foreach (var mapping in mappings)
        {
            var qualifies = ownedCollections.Contains(mapping.CollectionAddress);
            var hasRole = await _roles.HasRoleAsync(guildId, userId, mapping.RoleId, cancellationToken).ConfigureAwait(false);

            if (qualifies && !hasRole)
            {
                if (await _roles.AssignRoleAsync(guildId, userId, mapping.RoleId, "owns qualifying NFT", cancellationToken).ConfigureAwait(false))
                {
                    changes++;
                }
            }
            else if (!qualifies && hasRole)
            {
                if (await _roles.RemoveRoleAsync(guildId, userId, mapping.RoleId, "no longer owns qualifying NFT", cancellationToken).ConfigureAwait(false))
                {
                    changes++;
                }
            }
        }

        return changes;
    }

    private async Task<HashSet<string>> GetOwnedCollectionsAsync(
        IReadOnlyList<DiscordWalletConnection> wallets, CancellationToken cancellationToken)
    {
        var cap = Math.Max(1, _engineOptions.GalleryMaxItems);
        var pageSize = Math.Clamp(50, 1, Math.Max(1, _solanaOptions.MaxPageSize));
        var collections = new HashSet<string>(StringComparer.Ordinal);
        var loaded = 0;

        foreach (var wallet in wallets)
        {
            var page = 1;
            while (loaded < cap)
            {
                var result = await _provider
                    .GetAssetsByOwnerAsync(wallet.WalletAddress, page, pageSize, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var asset in result.Items)
                {
                    loaded++;
                    if (asset is { IsSupportedNft: true, IsBurned: false } && asset.CollectionAddress is { Length: > 0 } collection)
                    {
                        collections.Add(collection);
                    }
                }

                if (result.Items.Count < pageSize || loaded >= cap)
                {
                    break;
                }

                page++;
            }
        }

        return collections;
    }
}
