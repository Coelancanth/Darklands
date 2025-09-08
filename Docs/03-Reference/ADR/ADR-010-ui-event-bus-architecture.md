# ADR-010: UI Event Bus Architecture for Domain-to-UI Event Routing

## Status
**Accepted** - 2025-09-08

## Context

We need to route domain events from our Clean Architecture domain layer (using MediatR) to Godot UI components. The challenge is a fundamental lifecycle mismatch:

- **Domain Layer**: MediatR handlers are transient, managed by DI container
- **UI Layer**: Godot nodes have their own lifecycle, managed by scene tree
- **Scale**: System must handle 200+ event types without modification
- **Current Problem**: Static event router violates SOLID principles and won't scale

### The Incident That Triggered This Decision

TD_012 attempted to remove static callbacks but broke all UI updates. Investigation revealed:
1. MediatR creates transient handler instances (by design)
2. Our singleton registration was ignored
3. GameManager exists outside DI container scope
4. Emergency fix used static handlers (anti-pattern)

## Decision

Implement a **UI Event Bus** pattern with typed subscriptions that acts as a stable bridge between MediatR domain events and Godot UI components.

### Architecture Overview

```
[Domain Layer]          [Infrastructure]         [Presentation Layer]
MediatR Events    →    UIEventBus (Singleton)  ←    Godot UI Nodes
(Transient)            (DI Container)               (Scene Tree)
                            ↑
                    Stable Reference Point
```

### Core Components

#### 1. Event Bus Interface (Core Layer)
```csharp
public interface IUIEventBus
{
    // Typed subscription for specific events
    void Subscribe<TEvent>(object subscriber, Action<TEvent> handler) 
        where TEvent : INotification;
    
    // Unsubscribe specific event type
    void Unsubscribe<TEvent>(object subscriber) 
        where TEvent : INotification;
    
    // Unsubscribe all events for a subscriber
    void UnsubscribeAll(object subscriber);
    
    // Called by MediatR handlers to publish events
    Task PublishAsync<TEvent>(TEvent notification) 
        where TEvent : INotification;
}
```

#### 2. Event Bus Implementation (Infrastructure Layer)
```csharp
public sealed class UIEventBus : IUIEventBus
{
    private readonly Dictionary<Type, List<WeakSubscription>> _subscriptions = new();
    private readonly ILogger<UIEventBus> _logger;
    private readonly UIDispatcher _dispatcher;
    
    private class WeakSubscription
    {
        public WeakReference<object> Subscriber { get; }
        public Delegate Handler { get; }
        
        public WeakSubscription(object subscriber, Delegate handler)
        {
            Subscriber = new WeakReference<object>(subscriber);
            Handler = handler;
        }
    }
    
    public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler) 
        where TEvent : INotification
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.ContainsKey(eventType))
        {
            _subscriptions[eventType] = new List<WeakSubscription>();
        }
        
        _subscriptions[eventType].Add(new WeakSubscription(subscriber, handler));
        _logger.LogDebug("Subscriber {Type} registered for {Event}", 
            subscriber.GetType().Name, eventType.Name);
    }
    
    public void Unsubscribe<TEvent>(object subscriber)
        where TEvent : INotification
    {
        var eventType = typeof(TEvent);
        if (_subscriptions.TryGetValue(eventType, out var subs))
        {
            subs.RemoveAll(s => !s.Subscriber.TryGetTarget(out var t) || ReferenceEquals(t, subscriber));
        }
    }
    
    public void UnsubscribeAll(object subscriber)
    {
        foreach (var kvp in _subscriptions)
        {
            kvp.Value.RemoveAll(s => !s.Subscriber.TryGetTarget(out var t) || ReferenceEquals(t, subscriber));
        }
    }
    
    public async Task PublishAsync<TEvent>(TEvent notification) 
        where TEvent : INotification
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.TryGetValue(eventType, out var subs))
            return;
        
        var deadSubscriptions = new List<WeakSubscription>();
        
        foreach (var sub in subs)
        {
            if (sub.Subscriber.TryGetTarget(out var target))
            {
                // Thread-safe UI update for Godot via UIDispatcher
                _dispatcher.Enqueue(() => ((Action<TEvent>)sub.Handler).Invoke(notification), target);
            }
            else
            {
                deadSubscriptions.Add(sub);
            }
        }
        
        // Clean up dead references
        foreach (var dead in deadSubscriptions)
        {
            subs.Remove(dead);
        }
    }
}
```

```csharp
/// <summary>
/// Godot-side dispatcher that marshals actions onto the main thread via CallDeferred.
/// Register a single instance early (e.g., under /root) and provide it to UIEventBus.
/// </summary>
public sealed partial class UIDispatcher : Node
{
    private readonly Queue<(object? target, Action action)> _queue = new();
    
    public void Enqueue(Action action, object? target = null)
    {
        lock (_queue)
        {
            _queue.Enqueue((target, action));
        }
        CallDeferred(nameof(Drain));
    }
    
    private void Drain()
    {
        while (true)
        {
            (object? target, Action action) item;
            lock (_queue)
            {
                if (_queue.Count == 0) break;
                item = _queue.Dequeue();
            }
            // If a target Node was provided, ensure it is still valid
            if (item.target is Node node && !node.IsInsideTree())
                continue;
            item.action();
        }
    }
}
```

#### 3. MediatR Handler Forwarder
```csharp
public class UIEventForwarder<TEvent> : INotificationHandler<TEvent> 
    where TEvent : INotification
{
    private readonly IUIEventBus _eventBus;
    
    public UIEventForwarder(IUIEventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        return _eventBus.PublishAsync(notification);
    }
}
```

