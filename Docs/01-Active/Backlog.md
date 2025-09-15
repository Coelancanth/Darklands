# Darklands Development Backlog


**Last Updated**: 2025-09-15 20:07 (Tech Lead - Clean Architecture approach: detailed critical fixes for pre-DDD state)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 042
- **Next VS**: 015 


**Protocol**: Check your type's counter → Use that number → Increment the counter → Update timestamp

## 📖 How to Use This Backlog

### 🧠 Owner-Based Protocol

**Each item has a single Owner persona responsible for decisions and progress.**

#### When You Embody a Persona:
1. **Filter** for items where `Owner: [Your Persona]`
3. **Quick Scan** for other statuses you own (<2 min updates)
4. **Update** the backlog before ending your session
5. **Reassign** owner when handing off to next persona


### Default Ownership Rules
| Item Type | Status | Default Owner | Next Owner |
|-----------|--------|---------------|------------|
| **VS** | Proposed | Product Owner | → Tech Lead (breakdown) |
| **VS** | Approved | Tech Lead | → Dev Engineer (implement) |
| **BR** | New | Test Specialist | → Debugger Expert (complex) |
| **TD** | Proposed | Tech Lead | → Dev Engineer (approved) |

### Pragmatic Documentation Approach
- **Quick items (<1 day)**: 5-10 lines inline below
- **Medium items (1-3 days)**: 15-30 lines inline (like VS_001-003 below)
- **Complex items (>3 days)**: Create separate doc and link here

**Rule**: Start inline. Only extract to separate doc if it grows beyond 30 lines or needs diagrams.

### Adding New Items
```markdown
### [Type]_[Number]: Short Name
**Status**: Proposed | Approved | In Progress | Done
**Owner**: [Persona Name]  ← Single responsible persona
**Size**: S (<4h) | M (4-8h) | L (1-3 days) | XL (>3 days)
**Priority**: Critical | Important | Ideas
**Markers**: [ARCHITECTURE] [SAFETY-CRITICAL] etc. (if applicable)

**What**: One-line description
**Why**: Value in one sentence  
**How**: 3-5 technical approach bullets (if known)
**Done When**: 3-5 acceptance criteria
**Depends On**: Item numbers or None

**[Owner] Decision** (date):  ← Added after ultra-think
- Decision rationale
- Risks considered
- Next steps
```

#### 🚨 CRITICAL: VS Items Must Include Architectural Compliance Check
```markdown
**Architectural Constraints** (MANDATORY for VS items):
□ Deterministic: Uses IDeterministicRandom for any randomness (ADR-004)
□ Save-Ready: Entities use records and ID references (ADR-005)  
□ Time-Independent: No wall-clock time, uses turns/actions (ADR-004)
□ Integer Math: Percentages use integers not floats (ADR-004)
□ Testable: Can be tested without Godot runtime (ADR-006)
```

## 🔥 Critical (Do First)
*Blockers preventing other work, production bugs, dependencies for other features*

### TD_042: Enforce Clean Architecture via Project Separation
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer (after critical fixes)
**Size**: M (6-8h)
**Priority**: Important - Architectural integrity enforcement
**Created**: 2025-09-15 20:40 (Tech Lead)
**Markers**: [ARCHITECTURE] [CLEAN-ARCHITECTURE] [BUILD-SYSTEM]

**What**: Separate monolithic Darklands.Core.csproj into layer-specific projects to enforce architectural boundaries at compile-time

**Why**: Current single project allows architectural violations (e.g., Domain referencing Infrastructure). Project separation makes violations impossible, not just discouraged.

**📋 Implementation Plan**:

**Phase 1: Create Layer Projects** (2h):
```
src/
├── Core/
│   ├── Darklands.Domain/         (NO dependencies)
│   ├── Darklands.Application/    (→ Domain only)
│   ├── Darklands.Infrastructure/ (→ Application, Domain)
│   └── Darklands.Presentation/   (→ Application, Domain)
```

