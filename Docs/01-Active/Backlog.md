# Darklands Development Backlog


**Last Updated**: 2025-09-16 01:00 (Tech Lead - Reorganized with dependency chains after ADR consistency review)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 047
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

## 🔗 Dependency Chain Analysis

**EXECUTION ORDER**: Following this sequence ensures no item is blocked by missing dependencies.

### Chain 1: Architecture Foundation (MUST be first)
```
TD_046 → (All other work depends on this)
├─ Enables: Clean separation of concerns
├─ Enables: Compile-time MVP enforcement
└─ Blocks: All VS and complex TD items until complete
```

### Chain 2: Movement & Vision System
```
VS_014 (A* Pathfinding) → VS_012 (Vision-Based Movement) → VS_013 (Enemy AI)
├─ VS_014: Foundation for all movement
├─ VS_012: Tactical movement using pathfinding
└─ VS_013: AI needs movement system to function
```

### Chain 3: Technical Debt Cleanup
```
TD_035 (Error Handling) → TD_046 completion
├─ Can be done in parallel with TD_046
└─ Should be completed before new feature development
```

### Chain 4: Future Features (After foundations)
```
All IDEA_* items depend on:
├─ Chain 1 (Architecture) - COMPLETE
├─ Chain 2 (Movement/Vision) - COMPLETE
└─ Chain 3 (Technical Debt) - COMPLETE
```

## 🚀 Ready for Immediate Execution

*Items with no blocking dependencies, approved and ready to start*

### TD_046: Minimal Project Separation with Feature Namespaces
**Status**: Approved - Ready for implementation
**Owner**: Dev Engineer
**Size**: L (8h total) - Project extraction (3h) + EventAwareNode refactor (1h) + Feature namespaces (4h)
**Priority**: CRITICAL - Blocks all other development
**Created**: 2025-09-15 23:15 (Tech Lead)
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to immediate execution after dependency analysis)
**Complexity**: 4/10 - Increased due to EventAwareNode refactoring
**Markers**: [ARCHITECTURE] [CLEAN-ARCHITECTURE] [FEATURE-ORGANIZATION] [BREAKING-CHANGE] [CHAIN-1-FOUNDATION]
**ADRs**: ADR-021 (minimal separation with MVP), ADR-018 (DI alignment updated)
**Migration Plan**: Docs/01-Active/TD_046_Migration_Plan.md

**What**: Extract Domain and Presentation to separate projects + reorganize into feature namespaces
**Why**: Enforce architectural purity at compile-time while eliminating namespace collisions

**DEPENDENCY CHAIN**: This is Chain 1 - MUST complete before any other development work
- Blocks: VS_012, VS_013, VS_014, TD_035, all future features
- Enables: Compile-time MVP enforcement, clean separation of concerns

**Done When**:
- [ ] Domain project created and referenced
- [ ] Presentation project created with MVP enforcement
- [ ] EventAwareNode converted to EventAwarePresenter pattern
- [ ] Feature namespaces applied consistently across all layers
- [ ] No namespace-class collisions exist
- [ ] All 673 tests pass
- [ ] Architecture test validates domain purity
- [ ] IntelliSense shows clear, intuitive suggestions

### TD_035: Standardize Error Handling in Infrastructure Services
**Status**: Approved - Can run parallel with TD_046
**Owner**: Dev Engineer
**Size**: S (3h)
**Priority**: Important - Technical debt cleanup
**Created**: 2025-09-11 18:07
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to immediate execution)
**Complexity**: 3/10
**Markers**: [TECHNICAL-DEBT] [ERROR-HANDLING] [CHAIN-3-CLEANUP]

**What**: Replace remaining try-catch blocks with Fin<T> in infrastructure services
**Why**: Inconsistent error handling breaks functional composition and makes debugging harder

**DEPENDENCY CHAIN**: Chain 3 - Can run parallel with TD_046
- Compatible with: TD_046 (different code areas)
- Should complete: Before new VS development

**Scope** (LIMITED TO):
1. **PersistentVisionStateService** (7 try-catch blocks)
2. **GridPresenter** (3 try-catch in event handlers)
3. **ExecuteAttackCommandHandler** (mixed side effects)

**Done When**:
- [ ] Zero try-catch blocks in listed services
- [ ] All errors flow through Fin<T> consistently
- [ ] Side effects isolated into dedicated methods
- [ ] Performance unchanged (measure before/after)
- [ ] All existing tests still pass

## 📋 Blocked - Waiting for Dependencies

*Items that cannot start until blocking dependencies are resolved*

### VS_014: A* Pathfinding Foundation
**Status**: Approved - BLOCKED by TD_046
**Owner**: Dev Engineer
**Size**: S (3h)
**Priority**: Critical - Foundation for movement system
**Created**: 2025-09-11 18:12
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to blocked section)
**Markers**: [MOVEMENT] [PATHFINDING] [CHAIN-2-MOVEMENT] [BLOCKED]
**Blocking Dependency**: TD_046 (project structure must be established first)

