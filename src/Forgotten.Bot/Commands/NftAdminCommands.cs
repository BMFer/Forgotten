using Discord;
using Discord.Interactions;
using Forgotten.Bot.Embeds;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Bot.Commands;

/// <summary>
/// Administrator NFT tools (spec §6.5 admin subset, §13). Access is enforced
/// server-side and fails closed: both a default-permission gate (hides the
/// commands) and a hard Administrator precondition (rejects execution) apply,
/// and everything is guild-scoped (spec §6.5, §16.9, §16.15). Responses are
/// ephemeral so registry/audit data is not exposed publicly.
/// </summary>
[Group("nft-admin", "Administrator NFT tools")]
[RequireContext(ContextType.Guild)]
[DefaultMemberPermissions(GuildPermission.Administrator)]
[RequireUserPermission(GuildPermission.Administrator)]
public sealed class NftAdminCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICollectionRegistryService _registry;
    private readonly INftVerificationLogRepository _auditLog;
    private readonly ISolanaAssetProvider _assetProvider;
    private readonly IMemoryCache _cache;
    private readonly NftEmbedFactory _embeds;
    private readonly SolanaOptions _solanaOptions;
    private readonly ILogger<NftAdminCommands> _logger;

    public NftAdminCommands(
        ICollectionRegistryService registry,
        INftVerificationLogRepository auditLog,
        ISolanaAssetProvider assetProvider,
        IMemoryCache cache,
        NftEmbedFactory embeds,
        IOptions<SolanaOptions> solanaOptions,
        ILogger<NftAdminCommands> logger)
    {
        _registry = registry;
        _auditLog = auditLog;
        _assetProvider = assetProvider;
        _cache = cache;
        _embeds = embeds;
        _solanaOptions = solanaOptions.Value;
        _logger = logger;
    }

    [SlashCommand("collection-add", "Approve a collection for this server.")]
    public async Task CollectionAddAsync(
        [Summary("address", "The collection address")] string address,
        [Summary("name", "Optional display name")] string? name = null)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        try
        {
            await _registry.ApproveAsync(Context.Guild.Id, address, name, Context.User.Id, CancellationToken.None)
                .ConfigureAwait(false);
            var suffix = string.IsNullOrWhiteSpace(name) ? string.Empty : $" as **{name}**";
            await FollowupAsync($"✅ Approved collection `{address}`{suffix}.", ephemeral: true).ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            await FollowupAsync("That is not a valid Solana collection address.", ephemeral: true).ConfigureAwait(false);
        }
    }

    [SlashCommand("collection-remove", "Remove a collection's approval for this server.")]
    public async Task CollectionRemoveAsync([Summary("address", "The collection address")] string address)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        try
        {
            var removed = await _registry.RemoveAsync(Context.Guild.Id, address, CancellationToken.None).ConfigureAwait(false);
            await FollowupAsync(
                removed ? $"🗑️ Removed `{address}` from the registry." : "That collection was not in the registry.",
                ephemeral: true).ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            await FollowupAsync("That is not a valid Solana collection address.", ephemeral: true).ConfigureAwait(false);
        }
    }

    [SlashCommand("collection-list", "List collections approved for this server.")]
    public async Task CollectionListAsync()
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var collections = await _registry.ListAsync(Context.Guild.Id, CancellationToken.None).ConfigureAwait(false);
        await FollowupAsync(embed: _embeds.BuildRegistryEmbed(collections), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("provider-status", "Show the Solana provider configuration status.")]
    public async Task ProviderStatusAsync()
    {
        var configured = !string.IsNullOrWhiteSpace(_solanaOptions.DasEndpoint);
        var embed = new EmbedBuilder()
            .WithTitle("Solana Provider Status")
            .WithColor(configured ? new Color(0x2ECC71) : new Color(0xE67E22))
            .AddField("Provider", _assetProvider.Name, inline: true)
            .AddField("Network", _solanaOptions.Network, inline: true)
            .AddField("DAS Endpoint", configured ? "Configured" : "NOT configured", inline: true)
            .WithCurrentTimestamp()
            .Build();
        await RespondAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("cache-clear", "Clear cached NFT inspection data.")]
    public async Task CacheClearAsync()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Clear();
            await RespondAsync("🧹 NFT cache cleared.", ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            await RespondAsync("Cache clearing is not supported by the current cache implementation.", ephemeral: true)
                .ConfigureAwait(false);
        }
    }

    [SlashCommand("verification-log", "Show recent verification activity for this server.")]
    public async Task VerificationLogAsync()
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var logs = await _auditLog.GetRecentAsync(Context.Guild.Id, 15, CancellationToken.None).ConfigureAwait(false);
        await FollowupAsync(embed: _embeds.BuildVerificationLogEmbed(logs), ephemeral: true).ConfigureAwait(false);
    }
}
