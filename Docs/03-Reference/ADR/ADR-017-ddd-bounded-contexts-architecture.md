# ADR-017: DDD Bounded Contexts Architecture

## Status
**Status**: Proposed  
**Date**: 2025-09-12  
**Updated**: 2025-09-12 (Simplified with pragmatic patterns for single-player game)
**Decision Makers**: Tech Lead, Dev Engineer  
**Supersedes**: Enhances ADR-015 (Namespace Organization)
**Key Enhancement**: Contracts assembly pattern for true module isolation
**Pragmatic Focus**: Single MediatR with interface differentiation (no over-engineering)
**Quick Guide**: [DDD Feature Implementation Protocol](./DDD-Feature-Implementation-Protocol.md)

## Context

### The Problem
Our current monolithic domain structure creates several architectural issues:
1. **Mixed Concerns**: Tactical game logic, diagnostics, and platform abstractions in one domain
2. **Determinism Conflicts**: Performance monitoring needs DateTime/double, violating ADR-004
3. **Architecture Violations**: Application layer forced to reference Infrastructure for legitimate monitoring
4. **Unclear Boundaries**: No separation between core game logic and cross-cutting concerns
5. **Testing Complexity**: Can't apply different rules to different parts of the system
6. **Accidental Coupling**: Easy to reference wrong types across logical boundaries
7. **Feature Organization Confusion**: Unclear where new features should be implemented

### Pragmatic Approach
We adopt patterns that solve REAL problems for a single-player monolithic game, avoiding over-engineering:
- ✅ **DO**: Module isolation, type safety, clear boundaries
- ❌ **DON'T**: Distributed system patterns (Outbox/Inbox), complex event sourcing
- 📋 **GUIDE**: [DDD Feature Implementation Protocol](./DDD-Feature-Implementation-Protocol.md) for clear decisions

### Current Pain Points
```csharp
// Current: Everything in one domain with conflicting requirements
Domain.Vision.VisionState        // Must be deterministic (game logic)
Domain.Vision.VisionPerformance  // Needs DateTime/double (monitoring)
Domain.Services.IAudioService    // Platform concern (not domain logic)
Domain.Debug.ICategoryLogger     // Infrastructure concern in domain
```

### DDD Principles Being Violated
- **Bounded Context**: Different models for different contexts
- **Ubiquitous Language**: Terms mean different things in different contexts  
- **Context Mapping**: No clear boundaries or integration points
- **Aggregate Boundaries**: Entities spread across multiple namespaces

## Decision

We will reorganize our architecture into **proper DDD bounded contexts with assembly boundaries**, each with its own domain model, rules, and architectural constraints.

### Assembly-Based Bounded Context Structure

```
src/
├── Tactical/                                   # Core game mechanics
│   ├── Darklands.Tactical.Domain.csproj       # STRICT determinism enforced
│   ├── Darklands.Tactical.Application.csproj  # Commands, Queries, Handlers
│   ├── Darklands.Tactical.Infrastructure.csproj # Repositories, Services
│   └── Darklands.Tactical.Contracts.csproj    # PUBLIC API for other contexts
│
├── Diagnostics/                                # Monitoring & Debug
│   ├── Darklands.Diagnostics.Domain.csproj    # DateTime/double allowed
│   ├── Darklands.Diagnostics.Application.csproj # Monitoring queries
│   ├── Darklands.Diagnostics.Infrastructure.csproj # Loggers, Monitors
│   └── Darklands.Diagnostics.Contracts.csproj # PUBLIC API for other contexts
│
├── Platform/                                   # External integrations
│   ├── Darklands.Platform.Domain.csproj       # Service interfaces
│   ├── Darklands.Platform.Infrastructure.Godot.csproj # Godot implementations
│   └── Darklands.Platform.Contracts.csproj    # PUBLIC API for other contexts
│
├── SharedKernel/                               # Minimal shared types
│   ├── Darklands.SharedKernel.Domain.csproj   # Entity, ValueObject, IBusinessRule, IDomainEvent
│   ├── Darklands.SharedKernel.Application.csproj # ICommand, IQuery, IApplicationNotification
│   └── Darklands.SharedKernel.Infrastructure.csproj # IIntegrationEvent
│
└── Darklands.csproj                           # Main Godot project
    └── Presentation/                           # All Godot nodes here
        ├── Views/
        ├── Presenters/
        └── Bootstrapper.cs                     # DI composition root
```

