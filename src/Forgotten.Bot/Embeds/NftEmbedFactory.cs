using System.Text;
using Discord;
using Forgotten.Bot.Configuration;
using Forgotten.Nft.Application.Common;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DTOs;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Forgotten.Bot.Embeds;

/// <summary>
/// Builds Discord embeds for NFT inspection (spec §6.1). All metadata-derived text
/// is sanitized and length-capped before display (spec §8.2); callers must also set
/// <see cref="AllowedMentions.None"/> on the outgoing message. Collection "verified"
/// reflects on-chain grouping only — never name/image — and registry approval is
/// deliberately not shown until Phase 2 (spec §6.1, §13).
/// </summary>
public sealed class NftEmbedFactory
{
    private static readonly Color VerifiedColor = new(0x2E, 0xCC, 0x71);
    private static readonly Color NeutralColor = new(0x95, 0xA5, 0xA6);
    private static readonly Color WarnColor = new(0xE6, 0x7E, 0x22);

    private readonly PresentationOptions _presentation;
    private readonly SolanaOptions _solana;

    public NftEmbedFactory(IOptions<PresentationOptions> presentation, IOptions<SolanaOptions> solana)
    {
        _presentation = presentation.Value;
        _solana = solana.Value;
    }

    public Embed BuildInspectionEmbed(NftInspectionResult r)
    {
        var builder = new EmbedBuilder()
            .WithTitle(Truncate(Sanitize(r.Name) is { Length: > 0 } name ? name : "Unknown NFT", 256))
            .WithColor(r.Status == NftVerificationStatus.Verified ? VerifiedColor
                : r.Status == NftVerificationStatus.Burned ? WarnColor : NeutralColor)
            .WithFooter($"Provider: {r.Provider ?? "n/a"} • snapshot, not proof of current ownership")
            .WithCurrentTimestamp();

        var image = DisplayableImageUrl(r.ImageUrl);
        if (image is not null)
        {
            builder.WithImageUrl(image);
        }
        else if (!string.IsNullOrWhiteSpace(_presentation.PlaceholderImageUrl))
        {
            builder.WithImageUrl(_presentation.PlaceholderImageUrl);
        }

        if (!string.IsNullOrWhiteSpace(r.Symbol))
        {
            builder.AddField("Symbol", Truncate(Sanitize(r.Symbol)!, 64), inline: true);
        }

        builder.AddField("Status", StatusText(r.Status), inline: true);
        builder.AddField("Blockchain", "Solana", inline: true);

        if (r.CollectionAddress is not null)
        {
            builder.AddField(
                "Collection",
                $"{ShortenAddress(r.CollectionAddress)}\nOn-chain verified: {(r.CollectionVerified ? "YES" : "NO")}",
                inline: true);
        }

        if (r.OwnerAddress is not null)
        {
            builder.AddField("Owner", ShortenAddress(r.OwnerAddress), inline: true);
        }

        var flags = new List<string>();
        if (r.IsCompressed) flags.Add("Compressed");
        if (r.IsBurned) flags.Add("Burned");
        if (r.IsMutable == false) flags.Add("Immutable");
        if (!string.IsNullOrWhiteSpace(r.InterfaceType)) flags.Add(Sanitize(r.InterfaceType)!);
        if (flags.Count > 0)
        {
            builder.AddField("Type", Truncate(string.Join(" • ", flags), 1024), inline: true);
        }

        if (r.Attributes.Count > 0)
        {
            builder.AddField("Attributes", FormatAttributes(r), inline: false);
        }

        var links = BuildLinks(r);
        if (links.Length > 0)
        {
            builder.AddField("Links", links, inline: false);
        }

        return builder.Build();
    }

    public Embed BuildAttributesEmbed(NftInspectionResult r)
    {
        var builder = new EmbedBuilder()
            .WithTitle(Truncate($"Attributes — {(Sanitize(r.Name) ?? "NFT")}", 256))
            .WithColor(NeutralColor)
            .WithCurrentTimestamp();

        builder.WithDescription(r.Attributes.Count > 0
            ? Truncate(FormatAttributes(r), 4096)
            : "This NFT has no attributes.");

        return builder.Build();
    }

