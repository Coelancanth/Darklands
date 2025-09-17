# Production Patterns Catalog

**Last Updated**: 2025-09-17
**Purpose**: Battle-tested patterns extracted from completed features
**Source**: Extracted from HANDBOOK.md, post-mortems, and MediatR best practices analysis

## 🎯 MediatR Best Practices Summary

### Core Principles
- **One Handler per Request**: Each command/query has exactly one handler
- **No Nested Handlers**: NEVER inject IMediator into handlers
- **Pipeline Order Matters**: ErrorHandling→Logging→Caching→Validation→Transaction
- **Domain Events are Good**: Publishing INotification from handlers is encouraged
- **Explicit Dependencies**: Services over service locator pattern

### Key Patterns We Use
- **Fin<T> Error Handling**: All handlers return Fin<T> for functional error handling
- **UIEventForwarder**: Generic notification handler bridges domain events to UI
- **Pipeline Behaviors**: Cross-cutting concerns (logging, error handling)
- **CQRS Separation**: Commands (IRequest) modify state, Queries read state

## 🎯 Pattern Index

### ✅ Recommended Patterns
1. [MediatR Pipeline Behavior Registration Order](#pattern-mediatr-pipeline-behavior-registration-order) ⭐⭐⭐⭐⭐
2. [Shared Domain Service (Avoiding Nested Handlers)](#pattern-shared-domain-service-avoiding-nested-handlers) ⭐⭐⭐⭐⭐
3. [Performance Monitoring Pipeline Behavior](#pattern-performance-monitoring-pipeline-behavior) ⭐⭐⭐⭐
4. [Value Object Factory with Validation](#pattern-value-object-factory-with-validation-vs_001)
5. [Thread-Safe DI Container](#pattern-thread-safe-di-container-vs_001)
6. [Cross-Presenter Coordination](#pattern-cross-presenter-coordination-vs_010a)
7. [Optional Feedback Service](#pattern-optional-feedback-service-vs_010b)
8. [Death Cascade Coordination](#pattern-death-cascade-coordination-vs_010b)
9. [Godot Node Lifecycle](#pattern-godot-node-lifecycle-vs_010a)
10. [Queue-Based CallDeferred](#pattern-queue-based-calldeferred-td_011)
11. [Godot Parent-Child UI Pattern](#pattern-godot-parent-child-ui-pattern-vs_011) ⭐⭐⭐
12. [Godot API Casing Requirements](#pattern-godot-api-casing-requirements)
13. [CallDeferred for Thread Safety](#pattern-calldeferred-for-thread-safety)

### ⚠️ Anti-Patterns (DO NOT USE)
- ❌ [Nested MediatR Handlers](#anti-pattern-nested-mediatr-handlers) - Creates hidden dependencies
- ❌ [Bridge Pattern for Split Views](#anti-pattern-bridge-pattern-for-split-views) - Symptom of poor architecture
- ❌ [Static Event Router](#anti-pattern-static-event-router) - Replaced by ADR-010 UIEventBus

---

## 🔨 Extracted Production Patterns

### Pattern: MediatR Pipeline Behavior Registration Order ⭐⭐⭐⭐⭐
**Problem**: Incorrect registration order causes behaviors to miss exceptions or execute inefficiently
**Context**: Pipeline behaviors execute in registration order - this is CRITICAL
**Solution**: Register in specific order based on behavior interactions
```csharp
// CORRECT registration order in GameStrapper
services.AddMediatR(config =>
{
    // 1. ErrorHandling FIRST - catches ALL exceptions
    config.AddOpenBehavior(typeof(ErrorHandlingBehavior<,>));

    // 2. Logging EARLY - for observability
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));

    // 3. Caching (if implemented) - short-circuit before validation
    config.AddOpenBehavior(typeof(CachingBehavior<,>));

    // 4. Validation - ensures valid data before handler
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));

    // 5. Transaction LAST - just before handler
    config.AddOpenBehavior(typeof(TransactionBehavior<,>));
});
```
**Key**: Order matters! ErrorHandling→Logging→Caching→Validation→Transaction→Handler

### Pattern: Shared Domain Service (Avoiding Nested Handlers) ⭐⭐⭐⭐⭐
**Problem**: Handler needs to trigger logic that another handler implements
**Anti-Pattern**: Handler calling `_mediator.Send()` - creates hidden dependencies
**Solution**: Extract shared logic into domain service injected into both handlers
```csharp
// ❌ WRONG - Nested handler anti-pattern
public class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand, Fin<Unit>>
{
    private readonly IMediator _mediator; // RED FLAG!

    public async Task<Fin<Unit>> Handle(ExecuteAttackCommand request, CancellationToken ct)
    {
        // ... attack logic ...
        await _mediator.Send(new DamageActorCommand(...)); // ANTI-PATTERN!
    }
}

// ✅ CORRECT - Shared domain service
public interface IDamageService
{
    Fin<Unit> ApplyDamage(ActorId target, int damage, string source);
}

public class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand, Fin<Unit>>
{
    private readonly IDamageService _damageService; // Direct dependency

    public async Task<Fin<Unit>> Handle(ExecuteAttackCommand request, CancellationToken ct)
    {
        // ... attack logic ...
        return _damageService.ApplyDamage(target, damage, "sword");
    }
}

public class DamageActorCommandHandler : IRequestHandler<DamageActorCommand, Fin<Unit>>
{
    private readonly IDamageService _damageService; // Same service

    public Task<Fin<Unit>> Handle(DamageActorCommand request, CancellationToken ct)
    {
        return Task.FromResult(_damageService.ApplyDamage(request.Target, request.Damage, request.Source));
    }
}
```
**Key**: Extract shared logic to services, not nested MediatR calls

### Pattern: Performance Monitoring Pipeline Behavior ⭐⭐⭐⭐
**Problem**: Need to identify slow-running operations without adding timing code to handlers
**Solution**: Pipeline behavior that wraps all handlers with performance monitoring
```csharp
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICategoryLogger _logger;
    private readonly int _warningThresholdMs;

    public PerformanceBehavior(ICategoryLogger logger, int warningThresholdMs = 500)
    {
        _logger = logger;
        _warningThresholdMs = warningThresholdMs;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > _warningThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            _logger.Log(LogLevel.Warning, LogCategory.Performance,
                "Slow request: {Name} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                requestName, stopwatch.ElapsedMilliseconds, _warningThresholdMs);
        }

        return response;
    }
}
```
**Key**: Automatic performance monitoring without cluttering business logic

### Pattern: Value Object Factory with Validation (VS_001)
**Problem**: Public constructors allow invalid state (`new TimeUnit(-999)`)
**Solution**: Private constructor + validated factory methods
```csharp
// Implementation pattern from VS_001
public record TimeUnit {
    private TimeUnit(int value) { Value = value; }
    
    public static Fin<TimeUnit> Create(int value) {
        if (value < 0 || value > 10000) 
            return FinFail<TimeUnit>(Error.New($"TimeUnit must be 0-10000, got {value}"));
        return FinSucc(new TimeUnit(value));
    }
    
    // For known-valid compile-time values only
    public static TimeUnit CreateUnsafe(int value) => new(value);
}
```
**Key**: Impossible to create invalid instances in production

### Pattern: Thread-Safe DI Container (VS_001)
**Problem**: Static fields cause race conditions during initialization
**Solution**: Double-checked locking pattern
```csharp
// Implementation from GameStrapper.cs fix
private static volatile IServiceProvider? _instance;
private static readonly object _lock = new();

public static IServiceProvider Instance {
    get {
        if (_instance == null) {
            lock (_lock) {
                if (_instance == null) {
                    _instance = BuildServiceProvider();
                }
            }
        }
        return _instance;
    }
}
```
**Key**: Thread-safe singleton without performance penalty

### Pattern: Cross-Presenter Coordination (VS_010a)
**Problem**: Isolated presenters can't coordinate cross-cutting features (health bars)
**Solution**: Explicit presenter coordination via setter injection
```csharp
// Implementation from VS_010a health system
public class ActorPresenter : PresenterBase<IActorView> {
    private HealthPresenter? _healthPresenter;
    
    // Coordination injection
    public void SetHealthPresenter(HealthPresenter healthPresenter) {
        _healthPresenter = healthPresenter;
    }
    
    private async Task CreateActor(ActorId id, Position position) {
        var actor = await _actorService.CreateActor(id);
        await _view.DisplayActor(id, position);
        
        // Coordinate with health presenter
        _healthPresenter?.HandleActorCreated(id, actor.Health);
    }
}
```
**Key**: Cross-cutting features need explicit presenter coordination

### Pattern: Optional Feedback Service (VS_010b)
**Problem**: Presentation layer feedback without violating Clean Architecture
**Solution**: Optional service injection in handlers
```csharp
// From VS_010b combat system
public interface IAttackFeedbackService {
    void OnAttackSuccess(ActorId attacker, ActorId target, int damage);
    void OnAttackFailed(ActorId attacker, string reason);
}

public class ExecuteAttackCommandHandler {
    private readonly IAttackFeedbackService? _feedbackService;
    
    public ExecuteAttackCommandHandler(
        IAttackFeedbackService? feedbackService = null) {
        _feedbackService = feedbackService; // Null in tests, real in production
    }
    
    public Fin<Unit> Handle(ExecuteAttackCommand cmd) {
        // Business logic...
        _feedbackService?.OnAttackSuccess(attacker, target, damage);
        return FinSucc(Unit.Default);
    }
}
```
**Key**: Optional injection maintains testability while enabling rich UI

### Pattern: Death Cascade Coordination (VS_010b)
**Problem**: Actor death requires coordinated cleanup across multiple systems
**Solution**: Ordered removal from all systems
```csharp
// Death cascade order matters!
private void HandleActorDeath(ActorId actorId) {
    // 1. Remove from scheduler (no more turns)
    _scheduler.RemoveActor(actorId);
    
    // 2. Remove from grid (free position)
    _gridService.RemoveActor(actorId);
    
    // 3. Remove from scene (visual cleanup)
    _view.RemoveActor(actorId);
    
    // 4. Remove from state (memory cleanup)
    _actorService.RemoveActor(actorId);
}
```
**Key**: Order prevents orphaned state and null references

### Pattern: Godot Node Lifecycle (VS_010a)
**Problem**: Node initialization in constructor fails - tree not ready
**Solution**: Always use _Ready() for node initialization
```csharp
// WRONG - Constructor too early
public partial class HealthView : Node2D {
    public HealthView() {
        _healthBar = GetNode<ProgressBar>("HealthBar"); // CRASH!
    }
}

// CORRECT - _Ready() when tree ready
public partial class HealthView : Node2D {
    private ProgressBar? _healthBar;
    
    public override void _Ready() {
        _healthBar = GetNode<ProgressBar>("HealthBar"); // Safe
        _label = GetNode<Label>("HealthLabel");
    }
}
```
**Key**: Godot node tree isn't ready until _Ready() is called

### Pattern: Queue-Based CallDeferred (TD_011)
**Problem**: Shared fields cause race conditions in Godot UI updates
**Solution**: Queue-based deferred processing
```csharp
// Implementation from ActorView.cs fix
private readonly Queue<ActorCreationData> _pendingCreations = new();
private readonly object _queueLock = new();

public void DisplayActor(ActorId id, Position pos) {
    lock (_queueLock) {
        _pendingCreations.Enqueue(new(id, pos));
    }
    CallDeferred(nameof(ProcessPendingCreations));
}

private void ProcessPendingCreations() {
    lock (_queueLock) {
        while (_pendingCreations.Count > 0) {
            var data = _pendingCreations.Dequeue();
            // Safe UI update on main thread
        }
    }
}
```
**Key**: Thread-safe UI updates without races

## 📚 Technical Debt Patterns

### Pattern: Documentation as Code (TD_001)
**Problem**: Separate setup docs drift from reality
**Solution**: Integrate into daily reference (HANDBOOK.md)
- Single source of truth
- Used daily = stays current
- <10 minute setup achieved

### Pattern: Glossary SSOT Enforcement (TD_002)
**Problem**: Inconsistent terminology causes bugs
**Solution**: Strict glossary adherence
- Even small fixes matter ("combatant" → "Actor")
- Prevents confusion at scale
- Enforced in code reviews

### Pattern: Interface-First Design (TD_003)
**Problem**: Implementation before contract
**Solution**: Define interfaces before coding
```csharp
// Define contract first
public interface ISchedulable {
    ActorId Id { get; }
    Position Position { get; }
    TimeUnit NextTurn { get; }
}
// Then implement...
```

### Pattern: Grid System Design (VS_005)
**Problem**: Need efficient grid storage and pathfinding
**Solution**: 1D array with row-major ordering
```csharp
// From VS_005 implementation
public record Grid {
    private readonly Tile[] _tiles; // 1D array for cache efficiency
    
    private int GetIndex(Position pos) => pos.Y * Width + pos.X;
    
    public Fin<Tile> GetTile(Position pos) {
        if (!IsInBounds(pos)) return FinFail<Tile>(Error.New("Out of bounds"));
        return FinSucc(_tiles[GetIndex(pos)]);
    }
}
```
**Key**: 1D arrays are faster than 2D, row-major ordering for cache locality

### Pattern: CQRS with Auto-Discovery (VS_006)
**Problem**: Clean command/query separation with automatic handler registration
**Solution**: MediatR with namespace-based discovery
```csharp
// Commands return Fin<Unit>, Queries return Fin<T>
public class MoveActorCommand : IRequest<Fin<Unit>> { }
public class GetGridStateQuery : IRequest<Fin<GridState>> { }

// Handler auto-discovered by namespace
namespace Darklands.Core.Application.Grid.Commands {
    public class MoveActorCommandHandler : IRequestHandler<MoveActorCommand, Fin<Unit>> { }
}
```
**Key**: Namespace MUST be Darklands.Core.* for auto-discovery

### Pattern: List vs SortedSet for Scheduling (VS_002)
**Problem**: Need priority queue that allows duplicates
**Solution**: List with binary search insertion
```csharp
// From VS_002 scheduler
public class CombatScheduler {
    private readonly List<ISchedulable> _timeline = new();
    
    public void Schedule(ISchedulable entity) {
        var index = _timeline.BinarySearch(entity, _comparer);
        if (index < 0) index = ~index;
        _timeline.Insert(index, entity); // Allows duplicates!
    }
}
```
**Key**: SortedSet prevents duplicates, List allows rescheduling

### Pattern: Composite Query Service (TD_009)
**Problem**: Need data from multiple services
**Solution**: Composite service queries both
```csharp
// From TD_009 SSOT refactor
public class CombatQueryService : ICombatQueryService {
    public Fin<CombatView> GetCombatView(ActorId id) {
        var actorResult = _actorService.GetActor(id);
        var positionResult = _gridService.GetPosition(id);
        
        return actorResult.Bind(actor =>
            positionResult.Map(pos => 
                new CombatView(actor, pos)));
    }
}
```
**Key**: Each service owns its domain, composite combines

### Pattern: Library Migration Strategy (TD_004)
**Problem**: Breaking changes block builds
**Solution**: Systematic migration approach
```csharp
// LanguageExt v4 → v5 patterns
Error.New(code, msg) → Error.New($"{code}: {msg}")
.ToSeq() → Seq(collection.AsEnumerable())
Seq1(x) → [x]
```
**Process**: Fix compilation → Test → Refactor patterns

## 🎯 Common Patterns Summary

### Integer-Only Arithmetic for Determinism
**When**: Any game system requiring reproducible behavior
**Why**: Float math causes platform inconsistencies, save/load desyncs
```csharp
// ✅ CORRECT Integer Pattern (from BR_001 fix)
public static int CalculateTimeUnits(int baseTime, int agility, int encumbrance) {
    // Scale by 100 for precision, round at boundaries
    var numerator = baseTime * 100 * (10 + encumbrance);
    var denominator = agility * 10;
    return (numerator + denominator/2) / denominator;  // Integer division with rounding
}
```
**Key**: Multiply by powers of 10, do math, divide back down

### SSOT Service Architecture  
**When**: Multiple services need same data
**Why**: Prevents state synchronization bugs
```csharp
// ✅ CORRECT SSOT Pattern (from TD_009 fix)
public interface IGridStateService {
    Fin<Position> GetActorPosition(ActorId id);  // ONLY source for positions
}
public interface IActorStateService {
    Fin<Actor> GetActor(ActorId id);  // ONLY source for actor stats
}
public interface ICombatQueryService {
    Fin<CombatView> GetCombatView(ActorId id);  // Composes from both
}
```
**Key**: Each service owns specific domain, composite services query both

### Sequential Turn Processing
**When**: Turn-based game mechanics
**Why**: Async creates race conditions in inherently sequential systems
```csharp
// ✅ CORRECT Sequential Pattern (from TD_011 fix)
public class GameLoopCoordinator {
    public void ProcessTurn() {
        var actor = _scheduler.GetNextActor();      // Step 1
        var action = GetPlayerAction(actor);        // Step 2
        ExecuteAction(actor, action);               // Step 3
        UpdateUI();                                  // Step 4
        // ONE actor, ONE action, ONE update - NO concurrency
    }
}
```
**Key**: Complete one actor fully before starting next

### Pattern: Godot Parent-Child UI Pattern (VS_011) ⭐
**Problem**: Manual synchronization between related UI elements (actors and health bars)
**Why This Failed**: 100+ lines of complex synchronization code, race conditions, orphaned elements
**Solution**: Use Godot's parent-child node relationships
```csharp
// ❌ WRONG - Manual synchronization (100+ lines, race conditions)
public class ActorView {
    private Dictionary<ActorId, Node2D> _actors;
    private Dictionary<ActorId, HealthBar> _healthBars;
    
    public void MoveActor(ActorId id, Position pos) {
        _actors[id].Position = pos;
        _healthBars[id].Position = pos + offset; // Manual sync
        // Race conditions, orphaned health bars, complex cleanup
    }
}

// ✅ CORRECT - Parent-child relationship (automatic)
public class ActorView {
    public void CreateActor(ActorId id, Position pos) {
        var actor = _actorScene.Instantiate<Node2D>();
        var healthBar = _healthBarScene.Instantiate<Control>();
        
        actor.AddChild(healthBar); // CRITICAL: Make it a child!
        healthBar.Position = new Vector2(0, -20); // Relative to parent
        
        AddChild(actor);
        // Movement, visibility, cleanup ALL automatic through scene tree
    }
}
```
**Key Insight**: When UI elements move together, make them parent-child nodes. Godot handles position, visibility, and cleanup automatically. This eliminated 60+ lines of synchronization code.


### Pattern: Godot API Casing Requirements
**Problem**: Tween animations not working despite correct logic
**Context**: Godot C# API requires exact property name casing
**Solution**: Use PascalCase for C# properties
```csharp
// ❌ WRONG - GDScript style (works in GDScript, not C#)
tween.TweenProperty(actor, "position", targetPos, 0.2f);

// ✅ CORRECT - C# PascalCase
tween.TweenProperty(actor, "Position", targetPos, 0.2f);

// Also affects other properties:
"rotation" → "Rotation"
"scale" → "Scale"  
"modulate" → "Modulate"
```
**Key**: Godot C# bindings use PascalCase, not snake_case or camelCase.

### Pattern: CallDeferred for Thread Safety
**Problem**: UI updates from async contexts crash or don't execute
**Context**: Godot requires all UI updates on main thread
**Solution**: Always use CallDeferred for UI updates from handlers
```csharp
// ❌ WRONG - Direct UI update from async context
public async Task HandleActorMoved(ActorMovedNotification notification) {
    _actors[notification.ActorId].Visible = false; // CRASH or ignored!
}

// ✅ CORRECT - Deferred execution on main thread
public async Task HandleActorMoved(ActorMovedNotification notification) {
    CallDeferred(() => {
        _actors[notification.ActorId].Visible = false; // Safe!
    });
}
```
**Key**: Any UI update from MediatR handlers MUST use CallDeferred.

---

## ⚠️ Anti-Patterns (DO NOT USE)

These patterns were discovered during development but represent WRONG approaches. They're documented here as warnings of what NOT to do.

### Anti-Pattern: Nested MediatR Handlers
**⛔ STATUS: DO NOT USE - Violates MediatR core principles**
**Problem**: A handler needs to execute logic that another handler implements
**What Happened**: ExecuteAttackCommandHandler called _mediator.Send(DamageActorCommand)
**Wrong Solution**: Injecting IMediator into a handler to call other handlers

```csharp
// ❌ DON'T DO THIS - Creates hidden dependencies and re-triggers pipeline
public class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand, Fin<Unit>>
{
    private readonly IMediator _mediator; // RED FLAG!

    public async Task<Fin<Unit>> Handle(ExecuteAttackCommand request, CancellationToken ct)
    {
        // ... validation logic ...

        // This re-triggers the ENTIRE pipeline (logging, validation, error handling)
        var damageResult = await _mediator.Send(new DamageActorCommand(...));

        return damageResult;
    }
}
```

**Why This Is Wrong**:
- Creates hidden, hard-to-trace dependencies between handlers
- Re-triggers entire pipeline for nested call (performance overhead)
- Makes unit testing complex (need to mock IMediator)
- Violates Explicit Dependencies Principle
- Can cause nested transactions and unexpected behavior

**✅ CORRECT APPROACH**:
Extract shared logic into a domain service:
```csharp
public interface IDamageService
{
    Fin<Unit> ApplyDamage(ActorId target, int damage, string source);
}

// Both handlers use the same service
public class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand, Fin<Unit>>
{
    private readonly IDamageService _damageService; // Explicit dependency

    public Task<Fin<Unit>> Handle(ExecuteAttackCommand request, CancellationToken ct)
    {
        // Direct service call, no pipeline re-entry
        return Task.FromResult(_damageService.ApplyDamage(target, damage, source));
    }
}
```

**Exception**: Publishing domain events (INotification) from handlers is acceptable and encouraged.

### Anti-Pattern: Bridge Pattern for Split Views
**⛔ STATUS: DO NOT USE - Symptom of poor architecture**
**Problem**: Two view systems think they own the same UI elements
**What Happened**: ActorView created health bars but HealthView received updates (with no UI!)
**Wrong Solution**: Bridge pattern to route updates between views

```csharp
// ❌ DON'T DO THIS - This is a band-aid on bad architecture
public class HealthPresenter {
    private readonly IHealthView _healthView;
    private readonly IActorPresenter _actorPresenter; // Bridge hack
    
    public async Task HandleHealthChanged(ActorId id, int newHealth) {
        await _healthView.UpdateHealthAsync(id, newHealth); // Empty view!
        await _actorPresenter.UpdateActorHealthAsync(id, newHealth); // Actual UI
    }
}
```

**Why This Is Wrong**:
- Creates hidden dependencies between presenters
- Indicates split-brain architecture
- Makes code flow confusing
- Symptom of not understanding view ownership

**✅ CORRECT APPROACH**:
1. Consolidate views - if UI elements are related, they belong in same view
2. Use parent-child pattern for related UI elements
3. Clear ownership - one view owns specific UI elements
4. See TD_034 for consolidation assessment

### Anti-Pattern: Static Event Router
**⛔ STATUS: OBSOLETE - Replaced by ADR-010 UIEventBus**
**Problem**: MediatR handlers couldn't reach Godot nodes
**What Happened**: Emergency fix used static handlers to bypass DI lifecycle issues
**Wrong Solution**: Static event registration

```csharp
// ❌ DON'T DO THIS - Violates SOLID, doesn't scale, hard to test
public static class GameManagerEventRouter {
    private static Action<ActorDamagedEvent>? _damageHandler; // Static = bad
    
    public static void RegisterHandlers(Action<ActorDamagedEvent> handler) {
        _damageHandler = handler; // Global state!
    }
}
```

**Why This Is Wrong**:
- Global mutable state
- Violates dependency injection principles
- Can't be mocked for testing
- Memory leaks from static references
- Race conditions in multi-threaded scenarios

**✅ CORRECT APPROACH**: 
Use ADR-010's UIEventBus pattern:
- Proper DI lifecycle management
- WeakReferences prevent memory leaks
- Type-safe subscriptions
- Testable and scalable

```csharp
// ✅ CORRECT - Use UIEventBus from ADR-010
public partial class GameManager : EventAwareNode {
    protected override void SubscribeToEvents() {
        EventBus.Subscribe<ActorDamagedEvent>(this, OnActorDamaged);
    }
}
```

### Anti-Pattern: Manual UI Synchronization
**⛔ STATUS: DO NOT USE - Fight complexity, not the engine**
**Problem**: Keeping related UI elements synchronized (position, visibility, lifecycle)
**Wrong Solution**: Manual tracking and synchronization

```csharp
// ❌ DON'T DO THIS - 100+ lines of unnecessary complexity
public class ActorView {
    private Dictionary<ActorId, Node2D> _actors;
    private Dictionary<ActorId, HealthBar> _healthBars;
    
    public void MoveActor(ActorId id, Position pos) {
        _actors[id].Position = pos;
        _healthBars[id].Position = pos + offset; // Manual sync
        // Race conditions, orphaned elements, complex cleanup...
    }
}
```

**✅ CORRECT APPROACH**: Use parent-child pattern (see above)

---

*These patterns represent hard-won knowledge from production implementation. Each solves a specific problem encountered during development. The anti-patterns show what we learned NOT to do.*