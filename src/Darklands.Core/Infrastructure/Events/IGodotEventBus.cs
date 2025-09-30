using MediatR;

namespace Darklands.Core.Infrastructure.Events;

/// <summary>
/// Event bus abstraction for routing domain events from Core to Godot nodes.
///
/// ARCHITECTURE (ADR-002):
/// - Interface in Core (abstraction) → testable without Godot
/// - Implementation in Presentation (GodotEventBus) → uses Godot.CallDeferred
/// - UIEventForwarder bridges MediatR.Publish → IGodotEventBus.PublishAsync
///
/// THREAD SAFETY:
/// - PublishAsync is thread-safe (can be called from any thread)
/// - Implementation must marshal to Godot main thread (via CallDeferred)
///
/// LIFECYCLE:
/// - Subscribers use strong references with explicit unsubscribe
/// - EventAwareNode base class handles automatic cleanup in _ExitTree()
/// </summary>
public interface IGodotEventBus
{
    /// <summary>
    /// Subscribe to a specific event type. Subscriber MUST unsubscribe in _ExitTree().
    /// </summary>
    /// <typeparam name="TEvent">Event type implementing INotification</typeparam>
    /// <param name="subscriber">The object subscribing (typically a Godot node)</param>
    /// <param name="handler">Callback invoked when event is published (on main thread)</param>
    void Subscribe<TEvent>(object subscriber, Action<TEvent> handler) where TEvent : INotification;

    /// <summary>
    /// Unsubscribe from a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">Event type to unsubscribe from</typeparam>
    /// <param name="subscriber">The subscriber object</param>
    void Unsubscribe<TEvent>(object subscriber) where TEvent : INotification;

    /// <summary>
    /// Unsubscribe from ALL event types. Called in EventAwareNode._ExitTree().
    /// </summary>
    /// <param name="subscriber">The subscriber object</param>
    void UnsubscribeAll(object subscriber);

    /// <summary>
    /// Publish an event to all subscribers. Thread-safe.
    /// Implementation marshals to Godot main thread via CallDeferred.
    /// </summary>
    /// <param name="eventData">The event to publish</param>
    Task PublishAsync<TEvent>(TEvent eventData) where TEvent : INotification;
}
