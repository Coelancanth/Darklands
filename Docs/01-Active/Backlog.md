# Darklands Development Backlog


**Last Updated**: 2025-09-11 19:05 (Completed TD_036, Created BR_005 - Debug log level filtering issue)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 006
- **Next TD**: 038
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

- Console only shows messages at or above the selected log level
- Manual verification confirms filtering works correctly

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