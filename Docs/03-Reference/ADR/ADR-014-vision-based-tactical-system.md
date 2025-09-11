# ADR-014: Vision-Based Tactical System (Solo Player)

## Status
Accepted (Updated for solo player focus)

## Context

We need a system that creates tactical combat gameplay without explicit combat modes for a **solo player character**. After analyzing DCSS, JA2 1.13, and other roguelikes, we identified that vision (line-of-sight) is the natural boundary between tactical and strategic gameplay.

The key insight: When the player can see enemies (or enemies can see the player), ordered turns are needed. When no hostile visibility exists, time can flow abstractly. This creates emergent "combat" without artificial mode switches.

**Critical Constraint**: Single player character only - no party management, no team coordination.

## Decision

We will implement a vision-based tactical system with these core principles:

### 1. Non-Reciprocal, Asymmetric Vision
**Each actor's vision is calculated independently based on their vision range.**

This enables tactical gameplay where you can see enemies who cannot see you (and vice versa). Combined with different vision ranges per actor type, this creates opportunities for stealth, ambush, and tactical positioning.

Example: A player (8 tile vision) can see a goblin (5 tile vision) at distance 6. The goblin cannot see the player, enabling a stealth approach.

### 2. Vision Triggers Scheduler Activation
**The turn-based scheduler activates when the player has line-of-sight to any hostile OR any hostile has line-of-sight to the player.**

```csharp
bool ShouldUseScheduler() {
    // Solo player - much simpler check
    return monsters.Any(m => 
        m.State != ActivationState.Dormant && 
        (CanSee(player, m) || CanSee(m, player))
    );
}
```

This single rule creates all tactical gameplay:
- No vision connections → instant travel (abstract time)
- Vision established → turn-by-turn processing (tactical time)
- Breaking vision → escape from combat naturally
- Solo focus → No ally coordination needed

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
- More complex than basic ray-casting but prevents future retrofitting

### 5. Monster Activation States (Performance & Gameplay)
**Monsters exist in activation states to optimize performance and enable stealth:**

#### State Definitions
- **Dormant**: No FOV calculation, no AI processing, not in scheduler
- **Alert**: Investigating disturbance, full FOV calculation, joins scheduler
- **Active**: Full FOV calculation, full AI processing, in scheduler
- **Returning**: Transitioning to dormant, limited AI, still scheduled

#### Wake Triggers (Priority Order)
1. **Direct Vision**: Monster sees player (requires vision check)
2. **Player Vision**: Player sees monster (monster becomes alert)
3. **Damage**: Any damage immediately activates to Active
4. **Alert Chain**: Active monsters can alert nearby dormant ones
5. **Noise Events**: Future - combat/spells create noise radius

#### Performance Strategy
```csharp
// Tiered checks minimize expensive calculations
public void UpdateMonsterStates() {
    foreach (var monster in monsters) {
        if (monster.State == Dormant) {
            // Tier 1: Skip if too far (cheap)
            if (Distance(monster, player) > monster.VisionRange + 3) continue;
            
            // Tier 2: Alert if player sees monster (already calculated)
            if (playerFOV.Contains(monster.Position)) {
                monster.State = Alert;
            }
            // Tier 3: Active if monster sees player (use full FOV)
            else if (Distance(monster, player) <= monster.VisionRange) {
                var monsterFOV = CalculateFOV(monster);
                if (monsterFOV.Contains(player.Position)) {
                    monster.State = Active;
                }
            }
        }
    }
}
```

#### Gameplay Implications
- **Stealth Enabled**: Sneak past dormant monsters
- **Tactical Retreats**: Break LOS to escape (monsters eventually return to dormant)
- **Alert Behavior**: Monsters investigate disturbances
- **Ambush Tactics**: Player can set up before waking monsters

### 6. FOV Calculation Strategy
**Uniform FOV algorithm for all actors (optimization deferred):**

#### All Actors Use Shadowcasting
- Player uses full recursive shadowcasting
- Monsters use full recursive shadowcasting (same algorithm)
- Cached per turn, invalidated on movement
- **Principle**: Correctness first, optimization later