**Phase 2: Migrate Code by Layer** (3h):
- Move domain entities/value objects → `Darklands.Domain`
- Move commands/handlers/services → `Darklands.Application`
- Move repositories/external services → `Darklands.Infrastructure`
- Move MVP presenters/view models → `Darklands.Presentation`
- Keep VSA Features/ as-is initially (gradual migration)

**Phase 3: Update References** (2h):
```xml
<!-- Example: Darklands.Application.csproj -->
<ProjectReference Include="..\Darklands.Domain\Darklands.Domain.csproj" />
<!-- CANNOT reference Infrastructure - compile error if violated -->
```

**Phase 4: Update Build & Tests** (1h):
- Update Darklands.sln with new projects
- Update test projects to reference appropriate layers
- Update CI/CD scripts for new structure
- Validate all 662 tests still pass

**✅ Benefits**:
- **Compile-time enforcement**: Domain can't accidentally use Godot types
- **Clear boundaries**: Each layer's dependencies explicit in .csproj
- **Better testability**: Domain/Application tests don't need Godot runtime
- **Parallel builds**: Separate projects can build in parallel
- **Cleaner namespaces**: `Darklands.Domain.Entities` vs mixed paths

**⚠️ Migration Notes**:
- Do AFTER critical fixes (TD_039, TD_040, TD_041) to avoid disruption
- Each step reversible if issues arise
- Keep Features/ folder initially, migrate gradually
- Update imports/namespaces as files move

**Done When**:
- [ ] 4-5 separate layer projects created
- [ ] All code moved to appropriate layer
- [ ] Solution builds successfully
- [ ] All 662 tests pass
- [ ] Architecture tests validate layer dependencies
- [ ] No circular dependencies between projects

### VS_012: Vision-Based Movement System
**Status**: Approved  
**Owner**: Dev Engineer
**Size**: S (2h)
**Priority**: Critical
**Created**: 2025-09-11 00:10
**Updated**: 2025-09-11
**Tech Breakdown**: Movement using vision for scheduler activation

**What**: Movement system where scheduler activates based on vision connections
**Why**: Creates natural tactical combat without explicit modes

**Design** (per ADR-014):
- **Scheduler activation**: When player and hostiles have vision
- **Movement rules**: Adjacent-only when scheduled, pathfinding otherwise
- **Interruption**: Stop movement when enemy becomes visible
- **Fixed cost**: 100 TU per action when scheduled

**Implementation Plan**:
- **Phase 1**: Domain rules (0.5h)
  - Movement validation (adjacent when scheduled)
  - Fixed TU costs (100)
  
- **Phase 2**: Application layer (0.5h)
  - MoveCommand handler with vision check
  - Route to scheduler vs instant movement
  - Console output for states
  
- **Phase 3**: Infrastructure (0.5h)
  - SchedulerActivationService
  - PathfindingService integration
  - Movement interruption handler
  
- **Phase 4**: Integration (0.5h)
  - Wire to existing scheduler
  - Console messages and turn counter
  - Test with multiple scenarios

**Scheduler Activation (Solo)**:
```csharp
bool ShouldUseScheduler() {
    // Solo player - only check player vs monsters
    return monsters.Any(m => 
        m.State != Dormant && 
        (visionService.CanSee(player, m) || visionService.CanSee(m, player))
    );
}
```

**Movement Flow**:
```csharp
if (ShouldUseScheduler()) {
    // Tactical movement
    if (!Position.IsAdjacent(from, to)) {
        return "Only adjacent moves when enemies visible";
    }
    scheduler.Schedule(new MoveAction(actor, to, 100));
} else {
    // Instant travel with interruption check
    foreach (var step in path) {
        actor.Position = step;
        if (ShouldUseScheduler()) {
            return "Movement interrupted - enemy spotted!";
        }
    }
}
```

**Console Examples**:
```
// No vision - instant
> move to (30, 30)
[Traveling...]
You arrive at (30, 30)

// Vision exists - tactical
> move to (10, 10)
[Enemies visible - tactical movement]
> move north
[Turn 1] You move north (100 TU)
[Turn 2] Goblin moves west (100 TU)

// Interruption
> move to (50, 50)
[Traveling...]
Movement interrupted at (25, 25) - Orc spotted!
```

