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

## Complete Flow Example: Button Click → Health Bar Updates

This example demonstrates the **bidirectional but decoupled** flow.

### The Scenario
User clicks "Damage" button → Health decreases → Health bar updates

### Control Flow (Step-by-Step)

```
[USER CLICKS BUTTON] ─────────────────────────────────┐
                                                       │
                      ┌─ DIRECTION 1: Input (Commands) ─┘
                      │   Godot → Core
                      │
    ┌─────────────────▼────────────────────────────────┐
    │ Godot/Components/HealthComponentNode.cs          │
    │ public async void _on_damage_button_pressed()    │
    │ {                                                 │
    │     await _mediator.Send(                        │
    │         new TakeDamageCommand(_ownerId, 10));    │
    │ }                                                 │
    └─────────────────┬────────────────────────────────┘
                      │
                      ↓ MediatR routes to handler
    ┌─────────────────▼────────────────────────────────┐
    │ Core/Application/TakeDamageCommandHandler.cs     │
    │ public async Task<Result> Handle(...)            │
    │ {                                                 │
    │     // 1. Get domain component                   │
    │     var health = _registry.GetComponent<Health>();│
    │                                                   │
    │     // 2. Execute business logic                 │
    │     var result = health.TakeDamage(10);          │
    │     //    ↳ Health: 100 → 90                     │
    │                                                   │
    │     // 3. Publish domain event                   │
    │     await _mediator.Publish(                     │
    │         new HealthChangedEvent(                  │
    │             ActorId,                             │
    │             NewHealth: 90,                       │
    │             IsCritical: false));                 │
    │                                                   │
    │     return Result.Success();                     │
    │ }                                                 │
    └─────────────────┬────────────────────────────────┘
                      │
                      ↓ MediatR.Publish()
    ┌─────────────────▼────────────────────────────────┐
    │ Infrastructure/Events/UIEventForwarder<T>        │
    │ (Automatic - registered in DI)                   │
    │                                                   │
    │ public Task Handle(HealthChangedEvent evt)       │
    │ {                                                 │
    │     return _godotEventBus.PublishAsync(evt);     │
    │ }                                                 │
    └─────────────────┬────────────────────────────────┘
                      │
                      ↓ GodotEventBus.PublishAsync()
    ┌─────────────────▼────────────────────────────────┐
    │ Infrastructure/Events/GodotEventBus.cs           │
    │                                                   │
    │ foreach (var subscriber in subscribers)          │
    │ {                                                 │
    │     if (subscriber is Node godotNode)            │
    │     {                                             │
    │         // Thread-safe: marshal to main thread   │
    │         godotNode.CallDeferred(() =>             │
    │             handler.Invoke(evt));                │
    │     }                                             │
    │ }                                                 │
    └─────────────────┬────────────────────────────────┘
                      │
                      │ ┌─ DIRECTION 2: Output (Events)
                      │ │   Core → Godot
                      ↓ │
    ┌─────────────────▼─┴──────────────────────────────┐
    │ Godot/Components/HealthComponentNode.cs          │
    │ private void OnHealthChanged(HealthChangedEvent e)│
    │ {                                                 │
    │     if (e.ActorId != _ownerId) return;           │
    │                                                   │
    │     // Update UI with new state                  │
    │     _healthBar.Value = 90; // e.NewHealth        │
    │     _healthBar.Modulate = Colors.Green;          │
    │     _damageAnimation.Play("damage_flash");       │
    │ }                                                 │
    └───────────────────────────────────────────────────┘
                      │
                      ↓
            [HEALTH BAR UPDATES ON SCREEN]
```

### Key Observations

**1. Two Separate One-Way Flows (Not Circular!)**
```
Commands:  Godot ─────→ Core   (Request: "do this")
Events:    Core  ─────→ Godot  (Notification: "this happened")
```

