# ADR-009: Sequential Turn-Based Processing Pattern

**Status**: Accepted  
**Date**: 2025-09-07  
**Decision Makers**: Tech Lead  

## Context

Our current implementation uses async/await patterns with Task.Run() in the presentation layer, causing race conditions where actors fail to display correctly. This is fundamentally incompatible with turn-based game architecture.

### Evidence of the Problem

1. **Race Condition in ActorView.cs**:
```csharp
// PROBLEM: Shared fields overwritten by concurrent calls
private ColorRect? _pendingActorNode;  // Player sets this
private ActorId _pendingActorId;       // Dummy overwrites it
// Result: Only last actor displays
```

2. **Inappropriate Concurrency in Presenters**:
```csharp
Task.Run(async () => await View.DisplayActorAsync(...));  // Actor 1
Task.Run(async () => await View.DisplayActorAsync(...));  // Actor 2 races
```

3. **Traditional Roguelike Pattern** (from SPD analysis):
```java
// CORRECT: Sequential processing
while (gameRunning) {
    Actor next = findEarliestActor();
    next.act();  // Completes before next actor
}
```

## Decision

We will adopt a **strictly sequential turn-based processing pattern** that aligns with traditional roguelike architecture:

1. **Remove ALL async/await from game logic**
2. **Implement sequential game loop coordinator**
3. **Use Godot's built-in rendering loop**
4. **Keep operations synchronous and deterministic**

### Reviewer Addendum (2025-09-08)

This pattern must explicitly coordinate the time spent playing animations/effects. A simple synchronous loop is insufficient without a state machine that models the transition between instantaneous game logic and time-consuming presentation. We therefore adopt a small game loop state machine with a dedicated PlayingAnimation state, and use a presentation orchestrator to call back when animations complete.

- Use a synchronous command bus adapter (see Synchronous Command Bus section) to route through MediatR without blocking on `.GetAwaiter().GetResult()` in presenters.
- When iterating collections inside the loop (e.g., selecting targets, resolving effects), ensure deterministic ordering per ADR-004 (use `OrderByStable` with a deterministic tie-breaker; never depend on `Dictionary`/`HashSet` iteration order).

## Architecture Pattern

### Game Loop Structure with Time-Based Scheduler
```csharp
public class GameLoopCoordinator
{
    private readonly TimeScheduler _scheduler;  // Time-based, not energy-based
    private readonly ICommandBus _commands;     // Synchronous command bus
    private bool _isProcessing;                 // Reentrancy guard
    
    public void ProcessTurn()
    {
        if (_isProcessing) return; // Prevent re-entrancy from UI callbacks
        _isProcessing = true;
        try
        {
            // 1. Get next actor from time-based scheduler
            var (actor, currentTime) = _scheduler.GetNextActor();
            
            // 2. Determine action (player input or AI)
            var action = GetAction(actor);
            
            // 3. Execute action synchronously
            var result = _commands.Send(action);
            
            // 4. Reschedule actor based on action cost
            _scheduler.RescheduleActor(actor, result.ActionCost);
            
            // 5. Update UI synchronously
            UpdatePresentation(result);
            
            // 6. Let Godot handle rendering (automatic)
        }
        finally
        {
            _isProcessing = false;
        }
    }
}

// Clean time-based scheduler (no energy accumulation)
public class TimeScheduler
{
    private decimal _currentTime = 0;
    private SortedSet<(decimal Time, ActorId Actor)> _timeline;
    
    public (ActorId Actor, decimal Time) GetNextActor()
    {
        var next = _timeline.First();
        _timeline.Remove(next);
        _currentTime = next.Time;
        return (next.Actor, _currentTime);
    }
    
    public void RescheduleActor(ActorId actor, decimal actionCost)
    {
        var nextTime = _currentTime + actionCost / GetActorSpeed(actor);
        _timeline.Add((nextTime, actor));
    }
}
```

### State Machine Loop (Recommended)
```csharp
public enum GameLoopState
{
    AwaitingAction,
    ProcessingAction,
    PlayingAnimation,
    TurnEnded
}

public sealed class GameLoopCoordinator
{
    private readonly ICommandBus _commandBus;
    private readonly IPresentationOrchestrator _presentation;
    private readonly CombatScheduler _scheduler;
    private GameLoopState _state = GameLoopState.TurnEnded;
    private Option<ICommand> _pendingAction = None;
    
    // Called from a per-frame driver in Presentation (no Godot types here)
    public void Update()
    {
        switch (_state)
        {
            case GameLoopState.TurnEnded:
                StartNewTurn();
                _state = GameLoopState.AwaitingAction;
                break;
            case GameLoopState.AwaitingAction:
                if (_pendingAction.IsSome)
                {
                    _state = GameLoopState.ProcessingAction;
                    ProcessAction(_pendingAction.IfNoneUnsafe(() => throw new InvalidOperationException()));
                    _pendingAction = None;
                }
                break;
            case GameLoopState.ProcessingAction:
            case GameLoopState.PlayingAnimation:
                // Driven by callbacks; no polling work here
                break;
        }
    }
    
    public void SubmitAction(ICommand action)
    {
        if (_state == GameLoopState.AwaitingAction && _pendingAction.IsNone)
            _pendingAction = Some(action);
    }
    
    private void StartNewTurn()
    {
        _scheduler.ProcessNextTurn();
    }
    
    private void ProcessAction(ICommand action)
    {
        // 1) Execute logic synchronously
        var result = _commandBus.Send<CombatResult>(action);
        
        // 2) Hand off to presentation orchestrator, then wait for callback
        _state = GameLoopState.PlayingAnimation;
        _presentation.PlayAnimationsForResult(result, OnAnimationsComplete);
    }
    
    private void OnAnimationsComplete()
    {
        _state = GameLoopState.TurnEnded;
    }
}
```

