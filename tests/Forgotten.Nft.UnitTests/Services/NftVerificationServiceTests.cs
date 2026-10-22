using Forgotten.Nft.Application;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Application.Services;
using Forgotten.Nft.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forgotten.Nft.UnitTests.Services;

public class NftVerificationServiceTests
{
    private const ulong Guild = 111UL;
    private const string Asset = "So11111111111111111111111111111111111111112";
    private const string Collection = "11111111111111111111111111111111";

    private static SolanaAsset Nft(bool collectionVerified, bool supported = true, bool burned = false) => new()
    {
        AssetAddress = Asset,
        InterfaceType = supported ? "V1_NFT" : "FungibleToken",
        IsSupportedNft = supported,
        IsBurned = burned,
        Name = "INFERNA",
        OwnerAddress = Collection,
        CollectionAddress = Collection,
        CollectionVerified = collectionVerified,
        Provider = "fake",
        RetrievedAt = DateTimeOffset.UtcNow,
    };

    private static (NftVerificationService svc, FakeVerificationLogRepository log, FakeCollectionRepository repo) Build(
        SolanaAsset? asset, bool providerThrows = false)
    {
        var repo = new FakeCollectionRepository();
        var registry = new CollectionRegistryService(repo, new NullDomainEventPublisher());
        var log = new FakeVerificationLogRepository();
        var svc = new NftVerificationService(
            new FakeAssetProvider(asset, providerThrows),
            registry,
            log,
            NullLogger<NftVerificationService>.Instance);
        return (svc, log, repo);
    }

    [Fact]
    public async Task UnverifiedUnapproved_BothNo()
    {
        var (svc, _, _) = Build(Nft(collectionVerified: false));

        var r = await svc.VerifyAsync(Guild, Asset);

        Assert.True(r.AssetExists);
        Assert.True(r.IsSupportedNft);
        Assert.False(r.CollectionVerified);
        Assert.False(r.CollectionApproved);
        Assert.Equal(NftVerificationLevel.NftClassified, r.HighestLevel);
    }

    [Fact]
    public async Task OnChainVerified_NotApproved_AreIndependent()
    {
        var (svc, _, _) = Build(Nft(collectionVerified: true));

        var r = await svc.VerifyAsync(Guild, Asset);

        Assert.True(r.CollectionVerified);
        Assert.False(r.CollectionApproved);
        Assert.Equal(NftVerificationLevel.CollectionVerified, r.HighestLevel);
    }

    [Fact]
    public async Task Approved_NotOnChainVerified_AreIndependent()
    {
        var (svc, _, repo) = Build(Nft(collectionVerified: false));
        await new CollectionRegistryService(repo, new NullDomainEventPublisher()).ApproveAsync(Guild, Collection, "DMC", 7UL, default);

        var r = await svc.VerifyAsync(Guild, Asset);

        Assert.False(r.CollectionVerified);
        Assert.True(r.CollectionApproved);
        Assert.Equal("DMC", r.CollectionName);
        Assert.Equal(NftVerificationLevel.CollectionApproved, r.HighestLevel);
    }

    [Fact]
    public async Task Approval_IsGuildScoped()
    {
        var (svc, _, repo) = Build(Nft(collectionVerified: false));
        await new CollectionRegistryService(repo, new NullDomainEventPublisher()).ApproveAsync(Guild, Collection, "DMC", 7UL, default);

        var otherGuild = await svc.VerifyAsync(guildId: 999UL, Asset);

        Assert.False(otherGuild.CollectionApproved);
    }

    [Fact]
    public async Task Fungible_IsNeverCollectionVerified()
    {
        var (svc, _, _) = Build(Nft(collectionVerified: true, supported: false));

        var r = await svc.VerifyAsync(Guild, Asset);

        Assert.False(r.IsSupportedNft);
        Assert.False(r.CollectionVerified);
        Assert.Equal(NftVerificationStatus.Unsupported, r.VerificationStatus);
    }

    [Fact]
    public async Task ProviderFailure_IsProviderUnavailable_NotNotFound()
    {
        var (svc, _, _) = Build(asset: null, providerThrows: true);

        var r = await svc.VerifyAsync(Guild, Asset);

        Assert.Equal(NftVerificationStatus.ProviderUnavailable, r.VerificationStatus);
        Assert.Equal(ErrorCodes.BlockchainProviderUnavailable, r.ErrorCode);
    }

    [Fact]
    public async Task MissingAsset_IsNotFound()
    {
        var (svc, _, _) = Build(asset: null);

        var r = await svc.VerifyAsync(Guild, Asset);

        Assert.Equal(NftVerificationStatus.NotFound, r.VerificationStatus);
        Assert.False(r.AssetExists);
    }

    [Fact]
    public async Task InvalidAddress_SkipsProviderAndReportsInvalid()
    {
        var (svc, _, _) = Build(Nft(collectionVerified: true));

        var r = await svc.VerifyAsync(Guild, "not-valid");

        Assert.Equal(ErrorCodes.InvalidAssetAddress, r.ErrorCode);
    }

    [Fact]
    public async Task EveryVerification_WritesAnAuditLog()
    {
        var (svc, log, _) = Build(Nft(collectionVerified: true));

        await svc.VerifyAsync(Guild, Asset, requestingUserId: 42UL);

        var entry = Assert.Single(log.Entries);
        Assert.Equal(NftVerificationType.Verify, entry.VerificationType);
        Assert.Equal(42UL, entry.DiscordUserId);
        Assert.Equal(Guild, entry.GuildId);
    }
}
