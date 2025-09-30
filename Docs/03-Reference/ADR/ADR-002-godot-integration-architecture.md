# ADR-002: Godot Integration Architecture

**Status**: Approved
**Date**: 2025-09-30
**Last Updated**: 2025-09-30
**Decision Makers**: Tech Lead, Product Owner

**Changelog**:
- 2025-09-30: Added Component Lifecycle Management, State Mutation Discipline, Logging Integration, Thread Safety, Performance Considerations, Bootstrap Sequence, Async Error Handling
- 2025-09-30: **Critical Fix**: WeakReference memory leak (store MethodInfo, not delegates), ActorFactory layer clarification (Presentation not Core), complete IComponentRegistry implementation, handler error isolation

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

public partial class HealthComponentNode : EventAwareNode, IOwnerIdConsumer
{
    [Export] private ProgressBar? _healthBar;
    [Export] private Color _healthyColor = new(0, 1, 0);
    [Export] private Color _criticalColor = new(1, 0, 0);

    private IMediator? _mediator;
    private ILogger<HealthComponentNode>? _logger;
    private ActorId? _ownerId;

    public override void _Ready()
    {
        base._Ready(); // Initialize EventBus and Logger

        // Resolve services via ServiceLocator (Godot boundary pattern)
        _logger = ServiceLocator.Get<ILogger<HealthComponentNode>>();

        ServiceLocatorBridge.GetService<IMediator>()
            .Match(
                success: m => _mediator = m,
                failure: err => _logger?.LogError("Failed to resolve IMediator: {Error}", err)
            );
    }

    protected override void SubscribeToEvents()
    {
        EventBus!.Subscribe<HealthChangedEvent>(this, OnHealthChanged);
    }

    private void OnHealthChanged(HealthChangedEvent e)
    {
        if (_ownerId is null || e.ActorId != _ownerId.Value) return;

        // Update UI (pure view logic, no business logic!)
        _healthBar!.Value = e.NewHealth.Percentage * 100;
        _healthBar.Modulate = e.IsCritical ? _criticalColor : _healthyColor;
    }

    // Public API for other components/nodes
    public async Task ApplyDamage(float amount)
    {
        await _mediator!.Send(new TakeDamageCommand(_ownerId!.Value, amount));
    }