**What**: Implement A* pathfinding algorithm with visual path display
**Why**: Foundation for VS_012 movement system and all future tactical movement

**DEPENDENCY CHAIN**: Chain 2 - Step 1 (Movement Foundation)
- Blocked by: TD_046 (architectural foundation)
- Enables: VS_012 (Vision-Based Movement)
- Blocks: VS_013 (Enemy AI needs movement)

**Done When**:
- [ ] A* finds optimal paths deterministically
- [ ] Diagonal movement works correctly (1.41x cost)
- [ ] Path visualizes on grid before movement
- [ ] Performance <10ms for typical paths (50 tiles)
- [ ] Handles no-path-exists gracefully (returns None)
- [ ] All tests pass including edge cases

**Architectural Constraints**:
☑ Deterministic: Consistent tie-breaking rules
☑ Save-Ready: Paths are transient, not saved
☑ Time-Independent: Pure algorithm
☑ Integer Math: Use 100/141 for movement costs
☑ Testable: Pure domain function

### VS_012: Vision-Based Movement System
**Status**: Approved - BLOCKED by VS_014
**Owner**: Dev Engineer
**Size**: S (2h)
**Priority**: Critical
**Created**: 2025-09-11 00:10
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to blocked section)
**Markers**: [MOVEMENT] [VISION] [CHAIN-2-MOVEMENT] [BLOCKED]
**Blocking Dependency**: VS_014 (A* Pathfinding Foundation)

**What**: Movement system where scheduler activates based on vision connections
**Why**: Creates natural tactical combat without explicit modes

**DEPENDENCY CHAIN**: Chain 2 - Step 2 (Vision-Based Movement)
- Blocked by: VS_014 (needs pathfinding for non-adjacent movement)
- Enables: VS_013 (Enemy AI needs movement system)

**Architectural Constraints**:
☑ Deterministic: Fixed TU costs
☑ Save-Ready: Position state only
☑ Time-Independent: Turn-based
☑ Integer Math: Tile movement
☑ Testable: Clear state transitions

### VS_013: Basic Enemy AI
**Status**: Proposed - BLOCKED by VS_012
**Owner**: Product Owner → Tech Lead
**Size**: M (4-8h)
**Priority**: Important
**Created**: 2025-09-10 19:03
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to blocked section)
**Markers**: [AI] [COMBAT] [CHAIN-2-MOVEMENT] [BLOCKED]
**Blocking Dependency**: VS_012 (Vision-Based Movement System)

**What**: Simple but effective enemy AI for combat testing
**Why**: Need opponents to validate combat system and create gameplay loop

**DEPENDENCY CHAIN**: Chain 2 - Step 3 (Enemy Intelligence)
- Blocked by: VS_012 (AI needs movement system to function)
- Enables: Complete tactical combat gameplay loop

**Architectural Constraints**:
☑ Deterministic: AI decisions based on seeded random
☑ Save-Ready: AI state fully serializable
☑ Time-Independent: Decisions based on game state not time
☑ Integer Math: All AI calculations use integers
☑ Testable: AI logic can be unit tested

## 🗂️ Archived - Completed or Rejected

*Items moved out of active development*

### TD_045: Enforce Clean Architecture via Project Separation
**Status**: REJECTED - Over-engineered per architectural review
**Owner**: Tech Lead
**Size**: M (6-8h) - But complexity questionable
**Priority**: N/A - Superseded by TD_046
**Created**: 2025-09-15 20:40 (Tech Lead)
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to archived after dependency analysis)
**Markers**: [ARCHITECTURE] [CLEAN-ARCHITECTURE] [BUILD-SYSTEM] [REJECTED]
**Related**: ADR-019 (rejected for same reasons)

**What**: Separate monolithic Darklands.Core.csproj into layer-specific projects to enforce architectural boundaries at compile-time

**Why**: Current single project allows architectural violations (e.g., Domain referencing Infrastructure). Project separation makes violations impossible, not just discouraged.

**Tech Lead Note**: REJECTED - See TD_046 for simpler approach that achieves same goals

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

**Tech Lead Note**: REJECTED - See TD_046 Migration Plan for simpler approach

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

## 🔄 Execution Summary
**Status**: Approved - Ready for implementation
**Owner**: Dev Engineer
**Size**: L (8h total) - Project extraction (3h) + EventAwareNode refactor (1h) + Feature namespaces (4h)
**Priority**: Important - Architectural clarity and purity enforcement
**Created**: 2025-09-15 23:15 (Tech Lead)
**Updated**: 2025-09-16 00:30 (Tech Lead - Created detailed migration plan)
**Complexity**: 4/10 - Increased due to EventAwareNode refactoring
**Markers**: [ARCHITECTURE] [CLEAN-ARCHITECTURE] [FEATURE-ORGANIZATION] [BREAKING-CHANGE]
**ADRs**: ADR-021 (minimal separation with MVP), ADR-018 (DI alignment updated)
**Migration Plan**: Docs/01-Active/TD_046_Migration_Plan.md