#### 4. Base Class for Event-Aware UI Components
```csharp
public abstract class EventAwareNode : Node2D
{
    protected IUIEventBus EventBus { get; private set; }
    
    // Optional: Keep a reference to unsubscribe specific event types if needed
    
    
    public override void _Ready()
    {
        // Service locator pattern - necessary evil for Godot integration
        EventBus = ServiceLocator.GetRequiredService<IUIEventBus>();
        SubscribeToEvents();
    }
    
    protected abstract void SubscribeToEvents();
    
    public override void _ExitTree()
    {
        EventBus?.UnsubscribeAll(this);
    }
}
```

#### 5. Concrete UI Implementation
```csharp
public partial class GameManager : EventAwareNode
{
    protected override void SubscribeToEvents()
    {
        EventBus.Subscribe<ActorDiedEvent>(this, OnActorDied);
        EventBus.Subscribe<ActorDamagedEvent>(this, OnActorDamaged);
        EventBus.Subscribe<CombatEndedEvent>(this, OnCombatEnded);
    }
    
    private void OnActorDied(ActorDiedEvent e)
    {
        // Already on UI thread via CallDeferred
        RemoveActor(e.ActorId);
    }
    
    private void OnActorDamaged(ActorDamagedEvent e)
    {
        UpdateHealthBar(e.ActorId, e.RemainingHealth);
    }
}
```

### DI Registration
```csharp
// In ServiceConfiguration.cs
public static void ConfigureUIEventBus(this IServiceCollection services)
{
    // Register the bus as singleton
    services.AddSingleton<IUIEventBus, UIEventBus>();
    
    // Register generic forwarder for all domain events
    services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));
}
```

## Consequences

### Positive
- ✅ **Scalable**: Adding new events requires zero changes to infrastructure
- ✅ **SOLID Compliant**: Open for extension, closed for modification
- ✅ **Memory Safe**: WeakReferences prevent leaks from destroyed nodes
- ✅ **Type Safe**: Compile-time checking of event subscriptions
- ✅ **Testable**: Bus can be mocked, supports parallel test execution
- ✅ **Thread Safe**: CallDeferred ensures UI updates on main thread
- ✅ **Clean Separation**: Clear boundary between DI and Godot worlds

### Negative
- ❌ **Service Locator**: UI components use service locator pattern (necessary evil)
- ❌ **Reflection**: Some runtime reflection for generic type handling
- ❌ **Weak References**: Slight performance overhead for reference checks

### Neutral
- ➖ More complex than static handlers (but necessary for scale)
- ➖ Requires understanding of both DI and event patterns
- ➖ Additional abstraction layer between domain and UI

## Alternatives Considered

### 1. Static Event Router (Current)
- ✅ Simple, works for small scale
- ❌ Violates SOLID, doesn't scale, hard to test
- **Rejected**: Technical debt that compounds with growth

### 2. Direct MediatR Handlers in UI
- ✅ No additional abstraction
- ❌ Lifecycle mismatch causes handler instance issues
- **Rejected**: Root cause of original incident

### 3. Godot Signals Only
- ✅ Native to engine
- ❌ Duplicates event system, loses type safety
- **Rejected**: Unnecessary duplication

### 4. Reactive Extensions (Rx.NET)
- ✅ Powerful stream processing
- ❌ Over-engineered for our needs, steep learning curve
- **Rejected**: Too complex for current requirements

## Implementation Plan

### Phase 1: Core Infrastructure (Day 1)
- [ ] Create IUIEventBus interface
- [ ] Implement UIEventBus with weak references
- [ ] Create UIEventForwarder<T> handler
- [ ] Add EventAwareNode base class

### Phase 2: Migration (Day 2)
- [ ] Update GameManager to use EventAwareNode
- [ ] Migrate existing event handlers
- [ ] Remove static GameManagerEventRouter
- [ ] Update DI registration

### Phase 3: Testing & Documentation (Day 3)
- [ ] Integration tests for event flow
- [ ] Unit tests for bus implementation
- [ ] Performance tests with 100+ event types
- [ ] Update architecture documentation

## Migration Strategy

```csharp
// Step 1: Add new bus alongside static router
// Step 2: Migrate one event at a time
// Step 3: Verify each migration with tests
// Step 4: Remove static router when all migrated
```

## Success Metrics
- Zero static event handlers in codebase
- All UI updates working correctly
- Support for 200+ event types without modification
- Integration tests pass with parallel execution
- Memory profiler shows no leaks from destroyed nodes

## Related Documents
- [Post-Mortem: UI Event Routing Failure](../../06-PostMortems/Inbox/2025-09-08-ui-event-routing-failure.md)
- TD_012: Static Callback Elimination
- TD_017: Event Router Architecture Replacement
- TD_018: Integration Tests for Event Routing

## References
- [Microsoft: Domain Events Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Weak References in Event Systems](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/weak-references)

## Decision Makers
- **Author**: Tech Lead
- **Reviewed By**: Dev Engineer, Test Specialist
- **Approved By**: Tech Lead
- **Date**: 2025-09-08

## Appendix: Key Design Decisions

### Why WeakReference?
Godot nodes can be freed at any time by the scene tree. Strong references would prevent garbage collection and cause memory leaks. WeakReferences allow automatic cleanup when nodes are destroyed.

### Why CallDeferred?
Godot requires UI updates to happen on the main thread. CallDeferred ensures thread safety by marshalling the call to the next idle frame.

### Why Service Locator in _Ready?
Godot instantiates nodes via scene loading, not DI. The service locator pattern, while generally an anti-pattern, is the pragmatic bridge between these two worlds. It's localized to the EventAwareNode base class to minimize impact.

### Why Typed Subscriptions?
With 200+ events, a single HandleAsync with a giant switch statement would violate Open/Closed principle. Typed subscriptions keep each handler focused and allow compile-time verification.