    // Provided by ActorFactory to avoid magic metadata
    public void SetOwner(ActorId actorId)
    {
        _ownerId = actorId;
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
    private readonly object _lock = new();
    private readonly Dictionary<Type, List<WeakSubscription>> _subscriptions = new();
    private readonly ILogger<GodotEventBus> _logger;

    public GodotEventBus(ILogger<GodotEventBus> logger)
    {
        _logger = logger;
    }

    private sealed class WeakSubscription
    {
        public WeakReference<object> Target { get; }
        public MethodInfo Method { get; }

        public WeakSubscription(object subscriber, MethodInfo method)
        {
            Target = new WeakReference<object>(subscriber);
            Method = method;
        }

        public bool TryInvoke<TEvent>(TEvent notification, ILogger logger)
        {
            if (!Target.TryGetTarget(out var target))
                return false;

            try
            {
                // Invoke method on target (no delegate, prevents strong reference)
                Method.Invoke(target, new object[] { notification });
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Event handler failed for {EventType}", typeof(TEvent).Name);
                return true; // Target still alive, just failed to handle
            }
        }
    }

    public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler)
        where TEvent : INotification
    {
        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var list))
            {
                list = new List<WeakSubscription>();
                _subscriptions[eventType] = list;
            }
            // Store MethodInfo, not delegate (avoids strong reference to target)
            list.Add(new WeakSubscription(subscriber, handler.Method));
        }
    }

    public void Unsubscribe<TEvent>(object subscriber)
        where TEvent : INotification
    {
        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var list))
            {
                list.RemoveAll(ws => !ws.Subscriber.TryGetTarget(out var target) || ReferenceEquals(target, subscriber));
                if (list.Count == 0) _subscriptions.Remove(eventType);
            }
        }
    }

    public void UnsubscribeAll(object subscriber)
    {
        lock (_lock)
        {
            var keys = _subscriptions.Keys.ToList();
            foreach (var key in keys)
            {
                var list = _subscriptions[key];
                list.RemoveAll(ws => !ws.Subscriber.TryGetTarget(out var target) || ReferenceEquals(target, subscriber));
                if (list.Count == 0)
                {
                    _subscriptions.Remove(key);
                }
            }
        }
    }

    public Task PublishAsync<TEvent>(TEvent notification)
        where TEvent : INotification
    {
        var eventType = typeof(TEvent);
        List<WeakSubscription> snapshot;

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var list))
                return Task.CompletedTask;

            // Prune dead references and snapshot for iteration outside the lock
            list.RemoveAll(ws => !ws.Target.TryGetTarget(out _));

            if (list.Count == 0)
            {
                _subscriptions.Remove(eventType); // Clean up empty lists
                return Task.CompletedTask;
            }

            snapshot = new List<WeakSubscription>(list);
        }

        foreach (var sub in snapshot)
        {
            if (sub.Target.TryGetTarget(out var target) && target is Node node && node.IsInsideTree())
            {
                // Thread-safe UI update for Godot: defer to main thread
                var subscription = sub; // Capture for closure
                var evt = notification; // Capture event
                Callable.From(() => subscription.TryInvoke(evt, _logger)).CallDeferred();
            }
        }

        return Task.CompletedTask;
    }
}
```

**Why WeakReference?** Godot nodes can be freed at any time. We store `MethodInfo` instead of delegates to avoid strong references (delegates capture `this`, defeating weak references).

**Why CallDeferred?** Godot requires UI updates on the main thread. CallDeferred marshals the call automatically.

**Error Handling**: Handler exceptions are caught and logged, preventing one bad handler from crashing the entire event system.

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
    protected ILogger? Logger { get; private set; }

    public override void _Ready()
    {
        // Resolve logger first for error reporting
        Logger = ServiceLocator.Get<ILogger<EventAwareNode>>();

        ServiceLocatorBridge.GetService<IGodotEventBus>()
            .Match(
                success: bus => {
                    EventBus = bus;
                    SubscribeToEvents();
                },
                failure: err => Logger?.LogError("Failed to resolve IGodotEventBus: {Error}", err)
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

### Pattern 5: Logging at Godot Boundary

**Integration with Core Logging Infrastructure**

Presentation layer nodes use the same `ILogger<T>` abstraction as Core, maintaining consistency across all layers.

**Resolve Logger in _Ready():**
```csharp
public partial class HealthBarNode : Node2D
{
    private ILogger<HealthBarNode>? _logger;

    public override void _Ready()
    {
        // Resolve via ServiceLocator (Godot boundary pattern)
        _logger = ServiceLocator.Get<ILogger<HealthBarNode>>();

        _logger?.LogDebug("HealthBarNode initialized");
    }
}
```

**Error Handling with Structured Logging:**
```csharp
ServiceLocatorBridge.GetService<IMediator>()
    .Match(
        success: m => {
            _mediator = m;
            _logger?.LogInformation("IMediator resolved successfully");
        },
        failure: err => _logger?.LogError("Failed to resolve IMediator: {Error}", err)
    );