**Done When**:
- Scheduler activates on vision connections
- Adjacent-only when scheduled
- Pathfinding when not scheduled
- Movement interrupts on new vision
- Turn counter during tactical movement
- Clear console messages

**Architectural Constraints**:
☑ Deterministic: Fixed TU costs
☑ Save-Ready: Position state only
☑ Time-Independent: Turn-based
☑ Integer Math: Tile movement
☑ Testable: Clear state transitions

**Depends On**: 
- VS_011 (Vision System) - ✅ Infrastructure foundation complete (Phase 3)
- VS_014 (A* Pathfinding) - ⏳ Required for non-adjacent movement
**Next Step**: Implement VS_014 first, then begin VS_012


### VS_014: A* Pathfinding Foundation
**Status**: Approved
**Owner**: Dev Engineer  
**Size**: S (3h)
**Priority**: Critical
**Created**: 2025-09-11 18:12
**Tech Breakdown**: Complete by Tech Lead

**What**: Implement A* pathfinding algorithm with visual path display
**Why**: Foundation for VS_012 movement system and all future tactical movement

**Implementation Plan**:

**Phase 1: Domain Algorithm (1h)**
- Create `Domain.Pathfinding.AStarPathfinder`
- Pure functional implementation with no dependencies
- Deterministic tie-breaking (use Position.X then Y for equal F-scores)
- Support diagonal movement (8-way) with correct costs (100 ortho, 141 diagonal)
- Handle blocked tiles from Grid.Tile.IsWalkable

```csharp
public static class AStarPathfinder
{
    public static Option<ImmutableList<Position>> FindPath(
        Position start,
        Position goal,
        Grid grid,
        bool allowDiagonal = true)
    {
        // A* with deterministic tie-breaking
        // Returns None if no path exists
    }
}
```

**Phase 2: Application Service (0.5h)**
- Create `IPathfindingService` interface in Core
- `FindPathQuery` and handler for CQRS pattern
- Cache recent paths for performance (LRU cache, 32 entries)

**Phase 3: Infrastructure (0.5h)**
- Implement `PathfindingService` with caching
- Performance monitoring (target: <10ms for 50 tiles)
- Path validation before returning

**Phase 4: Presentation (1h)**
- Path visualization in GridPresenter
- Semi-transparent overlay tiles (blue for path, green for destination)
- Update on mouse hover to show potential paths
- Clear path display on movement/action

**Visual Feedback Design**:
```
Path tile: Modulate(0.5, 0.5, 1.0, 0.5) - Semi-transparent blue
Destination: Modulate(0.5, 1.0, 0.5, 0.7) - Semi-transparent green  
Current hover: Updates in real-time as mouse moves
Animation: Gentle pulse on destination tile
```

**Done When**:
- A* finds optimal paths deterministically
- Diagonal movement works correctly (1.41x cost)
- Path visualizes on grid before movement
- Performance <10ms for typical paths (50 tiles)
- Handles no-path-exists gracefully (returns None)
- All tests pass including edge cases

**Test Scenarios**:
1. Straight line path (no obstacles)
2. Path around single wall
3. Maze navigation
4. No path exists (surrounded)
5. Diagonal preference when optimal

**Architectural Constraints**:
☑ Deterministic: Consistent tie-breaking rules
☑ Save-Ready: Paths are transient, not saved
☑ Time-Independent: Pure algorithm
☑ Integer Math: Use 100/141 for movement costs
☑ Testable: Pure domain function

**Dependencies**: None (foundation feature)
**Blocks**: VS_012 (Movement System)

### TD_039: Remove Task.Run Violations (Pre-DDD Critical Fix)
**Status**: Done
**Owner**: Tech Lead → Dev Engineer (for implementation)
**Size**: S (2h) - Based on commit 65a22c1 implementation
**Priority**: Critical - ADR-009 violations causing race conditions
**Created**: 2025-09-15 20:07 (Tech Lead)
**Reference Implementation**: Commit 65a22c1 (TD_050 equivalent)
**Markers**: [ARCHITECTURE] [ADR-009] [CRITICAL-FIX] [CLEAN-ARCHITECTURE]

