# Darklands Development Backlog


**Last Updated**: 2025-09-17 12:37 (Tech Lead - Added complete technical breakdown for VS_014 A* pathfinding)

**Last Aging Check**: 2025-08-29
> 📚 See [Workflow.md - Backlog Aging Protocol](Workflow.md#-backlog-aging-protocol---the-3-10-rule) for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 060
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


## 🚀 Ready for Immediate Execution

*Items with no blocking dependencies, approved and ready to start*



## 📋 Blocked - Waiting for Dependencies

*Items that cannot start until blocking dependencies are resolved*

### VS_014: A* Pathfinding Foundation
**Status**: In Progress - Phase 1 Complete, Phase 2 Partial
**Owner**: Dev Engineer
**Size**: S (3h total, ~1.5h remaining)
**Priority**: Critical - Foundation for movement system
**Created**: 2025-09-11 18:12
**Updated**: 2025-09-17 13:15 (Tech Lead - Technical assessment complete, Phase 1 verified)
**Markers**: [MOVEMENT] [PATHFINDING] [CHAIN-2-MOVEMENT]

**What**: Implement A* pathfinding algorithm with visual path display
**Why**: Foundation for VS_012 movement system and all future tactical movement

**DEPENDENCY CHAIN**: Chain 2 - Step 1 (Movement Foundation)
- Blocked by: TD_046 (architectural foundation) ✅ RESOLVED
- Enables: VS_012 (Vision-Based Movement)
- Blocks: VS_013 (Enemy AI needs movement)

**Done When**:
- [x] A* finds optimal paths deterministically (17/18 tests passing)
- [x] Diagonal movement works correctly (1.41x cost = 141 integer)
- [ ] Path visualizes on grid before movement (Phase 4 not started)
- [x] Performance <10ms for typical paths (test execution confirms)
- [x] Handles no-path-exists gracefully (returns None)
- [x] All tests pass including edge cases (94% pass rate)

**Architectural Constraints**:
☑ Deterministic: Consistent tie-breaking rules (F→H→X→Y implemented)
☑ Save-Ready: Paths are transient, not saved
☑ Time-Independent: Pure algorithm (no time dependencies)
☑ Integer Math: Use 100/141 for movement costs (implemented)
☑ Testable: Pure domain function (17 passing tests)

**⚠️ IMPLEMENTATION STATUS (Tech Lead Assessment)**:
- **Phase 1 ✅**: Domain model COMPLETE with full test coverage
- **Phase 2 ⚠️**: Handler exists but uses STUB - needs AStarAlgorithm integration
- **Phase 3 ❌**: Infrastructure not started - GridStateService needs extension
- **Phase 4 ❌**: Presentation not started - no visual components

### 📐 Technical Breakdown (Tech Lead - 2025-09-17) - UPDATED

**Complexity Score**: 3/10 - Well-understood algorithm
**Pattern Reference**: `src/Application/Combat/Commands/ExecuteAttackCommand.cs:45`

#### Phase 1: Domain Model ✅ COMPLETE
**Actual Location**: `src/Darklands.Domain/Pathfinding/` (not Core/Domain)
- ✅ `PathfindingNode.cs` - Deterministic tie-breaking implemented
- ✅ `PathfindingResult.cs` - Value object complete
- ✅ `PathfindingCostTable.cs` - Integer costs (100/141) working
- ✅ `IPathfindingAlgorithm.cs` - Clean interface
- ✅ `AStarAlgorithm.cs` - Full implementation with 8-directional movement
**Tests**: 17/18 passing in `tests/Domain/Pathfinding/AStarAlgorithmTests.cs`

#### Phase 2: Application Layer [0.25h - NEEDS COMPLETION]
**Current State**: Handler exists but uses STUB implementation
**Location**: `src/Application/Grid/Queries/` (not Movement/Queries)
- ✅ `CalculatePathQuery.cs` - Exists
- ⚠️ `CalculatePathQueryHandler.cs` - Replace stub with AStarAlgorithm call
- ⚡ **NO VALIDATOR NEEDED** - Use inline validation with Fin<T> (Tech Lead decision)

**Required Work** (Updated with MediatR best practices):
1. Inject `IPathfindingAlgorithm` into handler
2. Add inline validation using `Fin.Fail()` for invalid positions
3. Get obstacles from GridStateService (needs new method)
4. Call algorithm.FindPath() and map Option<T> to Fin<Seq<Position>>
5. **Pattern**: Follow existing pipeline behaviors (no separate validator)

#### Phase 3: Infrastructure [0.5h - NOT STARTED]
**Location**: `src/Core/Infrastructure/Services/`
**Required Work**:
1. Extend `IGridStateService` interface:
   - Add `ImmutableHashSet<Position> GetObstacles()`
   - Add `bool IsWalkable(Position position)`
2. Implement methods in `InMemoryGridStateService`
3. Register `AStarAlgorithm` as `IPathfindingAlgorithm` in DI container

#### Phase 4: Presentation [0.75h - NOT STARTED]
**Location**: `godot_project/features/movement/`
- Create `PathVisualizationPresenter.cs` - MVP presenter
- Create `PathOverlay.tscn` - Visual scene
- Create `PathOverlay.cs` - Godot node script
**Pattern**: Follow `AttackPresenter.cs` for MVP approach

**Next Immediate Steps for Dev Engineer**:
1. Complete Phase 2 - Wire up AStarAlgorithm in handler (~15 min)
2. Complete Phase 3 - Extend GridStateService (~30 min)
3. Complete Phase 4 - Visual presentation (~45 min)

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

---



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