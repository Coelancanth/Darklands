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
            // Prefer scope-aware resolution; falls back to GameStrapper if ServiceLocator unavailable
            EventBus = this.GetService<IUIEventBus>();

            // Allow subclass to subscribe to specific events
            SubscribeToEvents();

            var categoryLogger = this.GetOptionalService<ICategoryLogger>();
            categoryLogger?.Log(LogLevel.Information, LogCategory.System, "{0} successfully subscribed to domain events", GetType().Name);
        }
        catch (Exception ex)
        {
            var categoryLogger = this.GetOptionalService<ICategoryLogger>();
            categoryLogger?.Log(LogLevel.Error, LogCategory.System, "{0} exception during event bus initialization: {1}", GetType().Name, ex.Message);
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
                var categoryLogger = this.GetOptionalService<ICategoryLogger>();
                categoryLogger?.Log(LogLevel.Information, LogCategory.System, "{0} unsubscribed from all events on exit", GetType().Name);
            }
        }
        catch (Exception ex)
        {
            var categoryLogger = this.GetOptionalService<ICategoryLogger>();
            categoryLogger?.Log(LogLevel.Error, LogCategory.System, "{0} error during event unsubscription: {1}", GetType().Name, ex.Message);
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
            var logger = GameStrapper.GetServices().Match(
                Succ: sp => sp.GetService<ICategoryLogger>(),
                Fail: _ => null);
            logger?.Log(LogLevel.Error, LogCategory.System, "{0} cannot subscribe to {1} - EventBus is null", GetType().Name, typeof(TEvent).Name);
            return;
        }

        try
        {
            EventBus.Subscribe<TEvent>(this, handler);
            var logger = GameStrapper.GetServices().Match(
                Succ: sp => sp.GetService<ICategoryLogger>(),
                Fail: _ => null);
            logger?.Log(LogLevel.Information, LogCategory.System, "{0} subscribed to {1}", GetType().Name, typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            var logger = GameStrapper.GetServices().Match(
                Succ: sp => sp.GetService<ICategoryLogger>(),
                Fail: _ => null);
            logger?.Log(LogLevel.Error, LogCategory.System, "{0} failed to subscribe to {1}: {2}", GetType().Name, typeof(TEvent).Name, ex.Message);
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
            var logger = GameStrapper.GetServices().Match(
                Succ: sp => sp.GetService<ICategoryLogger>(),
                Fail: _ => null);
            logger?.Log(LogLevel.Error, LogCategory.System, "{0} cannot unsubscribe from {1} - EventBus is null", GetType().Name, typeof(TEvent).Name);
            return;
        }

        try
        {
            EventBus.Unsubscribe<TEvent>(this);
            var logger = GameStrapper.GetServices().Match(
                Succ: sp => sp.GetService<ICategoryLogger>(),
                Fail: _ => null);
            logger?.Log(LogLevel.Information, LogCategory.System, "{0} unsubscribed from {1}", GetType().Name, typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            var logger = GameStrapper.GetServices().Match(
                Succ: sp => sp.GetService<ICategoryLogger>(),
                Fail: _ => null);
            logger?.Log(LogLevel.Error, LogCategory.System, "{0} failed to unsubscribe from {1}: {2}", GetType().Name, typeof(TEvent).Name, ex.Message);
        }
    }
}
