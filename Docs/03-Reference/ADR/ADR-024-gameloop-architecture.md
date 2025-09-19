# ADR-024: GameLoop Architecture - TimeUnit-Based Turn Processing

**Status**: Accepted
**Date**: 2025-09-19
**Decision Makers**: Tech Lead
**Tags**: `architecture` `gameloop` `scheduler` `time-units` `determinism`

## Context

Turn-based tactical games require precise control over time and action sequencing that is fundamentally incompatible with frame-based update loops. While Godot provides `_process()` and `_physics_process()` for real-time games, tactical games need:

1. **Deterministic Timing**: Actions must occur in exact, reproducible sequences
2. **Save/Replay Support**: Game state must be perfectly reconstructible
3. **Time Independence**: Game speed adjustable without affecting logic
4. **Action Interleaving**: Complex multi-actor sequences with precise ordering

Our analysis of successful tactical games reveals a consistent pattern:

- **Battle Brothers**: Uses action points and initiative-based scheduling
- **XCOM 2**: Time units determine movement and action costs
- **Dungeon Crawl Stone Soup**: Energy system with speed modifiers
- **Tales of Maj'Eyal**: Global game turns with actor speed modifiers

All these games separate their game logic loop from the rendering loop, using abstract time units rather than real-time seconds.

### The Problem with Frame-Based Updates

Using Godot's frame-based updates for game logic creates several issues:

```csharp
// ❌ WRONG: Frame-dependent game logic
func _process(delta):
    movement_timer += delta
    if movement_timer >= 0.2:  // 200ms per step
        actor.advance_movement()
        movement_timer = 0
```

This approach:
- Breaks at different frame rates (30fps vs 144fps)
- Cannot be deterministically replayed
- Couples game speed to rendering performance
- Makes save/load inconsistent

## Decision

We will implement a **TimeUnit-based GameLoop** that is completely independent of Godot's rendering loop:

1. **TimeUnits (TU)** are the universal currency of time in the game
2. **GameLoop** advances the game by small time increments (not frames)
3. **Scheduler** manages actor turn order based on TimeUnits
4. **Complete separation** between game time and wall-clock time

### Alignment with Core ADRs

This GameLoop architecture implements key requirements from:

- **ADR-006 (Selective Abstraction)**: GameLoop implements the required `IGameClock` abstraction for deterministic time management
- **ADR-010 (UI Event Bus)**: All domain events flow through `IUIEventBus` to reach presenters
- **ADR-022 (Two-Position Model)**: Logic and visual representation have separate timing

### Architecture Components

```
┌─────────────────────────────────────────────────────────────┐
│                        Godot Engine                          │
│  _process(delta) → Rendering, Input, Audio                   │
│  Runs at variable frame rate (30-144 fps)                   │
└──────────────────────────────────────────────────────────────┘
                    ║ COMPLETELY SEPARATE ║
┌─────────────────────────────────────────────────────────────┐
│                GameLoop (Independent Timer)                  │
│  Runs independently of rendering                             │
│  Advances game time by exactly 1 TU when triggered           │
│  Trigger rate controls game speed, NOT time advancement      │
└───────────────────┬─────────────────────────────────────────┘
                    │ Each tick advances exactly 1 TU
                    ↓
┌─────────────────────────────────────────────────────────────┐
│                    Scheduler Service                         │
│  Tracks WHO should act at current TimeUnit                   │
│  Returns actors ready to act                                 │
└───────────────────┬─────────────────────────────────────────┘
                    │ Actor ready
                    ↓
┌─────────────────────────────────────────────────────────────┐
│                    Actor Processing                          │
│  1. Advance movement (if moving)                             │
│  2. Process AI decisions (if AI turn)                        │
│  3. Handle player input (if player turn)                     │
└─────────────────────────────────────────────────────────────┘
```

### Implementation Pattern