#### Monster FOV by State
- **Dormant**: No FOV calculation at all (can't see)
- **Alert**: Full shadowcasting with vision range
- **Active**: Full shadowcasting with vision range  
- **Returning**: Full shadowcasting with vision range

#### Clean Architecture First
```csharp
public class VisionService {
    private Dictionary<Actor, HashSet<Position>> fovCache = new();
    
    public HashSet<Position> CalculateFOV(Actor actor) {
        // Check cache first
        if (fovCache.ContainsKey(actor) && !actor.HasMoved) {
            return fovCache[actor];
        }
        
        // All actors use same shadowcasting algorithm
        var fov = RecursiveShadowcast(actor.Position, actor.VisionRange);
        fovCache[actor] = fov;
        return fov;
    }
    
    public bool CanSee(Actor viewer, Actor target) {
        // Dormant can't see
        if (viewer is Monster m && m.State == ActivationState.Dormant) {
            return false;
        }
        
        // Everyone else uses proper FOV
        var fov = CalculateFOV(viewer);
        return fov.Contains(target.Position);
    }
}
```

## Consequences

### Positive
- **Emergent Combat**: No explicit modes needed - combat emerges from vision
- **Natural Transitions**: Smooth flow between exploration and combat
- **Tactical Depth**: Asymmetric vision enables stealth and ambush tactics
- **Solo Optimized**: Much simpler than party-based vision systems
- **Industry Standard**: Shadowcasting is proven and well-understood
- **No Exploits**: Proper FOV prevents diagonal vision and pillar dancing
- **Performance Optimized**: Only one player FOV to calculate fully
- **Clear Player Focus**: No confusion about which character you're controlling

### Negative
- **No Allies**: Cannot implement ally vision sharing or coordination
- **Limited Tactics**: No flanking with party members, no overwatch setups
- **Balance Critical**: Vision ranges and wake distances become crucial balance points
- **State Management**: Must track and save monster activation states
- **Single Point of Failure**: Player death = game over

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
Implement basic vision queries with wake states:
- `CanSee(viewer, target)` - Core FOV check using shadowcasting
- `GetVisibleActors(viewer)` - List visible actors
- Asymmetric vision ranges
- Wall occlusion
- Basic monster wake states (Dormant/Active)

### Phase 2: Movement Integration (VS_012)
Apply vision to movement:
- Scheduler activation based on vision
- Adjacent-only movement when scheduled
- Pathfinding when no vision connections
- Interrupt on enemy becoming visible
- Wake dormant monsters when seen

### Phase 3: AI Integration (VS_013)
Implement wake-aware AI:
- Different AI behavior per activation state
- Alert state investigation behavior
- Return to dormant after losing player
- Alert propagation to nearby monsters

### Phase 4: Future Enhancements
- **Noise System**: Combat/spells create noise events
- **Stealth/Detection Layer**: Visible but undetected states
- **Fog of War**: Remember last seen positions
- **Vision Modifiers**: Darkness, smoke, magic effects
- **Patrol Routes**: Dormant monsters follow patrol patterns

## Code Examples

### Vision Check with Wake States
```csharp
public bool CanSee(Actor viewer, Actor target) {
    // Dormant monsters can't see anything
    if (viewer is Monster m && m.State == ActivationState.Dormant) {
        return false;
    }
    
    // Distance check first (cheap)
    var distance = Position.Distance(viewer.Position, target.Position);
    if (distance > viewer.VisionRange) return false;
    
    // Then full FOV check (shadowcasting)
    var fov = CalculateFOV(viewer);
    return fov.Contains(target.Position);
}
```

### Wake State Management
```csharp
public class MonsterActivationService {
    private VisionService visionService;
    
    public void UpdateActivationStates(IEnumerable<Monster> monsters, Actor player) {
        foreach (var monster in monsters) {
            switch (monster.State) {
                case ActivationState.Dormant:
                    CheckForWakeConditions(monster, player);
                    break;
                    
                case ActivationState.Active:
                    var monsterFOV = visionService.CalculateFOV(monster);
                    var playerFOV = visionService.CalculateFOV(player);
                    
                    if (!monsterFOV.Contains(player.Position) && 
                        !playerFOV.Contains(monster.Position)) {
                        monster.TurnsSincePlayerSeen++;
                        if (monster.TurnsSincePlayerSeen > 10) {
                            monster.State = ActivationState.Returning;
                        }
                    }
                    break;
                    
                case ActivationState.Returning:
                    if (monster.Position == monster.HomePosition) {
                        monster.State = ActivationState.Dormant;
                    }
                    break;
            }
        }
    }
    
    private void CheckForWakeConditions(Monster monster, Actor player) {
        // Tier 1: Too far - skip expensive checks
        var distance = Position.Distance(monster.Position, player.Position);
        if (distance > monster.VisionRange + 3) return;
        
        // Tier 2: Player sees monster - become alert
        var playerFOV = visionService.CalculateFOV(player);
        if (playerFOV.Contains(monster.Position)) {
            monster.State = ActivationState.Alert;
            monster.LastKnownPlayerPos = player.Position;
        }
        // Tier 3: Monster sees player - become active (full FOV)
        else if (distance <= monster.VisionRange) {
            var monsterFOV = visionService.CalculateFOV(monster);
            if (monsterFOV.Contains(player.Position)) {
                monster.State = ActivationState.Active;
                monster.LastKnownPlayerPos = player.Position;
            }
        }
    }
}
```

### Scheduler Activation
```csharp
public void ProcessPlayerMove(Position target) {
    // Check if any active/alert monsters can see player
    var tacticalMode = monsters.Any(m => 
        m.State != ActivationState.Dormant && 
        (CanSee(m, player) || CanSee(player, m)));
    
    if (tacticalMode) {
        // Vision exists - tactical movement
        ProcessThroughScheduler(target);
    } else {
        // No active threats - abstract movement
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