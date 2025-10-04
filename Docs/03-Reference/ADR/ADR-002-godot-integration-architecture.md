# ADR-002: Godot Integration Architecture

**Status**: Approved (Refined 2025-09-30)
**Date**: 2025-09-30
**Last Updated**: 2025-09-30 (Strong references refinement)
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
    private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();
    private readonly object _lock = new();

    private class Subscription
    {
        public object Subscriber { get; }
        public Delegate Handler { get; }

        public Subscription(object subscriber, Delegate handler)
        {
            Subscriber = subscriber;
            Handler = handler;
        }
    }

    public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler)
        where TEvent : INotification
    {
        lock (_lock)
        {
            var eventType = typeof(TEvent);
            if (!_subscriptions.ContainsKey(eventType))
                _subscriptions[eventType] = new();

            _subscriptions[eventType].Add(new Subscription(subscriber, handler));
        }
    }

    public void Unsubscribe<TEvent>(object subscriber)
        where TEvent : INotification
    {
        lock (_lock)
        {
            var eventType = typeof(TEvent);
            if (_subscriptions.TryGetValue(eventType, out var subs))
            {
                subs.RemoveAll(s => ReferenceEquals(s.Subscriber, subscriber));
            }
        }
    }

    public void UnsubscribeAll(object subscriber)
    {
        lock (_lock)
        {
            foreach (var kvp in _subscriptions)
            {
                kvp.Value.RemoveAll(s => ReferenceEquals(s.Subscriber, subscriber));
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent notification)
        where TEvent : INotification
    {
        List<Subscription> subscribers;
        lock (_lock)
        {
            var eventType = typeof(TEvent);
            if (!_subscriptions.TryGetValue(eventType, out var subs))
                return;

            // Copy to avoid holding lock during callbacks
            subscribers = new List<Subscription>(subs);
        }

        foreach (var sub in subscribers)
        {
            try
            {
                // Thread-safe UI update for Godot
                if (sub.Subscriber is Node godotNode && godotNode.IsInsideTree())
                {
                    godotNode.CallDeferred(() =>
                        ((Action<TEvent>)sub.Handler).Invoke(notification));
                }
            }
            catch (Exception ex)
            {
                // Prevent one handler error from breaking others
                GD.PrintErr($"EventBus handler error: {ex.Message}");
            }
        }
    }
}
```

**Why Strong References (Not WeakReferences)?**
- **EventAwareNode Pattern Guarantees Explicit Lifecycle**: Nodes subscribe in `_Ready()` (must be in tree) and unsubscribe in `_ExitTree()` (fires before GC)
- **Simpler & More Debuggable**: No cleanup logic, no `TryGetTarget()` checks, visible leaks teach correct usage
- **Predictable**: Deterministic unsubscribe vs GC timing uncertainty
- **Safe**: If someone bypasses EventAwareNode pattern, visible leak is better than silent failure

**Refined After**: Ultra-analysis during VS_004 breakdown (2025-09-30) - Dev Engineer questioned cleanup strategy, Tech Lead traced Godot node lifecycle

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

## Presentation/Logic Boundary - SSOT Principle

**Added**: 2025-10-04 (Lessons from TD_004 - Moving ALL Business Logic to Core)

### Core Principle: Single Source of Truth (SSOT)

**CRITICAL**: Business logic must exist in EXACTLY ONE place - the Core layer.

**Presentation Layer Responsibilities** (ONLY):
- ✅ Capture user input (mouse position → GridPosition conversion)
- ✅ Send queries/commands to Core via MediatR
- ✅ Render visual output using results from Core
- ✅ Handle Godot-specific APIs (TextureRect, Sprite2D, pixel coordinates)
- ❌ **NEVER** duplicate business logic calculations
- ❌ **NEVER** make business decisions (what/when/how)

**Core Layer Responsibilities**:
- ✅ ALL business logic (shape rotation, validation, calculations)
- ✅ ALL business rules (equipment slot behavior, placement rules)
- ✅ ALL domain decisions (swap vs move, centering vs origin)
- ✅ Return **results** (cell positions, render positions, validation states)

### The Seven Logic Leaks - Real Examples from TD_004

**Context**: During TD_004 analysis (2025-10-04), found **7 distinct business logic violations** in `SpatialInventoryContainerNode` (500+ lines of logic in Presentation). All were eliminated by creating Core queries/commands.

#### Leak #1: Shape Calculation ❌ → ✅ Core Query

**BEFORE (Presentation doing business logic)**:
```csharp
// ❌ Components/SpatialInventoryContainerNode.cs (lines 1057-1075)
private void RenderDragHighlight(GridPosition origin, ItemId itemId, Rotation rotation)
{
    // BUSINESS LOGIC: Rotating item shape
    var baseShape = _itemShapes[itemId];
    var rotatedShape = baseShape.RotateClockwise(rotation);  // ← CORE LOGIC!

    // BUSINESS RULE: Equipment slot override
    bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly;
    if (isEquipmentSlot)
        rotatedShape = ItemShape.CreateRectangle(1, 1).Value;  // ← DOMAIN RULE!

    // Finally render...
    foreach (var offset in rotatedShape.OccupiedCells) { ... }
}
```

**AFTER (Core provides results, Presentation renders)**:
```csharp
// ✅ Core/Application/Queries/CalculateHighlightCellsQuery.cs
public record CalculateHighlightCellsQuery(
    ActorId ContainerId,
    ItemId ItemId,
    GridPosition Position,
    Rotation Rotation
) : IRequest<Result<List<GridPosition>>>;

public class CalculateHighlightCellsQueryHandler
{
    public async Task<Result<List<GridPosition>>> Handle(...)
    {
        var inventory = await _inventories.GetByActorIdAsync(cmd.ContainerId);
        var item = await _items.GetByIdAsync(cmd.ItemId);

        // CORE OWNS: Shape rotation logic
        var rotatedShape = item.Shape.RotateClockwise(cmd.Rotation);

        // CORE OWNS: Equipment slot business rule
        if (inventory.ContainerType == ContainerType.WeaponOnly)
            rotatedShape = ItemShape.CreateRectangle(1, 1).Value;

        // Return absolute cell positions (WHAT to render)
        return rotatedShape.OccupiedCells
            .Select(offset => new GridPosition(
                cmd.Position.X + offset.X,
                cmd.Position.Y + offset.Y))
            .ToList();
    }
}

// ✅ Presentation: Dumb rendering (ONLY pixel math)
private async void RenderDragHighlight(GridPosition origin, ItemId itemId, Rotation rotation, bool isValid)
{
    // Ask Core: "Which cells should I highlight?"
    var query = new CalculateHighlightCellsQuery(OwnerActorId.Value, itemId, origin, rotation);
    var result = await _mediator.Send(query);

    if (result.IsFailure) return;

    // ONLY pixel math: Grid coordinates → Screen coordinates
    foreach (var cellPos in result.Value)
    {
        var pixelX = cellPos.X * CellSize;
        var pixelY = cellPos.Y * CellSize;
        var highlight = CreateHighlightSprite(pixelX, pixelY, isValid ? Colors.Green : Colors.Red);
        _highlightContainer.AddChild(highlight);
    }
}
```

**Result**: 27 lines of business logic eliminated → Pure rendering with Core query

---

#### Leak #2: Occupied Cell Calculation ❌ → ✅ Core Query

**BEFORE (Duplicating Core's collision logic)**:
```csharp
// ❌ Components/SpatialInventoryContainerNode.cs (lines 640-683)
private async Task<Dictionary<GridPosition, ItemId>> CalculateOccupiedCells()
{
    var occupied = new Dictionary<GridPosition, ItemId>();

    foreach (var (itemId, itemPos) in _itemsAtPositions)
    {
        // DUPLICATE: Same shape rotation logic as Core's Inventory.PlaceItemAt()
        var baseShape = _itemShapes[itemId];
        var rotation = _itemRotations[itemId];
        var rotatedShape = baseShape.RotateClockwise(rotation);  // ← DUPLICATED!

        // DUPLICATE: Same cell iteration as Core
        foreach (var offset in rotatedShape.OccupiedCells)
        {
            var cellPos = new GridPosition(itemPos.X + offset.X, itemPos.Y + offset.Y);
            occupied[cellPos] = itemId;
        }
    }

    return occupied;
}
```

**AFTER (Core is single source of truth)**:
```csharp
// ✅ Core/Application/Queries/GetOccupiedCellsQuery.cs
public class GetOccupiedCellsQueryHandler
{
    public async Task<Result<Dictionary<GridPosition, ItemId>>> Handle(...)
    {
        var inventory = await _inventories.GetByActorIdAsync(cmd.ContainerId);

        // CORE OWNS: Occupied cell calculation (SSOT!)
        return inventory.GetOccupiedCells(); // Domain method, already tested
    }
}

// ✅ Presentation: Just uses Core's result
private async Task RefreshItemDisplay()
{
    var query = new GetOccupiedCellsQuery(OwnerActorId.Value);
    var result = await _mediator.Send(query);

    // No calculation, just render what Core provides
    foreach (var (cellPos, itemId) in result.Value)
    {
        RenderItemAtCell(itemId, cellPos);
    }
}
```

**Result**: 43 lines eliminated, zero logic duplication

---

#### Leak #3: Equipment Slot Centering ❌ → ✅ Core Query

**BEFORE (Presentation decides centering rule)**:
```csharp
// ❌ Components/SpatialInventoryContainerNode.cs (lines 853-871)
private Vector2 CalculateItemRenderPosition(ItemId itemId, GridPosition origin)
{
    var basePixelX = origin.X * CellSize;
    var basePixelY = origin.Y * CellSize;

    // BUSINESS RULE: Equipment slots center items (domain knowledge!)
    bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly;
    if (isEquipmentSlot)
    {
        // BUSINESS LOGIC: Calculate center offset based on item size
        var (itemWidth, itemHeight) = _itemDimensions[itemId];
        var rotation = _itemRotations[itemId];

        // Rotation math (business logic!)
        var displayWidth = rotation.IsVertical ? itemHeight : itemWidth;
        var displayHeight = rotation.IsVertical ? itemWidth : itemHeight;

        var centerOffsetX = (CellSize - displayWidth * CellSize) / 2;
        var centerOffsetY = (CellSize - displayHeight * CellSize) / 2;

        return new Vector2(basePixelX + centerOffsetX, basePixelY + centerOffsetY);
    }

    return new Vector2(basePixelX, basePixelY);
}
```

**AFTER (Core provides render position)**:
```csharp
// ✅ Core/Application/Queries/GetItemRenderPositionQuery.cs
public record ItemRenderPositionDto(
    GridPosition RenderOrigin,      // Where to render (grid coords)
    GridOffset CenterOffset,        // Offset for centering (grid units)
    bool ShouldCenterInSlot         // Business rule flag
);

public class GetItemRenderPositionQueryHandler
{
    public async Task<Result<ItemRenderPositionDto>> Handle(...)
    {
        var inventory = await _inventories.GetByActorIdAsync(cmd.ContainerId);
        var item = await _items.GetByIdAsync(cmd.ItemId);

        // CORE OWNS: Centering business rule
        bool shouldCenter = inventory.ContainerType == ContainerType.WeaponOnly;

        GridOffset centerOffset = GridOffset.Zero;
        if (shouldCenter)
        {
            // CORE OWNS: Center offset calculation
            var rotatedShape = item.Shape.RotateClockwise(inventory.GetItemRotation(cmd.ItemId));
            var (width, height) = rotatedShape.Dimensions;
            centerOffset = new GridOffset(
                (1 - width) / 2.0f,
                (1 - height) / 2.0f
            );
        }

        return new ItemRenderPositionDto(cmd.Position, centerOffset, shouldCenter);
    }
}

// ✅ Presentation: ONLY pixel conversion
private async Task RenderItem(ItemId itemId, GridPosition gridPos)
{
    var query = new GetItemRenderPositionQuery(OwnerActorId.Value, itemId, gridPos);
    var renderData = await _mediator.Send(query);

    // ONLY pixel math (Grid → Pixel conversion)
    var pixelX = (gridPos.X + renderData.Value.CenterOffset.X) * CellSize;
    var pixelY = (gridPos.Y + renderData.Value.CenterOffset.Y) * CellSize;

    sprite.Position = new Vector2(pixelX, pixelY);
}
```

**Result**: 19 lines eliminated, business rule centralized in Core

---

#### Leak #4: Equipment Slot Detection (Scattered Business Rules) ❌ → ✅ Domain Property

**BEFORE (Inconsistent checks scattered across file)**:
```csharp
// ❌ Lines 478, 855, 1069 - THREE different checks!

// Line 478: Just container type
bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly;

// Line 855: Container type + grid size check
bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly &&
                        _gridWidth == 1 && _gridHeight == 1;

// Line 1069: Back to just container type
bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly;
```

**AFTER (Single domain property)**:
```csharp
// ✅ Core/Domain/Inventory.cs
public class Inventory
{
    public ContainerType ContainerType { get; }

    // DOMAIN PROPERTY: What is an equipment slot?
    public bool IsEquipmentSlot => ContainerType == ContainerType.WeaponOnly;
}

// ✅ Presentation: Uses domain property (no duplication)
if (inventory.IsEquipmentSlot) { ... }
```

**Result**: Business rule defined ONCE in Domain, Presentation just reads it

---

#### Leak #5: Swap Logic ❌ → ✅ Core Command

**BEFORE (78 lines of swap implementation in Presentation)**:
```csharp
// ❌ Components/SpatialInventoryContainerNode.cs (lines 1122-1202)
private async void SwapItemsSafeAsync(
    ActorId sourceActorId, ItemId sourceItemId, GridPosition sourcePos,
    ActorId targetActorId, ItemId targetItemId, GridPosition targetPos, Rotation rotation)
{
    // BUSINESS DECISION: Determine if swap is needed
    bool isSameContainer = sourceActorId == targetActorId;

    // BUSINESS LOGIC: Swap with rollback (78 lines!)
    try
    {
        // Step 1: Validate both items can be placed
        var sourceCanPlace = await _mediator.Send(
            new CanPlaceItemAtQuery(targetActorId, sourceItemId, targetPos, rotation));
        var targetCanPlace = await _mediator.Send(
            new CanPlaceItemAtQuery(sourceActorId, targetItemId, sourcePos, Rotation.Degrees0));

        if (sourceCanPlace.IsFailure || targetCanPlace.IsFailure)
        {
            _logger.LogWarning("Swap validation failed");
            return;
        }

        // Step 2: Remove both items (create rollback state)
        var removeSource = await _mediator.Send(
            new RemoveItemFromInventoryCommand(sourceActorId, sourceItemId));
        var removeTarget = await _mediator.Send(
            new RemoveItemFromInventoryCommand(targetActorId, targetItemId));

        if (removeSource.IsFailure || removeTarget.IsFailure)
        {
            _logger.LogError("Swap removal failed - attempting rollback");
            // Rollback logic... (20+ more lines)
            return;
        }

        // Step 3: Place in swapped positions
        var placeSource = await _mediator.Send(
            new PlaceItemAtPositionCommand(targetActorId, sourceItemId, targetPos, rotation));
        var placeTarget = await _mediator.Send(
            new PlaceItemAtPositionCommand(sourceActorId, targetItemId, sourcePos, Rotation.Degrees0));

        if (placeSource.IsFailure || placeTarget.IsFailure)
        {
            _logger.LogError("Swap placement failed - attempting rollback");
            // More rollback logic... (20+ more lines)
            return;
        }

        _logger.LogInformation("Swap succeeded: {ItemA} ↔ {ItemB}", sourceItemId, targetItemId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Swap failed with exception");
    }
}
```

**AFTER (Core command owns ALL swap logic)**:
```csharp
// ✅ Core/Application/Commands/SwapItemsCommand.cs
public class SwapItemsCommandHandler
{
    public async Task<Result> Handle(SwapItemsCommand cmd, CancellationToken ct)
    {
        // CORE OWNS: Validation
        var sourceInventory = await _inventories.GetByActorIdAsync(cmd.SourceContainerId, ct);
        var targetInventory = await _inventories.GetByActorIdAsync(cmd.TargetContainerId, ct);

        // Validate both placements (business logic)
        var sourceCanPlace = sourceInventory.CanPlaceItemAt(cmd.TargetItemId, cmd.SourcePos);
        var targetCanPlace = targetInventory.CanPlaceItemAt(cmd.SourceItemId, cmd.TargetPos, cmd.Rotation);

        if (sourceCanPlace.IsFailure || targetCanPlace.IsFailure)
            return Result.Failure("Swap validation failed");

        // CORE OWNS: Atomic swap with rollback
        return await Result.Try(async () =>
        {
            // Remove both
            sourceInventory.RemoveItem(cmd.SourceItemId);
            targetInventory.RemoveItem(cmd.TargetItemId);

            // Place swapped
            sourceInventory.PlaceItemAt(cmd.TargetItemId, cmd.SourcePos, Rotation.Degrees0);
            targetInventory.PlaceItemAt(cmd.SourceItemId, cmd.TargetPos, cmd.Rotation);

            // Save (transaction-like behavior)
            bool isSameContainer = cmd.SourceContainerId == cmd.TargetContainerId;
            if (isSameContainer)
            {
                await _inventories.SaveAsync(sourceInventory, ct);
            }
            else
            {
                await _inventories.SaveAsync(sourceInventory, ct);
                await _inventories.SaveAsync(targetInventory, ct);
            }

            return Result.Success();
        })
        .OnFailure(() => {
            // Automatic rollback via repository pattern
            _logger.LogError("Swap failed - rolling back");
        });
    }
}

// ✅ Presentation: 78 lines → 23 lines (just delegates)
private async void SwapItemsSafeAsync(...)
{
    var command = new SwapItemsCommand(
        sourceActorId, sourceItemId, sourcePos,
        targetActorId, targetItemId, targetPos, rotation);

    var result = await _mediator.Send(command);

    if (result.IsFailure)
        _logger.LogWarning("Swap failed: {Error}", result.Error);
}
```

**Result**: 55 lines eliminated, complex swap logic testable in isolation

**Bug Found During TD_004**: Original handler saved same inventory twice in same-container swaps → item duplication. Fixed in commit `4cd1dbe`.

---

#### Leak #6: Type Validation (Dead Code) ❌ → ✅ Removed

**BEFORE (Dead validation method)**:
```csharp
// ❌ Components/SpatialInventoryContainerNode.cs (lines 1248-1258)
private bool CanAcceptItemType(string itemType)
{
    // BUSINESS RULE: Weapon-only container validation
    if (_containerType == ContainerType.WeaponOnly)
        return itemType == "weapon";

    return true; // General inventory accepts all
}
```

**AFTER**: Method removed entirely (already delegated to `CanPlaceItemAtQuery` at line 368).

**Result**: 11 lines of dead code removed

---

#### Leak #7: Cache-Driven Anti-Pattern ❌ → ✅ Direct DTO Access

**BEFORE (Cache dictionaries recalculate business logic)**:
```csharp
// ❌ Components/SpatialInventoryContainerNode.cs (lines 85-88, 600-627)
private Dictionary<ItemId, (int, int)> _itemDimensions = new();
private Dictionary<ItemId, ItemShape> _itemShapes = new();
private Dictionary<ItemId, Rotation> _itemRotations = new();

private async Task RefreshInventoryState()
{
    var query = new GetInventoryStateQuery(OwnerActorId.Value);
    var result = await _mediator.Send(query);

    // Cache Core's data
    _itemDimensions = result.ItemDimensions;
    _itemShapes = result.ItemShapes;
    _itemRotations = result.ItemRotations;

    // Then RECALCULATE business logic using cached data (anti-pattern!)
    var rotatedShape = _itemShapes[itemId].RotateClockwise(_itemRotations[itemId]);
    // ... more calculations
}
```

**AFTER (Store DTO, query Core for calculations)**:
```csharp
// ✅ Components/SpatialInventoryContainerNode.cs
private InventoryDto? _currentInventory = null;  // Single source

private async Task RefreshInventoryState()
{
    var query = new GetInventoryStateQuery(OwnerActorId.Value);
    var result = await _mediator.Send(query);

    _currentInventory = result.Value;  // Store DTO

    // Don't recalculate - query Core when needed!
    var highlightQuery = new CalculateHighlightCellsQuery(...);
    var cells = await _mediator.Send(highlightQuery);  // Core calculates
}
```

**Result**: 31 lines eliminated, zero performance impact (same data access, no extra queries)

---

### Pattern: Query-Based Architecture (Not Cache-Based)

**Anti-Pattern** (Cache-Driven):
```
Presentation:
1. Query Core for state (items, shapes, rotations)
2. Cache state in dictionaries
3. RECALCULATE business logic using cached data ← LEAK!
```

**Correct Pattern** (Query-Based):
```
Presentation:
1. Query Core for RESULTS (highlight cells, render positions)
2. Store minimal state (current DTO for reference)
3. RENDER results (pixel math only) ← PURE!
```

### Performance Considerations

**Myth**: "Querying Core for every mouse move is too slow"

**Reality** (TD_004 measurements):
- Query overhead: ~0.5ms per call
- Mouse move events: ~30/sec during drag
- Total cost: 15ms/sec out of 16.67ms/frame budget = **3% overhead**
- **Verdict**: Imperceptible, architectural purity justified

### Acceptable "Routing Logic" Exception

**Context**: 4 lines remain in Presentation (lines 480-495) that decide "swap vs move" based on container type.

**Current Code**:
```csharp
bool isOccupied = _itemsAtPositions.TryGetValue(targetPos.Value, out var targetItemId);
bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly;

if (isOccupied && isEquipmentSlot)
    SwapItemsSafeAsync(...);  // Route to swap command
else
    MoveItemAsync(...);       // Route to move command
```

**Tech Lead Decision**: **Acceptable as "command routing"**
- ✅ ALL swap business logic is in `SwapItemsCommand` (validation, rollback)
- ✅ Routing is thin (4 lines vs original 78 lines of swap implementation)
- ✅ Alternative (unified command) has low ROI for 4 lines

**Documented Pattern**: Presentation may route to appropriate commands based on simple conditions, but NEVER implement business logic itself.

---

### TD_004 Summary: Before/After

**BEFORE TD_004**:
- 1372 lines in SpatialInventoryContainerNode
- 500+ lines of business logic in Presentation
- 7 distinct SSOT violations
- Logic duplicated between Core and Presentation

**AFTER TD_004**:
- 1208 lines (-12% complexity)
- 164 lines of business logic eliminated
- 3 new Core queries/commands created
- Presentation is pure rendering layer

**Key Lesson**: **SSOT is non-negotiable**. If Presentation calculates business logic, it WILL diverge from Core over time.

### Enforcement Checklist

**Before merging ANY Presentation code, verify:**
1. ✅ No shape rotation logic in Presentation
2. ✅ No business rule conditionals (e.g., "if equipment slot, do X")
3. ✅ No domain calculations (centering, occupied cells, collision)
4. ✅ Only pixel math (Grid → Screen coordinate conversion)
5. ✅ Queries Core for RESULTS, doesn't recalculate Core's logic
6. ✅ No cache dictionaries that duplicate Core's state

**Grep Commands** (must return 0 results):
```bash
# No shape rotation in Presentation
grep -r "RotateClockwise\|RotateCounterclockwise" Components/

# No equipment slot business rules
grep -r "ContainerType.WeaponOnly.*if\|isEquipmentSlot.*=" Components/

# No occupied cell calculations
grep -r "OccupiedCells.*foreach\|CalculateOccupied" Components/
```

## References

- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html) - Martin Fowler
- [Domain Events Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation) - Microsoft
- [Service Locator (Anti-)Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) - Mark Seemann
- [Single Source of Truth (SSOT)](https://en.wikipedia.org/wiki/Single_source_of_truth) - Wikipedia