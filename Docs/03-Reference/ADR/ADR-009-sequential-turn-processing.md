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

## Architecture Pattern

### Game Loop Structure
```csharp
public class GameLoopCoordinator
{
    private readonly CombatScheduler _scheduler;
    private readonly IMediator _mediator;
    
    public void ProcessTurn()
    {
        // 1. Get next actor from scheduler
        var actor = _scheduler.ProcessNextTurn();
        
        // 2. Determine action (player input or AI)
        var action = GetAction(actor);
        
        // 3. Execute action synchronously
        var result = _mediator.Send(action);
        
        // 4. Update UI synchronously
        UpdatePresentation(result);
        
        // 5. Let Godot handle rendering (automatic)
    }
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
    void MoveActor(ActorId id, Position from, to);   // Synchronous
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

## Implementation Guidelines

### Phase 1: Domain/Application Layer
- Verify all commands/handlers are synchronous ✅ (already done)
- Ensure Fin<T> used for error handling ✅ (already done)
- No changes needed - already correct

### Phase 2: Game Loop Coordinator
- Create GameLoopCoordinator class
- Connect to CombatScheduler
- Implement turn processing logic
- Handle player input vs AI decisions

### Phase 3: Fix Presentation Layer
- Remove all Task.Run() calls
- Remove async/await from presenters
- Fix ActorView shared field issue
- Update IView interfaces to be synchronous

### Phase 4: Godot Integration
- Use CallDeferred for thread marshalling only
- Let Godot's _process() handle frame updates
- Ensure scene tree operations on main thread

## Validation Criteria

1. **All actors display correctly** on scene initialization
2. **Turn order is deterministic** and predictable
3. **No race conditions** in UI updates
4. **Actions complete atomically** before next turn
5. **Tests pass without async complications**

## References

- [Shattered Pixel Dungeon Actor System](https://github.com/00-Evan/shattered-pixel-dungeon)
- [Godot Threading Model](https://docs.godotengine.org/en/stable/tutorials/performance/thread_safe_apis.html)
- [Traditional Roguelike Architecture](http://www.roguebasin.com/index.php/Game_Loop)
- TD_011: Async/Concurrent Architecture Mismatch (root cause analysis)

## Decision Record

This ADR addresses a fundamental architectural mismatch where async patterns were incorrectly applied to inherently sequential turn-based game logic. The sequential pattern is not just preferred—it's the only correct approach for deterministic turn-based games.