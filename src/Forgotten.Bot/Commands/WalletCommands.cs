using Discord;
using Discord.Interactions;
using Forgotten.Bot.Services;
using Forgotten.Nft.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Forgotten.Bot.Commands;

/// <summary>
/// Wallet connection commands (spec §6.5, §7). Ownership is proven cryptographically
/// via the verification website — these commands never accept a signature or key
/// directly. All responses are ephemeral because they expose account-linking details
/// (spec §12, §16.13).
/// </summary>
[Group("wallet", "Connect and manage your Solana wallets")]
public sealed class WalletCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IWalletVerificationService _verification;
    private readonly IWalletConnectionService _connections;
    private readonly CommandRateLimiter _rateLimiter;
    private readonly ILogger<WalletCommands> _logger;

    public WalletCommands(
        IWalletVerificationService verification,
        IWalletConnectionService connections,
        CommandRateLimiter rateLimiter,
        ILogger<WalletCommands> logger)
    {
        _verification = verification;
        _connections = connections;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    [SlashCommand("connect", "Securely connect and verify a Solana wallet.")]
    public async Task ConnectAsync()
    {
        if (!_rateLimiter.TryAcquire(Context.User.Id, "wallet-connect", out var retryAfter))
        {
            await RespondAsync($"⏳ Slow down — try again in {Math.Ceiling(retryAfter.TotalSeconds)}s.", ephemeral: true)
                .ConfigureAwait(false);
            return;
        }

        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        var session = await _verification.CreateSessionAsync(Context.User.Id, CancellationToken.None).ConfigureAwait(false);
        var expires = session.ExpiresAt.ToUnixTimeSeconds();

        var embed = new EmbedBuilder()
            .WithTitle("🔐 Connect your Solana wallet")
            .WithColor(new Color(0x5865F2))
            .WithDescription(
                $"Open your **private** verification link and sign the message to prove you control the wallet.\n\n" +
                $"**[→ Open verification page]({session.Url})**\n\n" +
                $"This link is for you only and expires <t:{expires}:R>. You will sign a message — " +
                "no transaction, no funds moved, and your keys are never shared.")
            .Build();

        await FollowupAsync(embed: embed, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("verify", "Check whether you have a verified wallet.")]
    public async Task VerifyAsync()
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var wallets = await _connections.ListAsync(Context.User.Id, CancellationToken.None).ConfigureAwait(false);

        var message = wallets.Count > 0
            ? $"✅ You have **{wallets.Count}** verified wallet(s). Use `/wallet list` to see them."
            : "You have no verified wallets yet. Use `/wallet connect` to link one.";
        await FollowupAsync(message, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("status", "Show your wallet verification status.")]
    public async Task StatusAsync()
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var wallets = await _connections.ListAsync(Context.User.Id, CancellationToken.None).ConfigureAwait(false);

        if (wallets.Count == 0)
        {
            await FollowupAsync("No wallets connected. Use `/wallet connect` to link one.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var primary = wallets.FirstOrDefault(w => w.IsPrimary);
        var text = $"**Verified wallets:** {wallets.Count}\n" +
                   $"**Primary:** {(primary is null ? "none set" : Shorten(primary.WalletAddress))}";
        await FollowupAsync(text, ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("list", "List your connected wallets.")]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var wallets = await _connections.ListAsync(Context.User.Id, CancellationToken.None).ConfigureAwait(false);

        var embed = new EmbedBuilder()
            .WithTitle("Your connected wallets")
            .WithColor(new Color(0x95A5A6));

        if (wallets.Count == 0)
        {
            embed.WithDescription("No wallets connected. Use `/wallet connect` to link one.");
        }
        else
        {
            var lines = wallets.Select(w =>
                $"{(w.IsPrimary ? "⭐" : "•")} `{w.WalletAddress}` — verified <t:{new DateTimeOffset(w.VerifiedAt, TimeSpan.Zero).ToUnixTimeSeconds()}:R>");
            embed.WithDescription(string.Join("\n", lines));
        }

        await FollowupAsync(embed: embed.Build(), ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("disconnect", "Disconnect one of your wallets.")]
    public async Task DisconnectAsync([Summary("address", "The wallet address to disconnect")] string address)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var removed = await _connections.DisconnectAsync(Context.User.Id, address, CancellationToken.None).ConfigureAwait(false);
        await FollowupAsync(
            removed ? $"🗑️ Disconnected `{address}`." : "That wallet isn't connected to your account.",
            ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("primary", "Set which of your wallets is primary.")]
    public async Task PrimaryAsync([Summary("address", "The wallet address to make primary")] string address)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var ok = await _connections.SetPrimaryAsync(Context.User.Id, address, CancellationToken.None).ConfigureAwait(false);
        await FollowupAsync(
            ok ? $"⭐ `{address}` is now your primary wallet." : "That wallet isn't connected to your account.",
            ephemeral: true).ConfigureAwait(false);
    }

    private static string Shorten(string address) =>
        address.Length <= 12 ? address : $"{address[..4]}…{address[^4..]}";
}
