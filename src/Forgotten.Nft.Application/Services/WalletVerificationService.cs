using System.Security.Cryptography;
using System.Text;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.DTOs;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.Enums;
using Forgotten.Nft.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.Application.Services;

/// <summary>
/// Implements the cryptographic wallet-verification workflow (spec §7). Ownership is
/// proven by an Ed25519 signature over a server-built, single-use, time-boxed message
/// bound to the Discord account, wallet, nonce, and domain — an address alone never
/// grants a link (spec §7.1). The session token is stored only as a hash (spec §7.3).
/// </summary>
public sealed class WalletVerificationService : IWalletVerificationService
{
    private const string Blockchain = "solana";

    private readonly IWalletChallengeRepository _challenges;
    private readonly IDiscordWalletConnectionRepository _connections;
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly WalletVerificationOptions _options;
    private readonly ILogger<WalletVerificationService> _logger;

    public WalletVerificationService(
        IWalletChallengeRepository challenges,
        IDiscordWalletConnectionRepository connections,
        ISignatureVerifier signatureVerifier,
        IOptions<WalletVerificationOptions> options,
        ILogger<WalletVerificationService> logger)
    {
        _challenges = challenges;
        _connections = connections;
        _signatureVerifier = signatureVerifier;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WalletConnectSession> CreateSessionAsync(ulong discordUserId, CancellationToken cancellationToken)
    {
        var sessionToken = Base64Url(RandomNumberGenerator.GetBytes(32));
        var nonce = Base64Url(RandomNumberGenerator.GetBytes(24));
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddSeconds(_options.SessionTtlSeconds);

        await _challenges.AddAsync(
            new WalletVerificationChallenge
            {
                DiscordUserId = discordUserId,
                SessionTokenHash = Sha256Hex(sessionToken),
                Nonce = nonce,
                IssuedAt = now.UtcDateTime,
                ExpiresAt = expiresAt.UtcDateTime,
                CreatedAt = now.UtcDateTime,
                Status = WalletVerificationStatus.Pending,
            },
            cancellationToken).ConfigureAwait(false);

        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/verify/{sessionToken}";
        return new WalletConnectSession(url, expiresAt);
    }

    public async Task<WalletChallengeResult?> IssueChallengeAsync(
        string sessionToken, string walletAddress, CancellationToken cancellationToken)
    {
        if (!SolanaAddress.TryParse(walletAddress, out var address))
        {
            return null;
        }

        var challenge = await _challenges
            .GetBySessionTokenHashAsync(Sha256Hex(sessionToken), cancellationToken)
            .ConfigureAwait(false);

        if (challenge is null || challenge.Status != WalletVerificationStatus.Pending)
        {
            return null;
        }

        if (challenge.ExpiresAt <= DateTime.UtcNow)
        {
            challenge.Status = WalletVerificationStatus.Expired;
            await _challenges.UpdateAsync(challenge, cancellationToken).ConfigureAwait(false);
            return null;
        }

        // Bind the challenge to the wallet the member connected (spec §7.3).
        challenge.WalletAddress = address.Value;
        await _challenges.UpdateAsync(challenge, cancellationToken).ConfigureAwait(false);

        var message = WalletChallengeMessage.Build(
            _options.EffectiveDomain, challenge.DiscordUserId, address.Value, challenge.Nonce,
            challenge.IssuedAt, challenge.ExpiresAt);

        return new WalletChallengeResult(message, challenge.ExpiresAt);
    }

    public async Task<WalletVerificationOutcome> VerifyAsync(
        string sessionToken, string walletAddress, byte[] signature, CancellationToken cancellationToken)
    {
        if (!SolanaAddress.TryParse(walletAddress, out var address))
        {
            return WalletVerificationOutcome.Fail(ErrorCodes.WalletChallengeInvalid, "Invalid wallet address.");
        }

        var challenge = await _challenges
            .GetBySessionTokenHashAsync(Sha256Hex(sessionToken), cancellationToken)
            .ConfigureAwait(false);

        if (challenge is null)
        {
            return WalletVerificationOutcome.Fail(ErrorCodes.WalletChallengeInvalid, "Unknown or invalid verification session.");
        }

        // Single-use: a consumed/verified/failed challenge cannot be replayed (spec §7.3).
        if (challenge.Status is not WalletVerificationStatus.Pending)
        {
            return WalletVerificationOutcome.Fail(ErrorCodes.WalletChallengeInvalid, "This verification link has already been used.");
        }

        if (challenge.ExpiresAt <= DateTime.UtcNow)
        {
            challenge.Status = WalletVerificationStatus.Expired;
            await _challenges.UpdateAsync(challenge, cancellationToken).ConfigureAwait(false);
            return WalletVerificationOutcome.Fail(ErrorCodes.WalletChallengeExpired, "This verification link has expired. Start again with /wallet connect.");
        }

        // The signed wallet must match the one bound to the challenge (spec §7.3 wallet binding).
        if (challenge.WalletAddress is null ||
            !string.Equals(challenge.WalletAddress, address.Value, StringComparison.Ordinal))
        {
            return WalletVerificationOutcome.Fail(ErrorCodes.WalletSignatureInvalid, "The wallet does not match this verification session.");
        }

        var message = WalletChallengeMessage.Build(
            _options.EffectiveDomain, challenge.DiscordUserId, address.Value, challenge.Nonce,
            challenge.IssuedAt, challenge.ExpiresAt);

        if (!_signatureVerifier.VerifyEd25519(address.Value, message, signature))
        {
            challenge.Status = WalletVerificationStatus.Failed;
            await _challenges.UpdateAsync(challenge, cancellationToken).ConfigureAwait(false);
            return WalletVerificationOutcome.Fail(ErrorCodes.WalletSignatureInvalid, "The signature could not be verified.");
        }

        // Duplicate-claim policy: a wallet may belong to only one active identity (spec §7.4).
        if (_options.OneWalletPerIdentity)
        {
            var otherOwner = await _connections
                .GetActiveByWalletAsync(Blockchain, address.Value, cancellationToken)
                .ConfigureAwait(false);
            if (otherOwner is not null && otherOwner.DiscordUserId != challenge.DiscordUserId)
            {
                return WalletVerificationOutcome.Fail(ErrorCodes.WalletAlreadyLinked, "That wallet is already linked to another Discord account.");
            }
        }

        var existing = await _connections
            .GetAsync(challenge.DiscordUserId, Blockchain, address.Value, cancellationToken)
            .ConfigureAwait(false);
        var activeCount = await _connections
            .CountActiveByUserAsync(challenge.DiscordUserId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null && activeCount >= _options.MaximumLinkedWallets)
        {
            return WalletVerificationOutcome.Fail("WALLET_LIMIT_REACHED",
                $"You have reached the maximum of {_options.MaximumLinkedWallets} linked wallets.");
        }

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            await _connections.UpsertAsync(
                new DiscordWalletConnection
                {
                    DiscordUserId = challenge.DiscordUserId,
                    WalletAddress = address.Value,
                    Blockchain = Blockchain,
                    IsPrimary = activeCount == 0,
                    VerifiedAt = now,
                    LastVerifiedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Reconnecting required a fresh proof, which just succeeded (spec §7.4).
            existing.DisconnectedAt = null;
            existing.LastVerifiedAt = now;
            existing.UpdatedAt = now;
            await _connections.UpsertAsync(existing, cancellationToken).ConfigureAwait(false);
        }

        challenge.Status = WalletVerificationStatus.Verified;
        challenge.ConsumedAt = now;
        await _challenges.UpdateAsync(challenge, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Wallet verified for Discord user {UserId}.", challenge.DiscordUserId);
        return WalletVerificationOutcome.Ok(address.Value);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