```

**Benefits:**
- ✅ Consistent logging across Core and Presentation
- ✅ Structured logging with Serilog backend
- ✅ Centralized log aggregation and filtering
- ✅ Type-safe logger instances per node type

**Why Not GD.Print/GD.PushError?**
- ❌ Bypasses centralized logging infrastructure
- ❌ No structured data (makes debugging harder)
- ❌ No log level filtering or routing
- ❌ Inconsistent with Core layer patterns

**Exception:** Use `GD.Print()` only for quick debugging during development, never in committed code.

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
    Result RemoveAll(ActorId actorId);
}

// Infrastructure/Services/ComponentRegistry.cs
public sealed class ComponentRegistry : IComponentRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<(ActorId, Type), IComponent> _components = new();

    public Result<T> GetComponent<T>(ActorId actorId) where T : IComponent
    {
        lock (_lock)
        {
            var key = (actorId, typeof(T));
            return _components.TryGetValue(key, out var component)
                ? Result.Success((T)component)
                : Result.Failure<T>($"Component not found");
        }
    }

    public Result AddComponent<T>(ActorId actorId, T component) where T : IComponent
    {
        lock (_lock)
        {
            var key = (actorId, typeof(T));
            if (_components.ContainsKey(key))
                return Result.Failure($"Component {typeof(T).Name} already exists for actor {actorId}");

            _components[key] = component;
            return Result.Success();
        }
    }

    public Result RemoveComponent<T>(ActorId actorId) where T : IComponent
    {
        lock (_lock)
        {
            var key = (actorId, typeof(T));
            if (!_components.Remove(key))
                return Result.Failure($"Component {typeof(T).Name} not found for actor {actorId}");

            return Result.Success();
        }
    }

    public IEnumerable<T> GetAllComponents<T>() where T : IComponent
    {
        lock (_lock)
        {
            return _components.Values
                .OfType<T>()
                .ToList(); // Return copy to avoid collection modification issues
        }
    }

    public Result RemoveAll(ActorId actorId)
    {
        lock (_lock)
        {
            var keys = _components.Keys.Where(k => k.Item1.Equals(actorId)).ToList();
            foreach (var key in keys)
            {
                _components.Remove(key);
            }
            return Result.Success();
        }
    }
}
```

**Thread Safety**: All operations are protected by `lock` to prevent concurrent access issues from async command handlers.

## Component Lifecycle Management

**Critical**: Proper component lifecycle management prevents memory leaks and ensures state consistency.

### Actor/Component Creation Flow

```
1. ActorFactory creates ActorId
2. Instantiate Core components (HealthComponent, CombatComponent, etc.)
3. Register components in ComponentRegistry
4. Load Godot scene/nodes (via PackedScene.Instantiate)
5. Call SetOwner(actorId) on nodes to link View → Data
6. Add nodes to scene tree
```

**Implementation Example:**

```csharp
// Application/Factories/IActorFactory.cs
public interface IActorFactory
{
    Task<Result<ActorId>> CreateActorAsync(ActorDefinition definition, Vector2 position);
}

// Presentation/Factories/ActorFactory.cs (Godot-aware, in Presentation layer)
public partial class ActorFactory : Node, IActorFactory
{
    private readonly IComponentRegistry _registry;
    private readonly IMediator _mediator;
    private readonly ILogger<ActorFactory> _logger;

    // Constructor injection via DI
    public ActorFactory(
        IComponentRegistry registry,
        IMediator mediator,
        ILogger<ActorFactory> logger)
    {
        _registry = registry;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<ActorId>> CreateActorAsync(ActorDefinition definition, Vector2 position)
    {
        // 1. Generate unique ID
        var actorId = ActorId.New();

        // 2. Create Core components (pure C#, testable)
        var healthComponent = new HealthComponent(actorId, definition.MaxHealth);
        var combatComponent = new CombatComponent(actorId, definition.AttackPower);

        // 3. Register in ComponentRegistry (state source of truth)
        var registerResult = _registry.AddComponent(actorId, healthComponent)
            .Bind(() => _registry.AddComponent(actorId, combatComponent));

        if (registerResult.IsFailure)
        {
            _logger.LogError("Failed to register components: {Error}", registerResult.Error);
            return Result.Failure<ActorId>(registerResult.Error);
        }

        // 4. Instantiate Godot scene
        var scene = GD.Load<PackedScene>(definition.ScenePath);
        var actorNode = scene.Instantiate<ActorNode>();

        // 5. Link View → Data (critical step!)
        actorNode.SetOwner(actorId); // Implements IOwnerIdConsumer

        // 6. Position and add to scene tree (Godot API requires Node context)
        actorNode.Position = position;
        GetTree().CurrentScene.AddChild(actorNode);

        _logger.LogInformation("Actor created: {ActorId} at {Position}", actorId, position);

        // 7. Publish creation event (optional)
        await _mediator.Publish(new ActorCreatedEvent(actorId, definition.Type));

        return Result.Success(actorId);
    }
}
```