**What**: Remove Task.Run violations from GameManager, GridView, and ActorPresenter
**Why**: Task.Run in turn-based games creates concurrency where sequential processing is needed (ADR-009)

**🚨 Critical Violations to Fix**:
1. **GameManager.cs line 57**: Task.Run for async initialization
2. **GridView.cs line 322**: Task.Run for tile click handling
3. **ActorPresenter.cs lines 81, 94, 111**: Task.Run for actor display operations

**📋 Implementation Plan** (Based on proven 65a22c1 approach):

**Phase 1: GameManager.cs Fix** (30min):
```csharp
// BEFORE (line 57):
_ = Task.Run(async () => {
    await CompleteInitializationAsync();
});

// AFTER (sequential per ADR-009):
try {
    CompleteInitializationAsync().GetAwaiter().GetResult();
} catch (Exception ex) {
    // Error handling
}
```

**Phase 2: GridView.cs Fix** (45min):
```csharp
// BEFORE (line 322):
_ = Task.Run(async () => {
    await _presenter.HandleTileClickAsync(gridPosition);
});

// AFTER (use CallDeferred for Godot main-thread safety):
CallDeferred(MethodName.HandleTileClickDeferred, gridPosition);

// Add new method:
private void HandleTileClickDeferred(Position gridPosition) {
    _presenter.HandleTileClickAsync(gridPosition).GetAwaiter().GetResult();
}
```

**Phase 3: ActorPresenter.cs Fix** (45min):
```csharp
// BEFORE (lines 81, 94, 111):
_ = Task.Run(async () => {
    await View.DisplayActorAsync(actorId, position, type);
});

// AFTER (.GetAwaiter().GetResult() pattern):
try {
    View.DisplayActorAsync(actorId, position, type).GetAwaiter().GetResult();
} catch (Exception ex) {
    _logger.Log(LogLevel.Error, LogCategory.System, "Display actor failed: {0}", ex.Message);
}
```

**Success Criteria**:
- [x] No Task.Run calls in GameManager.cs, GridView.cs, ActorPresenter.cs
- [x] All async operations use .GetAwaiter().GetResult() pattern
- [x] Godot main-thread safety preserved with CallDeferred
- [x] All existing tests still pass
- [x] No new race conditions introduced

**Tech Lead Notes**: This fix eliminates the BR_007 race condition root cause and enforces ADR-009 sequential processing.

**Dev Engineer Decision** (2025-09-15):
- Implementation mirrored remote commit 65a22c1 pattern (sequentializing async and deferring to Godot main thread).
- Replaced Task.Run with synchronous `.GetAwaiter().GetResult()` in `GameManager.cs` and `ActorPresenter.cs`.
- Replaced Task.Run with `CallDeferred(nameof(HandleTileClickDeferred), position)` in `Views/GridView.cs`, executing handler synchronously on the main thread.
- Verified no remaining Task.Run in production code via search; remaining usages are confined to tests and mock services.
- Build succeeded; tests passed (664 passed, 2 skipped).
- Risk of deadlock mitigated by invoking from the main thread and using Godot deferred calls where UI is involved.

### TD_040: Replace Double Math with Fixed-Point for Determinism (Pre-DDD Critical Fix)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer (for implementation)
**Size**: S (3h) - Based on commit 63746e3 implementation
**Priority**: Critical - ADR-004 violations breaking save compatibility
**Created**: 2025-09-15 20:07 (Tech Lead)
**Reference Implementation**: Commit 63746e3 (TD_051 equivalent)
**Markers**: [ARCHITECTURE] [ADR-004] [CRITICAL-FIX] [DETERMINISM]

**What**: Replace floating-point calculations in ShadowcastingFOV with Fixed-point arithmetic
**Why**: Double math breaks determinism across platforms (ARM vs x86), violating ADR-004

**🚨 Critical Violation to Fix**:
- **ShadowcastingFOV.cs lines 98-100**: `double tileSlopeHigh/tileSlopeLow` calculations

