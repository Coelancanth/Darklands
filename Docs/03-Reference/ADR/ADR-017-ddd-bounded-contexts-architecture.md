# ADR-017: DDD Bounded Contexts Architecture

## Status
**Status**: Proposed  
**Date**: 2025-09-12  
**Updated**: 2025-09-12 (Revised based on architectural review)
**Decision Makers**: Tech Lead, Dev Engineer  
**Supersedes**: Enhances ADR-015 (Namespace Organization)

## Context

### The Problem
Our current monolithic domain structure creates several architectural issues:
1. **Mixed Concerns**: Tactical game logic, diagnostics, and platform abstractions in one domain
2. **Determinism Conflicts**: Performance monitoring needs DateTime/double, violating ADR-004
3. **Architecture Violations**: Application layer forced to reference Infrastructure for legitimate monitoring
4. **Unclear Boundaries**: No separation between core game logic and cross-cutting concerns
5. **Testing Complexity**: Can't apply different rules to different parts of the system
6. **Accidental Coupling**: Easy to reference wrong types across logical boundaries

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
│   └── Darklands.Tactical.Infrastructure.csproj # Repositories, Services
│
├── Diagnostics/                                # Monitoring & Debug
│   ├── Darklands.Diagnostics.Domain.csproj    # DateTime/double allowed
│   ├── Darklands.Diagnostics.Application.csproj # Monitoring queries
│   └── Darklands.Diagnostics.Infrastructure.csproj # Loggers, Monitors
│
├── Platform/                                   # External integrations
│   ├── Darklands.Platform.Domain.csproj       # Service interfaces
│   └── Darklands.Platform.Infrastructure.Godot.csproj # Godot implementations
│
├── SharedKernel/                               # Minimal shared types
│   └── Darklands.SharedKernel.csproj          # EntityId, Fixed, Fin<T>
│
└── Darklands.csproj                           # Main Godot project
    └── Presentation/                           # All Godot nodes here
        ├── Views/
        ├── Presenters/
        └── Bootstrapper.cs                     # DI composition root
```

### Assembly References
```xml
<!-- Darklands.csproj (Main Godot Project) -->
<ItemGroup>
  <ProjectReference Include="src/Tactical/Darklands.Tactical.Application.csproj" />
  <ProjectReference Include="src/Diagnostics/Darklands.Diagnostics.Application.csproj" />
  <ProjectReference Include="src/Platform/Darklands.Platform.Infrastructure.Godot.csproj" />
  <ProjectReference Include="src/SharedKernel/Darklands.SharedKernel.csproj" />
</ItemGroup>

<!-- No direct references between contexts! -->
```

## Shared Identity Strategy

### Problem: Context Isolation
Different contexts must NOT reference each other's domain types. Using `ActorId` from Tactical in Diagnostics violates isolation.

### Solution: Shared Identity Types
```csharp
// SharedKernel/Identity/EntityId.cs
namespace Darklands.SharedKernel.Identity
{
    public readonly record struct EntityId(Guid Value)
    {
        public static EntityId NewId() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString("N")[..8];
    }
}

