using Discord;
using Forgotten.Nft.Application.DTOs;

namespace Forgotten.Bot.Components;

/// <summary>
/// Builds the gallery's interactive buttons (spec §6.2, §12). The session id and
/// target page travel in each custom id (<c>nftgal:{sessionId}:{action}:{page}</c>)
/// so navigation needs no per-click DB write; the handler re-checks session ownership.
/// </summary>
public static class GalleryComponents
{
    public const string Prefix = "nftgal";

    public static MessageComponent Build(GalleryView view)
    {
        var builder = new ComponentBuilder();

        if (view.Status == GalleryViewStatus.Empty)
        {
            // Only offer Refresh, plus a way to clear an active filter.
            builder.WithButton("Refresh", Id(view.SessionId, "refresh", 0), ButtonStyle.Secondary, new Emoji("🔄"));
            if (view.CollectionFilter is { Length: > 0 })
            {
                builder.WithButton("Clear filter", Id(view.SessionId, "coll", 0), ButtonStyle.Secondary);
            }

            return builder.Build();
        }

        var page = view.PageIndex;
        var isFirst = page <= 0;
        var isLast = page >= view.TotalCount - 1;

        builder
            .WithButton("◀ Previous", Id(view.SessionId, "prev", page - 1), ButtonStyle.Primary, disabled: isFirst)
            .WithButton("Next ▶", Id(view.SessionId, "next", page + 1), ButtonStyle.Primary, disabled: isLast);

        builder
            .WithButton("Inspect", Id(view.SessionId, "inspect", page), ButtonStyle.Secondary, row: 1)
            .WithButton(
                view.CollectionFilter is { Length: > 0 } ? "Clear filter" : "Collection",
                Id(view.SessionId, "coll", page), ButtonStyle.Secondary, row: 1)
            .WithButton("Refresh", Id(view.SessionId, "refresh", page), ButtonStyle.Secondary, new Emoji("🔄"), row: 1);

        return builder.Build();
    }

    private static string Id(int sessionId, string action, int page) => $"{Prefix}:{sessionId}:{action}:{page}";
}
