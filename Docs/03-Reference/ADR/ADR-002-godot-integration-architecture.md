# ADR-002: Godot Integration Architecture

**Status**: Approved
**Date**: 2025-09-30
**Decision Makers**: Tech Lead, Product Owner

## Context

We have Clean Architecture with pure C# Core (ADR-001), but need to connect it to Godot's node system for:
- User input (clicks, keyboard)
- Visual updates (sprites, health bars, animations)
- Game loop (frame updates, physics)

**The Challenge**: Godot instantiates nodes via scene loading, not dependency injection. How do we bridge these two worlds?

## Decision

Use **Component Pattern + Event Bus + Service Locator Bridge** to connect Godot and Core layers.

### Architecture Overview

```
┌────────────────────────────────────────────────────────────┐
│ User Input (Godot)                                         │
│ - Mouse clicks, keyboard                                   │
└─────────────────┬──────────────────────────────────────────┘
                  ↓
┌────────────────────────────────────────────────────────────┐
│ Component Nodes (Godot C#)                                 │
│ - HealthComponentNode extends EventAwareNode               │
│ - Thin adapters, NO business logic                         │
│ - Subscribe to events, send commands                       │
└─────────────────┬──────────────────────────────────────────┘
                  ↓ (Send Command)
┌────────────────────────────────────────────────────────────┐
│ MediatR (Application Layer)                                │
│ - IMediator.Send(command)                                  │
└─────────────────┬──────────────────────────────────────────┘
                  ↓
┌────────────────────────────────────────────────────────────┐
│ Command Handler (Core Application Layer)                   │
│ - ExecuteAttackCommandHandler                              │
│ - Pure C# business logic                                   │
│ - Returns Result<T>                                        │
└─────────────────┬──────────────────────────────────────────┘
                  ↓ (Publish Event)
┌────────────────────────────────────────────────────────────┐
│ GodotEventBus (Infrastructure Layer)                       │
│ - Bridges MediatR events → Godot nodes                    │
│ - Automatic thread marshalling to main thread             │
└─────────────────┬──────────────────────────────────────────┘
                  ↓ (Notify Subscribers)
┌────────────────────────────────────────────────────────────┐
│ Component Nodes (Godot C#)                                 │
│ - Update visuals (health bar, animations, etc.)           │
└────────────────────────────────────────────────────────────┘
```

## Core Patterns

### Pattern 1: Component-Based Architecture