**Layer Clarification**: `ActorFactory` is in the **Presentation layer** because it:
- Uses Godot APIs (`PackedScene`, `GD.Load`, `GetTree()`)
- Instantiates and manipulates Godot nodes
- Requires `Node` context for scene tree access

**Core components** (HealthComponent, CombatComponent) remain pure C#, but their **instantiation and wiring** happens in Presentation.

### Actor/Component Cleanup Flow

**⚠️ CRITICAL**: When a Godot node is destroyed, you MUST clean up Core components to prevent memory leaks.

```csharp
// Presentation/Nodes/ActorNode.cs
public partial class ActorNode : CharacterBody2D, IOwnerIdConsumer
{
    private ActorId? _actorId;
    private IComponentRegistry? _registry;
    private ILogger<ActorNode>? _logger;

    public override void _Ready()
    {
        base._Ready();
        _registry = ServiceLocator.Get<IComponentRegistry>();
        _logger = ServiceLocator.Get<ILogger<ActorNode>>();
    }

    public override void _ExitTree()
    {
        // CRITICAL: Clean up Core components when node is destroyed
        if (_actorId.HasValue && _registry != null)
        {
            _registry.RemoveAll(_actorId.Value)
                .Match(
                    success: () => _logger?.LogDebug("Components cleaned up for {ActorId}", _actorId),
                    failure: err => _logger?.LogWarning("Failed to clean up components: {Error}", err)
                );
        }

        base._ExitTree(); // Unsubscribe from EventBus (in EventAwareNode)
    }

    public void SetOwner(ActorId actorId)
    {
        _actorId = actorId;
    }

    // When actor dies in game logic, destroy the node
    private void OnActorDied(ActorDiedEvent e)
    {
        if (e.ActorId != _actorId) return;

        _logger?.LogInformation("Actor died: {ActorId}", _actorId);

        // Play death animation, then destroy
        _animationPlayer.Play("death");
        _animationPlayer.AnimationFinished += (name) => {
            if (name == "death")
                QueueFree(); // Triggers _ExitTree → cleanup
        };
    }
}
```

### Lifecycle Best Practices

**DO**:
- ✅ Always call `ComponentRegistry.RemoveAll()` in `_ExitTree()`
- ✅ Use `IOwnerIdConsumer` pattern to link nodes to ActorId
- ✅ Create components BEFORE instantiating Godot nodes
- ✅ Log creation/destruction for debugging

**DON'T**:
- ❌ Never skip cleanup in `_ExitTree()` (causes memory leaks)
- ❌ Never create Core components directly in `_Ready()` (use factories)
- ❌ Never store ActorId in node metadata (use explicit interface)
- ❌ Never bypass factory (breaks component registration)

## DI Registration

```csharp
// Infrastructure/DependencyInjection/GameStrapper.cs
services.AddSingleton<IGodotEventBus, GodotEventBus>();
services.AddSingleton<IComponentRegistry, ComponentRegistry>();
services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));

// Logging infrastructure (Serilog + Microsoft.Extensions.Logging)
services.AddLogging(config => {
    config.AddSerilog(); // Configured in GameStrapper bootstrap
});
```

## Bootstrap Sequence

**⚠️ CRITICAL**: DI container MUST be initialized before any nodes call `ServiceLocator.Get<T>()`.

### Initialization Order Problem

