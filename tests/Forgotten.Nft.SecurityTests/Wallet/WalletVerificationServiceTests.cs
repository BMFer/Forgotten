using Forgotten.Nft.Application;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.Services;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.SecurityTests.Wallet;

/// <summary>
/// End-to-end wallet-verification security tests using a real Ed25519 keypair and
/// the real signature verifier (spec §7, §18 "Wallet Verification"). These prove
/// that only a genuine signature over the exact server-built, single-use, bound
/// message creates a link — never an address alone.
/// </summary>
public class WalletVerificationServiceTests
{
    private const ulong UserA = 100UL;
    private const ulong UserB = 999UL;

    private static (WalletVerificationService svc, FakeWalletChallengeRepository ch, FakeConnectionRepository conn) Build(
        bool oneWalletPerIdentity = true, int maxWallets = 5)
    {
        var ch = new FakeWalletChallengeRepository();
        var conn = new FakeConnectionRepository();
        var options = Options.Create(new WalletVerificationOptions
        {
            PublicBaseUrl = "https://localhost",
            Domain = "localhost",
            SessionTtlSeconds = 900,
            MaximumLinkedWallets = maxWallets,
            OneWalletPerIdentity = oneWalletPerIdentity,
        });

        var svc = new WalletVerificationService(
            ch, conn, new Ed25519SignatureVerifier(), new NullDomainEventPublisher(), options, NullLogger<WalletVerificationService>.Instance);
        return (svc, ch, conn);
    }

    private static string TokenFrom(string url) => url[(url.LastIndexOf('/') + 1)..];

    [Fact]
    public async Task ValidSignature_LinksWallet()
    {
        var (svc, _, conn) = Build();
        var kp = TestKeypair.Generate();

        var session = await svc.CreateSessionAsync(UserA, default);
        var token = TokenFrom(session.Url);
        var challenge = await svc.IssueChallengeAsync(token, kp.Address, default);

        Assert.NotNull(challenge);
        Assert.Contains(kp.Address, challenge!.Message);
        Assert.Contains("100", challenge.Message);     // Discord user id binding
        Assert.Contains("localhost", challenge.Message); // domain binding

        var outcome = await svc.VerifyAsync(token, kp.Address, kp.Sign(challenge.Message), default);

        Assert.True(outcome.Success);
        var connection = Assert.Single(conn.Items);
        Assert.Equal(UserA, connection.DiscordUserId);
        Assert.True(connection.IsPrimary);
    }

    [Fact]
    public async Task InvalidSignature_Rejected_AndNoLink()
    {
        var (svc, _, conn) = Build();
        var kp = TestKeypair.Generate();
        var session = await svc.CreateSessionAsync(UserA, default);
        var token = TokenFrom(session.Url);
        var challenge = await svc.IssueChallengeAsync(token, kp.Address, default);

        // Sign a different message than the one bound to the challenge.
        var outcome = await svc.VerifyAsync(token, kp.Address, kp.Sign(challenge!.Message + "tampered"), default);

        Assert.False(outcome.Success);
        Assert.Equal(ErrorCodes.WalletSignatureInvalid, outcome.ErrorCode);
        Assert.Empty(conn.Items);
    }

    [Fact]
    public async Task ExpiredChallenge_Rejected()
    {
        var (svc, ch, _) = Build();
        var kp = TestKeypair.Generate();
        var session = await svc.CreateSessionAsync(UserA, default);
        var token = TokenFrom(session.Url);
        var challenge = await svc.IssueChallengeAsync(token, kp.Address, default);
        var signature = kp.Sign(challenge!.Message);

        // Force expiry.
        ch.Items[0].ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        var outcome = await svc.VerifyAsync(token, kp.Address, signature, default);

        Assert.False(outcome.Success);
        Assert.Equal(ErrorCodes.WalletChallengeExpired, outcome.ErrorCode);
    }

    [Fact]
    public async Task ReusedChallenge_Rejected()
    {
        var (svc, _, _) = Build();
        var kp = TestKeypair.Generate();
        var session = await svc.CreateSessionAsync(UserA, default);
        var token = TokenFrom(session.Url);
        var challenge = await svc.IssueChallengeAsync(token, kp.Address, default);
        var signature = kp.Sign(challenge!.Message);

        var first = await svc.VerifyAsync(token, kp.Address, signature, default);
        var second = await svc.VerifyAsync(token, kp.Address, signature, default);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal(ErrorCodes.WalletChallengeInvalid, second.ErrorCode);
    }

    [Fact]
    public async Task WrongWallet_Rejected_DueToBinding()
    {
        var (svc, _, _) = Build();
        var bound = TestKeypair.Generate();
        var other = TestKeypair.Generate();
        var session = await svc.CreateSessionAsync(UserA, default);
        var token = TokenFrom(session.Url);
        await svc.IssueChallengeAsync(token, bound.Address, default); // challenge bound to `bound`

        // Submit a different wallet.
        var outcome = await svc.VerifyAsync(token, other.Address, other.Sign("anything"), default);

        Assert.False(outcome.Success);
        Assert.Equal(ErrorCodes.WalletSignatureInvalid, outcome.ErrorCode);
    }

    [Fact]
    public async Task UnknownSessionToken_Rejected()
    {
        var (svc, _, _) = Build();
        var kp = TestKeypair.Generate();

        var outcome = await svc.VerifyAsync("not-a-real-token", kp.Address, kp.Sign("m"), default);

        Assert.False(outcome.Success);
        Assert.Equal(ErrorCodes.WalletChallengeInvalid, outcome.ErrorCode);
    }

    [Fact]
    public async Task DuplicateWalletClaim_Rejected()
    {
        var (svc, _, conn) = Build(oneWalletPerIdentity: true);
        var kp = TestKeypair.Generate();

        // Another identity already actively owns this wallet.
        conn.Items.Add(new DiscordWalletConnection
        {
            Id = 1,
            DiscordUserId = UserB,
            WalletAddress = kp.Address,
            Blockchain = "solana",
            IsPrimary = true,
            VerifiedAt = DateTime.UtcNow,
            LastVerifiedAt = DateTime.UtcNow,
        });

        var session = await svc.CreateSessionAsync(UserA, default);
        var token = TokenFrom(session.Url);
        var challenge = await svc.IssueChallengeAsync(token, kp.Address, default);
        var outcome = await svc.VerifyAsync(token, kp.Address, kp.Sign(challenge!.Message), default);

        Assert.False(outcome.Success);
        Assert.Equal(ErrorCodes.WalletAlreadyLinked, outcome.ErrorCode);
    }
}
