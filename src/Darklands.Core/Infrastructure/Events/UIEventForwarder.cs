using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Infrastructure.Events;

/// <summary>
/// Generic bridge that forwards MediatR notifications to GodotEventBus.
///
/// ARCHITECTURE (ADR-002):
/// - Registered via open generics: AddTransient(typeof(INotificationHandler&lt;&gt;), typeof(UIEventForwarder&lt;&gt;))
/// - MediatR auto-resolves UIEventForwarder&lt;TEvent&gt; for any INotification
/// - Zero boilerplate - no manual registration per event type
///
/// FLOW:
/// 1. CommandHandler calls: await _mediator.Publish(new HealthChangedEvent(...))
/// 2. MediatR resolves: INotificationHandler&lt;HealthChangedEvent&gt; → UIEventForwarder&lt;HealthChangedEvent&gt;
/// 3. UIEventForwarder forwards to: IGodotEventBus.PublishAsync()
/// 4. GodotEventBus notifies all Godot node subscribers (on main thread)
///
/// TESTING:
/// - Type resolution test verifies MediatR creates correct generic instance
/// - Integration test verifies full flow: MediatR.Publish → UIEventForwarder → GodotEventBus
/// </summary>
/// <typeparam name="TEvent">Any event implementing INotification</typeparam>
public class UIEventForwarder<TEvent> : INotificationHandler<TEvent> where TEvent : INotification
{
    private readonly IGodotEventBus _eventBus;
    private readonly ILogger<UIEventForwarder<TEvent>> _logger;

    public UIEventForwarder(IGodotEventBus eventBus, ILogger<UIEventForwarder<TEvent>> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// MediatR calls this when an event is published.
    /// Forwards to GodotEventBus for routing to Godot nodes.
    /// </summary>
    public Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogTrace("UIEventForwarder forwarding {EventType} to GodotEventBus",
            typeof(TEvent).Name);

        return _eventBus.PublishAsync(notification);
    }
}