Godot's `_Ready()` callbacks fire in scene tree order. If a node's `_Ready()` calls `ServiceLocator.Get<T>()` before `GameStrapper` builds the DI container, the application crashes.

### Solution: Explicit Bootstrap

**Pattern**: Initialize DI container in main scene root, then signal "system ready" to other nodes.

```csharp
// Main.cs (root node of first/startup scene)
public partial class Main : Node
{
    private ILogger<Main>? _logger;

    public override void _Ready()
    {
        GD.Print("=== Bootstrapping Application ===");

        // 1. Build DI container FIRST
        GameStrapper.Initialize();

        // 2. Mark system as ready (other nodes can check this)
        GetTree().Root.SetMeta("SystemReady", true);

        // 3. Now safe to use ServiceLocator
        _logger = ServiceLocator.Get<ILogger<Main>>();
        _logger?.LogInformation("Application bootstrap complete");

        GD.Print("=== Bootstrap Complete ===");
    }
}
```

**Defensive Pattern for EventAwareNode**:

```csharp
// Presentation/Components/EventAwareNode.cs
public abstract partial class EventAwareNode : Node2D
{
    protected IGodotEventBus? EventBus { get; private set; }
    protected ILogger? Logger { get; private set; }

    public override void _Ready()
    {
        // Defensive: Wait for system initialization
        if (!IsSystemReady())
        {
            GD.PushWarning($"{Name}: System not ready, deferring initialization");
            CallDeferred(nameof(_Ready)); // Retry next frame
            return;
        }

        InitializeServices();
    }

    private bool IsSystemReady()
    {
        return GetTree().Root.HasMeta("SystemReady");
    }

    private void InitializeServices()
    {
        // Resolve logger first for error reporting
        Logger = ServiceLocator.Get<ILogger<EventAwareNode>>();

        ServiceLocatorBridge.GetService<IGodotEventBus>()
            .Match(
                success: bus => {
                    EventBus = bus;
                    SubscribeToEvents();
                },
                failure: err => Logger?.LogError("Failed to resolve IGodotEventBus: {Error}", err)
            );
    }

    protected abstract void SubscribeToEvents();
}
```

### Bootstrap Best Practices

**DO**:
- ✅ Initialize `GameStrapper` in the root node of your first scene
- ✅ Set a global flag/metadata when bootstrap completes
- ✅ Use defensive checks in critical nodes (or accept crash-on-misconfiguration)
- ✅ Log bootstrap steps for debugging

**DON'T**:
- ❌ Initialize DI container in autoload scripts (timing is unpredictable)
- ❌ Call `ServiceLocator.Get<T>()` in static constructors
- ❌ Assume `_Ready()` order across scenes
- ❌ Silently fail if services aren't available

### Alternative: Fail Fast

**For production**, you might prefer crashing immediately over silent failures:

```csharp
public override void _Ready()
{
    // No defensive check—crash if DI not ready
    Logger = ServiceLocator.Get<ILogger<EventAwareNode>>();

    // This forces you to fix initialization order issues during development
}
```

**Trade-off**: Crash-on-error makes bugs obvious, but requires correct bootstrap setup.

## Complete Flow Example: Attack

