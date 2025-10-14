using Godot;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Infrastructure.Events;

namespace Darklands.Presentation.Infrastructure.Events;

/// <summary>
/// Base class for Godot nodes that subscribe to domain events via GodotEventBus.
///
/// ARCHITECTURE (ADR-002):
/// - Resolves IGodotEventBus via ServiceLocator in _Ready()
/// - Calls UnsubscribeAll() in _ExitTree() to prevent memory leaks
/// - Child classes override SubscribeToEvents() to register event handlers
///
/// LIFECYCLE:
/// 1. Godot instantiates node (via scene loading)
/// 2. _Ready() → Get IGodotEventBus → Call SubscribeToEvents()
/// 3. Node active → receives events via registered handlers
/// 4. _ExitTree() → UnsubscribeAll(this) → cleanup complete
///
/// USAGE:
/// <code>
/// public partial class HealthBarNode : EventAwareNode
/// {
///     protected override void SubscribeToEvents()
///     {
///         EventBus.Subscribe&lt;HealthChangedEvent&gt;(this, OnHealthChanged);
///     }
///
///     private void OnHealthChanged(HealthChangedEvent evt)
///     {
///         // Update health bar UI
///     }
/// }
/// </code>
/// </summary>
public abstract partial class EventAwareNode : Node
{
    /// <summary>
    /// Event bus for subscribing to domain events.
    /// Resolved via ServiceLocator in _Ready().
    /// </summary>
    protected IGodotEventBus? EventBus { get; private set; }

    /// <summary>
    /// Godot lifecycle: Node added to scene tree.
    /// Resolves EventBus and calls SubscribeToEvents().
    /// </summary>
    public override void _Ready()
    {
        base._Ready();

        // Resolve EventBus via ServiceLocator (Godot nodes can't use constructor injection)
        var result = ServiceLocator.GetService<IGodotEventBus>();

        if (result.IsFailure)
        {
            GD.PrintErr($"[EventAwareNode] Failed to resolve IGodotEventBus: {result.Error}");
            GD.PrintErr($"[EventAwareNode] Ensure GameStrapper.Initialize() is called before loading scenes with EventAwareNode");
            return;
        }

        EventBus = result.Value;

        // Allow child class to subscribe to events
        SubscribeToEvents();

        GD.Print($"[EventAwareNode] {GetType().Name} subscribed to events");
    }

    /// <summary>
    /// Godot lifecycle: Node removed from scene tree.
    /// Unsubscribes from all events to prevent memory leaks.
    /// </summary>
    public override void _ExitTree()
    {
        // CRITICAL: Unsubscribe before node is destroyed
        EventBus?.UnsubscribeAll(this);

        GD.Print($"[EventAwareNode] {GetType().Name} unsubscribed from all events");

        base._ExitTree();
    }

    /// <summary>
    /// Override this in child classes to subscribe to domain events.
    ///
    /// Example:
    /// <code>
    /// protected override void SubscribeToEvents()
    /// {
    ///     EventBus.Subscribe&lt;HealthChangedEvent&gt;(this, OnHealthChanged);
    ///     EventBus.Subscribe&lt;ActorDiedEvent&gt;(this, OnActorDied);
    /// }
    /// </code>
    /// </summary>
    protected abstract void SubscribeToEvents();
}