    public Embed BuildVerificationEmbed(NftVerificationResult r)
    {
        var color = r.VerificationStatus switch
        {
            NftVerificationStatus.Verified => VerifiedColor,
            NftVerificationStatus.Burned => WarnColor,
            _ => NeutralColor,
        };

        var builder = new EmbedBuilder()
            .WithTitle(Truncate(Sanitize(r.Name) is { Length: > 0 } name ? name : "NFT Verification", 256))
            .WithColor(color)
            .WithFooter($"Provider: {r.Provider ?? "n/a"} • level {(int)r.HighestLevel}/7 • snapshot, not proof of current ownership")
            .WithCurrentTimestamp();

        builder.AddField("Asset Exists", YesNo(r.AssetExists), inline: true);
        builder.AddField("Supported NFT", YesNo(r.IsSupportedNft), inline: true);
        builder.AddField("Collection Verified (on-chain)", YesNo(r.CollectionVerified), inline: true);
        builder.AddField(
            "Collection Approved (registry)",
            r.CollectionApproved
                ? $"YES{(Sanitize(r.CollectionName) is { Length: > 0 } cn ? $" — {Truncate(cn, 48)}" : string.Empty)}"
                : "NO",
            inline: true);
        builder.AddField("Current Owner", r.OwnerAddress is null ? "—" : ShortenAddress(r.OwnerAddress), inline: true);
        builder.AddField("Member Ownership", "Not evaluated (Phase 3)", inline: true);

        if (r.CollectionAddress is not null)
        {
            builder.AddField(
                "Collection",
                $"{ShortenAddress(r.CollectionAddress)} • [Explorer]({ExplorerAddressUrl(r.CollectionAddress)})",
                inline: false);
        }

        builder.AddField("Links", $"[View Asset]({ExplorerAddressUrl(r.AssetAddress)})", inline: false);
        return builder.Build();
    }

    public Embed BuildCollectionEmbed(string collectionAddress, PagedResult<SolanaAsset> page, NftCollection? registry)
    {
        var builder = new EmbedBuilder()
            .WithTitle(Truncate(Sanitize(registry?.DisplayName) is { Length: > 0 } dn ? dn : "Collection", 256))
            .WithColor(NeutralColor)
            .WithCurrentTimestamp();

        builder.AddField("Collection", ShortenAddress(collectionAddress), inline: true);
        builder.AddField("Approved (registry)", registry is { IsApproved: true } ? "YES" : "NO", inline: true);
        if (page.TotalItems is int total)
        {
            builder.AddField("Total Items", total.ToString(), inline: true);
        }

        var names = string.Join(
            "\n",
            page.Items.Take(15).Select(a => $"• {Truncate(Sanitize(a.Name) is { Length: > 0 } n ? n : a.AssetAddress, 80)}"));
        builder.AddField(
            $"Items (page {page.Page})",
            page.Items.Count == 0 ? "No items returned." : Truncate(names, 1024),
            inline: false);

        builder.AddField("Links", $"[View Collection]({ExplorerAddressUrl(collectionAddress)})", inline: false);
        return builder.Build();
    }

    public Embed BuildRegistryEmbed(IReadOnlyList<NftCollection> collections)
    {
        var builder = new EmbedBuilder()
            .WithTitle("Approved Collections")
            .WithColor(NeutralColor)
            .WithCurrentTimestamp();

        if (collections.Count == 0)
        {
            builder.WithDescription("No collections are registered for this server. Add one with `/nft-admin collection-add`.");
            return builder.Build();
        }

        var sb = new StringBuilder();
        foreach (var c in collections.Take(20))
        {
            var name = Truncate(Sanitize(c.DisplayName) is { Length: > 0 } dn ? dn : "(unnamed)", 48);
            sb.Append($"{(c.IsApproved ? "✅" : "⬜")} **{name}** — {ShortenAddress(c.CollectionAddress)}\n");
        }

        builder.WithDescription(Truncate(sb.ToString(), 4096));
        return builder.Build();
    }

    public Embed BuildVerificationLogEmbed(IReadOnlyList<NftVerificationLog> logs)
    {
        var builder = new EmbedBuilder()
            .WithTitle("Recent Verifications")
            .WithColor(NeutralColor)
            .WithCurrentTimestamp();

        if (logs.Count == 0)
        {
            builder.WithDescription("No verification activity recorded yet.");
            return builder.Build();
        }

        var sb = new StringBuilder();
        foreach (var l in logs.Take(15))
        {
            var error = l.ErrorCode is { Length: > 0 } code ? $" ({code})" : string.Empty;
            sb.Append($"`{l.CheckedAt:yyyy-MM-dd HH:mm}` {l.VerificationStatus} — {ShortenAddress(l.AssetAddress)}{error}\n");
        }

        builder.WithDescription(Truncate(sb.ToString(), 4096));
        return builder.Build();
    }

    public Embed BuildGalleryEmbed(GalleryView view)
    {
        if (view.Status == GalleryViewStatus.Empty)
        {
            var reason = view.CollectionFilter is { Length: > 0 }
                ? "No NFTs in your verified wallets match that collection filter."
                : "No NFTs found in your verified wallets.";
            return new EmbedBuilder()
                .WithTitle("FORGOTTEN NFT Gallery")
                .WithColor(NeutralColor)
                .WithDescription(reason)
                .Build();
        }

        var item = view.Item!;
        var builder = new EmbedBuilder()
            .WithTitle(Truncate(Sanitize(item.Name) is { Length: > 0 } name ? name : "Unknown NFT", 256))
            .WithColor(VerifiedColor)
            .WithFooter(BuildGalleryFooter(view))
            .WithCurrentTimestamp();

        var image = DisplayableImageUrl(item.ImageUrl);
        if (image is not null)
        {
            builder.WithImageUrl(image);
        }
        else if (!string.IsNullOrWhiteSpace(_presentation.PlaceholderImageUrl))
        {
            builder.WithImageUrl(_presentation.PlaceholderImageUrl);
        }

        if (item.CollectionAddress is not null)
        {
            builder.AddField("Collection", ShortenAddress(item.CollectionAddress), inline: true);
        }

        if (!string.IsNullOrWhiteSpace(item.Symbol))
        {
            builder.AddField("Symbol", Truncate(Sanitize(item.Symbol)!, 64), inline: true);
        }

        builder.AddField("Links", $"[View on Explorer]({ExplorerAddressUrl(item.AssetAddress)})", inline: false);
        return builder.Build();
    }