```csharp
// 1. User clicks attack button
public partial class CombatUI : Node
{
    private IMediator? _mediator;
    private ILogger<CombatUI>? _logger;

    public override void _Ready()
    {
        _logger = ServiceLocator.Get<ILogger<CombatUI>>();

        ServiceLocatorBridge.GetService<IMediator>()
            .Match(
                success: m => _mediator = m,
                failure: err => _logger?.LogError("Failed to resolve IMediator: {Error}", err)
            );
    }

    private async void _on_attack_button_pressed()
    {
        if (_mediator is null)
        {
            _logger?.LogWarning("Cannot execute attack: IMediator not available");
            return;
        }

        await _mediator.Send(new ExecuteAttackCommand(attackerId, targetId));
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

### Summary: Bidirectional but Decoupled

| Flow | Mechanism | Purpose |
|------|-----------|---------|
| **Godot → Core** | Commands via MediatR | User actions trigger business logic |
| **Core → Godot** | Events via GodotEventBus | State changes update UI |

**Result**: Clean separation, fully testable, no circular dependencies!

## Async Signal Handler Pattern

**Problem**: Godot signal handlers require `void` return type, but you need to `await` async commands.

### The `async void` Trap

**Risk**: Unhandled exceptions in `async void` methods crash the application without stack trace.

```csharp
// ❌ DANGEROUS: Unhandled exception crashes game
private async void _on_attack_button_pressed()
{
    await _mediator.Send(new ExecuteAttackCommand(...));
    // If this throws, game crashes!
}
```

### Solution: Always Catch Exceptions

```csharp
// ✅ SAFE: Exceptions handled gracefully
private async void _on_attack_button_pressed()
{
    try
    {
        var result = await _mediator!.Send(
            new ExecuteAttackCommand(_attackerId, _targetId));

        result.Match(
            onSuccess: () => _logger?.LogInformation("Attack executed successfully"),
            onFailure: err => _logger?.LogError("Attack failed: {Error}", err));
    }
    catch (Exception ex)
    {
        // CRITICAL: Prevent application crash
        _logger?.LogError(ex, "Unhandled exception in attack handler");
        ShowErrorMessage("Attack failed due to unexpected error");
    }
}
```

### Best Practices for Async Signal Handlers

**DO**:
- ✅ Wrap entire `async void` body in try/catch
- ✅ Log exceptions with context
- ✅ Show user-friendly error messages when appropriate
- ✅ Use `Result<T>` pattern to avoid exception-based control flow

**DON'T**:
- ❌ Leave `async void` methods without try/catch
- ❌ Use `async void` outside of signal handlers (use `async Task` instead)
- ❌ Swallow exceptions silently (always log)
- ❌ Re-throw exceptions from `async void` (they crash the app)

### Helper Pattern (Optional)

Create a reusable wrapper for fire-and-forget tasks:

```csharp
// Infrastructure/Extensions/TaskExtensions.cs
public static class TaskExtensions
{
    public static async void FireAndForget(
        this Task task,
        ILogger logger,
        string operationName)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fire-and-forget task failed: {Operation}", operationName);
        }
    }
}

