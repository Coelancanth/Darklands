# ADR-013: Time-Based Action Scheduling

**Status**: Proposed  
**Date**: 2025-09-10  
**Decision Makers**: Tech Lead  

## Context

When implementing action scheduling for turn-based combat, we must choose between two fundamental approaches:

1. **Energy Accumulation** (used by DCSS, Angband, etc.): Actors accumulate "energy" based on player actions, acting when they reach a threshold
2. **Time-Based Scheduling**: Actors are scheduled on a timeline based on action costs and speeds

Our analysis of DCSS revealed that energy accumulation is a historical artifact from 1990s computational constraints that no longer apply.

### Energy Accumulation Problems

```csharp
// DCSS approach - arbitrary constants and batched resolution
const int ENERGY_THRESHOLD = 80;  // Why 80?
monster.Energy += monster.Speed * playerTime / 10;  // Why divide by 10?
while (monster.Energy >= THRESHOLD) {
    // All monster actions happen in batch after player acts
    ProcessMonsterAction();
    monster.Energy -= ActionCost;
}
```

Issues:
- **Magic constants**: Threshold=80 and scaling factors are arbitrary
- **Batched resolution**: Time "freezes" while monsters respond
- **Chunky speed differences**: Speed only matters at threshold boundaries
- **Complex state tracking**: Must maintain energy accumulators

### Time-Based Scheduling Advantages

```csharp
// Clean mathematical model
public record ScheduledActor(decimal NextActionTime, decimal Speed) {
    public ScheduledActor AfterAction(decimal actionCost) =>
        this with { NextActionTime = NextActionTime + actionCost / Speed };
}
```

Benefits:
- **Natural time flow**: Actions interleave realistically
- **Clean mathematics**: No arbitrary constants
- **Continuous speed**: Speed 1.7 means exactly 1.7x actions
- **Simple implementation**: ~5 lines of core logic

## Decision

We will use **time-based scheduling** instead of energy accumulation for all action timing.

### Core Implementation

```csharp
public class TimeScheduler
{
    private decimal _currentTime = 0;
    private readonly SortedSet<ScheduledAction> _timeline = new();
    
    public record ScheduledAction(decimal Time, ActorId Actor)
        : IComparable<ScheduledAction>
    {
        public int CompareTo(ScheduledAction? other) =>
            other is null ? 1 : 
            Time != other.Time ? Time.CompareTo(other.Time) :
            Actor.Value.CompareTo(other.Actor.Value); // Deterministic tie-breaker
    }
    
    public (ActorId actor, decimal time) GetNextAction()
    {
        var next = _timeline.First();
        _timeline.Remove(next);
        _currentTime = next.Time;
        return (next.Actor, _currentTime);
    }
    
    public void ScheduleAction(ActorId actor, decimal actionCost, decimal speed)
    {
        var nextTime = _currentTime + actionCost / speed;
        _timeline.Add(new(nextTime, actor));
    }
}
```

### Action Cost System

```csharp
// Use decimal for precise fractional costs
public readonly struct ActionCost
{
    public decimal Value { get; }
    
    // Standard action costs (in abstract time units)
    public static readonly ActionCost Move = new(10m);
    public static readonly ActionCost Attack = new(10m);
    public static readonly ActionCost QuickAttack = new(7m);
    public static readonly ActionCost HeavyAttack = new(15m);
    public static readonly ActionCost Potion = new(10m);
    public static readonly ActionCost Wait = new(10m);
}
```

### Speed System

```csharp
public readonly struct ActorSpeed
{
    public decimal Value { get; }
    
    // Speed modifiers (multiplicative)
    public static readonly ActorSpeed Normal = new(1.0m);
    public static readonly ActorSpeed Fast = new(1.5m);
    public static readonly ActorSpeed Slow = new(0.75m);
    public static readonly ActorSpeed VeryFast = new(2.0m);
    
    // Actual time = ActionCost / Speed
    // Speed 2.0 = actions take half as long
    // Speed 0.5 = actions take twice as long
}
```

## Integration with ADR-009

This scheduling approach integrates seamlessly with our sequential turn processing:

```csharp
public class GameLoopCoordinator
{
    private readonly TimeScheduler _scheduler;
    
    public void ProcessNextAction()
    {
        // 1. Get next actor from timeline (could be player or monster)
        var (actorId, currentTime) = _scheduler.GetNextAction();
        
        // 2. Process their action
        if (actorId == PlayerId)
            WaitForPlayerInput();
        else
            ProcessMonsterAction(actorId);
        
        // 3. Reschedule based on action taken
        _scheduler.ScheduleAction(actorId, actionCost, actorSpeed);
    }
}
```

## Consequences

### Positive
- **Realistic combat flow**: Faster actors naturally act more often throughout combat
- **Simple implementation**: Core logic in ~5 lines vs complex energy tracking
- **No magic numbers**: Clean mathematical model without arbitrary thresholds
- **Natural interruptions**: Can implement dodging/parrying between attacks
- **Smooth speed differences**: Speed 1.37 means exactly 1.37x as many actions
- **Easy to understand**: Time flows continuously, not in batches

### Negative
- **Different from roguelike tradition**: Players familiar with energy systems may need adjustment
- **Requires decimal arithmetic**: Slightly more complex than integer energy
- **Must handle tie-breaking**: When two actors act at same time (use deterministic ID comparison)

## Implementation Guidelines

1. **Use decimal for time values** to handle fractional speeds precisely
2. **Deterministic tie-breaking** when actors have same action time (sort by ActorId)
3. **Immutable scheduling state** - use records for ScheduledAction
4. **Action costs as value types** - use readonly structs
5. **Speed as multiplier** - higher speed = lower time cost

## Validation Criteria

1. **Deterministic ordering** - Same initial state produces same action sequence
2. **Correct speed ratios** - Actor with speed 2.0 acts exactly twice as often
3. **No timing artifacts** - No chunky behavior at threshold boundaries
4. **Smooth interpolation** - Speed changes apply smoothly
5. **Save/load compatibility** - Timeline state can be serialized

## Alternative Considered

**Energy Accumulation System** (rejected):
- Would match traditional roguelike patterns
- But adds complexity without benefit
- Magic constants (threshold=80) are arbitrary
- Batched resolution feels less realistic
- Historical artifact from 1990s constraints

## References

- [DCSS Combat Analysis](../../08-Learning/DCSS-Combat-Action-System-Analysis.md)
- [ADR-009: Sequential Turn Processing](ADR-009-sequential-turn-processing.md)
- [ADR-004: Deterministic Simulation](ADR-004-deterministic-simulation.md)

## Decision Record

Time-based scheduling provides a cleaner, more elegant solution than energy accumulation. It models time as it actually flows - continuously and uniformly - rather than in artificial batches. For a modern game in 2024, there's no reason to perpetuate the limitations of 1990s roguelike implementations.