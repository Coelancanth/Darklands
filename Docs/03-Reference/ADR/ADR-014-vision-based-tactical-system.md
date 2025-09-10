# ADR-014: Vision-Based Tactical System

## Status
Accepted

## Context

We need a system that creates tactical combat gameplay without explicit combat modes. After analyzing DCSS and other roguelikes, we identified that vision (line-of-sight) is the natural boundary between tactical and strategic gameplay.

The key insight: When actors can see each other, they need ordered turns. When no one can see anyone, time can flow abstractly. This creates emergent "combat" without artificial mode switches.

## Decision

We will implement a vision-based tactical system with these core principles:

### 1. Non-Reciprocal, Asymmetric Vision
**Each actor's vision is calculated independently based on their vision range.**

This enables tactical gameplay where you can see enemies who cannot see you (and vice versa). Combined with different vision ranges per actor type, this creates opportunities for stealth, ambush, and tactical positioning.

Example: A player (8 tile vision) can see a goblin (5 tile vision) at distance 6. The goblin cannot see the player, enabling a stealth approach.

### 2. Vision Triggers Scheduler Activation
**The turn-based scheduler activates when ANY actor has line-of-sight to ANY hostile actor.**

```csharp
bool ShouldUseScheduler() {
    return actors.Any(a => 
        actors.Any(b => a.IsHostileTo(b) && CanSee(a, b))
    );
}
```

This single rule creates all tactical gameplay:
- No vision connections → instant travel (abstract time)
- Vision established → turn-by-turn processing (tactical time)
- Breaking vision → escape from combat naturally

### 3. Asymmetric Vision Ranges
Different actor types have different vision ranges:
- Player: 8 tiles (baseline)
- Goblin: 5 tiles (poor vision)
- Human: 8 tiles (normal)
- Orc: 6 tiles (decent)
- Eagle: 12 tiles (superior)

This creates tactical variety and enables stealth gameplay (seeing enemies before they see you).

### 4. Proper Field-of-View using Recursive Shadowcasting
FOV implementation using recursive shadowcasting algorithm:
- Industry-standard algorithm used by many roguelikes
- Handles wall occlusion correctly (no diagonal exploits)
- Supports partial visibility and corner cases properly
- Enables hiding behind obstacles for stealth gameplay
- More complex than simple LOS but prevents future retrofitting

## Consequences

### Positive
- **Emergent Combat**: No explicit modes needed - combat emerges from vision
- **Natural Transitions**: Smooth flow between exploration and combat
- **Tactical Depth**: Asymmetric vision enables stealth and ambush tactics
- **Future Stealth**: Non-reciprocal vision enables proper stealth mechanics
- **Industry Standard**: Shadowcasting is proven and well-understood
- **No Exploits**: Proper FOV prevents diagonal vision and pillar dancing

### Negative
- **More Complex**: Shadowcasting takes longer to implement than simple LOS
- **Performance**: FOV calculations more expensive than simple distance
- **Learning Curve**: Players must understand vision asymmetry
- **Balance Critical**: Vision ranges become crucial balance points

## Alternatives Considered

### 1. Distance-Based Combat Activation (Rejected)
Activate scheduler when enemies within fixed distance (e.g., 15 tiles).
- **Rejected because**: Arbitrary distance, doesn't account for walls, feels artificial

### 2. Explicit Combat Modes (Rejected)
GameMode enum with Combat/Exploration states.
- **Rejected because**: Requires mode switching logic, not emergent, more complex

### 3. Complex Ray-Casting System (Rejected)
DCSS-style optimized ray-casting with precalculated rays.
- **Rejected because**: Premature optimization, unnecessary complexity for our needs

### 4. Symmetric Vision (Rejected)
All actors have the same vision range.
- **Rejected because**: Less tactical variety, no stealth gameplay potential

### 5. Reciprocal Vision (Rejected)
"If A sees B, then B sees A" - symmetric vision regardless of range.
- **Rejected because**: Completely negates asymmetric vision ranges, prevents stealth gameplay, removes tactical positioning opportunities

### 6. Simple Line-of-Sight (Rejected)
Basic ray from viewer to target checking for walls.
- **Rejected because**: Allows diagonal vision exploits, doesn't handle corners properly, would need retrofitting later for stealth

## Implementation Strategy

### Phase 1: Vision System (VS_011)
Implement basic vision queries:
- `CanSee(viewer, target)` - Core LOS check
- `GetVisibleActors(viewer)` - List visible actors
- Asymmetric vision ranges
- Wall occlusion

### Phase 2: Movement Integration (VS_012)
Apply vision to movement:
- Scheduler activation based on vision
- Adjacent-only movement when scheduled
- Pathfinding when no vision connections
- Interrupt on enemy becoming visible

### Phase 3: Future Enhancements (Enabled by Non-Reciprocal Vision)
- **Stealth/Detection Layer**: Visible but undetected states
- **Fog of War**: Remember last seen positions
- **Vision Modifiers**: Darkness, smoke, magic effects
- **Facing Direction**: Reduced vision behind actors
- **Alert States**: Unaware → Investigating → Alert → Pursuing

## Code Examples

### Vision Check
```csharp
public bool CanSee(Actor viewer, Actor target) {
    // Distance check first (cheap)
    var distance = Position.Distance(viewer.Position, target.Position);
    if (distance > viewer.VisionRange) return false;
    
    // Then line-of-sight (more expensive)
    return HasLineOfSight(viewer.Position, target.Position);
}
```

### Scheduler Activation
```csharp
public void ProcessPlayerMove(Position target) {
    if (IsAnyoneVisible()) {
        // Vision exists - tactical movement
        ProcessThroughScheduler(target);
    } else {
        // No vision - abstract movement
        ProcessInstantly(target);
    }
}
```

## References

- [DCSS Vision Analysis](../../08-Learning/DCSS-Vision-Stealth-Analysis.md)
- VS_011: Vision/LOS System Implementation
- VS_012: Vision-Based Movement System
- ADR-009: Sequential Turn Processing (scheduler foundation)

## Decision Record

- **Date**: 2025-09-11
- **Deciders**: Tech Lead, with user validation
- **Outcome**: Vision-based tactical system approved for implementation