```csharp
// 1. IGameClock - Core abstraction (per ADR-006)
public interface IGameClock
{
    TimeUnit CurrentTime { get; }
    bool IsPaused { get; }
    void Pause();
    void Resume();
    void SetSpeed(GameSpeed speed);
    event Action<TimeUnit> TimeAdvanced;
}

// 2. TimeUnit - Universal time currency (already implemented)
public readonly record struct TimeUnit
{
    public int Value { get; }
    // Movement might cost 25 TU per tile
    // Attack might cost 50 TU
}

// 3. GameLoop - Implements IGameClock abstraction
public class GameLoop : IHostedService, IGameClock
{
    private Timer? _timer;
    private TimeUnit _currentGameTime = TimeUnit.Zero;
    private readonly ISchedulerService _scheduler;
    private readonly IMovementService _movement;
    private readonly IGameStateService _gameState;
    private readonly IUIEventBus _uiEventBus; // Per ADR-010

    // Game always advances by exactly 1 TU per tick
    private const int TimeUnitsPerGameTick = 1;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Implementation detail: timer triggers advancement
        // The actual interval is configurable for game speed
        _timer = new Timer(CheckForGameAdvancement, null,
            dueTime: TimeSpan.Zero,
            period: GetGameSpeedInterval());
        return Task.CompletedTask;
    }

    private async void CheckForGameAdvancement(object? state)
    {
        // Only advance if game is running (not paused, not in menu)
        if (!_gameState.ShouldAdvanceTime()) return;

        // Advance by fixed amount regardless of real time elapsed
        _currentGameTime += TimeUnit.CreateUnsafe(TimeUnitsPerGameTick);

        // Process any actors ready at this time
        while (_scheduler.HasActorReadyAt(_currentGameTime))
        {
            var actor = await _scheduler.GetNextActorAsync(_currentGameTime);
            await ProcessActor(actor);
        }

        // Process ongoing activities (movement)
        await _movement.AdvanceActiveMovements(TimeUnitsPerGameTick);
    }

    private async Task ProcessActor(Actor actor)
    {
        if (actor.IsPlayerControlled)
        {
            // Enable input, wait for player action
            await _gameState.EnablePlayerInput(actor);
        }
        else
        {
            // AI makes decision
            var action = await _ai.DecideAction(actor);
            await ExecuteAction(actor, action);
        }
    }
}

// 3. Scheduler - Manages turn order
public class CombatScheduler
{
    private SortedSet<(TimeUnit Time, ActorId Actor)> _timeline;

    public bool HasActorReadyAt(TimeUnit currentTime)
    {
        return _timeline.Any(entry => entry.Time <= currentTime);
    }

    public Actor GetNextActor(TimeUnit currentTime)
    {
        var next = _timeline.First(entry => entry.Time <= currentTime);
        _timeline.Remove(next);
        return next.Actor;
    }

    public void ScheduleActor(ActorId actor, TimeUnit nextTurn)
    {
        _timeline.Add((nextTurn, actor));
    }
}

// 4. Movement with TimeUnit costs
public class MovementService
{
    public async Task AdvanceActiveMovements(int timeUnits)
    {
        foreach (var actor in _actorsWithActivePaths)
        {
            actor.MovementProgress += timeUnits;

            // Each tile costs 25 TU (example)
            if (actor.MovementProgress >= MovementCostPerTile)
            {
                actor.AdvancePosition();
                actor.MovementProgress -= MovementCostPerTile;

                if (actor.PathComplete)
                {
                    // Schedule next action
                    _scheduler.ScheduleActor(actor, _currentTime);
                }
            }
        }
    }
}
```

### Critical Design Principles

1. **TimeUnit Granularity**: Small increments (1 TU per tick) for smooth movement
2. **Scheduler Separation**: WHO acts (Scheduler) vs WHEN to tick (GameLoop)
3. **State Machine Integration**: GameLoop respects game states (see ADR-023)
4. **Determinism First**: No floating point, no wall-clock dependencies
5. **Save-Friendly**: Current time + scheduler state = complete temporal state

### Event Flow Architecture (ADR-010 + ADR-022)

Per **ADR-010** and **ADR-022**, events flow from GameLoop through proper architectural layers:

