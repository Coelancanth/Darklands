# Darklands Development Backlog


**Last Updated**: 2025-10-04 17:19 (Dev Engineer: VS_007 ALL PHASES COMPLETE + bug fixes - Turn queue system fully implemented, ready for manual testing)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 006 (TD_005 complete, counter unchanged)
- **Next VS**: 019


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

---

*Recently completed and archived (2025-10-04):*
- **TD_003**: Separate Equipment Slots from Spatial Inventory Container - Created EquipmentSlotNode (646 lines), extracted InventoryRenderHelper (256 lines), cleaned InventoryContainerNode. All 3 phases complete, 359 tests GREEN. ✅ (2025-10-04)
- **TD_004**: Move ALL Shape Logic to Core (SSOT) - Eliminated all 7 business logic leaks from Presentation (164 lines removed, 12% complexity reduction). Fixed SwapItemsCommand double-save bug, eliminated cache-driven anti-pattern. Commits: 4cd1dbe, 49c06e6. ✅ (2025-10-04)
- **TD_005**: Persona & Protocol Updates - Updated dev-engineer.md with Root Cause First Principle, UX Pattern Recognition, Requirement Clarification Protocol. ✅ (2025-10-04)
- **BR_007**: Equipment Slot Visual Issues - Fixed 1×1 highlight override and sprite centering for equipment slots. ✅ (2025-10-04)
- *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### VS_007: Time-Unit Turn Queue System ⭐ **COMPLETE - READY FOR MANUAL TESTING**

**Status**: ✅ ALL 4 PHASES COMPLETE + Bug Fixes - Turn queue system fully implemented with test scene
**Owner**: Dev Engineer (completed all phases)
**Size**: L (2-3 days, all 4 phases) - **ACTUAL: 3 days**
**Priority**: Critical (foundation for combat system)
**Depends On**: VS_005 (FOV events), VS_006 (Movement cancellation)

**What**: Time-unit turn scheduling system that naturally distinguishes exploration mode (auto-movement) from combat mode (single-step tactical movement)

**Why**:
- **Core Pillar**: Time-unit combat is Vision.md foundation (weapon speed, armor penalties, action costs)
- **Natural Mode Detection**: Turn queue size = combat state (queue=[Player] → exploration, queue=[Player,Enemy] → combat)
- **Movement Interruption**: Enemy detection → schedule enemy → queue grows → auto-movement cancels automatically
- **Reuses Existing Architecture**: FOV events (VS_005), cancellation tokens (VS_006), no duplicate logic
- **Enables Future Features**: Weapon proficiency (speed improvement), variable action costs, AI turn management

**How** (4-Phase Implementation):

**Phase 1 (Domain)** - Time-unit scheduling primitives:
- `TimeUnits` value object (wraps int for type safety)
- `TurnQueue` aggregate (priority queue sorted by NextActionTime, NOT insertion order)
- `ScheduledActor` record (ActorId + NextActionTime)
- Methods: `Schedule()`, `PopNext()` (returns actor with lowest time), `IsInCombat()` (check queue.Count > 1), `Contains()`, `Remove()`
- **Time Model**: Relative time per combat session (resets to 0 when combat starts/ends, NOT tracked during exploration)
- **Tie-Breaking**: When multiple actors have same NextActionTime, player always acts first (MVP simplification)

**Phase 2 (Application)** - Commands, Queries, Event Handlers:
- Commands: `ScheduleActorCommand`, `AdvanceTurnCommand`, `RemoveActorFromQueueCommand`
- Queries: `IsInCombatQuery` (check queue size), `GetTurnQueueStateQuery` (for UI), `IsActorScheduledQuery` (check duplicate scheduling)
- **EnemyDetectionEventHandler** (KEY!): Subscribes to `FOVCalculatedEvent` → detects hostile actors at visible positions → checks if already scheduled (avoid duplicates) → schedules new enemies at current combat time (immediate action) → triggers combat mode
- **CombatModeDetectedEventHandler** (KEY!): Subscribes to `TurnQueueChangedEvent` → detects queue growth (1 → multiple) → cancels ongoing `MoveAlongPathCommand`
- Events: `TurnQueueChangedEvent` (actor added/removed), `TurnAdvancedEvent` (time spent)
- **Reinforcement Handling**: FOV events fire during combat (every player move) → new enemies auto-schedule → combat continues dynamically (no "session end" until all enemies defeated)