**📋 Implementation Plan** (Based on proven 63746e3 approach):

**Phase 1: Create Fixed Type** (1h):
```csharp
// Add to src/Core/Domain/Determinism/Fixed.cs
public readonly struct Fixed : IComparable<Fixed>
{
    private readonly int _value;
    private const int SCALE = 65536; // 16.16 fixed point

    public static Fixed FromInt(int value) => new(value * SCALE);
    public static Fixed One => new(SCALE);
    public static Fixed Half => new(SCALE / 2);
    public static Fixed Zero => new(0);

    // Arithmetic operators
    public static Fixed operator +(Fixed a, Fixed b) => new(a._value + b._value);
    public static Fixed operator -(Fixed a, Fixed b) => new(a._value - b._value);
    public static Fixed operator *(Fixed a, Fixed b) => new((int)((long)a._value * b._value / SCALE));
    public static Fixed operator /(Fixed a, Fixed b) => new((int)((long)a._value * SCALE / b._value));

    // Comparison operators
    public static bool operator >(Fixed a, Fixed b) => a._value > b._value;
    public static bool operator <(Fixed a, Fixed b) => a._value < b._value;
}
```

**Phase 2: Update ShadowcastingFOV** (1.5h):
```csharp
// BEFORE (lines 98-100):
double tileSlopeHigh = distance == 0 ? 1.0 : (angle + 0.5) / (distance - 0.5);
double tileSlopeLow = (angle - 0.5) / (distance + 0.5);

// AFTER (Fixed-point arithmetic):
Fixed tileSlopeHigh = distance == 0 ? Fixed.One :
    (Fixed.FromInt(angle) + Fixed.Half) / (Fixed.FromInt(distance) - Fixed.Half);
Fixed tileSlopeLow = (Fixed.FromInt(angle) - Fixed.Half) / (Fixed.FromInt(distance) + Fixed.Half);
```

**Phase 3: Update Method Signatures** (30min):
```csharp
// Change CastShadow parameters from double to Fixed:
private static void CastShadow(
    Position origin,
    int range,
    Grid grid,
    int octant,
    HashSet<Position> visible,
    int distance,
    Fixed viewSlopeHigh,  // Changed from double
    Fixed viewSlopeLow)   // Changed from double
```

**Success Criteria**:
- [ ] No double/float arithmetic in ShadowcastingFOV.cs
- [ ] Fixed-point arithmetic maintains identical algorithmic behavior
- [ ] All vision tests still pass with identical results
- [ ] Cross-platform determinism verified (integer math only)
- [ ] Save/load compatibility preserved

**Tech Lead Notes**: This ensures FOV calculations are identical across all platforms and compiler optimizations.

### TD_041: Implement Production-Ready DI Lifecycle Management (Pre-DDD Critical Fix)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer (for implementation)
**Size**: M (4h) - Based on commit 92c3e93 implementation
**Priority**: Important - Memory leaks and scope management issues
**Created**: 2025-09-15 20:07 (Tech Lead)
**Reference Implementation**: Commit 92c3e93 (TD_052 equivalent)
**Markers**: [INFRASTRUCTURE] [DI] [MEMORY-MANAGEMENT] [CLEAN-ARCHITECTURE]

**What**: Implement proper DI scope management for Godot nodes without memory leaks
**Why**: Current GameStrapper approach causes memory leaks and improper service lifetimes

**📋 Implementation Plan** (Based on proven 92c3e93 approach):

**Phase 1: Create IScopeManager Interface** (1h):
```csharp
// src/Core/Infrastructure/Services/IScopeManager.cs
public interface IScopeManager
{
    bool TryCreateScope(Node node, out IServiceScope scope);
    bool TryGetScope(Node node, out IServiceScope scope);
    void DisposeScope(Node node);
    T GetService<T>(Node node) where T : notnull;
}
```

