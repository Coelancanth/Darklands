# DCSS Combat Action System Analysis

**Date**: 2025-09-10  
**Source**: Dungeon Crawl Stone Soup (crawl-ref/source)  
**Analyzed By**: Tech Lead  
**Updated**: 2025-09-10 (After verification against codebase)

## Executive Summary

Dungeon Crawl Stone Soup (DCSS) implements a sophisticated energy-based action system with phase-driven combat resolution. The system uses atomic time units (aut) with weighted probabilistic rounding to create variable-speed combat while maintaining mathematical fairness. The architecture provides valuable lessons for turn-based tactical games requiring deterministic combat with complex mechanics.

## Core Architecture Overview

### 1. Actor Hierarchy System

DCSS uses a classic object-oriented inheritance model:

```
actor (base class)
├── player
└── monster
```

**Key Design Decisions:**
- All combat entities inherit from `actor` base class (actor.h)
- Virtual methods define combat interface uniformly
- Both players and monsters use identical combat resolution paths
- No special-casing for player vs monster combat

### 2. Energy-Based Action System (Verified)

DCSS uses a sophisticated time-based system with atomic units and energy accumulation:

**Time Units:**
- **aut (Arbitrary Unit of Time)**: The atomic time unit, all actions resolve to integer auts
- **decaAut**: 10 aut, represents a "standard turn" (waiting, drinking potion, etc.)
- **BASELINE_DELAY**: Defined as 10 in defines.h, the standard action cost

**Energy Mechanics (Verified from monster.cc):**
- Actors have `speed_increment` that tracks accumulated energy
- Energy threshold is **80** (not 10) - defined as `ENERGY_THRESHOLD = 80` in monster.cc
- Standard speed monsters gain energy equal to their speed value per time unit
- When `speed_increment >= 80`, the monster can act (`has_action_energy()`)
- Actions consume energy based on `energy_usage` (typically 10 for movement)

**Weighted Rounding System (random.cc):**
```cpp
// div_rand_round implements probabilistic rounding
int div_rand_round(int num, int den) {
    int rem = num % den;
    if (rem)
        return num / den + (random2(den) < rem);
    else
        return num / den;
}
```
This ensures fractional delays (e.g., 5.4 aut) are fairly rounded: 40% chance of 5 aut, 60% chance of 6 aut.

**Player-Centric Time Flow:**
1. Player acts first (always has priority)
2. Player action consumes time (`you.time_taken`)
3. All monsters gain energy based on time consumed
4. Monsters with sufficient energy act sequentially
5. Process repeats with player's next action

### 3. Phase-Based Attack Resolution

Combat follows a strict phase progression system:

#### Attack Base Class Phases (attack.h)
1. **handle_phase_attempted()** - Validate attack can occur
2. **handle_phase_dodged()** - Check evasion
3. **handle_phase_blocked()** - Check shield blocking
4. **handle_phase_hit()** - Process successful hit
5. **handle_phase_damaged()** - Apply damage and effects
6. **handle_phase_killed()** - Handle death
7. **handle_phase_end()** - Cleanup and finalization

#### Melee-Specific Extensions (melee-attack.h)
- **handle_phase_aux()** - Additional attacks (kicks, bites, etc.)
- **handle_phase_cleaving()** - Area-of-effect attacks
- **handle_phase_multihit()** - Quick blade follow-ups

**Key Pattern**: Each phase returns a boolean indicating whether to continue to the next phase. This allows early termination on dodges, blocks, or special conditions.

### 4. Attack Class Hierarchy

```
attack (base combat resolution)
├── melee_attack (close combat)
└── ranged_attack (projectiles)
```

**Design Benefits:**
- Shared damage calculation logic
- Consistent brand/effect application
- Unified to-hit mechanics
- Reusable phase structure

### 5. Combat State Management

The attack classes maintain extensive state:

```cpp
class attack {
    // Core participants
    actor *attacker, *defender;
    actor *responsible;  // For blame/credit
    
    // Attack results
    bool attack_occurred;
    bool did_hit;
    int damage_done;
    int special_damage;
    
    // Weapon/brand info
    const item_def *weapon;
    brand_type damage_brand;
    attack_flavour attk_flavour;
    
    // Visibility for messaging
    bool attacker_visible, defender_visible;
    bool perceived_attack, obvious_effect;
};
```

### 6. Damage Calculation Pipeline

DCSS uses a multi-stage damage calculation:

1. **Base Damage** - From weapon or unarmed attack
2. **Skill Modifiers** - Fighting/weapon skills
3. **Stat Modifiers** - Strength/dexterity bonuses
4. **Brand Effects** - Elemental/special damage
5. **AC Reduction** - Armor class mitigation
6. **Resistance Application** - Elemental resistances
7. **Final Multipliers** - Vulnerabilities, etc.

Each stage is a separate method, allowing for:
- Easy debugging/tracing
- Consistent ordering
- Modular extension

### 7. Action Processing Loop

The main game loop processes actions via priority queue:

```cpp
// Simplified from mon-act.cc
while (!monster_queue.empty()) {
    monster *mon = monster_queue.top().first;
    int oldspeed = monster_queue.top().second;
    monster_queue.pop();
    
    if (!mon->has_action_energy())
        continue;
        
    handle_monster_move(mon);
    
    // Re-queue if still has energy
    if (mon->has_action_energy())
        monster_queue.emplace(mon, mon->speed_increment);
}
```

## Key Architectural Patterns

### 1. Phase-Driven Resolution
**Pattern**: Break complex operations into discrete, testable phases  
**Benefit**: Easier to debug, extend, and maintain  
**Application**: Any multi-step combat resolution

### 2. Energy Accumulation for Variable Speed
**Pattern**: Actors accumulate energy rather than taking fixed turns  
**Benefit**: Natural speed differences without complex timing  
**Application**: Games with speed/initiative mechanics

### 3. Unified Actor Interface
**Pattern**: Common base class for all combat participants  
**Benefit**: No special-casing, consistent mechanics  
**Application**: Any game with multiple entity types in combat

### 4. State Object for Complex Operations
**Pattern**: Encapsulate all operation state in a single object  
**Benefit**: Clean interfaces, easy to test, no global state  
**Application**: Complex multi-step processes

### 5. Virtual Method Customization Points
**Pattern**: Base class defines flow, subclasses customize steps  
**Benefit**: Consistent structure with flexible behavior  
**Application**: Systems with variations on common theme

## Critical Implementation Details

### 1. Attack Verb System
DCSS generates contextual combat messages based on damage:
- Damage thresholds determine verb intensity
- Per-weapon verb tables for flavor
- Special messages for critical events

### 2. Cleaving Implementation
Area attacks use a separate target list:
- Build target list before resolution
- Process each target through same phase system
- Damage reduction for secondary targets
- Prevents infinite cleave chains

### 3. Multi-Hit Handling
Quick weapons get follow-up attacks:
- Flag prevents recursive cleaving
- Same phase structure for consistency
- Energy cost applies once for all hits

### 4. Auxiliary Attacks
Additional attacks (kicks, bites) from mutations/forms:
- Processed after main attack succeeds
- Use simplified damage calculation
- Can trigger own effects/brands

## Lessons for Darklands

### What to Adopt

1. **Phase-Based Combat Resolution**
   - Clear, testable phases
   - Early termination on miss/block
   - Extensible for special abilities

2. **Time-Based Scheduling (Instead of Energy)**
   - More elegant and realistic than energy accumulation
   - Natural continuous time flow
   - Simpler conceptual model without magic constants

3. **Unified Actor Interface**
   - Consistent combat for all entities
   - No player/monster special cases
   - Simplified testing

4. **Attack State Objects**
   - Encapsulate all combat state
   - Pass between phases cleanly
   - No global state pollution

### What to Avoid

1. **Deep Inheritance Hierarchies**
   - DCSS's virtual method approach works but is rigid
   - Consider composition over inheritance
   - Use interfaces/traits instead

2. **Mutable Weapon References**
   - DCSS passes mutable item pointers
   - Dangerous for state consistency
   - Consider immutable value objects

3. **Mixed Concerns in Attack Classes**
   - DCSS attack classes handle damage AND messaging
   - Violates single responsibility
   - Separate combat logic from presentation

### Darklands-Specific Recommendations

1. **Use MediatR Commands for Actions**
   - Each action type as a command
   - Clean separation of concerns
   - Easy to test and extend

2. **Implement Phase Chain of Responsibility**
   - Each phase as a handler
   - Dynamic phase ordering
   - Pluggable special mechanics

3. **Value Objects for Combat State**
   - Immutable combat records
   - Functional transformation between phases
   - Natural save/replay support

4. **Separate Combat Rules Engine**
   - Pure functions for calculations
   - No side effects during resolution
   - Deterministic and testable

## Code Quality Observations

