using System;
using System.Threading.Tasks;
using MediatR;

namespace Darklands.Application.Events;

/// <summary>
/// Core contract for UI Event Bus - bridges MediatR domain events to Godot UI components.
/// 
/// Architecture Pattern:
/// - Application layer defines the contract (this interface)
/// - Infrastructure layer provides the implementation  
/// - Presentation layer consumes events via typed subscriptions
/// 
/// Lifecycle Management:
/// - Uses WeakReferences to prevent memory leaks from destroyed Godot nodes
/// - Automatic cleanup of dead subscribers during publication
/// - Thread-safe UI updates via Godot's CallDeferred mechanism
/// 
/// Scalability:
/// - Supports 200+ event types without modification
/// - Type-safe subscriptions with compile-time verification
/// - Open/Closed principle compliance - extend without modification
/// </summary>
public interface IUIEventBus
{
    /// <summary>
    /// Subscribes an object to receive notifications for a specific event type.
    /// Uses WeakReference to prevent memory leaks when subscribers are destroyed.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type to subscribe to (must implement INotification)</typeparam>
    /// <param name="subscriber">The object that will receive notifications (typically a Godot Node)</param>
    /// <param name="handler">The method to call when the event occurs</param>
    /// <remarks>
    /// Safe for UI components that may be destroyed by Godot's scene tree.
    /// WeakReference prevents memory leaks if subscriber is garbage collected.
    /// </remarks>
    void Subscribe<TEvent>(object subscriber, Action<TEvent> handler)
        where TEvent : INotification;

    /// <summary>
    /// Unsubscribes an object from receiving a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to unsubscribe from</typeparam>
    /// <param name="subscriber">The subscriber to remove</param>
    void Unsubscribe<TEvent>(object subscriber)
        where TEvent : INotification;

    /// <summary>
    /// Unsubscribes an object from ALL event types.
    /// Called automatically by EventAwareNode._ExitTree() to prevent memory leaks.
    /// </summary>
    /// <param name="subscriber">The subscriber to remove from all event types</param>
    void UnsubscribeAll(object subscriber);

    /// <summary>
    /// Publishes a domain event to all registered subscribers.
    /// Called by UIEventForwarder MediatR handlers to bridge events to UI.
    /// 
    /// Thread Safety:
    /// - Uses CallDeferred for Godot Node subscribers (main thread marshalling)
    /// - Direct invocation for non-Godot subscribers (testing scenarios)
    /// </summary>
    /// <typeparam name="TEvent">The domain event type being published</typeparam>
    /// <param name="notification">The event data to publish</param>
    /// <returns>Completion task for async coordination</returns>
    Task PublishAsync<TEvent>(TEvent notification)
        where TEvent : INotification;
}
