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

    /// <summary>Custom-id prefix for the collection dropdown (<c>nftgalcol:{sessionId}</c>).</summary>
    public const string SelectPrefix = "nftgalcol";

    /// <summary>Dropdown value meaning "show every NFT in my wallet(s)" (never a real address).</summary>
    public const string AllValue = "all";

    public static MessageComponent Build(GalleryView view)
    {
        var builder = new ComponentBuilder();

        if (view.Status == GalleryViewStatus.Empty)
        {
            builder.WithButton("Refresh", Id(view.SessionId, "refresh", 0), ButtonStyle.Secondary, new Emoji("🔄"));
            AddCollectionSelect(builder, view, row: 1);
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
            .WithButton("Refresh", Id(view.SessionId, "refresh", page), ButtonStyle.Secondary, new Emoji("🔄"), row: 1);

        AddCollectionSelect(builder, view, row: 2);
        return builder.Build();
    }

    /// <summary>
    /// Adds the "flex a collection" dropdown: one option per collection the server has approved
    /// (the default marked), plus "All my NFTs". Omitted outside a guild / with no approved
    /// collections, so the gallery behaves as a plain wallet view there.
    /// </summary>
    private static void AddCollectionSelect(ComponentBuilder builder, GalleryView view, int row)
    {
        if (view.AvailableCollections.Count == 0)
        {
            return;
        }

        var current = view.CollectionFilter;
        var menu = new SelectMenuBuilder()
            .WithCustomId($"{SelectPrefix}:{view.SessionId}")
            .WithPlaceholder("Flex a collection")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var c in view.AvailableCollections.Take(24))
        {
            var label = c.IsDefault ? $"{Label(c.Label)} (server default)" : Label(c.Label);
            menu.AddOption(label, c.Address, isDefault: string.Equals(current, c.Address, StringComparison.Ordinal));
        }

        menu.AddOption("All my NFTs", AllValue, isDefault: string.IsNullOrEmpty(current));
        builder.WithSelectMenu(menu, row: row);
    }

    private static string Label(string label) => label.Length <= 80 ? label : label[..79] + "…";

    private static string Id(int sessionId, string action, int page) => $"{Prefix}:{sessionId}:{action}:{page}";
}
