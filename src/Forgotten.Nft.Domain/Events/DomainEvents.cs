namespace Forgotten.Nft.Domain.Events;

/// <summary>Marker for in-process domain events (spec §10.3).</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Observed ownership change (spec §10.3). "Observed" means a snapshot comparison —
/// not a confirmed transfer; an exact transaction is never inferred (spec §10.2).
/// </summary>
public sealed record NftOwnershipChangedEvent(
    string AssetAddress, string? PreviousOwner, string? CurrentOwner, DateTimeOffset ObservedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => ObservedAt;
}

public sealed record NftVerifiedEvent(string AssetAddress, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record NftCollectionApprovedEvent(ulong GuildId, string CollectionAddress, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record WalletConnectedEvent(ulong DiscordUserId, string WalletAddress, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record WalletDisconnectedEvent(ulong DiscordUserId, string WalletAddress, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record NftEligibilityChangedEvent(
    ulong DiscordUserId, string AssetAddress, string ApplicationId, bool Eligible, DateTimeOffset OccurredAt) : IDomainEvent;