**Phase 2: Implement GodotScopeManager** (2h):
```csharp
// Create with ConditionalWeakTable to prevent memory leaks
public class GodotScopeManager : IScopeManager
{
    private readonly ConditionalWeakTable<Node, IServiceScope> _nodeScopes;
    private readonly ConcurrentDictionary<Node, IServiceScope> _scopeCache;

    // O(1) cached scope resolution
    // Automatic cleanup when nodes are freed
}
```

**Phase 3: ServiceLocator Autoload** (1h):
```csharp
// Create autoload for scene-based scope management
public class ServiceLocator : Node
{
    private static IScopeManager? _scopeManager;

    public static T GetService<T>(Node context) where T : notnull
    {
        return _scopeManager?.GetService<T>(context)
               ?? GameStrapper.Services.GetRequiredService<T>();
    }
}
```

**Success Criteria**:
- [ ] No memory leaks from orphaned node scopes
- [ ] O(1) service resolution performance
- [ ] Graceful fallback to GameStrapper when scope unavailable
- [ ] Thread-safe scope management
- [ ] Automatic cleanup when nodes are freed

**Tech Lead Notes**: This provides production-ready scope management without the complexity of full DDD bounded contexts.


## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

<!-- TD_031 moved to permanent archive (2025-09-10 21:02) - TimeUnit TU refactor completed successfully -->


### TD_032: Fix Namespace-Class Collisions (Grid.Grid, Actor.Actor)
**Status**: Approved
**Owner**: Dev Engineer
**Size**: S (4h)
**Priority**: Important
**Created**: 2025-09-11
**Complexity**: 2/10
**ADR**: ADR-015

**What**: Refactor namespace structure to eliminate collisions
**Why**: Current `Domain.Grid.Grid` and `Domain.Actor.Actor` patterns force verbose code and confuse developers

**Implementation Plan** (per ADR-015):
1. **Domain Layer** (2h):
   - Rename `Grid` → `WorldGrid` in new `Domain.Spatial` namespace
   - Move `Actor` to `Domain.Entities` namespace
   - Reorganize into bounded contexts: Spatial, Entities, TurnBased, Perception
   
2. **Application/Infrastructure** (1h):
   - Update all imports and references
   - No structural changes, just namespace updates
   
3. **Tests** (1h):
   - Update test imports
   - Verify all tests pass

**Done When**:
- No namespace-class collisions remain
- All tests pass without warnings
- Architecture fitness tests validate structure
- IntelliSense shows clear suggestions

**Technical Notes**:
- Single atomic PR for entire refactoring
- No behavior changes, pure reorganization
- Follow bounded context pattern from ADR-015





### TD_035: Standardize Error Handling in Infrastructure Services
**Status**: Approved
**Owner**: Dev Engineer
**Size**: S (3h)
**Priority**: Important
**Created**: 2025-09-11 18:07
**Complexity**: 3/10

**What**: Replace remaining try-catch blocks with Fin<T> in infrastructure services
**Why**: Inconsistent error handling breaks functional composition and makes debugging harder

**Scope** (LIMITED TO):
1. **PersistentVisionStateService** (7 try-catch blocks):
   - GetVisionState, UpdateVisionState, ClearVisionState methods
   - Convert to Try().Match() pattern with Fin<T>
   
2. **GridPresenter** (3 try-catch in event handlers):
   - OnActorSpawned, OnActorMoved, OnActorRemoved
   - Wrap in functional error handling
   
3. **ExecuteAttackCommandHandler** (mixed side effects):
   - Extract logging to separate methods
   - Isolate side effects from business logic