### Assembly References - The Critical Pattern
```xml
<!-- Darklands.csproj (Main Godot Project) -->
<ItemGroup>
  <ProjectReference Include="src/Tactical/Darklands.Tactical.Application.csproj" />
  <ProjectReference Include="src/Diagnostics/Darklands.Diagnostics.Application.csproj" />
  <ProjectReference Include="src/Platform/Darklands.Platform.Infrastructure.Godot.csproj" />
  <ProjectReference Include="src/SharedKernel/Darklands.SharedKernel.Domain.csproj" />
  <ProjectReference Include="src/SharedKernel/Darklands.SharedKernel.Infrastructure.csproj" />
</ItemGroup>

<!-- CRITICAL: Modules reference ONLY Contracts from other modules -->
<!-- Diagnostics.Application.csproj -->
<ItemGroup>
  <!-- Can subscribe to Tactical events WITHOUT accessing Tactical internals! -->
  <ProjectReference Include="../../Tactical/Contracts/Darklands.Tactical.Contracts.csproj" />
  <!-- But CANNOT reference Tactical.Domain, Application, or Infrastructure -->
</ItemGroup>

<!-- Tactical.Application.csproj -->
<ItemGroup>
  <!-- Can subscribe to Diagnostics events if needed -->
  <ProjectReference Include="../../Diagnostics/Contracts/Darklands.Diagnostics.Contracts.csproj" />
  <!-- Complete isolation of implementation details -->
</ItemGroup>
```

## Shared Identity Strategy

### Problem: Context Isolation
Different contexts must NOT reference each other's domain types. Using `ActorId` from Tactical in Diagnostics violates isolation.

### Solution: Strongly-Typed Identity Pattern
```csharp
// SharedKernel/Domain/TypedId.cs - Type-safe implementation
namespace Darklands.SharedKernel.Domain
{
    public abstract class TypedId<TSelf> : IEquatable<TSelf>
        where TSelf : TypedId<TSelf>
    {
        public Guid Value { get; }
        
        protected TypedId(Guid value)
        {
            if (value == Guid.Empty)
                throw new InvalidOperationException("Id value cannot be empty");
            Value = value;
        }
        
        public bool Equals(TSelf? other) => 
            other is not null && other.Value == Value;
        
        public override bool Equals(object? obj) => 
            obj is TSelf other && Equals(other);
        
        public override int GetHashCode() => 
            HashCode.Combine(typeof(TSelf), Value);
        
        public override string ToString() => Value.ToString("N")[..8];
        
        public static bool operator ==(TypedId<TSelf>? left, TypedId<TSelf>? right) => 
            EqualityComparer<TypedId<TSelf>>.Default.Equals(left, right);
        
        public static bool operator !=(TypedId<TSelf>? left, TypedId<TSelf>? right) => 
            !(left == right);
    }
}

// SharedKernel/Identity/EntityId.cs - For cross-context communication
namespace Darklands.SharedKernel.Identity
{
    public sealed class EntityId : TypedId<EntityId>
    {
        public EntityId(Guid value) : base(value) { }
        public static EntityId NewId() => new(Guid.NewGuid());
    }
}

// Tactical.Domain uses internal ActorId 
namespace Darklands.Tactical.Domain.Entities
{
    public readonly record struct ActorId(Guid Value)
    {
        public static ActorId New() => new(Guid.NewGuid());
        public bool IsEmpty => Value == Guid.Empty;
        
        // Convert to cross-context EntityId when needed
        public EntityId ToEntityId() => new(Value);
        public static ActorId FromEntityId(EntityId entityId) => new(entityId.Value);
    }
}

// Diagnostics uses EntityId directly (never ActorId)
namespace Darklands.Diagnostics.Domain.Performance
{
    public record VisionPerformanceReport(
        DateTime Timestamp,
        double CalculationTimeMs,
        Dictionary<EntityId, double> Metrics  // ✅ EntityId, not ActorId!
    );
}
```

## Event Architecture - Single MediatR + Interfaces (CLARIFIED)

### Single MediatR Instance with Multiple Event Types
**DECISION**: We use ONE MediatR instance for ALL event types, distinguished by interfaces:

- **Domain Events** (`IDomainEvent`) - Internal to bounded contexts
- **Contract Events** (`IContractEvent`) - Cross-context communication  
- **Application Notifications** (`IApplicationNotification`) - UI updates

**NO separate Integration Event Bus** - MediatR handles all event types through different interfaces.

```csharp
// 1. Domain Events (Internal) - Stay within context
namespace Darklands.Tactical.Domain.Events
{
    public record ActorDamagedEvent(ActorId Actor, int Damage, GameTick OccurredAt) : IDomainEvent;
    // Published via IMediator, handled within Tactical context only
}

// 2. Contract Events (Public API) - Can cross context boundaries
namespace Darklands.Tactical.Contracts.Events
{
    public sealed record ActorDamagedContractEvent(
        EntityId EntityId,       // SharedKernel type for consistency
        int Damage,
        string ActorName         // Additional context for other modules
    ) : IContractEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public int Version { get; } = 1;
    }
    // Also published via IMediator, but in Contracts assembly
}

// SharedKernel defines the interfaces  
namespace Darklands.SharedKernel.Domain
{
    // Deterministic time marker for domain events
    public readonly record struct GameTick(int Value)
    {
        public static GameTick Zero => new(0);
        public static GameTick operator +(GameTick left, int right) => new(left.Value + right);
        public static GameTick operator -(GameTick left, int right) => new(left.Value - right);
    }
    
    public interface IDomainEvent
    {
        GameTick OccurredAt { get; }
    }
}

namespace Darklands.SharedKernel.Contracts
{
    public interface IContractEvent : INotification
    {
        Guid Id { get; }
        DateTime OccurredAt { get; }
        int Version { get; }
    }
}

namespace Darklands.SharedKernel.Application
{
    public interface IApplicationNotification : INotification
    {
        DateTime OccurredAt { get; }
    }
    
    // Decouple domain from MediatR
    public interface IDomainEventPublisher
    {
        Task PublishAsync(IDomainEvent evt, CancellationToken ct = default);
    }
    
    // Deterministic time source for domain events
    public interface IGameClock
    {
        GameTick CurrentTick { get; }
        void AdvanceTick();
    }
}
```

