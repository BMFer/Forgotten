using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Forgotten.Bot.Embeds;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Forgotten.Bot.Components;

/// <summary>
/// Handles gallery button clicks (spec §6.2, §12). Every interaction re-validates
/// that the clicker owns the session — a different member's click is rejected and
/// never alters the gallery (spec §16.18). Navigation edits the existing (ephemeral)
/// message; Inspect replies with a separate ephemeral embed.
/// </summary>
public sealed class GalleryInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly INftGalleryService _gallery;
    private readonly INftInspectionService _inspection;
    private readonly NftEmbedFactory _embeds;
    private readonly ILogger<GalleryInteractions> _logger;

    public GalleryInteractions(
        INftGalleryService gallery,
        INftInspectionService inspection,
        NftEmbedFactory embeds,
        ILogger<GalleryInteractions> logger)
    {
        _gallery = gallery;
        _inspection = inspection;
        _embeds = embeds;
        _logger = logger;
    }

    [ComponentInteraction($"{GalleryComponents.Prefix}:*:*:*", ignoreGroupNames: true)]
    public async Task OnGalleryButtonAsync(string sessionIdRaw, string action, string pageRaw)
    {
        await DeferAsync().ConfigureAwait(false); // ack as a deferred message update

        if (!int.TryParse(sessionIdRaw, out var sessionId) || !int.TryParse(pageRaw, out var page))
        {
            await FollowupAsync("That showcase control is no longer valid.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var userId = Context.User.Id;
        var view = await _gallery.GetPageAsync(sessionId, userId, page, refresh: action == "refresh", CancellationToken.None).ConfigureAwait(false);

        if (await HandleNonRenderStatusAsync(view).ConfigureAwait(false))
        {
            return;
        }

        if (action == "inspect")
        {
            await InspectCurrentAsync(view).ConfigureAwait(false);
            return;
        }

        if (action == "flex")
        {
            await FlexCurrentAsync(view).ConfigureAwait(false);
            return;
        }

        await RenderAsync(view).ConfigureAwait(false);
    }

    [ComponentInteraction($"{GalleryComponents.SelectPrefix}:*", ignoreGroupNames: true)]
    public async Task OnCollectionSelectAsync(string sessionIdRaw, string[] selectedValues)
    {
        await DeferAsync().ConfigureAwait(false);

        if (!int.TryParse(sessionIdRaw, out var sessionId))
        {
            await FollowupAsync("That showcase control is no longer valid.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var selected = selectedValues.FirstOrDefault();
        var collection = string.Equals(selected, GalleryComponents.AllValue, StringComparison.Ordinal) ? null : selected;
        var view = await _gallery.SetCollectionFilterAsync(sessionId, Context.User.Id, collection, CancellationToken.None).ConfigureAwait(false);

        if (await HandleNonRenderStatusAsync(view).ConfigureAwait(false))
        {
            return;
        }

        await RenderAsync(view).ConfigureAwait(false);
    }

    /// <summary>Replies to NotOwner/Expired sessions. Returns true when the caller should stop.</summary>
    private async Task<bool> HandleNonRenderStatusAsync(GalleryView view)
    {
        switch (view.Status)
        {
            case GalleryViewStatus.NotOwner:
                await FollowupAsync("This isn't your showcase. Run `/nft showcase` to open your own.", ephemeral: true).ConfigureAwait(false);
                return true;

            case GalleryViewStatus.Expired:
                await FollowupAsync("This showcase has expired. Run `/nft showcase` again.", ephemeral: true).ConfigureAwait(false);
                return true;

            default:
                return false;
        }
    }

    private Task RenderAsync(GalleryView view) =>
        ModifyOriginalResponseAsync(m =>
        {
            m.Embed = _embeds.BuildGalleryEmbed(view);
            m.Components = GalleryComponents.Build(view);
            m.AllowedMentions = AllowedMentions.None;
        });

    /// <summary>
    /// Posts the current NFT to the channel as a PUBLIC (non-ephemeral) message so other members
    /// can see the flex. Who sees it is governed by the channel's permissions; who can flex is
    /// gated on the gallery command (holders-only role).
    /// </summary>
    private async Task FlexCurrentAsync(GalleryView view)
    {
        if (view.Status != GalleryViewStatus.Ok || view.Item is null)
        {
            await FollowupAsync("Nothing to flex yet — open a collection first.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var flexer = (Context.User as SocketGuildUser)?.DisplayName
            ?? Context.User.GlobalName
            ?? Context.User.Username;
        var avatarUrl = (Context.User as SocketGuildUser)?.GetDisplayAvatarUrl(size: 128)
            ?? Context.User.GetDisplayAvatarUrl(size: 128)
            ?? Context.User.GetDefaultAvatarUrl();

        try
        {
            var embed = _embeds.BuildFlexEmbed(view, flexer, avatarUrl);
            await FollowupAsync(embed: embed, ephemeral: false, allowedMentions: AllowedMentions.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Public flex failed for {Address}.", view.Item.AssetAddress);
            await FollowupAsync("Couldn't post that flex right now.", ephemeral: true).ConfigureAwait(false);
        }
    }

    private async Task InspectCurrentAsync(GalleryView view)
    {
        if (view.Status != GalleryViewStatus.Ok || view.Item is null)
        {
            await FollowupAsync("Nothing to inspect.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        try
        {
            var result = await _inspection.InspectAsync(view.Item.AssetAddress).ConfigureAwait(false);
            var embed = result.IsSuccess ? _embeds.BuildInspectionEmbed(result) : _embeds.BuildErrorEmbed(result);
            await FollowupAsync(embed: embed, ephemeral: true, allowedMentions: AllowedMentions.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gallery inspect failed for {Address}.", view.Item.AssetAddress);
            await FollowupAsync("Could not inspect that NFT right now.", ephemeral: true).ConfigureAwait(false);
        }
    }
}