```csharp
// 1. GameLoop triggers domain logic
private async void CheckForGameAdvancement(object? state)
{
    _currentGameTime += TimeUnit.CreateUnsafe(1);

    // Domain logic executes
    actor.AdvanceMovement();

    // Domain event raised
    await _mediator.Publish(new ActorMovedEvent(actor.Id, newPosition));
}

// 2. Application handler processes domain event
public class ActorMovedHandler : INotificationHandler<ActorMovedEvent>
{
    private readonly IUIEventBus _uiEventBus;

    public async Task Handle(ActorMovedEvent evt, CancellationToken ct)
    {
        // Update domain services
        _gridStateService.UpdatePosition(evt.ActorId, evt.NewPosition);

        // Publish to UI layer via event bus (per ADR-010)
        await _uiEventBus.PublishAsync(new ActorPositionUIEvent(
            evt.ActorId, evt.NewPosition));
    }
}

// 3. Presenter receives UI event (separate from domain)
public class ActorPresenter : EventAwarePresenter<IActorView>
{
    protected override void SubscribeToEvents()
    {
        // Subscribes via UIEventBus, not MediatR
        EventBus.Subscribe<ActorPositionUIEvent>(this, OnActorPositionChanged);
    }

    private void OnActorPositionChanged(ActorPositionUIEvent evt)
    {
        // Visual update (can animate while logic continues)
        View.AnimateToPosition(evt.NewPosition);
    }
}
```

**Critical Flow**:
1. GameLoop → Domain Logic (synchronous, deterministic)
2. Domain → Application Handler via MediatR (domain events)
3. Application → UIEventBus (UI events, per ADR-010)
4. UIEventBus → Presenter (decoupled from domain)
5. Presenter → Godot View (visual representation)

### ⚠️ CRITICAL: No Wall-Clock Time in Game Logic

The timer in GameLoop is **ONLY** a trigger mechanism, not a time source:

```csharp
// ❌ WRONG: Game time tied to real time
private async void OnTick(object? state)
{
    var elapsed = DateTime.Now - _lastTick;  // NO!
    _gameTime += elapsed.TotalSeconds * 100; // NEVER DO THIS!
}

// ✅ CORRECT: Fixed advancement per game tick
private async void CheckForGameAdvancement(object? state)
{
    if (!_gameState.ShouldAdvanceTime()) return;
    _gameTime += TimeUnit.CreateUnsafe(1); // Always advance by exactly 1 TU
}
```

**Why This Matters**:
- **Determinism**: Game advances by exactly 1 TU per tick, always
- **Replay**: Can reproduce exact sequences by tracking TU count
- **Testing**: Can advance time programmatically without waiting
- **Speed Control**: Change timer frequency, not TU advancement
- **Saves**: Game time is just an integer counter

**Game Speed Control**:
```csharp
// Speed is controlled by how often we check, not by TU advancement
public void SetGameSpeed(GameSpeed speed)
{
    // Change check frequency, not time advancement amount
    var newInterval = GetIntervalForSpeed(speed);
    _timer?.Change(TimeSpan.Zero, newInterval);

    // CRITICAL: Game ALWAYS advances by 1 TU per tick
    // Speed only affects how often ticks occur
}

private TimeSpan GetIntervalForSpeed(GameSpeed speed) => speed switch
{
    GameSpeed.Paused => Timeout.InfiniteTimeSpan,
    GameSpeed.Slow => TimeSpan.FromMilliseconds(200),   // Slower checks
    GameSpeed.Normal => TimeSpan.FromMilliseconds(50),  // Normal speed
    GameSpeed.Fast => TimeSpan.FromMilliseconds(20),    // Faster checks
    _ => TimeSpan.FromMilliseconds(50)
};
```

## Consequences

### Positive

- **Perfect Determinism**: Replay any sequence exactly
- **Save/Load Reliability**: Game time is just an integer
- **Speed Control**: Adjust game speed without affecting logic
- **Testing**: Can advance time programmatically in tests
- **Industry Standard**: Proven pattern in successful games
- **Clean Separation**: Game logic independent of rendering

### Negative

- **Initial Complexity**: More complex than frame-based updates
- **Learning Curve**: Developers need to think in TimeUnits
- **Tuning Required**: Action costs need careful balancing

### Neutral

- **Performance**: Timer-based rather than frame-based processing
- **Debugging**: Need tools to visualize timeline/scheduler state