### Event Flow Pattern
```csharp
// Application layer MediatR adapter (Infrastructure)
public sealed class MediatRDomainEventPublisher : IDomainEventPublisher
{
    private readonly IMediator _mediator;
    
    public MediatRDomainEventPublisher(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public Task PublishAsync(IDomainEvent evt, CancellationToken ct = default)
    {
        // Wrap domain event to make it MediatR compatible
        var wrapper = new DomainEventWrapper(evt);
        return _mediator.Publish(wrapper, ct);
    }
}

private record DomainEventWrapper(IDomainEvent DomainEvent) : INotification;

// 1. Tactical publishes domain event (no MediatR coupling)
public class ExecuteAttackCommandHandler
{
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly IGameClock _clock;
    
    public async Task<Fin<AttackResult>> Handle(ExecuteAttackCommand cmd)
    {
        // ... execute attack ...
        var evt = new ActorDamagedEvent(target, damage, _clock.CurrentTick);
        await _eventPublisher.PublishAsync(evt); // Pure domain event
        return result;
    }
}

// 2. Contract adapter bridges domain to public API
public class TacticalContractAdapter : INotificationHandler<DomainEventWrapper>
{
    private readonly IMediator _mediator;
    private readonly IActorRepository _actors;
    
    public async Task Handle(DomainEventWrapper wrapper, CancellationToken ct)
    {
        // Pattern match on domain event types
        switch (wrapper.DomainEvent)
        {
            case ActorDamagedEvent evt:
                // Get additional context for contract event
                var actor = await _actors.GetByIdAsync(evt.Actor);
                
                // Convert to contract event (public API) 
                var contractEvent = new ActorDamagedContractEvent(
                    EntityId: evt.Actor.ToEntityId(),
                    Damage: evt.Damage,
                    ActorName: actor.Name
                );
                
                // Publish through MediatR (contract event)
                await _mediator.Publish(contractEvent, ct);
                break;
        }
    }
}

// 3. Diagnostics consumes contract event
public class DamageMetricsHandler : INotificationHandler<ActorDamagedContractEvent>
{
    private readonly IPerformanceMonitor _monitor;
    
    public async Task Handle(ActorDamagedContractEvent evt, CancellationToken ct)
    {
        // Can use DateTime, double, etc (OK in diagnostics context)
        await _monitor.RecordDamage(evt.EntityId, evt.Damage, evt.OccurredAt);
    }
}
```

## Simplified Event Strategy

### MediatR with Interface Differentiation
We use a single MediatR bus with different interfaces to distinguish event types:

```csharp
// SharedKernel/Domain/IDomainEvent.cs
namespace Darklands.SharedKernel.Domain
{
    public interface IDomainEvent : INotification
    {
        DateTime OccurredAt { get; }
    }
}

// SharedKernel/Infrastructure/IIntegrationEvent.cs
namespace Darklands.SharedKernel.Infrastructure
{
    public interface IIntegrationEvent : INotification
    {
        Guid Id { get; }
        DateTime OccurredAt { get; }
        int Version { get; }
    }
}

// Domain event - stays within context
public record ActorDamagedEvent(ActorId ActorId, int Damage, GameTick OccurredAt) : IDomainEvent;

// Contract event - can cross contexts (in Contracts assembly)
public record ActorDamagedContractEvent(
    EntityId EntityId, // Uses SharedKernel types only
    int Damage,
    string ActorName   // Public API can include context
) : IContractEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public int Version { get; } = 1;
}
```

**Benefits**:
- Single event bus (MediatR) with clear semantics
- Type safety through interfaces
- No over-engineering for single-player game
- Clear distinction between internal and public events

## Business Rules Pattern

### Explicit Domain Validation
Business rules are first-class citizens in the domain:

```csharp
// SharedKernel/Domain/IBusinessRule.cs
namespace Darklands.SharedKernel.Domain
{
    public interface IBusinessRule
    {
        bool IsBroken();
        string Message { get; }
    }
}

// Example rule in Tactical domain
namespace Darklands.Tactical.Domain.Actors.Rules
{
    public class ActorMustBeAliveRule : IBusinessRule
    {
        private readonly int _health;
        
        public ActorMustBeAliveRule(int health) => _health = health;
        
        public bool IsBroken() => _health <= 0;
        public string Message => "Actor must be alive to perform this action";
    }
}

// Usage in aggregate
public class Actor : Entity, IAggregateRoot
{
    public void Move(Position newPosition)
    {
        CheckRule(new ActorMustBeAliveRule(_health));
        CheckRule(new PositionMustBeEmptyRule(newPosition, _grid));
        
        var oldPosition = _position;
        _position = newPosition;
        
        AddDomainEvent(new ActorMovedEvent(Id, oldPosition, newPosition));
    }
}
```

## Domain Events Collection Pattern

### Standardized Event Management
All entities collect domain events in a standard way:

```csharp
// SharedKernel/Domain/Entity.cs
namespace Darklands.SharedKernel.Domain
{
    public abstract class Entity
    {
        private List<IDomainEvent> _domainEvents;
        
        public IReadOnlyCollection<IDomainEvent> DomainEvents => 
            _domainEvents?.AsReadOnly() ?? Array.Empty<IDomainEvent>();
        
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents ??= new List<IDomainEvent>();
            _domainEvents.Add(domainEvent);
        }
        
        public void ClearDomainEvents()
        {
            _domainEvents?.Clear();
        }
        
        protected void CheckRule(IBusinessRule rule)
        {
            if (rule.IsBroken())
            {
                throw new BusinessRuleValidationException(rule);
            }
        }
    }
}
```

## VSA and DDD Alignment

### Vertical Slices Within Bounded Contexts
VSA and DDD work together in a hierarchical architecture:

```
Tactical Context (DDD Boundary)
├── Features/                          # VSA Organization
│   ├── Attack/                        # Vertical Slice
│   │   ├── Domain/
│   │   │   └── Rules/
│   │   ├── Application/
│   │   │   └── ExecuteAttackCommandHandler.cs
│   │   └── Infrastructure/
│   └── Movement/                      # Vertical Slice
│       ├── Domain/
│       ├── Application/
│       └── Infrastructure/
├── Domain/
│   └── Aggregates/                    # Shared within context
│       └── Actor.cs                   # Used by multiple slices
└── Contracts/                         # Public API
    └── Events/
        └── ActorDamagedContractEvent.cs

**CRITICAL Namespace Convention**: Use plural folder names for aggregates to avoid collisions:
- `Domain/Actors/Actor.cs` (not `Domain/Actor/Actor.cs`)
- `Domain/Grids/Grid.cs` (not `Domain/Grid/Grid.cs`)
- Inspired by modular-monolith-with-ddd pattern
```

**Key Principles**:
- **Slices operate WITHIN contexts**: Never cross bounded context boundaries
- **Aggregates shared within context**: Multiple slices can use same aggregate
- **Events for cross-context**: Use integration events between contexts
- **Direct calls within context**: Slices in same context can call each other
- **Test at both levels**: Slice tests AND context isolation tests
- **Follow the protocol**: Use [DDD Feature Implementation Protocol](./DDD-Feature-Implementation-Protocol.md) for decisions

## Feature Organization Guide

### Quick Decision Process
When implementing a new feature, follow this decision tree:

```
1. Which Context? → Primary concern determines context
   - Combat mechanics → Tactical
   - Performance metrics → Diagnostics
   - Audio/Input/Saves → Platform

2. Vertical Slice or Shared?
   - Complete user action with UI → Vertical Slice (Features/)
   - Core entity/aggregate → Shared Domain (Domain/)
   - Business rules used by multiple slices → Shared Domain

3. Which Event Type?
   - Within context communication → IDomainEvent
   - Cross-context communication → IContractEvent
   - UI updates → Application notifications
```

### Example: Adding "Poison Damage" Feature

```
Decision Process:
1. Context: Tactical (game mechanic)
2. Type: Vertical Slice (complete feature)
3. Events: DomainEvent (internal) + ContractEvent (for monitoring)

Structure:
Tactical/
├── Features/Poison/
│   ├── Domain/
│   │   └── Rules/
│   │       └── PoisonResistanceRule.cs
│   ├── Application/
│   │   ├── ApplyPoisonCommand.cs
│   │   └── ApplyPoisonCommandHandler.cs
│   └── Infrastructure/
│       └── PoisonEffectService.cs
└── Contracts/
    └── Events/
        └── ActorPoisonedContractEvent.cs
```

For detailed guidance, see [DDD Feature Implementation Protocol](./DDD-Feature-Implementation-Protocol.md).

## Godot Threading Strategy

### Main Thread Marshaling
All Godot API calls MUST occur on the main thread:

```csharp
// SharedKernel/Threading/IMainThreadDispatcher.cs
public interface IMainThreadDispatcher
{
    void Enqueue(Action action);
    Task EnqueueAsync(Func<Task> asyncAction);
}

// Presentation/Infrastructure/MainThreadDispatcher.cs
public sealed partial class MainThreadDispatcher : Node, IMainThreadDispatcher
{
    private readonly ConcurrentQueue<Action> _queue = new();
    private int _mainThreadId;
    
    public override void _EnterTree()
    {
        _mainThreadId = Environment.CurrentManagedThreadId;
        ProcessMode = ProcessModeEnum.Always;
    }
    
    public override void _Process(double delta)
    {
        // Process up to N actions per frame to avoid blocking
        const int maxActionsPerFrame = 10;
        int processed = 0;
        
        while (processed < maxActionsPerFrame && _queue.TryDequeue(out var action))
        {
            try
            {
                action();
                processed++;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"MainThread action failed: {ex}");
            }
        }
    }
    
    public void Enqueue(Action action)
    {
        if (IsOnMainThread())
            action(); // Execute immediately if already on main thread
        else
            _queue.Enqueue(action);
    }
    
    public Task EnqueueAsync(Func<Task> asyncAction)
    {
        var tcs = new TaskCompletionSource();
        Enqueue(async () =>
        {
            try
            {
                await asyncAction();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }
    
    private bool IsOnMainThread() => 
        Environment.CurrentManagedThreadId == _mainThreadId;
}

// Usage in Presenters
public class ActorPresenter
{
    private readonly IMainThreadDispatcher _dispatcher;
    
    private void OnActorDamaged(ActorDamagedNotification evt)
    {
        // Ensure Godot calls happen on main thread
        _dispatcher.Enqueue(() =>
        {
            _view.UpdateHealth(evt.EntityId, evt.NewHealth);
            _view.ShowDamageNumber(evt.Damage);
        });
    }
}

// Also used by UIEventBus from ADR-010
// UIEventBus uses this dispatcher for thread-safe UI updates
```

## Dependency Injection Strategy

### Composition Root via Autoload
```csharp
// Presentation/Bootstrapper.cs - Godot Autoload (Singleton)
public sealed partial class Bootstrapper : Node
{
    private static IServiceProvider? _services;
    public static IServiceProvider Services => _services 
        ?? throw new InvalidOperationException("DI not initialized");
    
    public override void _EnterTree()
    {
        // Initialize BEFORE any scenes load
        var services = new ServiceCollection();
        
        // Core services
        services.AddSingleton<IMainThreadDispatcher>(
            GetNode<MainThreadDispatcher>("/root/MainThreadDispatcher"));
        
        // Add bounded contexts
        services.AddTacticalContext();
        services.AddDiagnosticsContext();  
        services.AddPlatformContext(Engine.IsEditorHint());
        
        // NOTE: No separate integration bus - MediatR handles all events
        
        // Build container
        _services = services.BuildServiceProvider();
        
        // Contract adapters registered as MediatR handlers automatically
        // No need to initialize - MediatR discovers them via DI
    }
    
    public override void _ExitTree()
    {
        (_services as IDisposable)?.Dispose();
        _services = null;
    }
}
```

### Context Registration (NO Scoped Services in Game Loop)
```csharp
public static class TacticalContextExtensions
{
    public static IServiceCollection AddTacticalContext(this IServiceCollection services)
    {
        // Domain services - Singleton for game state
        services.AddSingleton<IDeterministicRandom, DeterministicRandom>();
        services.AddSingleton<ICombatSchedulerService, CombatSchedulerService>();
        services.AddSingleton<IActorStateService, ActorStateService>();
        
        // MediatR for domain events (within context only)
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(typeof(TacticalMarker).Assembly);
            cfg.Lifetime = ServiceLifetime.Singleton; // Handlers are stateless
        });
        
        // Contract adapter (registered as MediatR handler) 
        services.AddTransient<INotificationHandler<DomainEventWrapper>, TacticalContractAdapter>();
        
        return services;
    }
}

// NO Scoped services - they don't make sense in game loops
// Use Singleton for game state, Transient for stateless operations
```

## Performance Considerations

### Hot Path Optimization
MediatR should NOT be used in per-frame hot paths:

```csharp
// ❌ BAD: Publishing events every frame
public void UpdateEveryFrame()
{
    foreach (var actor in actors)
    {
        _mediator.Publish(new ActorMovedEvent(actor.Id, actor.Position)); // GC pressure!
    }
}

// ✅ GOOD: Direct service calls in hot path, batch events
public class GameLoop
{
    private readonly ICombatScheduler _scheduler;
    private readonly IVisionSystem _vision;
    private readonly IMediator _mediator; // Single MediatR for all events
    private readonly List<IGameEvent> _frameEvents = new();
    
    public void SimulateTick(int tickNumber)
    {
        // Direct service calls (no MediatR)
        _scheduler.AdvanceTick();
        _vision.UpdateAllActorVision();
        
        // Collect changes
        _frameEvents.AddRange(_scheduler.GetTickEvents());
        
        // Publish ONE coarse event at end of tick
        if (_frameEvents.Count > 0)
        {
            var tickEvent = new TickCompletedEvent(tickNumber, _frameEvents.ToArray());
            _ = _mediator.Publish(tickEvent); // Fire and forget via single MediatR
            _frameEvents.Clear();
        }
    }
}
```