**NOT IN SCOPE** (critical boundaries):
- Performance-critical loops in ShadowcastingFOV (keep imperative)
- ConcurrentDictionary in caching (proven pattern, don't change)
- Working switch statements (already readable)
- Domain layer (already fully functional)

**Implementation Guidelines**:
```csharp
// Pattern to follow:
public Fin<T> ServiceMethod() =>
    Try(() => 
    {
        // existing logic
    })
    .Match(
        Succ: result => FinSucc(result),
        Fail: ex => FinFail<T>(Error.New("Context-specific message", ex))
    );
```

**Done When**:
- Zero try-catch blocks in listed services
- All errors flow through Fin<T> consistently
- Side effects isolated into dedicated methods
- Performance unchanged (measure before/after)
- All existing tests still pass

**Tech Lead Notes**:
- This is about consistency, not FP purity
- Keep changes mechanical and predictable
- Don't get creative - follow existing patterns
- If performance degrades, revert that specific change



### VS_013: Basic Enemy AI
**Status**: Proposed
**Owner**: Product Owner → Tech Lead
**Size**: M (4-8h)  
**Priority**: Important
**Created**: 2025-09-10 19:03

**What**: Simple but effective enemy AI for combat testing
**Why**: Need opponents to validate combat system and create gameplay loop
**How**:
- Decision tree for action selection (move/attack/wait)
- Target prioritization (closest/weakest/most dangerous)
- Basic pathfinding to reach targets
- Flee behavior when low health
**Done When**:
- Enemies move towards player intelligently
- Enemies attack when in range
- AI makes decisions based on game state
- Different enemy types show different behaviors
- AI actions integrate with scheduler

**Architectural Constraints** (MANDATORY):
☑ Deterministic: AI decisions based on seeded random
☑ Save-Ready: AI state fully serializable
☑ Time-Independent: Decisions based on game state not time
☑ Integer Math: All AI calculations use integers
☑ Testable: AI logic can be unit tested

---

## 💡 Future Ideas (Not Current Priority)
*Features and systems to consider when foundational work is complete*

### IDEA_001: Life-Review/Obituary System
**Status**: Future Consideration
**Owner**: Unassigned
**Size**: L (2-3 days)
**Priority**: Ideas
**Created**: 2025-09-12

**What**: Battle Brothers-style obituary and company history system
**Why**: Creates narrative and emotional attachment to characters
**How**: 
- Track all character events (battles, injuries, level-ups, deaths)
- Generate procedural obituaries for fallen characters
- Company timeline showing major events
- Statistics and achievements per character
**Technical Approach**: 
- Separate IGameHistorian system (not debug logging)
- SQLite or JSON for structured event storage
- Query system for generating reports
**Reference**: ADR-007 Future Considerations section

### IDEA_002: Economy Analytics System  
**Status**: Future Consideration
**Owner**: Unassigned
**Size**: M (1-2 days)
**Priority**: Ideas
**Created**: 2025-09-12

**What**: Track economic metrics for balance analysis
**Why**: Balance item prices, loot tables, and gold flow
**How**:
- Record all transactions (buy/sell/loot/reward)
- Aggregate metrics (avg gold per battle, popular items)
- Export reports for balance decisions
**Technical Approach**:
- Separate IEconomyTracker system (not debug logging)
- Aggregated analytics database
- Periodic report generation
**Reference**: ADR-007 Future Considerations section

### IDEA_003: Player Analytics Dashboard
**Status**: Future Consideration  
**Owner**: Unassigned
**Size**: L (3-4 days)
**Priority**: Ideas
**Created**: 2025-09-12

**What**: Comprehensive player behavior analytics
**Why**: Understand difficulty spikes, player preferences, death patterns
**How**:
- Heat maps of death locations
- Progression funnel analysis
- Play session patterns
- Difficulty curve validation
**Technical Approach**:
- Separate IPlayerAnalytics system (not debug logging)
- Event stream processing
- Visual dashboard for analysis
**Reference**: ADR-007 Future Considerations section

## 📋 Quick Reference

**Priority Decision Framework:**
1. **Blocking other work?** → 🔥 Critical
2. **Current milestone?** → 📈 Important  
3. **Everything else** → 💡 Ideas

**Work Item Types:**
- **VS_xxx**: Vertical Slice (new feature) - Product Owner creates
- **BR_xxx**: Bug Report (investigation) - Test Specialist creates, Debugger owns
- **TD_xxx**: Technical Debt (refactoring) - Anyone proposes → Tech Lead approves

*Notes:*
- *Critical bugs are BR items with 🔥 priority*
- *TD items need Tech Lead approval to move from "Proposed" to actionable*



<!-- TD_017 and TD_019 moved to permanent archive (2025-09-09 17:53) -->

---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*