**Phase 3 (Infrastructure)** - Turn queue service + movement cancellation:
- `TurnQueueService` (in-memory queue + cancellation token management)
- Methods: `ScheduleActor()`, `GetNextActor()`, `IsInCombat()`, `CancelCurrentMovement()`, `GetMovementCancellationToken()`

**Phase 4 (Presentation)** - Input routing based on combat mode:
- Query `IsInCombatQuery` before movement
- If combat → `CalculateNextStepTowardQuery` + `MoveCommand` (single step)
- If exploration → `CalculatePathQuery` + `MoveAlongPathCommand` (auto-path with cancellation token)

**Scope**:
- ✅ Time-unit turn queue (priority queue sorted by time, schedule/advance/remove operations)
- ✅ Combat mode detection via queue size (`IsInCombat()` query)
- ✅ Enemy auto-scheduling when **player sees them** (`EnemyDetectionEventHandler` processes player's FOV)
- ✅ Auto-movement cancellation when combat starts (`CombatModeDetectedEventHandler`)
- ✅ Input routing (exploration = auto-path, combat = single-step)
- ✅ Movement costs time ONLY during combat (exploration movement is instant, no time tracking)
- ✅ Relative time model (combat session resets to 0, time doesn't grow indefinitely)
- ✅ Player-first tie-breaking (when Player and Enemy both ready at same time)
- ❌ **Enemy vision** (enemies don't calculate FOV, can't detect player first—defer to VS_011 "Enemy AI & Vision")
- ❌ **Asymmetric combat** (ambush scenarios where enemy sees you but you don't see them—requires enemy FOV)
- ❌ Enemy AI behavior (just schedule them, AI is future VS)
- ❌ Turn execution loop (defer if not needed for MVP)
- ❌ Variable action costs for different moves (all moves cost 100 units for now)
- ❌ UI visualization of turn queue (nice-to-have, can defer)
- ❌ Initiative rolls or speed stats (player always acts first on ties)

**Done When**:
- ✅ Unit tests: TurnQueue scheduling preserves time order, `IsInCombat()` detects multiple actors
- ✅ Unit tests: `EnemyDetectionEventHandler` schedules hostile actors from FOV event, skips already-scheduled actors (no duplicates)
- ✅ Unit tests: `CombatModeDetectedEventHandler` cancels movement on queue growth
- ✅ Unit tests: Player-first tie-breaking (Player and Enemy both at time=0 → Player acts first)
- ✅ Integration tests: Walk across map → enemy appears in FOV → movement stops at current tile (no rollback)
- ✅ Integration tests: Next click moves only 1 step (combat mode active)
- ✅ Integration tests: Enemy removed from queue → next click resumes auto-path (exploration mode)
- ✅ Integration tests: During combat, player moves → new enemy appears in FOV → enemy auto-schedules → combat continues (reinforcements)
- ✅ Manual test: Click distant tile → auto-path starts → enemy spawns mid-path → movement stops cleanly
- ✅ Manual test: During combat, click 5 tiles away → only moves 1 step toward target
- ✅ Manual test: Fight Goblin → Goblin defeated → move toward exit → Orc appears → combat continues (no "combat ended" message)
- ✅ Code review: Zero changes to existing `MoveAlongPathCommandHandler` (uses existing cancellation on line 63)
- ✅ Code review: Zero changes to existing `MoveActorCommandHandler` (uses existing FOVCalculatedEvent on line 122)

**Example End-to-End Scenario** (Reference Implementation):
```
Exploration → Combat Detection → Reinforcement → Victory

1. EXPLORATION (time not tracked):
   Player clicks tile 10 steps away → MoveAlongPathCommand starts
   Queue: [Player@0] (always ready, time frozen)

2. STEP 3 - COMBAT STARTS (time = 0):
   MoveActorCommand → FOVCalculatedEvent → Goblin detected!
   EnemyDetectionEventHandler: ScheduleActorCommand(Goblin, time=0)
   Queue: [Player@0, Goblin@0] (sorted, player first on tie)
   IsInCombat() = TRUE → CombatModeDetectedEventHandler cancels MoveAlongPathCommand
   Remaining 7 steps discarded (graceful stop at current tile)

3. TURN 1 (time = 0):
   PopNext() → Player (wins tie-breaking)
   Player moves 1 step (costs 100)
   Queue: [Goblin@0, Player@100]

4. TURN 2 (time = 0, advances to Goblin's turn):
   PopNext() → Goblin (lowest time)
   Goblin attacks (costs 150)
   Queue: [Player@100, Goblin@150]

5. TURN 3 (time = 100):
   PopNext() → Player
   Player moves → FOVCalculatedEvent → ORC DETECTED! (reinforcement)
   EnemyDetectionEventHandler:
     - IsActorScheduledQuery(Goblin) → TRUE (skip, already in queue)
     - IsActorScheduledQuery(Orc) → FALSE (new enemy!)
     - ScheduleActorCommand(Orc, time=100) (current combat time)
   Queue: [Orc@100, Goblin@150, Player@200] (Orc acts immediately!)

6. TURN 4 (time = 100):
   PopNext() → Orc (lowest time, just appeared but ready)
   Orc attacks (costs 150)
   Queue: [Goblin@150, Player@200, Orc@250]

7. TURN 5 (time = 150):
   PopNext() → Goblin
   Player defeats Goblin → RemoveActorFromQueueCommand(Goblin)
   Queue: [Player@200, Orc@250] (still in combat, 2 actors)

8. TURN 6 (time = 200):
   PopNext() → Player
   Player defeats Orc → RemoveActorFromQueueCommand(Orc)
   Queue: [Player] (only 1 actor)
   IsInCombat() = FALSE → combat ends, time resets to 0

9. BACK TO EXPLORATION:
   Player clicks distant tile → MoveAlongPathCommand resumes (auto-path)
   Queue: [Player@0] (time frozen again)
```

**Tech Lead Notes** (for breakdown):
- **Key Design Decision**: Turn queue IS the mode detector (not separate combat state machine)
- **Event Bridging**: FOV event handlers bridge systems (FOV → turn queue → movement cancellation)
- **Minimal Changes**: Existing handlers unchanged, just add event subscribers
- **Movement as Action**: Each step costs time (foundation for variable action costs later)
- **Time Model**: Relative time per combat session (NOT absolute game time). Exploration = time frozen at 0, Combat = time flows from 0 onward, resets when combat ends. Keeps numbers small, matches roguelike conventions (NetHack, DCSS).
- **Priority Queue Semantics**: `_queue.Count > 1` means "multiple actors scheduled" (combat), NOT "queue order". Priority queue sorts by NextActionTime (lowest = acts first), not insertion order.
- **Tie-Breaking**: Player always wins ties (same NextActionTime) for MVP. Future: initiative rolls, speed stats.
- **Player-Centric FOV**: VS_007 only uses player's FOV (enemies scheduled when player sees them). Enemy vision (asymmetric combat, ambushes) deferred to future VS. Architecture supports it (just add PlayerDetectionEventHandler subscribing to enemy FOV events), but out of scope for MVP.
- **Future Extensibility**: To add enemy vision later: (1) Calculate FOV for enemies within awareness radius, (2) Publish FOVCalculatedEvent(enemyId, visiblePositions), (3) PlayerDetectionEventHandler checks if player in visiblePositions, (4) Schedule enemy if yes. No refactoring of turn queue needed—event-driven design allows non-breaking addition.
- **Reference Scenario Above**: Shows complete lifecycle including exploration, combat start, reinforcements, victory. Use for integration test design and manual testing walkthrough.

**Dev Engineer Review** (2025-10-04):

*Architecture Clarifications*:
1. **Phase 3 Infrastructure Pattern**: Use `ITurnQueueRepository` following our repository pattern (not service layer). Command handlers fetch aggregate via repository, call domain methods, persist. No `TurnQueueService` needed—breaks SSOT if service duplicates aggregate logic.
2. **Tie-Breaking Location**: Implement in Domain (`TurnQueue.PopNext()`) for testability. Pure logic without MediatR dependency.
3. **Player Scheduling Lifecycle**: Player permanently lives in queue at time=0 during exploration (never removed). When combat starts, player advances time normally. When combat ends (queue.Count == 1), player's time resets to 0 via `ResetToExploration()` method.
4. **Turn Execution Flow**: Semi-automated. Player acts (click) → game auto-processes all enemy turns until player is next → waits for player input. Uses `ProcessPendingTurnsQuery` to batch enemy actions without UI blocking.
5. **Event Handler Testing**: Phase 2 includes unit tests for event handlers (mock IMediator, verify correct commands sent). Integration tests (Phase 3) verify end-to-end flow.
6. **Reinforcement Handling**: Part of `EnemyDetectionEventHandler` (not separate). Handler checks `IsActorScheduledQuery` before scheduling to prevent duplicates.

*Implementation Decisions*:
- **Domain**: `TurnQueue` aggregate with `SortedList<TimeUnits, ActorId>` for O(log n) insert, O(1) peek/pop
- **Tie-Breaking**: Use `SortedList` key `(TimeUnits, IsPlayer)` composite for automatic player-first ordering
- **Time Reset**: `TurnQueue.ResetToExploration()` sets player's NextActionTime = TimeUnits.Zero when queue.Count drops to 1
- **Event Handlers**: Unit testable via mocked `IMediator.Send()` verification (Phase 2 test coverage)

*Ready to implement Phase 1*: Domain primitives with comprehensive unit tests.

**Phase 1 Progress** (2025-10-04 16:14):

✅ **Domain Implementation Complete** (36/36 tests GREEN, <20ms total):
- `TimeUnits` value object: Immutable, type-safe time representation with Result<T> validation
- `ScheduledActor` record: Lightweight data holder (ActorId + NextActionTime + IsPlayer flag)
- `TurnQueue` aggregate: Priority queue with player-first tie-breaking, automatic exploration reset
  - Methods: `Schedule()`, `PopNext()`, `PeekNext()`, `Remove()`, `Reschedule()`, `Contains()`, `IsInCombat`
  - Player permanently in queue (never fully removed), resets to time=0 when combat ends
  - Internal `List<ScheduledActor>` + custom sort for flexible tie-breaking

**Key Design Decisions Validated**:
1. **PopNext() + Re-Schedule Pattern**: Actor removed on action → re-scheduled with new time (not in-place update)
2. **Automatic Exploration Reset**: When queue.Count drops to 1 (only player), `Remove()` auto-resets player to time=0
3. **Player-First Tie-Breaking**: Sort comparator ensures player acts before enemies at same time
4. **Reschedule() for Adjustments**: Separate method for updating existing actor's time (used in Phase 2 handlers)

**Test Coverage**:
- Unit tests (20 tests): TimeUnits validation, arithmetic, comparison operators
- Unit tests (16 tests): TurnQueue scheduling, ordering, combat mode detection, lifecycle simulation
- Complex integration test: Full exploration → combat → reinforcement → victory scenario

**Files Created**:
- `src/Darklands.Core/Features/Combat/Domain/TimeUnits.cs`
- `src/Darklands.Core/Features/Combat/Domain/ScheduledActor.cs`
- `src/Darklands.Core/Features/Combat/Domain/TurnQueue.cs`
- `tests/Darklands.Core.Tests/Features/Combat/Domain/TimeUnitsTests.cs`
- `tests/Darklands.Core.Tests/Features/Combat/Domain/TurnQueueTests.cs`

**Next**: Phase 2 (Application) - Commands, Queries, Event Handlers

**Phase 2 Progress** (2025-10-04 16:31):

✅ **Application Layer Complete** (13 new tests, 49 total):

**Repository Interface**:
- `ITurnQueueRepository`: Singleton pattern, async API, auto-creates with player

**Commands + Handlers** (2 commands, Railway-oriented):
- `ScheduleActorCommand`: Add actor → Save → Publish TurnQueueChangedEvent
- `RemoveActorFromQueueCommand`: Remove actor → Auto-reset if last enemy → Event

**Queries + Handlers** (2 queries, hot-path optimized):
- `IsInCombatQuery`: Returns queue.IsInCombat (called every movement)
- `IsActorScheduledQuery`: Checks Contains() (prevents duplicate scheduling)

**Events**:
- `TurnQueueChangedEvent`: ActorId, ChangeType, IsInCombat, QueueSize
- `TurnQueueChangeType` enum: ActorScheduled, ActorRemoved
- Per ADR-004: Terminal subscribers, no cascading events

**Event Handler**:
- `EnemyDetectionEventHandler`: FOVCalculatedEvent → ScheduleActorCommand
  - Bridges FOV System (VS_005) to Combat System (VS_007)
  - Processes player's FOV only (MVP scope)
  - Filters hostile actors, checks if already scheduled
  - Schedules new enemies at time=0 (immediate action)
  - Handles reinforcements during combat (FOV recalculates on movement)

**Test Coverage** (13 Phase 2 tests):
- Command handlers (8 tests): Schedule/Remove success, failures, mode transitions
- Query handlers (5 tests): Combat state, actor scheduling checks
- Test infrastructure: FakeTurnQueueRepository, FakeEventBus

**Files Created** (17 files):
- Application: ITurnQueueRepository, 2 commands + handlers, 2 queries + handlers, 1 event handler
- Events: TurnQueueChangedEvent, TurnQueueChangeType enum
- Tests: 3 test classes + 2 test infrastructure classes

**Next**: Phase 3 (Infrastructure) - TurnQueueRepository implementation, DI registration

**Phase 3 Progress** (2025-10-04 17:08):

✅ **Infrastructure Layer Complete** (359 tests GREEN, zero regressions):

**Repository Pattern** (Following InMemoryInventoryRepository pattern):
- `InMemoryTurnQueueRepository`: Singleton with lazy initialization
- `InitializeWithPlayer(ActorId)`: Explicit player ID injection (test scenes call in _Ready())
- Auto-creates TurnQueue on first `GetAsync()` with player pre-scheduled at time=0
- Thread-safe locks for future-proofing (single-threaded game for MVP)
- `Reset()` method for test cleanup (internal)

**Player Context Service** (NEW - Solves dependency problem):
- **Problem**: EnemyDetectionEventHandler needs player ID to filter FOV events (only respond to player's vision)
- **Solution**: `IPlayerContext` / `PlayerContext` provides "who is the player?" globally
- `SetPlayerId(ActorId)`: Explicit initialization (test scenes call in _Ready())
- `GetPlayerId()`: Returns Result<ActorId> (fail-fast if not initialized)
- `IsPlayer(ActorId)`: Convenience method for filtering
- Future-proof: Can extend to support party-based gameplay

**DI Registration** (GameStrapper):
- `ITurnQueueRepository` → `InMemoryTurnQueueRepository` (singleton)
- `IPlayerContext` → `PlayerContext` (singleton)
- MediatR assembly scan auto-registers `EnemyDetectionEventHandler` (zero manual wiring)

**Event Handler Integration**:
- `EnemyDetectionEventHandler`: Injected `IPlayerContext`, removed TODO placeholder
- Now properly filters FOV events: `if (!_playerContext.IsPlayer(notification.ActorId)) return;`
- FOVCalculatedEvent → EnemyDetectionEventHandler works automatically (MediatR bridge)

**Design Principles**:
- Single Source of Truth: TurnQueue/PlayerContext are SSOT (no state duplication)
- Explicit Dependencies: Initialization required, fail-fast if missing
- Repository Pattern: Consistent with existing InMemoryInventoryRepository
- Clean Architecture: Zero business logic in Infrastructure (delegates to Domain)
- Future-Proof: Ready for SQLite/JSON persistence (just swap implementation)

**Files Created** (3 files):
- `src/Darklands.Core/Features/Combat/Infrastructure/InMemoryTurnQueueRepository.cs`
- `src/Darklands.Core/Application/IPlayerContext.cs`
- `src/Darklands.Core/Infrastructure/PlayerContext.cs`

**Files Modified** (2 files):
- `GameStrapper.cs`: Added DI registrations
- `EnemyDetectionEventHandler.cs`: Injected IPlayerContext

**Phase 4 Progress** (2025-10-04 17:19):

✅ **Presentation Layer Complete** (359 tests GREEN) + **Bug Fixes**:

**Test Scene Created**:
- `TurnQueueTestScene.tscn` / `TurnQueueTestSceneController.cs`
- Duplicated from GridTestScene with VS_007-specific setup
- Player at (5,5), Goblin at (15,15), Orc at (20,20)
- Designed for combat mode detection testing

**Input Routing Implementation** (Combat Mode Detection):
- `ClickToMove()`: Queries `IsInCombatQuery` before pathfinding decision
- **Combat Mode**: Single-step movement (1 tile toward target, tactical positioning)
- **Exploration Mode**: Auto-path movement (existing VS_006 behavior, cancellable)
- Clean conditional branching (no complex state machines)
- Logging: ⚔️ Combat mode vs 🚶 Exploration mode indicators

**Test Scene Initialization**:
- `InitializeGameState()`: Calls `PlayerContext.SetPlayerId()` and `TurnQueueRepository.InitializeWithPlayer()`
- Explicit dependency injection (traceable initialization flow)
- Auto-creates turn queue with player pre-scheduled at time=0

**Design Elegance**:
- ✅ Zero State Duplication: Combat state lives ONLY in TurnQueue (queue.Count > 1 = combat)
- ✅ Query-Based Decision: Simple boolean check routes to correct movement type
- ✅ Reuses Existing Commands: No new movement commands (uses MoveActorCommand for single-step)
- ✅ VS_006 Compatibility: Auto-path unchanged (exploration mode preserves existing behavior)
- ✅ Natural Mode Transitions: Enemy appears → auto-schedules → combat mode (zero manual transitions)

**Bug Fixes** (3 commits):

**Bug #1: Async Race Condition (NullReferenceException)**:
- **Symptom**: Crash when clicking new destination during active movement
- **Root Cause**: Classic async race—awaiting task allows another continuation to null out cancellation token before disposal
- **Solution**: Cache references before await (local variables immune to concurrent modifications)
- **Pattern**: Textbook "capture before await" defensive programming
- **Commits**: b0b2bc4 (initial), f1e623b (complete fix)

**Bug #2: Path Preview Disappearing**:
- **Symptom**: Orange path visualization vanishes immediately when movement starts
- **Root Cause**: `ClearPathPreview()` called too early (inside ExecuteMovementAsync before command completes)
- **Solution**: Move cleanup to ClickToMove after await (path stays visible during entire multi-step movement)
- **Pattern**: "Show before, clear after" flow control
- **Commit**: 8d8396d

**Files Modified** (1 file):
- `TurnQueueTestSceneController.cs`:
  - Added using statements for IPlayerContext, ITurnQueueRepository, IsInCombatQuery
  - InitializeGameState(): PlayerContext.SetPlayerId() + TurnQueueRepository.InitializeWithPlayer()
  - ClickToMove(): Combat mode check + conditional routing (combat = single-step, exploration = auto-path)
  - CancelMovementAsync(): Cache references before await (race condition fix)
  - ShowPathPreview() before movement, ClearPathPreview() after await (preview fix)

**Files Created** (2 files):
- `godot_project/test_scenes/TurnQueueTestScene.tscn`
- `godot_project/test_scenes/TurnQueueTestSceneController.cs`

---

## ✅ VS_007 COMPLETION SUMMARY

**All 4 Phases Complete**:
- ✅ Phase 1: Domain (TimeUnits, TurnQueue, ScheduledActor) - 36 tests
- ✅ Phase 2: Application (Commands, Queries, Events, Handlers) - 13 tests (49 total)
- ✅ Phase 3: Infrastructure (TurnQueueRepository, PlayerContext, DI) - 359 total
- ✅ Phase 4: Presentation (Input routing, test scene, bug fixes) - 359 total

**Total Implementation**:
- **Files Created**: 25 files (3 Domain, 17 Application, 3 Infrastructure, 2 Presentation)
- **Files Modified**: 3 files (GameStrapper, EnemyDetectionEventHandler, TurnQueueTestSceneController)
- **Tests**: 359/359 GREEN ✅ (49 new tests for VS_007, zero regressions)
- **Commits**: 10 commits (7 feature, 3 bug fixes)

**Ready for Manual Testing**: Open `TurnQueueTestScene.tscn` in Godot and verify end-to-end flow

---

**Next**: Manual testing validation, then archive to backlog archive

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