## Determinism Enforcement

### Concrete Architecture Tests
```csharp
[Fact]
public void TacticalDomain_MustBeDeterministic()
{
    var tacticalAssembly = typeof(Darklands.Tactical.Domain.TacticalMarker).Assembly;
    
    // Ban non-deterministic types
    var result = Types.InAssembly(tacticalAssembly)
        .Should()
        .NotHaveDependencyOn("System.DateTime")
        .And().NotHaveDependencyOn("System.Random")  
        .And().NotHaveDependencyOn("System.Threading.Tasks.Task") // No async in domain
        .GetResult();
        
    result.IsSuccessful.Should().BeTrue($"Violations: {string.Join(", ", result.FailingTypeNames)}");
    
    // Ban floating point via reflection
    var floatViolations = tacticalAssembly.GetTypes()
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        .Where(f => f.FieldType == typeof(float) || f.FieldType == typeof(double))
        .Select(f => $"{f.DeclaringType?.Name}.{f.Name}")
        .ToList();
        
    floatViolations.Should().BeEmpty($"Found float/double fields: {string.Join(", ", floatViolations)}");
}

[Fact]
public void DiagnosticsDomain_CanUseNonDeterministicTypes()
{
    // Explicitly allowed - diagnostics can use DateTime, double, etc
    var diagnosticsAssembly = typeof(Darklands.Diagnostics.Domain.DiagnosticsMarker).Assembly;
    
    // This should NOT fail - different rules for different contexts
    var types = diagnosticsAssembly.GetTypes();
    types.Any(t => t.GetProperties().Any(p => p.PropertyType == typeof(DateTime)))
        .Should().BeTrue("Diagnostics should be able to use DateTime");
}

[Fact]
public void Contexts_MustNotDirectlyReference_EachOther()
{
    var tactical = typeof(Darklands.Tactical.Domain.TacticalMarker).Assembly;
    
    // Tactical must not reference other contexts
    Types.InAssembly(tactical)
        .Should().NotHaveDependencyOn("Darklands.Diagnostics")
        .And().NotHaveDependencyOn("Darklands.Platform")
        .GetResult().IsSuccessful.Should().BeTrue();
}

[Fact]
public void ModuleIsolation_WithSmartExclusions()
{
    // CRITICAL: This test enforces module isolation while allowing integration event handlers
    var tacticalAssemblies = new[]
    {
        typeof(Darklands.Tactical.Domain.TacticalMarker).Assembly,
        typeof(Darklands.Tactical.Application.TacticalMarker).Assembly,
        typeof(Darklands.Tactical.Infrastructure.TacticalMarker).Assembly
    };
    
    var otherModules = new[] 
    { 
        "Darklands.Diagnostics.Domain",
        "Darklands.Diagnostics.Application", 
        "Darklands.Diagnostics.Infrastructure",
        "Darklands.Platform.Domain",
        "Darklands.Platform.Infrastructure"
    };
    
    var result = Types.InAssemblies(tacticalAssemblies)
        .That()
        // CRITICAL: Exclude integration event handlers from isolation check!
        .DoNotImplementInterface(typeof(INotificationHandler<>))
        .And().DoNotHaveNameEndingWith("IntegrationEventHandler")
        .And().DoNotHaveName("EventsBusStartup")
        .Should()
        .NotHaveDependencyOnAny(otherModules)
        .GetResult();
        
    result.IsSuccessful.Should().BeTrue(
        $"Module isolation violated. Failing types: {string.Join(", ", result.FailingTypeNames)}");
}

[Fact]
public void ContractEvents_OnlyUseSharedTypes()
{
    // Contract events must only use primitive types or SharedKernel types
    var contractsAssembly = typeof(Darklands.Tactical.Contracts.TacticalMarker).Assembly;
    
    var result = Types.InAssembly(contractsAssembly)
        .Should()
        // Can reference SharedKernel
        .HaveDependencyOn("Darklands.SharedKernel")
        // But NOT internal domain types
        .And().NotHaveDependencyOn("Darklands.Tactical.Domain")
        .And().NotHaveDependencyOn("Darklands.Tactical.Application")
        .And().NotHaveDependencyOn("Darklands.Tactical.Infrastructure")
        .GetResult();
        
    result.IsSuccessful.Should().BeTrue(
        "Contract events must only use shared types, not internal domain types");
}
```

## Presentation Layer Decoupling

### Application Notifications Instead of Domain Events
Presentation should use application-level notifications, not domain events:

```csharp
// Application layer notification (NOT domain event)
namespace Darklands.Tactical.Application.Notifications
{
    public sealed record ActorMovedNotification(
        EntityId ActorId,        // Shared kernel type
        int FromX, int FromY,    // Primitive types
        int ToX, int ToY
    ) : IApplicationNotification    // Uses SharedKernel.Application interface
    {
        public DateTime OccurredAt { get; } = DateTime.UtcNow; // OK for Application notifications
    }
}

// Application handler converts domain to notification
public class MoveActorCommandHandler : IRequestHandler<MoveActorCommand, Fin<Unit>>
{
    public async Task<Fin<Unit>> Handle(MoveActorCommand cmd, CancellationToken ct)
    {
        // ... perform domain logic ...
        
        // Publish application notification (not domain event)
        await _mediator.Publish(new ActorMovedNotification(
            cmd.ActorId,
            oldPos.X, oldPos.Y,
            newPos.X, newPos.Y
        ), ct);
        
        return unit;
    }
}

// Presenter subscribes to application notifications
public class ActorPresenter : EventAwarePresenter
{
    protected override void SubscribeToEvents()
    {
        // Subscribe to application notifications, NOT domain events
        EventBus.Subscribe<ActorMovedNotification>(this, OnActorMoved);
    }
    
    private void OnActorMoved(ActorMovedNotification notification)
    {
        _dispatcher.Enqueue(() =>
        {
            _view.MoveActor(notification.ActorId, notification.ToX, notification.ToY);
        });
    }
}
```

## Platform Feature Detection

### Centralized Runtime Environment
```csharp
// SharedKernel/Platform/IRuntimeEnvironment.cs
public interface IRuntimeEnvironment
{
    bool IsEditor { get; }
    bool IsDebugBuild { get; }
    bool IsHeadless { get; }
    PlatformType Platform { get; }
    bool HasFeature(string feature);
}

// Platform.Infrastructure.Godot/GodotRuntimeEnvironment.cs
public sealed class GodotRuntimeEnvironment : IRuntimeEnvironment
{
    public bool IsEditor => Engine.IsEditorHint();
    public bool IsDebugBuild => OS.IsDebugBuild();
    public bool IsHeadless => DisplayServer.GetName() == "headless";
    public PlatformType Platform => OS.GetName() switch
    {
        "Windows" => PlatformType.Windows,
        "Linux" => PlatformType.Linux,
        "macOS" => PlatformType.MacOS,
        "Android" => PlatformType.Android,
        "iOS" => PlatformType.iOS,
        "Web" => PlatformType.Web,
        _ => PlatformType.Unknown
    };
    
    public bool HasFeature(string feature) => OS.HasFeature(feature);
}

// Usage in DI configuration
public static IServiceCollection AddPlatformContext(
    this IServiceCollection services, 
    IRuntimeEnvironment env)
{
    if (env.IsEditor || env.IsHeadless)
    {
        services.AddSingleton<IAudioService, MockAudioService>();
        services.AddSingleton<IInputService, MockInputService>();
    }
    else
    {
        services.AddSingleton<IAudioService, GodotAudioService>();
        services.AddSingleton<IInputService, GodotInputService>();
    }
    
    return services;
}
```

## Implementation Protocol

### Phase 0: Team Alignment (30 minutes)
1. **Review DDD Feature Implementation Protocol** with all personas
2. **Establish decision tree** for feature placement
3. **Agree on event types** (IDomainEvent vs IContractEvent)

### Phase 1: Foundation Patterns (4 hours) - CRITICAL
1. **Create Contracts assemblies** for each context
   - Tactical.Contracts.csproj (public API)
   - Diagnostics.Contracts.csproj (public API)
   - Platform.Contracts.csproj (public API)
2. **Implement TypedIdValueBase** in SharedKernel.Domain
3. **Add Entity base class** with domain events collection
4. **Add IBusinessRule interface** to SharedKernel.Domain
5. **Create module isolation tests** with smart exclusions

### Phase 2: Assembly Structure (1 day)
1. Create separate .csproj files for each context
2. Layer SharedKernel (Domain, Application, Infrastructure)
3. Set up project references (contexts reference ONLY Contracts)
4. Configure build pipeline to enforce boundaries
5. Add TacticalMarker, DiagnosticsMarker, etc. for test references

### Phase 3: Core Patterns Implementation (1 day)
1. **Business Rules**: Create rules folder per aggregate
2. **Domain Events**: Standardize AddDomainEvent pattern
3. **Strongly-Typed IDs**: Convert all Guid IDs to typed versions
4. **VSA Integration**: Create Features/ folder structure within contexts
5. **Repository Per Aggregate**: Replace generic repositories

### Phase 4: Event Infrastructure (4 hours)
1. **Configure Single MediatR** for both event types
   - IDomainEvent handlers within context
   - IContractEvent handlers can cross contexts
2. **Create Contract Adapters**
   - Convert domain events to contract events
   - Map at context boundaries only
3. **Wire up event flow**
   - Domain → Adapter → Contract → Other contexts

### Phase 5: Contract Event Wiring (2 hours)
1. Configure MediatR handlers for contract events
2. Add event versioning (Version property)
3. Create TacticalContractAdapter for domain→contract mapping
4. Wire up cross-context subscriptions via Contracts
5. Add version tracking for contract evolution