// Usage in signal handler
private void _on_attack_button_pressed()
{
    _mediator!.Send(new ExecuteAttackCommand(_attackerId, _targetId))
        .FireAndForget(_logger, "ExecuteAttack");
}
```

**Trade-off**: Cleaner code, but harder to handle specific errors.

## State Mutation Discipline

**⚠️ CRITICAL ARCHITECTURAL RULE**: This architecture's integrity depends on strict state mutation discipline.

### The Golden Rule

> **All state changes MUST flow through Command → Handler → Event**

**Valid Flow** ✅:
```
User Action → Command → Handler (mutates Core state) → Event → UI Update
```

**Invalid Flow** ❌:
```
User Action → Direct component mutation (bypasses handlers/events)
```

### Why This Matters

**If you bypass the Command/Event flow:**
- ❌ State changes are not logged (debugging nightmare)
- ❌ Other systems don't know state changed (UI desync)
- ❌ Can't replay or test state transitions
- ❌ Violates Single Source of Truth principle
- ❌ Architecture collapses into "big ball of mud"

### Examples of Violations

**❌ WRONG - Direct mutation in UI:**
```csharp
public partial class HealthBarNode : Node2D
{
    private void _on_heal_button_pressed()
    {
        // WRONG: Directly mutating Core component from UI!
        var health = _registry.GetComponent<HealthComponent>(_actorId);
        health.Heal(25); // Bypasses Command/Event flow
    }
}
```

**✅ CORRECT - Use Command:**
```csharp
public partial class HealthBarNode : Node2D
{
    private async void _on_heal_button_pressed()
    {
        // CORRECT: Send command, handler updates state, event notifies UI
        await _mediator.Send(new HealActorCommand(_actorId, 25));
    }
}
```

**❌ WRONG - Handler mutates without publishing event:**
```csharp
public class HealActorCommandHandler : IRequestHandler<HealActorCommand>
{
    public async Task<Result> Handle(HealActorCommand cmd)
    {
        var health = await _registry.GetComponent<HealthComponent>(cmd.ActorId);
        health.Heal(cmd.Amount); // State changed...

        return Result.Success(); // ...but no event published! UI won't update!
    }
}
```

**✅ CORRECT - Always publish event after mutation:**
```csharp
public class HealActorCommandHandler : IRequestHandler<HealActorCommand>
{
    public async Task<Result> Handle(HealActorCommand cmd)
    {
        var health = await _registry.GetComponent<HealthComponent>(cmd.ActorId);
        var result = health.Heal(cmd.Amount);

        // CRITICAL: Publish event so UI updates
        await _mediator.Publish(new HealthChangedEvent(cmd.ActorId, health.CurrentHealth));

        return result;
    }
}
```

### Enforcement Strategies

**Code Review Checklist**:
- [ ] Are all state mutations initiated by Commands?
- [ ] Do all handlers publish Events after state changes?
- [ ] Are UI components read-only (only respond to Events)?
- [ ] Is `ComponentRegistry.GetComponent()` only used in handlers, not UI?

**Architecture Tests** (recommended):
```csharp
[Test]
public void ComponentRegistry_ShouldOnlyBeAccessedFrom_ApplicationLayer()
{
    // Use NetArchTest or similar to enforce:
    // - Only Application layer can call ComponentRegistry
    // - Presentation layer can only use IMediator
}
```

**Team Discipline**:
- Treat this rule as seriously as "no null references" or "no SQL injection"
- During PR reviews, ask: "Why is this bypassing the Command flow?"
- When in doubt, add a Command (even if it feels like overhead)

## Performance Considerations

### EventBus is NOT for High-Frequency Updates

**Understanding the Cost**: `GodotEventBus` uses `lock` + list iteration + `CallDeferred`, which introduces overhead. This is acceptable for **state change events** but problematic for **per-frame updates**.

### What NOT to Use EventBus For

**❌ High-Frequency Events (Avoid)**:
```csharp
// ❌ BAD: Publishing position updates every frame
void _Process(float delta)
{
    // 100 actors × 60 fps = 6,000 events/second
    await _mediator.Publish(new ActorPositionChangedEvent(ActorId, Position));
}

// ❌ BAD: Animation state every frame
void _Process(float delta)
{
    await _mediator.Publish(new AnimationStateChangedEvent(...));
}

// ❌ BAD: Physics collision events (hundreds per second)
void OnBodyEntered(Node2D body)
{
    await _mediator.Publish(new CollisionDetectedEvent(...));
}
```

**Why it fails**: Lock contention, GC pressure, CallDeferred queue bloat → frame drops.

---

### What TO Use EventBus For

**✅ Business Logic Events (Correct Usage)**:
```csharp
// ✅ GOOD: Actor died (infrequent, important)
public async Task<Result> Handle(ExecuteAttackCommand cmd)
{
    // ... damage calculation
    if (target.Health.IsDepleted)
    {
        await _mediator.Publish(new ActorDiedEvent(target.Id)); // Low frequency
    }
}

// ✅ GOOD: Health changed (episodic, not every frame)
public async Task<Result> Handle(TakeDamageCommand cmd)
{
    var result = target.TakeDamage(cmd.Amount);
    await _mediator.Publish(new HealthChangedEvent(target.Id, target.Health));
}

// ✅ GOOD: Quest completed (rare, significant)
await _mediator.Publish(new QuestCompletedEvent(questId));

