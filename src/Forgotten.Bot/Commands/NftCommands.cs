using Discord;
using Discord.Interactions;
using Forgotten.Bot.Embeds;
using Forgotten.Bot.Services;
using Forgotten.Nft.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Forgotten.Bot.Commands;

/// <summary>
/// NFT slash commands (spec §6.1/§6.5 inspection subset). Handlers stay thin:
/// they defer, delegate to the application service, and render an embed (spec §3, §19).
/// </summary>
[Group("nft", "Inspect and explore Solana NFTs")]
public sealed class NftCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly INftInspectionService _inspection;
    private readonly NftEmbedFactory _embeds;
    private readonly CommandRateLimiter _rateLimiter;
    private readonly ILogger<NftCommands> _logger;

    public NftCommands(
        INftInspectionService inspection,
        NftEmbedFactory embeds,
        CommandRateLimiter rateLimiter,
        ILogger<NftCommands> logger)
    {
        _inspection = inspection;
        _embeds = embeds;
        _rateLimiter = rateLimiter;
        _logger = logger;
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
