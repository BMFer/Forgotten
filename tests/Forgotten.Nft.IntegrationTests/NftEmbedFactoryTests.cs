using Forgotten.Bot.Configuration;
using Forgotten.Bot.Embeds;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DTOs;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.IntegrationTests;

public class NftEmbedFactoryTests
{
    private static NftEmbedFactory CreateFactory() => new(
        Options.Create(new PresentationOptions { ExplorerBaseUrl = "https://explorer.solana.com" }),
        Options.Create(new SolanaOptions { Network = "mainnet-beta" }));

    private static NftInspectionResult VerifiedResult(string name, string description, params NftAttributeData[] attributes) => new()
    {
        AssetAddress = "So11111111111111111111111111111111111111112",
        Status = NftVerificationStatus.Verified,
        AssetExists = true,
        IsSupportedNft = true,
        Name = name,
        Description = description,
        ImageUrl = "https://cdn.example/art.png",
        Attributes = attributes,
        Provider = "das",
        RetrievedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void InspectionEmbed_EscapesMarkdownInUntrustedText()
    {
        var factory = CreateFactory();
        var result = VerifiedResult("**rug** `exploit`", "~~scam~~ _text_");

        var embed = factory.BuildInspectionEmbed(result);

        // Discord.Net Format.Sanitize backslash-escapes markdown control characters.
        Assert.Contains('\\', embed.Title);
        Assert.DoesNotContain("**rug**", embed.Title);
    }

    [Fact]
    public void InspectionEmbed_IncludesExplorerLink()
    {
        var factory = CreateFactory();
        var embed = factory.BuildInspectionEmbed(VerifiedResult("Art", "desc"));

        var links = Assert.Single(embed.Fields, f => f.Name == "Links");
        Assert.Contains("explorer.solana.com/address/", links.Value);
    }

    [Fact]
    public void InspectionEmbed_UsesHttpArtworkImage()
    {
        var factory = CreateFactory();
        var embed = factory.BuildInspectionEmbed(VerifiedResult("Art", "desc"));

        Assert.Equal("https://cdn.example/art.png", embed.Image?.Url);
    }

    [Fact]
    public void InspectionEmbed_LongAttributeListIsTruncated()
    {
        var factory = CreateFactory();
        var many = Enumerable.Range(0, 50)
            .Select(i => new NftAttributeData($"Trait{i}", $"Value{i}"))
            .ToArray();

        var embed = factory.BuildInspectionEmbed(VerifiedResult("Art", "desc", many));

        var attributes = Assert.Single(embed.Fields, f => f.Name == "Attributes");
        Assert.True(attributes.Value.Length <= 1024, "Attribute field must respect Discord's 1024-char limit.");
        Assert.Contains("more", attributes.Value);
    }
}
