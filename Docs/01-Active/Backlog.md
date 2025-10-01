# Darklands Development Backlog


**Last Updated**: 2025-10-01 16:17 (Tech Lead: VS_006 updated with 8-directional + right-click cancellation)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 004
- **Next TD**: 002
- **Next VS**: 007


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

## 🔥 Critical (Do First)
*Blockers preventing other work, production bugs, dependencies for other features*

### VS_006: Interactive Movement System
**Status**: Approved
**Owner**: Tech Lead → Dev Engineer (for implementation)
**Size**: L (2-3 days)
**Priority**: Critical (core gameplay mechanic)
**Markers**: [GAMEPLAY] [USER-EXPERIENCE]

**What**: Point-and-click movement with A* pathfinding, visual path preview, and smooth tile-to-tile animation

**Why**:
- Natural interaction model (click where you want to go vs. mashing arrow keys)
- Tactical clarity (see path before committing → enables strategic planning)
- Game feel (animation eliminates prototype jank)
- Foundation for all future targeting/interaction features

**How** (4-Phase Implementation):
- **Phase 1 (Domain)**: Minimal (existing Position/GridMap sufficient, ~1h)
- **Phase 2 (Application)**: `FindPathQuery` + `MoveAlongPathCommand` with waypoint list (~3h)
  - **CRITICAL**: Design `IPathfindingService.FindPath()` to accept `Func<Position, int> getMovementCost` parameter NOW (prevents refactoring later when action costs added)
- **Phase 3 (Infrastructure)**: `AStarPathfindingService` with Chebyshev distance heuristic (~4h)
  - A* implementation uses `getMovementCost(neighbor)` instead of hardcoded `1`
  - Performance target: <50ms for longest path on 30x30 grid
  - Tests: No path exists, start=end, complex obstacle navigation, varied costs (floor=1, smoke=2)
- **Phase 4 (Presentation)**: Mouse input + path visualization + Godot Tween animation (~4h)
  - Click → show path overlay → confirm → animate each step
  - Initial usage passes `pos => 1` lambda (uniform cost for now)
  - FOV updates during movement (leverage existing event system)

**Scope:**
- ✅ A* pathfinding around obstacles (8-directional movement per roguelike standard)
- ✅ Mouse click to select destination
- ✅ Visual path preview (highlight tiles)
- ✅ Discrete tile-to-tile "jump" animation (0.1-0.2s per tile)
- ✅ Path validation (clicking impassable tiles gives feedback)
- ✅ **Design for action costs** (interface accepts cost function, implementation uses it, gameplay passes uniform cost=1)
- ✅ Diagonal movement (8 directions: N/S/E/W + NE/NW/SE/SW, diagonal cost=1.0 per Caves of Qud)
- ✅ **Manual path cancellation** via right-click (CancellationToken pattern, stops movement immediately)
- ❌ **Using** variable action costs in gameplay (Phase 4 passes `pos => 1`, but infrastructure ready for future VS)
- ❌ **Auto-interruption** (enemy spotted, loot discovered) - deferred to VS_007

**Done When**:
- ✅ Click passable tile → path visualized → confirm → smooth animated movement
- ✅ Animation feels "jumpy" (discrete tiles, not smooth slide)
- ✅ FOV overlay updates properly during movement
- ✅ Clicking walls/impassable shows clear error feedback
- ✅ All 189 existing tests still pass
- ✅ Phase 3 performance test: Pathfinding completes in <50ms
- ✅ Phase 3 cost variation test: A* finds optimal path when floor=1, smoke=2 (validates cost system works)
- ✅ Manual test: Click across map → actor navigates around walls → FOV follows
- ✅ Manual test: Right-click during movement → movement stops immediately at current tile
- ✅ Code review confirms:
  - Event-driven architecture (no polling in Presentation)
  - `IPathfindingService.FindPath()` accepts cost function (future-proof interface)
  - A* implementation uses provided cost function (not hardcoded)

**Depends On**: VS_005 (Grid + FOV) - ✅ Complete

