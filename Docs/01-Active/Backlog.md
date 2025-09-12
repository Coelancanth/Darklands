# Darklands Development Backlog


**Last Updated**: 2025-09-12 10:07 (Tech Lead created ADR-007 for logger architecture, added future analytics ideas)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 007
- **Next TD**: 039
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

### TD_038: Complete Logger System Rewrite
**Status**: Architecture Approved → Ready for Implementation  
**Owner**: Tech Lead → Dev Engineer  
**Size**: M (6h total → 3.5h completed, 2.5h remaining)
**Priority**: Critical  
**Complexity**: 3/10 (Architecture simplified via ADR-007)
**Created**: 2025-09-12  
**Tech Lead Decision** (2025-09-12): **ADR-007 Created - Unified Logger Architecture**  
**Dev Engineer Progress** (2025-09-12): Core implementation complete, architectural issue discovered  

**What**: Delete all existing logger implementations and rebuild from scratch with single, elegant design
**Why**: Current system has 4+ parallel logger implementations creating unmaintainable complexity

**Root Cause**: Years of incremental "fixes" created 4+ parallel logging systems:
1. `ILogger` (Serilog) - Used by 18+ Application files
2. `ILogger<T>` (Microsoft Extensions) - Used by 8+ Infrastructure files  
3. `ICategoryLogger` with two broken implementations (CategoryFilteredLogger, GodotCategoryLogger)
4. Direct `GD.Print()` calls scattered throughout Godot layer

**Tech Lead Decision** (2025-09-12): **Complete rewrite from scratch**
- Delete ALL existing logger code
- Build ONE unified logger with clean architecture
- Support all requirements: categories, filtering, rich output, runtime toggles

## 🚨 **Dev Engineer Implementation Report** (2025-09-12):

### ✅ **COMPLETED Successfully:**
1. **Phase 1: UnifiedLogger Implementation** 
   - ✅ Created fully functional UnifiedLogger with rich console formatting
   - ✅ Context detection (Godot vs Tests)  
   - ✅ Template formatting with fallback error handling
   - ✅ Complete interface compliance with ICategoryLogger

2. **Phase 2: DI Integration**
   - ✅ Successfully integrated with GameManager composition root
   - ✅ All integration tests pass (34/34)
   - ✅ Rich Godot console output working with proper categories and colors

3. **Phase 3: View Layer Migration**  
   - ✅ GridView: Converted 15+ logging calls from Serilog to UnifiedLogger
   - ✅ ActorView: Converted 30+ logging calls from Serilog to UnifiedLogger
   - ✅ Build success with zero errors
   - ✅ Template formatting implemented (no more `[FORMAT ERROR]`)

### 🔍 **ISSUE DISCOVERED: Architectural Inconsistency**

**Problem**: Mixed logging output styles reveal 3 parallel systems still running:
- `[09:23:27] [INF] [System]` ← DebugSystem using old CategoryFilteredLogger from DI
- `Information [System]` ← GameManager using Serilog directly  
- `Information [System] Creating {Width}x{Height}` ← Views using UnifiedLogger

