# Darklands Development Backlog


**Last Updated**: 2025-09-18 20:52 (Tech Lead - Critical architectural alignment: ADR-022/023 and TD_061/063 synchronized)

**Last Aging Check**: 2025-08-29
> 📚 See [Workflow.md - Backlog Aging Protocol](Workflow.md#-backlog-aging-protocol---the-3-10-rule) for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 065
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


### TD_063: Layered Game State Management
**Status**: ✅ APPROVED (See ADR-023)
**Owner**: Dev Engineer (implement per ADR-023)
**Size**: M (6h for complete implementation)
**Priority**: High - Foundational system needed before AI turns
**Created**: 2025-09-17 21:55 (Dev Engineer - initial proposal)
**Updated**: 2025-09-17 22:30 (Tech Lead - ADR-023 created with layered architecture)
**Markers**: [STATE-MACHINE] [FOUNDATION] [ARCHITECTURE] [ADR-023]

**What**: Implement input state management to lock user interactions during ongoing work operations
**Why**: Users can currently click/interact while animations, commands, or other work is in progress, causing conflicts and inconsistent state

**Problem Statement**:
- User can click tiles while actor is moving, causing command queuing issues
- Input events can interrupt ongoing operations (attacks, movement, etc.)
- No visual feedback when system is "busy" vs ready for input
- Race conditions between user input and system state changes

**Technical Approach** (State Machine Pattern):
- Create `IInputStateManager` service in Application layer
- State machine with states: `Ready`, `Processing`, `Animating`, `Disabled`
- Each state defines what input events are allowed/blocked
- Presenter layer queries state before processing user input
- Visual feedback shows when input is locked (cursor changes, UI graying, etc.)

**Architectural Constraints**:
□ Deterministic: State changes based on clear triggers, not timing
□ Save-Ready: State can be serialized if needed for save games
□ Time-Independent: Uses game events not wall-clock time
□ Integer Math: N/A for this feature
□ Testable: State machine logic testable without Godot runtime

**State Transition Examples** (Per ADR-023 Layered System):
```
Combat Layer:
PlayerTurn → ExecutingAction: Movement command starts (per ADR-022)
ExecutingAction → PlayerTurn: Movement progression completes
PlayerTurn → TurnTransition: End turn clicked
TurnTransition → AIThinking: AI turn begins

UI Layer (stackable):
Push TargetingMode: Ability clicked requiring target
Pop TargetingMode: Target selected or ESC pressed
Push ModalDialog: Important dialog appears
Pop ModalDialog: Dialog dismissed
```

**TECH LEAD FINAL DESIGN** (2025-09-17 22:30):
✅ **APPROVED - See ADR-023 for Complete Architecture**

**Why Layered State System**:
- **Layer 1**: Game flow (MainMenu, InGame, Victory)
- **Layer 2**: Combat states (PlayerTurn, AITurn, Executing)
- **Layer 3**: UI overlays (Dialog, Inventory, Targeting)
- Handles concurrent states elegantly
- Foundation for entire game's state management

**Implementation per ADR-023** (Layered State System):
```csharp
// Layer 1: Core game flow (mutually exclusive)
public enum GameFlowState { MainMenu, Loading, InGame, Victory, Defeat }

// Layer 2: Combat substates (only valid when InGame)
public enum CombatState
{
    PlayerTurn,         // Can accept input
    ExecutingAction,    // Movement/animation playing (per ADR-022)
    AIThinking,         // AI calculating
    TurnTransition      // Between turns
}

// Layer 3: UI overlays (can stack)
public enum UIOverlay { None, ModalDialog, Inventory, TargetingMode }

public interface IGameStateManager
{
    GameFlowState CurrentFlowState { get; }
    CombatState? CurrentCombatState { get; }
    bool CanProcessPlayerInput();
    bool CanExecuteCombatAction();
}

// Usage in Presenter
if (!_stateManager.CanProcessPlayerInput()) return;
```

**Phased Implementation**:
1. **Phase 1** (2h): Core state manager with basic states
2. **Phase 2** (1h): Integration with command handlers
3. **Phase 3** (1h): UI feedback (cursor changes)
4. **Phase 4** (2h): Testing and state validation

**Complexity Score**: 4/10 (balanced approach)
**Time Estimate**: 4-6h (foundational system)
**Pattern Match**: Standard FSM pattern, used in all tactical games

**Done When**:
- [ ] IGameStateManager interface and implementation
- [ ] Basic states defined (PlayerTurn, AnimatingAction, AITurn minimum)
- [ ] State transitions validated (can't go from AITurn to TargetingMode)
- [ ] Integration with command handlers
- [ ] Presenters check CanProcessInput before accepting input
- [ ] Cursor changes based on state
- [ ] GameStateChangedEvent published on transitions
- [ ] Unit tests for valid/invalid transitions

**Integration with Movement (TD_061/ADR-022)**:
- When movement starts: Transition to `ExecutingAction` state
- During movement: Input blocked except ESC (cancel) or new destination
- Movement progresses cell-by-cell per ADR-022 (logical position advances)
- When movement completes/cancels: Transition back to `PlayerTurn`
- Visual teleports to match logical position (no smooth animation)

**Dependencies**: Works with TD_061 (movement progression triggers state changes)
**Enables**: TD_064 (provides state context for movement redirection)

---

### TD_061: Progressive FOV Updates During Movement
**Status**: ✅ APPROVED - Phase 1/4 Complete
**Owner**: Dev Engineer (implementing Phase 2)
**Size**: M (4-6h total, ~3h remaining)
**Priority**: Critical - Game mechanic bug
**Created**: 2025-09-17 20:35 (Dev Engineer - initial proposal)
**Updated**: 2025-09-18 21:30 (Tech Lead - Aligned with ADR-022 clarifications)
**Markers**: [FOV] [VISION] [MOVEMENT] [GAME-LOGIC] [ARCHITECTURE]

**What**: Update Field of View progressively as actor moves cell-by-cell
**Why**: Currently FOV updates instantly to destination, revealing areas before actor arrives

**PROBLEM IDENTIFIED** (Dev Engineer Ultra-Analysis):
**Root Cause**: `GridPresenter.cs:236` - FOV updates after entire move completes
```csharp
// CURRENT BROKEN FLOW:
var result = await _mediator.Send(moveCommand);  // Move completes instantly
result.Match(
    Succ: async _ => {
        await UpdatePlayerVisionAsync(_currentTurn);  // FOV reveals destination immediately!
    }
);
```

**Visual Example**:
```
Current (WRONG):              Expected (CORRECT):
Turn 1: Click destination     Turn 1: Click destination
  ####?                         ####?
  #@..?  <- FOV shows           #@..?  <- FOV at start
  #...?     destination         #...?     position only
  ????      immediately         ????

Turn 2: Actor animating       Turn 2: Actor at cell 1
  ####.                         ####?
  #...@  <- Actor still         #.@.?  <- FOV updates
  #....     moving but          #...?     per cell
  ....      FOV already         ???       progressively
            revealed all
```

**CRITICAL ARCHITECTURAL ISSUE WITH TECH LEAD'S OPTION A**:

❌ **Couples Game Logic to Animation Timing**
- FOV updates driven by animation callbacks
- Game state becomes dependent on visual timing
- Violates Clean Architecture separation
- Creates save/load complexity (animation state in saves?)
- Makes testing require animation system

**DEV ENGINEER COUNTER-PROPOSAL: Option D - Logical Movement Progression** ⭐

**Core Principle**: **Logical position IS the authoritative position - it progresses cell-by-cell**

```csharp
// 1. Domain Layer - Movement progression (per ADR-022)
public class MovementProgression
{
    public Position CurrentPosition { get; private set; }  // THE authoritative position
    public IReadOnlyList<Position> RemainingPath { get; private set; }
    // Advances on timer, returns new position when cell reached
}

// 2. Application Layer - Service manages progressions
public interface IMovementProgressionService
{
    void StartMovement(ActorId actor, IReadOnlyList<Position> path);
    void CancelMovement(ActorId actor);  // For ESC or redirect
    void AdvanceGameTime(int milliseconds);
    Position GetCurrentPosition(ActorId actor);  // Returns THE position
}

// 3. Presentation Layer - Visual teleports to logical position
public class ActorView : Node2D
{
    public void OnPositionChanged(ActorPositionChangedEvent evt)
    {
        // Teleport instantly to new position (no interpolation)
        Position = GridToWorld(evt.NewPosition);
        // Brief visual feedback for movement
        PlayMovementFlash();
    }
}
```

**Enhanced Flow** (Per ADR-022):
1. **User clicks** → `MoveActorCommand` calculates path
2. **Movement starts** → Actor stays at current position, progression begins
3. **Timer advances** → Every 200ms, logical position moves to next cell
4. **FOV updates** → Calculated from new logical position (THE authoritative position)
5. **Visual teleports** → Sprite jumps to match logical position
6. **User sees** → Cell-by-cell movement with progressive FOV revelation

**Architectural Advantages** (Two-Position Model):

✅ **Clean Separation**: Logical position owns ALL game logic, visual is pure feedback
✅ **Fully Deterministic**: Fixed 200ms timer progression, no animation dependencies
✅ **Save-Simple**: Current position + optional movement state = complete save
✅ **Testable**: All FOV/combat logic uses logical position, no visual dependencies
✅ **No Clipping**: Teleport movement prevents diagonal sprite clipping (TD_062)
✅ **ESC-Friendly**: Cancel movement = stay at current position, trivial to implement

**Architectural Constraints**:
□ Deterministic: Fixed 200ms timer timing, rule-based progression ✅
□ Save-Ready: Logical position + timer = serializable game state ✅
□ Time-Independent: Uses fixed intervals, not wall-clock time ✅
□ Integer Math: 200ms intervals, deterministic timing ✅
□ Testable: Complete FOV logic testable without Godot runtime ✅

**Implementation Plan** (4-6h estimate, aligned with ADR-022):

**Phase 1: Domain Logic** (1.5h)
- Create `MovementProgression` class with timer-based advancement
- Add `CurrentPosition` and `RemainingPath` properties
- Unit tests for position progression logic

**Phase 2: Application Service** (2h)
- Implement `IMovementProgressionService` with Start/Cancel/Advance methods
- Wire up FOV updates when logical position changes
- Integration tests for movement + FOV coordination

**Phase 3: Presentation Layer** (1.5h)
- Update `ActorView` to teleport on position changes
- Add visual feedback (flash effect) for movement
- Manual testing for cell-by-cell visual updates

**Phase 4: Edge Cases** (1h)
- ESC cancellation (actor stays at current position)
- Click redirect (cancel + new path from current)
- Save/load during movement (position + remaining path)

**Dev Engineer Assessment**:
- **Complexity Score**: 6/10 - Cross-layer but architecturally pure
- **Maintainability**: Excellent - clear separation of concerns
- **Testability**: Outstanding - game logic completely unit testable
- **Robustness**: Superior - animation issues cannot break game state

**TECH LEAD APPROVAL** (2025-09-17 22:09, Updated 2025-09-18 21:30):
✅ **APPROVED - Two-Position Model** (See **ADR-022: Logical-Visual Position Separation**)

**Technical Clarification** (After ADR-022 revision):
- **Two Positions Only**: Logical (authoritative, progresses on timer) + Visual (teleports to match)
- **NO Third Position**: Actor is WHERE THEY ARE, not at destination
- **ESC Cancellation**: Trivial - actor stays at current logical position
- **Save/Load**: Simple - current position + optional movement state
- **Pattern**: Board game model - piece position determines everything

**Required Refinements** (Per ADR-022):
1. **Service Naming**: `IMovementProgressionService` (manages cell-by-cell progression)
2. **Two-Position Model**: Logical Position (authoritative, progresses on timer) + Visual Position (teleports to match)
3. **Game-Time Based**: Not wall-clock, for pause/save support
4. **Configurable Timing**: `MillisecondsPerCell` property (default 200ms)
5. **Pattern Documentation**: Standard for ALL timed progressions

**Why Not Simpler?**
Considered waypoint events in command handler - rejected because:
- Blocks handler during movement
- Can't handle interruptions cleanly
- Mixes timing into business logic
- Makes testing require async delays

**Implementation Approach** (Per ADR-022):
```csharp
// Core service interface (Application layer)
// Implements ADR-022: Logical-Visual Position Separation
public interface IMovementProgressionService
{
    void StartMovement(ActorId actor, IReadOnlyList<Position> path);
    void CancelMovement(ActorId actor);  // ESC or redirect
    void AdvanceGameTime(int milliseconds);
    Position GetCurrentPosition(ActorId actor);  // THE authoritative position
}
```

**Key Design Points**:
- Logical position IS the authoritative position (no third position)
- Visual position teleports to match (no interpolation)
- ESC cancellation is trivial (stay at current position)
- Save/load only needs current position + optional path
- FOV updates from logical position only

**Implementation Note**: Start with Phase 1 (Domain) immediately - no blockers

**Dependencies**: None (can be implemented independently)
**Enables**: TD_064 (Interruptible Movement - extends with cancellation support)

**Recommendation**: Implement Option D for superior architecture and maintainability

**Phase 1 Complete** (2025-09-18 17:24):
✅ Tests: 13/13 passing (19ms execution)

**What I Actually Did**:
- Implemented pure domain layer with zero external dependencies
- Created FogOfWar namespace (renamed from Movement to avoid conflicts)
- Built timer-based RevealProgression value object with immutable advancement logic
- Added comprehensive domain events: RevealPositionAdvanced, RevealProgressionStarted/Completed
- Converted tests from NUnit to Xunit/FluentAssertions to match existing patterns

**Problems Encountered**:
- Namespace collision: "Movement" namespace conflicted with existing Domain.Grid.Movement class
  → Solution: Renamed to FogOfWar namespace, more descriptive and avoids collision
- Test framework mismatch: Initially used NUnit but existing tests use Xunit
  → Solution: Converted to Xunit with FluentAssertions for consistency
- Domain purity violation: Initially tried to use MediatR in domain events
  → Solution: Made domain events pure data structures, MediatR integration in Application layer

**Technical Debt Created**:
- None - Pure domain implementation with excellent test coverage

**Lessons for Phase 2**:
- Application layer will need MediatR notification wrappers for domain events
- Service interface should be named IMovementProgressionService (per ADR-022)
- Timer advancement will need game-time integration, not wall-clock time
- Must implement CancelMovement for ESC/redirect support

**Phase 2 Complete** (2025-09-18 21:18):
✅ Tests: 13/13 passing (332ms execution) - Application layer integration tests
✅ Files Created:
  - `src/Application/FogOfWar/Services/IMovementProgressionService.cs`
  - `src/Application/FogOfWar/Services/MovementProgressionService.cs`
  - `src/Application/FogOfWar/Events/RevealProgressionStartedNotification.cs`
  - `src/Application/FogOfWar/Events/RevealPositionAdvancedNotification.cs`
  - `src/Application/FogOfWar/Events/RevealProgressionCompletedNotification.cs`
  - `src/Application/FogOfWar/Handlers/RevealProgressionStartedHandler.cs`
  - `src/Application/FogOfWar/Handlers/RevealPositionAdvancedHandler.cs`
  - `src/Application/FogOfWar/Handlers/RevealProgressionCompletedHandler.cs`
  - `tests/Application/FogOfWar/Services/MovementProgressionServiceIntegrationTests.cs`
  - `tests/Application/FogOfWar/Handlers/ProgressiveFOVCoordinationTests.cs`
✅ DI Registration: Added IMovementProgressionService to GameStrapper

**What I Actually Did**:
- Implemented MovementProgressionService with thread-safe ConcurrentDictionary state management
- Created comprehensive MediatR notification wrappers for all domain events
- Built RevealPositionAdvancedHandler that triggers FOV recalculation via CalculateFOVQuery
- Used fire-and-forget Task.Run patterns to avoid blocking synchronous methods with async notifications
- Followed established patterns from ExecuteAttackCommandHandler for error handling
- Used LogCategory.Gameplay for movement-related logging (no Movement category exists)

**Problems Encountered**:
- Unit type ambiguity: LanguageExt.Unit vs MediatR.Unit naming conflicts
  → Solution: Added `using Unit = LanguageExt.Unit;` alias in all files
- Service not registered: Integration tests failed with "No service registered"
  → Solution: Added service registration to GameStrapper.ConfigureApplicationServices()
- Async/await mismatch: Tests tried to await Fin<T> synchronous types
  → Solution: Removed async keywords and await calls on Fin<T> operations

**Technical Debt Created**:
- None - Clean Application layer following established service patterns

**Lessons for Phase 3**:
- Infrastructure layer needs timer coordination for AdvanceGameTime calls
- May need game-time service integration instead of manual timer advancement
- Presentation layer will need to subscribe to position advancement notifications
- Visual teleport implementation will need actor view coordination

**Phase 3 Complete** (2025-09-18 21:51):
✅ Tests: 428/428 passing (837ms execution) - All existing tests still pass, no regression
✅ Build: Clean compilation with zero warnings

**What I Actually Did**:
- Implemented IGameTimeService and GameTimeService with deterministic timing and pause/resume support
- Created IMovementTimer and MovementTimer for coordinating timer events with MovementProgressionService
- Built thread-safe event subscription model: GameTimeService.TimeAdvanced → MovementTimer → MovementProgressionService.AdvanceGameTime
- Registered both services as singletons in GameStrapper.ConfigureApplicationServices()
- Used proper LanguageExt v5 patterns: FinFail<T>, FinSucc, Error.New() throughout
- Added comprehensive logging with AppLogLevel aliases to avoid namespace conflicts

**Files Created**:
- `src/Infrastructure/Services/IGameTimeService.cs` - Core game time management interface
- `src/Infrastructure/Services/GameTimeService.cs` - Production implementation with System.Threading.Timer
- `src/Infrastructure/Services/IMovementTimer.cs` - Movement timer coordination interface
- `src/Infrastructure/Services/MovementTimer.cs` - Bridges GameTimeService → MovementProgressionService
- Updated `src/Infrastructure/DependencyInjection/GameStrapper.cs` - Added service registrations

**Problems Encountered**:
- LanguageExt namespace conflicts: Microsoft.Extensions.Logging.LogLevel vs Darklands.Application.Common.LogLevel
  → Solution: Used `using AppLogLevel = Darklands.Application.Common.LogLevel;` alias pattern
- Missing Prelude imports for FinFail/FinSucc: Infrastructure services needed `using static LanguageExt.Prelude;`
  → Solution: Added proper imports and followed existing MovementProgressionService patterns
- GameStrapper namespace mismatch: Used `Darklands.Infrastructure.Services` instead of full namespace
  → Solution: Updated to `Darklands.Infrastructure.Services.IGameTimeService` registration
- LogLevel.Trace doesn't exist: Application.Common.LogLevel only has Debug/Information/Warning/Error
  → Solution: Changed Trace usages to Debug level for detailed diagnostic logging

**Technical Architecture Achieved**:
- **Two-service coordination**: GameTimeService (timing) + MovementTimer (coordination)
- **Clean event model**: Timer events flow through proper abstraction layers
- **Deterministic timing**: 200ms default configurable per ADR-022, pausable for save/load
- **Thread-safe implementation**: All services use proper locking and disposal patterns
- **Performance monitoring**: MovementTimer tracks advancement count, timing, and error statistics

**Lessons for Phase 4**:
- Presentation layer needs to subscribe to position advancement events for visual updates
- Actor visual teleporting will need coordination with logical position changes
- GameTimeService should be started/stopped based on game state (playing vs paused)
- MovementTimer.CurrentTurn property needs integration with turn management system

---



## 📋 Blocked - Waiting for Dependencies

*Items that cannot start until blocking dependencies are resolved*

### TD_064: Interruptible Movement System
**Status**: Proposed - BLOCKED by TD_061 (Phase 2-4) and TD_063
**Owner**: Tech Lead (review) → Dev Engineer (implement)
**Size**: S (2-3h for implementation)
**Priority**: Important - Enhanced player experience
**Created**: 2025-09-18 20:15 (Tech Lead - architectural analysis complete)
**Markers**: [MOVEMENT] [INPUT] [STATE-COORDINATION] [BLOCKED]

**What**: Enable cancellation and redirection of movement while in progress
**Why**: Players expect to change destination mid-movement for responsive controls

**Problem Statement**:
- Currently no way to cancel movement once started
- Players must wait for movement to complete before issuing new command
- Feels unresponsive compared to modern tactical games
- Path recalculation from partial position unclear

**Technical Approach** (Hard Cancel Pattern):
```csharp
public class MovementRedirectHandler
{
    public Fin<Unit> HandleMovementRedirect(ActorId actorId, Position newDest)
    {
        // 1. Check input state allows redirect (TD_063)
        if (!_inputStateManager.AllowsMovementRedirect())
            return Fail("Input locked");

        // 2. Check game state allows redirect (ADR-023)
        if (!_gameStateManager.CanExecuteMovementCommand())
            return Fail("Invalid state");

        // 3. Cancel at current logical position
        var currentPos = _movementService.CancelMovement(actorId);

        // 4. Recalculate and start new path
        var newPath = _pathfinding.CalculatePath(currentPos, newDest);
        return _movementService.StartMovement(actorId, newPath);
    }
}
```

**System Coordination Required**:
- **TD_061**: Provides movement progression mechanics (HOW to redirect)
- **TD_063**: Controls input acceptance (IF redirect allowed)
- **ADR-023**: Validates game state (WHEN redirect valid)
- **VS_014**: A* pathfinding from discrete logical position

**Why "Hard Cancel" Approach**:
- **Simplest**: Always at discrete cell (logical position)
- **Deterministic**: No sub-cell interpolation math
- **Testable**: Clear state at all times
- **Save-friendly**: Position always well-defined

**Rejected Alternatives**:
- **Smooth Redirect**: Complex state tracking for marginal UX gain
- **Sub-cell Interpolation**: A* requires discrete cells, not fractional positions
- **Command Queuing**: Feels laggy for movement (OK for abilities)

**Architectural Constraints**:
☑ Deterministic: Cancel at discrete logical position
☑ Save-Ready: Clear position state at all times
☑ Time-Independent: Based on logical position not animation
☑ Integer Math: Grid positions only
☑ Testable: State transitions without Godot

**Implementation Steps**:
1. **MovementProgressionService**: Add `CancelMovement()` method
2. **InputStateManager**: Configure `ProcessingMove` to allow redirects
3. **GameStateManager**: Validate redirect during `PlayerTurn`
4. **GridPresenter**: Handle new click during movement
5. **Tests**: Cancellation, state validation, edge cases

**Complexity Score**: 3/10 (straightforward with clear patterns)
**Pattern Match**: Common in tactical games (XCOM, Divinity, BG3)

**Done When**:
- [ ] Movement can be cancelled mid-path
- [ ] New destination triggers path recalculation
- [ ] State validation prevents invalid redirects
- [ ] Visual feedback shows redirect occurred
- [ ] Edge cases handled (same cell, no path, etc.)
- [ ] Unit tests for state coordination

**Dependencies**: TD_061 (movement progression), TD_063 (input states), ADR-023 (game states)

### VS_012: Vision-Based Movement System
**Status**: Approved - BLOCKED by TD_060
**Owner**: Tech Lead → Dev Engineer
**Size**: S (2h)
**Priority**: Critical
**Created**: 2025-09-11 00:10
**Updated**: 2025-09-17 18:30 (Tech Lead - Added technical breakdown, blocked by TD_060)
**Markers**: [MOVEMENT] [VISION] [CHAIN-2-MOVEMENT]

**What**: Movement system where scheduler activates based on vision connections
**Why**: Creates natural tactical combat without explicit modes

**DEPENDENCY CHAIN**: Chain 2 - Step 2 (Vision-Based Movement)
- Dependencies: VS_014 ✅ COMPLETE, TD_060 (Animation Foundation) ⏳
- Enables: VS_013 (Enemy AI needs movement system)

**Architectural Constraints**:
☑ Deterministic: Fixed TU costs (integer-based)
☑ Save-Ready: Position state only, no animation state
☑ Time-Independent: Turn-based execution
☑ Integer Math: All movement costs in TU (Time Units)
☑ Testable: Clear state transitions without UI

### 📐 Technical Breakdown (Tech Lead - 2025-09-17 18:30)

**Complexity Score**: 4/10 - Straightforward with clear patterns
**Pattern Reference**: `src/Application/Combat/Commands/ExecuteAttackCommand.cs`

#### Phase 1: Domain Model [0.5h]
**Location**: `src/Darklands.Domain/Movement/`
- Create `MovementCost.cs` - Value object (TU costs per tile)
- Create `MovementPath.cs` - Validated path with total cost
- Create `IMovementValidator.cs` - Interface for validation rules
- Create `MovementValidator.cs` - Vision & TU validation logic
**Pattern**: Follow AttackDamage/AttackResult pattern

#### Phase 2: Application Layer [0.5h]
**Location**: `src/Application/Movement/Commands/`
- Create `MoveActorCommand.cs` - Command with actor ID and target
- Create `MoveActorCommandHandler.cs` - Orchestrate movement
- Integration: Call CalculatePathQuery for pathfinding
- Integration: Update GridStateService with new position
- Integration: Publish ActorMovedNotification
**Pattern**: Copy ExecuteAttackCommandHandler structure

#### Phase 3: Infrastructure [0.5h]
**Location**: `src/Core/Infrastructure/Services/`
- Extend `ISchedulerService` with vision-based activation
- Add `CheckVisionTrigger(Position from, Position to)` method
- Implement enemy activation when player enters vision
**Key Logic**: If enemy sees movement → activate enemy turn

#### Phase 4: Presentation [0.5h]
**Location**: `godot_project/features/movement/`
- Create `MovementPresenter.cs` - MVP presenter
- Integration: Use ActorAnimator from TD_060
- Handle: Path preview → click → animation → completion
- Update: PathOverlay to trigger actual movement
**Critical**: Animation completes BEFORE next turn starts



## 📋 Blocked - Waiting for Dependencies

*Items that cannot start until blocking dependencies are resolved*

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