**What**: Extract Domain and Presentation to separate projects + reorganize into feature namespaces
**Why**: Enforce architectural purity at compile-time while eliminating namespace collisions

**Final Project Structure**:
```
Darklands.csproj            → Godot Views & Entry (has Godot references)
Darklands.Domain.csproj     → Pure domain logic (NO external dependencies)
Darklands.Core.csproj       → Application & Infrastructure (NO Godot references)
Darklands.Presentation.csproj → Presenters & View Interfaces (NO Godot references)
```

**Combined Implementation Plan**:

### Part 1: Project Extraction (3h)
1. **Create Domain Project** (30min):
   - Create `src/Darklands.Domain/Darklands.Domain.csproj`
   - Add to solution
   - Reference from Core project

2. **Move Domain Types** (1h):
   - Move entire `src/Domain/` to `src/Darklands.Domain/`
   - Update namespace from `Darklands.Core.Domain` to `Darklands.Domain`

3. **Fix References** (30min):
   - Update all using statements
   - Verify build succeeds

### Part 2: Feature Organization (4h)
Apply to ALL layers (Domain, Application, Infrastructure, Presentation):

```
Domain.World/      → Grid, Tile, Position
Domain.Characters/ → Actor, Health, ActorState
Domain.Combat/     → Damage, TimeUnit, AttackAction
Domain.Vision/     → VisionRange, VisionState, ShadowcastingFOV

Application.World.Commands/
Application.Characters.Handlers/
Application.Combat.Commands/
Application.Vision.Queries/

(Similar for Infrastructure and Presentation)
```

**Done When**:
- [ ] Domain project created and referenced
- [ ] Domain types extracted with no external dependencies
- [ ] Feature namespaces applied consistently across all layers
- [ ] No namespace-class collisions exist
- [ ] All 662 tests pass
- [ ] Architecture test validates domain purity
- [ ] IntelliSense shows clear, intuitive suggestions

**Benefits**:
- Compile-time domain purity enforcement
- No namespace collisions
- Intuitive feature organization
- Minimal complexity (just 3 projects total)
- Aligns with post-TD_042 simplification





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

## 💡 Future Ideas - Chain 4 Dependencies

*Features and systems to consider when foundational work is complete*

**DEPENDENCY CHAIN**: All future ideas are Chain 4 - blocked until prerequisites complete:
- ✅ Chain 1 (Architecture Foundation): TD_046 → MUST COMPLETE FIRST
- ⏳ Chain 2 (Movement/Vision): VS_014 → VS_012 → VS_013
- ⏳ Chain 3 (Technical Debt): TD_035
- 🚫 Chain 4 (Future Features): Cannot start until Chains 1-3 complete

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

## 🔄 Execution Summary

**Current State**: All items properly organized by dependency chains after ADR consistency review
**Critical Path**: TD_046 → VS_014 → VS_012 → VS_013 → Future Features

**Next Actions**:
1. **Immediate**: Execute TD_046 (8h) - Architectural foundation that blocks all other work
2. **Parallel**: Execute TD_035 (3h) - Technical debt cleanup, compatible with TD_046
3. **After Chain 1**: Begin VS_014 → VS_012 → VS_013 sequence (7h total)
4. **Future**: Evaluate IDEA_* items once foundations are complete

**Estimated Timeline**:
- ✅ **Week 1**: TD_046 + TD_035 (Architecture + Cleanup)
- ⏳ **Week 2**: VS_014 + VS_012 (Movement Foundation)
- ⏳ **Week 3**: VS_013 (Enemy AI) + Polish
- 🔮 **Future**: Feature expansion with solid architectural foundation

## 📋 Quick Reference

**Dependency Chain Rules:**
- 🚫 **Never** start items with blocking dependencies
- ✅ **Always** complete architectural foundations first
- ⚡ **Parallel** work only when items are in different code areas
- 🔄 **Re-evaluate** priorities after each chain completion

**Work Item Types:**
- **TD_xxx**: Technical Debt (refactoring) - Anyone proposes → Tech Lead approves
- **VS_xxx**: Vertical Slice (new feature) - Product Owner creates, Tech Lead breaks down
- **BR_xxx**: Bug Report (investigation) - Test Specialist creates, Debugger owns
- **IDEA_xxx**: Future Features - No owner until prerequisite chains complete

---
*Single Source of Truth for all Darklands development work. Organized by dependency chains for optimal execution order.*