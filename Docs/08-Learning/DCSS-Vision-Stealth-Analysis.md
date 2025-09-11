# DCSS Vision, Stealth, and Detection Analysis

**Created**: 2025-09-10  
**Purpose**: Technical analysis of DCSS implementation to inform VS_011 Movement System design  
**Author**: Tech Lead

## Executive Summary

DCSS implements a sophisticated vision and stealth system that creates emergent tactical gameplay through:
- **Asymmetric vision ranges** (not all creatures see equally far)
- **Probabilistic detection** (stealth vs perception checks each turn)
- **Line-of-sight reciprocity** (if you can't see them, they can't see you)
- **Noise propagation** (actions create noise that alerts but doesn't reveal)

**Key Insight for Darklands**: We should adopt the core concepts (asymmetric vision, interrupt-on-detection) but simplify the formulas significantly. DCSS's complexity evolved over 15+ years - we can learn from their design without inheriting their technical debt.

## 1. Vision System (LOS)

### Core Implementation (los.cc, los.h)

```cpp
// Default LOS radius is 7 tiles (LOS_DEFAULT_RANGE)
// Can be modified to LOS_RADIUS (up to 8) 
int los_radius = LOS_DEFAULT_RANGE;

// Vision is ALWAYS reciprocal
bool actor::can_see(const actor &target) const {
    return target.visible_to(this) && see_cell(target.pos());
}
```

### Key Design Decisions

1. **Reciprocal Vision**: If A can't see B, then B can't see A (with very rare exceptions)
2. **Cell-based LOS**: Uses ray-casting to determine visibility through grid cells
3. **Circular vision**: Default 7-tile radius, creating natural sight boundaries
4. **Opacity blocking**: Walls and other features block vision rays

### What We Should Adopt
- **Reciprocal vision** - prevents unfair "they see you but you don't see them" situations
- **Simple radius-based sight** - easy to understand and implement
- **Cell-based blocking** - walls naturally block vision

### What We Should Skip
- **Complex ray-casting** - DCSS precalculates thousands of rays for optimization
- **Corner-case handling** - years of edge cases we don't need yet
- **Dynamic LOS changes** - we can use fixed vision ranges

## 2. Stealth System

### Stealth Score Calculation (player.cc:3198)

```cpp
int player_stealth() {
    // Base: Dexterity * 3
    int stealth = you.dex() * 3;
    
    // Add skill (15 points per skill level)
    stealth += you.skill(SK_STEALTH, 15);
    
    // Armor penalty: 2 * (encumbrance^2) / 3
    const int evp = player_armour_stealth_penalty();
    const int penalty = evp * evp * 2 / 3;
    stealth -= penalty;
    
    // Various modifiers
    if (you.confused()) stealth /= 3;
    if (you.backlit()) stealth = stealth * 2 / 5;  // Glowing
    if (you.umbra()) stealth = stealth * 3 / 2;     // Shadow
    
    // Items and mutations add fixed "pips" (50 points each)
    stealth += STEALTH_PIP * [various bonuses];
    
    return stealth;
}
```

### Key Formula Insights

1. **STEALTH_PIP = 50**: Most bonuses add in increments of 50
2. **Dexterity matters**: Base stealth = DEX * 3 (typically 30-60)
3. **Skills scale linearly**: Each skill level adds 15 points
4. **Armor penalty is quadratic**: Heavy armor devastates stealth
5. **Environmental factors multiply**: Not just additive bonuses

### Simplified Version for Darklands

```csharp
public int CalculateStealth(Actor actor) {
    // Much simpler: Base + Skill - Armor
    int stealth = actor.Agility * 5;  // 50-150 range
    stealth += actor.StealthSkill * 10;
    stealth -= actor.ArmorWeight * 5;
    
    // Simple multipliers
    if (actor.IsGlowing) stealth /= 2;
    if (actor.InShadows) stealth = (stealth * 3) / 2;
    
    return Math.Max(0, stealth);
}
```

## 3. Detection Mechanics

### The Critical Check (shout.cc:252)

```cpp
bool check_awaken(monster* mons, int stealth) {
    // Monster perception score
    int mons_perc = 10 + (mons_intel(*mons) * 4) + mons->get_hit_dice();
    
    // Modifiers
    if (mons_is_wandering(*mons) && mons->foe == MHITYOU)
        mons_perc += 15;  // Alert for player
    
    if (!you.visible_to(mons))
        mons_perc -= 75;  // Can't see you (invisible, etc)
    
    if (mons->asleep())
        mons_perc -= 10;  // Sleeping (natural creatures)
    
    // The check: (perception + 1) / stealth chance
    if (x_chance_in_y(mons_perc + 1, stealth))
        return true; // Monster wakes/notices you!
    
    return false;
}
```

### Detection Formula Analysis

**Probability = (Perception + 1) / Stealth**

Examples:
- Perception 30, Stealth 250 = 12.4% chance per action
- Perception 50, Stealth 100 = 51% chance per action
- Perception 10, Stealth 500 = 2.2% chance per action

**Critical insight**: Check happens PER ACTION (movement or attack), not per turn or time unit.

### Asymmetric Vision Implementation

Different monsters have different vision ranges (implicit in code):
- Most monsters: Standard LOS (7 tiles)
- Some monsters: Reduced vision (understood through behavior)
- Player: Can be modified by mutations, items

## 4. CORRECTION: DCSS Interrupt Mechanics

**Important Clarification**: DCSS interrupts on VISIBILITY, not detection:
- **Interrupt occurs when**: Dangerous monster comes into player's view
- **Detection occurs when**: Monster's stealth check succeeds against player
- These are SEPARATE systems that work together

The correct understanding:
1. **Vision Interrupt**: Stop travel when you SEE danger (safety)
2. **Detection Check**: Whether monster NOTICES you (stealth gameplay)
3. **Key Insight**: Vision creates the tactical space, detection adds stealth layer

## 5. Practical Design Recommendations for VS_011

### Adopt These Concepts (Revised)

1. **Asymmetric Vision Ranges**
   ```csharp
   public enum VisionRange {
       Short = 5,    // Goblins, undead
       Normal = 8,   // Player, humans
       Long = 10,    // Eagles, watchers
       Superior = 12 // Special enemies
   }
   ```

2. **Vision-Based Scheduler Activation** (Primary System)
   ```csharp
   public bool ShouldUseScheduler() {
       // Scheduler activates when anyone sees anyone hostile
       return actors.Any(a => 
           actors.Any(b => a.IsHostileTo(b) && CanSee(a, b))
       );
   }
   ```

3. **Interrupt on Vision** (Not Detection)
   ```csharp
   public MoveResult MoveWithInterruption(Position from, Position to) {
       foreach (var step in GetPath(from, to)) {
           MoveToCell(step);
           
           // Interrupt when enemy becomes VISIBLE (not detected)
           var newlyVisible = GetNewlyVisibleEnemies();
           if (newlyVisible.Any()) {
               return MoveResult.Interrupted(step, newlyVisible);
           }
       }
       return MoveResult.Completed(to);
   }
   ```

4. **Detection as Future Enhancement** (Deferred)
   ```csharp
   // FUTURE: Add stealth layer on top of vision
   public bool CheckDetection(Actor viewer, Actor target) {
       if (!CanSee(viewer, target)) return false;
       
       // Stealth check only matters for unaware enemies
       if (viewer.State == ActorState.Unaware) {
           int perception = 10 + viewer.Awareness;
           int stealth = target.Stealth;
           return Random.Next(stealth) < perception;
       }
       return true; // Already aware
   }
   ```

### Skip These Complexities

1. **Noise System** - DCSS's noise propagation is complex and often confusing
2. **75+ Stealth Modifiers** - Start with 3-5 key factors
3. **Intelligence-based Perception** - Use fixed values per enemy type
4. **Sleep States** - Just use "unaware" vs "aware"

## 5. Console Output Design (Based on DCSS)

DCSS uses clear, informative messages for detection events:

```
// Movement in exploration
You move to (5, 5).

// Detection interruption
You start moving east.
A goblin comes into view.
The goblin shouts!
There is a goblin here.

// Asymmetric detection
You see an orc warrior.
The orc warrior hasn't noticed you yet.

// Combat proximity
You move north.  // Only adjacent moves allowed
The goblin closely misses you.
```

### Recommended Messages for Darklands

```csharp
// Exploration
"You arrive at ({x}, {y})"

// Interruption
"Movement interrupted - {enemy} spotted at ({x}, {y})!"
"The {enemy} hasn't noticed you (Vision: {range}, Distance: {dist})"

// Combat
"[Turn {n}] You move {direction} (100 TU)"
"[Turn {n}] {Enemy} moves {direction} (100 TU)"
```

## 6. Implementation Priority

### Phase 1: Core Vision (MUST HAVE)
- Asymmetric vision ranges
- Line-of-sight checking
- Vision reciprocity

### Phase 2: Basic Detection (MUST HAVE)
- Simple stealth scores
- Detection checks on movement
- Interrupt on detection

### Phase 3: Enhancements (NICE TO HAVE)
- Environmental modifiers (shadows, light)
- Stealth skills/equipment
- Different awareness levels per enemy type

### Phase 4: Polish (FUTURE)
- Noise/sound propagation
- Advanced stealth mechanics
- Vision-blocking abilities

## 7. Key Lessons from DCSS

### What Works Well
1. **Reciprocal vision** creates fair, predictable gameplay
2. **Interrupt-on-detection** prevents unfair deaths
3. **Asymmetric vision** adds tactical depth (scout carefully!)
4. **Simple formula** at core (perception vs stealth)

### What's Over-Engineered
1. **Ray-casting optimization** - premature optimization
2. **Dozens of special cases** - accumulated cruft
3. **Noise system** - rarely understood by players
4. **Complex multipliers** - hard to reason about

## 8. Recommended Approach for VS_011

```csharp
public class VisionSystem {
    // Simple, clear, effective
    
    public bool CanSee(Actor viewer, Actor target) {
        var distance = Position.Distance(viewer.Position, target.Position);
        if (distance > viewer.VisionRange) return false;
        return HasLineOfSight(viewer.Position, target.Position);
    }
    
    public DetectionResult CheckDetection(Actor viewer, Actor target) {
        if (!CanSee(viewer, target)) 
            return DetectionResult.NotVisible;
            
        if (target.IsPlayer && !viewer.HasDetectedPlayer) {
            // One-time check when player enters vision
            int roll = Random.Next(100);
            int chance = (viewer.Awareness * 100) / target.Stealth;
            
            if (roll < chance) {
                viewer.HasDetectedPlayer = true;
                return DetectionResult.NewDetection;
            }
            return DetectionResult.Undetected;
        }
        
        return DetectionResult.AlreadyDetected;
    }
}
```

## Conclusion

DCSS provides excellent design patterns for vision and stealth, but we should adopt the concepts, not the implementation. Their system evolved organically over 15+ years, accumulating complexity. We can achieve 90% of the gameplay value with 10% of the complexity by:

1. **Vision-based scheduler activation** - When anyone sees anyone hostile
2. **Asymmetric vision ranges** - Different creatures see different distances (5-12 tiles)
3. **Interrupt-on-visibility** - Stop movement when enemies come into view (safety)
4. **Reciprocal vision** - If you can't see them, they can't see you (fairness)
5. **Defer stealth/detection** - Add as enhancement later, not core system

The key insight: **Vision creates the tactical battlefield**. The scheduler should activate based on vision connections, not arbitrary distance or explicit modes. Combat emerges naturally when actors can see each other.

**Start with vision only, add stealth later when the core gameplay loop is proven.**

## References

- DCSS Source: `crawl-ref/source/`
  - `los.cc`, `los.h` - Vision implementation
  - `player.cc:3198` - Stealth calculation
  - `shout.cc:252` - Detection checks
  - `view.cc:134` - Monster reaction to player
  - `actor-los.cc` - Actor vision methods