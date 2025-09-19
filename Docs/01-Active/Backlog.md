# Darklands Development Backlog


**Last Updated**: 2025-09-19 03:33 (Tech Lead - TD_061 replaced with TD_065, added prevention TDs 066-069, created post-mortem)

**Last Aging Check**: 2025-08-29
> 📚 See [Workflow.md - Backlog Aging Protocol](Workflow.md#-backlog-aging-protocol---the-3-10-rule) for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 071
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

## 🚨 Critical Lessons Learned from TD_061 Incident

**Post-Mortem**: [`/Docs/06-PostMortems/Inbox/2025-09-19-td061-presenter-handler-violation.md`](../06-PostMortems/Inbox/2025-09-19-td061-presenter-handler-violation.md)

### Key Violations to Avoid:
1. ❌ **NEVER** make Presenters implement `INotificationHandler<T>`
2. ❌ **NEVER** let domain models lie about state (instant teleport vs step-by-step)
3. ❌ **NEVER** create timer services for simple progressions
4. ❌ **NEVER** register MediatR before custom handler registrations

### Correct Patterns:
1. ✅ Handlers in Application layer, Presenters use UIEventBus
2. ✅ Domain models truth (actors move step-by-step)
3. ✅ Events drive reactions (not complex orchestration)
4. ✅ MediatR registered LAST in DI configuration

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

**State Transition Examples**:
```
Ready → Processing: User clicks move command
Processing → Animating: Command validated, animation starts
Animating → Ready: Animation complete event received
Ready → Disabled: Dialog/menu opens
Disabled → Ready: Dialog/menu closes
```

**TECH LEAD FINAL DESIGN** (2025-09-17 22:30):
✅ **APPROVED - See ADR-023 for Complete Architecture**

**Why Layered State System**:
- **Layer 1**: Game flow (MainMenu, InGame, Victory)
- **Layer 2**: Combat states (PlayerTurn, AITurn, Executing)
- **Layer 3**: UI overlays (Dialog, Inventory, Targeting)
- Handles concurrent states elegantly
- Foundation for entire game's state management

**Implementation per ADR-023**:
```csharp
public enum GameState
{
    PlayerTurn,         // Can accept input
    AnimatingAction,    // Blocking input during animation
    AITurn,            // AI thinking
    DialogOpen,        // Modal UI active
    TargetingMode      // Selecting target
}

public interface IGameStateManager
{
    GameState CurrentState { get; }
    bool CanProcessInput { get; }
    bool TransitionTo(GameState newState);
}

// Usage in Presenter
if (!_stateManager.CanProcessInput) return;
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

**Dependencies**: Complements TD_065 perfectly (state changes when movement starts/ends)

---

### TD_065: Domain-Driven Step-by-Step Movement
**Status**: ✅ APPROVED - Replaces TD_061
**Owner**: Dev Engineer
**Size**: S (4h) - Reduced from 5h due to single-actor simplification
**Priority**: Critical - Fixes TD_061 architectural issues
**Created**: 2025-09-19 03:33 (Tech Lead - from architectural review)
**Updated**: 2025-09-19 17:45 (Tech Lead - clarified single-actor movement and FOV distinction)
**Markers**: [ARCHITECTURE] [DOMAIN] [MOVEMENT] [FOV] [SCHEDULER-BASED]

#### 🎯 Core Insight
**The domain was lying.** When an actor moves from (5,5) to (8,8), they don't teleport - they physically move through (6,6), (7,7), etc. TD_061 failed because it tried to fake this truth with complex timer infrastructure instead of modeling reality in the domain.

#### 🎮 Critical Design Clarifications
**Single-Actor Movement**: This is a scheduler-based game (like Battle Brothers). Only ONE actor moves at a time - the scheduler's current actor. No concurrent movement handling needed.

**FOV Logic vs Visualization**:
- Calculate FOV for ALL actors (AI needs vision data)
- Update fog display ONLY for player (no spoilers!)
- Enemy vision is tracked but never shown

#### 📐 Architectural Alignment
- **ADR-022**: Domain models step-by-step truth, events flow naturally
- **ADR-010**: Domain events → Application handlers → UIEventBus → Presenters
- **ADR-006**: No Godot types in Domain/Application, direct usage in Views

#### 🔄 Control Flow
```
Scheduler.CurrentActor → User Click → GridPresenter → MoveActorCommand → Domain.StartMovement()
                                                                                ↓
Game Loop (200ms) → CurrentActor.AdvanceMovement() → Position changes → ActorMovedEvent
                            ↓ (only one actor)                                 ↓
                    If movement complete → Scheduler.NextActor      ActorMovedHandler
                                                                           ↓
                                                            Calculate FOV (ALL actors)
                                                                           ↓
                                                            If Player: Update fog display
                                                            If Enemy: Store vision only
```

#### ✅ Current View Infrastructure (Ready!)
- **GridView.UpdateFogOfWarAsync()** - FOV visualization works perfectly
- **ActorView.AnimatePathProgression()** - Discrete tile animation with flash
- **ActorView.OnTileArrival()** - Visual feedback per step
- **Call Method Tracks** - Can trigger at precise animation moments

**Problem Statement**:
- Domain instantly teleports actors (lies about reality)
- Complex timer services fake intermediate positions
- Presenters handle domain notifications (violates Clean Architecture)
- 12+ hours spent on 4-hour problem due to wrong architecture

**Current Animation Analysis** (2025-09-19):
✅ **Animation Already Works**: The discrete tile-by-tile animation with flash feedback is perfect
- GridPresenter orchestrates: Calculates A* path → Sends MoveCommand → Calls ActorPresenter
- ActorView animates correctly: Discrete jumps with OnTileArrival() flash effect (from TD_062)
- MovementPresenter is dead code: Subscribes to ActorMovedEvent that's never published
⚠️ **Problem**: Domain lies (instant teleport) while visual shows step-by-step truth

---

## 📋 Refined Implementation Plan

### Phase 1: Domain Truth (2h)
**Goal**: Actor owns position and movement state (THE fundamental fix)

```csharp
// src/Darklands.Domain/Actor/Actor.cs
public sealed record Actor
{
    public Position Position { get; private set; }     // THE truth
    public Path? ActivePath { get; private set; }      // Current movement
    public bool HasActivePath => ActivePath != null && !ActivePath.IsComplete;
    private readonly List<IDomainEvent> _domainEvents = new();

    public void StartMovement(Path path)
    {
        ActivePath = path;
        _domainEvents.Add(new MovementStartedEvent(Id, Position, path));
    }

    public void AdvanceMovement()
    {
        if (ActivePath == null || ActivePath.IsComplete) return;

        var previousPosition = Position;
        Position = ActivePath.GetNextStep();
        _domainEvents.Add(new ActorMovedEvent(Id, previousPosition, Position));

        if (ActivePath.IsComplete)
        {
            ActivePath = null;
            _domainEvents.Add(new MovementCompletedEvent(Id, Position));
        }
    }

    public void InterruptMovement(string reason)
    {
        if (ActivePath != null)
        {
            var remainingPath = ActivePath.GetRemainingSteps();
            ActivePath = null;
            _domainEvents.Add(new MovementInterruptedEvent(Id, Position, remainingPath, reason));
        }
    }

    public void CancelMovement()
    {
        if (ActivePath != null)
        {
            ActivePath = null;
            _domainEvents.Add(new MovementCancelledEvent(Id, Position));
        }
    }
}

// src/Darklands.Domain/Movement/Path.cs
public sealed record Path
{
    private readonly ImmutableList<Position> _steps;
    private int _currentIndex = 0;

    public Path(IEnumerable<Position> steps)
    {
        _steps = steps.ToImmutableList();
    }

    public Position GetNextStep()
    {
        if (IsComplete) throw new InvalidOperationException("Path is complete");
        return _steps[++_currentIndex];
    }

    public ImmutableList<Position> GetRemainingSteps()
    {
        return _steps.Skip(_currentIndex + 1).ToImmutableList();
    }

    public bool IsComplete => _currentIndex >= _steps.Count - 1;
    public Position CurrentPosition => _steps[_currentIndex];
    public Position FinalDestination => _steps.Last();
}
```

**Files to Create/Modify**:
- `src/Darklands.Domain/Actor/Actor.cs` - Add Position, ActivePath, movement methods
- `src/Darklands.Domain/Movement/Path.cs` - Path value object
- `src/Darklands.Domain/Events/MovementStartedEvent.cs`
- `src/Darklands.Domain/Events/ActorMovedEvent.cs`
- `src/Darklands.Domain/Events/MovementCompletedEvent.cs`
- `src/Darklands.Domain/Events/MovementInterruptedEvent.cs` - NEW for interruptions
- `src/Darklands.Domain/Events/MovementCancelledEvent.cs`

---

### Phase 2: Application Orchestration (1h - reduced from 1.5h)
**Goal**: Handle domain events, update services, calculate FOV per step

**🎯 CRITICAL FOV DISTINCTION**:
- Calculate FOV for ALL actors (AI needs to know what they see)
- Update visual fog ONLY for player-controlled actors
- Enemy FOV is stored but never displayed (no spoilers!)

```csharp
// src/Application/Grid/Handlers/ActorMovedHandler.cs
public class ActorMovedHandler : INotificationHandler<ActorMovedEvent>
{
    private readonly IGridStateService _gridState;
    private readonly IGameStateService _gameState;
    private readonly IFOVCalculator _fovCalculator;
    private readonly IVisionService _visionService;
    private readonly IUIEventBus _uiEventBus;
    private readonly ICategoryLogger _logger;

    public async Task Handle(ActorMovedEvent evt, CancellationToken ct)
    {
        // Update grid state (single source of truth for queries)
        _gridState.UpdateActorPosition(evt.ActorId, evt.NewPosition);

        // Calculate FOV for new position (ALL actors need this for AI!)
        var fov = _fovCalculator.Calculate(evt.NewPosition, _gridState.GetGrid());
        _visionService.UpdateVision(evt.ActorId, fov);

        // CRITICAL: Only send vision state for player-controlled actors
        if (_gameState.IsPlayerControlled(evt.ActorId))
        {
            // Player movement - include FOV for display update
            var visionState = _visionService.GetVisionState(evt.ActorId);

            await _uiEventBus.PublishAsync(new ActorPositionChangedUIEvent(
                evt.ActorId,
                evt.PreviousPosition,
                evt.NewPosition,
                visionState  // Player gets fog update
            ));

            _logger.Log(LogLevel.Debug, LogCategory.Movement,
                "Player {ActorId} moved to {To}, FOV updated visually",
                evt.ActorId, evt.NewPosition);
        }
        else
        {
            // Enemy movement - NO vision display update
            await _uiEventBus.PublishAsync(new ActorPositionChangedUIEvent(
                evt.ActorId,
                evt.PreviousPosition,
                evt.NewPosition,
                null  // Enemy vision not displayed!
            ));

            _logger.Log(LogLevel.Debug, LogCategory.Movement,
                "Enemy {ActorId} moved to {To}, FOV calculated for AI only",
                evt.ActorId, evt.NewPosition);
        }
    }
}

// src/Application/Grid/Commands/MoveActorCommandHandler.cs (Modified)
public async Task<Fin<Unit>> Handle(MoveActorCommand command, CancellationToken ct)
{
    // Get actor and validate
    var actor = await _actorRepository.GetByIdAsync(command.ActorId);
    if (actor == null) return Error.New("Actor not found");

    // Calculate path using A*
    var pathResult = await _mediator.Send(new CalculatePathQuery(
        actor.Position, command.Target));

    if (pathResult.IsFail) return pathResult.Map(_ => unit);

    // Start movement (domain will handle progression)
    var path = new Path(pathResult.IfFail(Seq<Position>.Empty));
    actor.StartMovement(path);

    // Save and dispatch events
    await _actorRepository.SaveAsync(actor);
    await _eventDispatcher.DispatchDomainEventsAsync(actor);

    return unit;
}
```

**Files to Create/Modify**:
- `src/Application/Grid/Handlers/ActorMovedHandler.cs` - NEW
- `src/Application/Grid/Handlers/MovementStartedHandler.cs` - NEW
- `src/Application/Grid/Handlers/MovementCompletedHandler.cs` - NEW
- `src/Application/Grid/Commands/MoveActorCommandHandler.cs` - Modify to start movement
- `src/Application/Grid/Services/IGridStateService.cs` - Add UpdateActorPosition

---

### Phase 3: Game Loop (0.5h - reduced from 1h)
**Goal**: Simple timer to advance movement (no complex infrastructure!)

**🎮 Single-Actor Design**: Only the scheduler's current actor can move at any time

```csharp
// src/Application/Common/GameLoop.cs
public class GameLoop : IHostedService
{
    private Timer? _timer;
    private readonly ISchedulerService _scheduler;
    private readonly IGameStateService _gameState;
    private readonly IActorRepository _actors;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly ICategoryLogger _logger;
    private const int TickIntervalMs = 200; // 5 steps per second

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information, LogCategory.System,
            "Game loop started with {Interval}ms tick", TickIntervalMs);

        _timer = new Timer(OnTick, null, 0, TickIntervalMs);
        return Task.CompletedTask;
    }

    private async void OnTick(object? state)
    {
        try
        {
            // Check if game is paused
            if (_gameState.IsPaused) return;

            // Get THE SINGLE currently active actor (scheduler-based!)
            var currentActor = await _scheduler.GetCurrentActorAsync();

            if (currentActor?.HasActivePath == true)
            {
                // Only ONE actor advances per tick
                currentActor.AdvanceMovement();

                // Save and dispatch events
                await _actors.SaveAsync(currentActor);
                await _eventDispatcher.DispatchDomainEventsAsync(currentActor);

                // If movement complete, notify scheduler
                if (!currentActor.HasActivePath)
                {
                    await _scheduler.OnActorActionCompleteAsync(currentActor.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, LogCategory.System,
                "Game loop tick error: {Error}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }
}

// Register in DI (GameStrapper.cs)
services.AddHostedService<GameLoop>();
```

---

### Phase 4: Presentation Integration (30m)
**Goal**: Clean integration with existing view infrastructure

```csharp
// ActorPresenter subscribes to UI events (already correct!)
public class ActorPresenter : EventAwarePresenter<IActorView>
{
    protected override void SubscribeToEvents()
    {
        // Subscribe to UI events via bus
        _eventBus.Subscribe<ActorPositionChangedUIEvent>(this, OnActorPositionChanged);
        _eventBus.Subscribe<VisionStateChangedUIEvent>(this, OnVisionStateChanged);
    }

    private void OnActorPositionChanged(ActorPositionChangedUIEvent evt)
    {
        // View already has the animation methods!
        _view?.AnimatePathProgression(evt.ActorId, new List<Position> { evt.NewPosition });
    }
}

// Optional: Enhanced with Call Method Tracks
public partial class ActorView : Node2D
{
    private AnimationPlayer? _stepAnimationPlayer;

    // Called by animation via call method track at frame 10
    public void OnFootPlant()
    {
        // Trigger existing arrival feedback
        OnTileArrival(_actorNodes[_currentActorId], Position);

        // Optional: Play footstep sound
        GameServices.Audio?.PlaySound(SoundId.Footstep, _currentGridPosition);
    }

    // Called by animation via call method track at frame 20
    public void OnStepComplete()
    {
        // Ready for next step
        _isAnimating = false;
    }
}
```

**Cleanup Tasks**:
- ❌ **REMOVE**: MovementPresenter (dead code)
- ❌ **REMOVE**: Any INotificationHandler in Presenters
- ✅ **KEEP**: Current animation (discrete with flash)
- ✅ **KEEP**: Current FOV visualization
- ✅ **VERIFY**: All events flow through UIEventBus

---

### 🧪 Testing Strategy

```csharp
[Test]
public void Actor_AdvanceMovement_UpdatesPosition()
{
    // Arrange
    var actor = Actor.Create(/* ... */);
    var path = new Path(new[] { new Position(0,0), new Position(1,0), new Position(2,0) });
    actor.StartMovement(path);

    // Act
    actor.AdvanceMovement();

    // Assert
    actor.Position.Should().Be(new Position(1,0));
    actor.HasActivePath.Should().BeTrue();
}

[Test]
public void Actor_InterruptMovement_StoresRemainingPath()
{
    // Arrange
    var actor = Actor.Create(position: new Position(0,0));
    var path = new Path(new[] { new Position(0,0), new Position(1,0), new Position(2,0) });
    actor.StartMovement(path);
    actor.AdvanceMovement(); // Now at (1,0)

    // Act
    actor.InterruptMovement("Hit by arrow");

    // Assert
    actor.HasActivePath.Should().BeFalse();
    var evt = actor.GetDomainEvents().OfType<MovementInterruptedEvent>().Single();
    evt.RemainingPath.Should().ContainSingle().Which.Should().Be(new Position(2,0));
}

[Test]
public void GameLoop_AdvancesOnlyCurrentActor()
{
    // Test that game loop only advances scheduler's current actor
}

[Test]
public void FOV_CalculatedForAllActors_DisplayedForPlayerOnly()
{
    // Critical test - enemy FOV calculated but not displayed
}
```

---

### ✅ Done When
- [ ] Actor owns Position and ActivePath (domain truth)
- [ ] Path value object tracks progression with GetRemainingSteps()
- [ ] Movement interruption handled (InterruptMovement method)
- [ ] Domain events fire naturally per step
- [ ] Game loop advances ONLY current actor (scheduler-based)
- [ ] Pause state checked in game loop
- [ ] FOV calculated for ALL actors (AI needs it)
- [ ] FOV display updated for PLAYER ONLY
- [ ] GridStateService updated BY events (not driving)
- [ ] Animation unchanged (discrete with flash)
- [ ] No complex timer infrastructure
- [ ] Architecture tests prevent violations
- [ ] 15+ unit tests pass

**Complexity Score**: 3/10 (reduced from 4/10 - single-actor simplification)
**Revised Time**: 4h total (Phase 1: 2h, Phase 2: 1h, Phase 3: 0.5h, Phase 4: 0.5h)

#### ✅ Phase 1 Complete (2025-09-19 19:30)
**Tests**: 256/258 passing (427ms execution time, 2 skipped FOV tests expected)

**What I Actually Did**:
- Extended Actor domain with movement state: `ActivePath`, `CurrentPathStep`, `HasActivePath`, `NextPosition`, `RemainingPath`
- Added pure movement methods: `StartMovement()`, `AdvanceMovement()`, `CancelMovement()`, `InterruptMovement()`
- Created 3 movement domain events following MediatR INotification pattern: `MovementStartedEvent`, `ActorMovedEvent`, `MovementCompletedEvent`
- Implemented 14 comprehensive unit tests covering all movement scenarios and edge cases
- Maintained Actor immutability and save-ready state (IPersistentEntity pattern)

**Problems Encountered**:
- Namespace collision: `Actor` class vs `Actor` namespace in tests
  → Solution: Used alias `using ActorEntity = Darklands.Domain.Actor.Actor;`
- LanguageExt delegate signature: `IfFail(() => ...)` incorrect syntax
  → Solution: Updated to `IfFail(error => ...)` with proper Error parameter

**Technical Decisions Made**:
- Actor tracks movement state but doesn't manage position (GridStateService remains source of truth)
- Domain events published in Application layer (not Domain) following existing patterns
- Movement state is save-ready (all properties serializable) per ADR-005
- Path representation uses `ImmutableList<Position>` from existing PathfindingResult

**Lessons for Phase 2**:
- Application handlers will need to coordinate position updates and FOV calculation
- Events follow pattern: Domain changes state → Application publishes events → Handlers react
- MoveActorCommandHandler needs modification to call `Actor.StartMovement()` instead of instant teleport

---

### TD_066: Architectural Boundary Enforcement Tests
**Status**: ✅ APPROVED
**Owner**: Dev Engineer
**Size**: S (2-3h)
**Priority**: Important - Prevents future violations
**Created**: 2025-09-19 03:33 (Tech Lead - from lessons learned)
**Markers**: [ARCHITECTURE] [TESTING] [QUALITY]

**What**: Add NetArchTest rules to enforce Clean Architecture boundaries
**Why**: TD_061's 12+ hour struggle was caused by presenters violating layer boundaries

**Technical Approach**:
```csharp
[Test]
public void Presenters_Should_Not_Be_Handlers() {
    Types.InNamespace("Presentation.Presenters")
        .Should().NotImplementInterface(typeof(INotificationHandler<>))
        .GetResult().IsSuccessful.Should().BeTrue();
}

[Test]
public void Handlers_Must_Be_In_Application_Layer() {
    Types.That().ImplementInterface(typeof(IRequestHandler<,>))
        .Should().ResideInNamespace("Application")
        .GetResult().IsSuccessful.Should().BeTrue();
}
```

**Done When**:
- [ ] Test enforcing presenters aren't handlers
- [ ] Test enforcing handlers in Application layer
- [ ] Test enforcing domain independence
- [ ] Tests run in CI pipeline

---

### TD_067: MediatR Registration Pattern Documentation
**Status**: ✅ APPROVED
**Owner**: Dev Engineer
**Size**: S (2h)
**Priority**: Important - Prevents future DI issues
**Created**: 2025-09-19 03:33 (Tech Lead - from post-mortem)
**Markers**: [DOCUMENTATION] [MEDIATR] [DI] [PATTERNS]

**What**: Document and enforce correct MediatR registration patterns
**Why**: TD_061 failed due to registration order issues and handler lifetime confusion

**Documentation to Create**:
1. **MediatR Best Practices** guide
2. **Registration order** rules
3. **Handler lifetime** guidelines
4. **Anti-patterns** to avoid

**Done When**:
- [ ] Helper method for standardized registration
- [ ] Documentation with examples
- [ ] Update existing registration code

---

### TD_068: DI Registration Order Standardization
**Status**: ✅ APPROVED
**Owner**: DevOps Engineer
**Size**: S (3h)
**Priority**: Important - Prevents registration conflicts
**Created**: 2025-09-19 03:33 (Tech Lead - from root cause analysis)
**Markers**: [DI] [INFRASTRUCTURE] [PATTERNS]

**What**: Standardize and enforce DI registration order across all projects
**Why**: Registration order conflicts caused BR_022 and wasted 12+ hours

**Enforcement**:
1. Create startup analyzer for order issues
2. Add build-time warnings for violations
3. Unit test to verify registration order

**Done When**:
- [ ] Standardized registration methods
- [ ] Order enforcement tests
- [ ] Update all ServiceConfiguration files

---

### TD_069: Persona Protocol ADR Compliance Update
**Status**: ✅ APPROVED
**Owner**: Tech Lead → All Personas
**Size**: M (4-5h)
**Priority**: Critical - Prevents future violations
**Created**: 2025-09-19 03:33 (Tech Lead - from lessons learned)
**Markers**: [DOCUMENTATION] [ARCHITECTURE] [PROCESS]

**What**: Update all persona protocols to include ADR compliance checks
**Why**: TD_061 violation could have been prevented with proper protocol checks

**Updates Required**:
- Architecture review checklists
- Smell detection guidelines
- Phase 2 review requirements
- ADR compliance verification

**Done When**:
- [ ] All persona protocols updated
- [ ] Review checklists added
- [ ] All personas acknowledge

---

### TD_070: Dynamic Movement Control (Smooth Rerouting)
**Status**: Proposed - Follow-up to TD_065
**Owner**: Tech Lead → Dev Engineer
**Size**: S (2-3h)
**Priority**: Important - Quality of life improvement
**Created**: 2025-09-19 17:49 (Tech Lead - from TD_065 review)
**Markers**: [MOVEMENT] [UX] [DOMAIN]

**What**: Add smooth destination changing without movement interruption
**Why**: Current design requires cancel-then-restart which causes movement stuttering

**Problem Statement**:
- TD_065 supports `CancelMovement()` and `InterruptMovement()`
- Changing destination requires: stop → calculate new path → start
- This creates visible "hiccup" in movement
- Players expect smooth rerouting (like Battle Brothers, XCOM)

**Technical Approach**:
```csharp
// Add to Actor class
public void ChangeDestination(Path newPath)
{
    if (ActivePath != null)
    {
        var oldDestination = ActivePath.FinalDestination;
        ActivePath = newPath;  // Seamless transition
        _domainEvents.Add(new DestinationChangedEvent(
            Id, Position, oldDestination, newPath.FinalDestination));
    }
    else
    {
        StartMovement(newPath);
    }
}
```

**Implementation Pattern**:
- Domain: Add `ChangeDestination()` method to Actor
- Application: Update `MoveActorCommandHandler` to detect rerouting
- Events: Create `DestinationChangedEvent`
- UI: No change needed (already handles path updates)

**Done When**:
- [ ] `ChangeDestination()` method added to Actor
- [ ] `DestinationChangedEvent` created and handled
- [ ] Command handler uses smart rerouting logic
- [ ] Movement transitions smoothly when destination changes
- [ ] Tests verify no position jump or reset
- [ ] Player can click new destination while moving

**Dependencies**: Requires TD_065 complete (domain movement foundation)




## 📋 Blocked - Waiting for Dependencies

*Items that cannot start until blocking dependencies are resolved*

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