**Product Owner Decision** (2025-10-01 15:40, updated 15:50):
- **Scope Challenge Resolved**: Keep as ONE comprehensive VS (not split into pathfinding + animation)
- **Rationale**: Path visualization, animation, and pathfinding deliver incomplete value when separated. Shipping "click to move but it teleports" would feel broken.
- **Movement Direction Revised**: 8-directional (changed from initial 4-directional to match roguelike genre standard per Tech Lead research)
- **Action Cost Decision** (credit: user feedback): Design infrastructure to accept variable costs NOW, use uniform cost=1 in VS_006 gameplay
  - **Why**: Adding `Func<Position, int> getCost` parameter costs ~10 minutes but prevents major refactoring when action costs added later (VS_008+)
  - **Benefit**: Phase 3 can test cost variations (floor=1, smoke=2) even though gameplay doesn't use them yet → validates algorithm correctness
  - **Risk Mitigation**: Zero cost to gameplay in VS_006, zero breaking changes when costs become real in future VS
- **Risk Assessment**: Medium (mostly animation complexity with Godot Tween async)
- **Next Step**: Tech Lead breaks down implementation and approves architecture

**Tech Lead Decision** (2025-10-01 16:02, updated 16:17 for 8-directional + right-click cancel):
- **Architecture Approved**: NEW FEATURE `Features/Movement/` (separate from Grid per ADR-004)
  - **Rationale**: Grid handles terrain/positions/FOV, Movement handles pathfinding/navigation
  - Clear separation prevents Grid from becoming god-feature
- **Phase 1 (~1h)**: Minimal - folder structure only, no new domain models (reuse Position)
- **Phase 2 (~3h)**: `FindPathQuery` + `MoveAlongPathCommand`
  - `IPathfindingService.FindPath(start, goal, isPassable, getCost)` - **REFINED**: Separate `Func<Position,bool> isPassable` and `Func<Position,int> getCost` for clearer semantics
  - `MoveAlongPathCommand` delegates to existing `MoveActorCommand` per step (reuses validation, FOV, events)
  - **Key Decision**: Emit `ActorMovedEvent` PER STEP (enables discrete tile animation + FOV updates)
  - **Cancellation**: Handler respects `CancellationToken` - checks `ct.ThrowIfCancellationRequested()` before each step
- **Phase 3 (~4h)**: `AStarPathfindingService` with Chebyshev heuristic
  - 8-directional (N/S/E/W + NE/NW/SE/SW), diagonal cost=1.0, PriorityQueue for open set
  - Performance target: <50ms (enforced by test)
  - Cost variation test validates algorithm correctness (floor=1, smoke=2 scenario)
- **Phase 4 (~4.5h)**: `MovementInputController` + `PathVisualizationNode` (Godot)
  - Left-click → FindPathQuery → Preview → Left-click again → MoveAlongPathCommand → Animate
  - Right-click → Cancel active movement (via `CancellationTokenSource.Cancel()`)
  - **Animation**: Godot Tween for 0.1-0.2s tile-to-tile "jump" (current handler does instant teleport - needs update)
- **Event Topology**: Reuse `ActorMovedEvent` (terminal subscriber, depth=1, ADR-004 compliant)
- **ADR Compliance**: ✅ All 4 ADRs validated (Clean Architecture, Godot boundary, Result<T>, Event Rules)
- **Total Estimate**: 12.5h (1.5-2 days) - includes manual cancellation via right-click
- **Risks**: A* performance (mitigated by algorithm choice + benchmark test), Godot Tween async (mitigated by existing pattern)
- **Next Step**: Hand off to Dev Engineer for Phase 1-4 implementation

**Dev Engineer Progress** (2025-10-01 16:32, Phase 1 complete 16:33, Phase 2 complete 16:40, Phase 3 complete 16:59):
- **Architecture Review Complete**: ADR alignment validated, existing Grid/FOV patterns studied
- **Interface Refinement**: Separated passability check from cost calculation for clearer A* semantics
  - **Rationale**: Boolean passability (can/cannot) vs quantitative cost (how expensive) are distinct concerns
  - **Benefit**: A* checks passability first (more efficient), clearer contract for callers