// Tactical.Domain uses internal ActorId that wraps EntityId
namespace Darklands.Tactical.Domain.Entities
{
    internal readonly record struct ActorId(EntityId Value)
    {
        public static implicit operator EntityId(ActorId id) => id.Value;
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

## Event Bus Architecture

### Dual Bus Strategy
We use TWO separate event buses to avoid mixing concerns:

```csharp
// 1. Domain Event Bus (MediatR) - Within context only
namespace Darklands.Tactical.Domain.Events
{
    public record ActorDamagedEvent(ActorId Actor, int Damage) : INotification;
    // Published via IMediator within Tactical context only
}

// 2. Integration Event Bus - Cross-context communication
namespace Darklands.SharedKernel.Integration
{
    public interface IIntegrationEvent 
    { 
        int Version { get; }
        DateTime OccurredUtc { get; }
        string CorrelationId { get; }
    }
    
    public interface IIntegrationEventBus
    {
        Task PublishAsync(IIntegrationEvent evt, CancellationToken ct = default);
        void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IIntegrationEvent;
    }
}

// Integration events use primitive types only
namespace Darklands.SharedKernel.Integration.Events
{
    public sealed record CombatMetricRecordedEvent(
        string EntityId,           // String, not ActorId
        long TimestampTicks,        // Ticks, not DateTime
        string MetricType,
        double MetricValue,
        int Version = 1,
        DateTime OccurredUtc = default,
        string CorrelationId = ""
    ) : IIntegrationEvent;
}
```

### Event Flow Pattern
```csharp
// 1. Tactical publishes domain event
public class ExecuteAttackCommandHandler
{
    public async Task<Fin<AttackResult>> Handle(ExecuteAttackCommand cmd)
    {
        // ... execute attack ...
        await _mediator.Publish(new ActorDamagedEvent(target, damage)); // Domain event
        return result;
    }
}

// 2. Integration adapter bridges to other contexts
public class TacticalIntegrationAdapter : INotificationHandler<ActorDamagedEvent>
{
    private readonly IIntegrationEventBus _integrationBus;
    
    public async Task Handle(ActorDamagedEvent evt, CancellationToken ct)
    {
        // Convert to integration event (primitive types only)
        var integration = new CombatMetricRecordedEvent(
            EntityId: evt.Actor.Value.ToString(),
            TimestampTicks: DateTime.UtcNow.Ticks,
            MetricType: "Damage",
            MetricValue: evt.Damage,
            CorrelationId: Guid.NewGuid().ToString()
        );
        await _integrationBus.PublishAsync(integration, ct);
    }
}

// 3. Diagnostics consumes integration event
public class MetricsRecorder
{
    public MetricsRecorder(IIntegrationEventBus bus)
    {
        bus.Subscribe<CombatMetricRecordedEvent>(RecordMetric);
    }
    
    private async Task RecordMetric(CombatMetricRecordedEvent evt)
    {
        // Can use DateTime, double, etc (OK in diagnostics)
        var timestamp = new DateTime(evt.TimestampTicks);
        await _monitor.Record(timestamp, evt.MetricType, evt.MetricValue);
    }
}
```

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
    private static MainThreadDispatcher? _instance;
    
    public override void _EnterTree()
    {
        _instance = this;
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
    
    private static bool IsOnMainThread() => 
        Thread.CurrentThread == _instance?.GetProcessThread();
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
        
        // Integration event bus (separate from MediatR)
        services.AddSingleton<IIntegrationEventBus, IntegrationEventBus>();
        
        // Build container
        _services = services.BuildServiceProvider();
        
        // Initialize integration adapters
        _services.GetRequiredService<TacticalIntegrationAdapter>();
        _services.GetRequiredService<DiagnosticsIntegrationAdapter>();
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
        
        // Integration adapter
        services.AddSingleton<TacticalIntegrationAdapter>();
        
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
    private readonly IIntegrationEventBus _eventBus;
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
            _ = _eventBus.PublishAsync(tickEvent); // Fire and forget
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
    ) : INotification;
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

### Phase 1: Create Assembly Structure (1 day)
1. Create separate .csproj files for each context
2. Move SharedKernel types first (EntityId, Fixed, Fin<T>)
3. Set up project references (no cross-context refs)
4. Configure build pipeline

### Phase 2: Namespace to Assembly Migration (2 days)
1. Execute TD_032 namespace reorganization first
2. Move reorganized code to appropriate assemblies
3. Fix compilation errors from boundary violations
4. Update all using statements

### Phase 3: Extract Diagnostics Context (TD_040 - 6h)
1. Create Diagnostics assemblies
2. Move performance monitoring (use EntityId, not ActorId)
3. Set up integration event adapter
4. Configure separate DI registration

### Phase 4: Implement Integration Events (1 day)
1. Create IIntegrationEventBus implementation
2. Add versioning and correlation IDs
3. Create adapters for each context
4. Wire up cross-context communication

### Phase 5: Platform & Threading (1 day)
1. Extract Platform context assemblies
2. Implement MainThreadDispatcher
3. Update all presenters to use dispatcher
4. Create GodotRuntimeEnvironment

### Phase 6: Testing & Validation (1 day)
1. Add architecture tests per context
2. Verify determinism in Tactical
3. Test integration event flow
4. Performance profiling

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

We will implement assembly-based bounded contexts with:
1. **Separate assemblies** per context (not just namespaces)
2. **Dual event buses** (MediatR for domain, custom for integration)
3. **Main thread marshaling** for all Godot API calls
4. **No scoped services** (Singleton or Transient only)
5. **Shared identity types** in SharedKernel
6. **Application notifications** for UI (not domain events)
7. **Integration event versioning** from day one

## References
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Implementing Domain-Driven Design by Vaughn Vernon](https://www.informit.com/store/implementing-domain-driven-design-9780321834577)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Clean Architecture by Robert Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Assembly-based Architecture Enforcement](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)