### Strengths
- Well-organized phase structure
- Clear separation of attack types
- Extensive state tracking
- Comprehensive effect system

### Weaknesses
- Heavy use of friend classes and globals
- Mixing of concerns (combat + UI)
- Complex inheritance hierarchies
- Some circular dependencies

### Maintenance Challenges
- Virtual method overrides spread across files
- State mutations during phases
- Complex damage calculation pipeline
- Hard to trace execution flow

## Compatibility with Darklands ADR-009

**DCSS's system is MORE compatible with ADR-009 than initially assessed:**

1. **Player Priority**: Player always acts first, monsters react to player time consumption
2. **Sequential Processing**: Monsters act one at a time, never in parallel
3. **Deterministic**: With seeded RNG, the weighted rounding is deterministic
4. **No Async**: Pure synchronous processing throughout

The energy system can be implemented within ADR-009's framework:
```csharp
// ADR-009 Compatible Implementation
public void ProcessPlayerAction(ICommand action)
{
    // 1. Player acts (priority) - matches ADR-009
    var result = ExecuteAction(action);
    var timeCost = CalculateActionCost(action); // in auts
    
    // 2. Time advances deterministically
    _gameTime += timeCost;
    
    // 3. Monsters gain energy sequentially
    foreach (var monster in _monsters.OrderByStable(m => m.Id))
    {
        monster.Energy += timeCost * monster.SpeedModifier / 8; // Scale for 80 threshold
        
        // 4. Monster acts if has enough energy (sequential)
        while (monster.Energy >= 80)
        {
            ProcessMonsterAction(monster);
            monster.Energy -= monster.ActionCost;
        }
    }
}
```

## Energy vs Scheduler: A Critical Comparison

### Energy Accumulation (DCSS Approach)
- **Batched Resolution**: Player acts → All monsters accumulate energy → Monsters act in sequence
- **Artificial "Turns"**: Time freezes while monsters respond to player actions
- **Magic Constants**: Threshold = 80 (arbitrary), energy gain = speed * time / 10 (why?)
- **Historical Artifact**: Inherited from 1990s roguelikes when processing each time slice was expensive

### Time-Based Scheduling (Modern Approach)
- **Continuous Time Flow**: Actions interleave naturally based on actual time
- **Realistic Model**: Speed 1.7 means exactly 1.7x as many actions, not chunky energy accumulation
- **Clean Mathematics**: NextActionTime = CurrentTime + ActionCost / Speed
- **Elegant Simplicity**: No arbitrary thresholds or scaling factors

### Why Scheduler is More Elegant (2024 Perspective)
1. **Models Reality Better**: Combat doesn't "batch" - faster actors act more frequently throughout
2. **Simpler Implementation**: ~5 lines of core logic vs complex energy tracking
3. **Natural Interruptions**: Can dodge/parry between enemy attacks (realistic)
4. **No Magic Numbers**: No need to explain why threshold is 80 or why we divide by 10
5. **Continuous Speed**: Speed differences are smooth, not chunky at threshold boundaries

## Conclusion

DCSS's combat system demonstrates a mature, feature-rich implementation of turn-based tactical combat. The system is fundamentally **player-centric and sequential**, aligning well with ADR-009's requirements. However, the energy accumulation system is a historical artifact from computational constraints that no longer exist.

The key insights for Darklands are:
1. **Combat resolution benefits from discrete, testable phases** with clear contracts
2. **Atomic time units (aut)** provide precision, but should use scheduler not energy
3. **Time-based scheduling** is more elegant and realistic than energy accumulation
4. **Functional programming style** for phases provides better testability than OO virtual methods
5. **Player-priority processing** ensures deterministic, sequential execution

For Darklands, we should:
- Adopt DCSS's phase-based combat resolution concept
- Use modern time-based scheduling instead of energy accumulation
- Implement with functional programming patterns for immutability
- Maintain ADR-009's sequential processing model with natural time flow

This gives us sophisticated combat mechanics with clean, elegant, deterministic execution.

## Appendix: Key Files for Reference

- **actor.h/cc** - Base actor interface
- **attack.h/cc** - Base attack resolution
- **melee-attack.h/cc** - Melee combat implementation  
- **mon-act.cc** - Monster action processing
- **fight.cc** - General combat utilities
- **delay.h/cc** - Multi-turn action system

---

*This analysis focuses on architectural patterns rather than specific mechanics. For detailed combat formulas and balance considerations, additional analysis of the damage calculation methods would be required.*