- **✅ Phase 1 Complete** (~30 min actual, 1h estimated)
  - Created `Features/Movement/` folder structure (Domain/Application/Infrastructure layers)
  - Defined `IPathfindingService` interface with `isPassable` + `getCost` parameters
  - **Tests**: All 189 existing tests pass ✅
  - **Key Insight**: No new domain models needed - Position from Domain/Common sufficient (validates ADR-004 shared primitive strategy)
- **✅ Phase 2 Complete** (~1h actual, 3h estimated - ahead of schedule!)
  - **Query Layer**: `FindPathQuery` + handler (thin wrapper, delegates to service)
  - **Command Layer**: `MoveAlongPathCommand` + handler with cancellation support
  - **Key Design**: Command composition - delegates to existing `MoveActorCommand` per step (reuses validation, FOV, events)
  - **Cancellation Strategy**: Checks `IsCancellationRequested` before each step (graceful stop, no rollback)
  - **Tests**: 14 new tests (7 query, 7 command) covering valid paths, failures, cancellation, architecture validation
  - **Total Tests**: 203 pass (189 existing + 14 new Phase 2) ✅
  - **Key Insight**: Graceful cancellation returns `Result.Success()` with partial completion (not exception) - actor stays at current tile
- **✅ Phase 3 Complete** (~2h actual, 4h estimated - ahead of schedule!)
  - **A* Implementation**: `AStarPathfindingService` with 8-directional movement + Chebyshev heuristic
  - **Algorithm Details**: PriorityQueue for open set, HashSet for closed set, Chebyshev distance `max(|dx|, |dy|)` (optimal for diagonal movement)
  - **Bug Fixed**: Unbounded exploration crash - tests needed bounds in `isPassable` functions to prevent infinite space exploration
  - **Tests**: 12 new tests covering paths, obstacles, cost variation, edge cases, 8-directions, performance (<50ms), maze solving
  - **Total Tests**: 215 pass (189 existing + 14 Phase 2 + 12 Phase 3) ✅
  - **Performance**: <50ms for longest path on 30x30 grid (meets VS_006 requirement)
  - **Key Insight**: Separate `openSetHash` for O(1) membership checks prevents O(n) lookups with PriorityQueue.UnorderedItems.Any()
- **✅ Phase 4 Complete** (~1.5h actual, 4.5h estimated - ahead of schedule!)
  - **DI Registration**: `AStarPathfindingService` registered in `GameStrapper.RegisterCoreServices()`
  - **Mouse Input**: Left-click executes movement, right-click cancels (ColorRect.MouseFilter.Stop enables input capture)
  - **Hover-Based Path Preview**: Orange overlay updates dynamically as mouse moves over tiles (standard roguelike UX)
  - **Movement Animation**: 100ms delay per tile for visible step-by-step movement (10 tiles/second)
  - **Graceful Cancellation**: `TaskCanceledException` handling during delays, path preview clears on cancel
  - **Debug Output**: Console prints full path coordinates for pathfinding verification
  - **Total Tests**: 215 pass (all existing tests + Core changes) ✅
  - **Key Insight**: Hover preview uses `InputEventMouseMotion` to recalculate paths on-the-fly - no click needed to see where you'll go
  - **Known Issue**: A* pathfinding may not produce straight lines for unobstructed paths (needs investigation)
  - **Next**: Bug fix for pathfinding optimality

---

*Recently completed and archived (2025-10-01):*
- **VS_005**: Grid, FOV & Terrain System - Custom shadowcasting, 189 tests, event-driven integration ✅ (2025-10-01 15:19)
- **VS_001**: Health System Walking Skeleton - Architectural foundation validated ✅
- **BR_001**: Race Condition - Fixed with WithComponentLock pattern ✅
- **BR_002**: Fire-and-Forget Events - Fixed with async/await ✅
- **BR_003**: Heal Button CQRS Bypass - Removed per YAGNI ✅
- **TD_001**: Architecture Enforcement Tests - 10 tests enforcing all 4 ADRs ✅
- *See: [Completed_Backlog_2025-10.md](../07-Archive/Completed_Backlog_2025-10.md)*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

**No important items!** ✅ (Clear until VS_006 progresses to next owner)

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*

**No items in Ideas section!** ✅

*Future work is tracked in [Roadmap.md](../02-Design/Game/Roadmap.md) with dependency chains and sequencing.*

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



---



---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*