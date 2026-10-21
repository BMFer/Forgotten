using System.Text;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.ValueObjects;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.Security;

namespace Forgotten.Nft.SecurityTests.Wallet;

/// <summary>A generated Ed25519 keypair plus its Base58 Solana address, for signing in tests.</summary>
internal sealed class TestKeypair
{
    private readonly byte[] _privateKey;

    private TestKeypair(byte[] privateKey, string address)
    {
        _privateKey = privateKey;
        Address = address;
    }

    public string Address { get; }

    public static TestKeypair Generate()
    {
        var random = new SecureRandom();
        var sk = new byte[Ed25519.SecretKeySize];
        random.NextBytes(sk);
        var pk = new byte[Ed25519.PublicKeySize];
        Ed25519.GeneratePublicKey(sk, 0, pk, 0);
        return new TestKeypair(sk, Base58.Encode(pk));
    }

    public byte[] Sign(string message)
    {
        var m = Encoding.UTF8.GetBytes(message);
        var sig = new byte[Ed25519.SignatureSize];
        Ed25519.Sign(_privateKey, 0, m, 0, m.Length, sig, 0);
        return sig;
    }
}

internal sealed class FakeWalletChallengeRepository : IWalletChallengeRepository
{
    public List<WalletVerificationChallenge> Items { get; } = new();

    public Task AddAsync(WalletVerificationChallenge challenge, CancellationToken cancellationToken)
    {
        if (challenge.Id == 0)
        {
            challenge.Id = Items.Count + 1;
        }

        Items.Add(challenge);
        return Task.CompletedTask;
    }

    public Task<WalletVerificationChallenge?> GetBySessionTokenHashAsync(string sessionTokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(c => c.SessionTokenHash == sessionTokenHash));

    public Task UpdateAsync(WalletVerificationChallenge challenge, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeConnectionRepository : IDiscordWalletConnectionRepository
{
    public List<DiscordWalletConnection> Items { get; } = new();

    public Task<IReadOnlyList<DiscordWalletConnection>> ListActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DiscordWalletConnection>>(
            Items.Where(c => c.DiscordUserId == discordUserId && c.DisconnectedAt == null).ToList());

    public Task<DiscordWalletConnection?> GetActiveByWalletAsync(string blockchain, string walletAddress, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(c =>
            c.Blockchain == blockchain && c.WalletAddress == walletAddress && c.DisconnectedAt == null));

    public Task<DiscordWalletConnection?> GetAsync(ulong discordUserId, string blockchain, string walletAddress, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(c =>
            c.DiscordUserId == discordUserId && c.Blockchain == blockchain && c.WalletAddress == walletAddress));

    public Task<int> CountActiveByUserAsync(ulong discordUserId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Count(c => c.DiscordUserId == discordUserId && c.DisconnectedAt == null));

    public Task UpsertAsync(DiscordWalletConnection connection, CancellationToken cancellationToken)
    {
        if (connection.Id == 0)
        {
            connection.Id = Items.Count + 1;
            Items.Add(connection);
        }
        else if (!Items.Contains(connection))
        {
            Items.Add(connection);
        }

        return Task.CompletedTask;
    }
}
