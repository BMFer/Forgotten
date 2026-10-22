namespace Forgotten.Nft.Domain.Entities;

/// <summary>
/// A recorded ownership observation for an NFT (spec §9 "NftOwnershipHistory").
/// Distinguishes observed state changes from confirmed transfers (spec §9.2, §10.2):
/// monitor-observed rows carry a null <see cref="TransactionSignature"/> — a transfer
/// signature is never fabricated from polling.
/// </summary>
public class NftOwnershipHistory
{
    public const string ObservedSource = "monitor-observed";

    public int Id { get; set; }

    public int NftAssetId { get; set; }

    public string? PreviousOwnerAddress { get; set; }

    public string? NewOwnerAddress { get; set; }

    public DateTime ObservedAt { get; set; }

    /// <summary>Origin of the observation, e.g. "monitor-observed". Not proof of a transfer.</summary>
    public string Source { get; set; } = ObservedSource;

    /// <summary>Only set when a confirmed on-chain transfer is known; null for polled observations.</summary>
    public string? TransactionSignature { get; set; }

    public NftAsset? NftAsset { get; set; }
}