### Phase 6: Testing & Validation (4 hours)
1. **Module isolation tests** - Verify no cross-references
2. **Contract tests** - Verify only shared types used
3. **Determinism tests** - Tactical must be deterministic
4. **Event flow tests** - End-to-end event processing
5. **VSA slice tests** - Per-feature testing

## Context Mapping Patterns

### 1. Shared Kernel
- Minimal types: EntityId, Fixed, Fin<T>, IIntegrationEvent
- No behavior, only data structures
- Carefully versioned

### 2. Customer-Supplier
- Tactical (Supplier) → Diagnostics (Customer)
- Via integration events only
- Diagnostics cannot affect Tactical

### 3. Anti-Corruption Layer
- Platform context shields from Godot specifics
- MainThreadDispatcher handles threading
- RuntimeEnvironment abstracts platform detection

## Consequences

### Positive
1. **Compile-time Safety**: Assembly boundaries prevent accidental coupling
2. **Clear Boundaries**: Integration events make cross-context flow explicit
3. **Performance**: Hot paths avoid MediatR overhead
4. **Threading Safety**: Main thread marshaling prevents Godot crashes
5. **True Isolation**: Each context genuinely independent
6. **Determinism**: Enforced via concrete tests and assembly boundaries

### Negative
1. **More Projects**: Multiple .csproj files to manage
2. **Build Complexity**: More complex build pipeline
3. **Integration Overhead**: Adapters and mappers between contexts
4. **Learning Curve**: Team needs to understand assembly boundaries

### Mitigations
- Start with Tactical and Diagnostics only
- Automate build with proper scripts
- Provide clear examples and documentation
- Use integration events sparingly

## Decision Outcome

We will implement assembly-based bounded contexts with enhanced patterns from modular-monolith-with-ddd:

### Core Architecture
1. **Contracts assemblies** - Public API between contexts
2. **Module isolation with smart exclusions** - Allow event handlers while enforcing boundaries
3. **Single MediatR with interfaces** - IDomainEvent for internal, IContractEvent for public
4. **VSA within contexts** - Vertical slices operate within bounded contexts

### Foundational Patterns
5. **Strongly-typed IDs** - TypedIdValueBase for all identifiers
6. **Business Rules pattern** - IBusinessRule for explicit domain validation
7. **Domain Events collection** - Standardized in Entity base class
8. **Repository per aggregate** - No generic repositories

### Infrastructure & Simplicity
9. **Single MediatR instance** - One bus, two interfaces (IDomainEvent, IContractEvent)
10. **Main thread marshaling** - All Godot API calls on main thread
11. **No scoped services** - Singleton or Transient only in game loops
12. **Application notifications** - Presentation uses app events, not domain events

### Quality & Testing
13. **Architecture tests per module** - With ContractEventHandler exclusions
14. **Contract versioning** - Version property from day one for evolution
15. **Determinism enforcement** - Concrete tests for Tactical context

## Cross-References

### Related ADRs
- **[ADR-010: UI Event Bus Architecture](./ADR-010-ui-event-bus-architecture.md)** - Uses MainThreadDispatcher from this ADR
- **[ADR-006: Selective Abstraction Strategy](./ADR-006-selective-abstraction-strategy.md)** - Aligned service resolution patterns

### Event Architecture Overview
This ADR defines the complete event architecture with multiple event types using **SINGLE MediatR**:

1. **Domain Events** (`IDomainEvent`): Internal to bounded contexts, use `GameTick` for determinism
2. **Contract Events** (`IContractEvent`): Cross-context integration via adapters, use `DateTime`
3. **Application Notifications** (`IApplicationNotification`): UI updates (see ADR-010), use `DateTime`

**Key Design Decisions**:
- **Single MediatR instance** handles all event types via different interfaces  
- **Domain events decoupled** from MediatR via `IDomainEventPublisher` adapter
- **Type-safe IDs** prevent cross-type equality bugs with `TypedId<TSelf>`
- **Thread-safe main thread dispatcher** uses `Environment.CurrentManagedThreadId`

## References
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Implementing Domain-Driven Design by Vaughn Vernon](https://www.informit.com/store/implementing-domain-driven-design-9780321834577)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Clean Architecture by Robert Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Assembly-based Architecture Enforcement](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [Modular Monolith with DDD by Kamil Grzybek](https://github.com/kgrzybek/modular-monolith-with-ddd) - **Source of key enhancement patterns**
- [ADR-017 Enhancement Analysis](./ADR-017-enhancements-from-modular-monolith.md) - Detailed pattern analysis
- [DDD Bounded Contexts Patterns Learning](../../08-Learning/2025-09-12-ddd-bounded-contexts-patterns.md) - In-depth learnings
- **[DDD Feature Implementation Protocol](./DDD-Feature-Implementation-Protocol.md)** - Quick decision guide for feature placement
