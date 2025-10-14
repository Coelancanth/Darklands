using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;
using Darklands.Core.Infrastructure.Events;

namespace Darklands.Presentation.Infrastructure.Events;

/// <summary>
/// Event bus implementation that bridges MediatR domain events to Godot nodes.
///
/// ARCHITECTURE (ADR-002):
/// - Implements IGodotEventBus (Core abstraction)
/// - Uses Godot.CallDeferred for thread-safe UI updates
/// - Strong references with explicit unsubscribe (via EventAwareNode base class)
///
/// THREAD SAFETY:
/// - ConcurrentDictionary for subscription storage (lock-free reads)
/// - PublishAsync marshals to Godot main thread via CallDeferred
///
/// LIFECYCLE:
/// - Subscribers MUST call UnsubscribeAll() in _ExitTree() to prevent leaks
/// - EventAwareNode base class handles this automatically
/// </summary>
public class GodotEventBus : IGodotEventBus
{
    private readonly ILogger<GodotEventBus> _logger;

    // Type → (Subscriber → Handler)
    // ConcurrentDictionary provides thread-safe operations
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, Delegate>> _subscriptions = new();

    public GodotEventBus(ILogger<GodotEventBus> logger)
    {
        _logger = logger;
        _logger.LogDebug("GodotEventBus initialized");
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler) where TEvent : INotification
    {
        var eventType = typeof(TEvent);

        // Get or create subscription dictionary for this event type
        var subscribers = _subscriptions.GetOrAdd(eventType, _ => new ConcurrentDictionary<object, Delegate>());

        // Add or update subscriber's handler (strong reference)
        if (subscribers.TryAdd(subscriber, handler))
        {
            _logger.LogDebug("Subscriber {Subscriber} registered for {EventType}",
                subscriber.GetType().Name, eventType.Name);
        }
        else
        {
            // Update existing subscription
            subscribers[subscriber] = handler;
            _logger.LogDebug("Subscriber {Subscriber} updated handler for {EventType}",
                subscriber.GetType().Name, eventType.Name);
        }
    }

    /// <inheritdoc />
    public void Unsubscribe<TEvent>(object subscriber) where TEvent : INotification
    {
        var eventType = typeof(TEvent);

        if (_subscriptions.TryGetValue(eventType, out var subscribers))
        {
            if (subscribers.TryRemove(subscriber, out _))
            {
                _logger.LogDebug("Subscriber {Subscriber} unsubscribed from {EventType}",
                    subscriber.GetType().Name, eventType.Name);
            }
        }
    }

    /// <inheritdoc />
    public void UnsubscribeAll(object subscriber)
    {
        var unsubscribedCount = 0;

        // Remove subscriber from all event types
        foreach (var kvp in _subscriptions)
        {
            if (kvp.Value.TryRemove(subscriber, out _))
            {
                unsubscribedCount++;
                _logger.LogTrace("Subscriber {Subscriber} removed from {EventType}",
                    subscriber.GetType().Name, kvp.Key.Name);
            }
        }

        if (unsubscribedCount > 0)
        {
            _logger.LogDebug("Subscriber {Subscriber} unsubscribed from {Count} event types",
                subscriber.GetType().Name, unsubscribedCount);
        }
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent eventData) where TEvent : INotification
    {
        var eventType = typeof(TEvent);

        if (!_subscriptions.TryGetValue(eventType, out var subscribers) || subscribers.IsEmpty)
        {
            _logger.LogTrace("No subscribers for {EventType}", eventType.Name);
            return Task.CompletedTask;
        }

        _logger.LogDebug("Publishing {EventType} to {Count} subscribers",
            eventType.Name, subscribers.Count);

        // Invoke all subscribers on Godot main thread
        foreach (var kvp in subscribers)
        {
            var subscriber = kvp.Key;
            var handler = (Action<TEvent>)kvp.Value;

            // Marshal to Godot main thread via CallDeferred
            // Godot 4 C# pattern: Callable.From(() => ...).CallDeferred()
            try
            {
                Callable.From(() =>
                {
                    try
                    {
                        handler(eventData);
                    }
                    catch (Exception ex)
                    {
                        // Isolate errors - one bad subscriber doesn't break others
                        _logger.LogError(ex,
                            "Error in subscriber {Subscriber} handling {EventType}",
                            subscriber.GetType().Name, eventType.Name);
                    }
                }).CallDeferred();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to defer call for subscriber {Subscriber}",
                    subscriber.GetType().Name);
            }
        }

        return Task.CompletedTask;
    }
}