**Root Cause**: UnifiedLogger in Godot layer can't be registered in Core's GameStrapper due to Clean Architecture constraints (Core can't reference Godot).

## 🎯 **TECH LEAD DECISION (2025-09-12):**

### **Architecture Approved: ADR-007 Unified Logger Architecture**

**Decision**: Implement **Composite Pattern with ILogOutput abstraction** as documented in ADR-007
- ✅ Preserves rich Godot console output via GodotConsoleOutput
- ✅ Maintains Clean Architecture (abstractions in Core, implementations in appropriate layers)
- ✅ Supports multiple simultaneous destinations (console + file)
- ✅ Enables category-based filtering via DebugConfig
- ✅ Separates debug logging from future game analytics needs

**Key Components**:
1. `ILogOutput` interface in Core.Infrastructure
2. `CompositeLogOutput` for multiple destinations
3. `UnifiedLogger` implementing ICategoryLogger
4. `GodotConsoleOutput` for rich console in Godot layer
5. `FileLogOutput` for session-based log files
6. `TestConsoleOutput` for unit tests

**Next Steps for Dev Engineer**:
1. Implement components as specified in ADR-007
2. Migrate existing loggers following the 5-phase plan
3. Verify Debug Window integration works correctly
4. Ensure all tests pass with new logger

**Original Implementation Plan** (3/5 phases completed):

**Phase 0: Pre-Flight Check (0.5h)**
- Audit all logger usages across codebase (~30 files)
- Create migration checklist by logger type
- Document special cases and patterns
- Set up test environment

**Phase 1: Build UnifiedLogger First (1.5h)**
- Create UnifiedLogger.cs with core functionality
- Add context detection (Godot vs Tests)
- Implement rich formatting patterns
- Create comprehensive test suite
- **CHECKPOINT**: New logger works in isolation

**Phase 2: Integration Layer (0.75h)**
- Create temporary LoggerCompatibilityBridge for parallel running
- Update DI to register both old and new loggers
- Test that existing code still works
- **CHECKPOINT**: Both systems coexist safely

**Phase 3: Systematic Migration (1.5h)**
- **Infrastructure Layer** (30min): 8 files using ILogger<T>
- **Domain Layer** (15min): 1 file (DeterministicRandom.cs)
- **Godot Layer** (30min): 7 files using GD.Print
- **Test Updates** (15min): Update all test mocks
- **CHECKPOINT**: All code using new logger

**Phase 4: Cleanup (0.5h)**
- Remove compatibility bridge
- DELETE old implementations (CategoryFilteredLogger, GodotCategoryLogger, etc.)
- Clean up unnecessary using statements
- **CHECKPOINT**: Only UnifiedLogger remains

**Phase 5: Verification (0.5h)**
- Test F12 debug window controls
- Verify rich console formatting
- Check log file output
- Run performance benchmarks
- Validate in both Editor and Tests

**Key Design Principles**:
- ONE logger instance for entire application
- Lazy configuration loading (no timing issues)
- Context-aware output (rich in Godot, simple in tests)
- Zero proxies or indirection layers
- Direct integration with DebugConfig.tres

**Done When**:
- ✅ Single UnifiedLogger handles ALL logging in the application
- ✅ Debug window toggles immediately affect all log output  
- ✅ Rich formatting appears in Godot console (colors, highlights)
- ✅ Unit tests run without Godot dependencies (no GD errors)
- ✅ Zero duplicate logger implementations remain (4→1)
- ✅ All 30+ files migrated to use ICategoryLogger
- ✅ Performance: <10μs per log call (benchmark 10,000 calls)
- ✅ Memory: No leaks after 5 minutes of logging
- ✅ Log file: Captures 100% of messages (not <10% like before)

**Migration Patterns** (for Dev Engineer reference):
```csharp
// Pattern 1: ILogger → ICategoryLogger
OLD: private readonly ILogger _logger;
NEW: private readonly ICategoryLogger _logger;

// Pattern 2: ILogger<T> → ICategoryLogger  
OLD: ILogger<MyService> _logger
NEW: ICategoryLogger _logger

// Pattern 3: Logging calls
OLD: _logger?.Debug("Message {Value}", value);
NEW: _logger.Log(LogLevel.Debug, LogCategory.Combat, "Message {0}", value);

// Pattern 4: GD.Print → ICategoryLogger
OLD: GD.Print($"Debug: {message}");
NEW: _logger.Log(LogLevel.Debug, LogCategory.System, message);
```

**Risk Mitigation**:
- **Compatibility Bridge**: Allows both loggers during migration
- **Layer-by-Layer**: Test after each layer migration
- **Checkpoints**: Clear commit points for rollback if needed
- **Parallel Running**: Old system remains functional until Phase 4
- **Test Coverage**: Each phase has verification steps

**Tech Lead Notes**:
- Refined plan maintains working system throughout migration
- Checkpoints allow safe rollback at any stage
- Build-first approach validates solution before touching existing code
- Total time increased to 5h for safety, but much lower risk

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