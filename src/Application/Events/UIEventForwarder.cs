using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Darklands.Application.Common;
using Darklands.Application.Infrastructure.Debug;

namespace Darklands.Application.Events;

/// <summary>
/// Generic MediatR notification handler that forwards domain events to the UI Event Bus.
/// 
/// Architecture Role:
/// - Bridges the gap between MediatR's transient handler lifecycle and UI components
/// - Acts as the stable entry point for all domain events flowing to the UI
/// - Provides clean separation between domain events and UI event routing
/// 
/// Scalability:
/// - Generic implementation works for ANY INotification without code changes
/// - DI container automatically creates instances for each event type
/// - Supports 200+ event types with zero modification required
/// 
/// Registration:
/// - Auto-registered in DI as INotificationHandler{T} for all domain events
/// - MediatR scans and discovers this handler automatically
/// - No manual registration needed for new event types
/// </summary>
/// <typeparam name="TEvent">The domain event type (must implement INotification)</typeparam>
public sealed class UIEventForwarder<TEvent> : INotificationHandler<TEvent>
    where TEvent : INotification
{
    private readonly IUIEventBus _eventBus;
    private readonly ICategoryLogger _logger;

    /// <summary>
    /// Initializes a new UIEventForwarder for the specified event type.
    /// Called automatically by DI container when MediatR publishes events.
    /// </summary>
    /// <param name="eventBus">The UI event bus for forwarding events to subscribers</param>
    /// <param name="logger">Logger for debugging and monitoring event flow</param>
    public UIEventForwarder(IUIEventBus eventBus, ICategoryLogger logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Handles a domain event by forwarding it to all UI subscribers.
    /// Called automatically by MediatR when domain events are published.
    /// 
    /// Flow:
    /// 1. Domain layer publishes INotification via MediatR
    /// 2. MediatR routes to this handler (among others)
    /// 3. Handler forwards to UIEventBus for UI distribution
    /// 4. UIEventBus notifies all subscribers safely
    /// 
    /// Error Handling:
    /// - Does not throw exceptions (follows LanguageExt patterns)
    /// - Logs failures for monitoring without breaking event flow
    /// - UI failures don't impact other MediatR handlers
    /// </summary>
    /// <param name="notification">The domain event to forward to UI</param>
    /// <param name="cancellationToken">Cancellation token for async coordination</param>
    /// <returns>Completion task for MediatR pipeline coordination</returns>
    public async Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Log(LogLevel.Debug, LogCategory.Event, "[UIEventForwarder<{EventType}>] Forwarding event to UI subscribers: {Event}",
                typeof(TEvent).Name, notification);

            await _eventBus.PublishAsync(notification);

            _logger.Log(LogLevel.Debug, LogCategory.Event, "[UIEventForwarder<{EventType}>] Successfully forwarded event to UI",
                typeof(TEvent).Name);
        }
        catch (System.Exception ex)
        {
            // Log but don't rethrow - UI failures shouldn't break domain event processing
            _logger.Log(LogLevel.Error, LogCategory.Event, "[UIEventForwarder<{EventType}>] Failed to forward event to UI: {Event}. Exception: {Exception}",
                typeof(TEvent).Name, notification, ex);
        }
    }
}
