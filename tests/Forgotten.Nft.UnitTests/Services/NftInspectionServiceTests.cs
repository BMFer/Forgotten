using Forgotten.Nft.Application;
using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Application.Common;
using Forgotten.Nft.Application.Configuration;
using Forgotten.Nft.Application.Models;
using Forgotten.Nft.Application.Services;
using Forgotten.Nft.Domain.Entities;
using Forgotten.Nft.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forgotten.Nft.UnitTests.Services;

public class NftInspectionServiceTests
{
    private const string ValidAddress = "So11111111111111111111111111111111111111112";

    private static SolanaAsset SupportedAsset(bool burned = false, bool compressed = false) => new()
    {
        AssetAddress = ValidAddress,
        InterfaceType = "V1_NFT",
        IsSupportedNft = true,
        IsCompressed = compressed,
        IsBurned = burned,
        Name = "INFERNA",
        Description = "A fire warrior.",
        ImageUrl = "https://cdn.example/inferna.png",
        OwnerAddress = "11111111111111111111111111111111",
        CollectionAddress = "11111111111111111111111111111111",
        CollectionVerified = true,
        Attributes = new[] { new NftAttributeData("Class", "Fire Warrior") },
        Provider = "fake",
        RetrievedAt = DateTimeOffset.UtcNow,
    };

    private static NftInspectionService CreateService(
        FakeProvider provider,
        FakeResolver? resolver = null,
        FakeRepository? repository = null)
    {
        return new NftInspectionService(
            provider,
            resolver ?? new FakeResolver(),
            repository ?? new FakeRepository(),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new SolanaOptions { CacheDurationSeconds = 300 }),
            NullLogger<NftInspectionService>.Instance);
    }

    [Fact]
    public async Task ValidNft_ReturnsVerifiedWithFields()
    {
        var service = CreateService(new FakeProvider(SupportedAsset()));

        var result = await service.InspectAsync(ValidAddress);

        Assert.Equal(NftVerificationStatus.Verified, result.Status);
        Assert.True(result.AssetExists);
        Assert.True(result.IsSupportedNft);
        Assert.Equal("INFERNA", result.Name);
        Assert.Single(result.Attributes);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task InvalidAddress_ReturnsInvalidAndSkipsProvider()
    {
        var provider = new FakeProvider(SupportedAsset());
        var service = CreateService(provider);

        var result = await service.InspectAsync("not-valid");

        Assert.Equal(ErrorCodes.InvalidAssetAddress, result.ErrorCode);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task MissingAsset_ReturnsNotFound()
    {
        var service = CreateService(new FakeProvider(asset: null));

        var result = await service.InspectAsync(ValidAddress);

        Assert.Equal(NftVerificationStatus.NotFound, result.Status);
        Assert.Equal(ErrorCodes.NftNotFound, result.ErrorCode);
        Assert.False(result.AssetExists);
    }

    [Fact]
    public async Task BurnedAsset_ReturnsBurned()
    {
        var service = CreateService(new FakeProvider(SupportedAsset(burned: true)));

        var result = await service.InspectAsync(ValidAddress);

        Assert.Equal(NftVerificationStatus.Burned, result.Status);
        Assert.True(result.IsBurned);
    }

    [Fact]
    public async Task CompressedNft_IsClassifiedSupported()
    {
        var service = CreateService(new FakeProvider(SupportedAsset(compressed: true)));

        var result = await service.InspectAsync(ValidAddress);

        Assert.Equal(NftVerificationStatus.Verified, result.Status);
        Assert.True(result.IsCompressed);
    }

    [Fact]
    public async Task FungibleToken_ReturnsUnsupported()
    {
        var fungible = new SolanaAsset
        {
            AssetAddress = ValidAddress,
            InterfaceType = "FungibleToken",
            IsSupportedNft = false,
            Provider = "fake",
            RetrievedAt = DateTimeOffset.UtcNow,
        };
        var service = CreateService(new FakeProvider(fungible));

        var result = await service.InspectAsync(ValidAddress);

        Assert.Equal(NftVerificationStatus.Unsupported, result.Status);
        Assert.Equal(ErrorCodes.UnsupportedAsset, result.ErrorCode);
        Assert.True(result.AssetExists);
        Assert.False(result.IsSupportedNft);
    }

    [Fact]
    public async Task ProviderThrows_ReturnsProviderUnavailableNotNotFound()
    {
        var service = CreateService(new FakeProvider(throwError: true));

        var result = await service.InspectAsync(ValidAddress);

        Assert.Equal(NftVerificationStatus.ProviderUnavailable, result.Status);
        Assert.Equal(ErrorCodes.BlockchainProviderUnavailable, result.ErrorCode);
        Assert.NotEqual(NftVerificationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task SecondCall_IsServedFromCache_UnlessRefreshed()
    {
        var provider = new FakeProvider(SupportedAsset());
        var service = CreateService(provider);

        var first = await service.InspectAsync(ValidAddress);
        var second = await service.InspectAsync(ValidAddress);
        var refreshed = await service.InspectAsync(ValidAddress, refresh: true);

        Assert.False(first.ServedFromCache);
        Assert.True(second.ServedFromCache);
        Assert.False(refreshed.ServedFromCache);
        Assert.Equal(2, provider.CallCount); // first + refresh only
    }

    [Fact]
    public async Task MetadataResolver_FillsMissingAttributes()
    {
        var sparse = new SolanaAsset
        {
            AssetAddress = ValidAddress,
            InterfaceType = "V1_NFT",
            IsSupportedNft = true,
            Name = "Sparse",
            MetadataUri = "https://arweave.net/meta.json",
            Provider = "fake",
            RetrievedAt = DateTimeOffset.UtcNow,
        };
        var resolver = new FakeResolver(new ResolvedMetadata
        {
            Description = "from metadata",
            Image = "https://cdn.example/x.png",
            Attributes = new[] { new NftAttributeData("Rarity", "Legendary") },
        });
        var service = CreateService(new FakeProvider(sparse), resolver);

        var result = await service.InspectAsync(ValidAddress);

        Assert.Equal("from metadata", result.Description);
        Assert.Contains(result.Attributes, a => a.TraitType == "Rarity");
    }

    private sealed class FakeProvider : ISolanaAssetProvider
    {
        private readonly SolanaAsset? _asset;
        private readonly bool _throw;

        public FakeProvider(SolanaAsset? asset = null, bool throwError = false)
        {
            _asset = asset;
            _throw = throwError;
        }

        public int CallCount { get; private set; }

        public string Name => "fake";

        public Task<SolanaAsset?> GetAssetAsync(string assetAddress, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_throw)
            {
                throw new HttpRequestException("simulated timeout");
            }

            return Task.FromResult(_asset);
        }

        public Task<PagedResult<SolanaAsset>> GetAssetsByOwnerAsync(
            string walletAddress, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PagedResult<SolanaAsset>> GetAssetsByCollectionAsync(
            string collectionAddress, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeResolver : IMetadataResolver
    {
        private readonly ResolvedMetadata? _metadata;

        public FakeResolver(ResolvedMetadata? metadata = null) => _metadata = metadata;

        public Task<ResolvedMetadata?> ResolveAsync(string metadataUri, CancellationToken cancellationToken) =>
            Task.FromResult(_metadata);
    }

    private sealed class FakeRepository : INftAssetRepository
    {
        public Task<NftAsset?> GetByAddressAsync(string blockchain, string assetAddress, CancellationToken cancellationToken) =>
            Task.FromResult<NftAsset?>(null);

        public Task UpsertAsync(NftAsset asset, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