### Presentation Orchestrator Interface
```csharp
public interface IPresentationOrchestrator
{
    // Implemented in Presentation; sequences tweens/effects and invokes callback when done
    void PlayAnimationsForResult(CombatResult result, Action onComplete);
}
```

### Synchronous Command Bus (Adapter)
```csharp
public interface ICommand { }
public interface ICommandBus
{
    Fin<TResult> Send<TResult>(ICommand command);
}

// Example adapter if using MediatR internally without async in the game loop
public sealed class MediatorCommandBus : ICommandBus
{
    private readonly IMediator _mediator;
    public MediatorCommandBus(IMediator mediator) { _mediator = mediator; }
    public Fin<TResult> Send<TResult>(ICommand command)
        => _mediator.Send((IRequest<Fin<TResult>>)command).GetAwaiter().GetResult();
}
```

### Presentation Layer Pattern
```csharp
public class ActorPresenter
{
    public void HandleActorCreated(ActorId id, Position pos)
    {
        // Direct synchronous call - no Task.Run()
        View.DisplayActor(id, pos);  
        
        // Godot handles thread safety internally
        if (RequiresMainThread())
            View.CallDeferred("DisplayActorDeferred", id, pos);
    }
}
```

### View Interface (Simplified)
```csharp
public interface IActorView
{
    void DisplayActor(ActorId id, Position pos);     // Synchronous
    void MoveActor(ActorId id, Position from, Position to);   // Synchronous
    void RemoveActor(ActorId id);                    // Synchronous
    // NO async methods
}
```

## Consequences

### Positive
- **Eliminates race conditions** - Sequential execution prevents data races
- **Simplifies debugging** - Linear execution flow easy to trace
- **Improves determinism** - Same input always produces same output
- **Aligns with roguelike standards** - Follows proven patterns from NetHack, DCSS, SPD
- **Reduces complexity** - No async state management needed
- **Better performance** - No Task overhead for synchronous operations

### Negative
- **Major refactoring required** - Must update all presenters and views
- **Learning curve** - Team must understand Godot's threading model
- **No real-time features** - Cannot add real-time elements later (acceptable for roguelike)
- **Requires loop state machine and presentation orchestrator** - Additional boilerplate to coordinate animation time

## Implementation Guidelines

### Phase 1: Domain/Application Layer
- Verify commands/handlers expose synchronous entry points (adapters allowed)
- Ensure Fin<T> used for error handling (ADR-008)
- Validate deterministic math and RNG usage (ADR-004)

### Phase 2: Game Loop Coordinator
- Create GameLoopCoordinator class
- Connect to CombatScheduler
- Implement turn processing logic
- Handle player input vs AI decisions
- Add reentrancy guards; prevent nested ProcessTurn calls from UI events
- Ensure single-threaded processing; no Task.Run or async void

### Phase 3: Fix Presentation Layer
- Remove all Task.Run() calls
- Remove async/await from presenters
- Fix ActorView shared field issue
- Update IView interfaces to be synchronous
- Use CallDeferred only for main-thread marshalling
- Apply stable ordering when iterating unordered collections for UI updates

### Phase 4: Godot Integration
- Use CallDeferred for thread marshalling only
- Let Godot's _process() handle frame updates
- Ensure scene tree operations on main thread
- Keep hydrator (ADR-005) separate from loop; no scene building in the loop

## Validation Criteria

1. **All actors display correctly** on scene initialization
2. **Turn order is deterministic** and predictable
3. **No race conditions** in UI updates
4. **Actions complete atomically** before next turn
5. **Tests pass without async complications**
6. **No loop advancement during animations** - While in PlayingAnimation, next turn cannot start

## References

- [Shattered Pixel Dungeon Actor System](https://github.com/00-Evan/shattered-pixel-dungeon)
- [Godot Threading Model](https://docs.godotengine.org/en/stable/tutorials/performance/thread_safe_apis.html)
- [Traditional Roguelike Architecture](http://www.roguebasin.com/index.php/Game_Loop)
- TD_011: Async/Concurrent Architecture Mismatch (root cause analysis)

## Decision Record

This ADR addresses a fundamental architectural mismatch where async patterns were incorrectly applied to inherently sequential turn-based game logic. The sequential pattern is not just preferred—it's the only correct approach for deterministic turn-based games.