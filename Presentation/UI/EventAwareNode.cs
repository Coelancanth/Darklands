using System;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Darklands.Core.Application.Events;
using Darklands.Core.Infrastructure.DependencyInjection;
using MediatR;

namespace Darklands.Presentation.UI;

/// <summary>
/// Base class for Godot nodes that need to subscribe to domain events from MediatR.
/// 
/// Architecture Pattern:
/// - Bridges the gap between Godot's scene tree lifecycle and Clean Architecture events
/// - Uses service locator pattern to access DI container (necessary evil for Godot integration)
/// - Provides automatic subscription/unsubscription lifecycle management
/// - Ensures thread-safe UI updates via Godot's main thread marshalling
/// 
/// Lifecycle Management:
/// - _Ready(): Retrieves IUIEventBus from DI container and calls SubscribeToEvents()
/// - _ExitTree(): Automatically unsubscribes from ALL events to prevent memory leaks
/// - Subclasses override SubscribeToEvents() to register for specific event types
/// 
/// Thread Safety:
/// - UIEventBus handles CallDeferred marshalling to main thread automatically
/// - Event handlers in subclasses are guaranteed to run on Godot's main thread
/// - Safe for events published from background threads or other game systems
/// </summary>
public abstract partial class EventAwareNode : Node2D
{
    /// <summary>
    /// The UI Event Bus for subscribing to domain events.
    /// Retrieved from DI container during _Ready().
    /// </summary>
    protected IUIEventBus? EventBus { get; private set; }

    /// <summary>
    /// Called when the node is added to the scene tree.
    /// Retrieves IUIEventBus from DI container and calls SubscribeToEvents().
    /// 
    /// Service Locator Pattern:
    /// This is one of the few places where service locator is acceptable,
    /// as Godot instantiates nodes via scene loading, not dependency injection.
    /// </summary>
    public override void _Ready()
    {
        try
        {
            // Get the service provider from the GameStrapper
            var serviceProviderResult = GameStrapper.GetServices();

            serviceProviderResult.Match(
                Succ: serviceProvider =>
                {
                    EventBus = serviceProvider.GetRequiredService<IUIEventBus>();

                    // Allow subclass to subscribe to specific events
                    SubscribeToEvents();

                    GD.Print($"[{GetType().Name}] Successfully subscribed to domain events");
                },
                Fail: error =>
                {
                    GD.PrintErr($"[{GetType().Name}] Failed to get UI Event Bus: {error.Message}");
                    GD.PrintErr("Event subscriptions will not work - check DI container configuration");
                }
            );
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{GetType().Name}] Exception during event bus initialization: {ex.Message}");
            GD.PrintErr("Event subscriptions will not work - check service registration");
        }
    }

    /// <summary>
    /// Called when the node is removed from the scene tree.
    /// Automatically unsubscribes from ALL events to prevent memory leaks.
    /// 
    /// This is critical for preventing memory leaks when nodes are destroyed
    /// or scenes are changed in Godot.
    /// </summary>
    public override void _ExitTree()
    {
        try
        {
            if (EventBus != null)
            {
                EventBus.UnsubscribeAll(this);
                GD.Print($"[{GetType().Name}] Unsubscribed from all events on exit");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{GetType().Name}] Error during event unsubscription: {ex.Message}");
        }
        finally
        {
            EventBus = null;
        }
    }

    /// <summary>
    /// Abstract method that subclasses must override to subscribe to specific event types.
    /// 
    /// Called automatically after EventBus is initialized in _Ready().
    /// 
    /// Example implementation:
    /// <code>
    /// protected override void SubscribeToEvents()
    /// {
    ///     EventBus!.Subscribe{ActorDiedEvent}(this, OnActorDied);
    ///     EventBus!.Subscribe{ActorDamagedEvent}(this, OnActorDamaged);
    /// }
    /// 
    /// private void OnActorDied(ActorDiedEvent e)
    /// {
    ///     // Handle the event - already on main thread
    ///     RemoveActorFromUI(e.ActorId);
    /// }
    /// </code>
    /// </summary>
    protected abstract void SubscribeToEvents();

    /// <summary>
    /// Helper method for subclasses to safely subscribe to events.
    /// Provides null-safety checking and error handling.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to</typeparam>
    /// <param name="handler">The handler method to call when the event occurs</param>
    protected void SafeSubscribe<TEvent>(Action<TEvent> handler) where TEvent : INotification
    {
        if (EventBus == null)
        {
            GD.PrintErr($"[{GetType().Name}] Cannot subscribe to {typeof(TEvent).Name} - EventBus is null");
            return;
        }

        try
        {
            EventBus.Subscribe<TEvent>(this, handler);
            GD.Print($"[{GetType().Name}] Subscribed to {typeof(TEvent).Name}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{GetType().Name}] Failed to subscribe to {typeof(TEvent).Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method for subclasses to safely unsubscribe from specific events.
    /// Usually not needed as _ExitTree() unsubscribes from all events automatically.
    /// </summary>
    /// <typeparam name="TEvent">The event type to unsubscribe from</typeparam>
    protected void SafeUnsubscribe<TEvent>() where TEvent : INotification
    {
        if (EventBus == null)
        {
            GD.PrintErr($"[{GetType().Name}] Cannot unsubscribe from {typeof(TEvent).Name} - EventBus is null");
            return;
        }

        try
        {
            EventBus.Unsubscribe<TEvent>(this);
            GD.Print($"[{GetType().Name}] Unsubscribed from {typeof(TEvent).Name}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{GetType().Name}] Failed to unsubscribe from {typeof(TEvent).Name}: {ex.Message}");
        }
    }
}
