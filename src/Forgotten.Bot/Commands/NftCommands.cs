using Discord;
using Discord.Interactions;
using Forgotten.Bot.Components;
using Forgotten.Bot.Embeds;
using Forgotten.Bot.Services;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DTOs;
using Forgotten.Nft.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Bot.Commands;

/// <summary>
/// NFT slash commands (spec §6.1–6.4/§6.5). Handlers stay thin: they defer,
/// delegate to the application service, and render an embed (spec §3, §19).
/// </summary>
[Group("nft", "Inspect and explore Solana NFTs")]
public sealed class NftCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly INftInspectionService _inspection;
    private readonly INftVerificationService _verification;
    private readonly INftGalleryService _gallery;
    private readonly ISolanaAssetProvider _assetProvider;
    private readonly ICollectionRegistryService _registry;
    private readonly NftEmbedFactory _embeds;
    private readonly CommandRateLimiter _rateLimiter;
    private readonly SolanaOptions _solanaOptions;
    private readonly ILogger<NftCommands> _logger;

    public NftCommands(
        INftInspectionService inspection,
        INftVerificationService verification,
        INftGalleryService gallery,
        ISolanaAssetProvider assetProvider,
        ICollectionRegistryService registry,
        NftEmbedFactory embeds,
        CommandRateLimiter rateLimiter,
        IOptions<SolanaOptions> solanaOptions,
        ILogger<NftCommands> logger)
    {
        _inspection = inspection;
        _verification = verification;
        _gallery = gallery;
        _assetProvider = assetProvider;
        _registry = registry;
        _embeds = embeds;
        _rateLimiter = rateLimiter;
        _solanaOptions = solanaOptions.Value;
        _logger = logger;
    }

    [SlashCommand("gallery", "Browse the NFTs held by your verified wallet(s).")]
    public async Task GalleryAsync([Summary("collection", "Optional collection address to filter by")] string? collection = null)
    {
        if (!_rateLimiter.TryAcquire(Context.User.Id, "gallery", out var retryAfter))
        {
            await RespondAsync($"⏳ Slow down — try again in {Math.Ceiling(retryAfter.TotalSeconds)}s.", ephemeral: true)
                .ConfigureAwait(false);
            return;
        }

        // Private by default so wallet holdings aren't exposed publicly (spec §12, §16.13).
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        GalleryView view;
        try
        {
            view = await _gallery.CreateAsync(Context.User.Id, Context.Guild?.Id ?? 0UL, collection, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gallery load failed for {User}.", Context.User.Id);
            await ModifyOriginalResponseAsync(m =>
                m.Content = "Couldn't load your gallery right now — the blockchain provider may be unavailable or not DAS-capable. Please try again shortly.")
                .ConfigureAwait(false);
            return;
        }

        if (view.Status == GalleryViewStatus.NoWallet)
        {
            await ModifyOriginalResponseAsync(m =>
                m.Content = "You need a verified wallet first. Use `/wallet connect`.").ConfigureAwait(false);
            return;
        }

        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = _embeds.BuildGalleryEmbed(view);
            m.Components = GalleryComponents.Build(view);
            m.AllowedMentions = AllowedMentions.None;
        }).ConfigureAwait(false);
    }

    [SlashCommand("verify", "Verify an NFT: validity, on-chain collection, and registry approval.")]
    public async Task VerifyAsync([Summary("address", "The NFT asset/mint address")] string address)
    {
        if (!_rateLimiter.TryAcquire(Context.User.Id, "verify", out var retryAfter))
        {
            await RespondAsync($"⏳ Slow down — try again in {Math.Ceiling(retryAfter.TotalSeconds)}s.", ephemeral: true)
                .ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);

        var guildId = Context.Guild?.Id ?? 0UL;
        var result = await _verification.VerifyAsync(guildId, address, Context.User.Id).ConfigureAwait(false);

        var embed = result.IsSuccess ? _embeds.BuildVerificationEmbed(result) : _embeds.BuildErrorEmbed(result);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = embed;
            m.AllowedMentions = AllowedMentions.None;
        }).ConfigureAwait(false);
    }

    [SlashCommand("collection", "Show a Solana collection and a page of its NFTs.")]
    public async Task CollectionAsync([Summary("address", "The collection address")] string address)
    {
        if (!_rateLimiter.TryAcquire(Context.User.Id, "collection", out var retryAfter))
        {
            await RespondAsync($"⏳ Slow down — try again in {Math.Ceiling(retryAfter.TotalSeconds)}s.", ephemeral: true)
                .ConfigureAwait(false);
            return;
        }

        if (!SolanaAddress.TryParse(address, out var collection))
        {
            await RespondAsync("That is not a valid Solana collection address.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);

        try
        {
            var page = await _assetProvider
                .GetAssetsByCollectionAsync(collection.Value, 1, _solanaOptions.DefaultPageSize, CancellationToken.None)
                .ConfigureAwait(false);
            var registryEntry = Context.Guild is null
                ? null
                : await _registry.GetAsync(Context.Guild.Id, collection.Value, CancellationToken.None).ConfigureAwait(false);

            var embed = _embeds.BuildCollectionEmbed(collection.Value, page, registryEntry);
            await ModifyOriginalResponseAsync(m =>
            {
                m.Embed = embed;
                m.AllowedMentions = AllowedMentions.None;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Collection lookup failed for {Address}.", address);
            await ModifyOriginalResponseAsync(m =>
                m.Content = "The blockchain provider is temporarily unavailable. Please try again shortly.")
                .ConfigureAwait(false);
        }
    }

    [SlashCommand("inspect", "Inspect a Solana NFT by its asset (mint) address.")]
    public Task InspectAsync([Summary("address", "The NFT asset/mint address")] string address)
        => HandleInspectAsync(address, refresh: false, attributesOnly: false);

    [SlashCommand("attributes", "Show the attributes/traits of a Solana NFT.")]
    public Task AttributesAsync([Summary("address", "The NFT asset/mint address")] string address)
        => HandleInspectAsync(address, refresh: false, attributesOnly: true);

    [SlashCommand("refresh", "Re-fetch a Solana NFT, bypassing the cache.")]
    public Task RefreshAsync([Summary("address", "The NFT asset/mint address")] string address)
        => HandleInspectAsync(address, refresh: true, attributesOnly: false);

    [SlashCommand("help", "How to use the FORGOTTEN NFT commands.")]
    public async Task HelpAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("FORGOTTEN NFT — Commands")
            .WithColor(new Color(0x5865F2))
            .WithDescription(
                "`/nft inspect <address>` — inspect an NFT and show its artwork, owner, and traits.\n" +
                "`/nft verify <address>` — validity, on-chain collection, and registry approval.\n" +
                "`/nft gallery [collection]` — browse the NFTs in your verified wallet(s).\n" +
                "`/nft collection <address>` — show a collection and a page of its NFTs.\n" +
                "`/nft attributes <address>` — show just the NFT's traits.\n" +
                "`/nft refresh <address>` — re-fetch an NFT, bypassing the cache.\n" +
                "`/nft help` — show this message.\n\n" +
                "Addresses are Solana asset/mint addresses. Inspection reflects a recent snapshot " +
                "and is not proof of current ownership.")
            .Build();

        await RespondAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    private async Task HandleInspectAsync(string address, bool refresh, bool attributesOnly)
    {
        var command = attributesOnly ? "attributes" : refresh ? "refresh" : "inspect";
        _logger.LogInformation("/nft {Command} invoked by {User} for {Address}", command, Context.User.Id, address);

        if (!_rateLimiter.TryAcquire(Context.User.Id, command, out var retryAfter))
        {
            await RespondAsync(
                $"⏳ Slow down — try again in {Math.Ceiling(retryAfter.TotalSeconds)}s.",
                ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Defer before the (potentially slow) provider call (spec §19).
        await DeferAsync().ConfigureAwait(false);

        try
        {
            var result = await _inspection.InspectAsync(address, refresh).ConfigureAwait(false);

            var embed = !result.IsSuccess
                ? _embeds.BuildErrorEmbed(result)
                : attributesOnly
                    ? _embeds.BuildAttributesEmbed(result)
                    : _embeds.BuildInspectionEmbed(result);

            await ModifyOriginalResponseAsync(m =>
            {
                m.Embed = embed;
                m.AllowedMentions = AllowedMentions.None;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in /nft {Command} for {Address}.", command, address);
            await ModifyOriginalResponseAsync(m =>
                m.Content = "Something went wrong while inspecting that NFT. Please try again.")
                .ConfigureAwait(false);
        }
    }
}
