using Forgotten.Nft.Application.Abstractions;
using Forgotten.Nft.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Forgotten.Nft.Application.Services;

/// <summary>
/// In-process domain event publisher (spec §10.3). Logs each event and dispatches
/// to every registered handler; a handler fault is isolated and logged so it cannot
/// break emission. Tolerates having no handlers.
/// </summary>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IEnumerable<IDomainEventHandler> _handlers;
    private readonly ILogger<DomainEventPublisher> _logger;

    public DomainEventPublisher(IEnumerable<IDomainEventHandler> handlers, ILogger<DomainEventPublisher> logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain event {Event} at {OccurredAt:o}", domainEvent.GetType().Name, domainEvent.OccurredAt);

        foreach (var handler in _handlers)
        {
            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {Handler} failed for {Event}.", handler.GetType().Name, domainEvent.GetType().Name);
            }
        }
    }
}
