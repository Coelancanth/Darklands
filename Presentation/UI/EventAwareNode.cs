using System;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Darklands.Core.Domain.Debug;
using Darklands.Core.Infrastructure.Logging;
using Darklands.Core.Application.Events;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Presentation.Infrastructure;
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
    /// Category logger for diagnostic output.
    /// Cached during _Ready() from the scoped service provider.
    /// </summary>
    protected ICategoryLogger? Logger { get; private set; }

    /// <summary>
    /// Called when the node is added to the scene tree.
    /// TD_052: Updated to use scope-aware service resolution instead of static GameStrapper pattern.
    ///
    /// Scope-Aware Pattern:
    /// Uses NodeServiceExtensions to get services from the appropriate scope for this node.
    /// Falls back to GameStrapper pattern if scope management fails.
    /// Caches resolved services for performance.
    /// </summary>
    public override void _Ready()
    {
        try
        {
            // TD_052: Use scope-aware service resolution instead of static GameStrapper
            // This will get services from the appropriate scope for this node's location in the tree
            EventBus = this.GetService<IUIEventBus>();
            Logger = this.GetService<ICategoryLogger>();

            // Allow subclass to subscribe to specific events
            SubscribeToEvents();

            Logger?.Log(LogLevel.Information, LogCategory.System,
                       "{0} successfully subscribed to domain events via scope-aware resolution",
                       GetType().Name);
        }
        catch (Exception ex)
        {
            // Fallback to GameStrapper pattern for error logging if scope resolution fails
            var fallbackLogger = GameStrapper.GetServices().Match(
                Succ: sp => sp.GetService<ICategoryLogger>(),
                Fail: _ => null);

            fallbackLogger?.Log(LogLevel.Error, LogCategory.System,
                               "{0} exception during scope-aware event bus initialization: {1}. " +
                               "Check ServiceLocator autoload and scope creation.",
                               GetType().Name, ex.Message);

            // Set EventBus to null so SafeSubscribe methods can handle gracefully
            EventBus = null;
            Logger = fallbackLogger;
        }
    }

    /// <summary>
    /// Called when the node is removed from the scene tree.
    /// Automatically unsubscribes from ALL events to prevent memory leaks.
    ///
    /// This is critical for preventing memory leaks when nodes are destroyed
    /// or scenes are changed in Godot.
    /// TD_052: Uses cached Logger instead of accessing GameStrapper.
    /// </summary>
    public override void _ExitTree()
    {
        try
        {
            if (EventBus != null)
            {
                EventBus.UnsubscribeAll(this);
                Logger?.Log(LogLevel.Information, LogCategory.System,
                           "{0} unsubscribed from all events on exit",
                           GetType().Name);
            }
        }
        catch (Exception ex)
        {
            Logger?.Log(LogLevel.Error, LogCategory.System,
                       "{0} error during event unsubscription: {1}",
                       GetType().Name, ex.Message);
        }
        finally
        {
            // Clear cached services
            EventBus = null;
            Logger = null;
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
    /// TD_052: Uses cached Logger instead of accessing GameStrapper.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to</typeparam>
    /// <param name="handler">The handler method to call when the event occurs</param>
    protected void SafeSubscribe<TEvent>(Action<TEvent> handler) where TEvent : INotification
    {
        if (EventBus == null)
        {
            Logger?.Log(LogLevel.Error, LogCategory.System,
                       "{0} cannot subscribe to {1} - EventBus is null",
                       GetType().Name, typeof(TEvent).Name);
            return;
        }

        try
        {
            EventBus.Subscribe<TEvent>(this, handler);
            Logger?.Log(LogLevel.Information, LogCategory.System,
                       "{0} subscribed to {1}",
                       GetType().Name, typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            Logger?.Log(LogLevel.Error, LogCategory.System,
                       "{0} failed to subscribe to {1}: {2}",
                       GetType().Name, typeof(TEvent).Name, ex.Message);
        }
    }

    /// <summary>
    /// Helper method for subclasses to safely unsubscribe from specific events.
    /// Usually not needed as _ExitTree() unsubscribes from all events automatically.
    /// TD_052: Uses cached Logger instead of accessing GameStrapper.
    /// </summary>
    /// <typeparam name="TEvent">The event type to unsubscribe from</typeparam>
    protected void SafeUnsubscribe<TEvent>() where TEvent : INotification
    {
        if (EventBus == null)
        {
            Logger?.Log(LogLevel.Error, LogCategory.System,
                       "{0} cannot unsubscribe from {1} - EventBus is null",
                       GetType().Name, typeof(TEvent).Name);
            return;
        }

        try
        {
            EventBus.Unsubscribe<TEvent>(this);
            Logger?.Log(LogLevel.Information, LogCategory.System,
                       "{0} unsubscribed from {1}",
                       GetType().Name, typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            Logger?.Log(LogLevel.Error, LogCategory.System,
                       "{0} failed to unsubscribe from {1}: {2}",
                       GetType().Name, typeof(TEvent).Name, ex.Message);
        }
    }
}
