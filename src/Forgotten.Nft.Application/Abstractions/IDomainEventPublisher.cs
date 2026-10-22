using Forgotten.Nft.Domain.Events;

namespace Forgotten.Nft.Application.Abstractions;

/// <summary>Publishes in-process domain events to all registered handlers (spec §10.3).</summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Handles domain events. Implementations should be idempotent where appropriate
/// (spec §10.3) and must not throw back into the publisher — a failing handler is
/// logged and isolated so it can't break event emission.
/// </summary>
public interface IDomainEventHandler
{
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
