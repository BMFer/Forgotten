using Forgotten.Nft.Domain.Enums;

namespace Forgotten.Nft.Domain.Entities;

/// <summary>Security audit record of a verification check (spec §9 "NftVerificationLogs", §16.14).</summary>
public class NftVerificationLog
{
    public int Id { get; set; }

    public ulong? DiscordUserId { get; set; }

    public ulong? GuildId { get; set; }

    public string AssetAddress { get; set; } = string.Empty;

    public NftVerificationType VerificationType { get; set; }

    public NftVerificationStatus VerificationStatus { get; set; }

    public string? Provider { get; set; }

    public DateTime CheckedAt { get; set; }

    public string? ErrorCode { get; set; }
}