// ✅ GOOD: Turn-based combat events
await _mediator.Publish(new TurnStartedEvent(actorId));
```

**Why it works**: Low frequency (< 10 events/second typical), meaningful state changes.

---

### High-Frequency Data Patterns

**For position/animation queries, use direct access**:

```csharp
// ✅ CORRECT: Direct position access (no events)
public Vector2 GetActorPosition(ActorId id)
{
    // Option 1: Query from spatial data structure (fast)
    return _spatialIndex.GetPosition(id);

    // Option 2: Direct node reference (if available)
    return _actorNodes[id].Position;
}

// ✅ CORRECT: Godot's native systems for visuals
public partial class ActorNode : CharacterBody2D
{
    void _Process(float delta)
    {
        // Use Godot's transform system directly—no events needed
        Position = Position.MoveToward(_targetPosition, _speed * delta);

        // Animation updates via AnimationTree (Godot native)
        _animationTree.Set("parameters/movement/blend_position", Velocity);
    }
}
```

---

### Performance Guidelines

| Event Type | Frequency | EventBus | Alternative |
|------------|-----------|----------|-------------|
| Actor died | < 1/second | ✅ Yes | - |
| Health changed | 1-10/second | ✅ Yes | - |
| Combat resolved | < 1/second | ✅ Yes | - |
| Quest event | < 0.1/second | ✅ Yes | - |
| Position update | 60/second | ❌ No | Direct access or spatial index |
| Animation state | 60/second | ❌ No | Godot AnimationTree |
| Collision | 100s/second | ❌ No | Godot physics callbacks |
| UI input | 60/second | ❌ No | Godot signals |

**Rule of Thumb**: If it happens every frame or multiple times per second per actor, don't use EventBus.

---

### When Performance Becomes an Issue

**If profiling shows EventBus overhead**:

1. **Verify you're not publishing high-frequency events** (this is the usual culprit)
2. **Batch events**: Collect multiple changes, publish once per frame
3. **Filter subscribers**: Unsubscribe inactive nodes immediately
4. **Use direct references** for hot paths (sacrifice decoupling for performance)

**Don't prematurely optimize**: Start with EventBus for all state changes. Profile. Optimize only proven bottlenecks.

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
- ⚠️ **Service Locator at Boundary**: Considered anti-pattern in enterprise software (hides dependencies),
  but pragmatic for game engine integration. Godot's scene instantiation model requires this bridge.
  We limit it to presentation layer _Ready() methods—Core uses constructor injection for testability.
- ⚠️ **Reflection for Event Handlers**: Using `MethodInfo.Invoke` to avoid WeakReference leaks has ~10x overhead vs direct delegates. Acceptable for low-frequency events (< 10/sec), but not suitable for high-frequency updates.
- ❌ **Learning Curve**: Team must understand pattern
- ❌ **Event Overhead**: Reflection + weak references add cost (mitigated by event frequency guidance)

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

**Architecture Integrity**:
- ✅ Zero business logic in component nodes
- ✅ All domain components testable without Godot
- ✅ All state mutations follow Command → Handler → Event flow
- ✅ Consistent logging infrastructure across Core and Presentation layers

**Lifecycle & Safety**:
- ✅ No memory leaks from destroyed nodes (verified via `_ExitTree()` cleanup)
- ✅ Proper component lifecycle management (creation via factory, cleanup in `_ExitTree()`)
- ✅ `ComponentRegistry` operations are thread-safe
- ✅ DI container initialized before any ServiceLocator calls (bootstrap sequence verified)

**Event System**:
- ✅ Events delivered to correct subscribers only
- ✅ Thread-safe UI updates (CallDeferred for cross-thread)
- ✅ No high-frequency events published through EventBus (position, animation, etc.)
- ✅ EventBus used only for business logic state changes (< 10/sec typical)

**Error Handling**:
- ✅ All `async void` signal handlers wrapped in try/catch
- ✅ Unhandled exceptions logged, never crash application silently

## References

- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html) - Martin Fowler
- [Domain Events Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation) - Microsoft
- [Service Locator (Anti-)Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) - Mark Seemann