**2. Decoupling Mechanisms**
- **Commands**: Mediator pattern (request/response)
- **Events**: Pub/Sub pattern (broadcast)
- **Result**: Core never knows about Godot!

**3. Thread Safety**
```csharp
// GodotEventBus automatically marshals to main thread
godotNode.CallDeferred(() => handler.Invoke(evt));
// This prevents threading issues in Godot
```

**4. Memory Safety**
```csharp
// WeakReference in subscriptions
private class WeakSubscription
{
    public WeakReference<object> Subscriber { get; }
    // ↳ Prevents memory leaks when nodes are freed
}
```

### Code: Complete Working Example

```csharp
// ===== GODOT LAYER =====
// Godot/Components/HealthComponentNode.cs
public partial class HealthComponentNode : EventAwareNode
{
    [Export] private ProgressBar _healthBar;
    [Export] private Button _damageButton;

    private IMediator _mediator;
    private ActorId _ownerId = ActorId.From("player-1");

    public override void _Ready()
    {
        base._Ready(); // Initialize EventBus subscription

        _mediator = ServiceLocatorBridge.GetService<IMediator>()
            .Match(m => m, _ => null);

        _damageButton.Pressed += _on_damage_button_pressed;
    }

    // ──────────────────────────────────────────────────────
    // DIRECTION 1: User Input → Send Command to Core
    // ──────────────────────────────────────────────────────
    private async void _on_damage_button_pressed()
    {
        // Send command (Godot → Core)
        var result = await _mediator.Send(
            new TakeDamageCommand(_ownerId, DamageAmount: 10f));

        // Handle failure (optional)
        result.Match(
            success: () => GD.Print("Damage applied"),
            failure: err => GD.PrintErr($"Failed: {err}"));
    }

    // ──────────────────────────────────────────────────────
    // DIRECTION 2: Receive Event from Core → Update UI
    // ──────────────────────────────────────────────────────
    protected override void SubscribeToEvents()
    {
        // Subscribe to events (Core → Godot)
        EventBus.Subscribe<HealthChangedEvent>(this, OnHealthChanged);
    }

    private void OnHealthChanged(HealthChangedEvent e)
    {
        // Filter: Only react to OUR actor's events
        if (e.ActorId != _ownerId) return;

        // Update UI (pure presentation logic)
        _healthBar.Value = e.NewHealth.Percentage * 100;
        _healthBar.Modulate = e.IsCritical ? Colors.Red : Colors.Green;

        GD.Print($"Health updated: {e.NewHealth.Current}/{e.NewHealth.Maximum}");
    }
}

// ===== CORE LAYER (Pure C#, no Godot!) =====

// Application/Commands/TakeDamageCommand.cs
public record TakeDamageCommand(
    ActorId TargetId,
    float DamageAmount
) : IRequest<Result>;

// Application/Commands/TakeDamageCommandHandler.cs
public class TakeDamageCommandHandler
    : IRequestHandler<TakeDamageCommand, Result>
{
    private readonly IComponentRegistry _components;
    private readonly IMediator _mediator;

    public async Task<Result> Handle(
        TakeDamageCommand cmd,
        CancellationToken ct)
    {
        // Get domain component (pure business logic)
        var healthResult = _components.GetComponent<IHealthComponent>(cmd.TargetId);
        if (healthResult.IsFailure)
            return Result.Failure("Actor not found");

        var health = healthResult.Value;

        // Execute business logic
        var damageResult = health.TakeDamage(cmd.DamageAmount);
        if (damageResult.IsFailure)
            return damageResult;

        var newHealth = damageResult.Value;

        // Publish domain event (Core → Godot via EventBus)
        await _mediator.Publish(new HealthChangedEvent(
            cmd.TargetId,
            newHealth,
            IsCritical: newHealth.Percentage < 0.25f
        ), ct);

        return Result.Success();
    }
}

// Application/Events/HealthChangedEvent.cs
public record HealthChangedEvent(
    ActorId ActorId,
    Health NewHealth,
    bool IsCritical
) : INotification;

// Domain/ValueObjects/Health.cs
public record Health(float Current, float Maximum)
{
    public float Percentage => Current / Maximum;

    public Result<Health> Reduce(float amount) =>
        amount < 0
            ? Result.Failure<Health>("Damage cannot be negative")
            : Result.Success(new Health(
                Math.Max(0, Current - amount),
                Maximum));
}
```

