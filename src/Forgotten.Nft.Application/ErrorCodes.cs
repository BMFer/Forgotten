namespace Forgotten.Nft.Application;

/// <summary>User-facing error codes (spec §17). Never expose internals/credentials.</summary>
public static class ErrorCodes
{
    public const string NftNotFound = "NFT_NOT_FOUND";
    public const string InvalidAssetAddress = "INVALID_ASSET_ADDRESS";
    public const string UnsupportedAsset = "UNSUPPORTED_ASSET";
    public const string CollectionNotVerified = "COLLECTION_NOT_VERIFIED";
    public const string CollectionNotApproved = "COLLECTION_NOT_APPROVED";
    public const string WalletNotConnected = "WALLET_NOT_CONNECTED";
    public const string BlockchainProviderUnavailable = "BLOCKCHAIN_PROVIDER_UNAVAILABLE";
    public const string MetadataUnavailable = "METADATA_UNAVAILABLE";
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
}