**Domain Components (Pure C#)**:
```csharp
// Domain/Components/IHealthComponent.cs
namespace Darklands.Core.Domain.Components;

public interface IHealthComponent : IComponent
{
    ActorId OwnerId { get; }
    Health CurrentHealth { get; }
    bool IsAlive { get; }

    Result<Health> TakeDamage(float amount);
    Result<Health> Heal(float amount);
}

public class HealthComponent : IHealthComponent
{
    public ActorId OwnerId { get; }
    public Health CurrentHealth { get; private set; }
    public bool IsAlive => !CurrentHealth.IsDepleted;

    public Result<Health> TakeDamage(float amount) =>
        CurrentHealth.Reduce(amount)
            .Tap(newHealth => CurrentHealth = newHealth);
}
```

**Godot Component Node (Adapter)**:
```csharp
// Components/HealthComponentNode.cs (Godot project)
namespace Darklands.Components;

public partial class HealthComponentNode : EventAwareNode
{
    [Export] private ProgressBar? _healthBar;
    [Export] private Color _healthyColor = new(0, 1, 0);
    [Export] private Color _criticalColor = new(1, 0, 0);

    private IMediator? _mediator;
    private ActorId? _ownerId;

    public override void _Ready()
    {
        base._Ready(); // Initialize EventBus

        _mediator = ServiceLocatorBridge.GetService<IMediator>();
        _ownerId = ActorId.From(GetParent().GetMeta("ActorId").AsString());
    }

    protected override void SubscribeToEvents()
    {
        EventBus!.Subscribe<HealthChangedEvent>(this, OnHealthChanged);
    }

    private void OnHealthChanged(HealthChangedEvent e)
    {
        if (e.ActorId != _ownerId) return;

        // Update UI (pure view logic, no business logic!)
        _healthBar!.Value = e.NewHealth.Percentage * 100;
        _healthBar.Modulate = e.IsCritical ? _criticalColor : _healthyColor;
    }

    // Public API for other components/nodes
    public async void ApplyDamage(float amount)
    {
        await _mediator!.Send(new TakeDamageCommand(_ownerId!.Value, amount));
    }
}
```

**Benefits**:
- ✅ Same component works for player, NPCs, bosses
- ✅ Configure visuals per-scene in Godot editor
- ✅ Business logic in domain, presentation in nodes
- ✅ Fully testable (domain components)

### Pattern 2: GodotEventBus

**Purpose**: Bridge MediatR domain events to Godot nodes.

**Implementation**:
```csharp
// Infrastructure/Events/IGodotEventBus.cs
namespace Darklands.Core.Infrastructure.Events;

public interface IGodotEventBus
{
    void Subscribe<TEvent>(object subscriber, Action<TEvent> handler)
        where TEvent : INotification;

    void Unsubscribe<TEvent>(object subscriber)
        where TEvent : INotification;

    void UnsubscribeAll(object subscriber);

    Task PublishAsync<TEvent>(TEvent notification)
        where TEvent : INotification;
}

// Infrastructure/Events/GodotEventBus.cs
public sealed class GodotEventBus : IGodotEventBus
{
    private readonly Dictionary<Type, List<WeakSubscription>> _subscriptions = new();

    private class WeakSubscription
    {
        public WeakReference<object> Subscriber { get; }
        public Delegate Handler { get; }
    }

    public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler)
        where TEvent : INotification
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.ContainsKey(eventType))
            _subscriptions[eventType] = new();

        _subscriptions[eventType].Add(new WeakSubscription(subscriber, handler));
    }

    public async Task PublishAsync<TEvent>(TEvent notification)
        where TEvent : INotification
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.TryGetValue(eventType, out var subs))
            return;

        foreach (var sub in subs)
        {
            if (sub.Subscriber.TryGetTarget(out var target))
            {
                // Thread-safe UI update for Godot
                if (target is Node godotNode && godotNode.IsInsideTree())
                {
                    godotNode.CallDeferred(() =>
                        ((Action<TEvent>)sub.Handler).Invoke(notification));
                }
            }
        }
    }
}
```

**Why WeakReference?** Godot nodes can be freed at any time. Strong references would prevent garbage collection and cause memory leaks.

**Why CallDeferred?** Godot requires UI updates on the main thread. CallDeferred marshals the call automatically.

**MediatR Forwarder**:
```csharp
// Infrastructure/Events/UIEventForwarder.cs
public class UIEventForwarder<TEvent> : INotificationHandler<TEvent>
    where TEvent : INotification
{
    private readonly IGodotEventBus _eventBus;

    public UIEventForwarder(IGodotEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        return _eventBus.PublishAsync(notification);
    }
}
```

### Pattern 3: EventAwareNode Base Class

**Purpose**: Automatic subscription lifecycle management for Godot nodes.

```csharp
// Presentation/Components/EventAwareNode.cs (in Godot project)
namespace Darklands.Components;

public abstract partial class EventAwareNode : Node2D
{
    protected IGodotEventBus? EventBus { get; private set; }

    public override void _Ready()
    {
        ServiceLocatorBridge.GetService<IGodotEventBus>()
            .Match(
                success: bus => {
                    EventBus = bus;
                    SubscribeToEvents();
                },
                failure: err => GD.PrintErr($"Failed to get EventBus: {err}")
            );
    }

    public override void _ExitTree()
    {
        EventBus?.UnsubscribeAll(this);
        EventBus = null;
    }

    protected abstract void SubscribeToEvents();
}
```

**Usage**:
```csharp
public partial class CombatComponent : EventAwareNode
{
    protected override void SubscribeToEvents()
    {
        EventBus!.Subscribe<AttackExecutedEvent>(this, OnAttackExecuted);
        EventBus!.Subscribe<ActorDiedEvent>(this, OnActorDied);
    }

    private void OnAttackExecuted(AttackExecutedEvent e)
    {
        // Update UI...
    }
}
```

### Pattern 4: Service Locator Bridge

**Problem**: Godot instantiates nodes via scene loading, not DI.

**Solution**: Service Locator pattern at Godot boundary ONLY.

```csharp
// Infrastructure/DependencyInjection/ServiceLocatorBridge.cs
namespace Darklands.Core.Infrastructure.DependencyInjection;

public static class ServiceLocatorBridge
{
    public static Result<T> GetService<T>() where T : class
    {
        return GameStrapper.GetServices()
            .Bind(provider =>
            {
                try
                {
                    var service = provider.GetService<T>();
                    return service != null
                        ? Result.Success(service)
                        : Result.Failure<T>($"Service {typeof(T).Name} not registered");
                }
                catch (Exception ex)
                {
                    return Result.Failure<T>($"Failed to resolve: {ex.Message}");
                }
            });
    }
}
```

**When to Use**:
- ✅ **ONLY** in Godot node `_Ready()` methods
- ✅ With functional error handling (returns `Result<T>`)
- ✅ For services that can't be constructor-injected

**When NOT to Use**:
- ❌ In Core project (use constructor injection)
- ❌ Outside of `_Ready()` (cache the service)
- ❌ For logic (only for wiring)

## Component Registry

**Purpose**: Track which components belong to which actors.

```csharp
// Application/Services/IComponentRegistry.cs
public interface IComponentRegistry
{
    Result<T> GetComponent<T>(ActorId actorId) where T : IComponent;
    Result AddComponent<T>(ActorId actorId, T component) where T : IComponent;
    Result RemoveComponent<T>(ActorId actorId) where T : IComponent;
    IEnumerable<T> GetAllComponents<T>() where T : IComponent;
}

// Infrastructure/Services/ComponentRegistry.cs
public sealed class ComponentRegistry : IComponentRegistry
{
    private readonly Dictionary<(ActorId, Type), IComponent> _components = new();

    public Result<T> GetComponent<T>(ActorId actorId) where T : IComponent
    {
        var key = (actorId, typeof(T));
        return _components.TryGetValue(key, out var component)
            ? Result.Success((T)component)
            : Result.Failure<T>($"Component not found");
    }

    // ... implementation
}
```

## DI Registration

```csharp
// Infrastructure/DependencyInjection/GameStrapper.cs
services.AddSingleton<IGodotEventBus, GodotEventBus>();
services.AddSingleton<IComponentRegistry, ComponentRegistry>();
services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));
```

## Complete Flow Example: Attack

```csharp
// 1. User clicks attack button
public partial class CombatUI : Node
{
    private void _on_attack_button_pressed()
    {
        var mediator = ServiceLocatorBridge.GetService<IMediator>();
        await mediator.Send(new ExecuteAttackCommand(attackerId, targetId));
    }
}

// 2. Handler processes command (Core, pure C#)
public class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand>
{
    public async Task<Result> Handle(ExecuteAttackCommand cmd)
    {
        var attacker = await _actors.GetActor(cmd.AttackerId);
        var target = await _actors.GetActor(cmd.TargetId);

        var damage = CalculateDamage(attacker, target);
        target.TakeDamage(damage);

        // Publish event
        await _mediator.Publish(new AttackExecutedEvent(cmd.AttackerId, cmd.TargetId, damage));

        return Result.Success();
    }
}

// 3. UIEventForwarder routes to GodotEventBus (automatic)

// 4. Components receive event and update UI
public partial class HealthComponentNode : EventAwareNode
{
    private void OnHealthChanged(HealthChangedEvent e)
    {
        _healthBar.Value = e.NewHealth.Percentage * 100;
        _damageAnimation.Play("damage_flash");
    }
}
```

## Consequences

### Positive
- ✅ **Clear Separation**: Godot is just a presentation layer
- ✅ **Testable**: Components can be tested without Godot
- ✅ **Composable**: Mix and match components in editor
- ✅ **Type-Safe**: Compile-time checking of events
- ✅ **Memory-Safe**: WeakReferences prevent leaks
- ✅ **Thread-Safe**: CallDeferred handles marshalling

### Negative
- ❌ **Service Locator**: Anti-pattern, but necessary at Godot boundary
- ❌ **Learning Curve**: Team must understand pattern
- ❌ **Event Overhead**: Slight performance cost from weak references

### Neutral
- ➖ **More Abstraction**: EventBus adds layer between domain and UI
- ➖ **Boilerplate**: Each component needs node wrapper

## Alternatives Considered

### 1. Direct Node Access from Handlers
**Rejected**: Couples Core to Godot, untestable

### 2. Godot Signals Only
**Rejected**: Duplicates event system, loses MediatR benefits

### 3. Constructor Injection in Nodes
**Rejected**: Impossible, Godot owns node instantiation

## Success Metrics

- ✅ Zero business logic in component nodes
- ✅ All domain components testable without Godot
- ✅ No memory leaks from destroyed nodes
- ✅ Events delivered to correct subscribers only
- ✅ Thread-safe UI updates

## References

- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html) - Martin Fowler
- [Domain Events Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation) - Microsoft
- [Service Locator (Anti-)Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) - Mark Seemann