    private string BuildGalleryFooter(GalleryView view)
    {
        var footer = $"NFT {view.PageNumber} of {view.TotalCount}";
        if (view.CollectionFilter is { Length: > 0 })
        {
            footer += " • filtered";
        }

        if (view.Truncated)
        {
            footer += " • showing first " + view.TotalCount;
        }

        return footer;
    }

    private static string YesNo(bool value) => value ? "YES" : "NO";

    public Embed BuildErrorEmbed(NftInspectionResult r) =>
        new EmbedBuilder()
            .WithTitle("Could not inspect NFT")
            .WithColor(WarnColor)
            .WithDescription(Truncate(Sanitize(r.ErrorMessage) ?? "An error occurred.", 1024))
            .WithFooter(r.ErrorCode ?? "ERROR")
            .Build();

    public Embed BuildErrorEmbed(NftVerificationResult r) =>
        new EmbedBuilder()
            .WithTitle("Could not verify NFT")
            .WithColor(WarnColor)
            .WithDescription(Truncate(Sanitize(r.ErrorMessage) ?? "An error occurred.", 1024))
            .WithFooter(r.ErrorCode ?? "ERROR")
            .Build();

    private string FormatAttributes(NftInspectionResult r)
    {
        var sb = new StringBuilder();
        var shown = 0;
        foreach (var attr in r.Attributes)
        {
            if (shown >= 16)
            {
                sb.Append($"…and {r.Attributes.Count - shown} more");
                break;
            }

            var line = $"**{Truncate(Sanitize(attr.TraitType)!, 64)}:** {Truncate(Sanitize(attr.TraitValue)!, 96)}\n";
            if (sb.Length + line.Length > 1000)
            {
                sb.Append($"…and {r.Attributes.Count - shown} more");
                break;
            }

            sb.Append(line);
            shown++;
        }

        return sb.Length == 0 ? "—" : sb.ToString().TrimEnd();
    }

    private string BuildLinks(NftInspectionResult r)
    {
        var parts = new List<string>
        {
            $"[View on Explorer]({ExplorerAddressUrl(r.AssetAddress)})",
        };

        if (IsHttp(r.MetadataUri))
        {
            parts.Add($"[Metadata]({r.MetadataUri})");
        }

        if (IsHttp(r.ExternalUrl))
        {
            parts.Add($"[External]({r.ExternalUrl})");
        }

        return string.Join(" • ", parts);
    }

    private string ExplorerAddressUrl(string address)
    {
        var baseUrl = _presentation.ExplorerBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/address/{Uri.EscapeDataString(address)}";

        // explorer.solana.com defaults to mainnet-beta; only append a cluster otherwise.
        if (!string.IsNullOrWhiteSpace(_solana.Network) &&
            !_solana.Network.Equals("mainnet-beta", StringComparison.OrdinalIgnoreCase))
        {
            url += $"?cluster={Uri.EscapeDataString(_solana.Network)}";
        }

        return url;
    }

    private static string StatusText(NftVerificationStatus status) => status switch
    {
        NftVerificationStatus.Verified => "VERIFIED (supported NFT)",
        NftVerificationStatus.Burned => "BURNED",
        NftVerificationStatus.Unsupported => "UNSUPPORTED",
        NftVerificationStatus.NotFound => "NOT FOUND",
        NftVerificationStatus.ProviderUnavailable => "PROVIDER UNAVAILABLE",
        _ => status.ToString().ToUpperInvariant(),
    };

    private static string? DisplayableImageUrl(string? url) => IsHttp(url) ? url : null;

    private static bool IsHttp(string? url) =>
        !string.IsNullOrWhiteSpace(url) &&
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    private static string ShortenAddress(string address) =>
        address.Length <= 12 ? address : $"`{address[..4]}…{address[^4..]}`";

    /// <summary>
    /// Escapes Discord markdown in untrusted metadata (spec §8.2). Mentions are
    /// additionally neutralized at send time via <see cref="AllowedMentions.None"/>,
    /// and embed text never triggers mentions regardless.
    /// </summary>
    private static string? Sanitize(string? text) =>
        string.IsNullOrEmpty(text) ? text : Format.Sanitize(text);

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";
}
