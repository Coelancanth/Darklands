# Production Patterns Catalog

**Last Updated**: 2025-09-11  
**Purpose**: Battle-tested patterns extracted from completed features  
**Source**: Extracted from HANDBOOK.md and post-mortems for focused reference

## 🎯 Pattern Index

### ✅ Recommended Patterns
1. [Value Object Factory with Validation](#pattern-value-object-factory-with-validation-vs_001)
2. [Thread-Safe DI Container](#pattern-thread-safe-di-container-vs_001)
3. [Cross-Presenter Coordination](#pattern-cross-presenter-coordination-vs_010a)
4. [Optional Feedback Service](#pattern-optional-feedback-service-vs_010b)
5. [Death Cascade Coordination](#pattern-death-cascade-coordination-vs_010b)
6. [Godot Node Lifecycle](#pattern-godot-node-lifecycle-vs_010a)
7. [Queue-Based CallDeferred](#pattern-queue-based-calldeferred-td_011)
8. [Godot Parent-Child UI Pattern](#pattern-godot-parent-child-ui-pattern-vs_011) ⭐⭐⭐
9. [Godot API Casing Requirements](#pattern-godot-api-casing-requirements)
10. [CallDeferred for Thread Safety](#pattern-calldeferred-for-thread-safety)

### ⚠️ Anti-Patterns (DO NOT USE)
- ❌ [Bridge Pattern for Split Views](#anti-pattern-bridge-pattern-for-split-views) - Symptom of poor architecture
- ❌ [Static Event Router](#anti-pattern-static-event-router) - Replaced by ADR-010 UIEventBus

---

## 🔨 Extracted Production Patterns

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