## Alternatives Considered

### Alternative 1: Frame-Based Game Logic
Use Godot's `_process()` for all game updates.
- **Pros**: Simpler initially, built into engine
- **Cons**: Non-deterministic, frame-rate dependent, bad for saves
- **Reason not chosen**: Fundamentally incompatible with turn-based requirements

### Alternative 2: Pure Turn-Based (No Time)
Simple "your turn, my turn" without time units.
- **Pros**: Very simple, easy to understand
- **Cons**: No variable action speeds, limited tactical depth
- **Reason not chosen**: Reduces tactical options, less interesting gameplay

### Alternative 3: Energy Accumulation System
Actors accumulate energy each tick until they can act.
- **Pros**: Smooth speed variations, used by some roguelikes
- **Cons**: More complex state, harder to predict turn order
- **Reason not chosen**: TimeUnit scheduling is clearer and more predictable

## Implementation Notes

### Integration with Existing Systems

1. **Dependency Injection (ADR-006)**:
   - GameLoop registered as both `IHostedService` and `IGameClock`
   - Domain/Application layers depend on `IGameClock` abstraction
   - Infrastructure provides concrete `GameLoop` implementation
   ```csharp
   // In GameStrapper.cs
   services.AddSingleton<GameLoop>();
   services.AddSingleton<IGameClock>(provider => provider.GetRequiredService<GameLoop>());
   services.AddHostedService(provider => provider.GetRequiredService<GameLoop>());
   ```

2. **Movement System (TD_065)**:
   - Each tile movement costs TimeUnits (e.g., 25 TU)
   - Movement progresses incrementally as time advances
   - Actor scheduled for next action when movement completes

3. **State Management (ADR-023)**:
   - GameLoop only advances during appropriate states
   - Paused during menus, dialogs, etc.
   - State machine controls when time can advance

4. **Scheduler (ADR-009)**:
   - Already uses TimeUnit-based scheduling
   - GameLoop queries scheduler each tick
   - Clean separation of concerns maintained

### Configuration

```csharp
public class GameTimeConfig
{
    // Game time is measured in TimeUnits, not seconds
    public const int TimeUnitsPerGameTick = 1;  // ALWAYS advance by 1 TU

    // Action costs in TimeUnits (game balance)
    public const int MovementCostPerTile = 25;  // 25 TU to move one tile
    public const int BasicAttackCost = 50;      // 50 TU to attack
    public const int SpellCastCost = 100;       // 100 TU to cast spell

    // Speed settings (implementation detail)
    // These control check frequency, NOT game time advancement
    public static readonly Dictionary<GameSpeed, TimeSpan> SpeedIntervals = new()
    {
        [GameSpeed.Paused] = Timeout.InfiniteTimeSpan,
        [GameSpeed.Slow] = TimeSpan.FromMilliseconds(200),
        [GameSpeed.Normal] = TimeSpan.FromMilliseconds(50),
        [GameSpeed.Fast] = TimeSpan.FromMilliseconds(20)
    };
}
```

## References

### Core Architectural Dependencies
- [ADR-006: Selective Abstraction](ADR-006-selective-abstraction-strategy.md) - **GameLoop implements IGameClock abstraction**
- [ADR-010: UI Event Bus](ADR-010-ui-event-bus-architecture.md) - **All UI events flow through UIEventBus**
- [ADR-004: Deterministic Simulation](ADR-004-deterministic-simulation.md) - Determinism requirements

### Related Patterns
- [ADR-009: Sequential Turn Processing](ADR-009-sequential-turn-processing.md) - Scheduler pattern
- [ADR-022: Two-Position Model](ADR-022-temporal-decoupling-pattern.md) - Movement truth
- [ADR-023: Game State Management](ADR-023-game-state-management.md) - State control
- [Battle Brothers Wiki - Initiative System](https://battlebrothers.fandom.com/wiki/Initiative) - Industry example
- [XCOM 2 Time Units](https://xcom.fandom.com/wiki/Time_Units) - Classic TU system
- [Roguelike Dev - Time Systems](https://www.roguebasin.com/index.php/Time_Systems) - Various approaches