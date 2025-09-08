# Production Patterns Catalog

**Last Updated**: 2025-09-08  
**Purpose**: Battle-tested patterns extracted from completed features  
**Source**: Extracted from HANDBOOK.md for focused reference

## 🎯 Pattern Index

1. [Value Object Factory with Validation](#pattern-value-object-factory-with-validation-vs_001)
2. [Thread-Safe DI Container](#pattern-thread-safe-di-container-vs_001)
3. [Cross-Presenter Coordination](#pattern-cross-presenter-coordination-vs_010a)
4. [Optional Feedback Service](#pattern-optional-feedback-service-vs_010b)
5. [Death Cascade Coordination](#pattern-death-cascade-coordination-vs_010b)
6. [Godot Node Lifecycle](#pattern-godot-node-lifecycle-vs_010a)
7. [Queue-Based CallDeferred](#pattern-queue-based-calldeferred-td_011)

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

---

*These patterns represent hard-won knowledge from production implementation. Each solves a specific problem encountered during development.*