### Summary: Bidirectional but Decoupled

| Flow | Mechanism | Purpose |
|------|-----------|---------|
| **Godot → Core** | Commands via MediatR | User actions trigger business logic |
| **Core → Godot** | Events via GodotEventBus | State changes update UI |

**Result**: Clean separation, fully testable, no circular dependencies!

---

## ⚠️ IMPORTANT: When to Use Godot Features vs EventBus

### **Philosophy: Use Godot Naturally, EventBus for Cross-System State**

We use Clean Architecture for **complex game logic** (time-unit combat, AI, world simulation), NOT to avoid Godot features. Use Godot's built-in systems wherever appropriate!

### **Decision Matrix**

| Scenario | Use Godot Features ✅ | Use EventBus ⚠️ |
|----------|----------------------|-----------------|
| **Parent-child UI communication** | ✅ Godot Signals | ❌ Overkill |
| **Scene-local events** | ✅ Godot Signals | ❌ Overkill |
| **Button clicks** | ✅ Godot Signals | ❌ Overkill |
| **Animation events** | ✅ AnimationPlayer signals | ❌ Overkill |
| **Physics collisions** | ✅ Area2D/Body2D signals | ❌ Overkill |
| **Particle effects** | ✅ Godot ParticleSystem | ❌ Overkill |
| **UI transitions** | ✅ Godot Tween/AnimationPlayer | ❌ Overkill |
| **Sound effects** | ✅ AudioStreamPlayer + signals | ❌ Overkill |
| **Cross-system state sync** | ⚠️ Complicated | ✅ EventBus |
| **Domain events affecting multiple systems** | ⚠️ Tight coupling | ✅ EventBus |
| **Actor state changes (health, status)** | ⚠️ Hard to test | ✅ EventBus |

### **Concrete Examples**

#### ✅ **Use Godot Signals** (Simple, Local, Visual)

```csharp
// Example 1: Button click in UI
public partial class MainMenu : Control
{
    [Signal]
    public delegate void StartGameRequestedEventHandler();

    private void _on_start_button_pressed()
    {
        // Emit Godot signal - parent will connect to it
        EmitSignal(SignalName.StartGameRequested);
        // NO EventBus needed!
    }
}

// Parent scene connects via editor or code:
public override void _Ready()
{
    var menu = GetNode<MainMenu>("MainMenu");
    menu.StartGameRequested += OnStartGameRequested;
}

// Example 2: Health bar tracks parent actor
public partial class HealthBar : ProgressBar
{
    [Export] public NodePath ActorPath;

    public override void _Ready()
    {
        var actor = GetNode<Actor>(ActorPath);

        // Connect to actor's signal (local parent-child relationship)
        actor.HealthChanged += (newHealth) => {
            Value = newHealth;
            Modulate = newHealth < 25 ? Colors.Red : Colors.Green;
        };
    }
}

// Example 3: Animation finished callback
public partial class AttackAnimation : AnimationPlayer
{
    [Signal]
    public delegate void AttackAnimationFinishedEventHandler();

    public override void _Ready()
    {
        AnimationFinished += (name) => {
            if (name == "attack")
                EmitSignal(SignalName.AttackAnimationFinished);
        };
    }
}
```

**Why Godot Signals Here?**
- ✅ Simple parent-child or sibling communication
- ✅ Visual wiring in Godot editor
- ✅ No business logic involved
- ✅ Fast, native, no overhead

#### ✅ **Use EventBus** (Cross-System, Domain State, Complex)

