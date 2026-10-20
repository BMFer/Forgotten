using System.Text;
using Discord;
using Forgotten.Bot.Configuration;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DTOs;
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

    public Embed BuildErrorEmbed(NftInspectionResult r) =>
        new EmbedBuilder()
            .WithTitle("Could not inspect NFT")
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
