using Discord;
using Forgotten.Bot.Components;
using Forgotten.Nft.Application.DTOs;
using Forgotten.Nft.Application.Models;

namespace Forgotten.Nft.IntegrationTests;

public class GalleryComponentsTests
{
    private static GalleryView OkView(int page, int total) => new()
    {
        Status = GalleryViewStatus.Ok,
        SessionId = 7,
        OwnerUserId = 1,
        Item = new SolanaAsset { AssetAddress = "A", Provider = "fake", RetrievedAt = DateTimeOffset.UtcNow },
        PageIndex = page,
        TotalCount = total,
    };

    private static IEnumerable<ButtonComponent> Buttons(MessageComponent component) =>
        component.Components.OfType<ActionRowComponent>().SelectMany(r => r.Components).OfType<ButtonComponent>();

    [Fact]
    public void FirstPage_PreviousDisabled_NextEnabled()
    {
        var buttons = Buttons(GalleryComponents.Build(OkView(page: 0, total: 3))).ToList();

        var prev = buttons.Single(b => b.CustomId.Contains(":prev:"));
        var next = buttons.Single(b => b.CustomId.Contains(":next:"));

        Assert.True(prev.IsDisabled);
        Assert.False(next.IsDisabled);
        Assert.Equal("nftgal:7:next:1", next.CustomId);
    }

    [Fact]
    public void LastPage_NextDisabled()
    {
        var buttons = Buttons(GalleryComponents.Build(OkView(page: 2, total: 3))).ToList();

        Assert.True(buttons.Single(b => b.CustomId.Contains(":next:")).IsDisabled);
        Assert.False(buttons.Single(b => b.CustomId.Contains(":prev:")).IsDisabled);
    }

    [Fact]
    public void AllCustomIds_CarrySessionAndAreWithinDiscordLimit()
    {
        var buttons = Buttons(GalleryComponents.Build(OkView(page: 1, total: 5))).ToList();

        Assert.NotEmpty(buttons);
        Assert.All(buttons, b =>
        {
            Assert.StartsWith("nftgal:7:", b.CustomId);
            Assert.True(b.CustomId.Length <= 100, "custom_id must fit Discord's 100-char limit");
        });
    }

    [Fact]
    public void EmptyView_OffersRefreshOnly()
    {
        var view = new GalleryView { Status = GalleryViewStatus.Empty, SessionId = 7 };
        var buttons = Buttons(GalleryComponents.Build(view)).ToList();

        Assert.Single(buttons);
        Assert.Contains(":refresh:", buttons[0].CustomId);
    }
}