```csharp
// Example 1: Actor death affects multiple systems
// Domain/Events/ActorDiedEvent.cs
public record ActorDiedEvent(
    ActorId ActorId,
    ActorType Type,
    Vector2I Position
) : INotification;

// Multiple unrelated systems subscribe:
// - UI: Remove health bar, show death animation
// - Combat: Remove from scheduler, check victory
// - World: Update reputation, spawn loot
// - Quest: Check if quest target died
// - Audio: Play death sound based on actor type

// Without EventBus: Actor would need references to all systems (tight coupling!)
// With EventBus: Actor just publishes event, systems react independently

// Example 2: Time-unit advancement affects combat queue
public record TimeAdvancedEvent(
    int CurrentTime,
    ActorId NextActorId
) : INotification;

// Subscribers:
// - CombatUI: Update timeline visualization
// - ActorComponents: Highlight active actor
// - StatusEffects: Tick down durations
// - AI: Prepare next decision

// Example 3: Contract expired (macro layer → UI)
public record ContractExpiredEvent(
    ContractId ContractId,
    int ReputationPenalty
) : INotification;

// Subscribers in different systems:
// - WorldMap: Remove contract marker
// - QuestLog: Update quest status
// - ReputationDisplay: Show penalty notification
// - SaveSystem: Mark contract as failed
```

**Why EventBus Here?**
- ✅ Cross-system communication (combat → UI → audio → quests)
- ✅ Domain logic is publisher (testable without Godot)
- ✅ Multiple unrelated subscribers
- ✅ Loose coupling (systems don't know about each other)

### **The Rule of Thumb**

```
If it's VISUAL or LOCAL → Use Godot features
If it's DOMAIN STATE affecting multiple systems → Use EventBus

Ask yourself:
1. "Is this a Godot presentation concern?" → Godot Signals
2. "Does this involve business logic from Core?" → EventBus
3. "Would I want to test this without running Godot?" → EventBus
4. "Is this just parent-child UI communication?" → Godot Signals
```

### **Hybrid Example: Best of Both Worlds**

```csharp
// Godot component uses BOTH patterns appropriately
public partial class ActorNode : CharacterBody2D
{
    // ──────────────────────────────────────────────────────
    // Godot Signals: Local presentation events
    // ──────────────────────────────────────────────────────
    [Signal]
    public delegate void AnimationFinishedEventHandler();

    [Signal]
    public delegate void ClickedEventHandler();

    private void _on_input_event(InputEvent @event)
    {
        if (@event is InputEventMouseButton click && click.Pressed)
        {
            // Local UI event - use Godot signal
            EmitSignal(SignalName.Clicked);
        }
    }

    // ──────────────────────────────────────────────────────
    // EventBus: Domain events from Core
    // ──────────────────────────────────────────────────────
    protected override void SubscribeToEvents()
    {
        // Domain state changes - use EventBus
        EventBus.Subscribe<ActorDiedEvent>(this, OnActorDied);
        EventBus.Subscribe<HealthChangedEvent>(this, OnHealthChanged);
        EventBus.Subscribe<StatusEffectAppliedEvent>(this, OnStatusEffect);
    }

    private void OnActorDied(ActorDiedEvent e)
    {
        if (e.ActorId != _actorId) return;

        // Play death animation (Godot feature)
        _animationPlayer.Play("death");

        // Wait for animation, then clean up
        _animationPlayer.AnimationFinished += (name) => {
            if (name == "death")
                QueueFree(); // Godot cleanup
        };
    }
}
```

**Result**:
- ✅ Godot signals for presentation/input
- ✅ EventBus for domain state synchronization
- ✅ Work WITH Godot, not against it!

### **Key Takeaway**

> **"Use the right tool for the job. Godot excels at visuals and input. EventBus excels at decoupled domain state synchronization. Use both!"**

The EventBus is NOT meant to replace Godot signals - it's meant to bridge Core business logic to Godot presentation without coupling them.

---

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