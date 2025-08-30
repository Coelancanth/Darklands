# Shattered Pixel Dungeon - Architecture Analysis for Darklands

**Created**: 2025-08-30
**Purpose**: Extract valuable combat patterns from SPD for Darklands implementation

## 🎯 Executive Summary

Shattered Pixel Dungeon (SPD) uses a **time-unit based turn system** with float-based timing that enables variable speed actors. This aligns perfectly with Darklands' vision of time-unit combat.

## 📐 Core Architecture Patterns

### 1. Actor System (Time Management)

**Location**: `actors/Actor.java`

#### Key Concepts:
- **TICK = 1.0f**: Base time unit for all actions
- **Float-based timing**: Allows fractional speeds (0.5 = double speed, 2.0 = half speed)
- **Priority queue processing**: Actors sorted by `time` field, earliest acts first
- **Act priorities**: When times are equal, higher priority acts first
  - VFX_PRIO = 100 (visual effects)
  - HERO_PRIO = 0 (player)
  - BLOB_PRIO = -10 (environmental effects)
  - MOB_PRIO = -20 (enemies)
  - BUFF_PRIO = -30 (status effects)

#### Implementation Pattern:
```java
// Core time management
private float time;
protected void spend(float time) {
    this.time += time;
}

// Processing loop
public static void process() {
    // Find actor with earliest time
    // Set now = actor.time
    // Call actor.act()
    // Repeat until no actors ready
}
```

#### Darklands Mapping:
- Replace our integer TimeUnits with float-based system
- Implement priority queue for turn processing
- Add act priorities for different entity types

### 2. Character System

**Location**: `actors/Char.java`

#### Key Properties:
- **HT/HP**: Max health / current health
- **baseSpeed**: Multiplier for action costs (1.0 = normal)
- **pos**: Position on grid (1D array index)
- **alignment**: ENEMY, NEUTRAL, ALLY (relative to hero)
- **fieldOfView**: Boolean array for visibility

#### Combat Flow:
1. **Attack Method**: `attack(Char enemy, float dmgMulti, float dmgBonus, float accMulti)`
2. **Hit Calculation**: Based on accuracy vs defense
3. **Damage Calculation**: Roll damage, apply multipliers, subtract armor
4. **Damage Application**: `damage(int dmg, Object src)`
5. **Death Handling**: `die(Object src)`

### 3. Buff System (Status Effects)

**Location**: `actors/buffs/`

#### Architecture:
- Buffs are Actors that spend time alongside their host
- Attached to characters via `Buff.affect(target, BuffClass)`
- Can modify stats, block actions, or trigger over time
- Stack or refresh based on implementation

#### Common Patterns:
```java
public class Burning extends Buff {
    @Override
    public boolean act() {
        target.damage(Random.Int(1, 5), this);
        spend(TICK);
        return true;
    }
}
```

### 4. Damage System

#### Damage Types:
- Physical (reduced by armor)
- Magical (bypasses armor)
- True damage (cannot be reduced)

#### Damage Pipeline:
1. **Pre-damage**: Buffs can intercept (Shield, Invulnerability)
2. **Armor reduction**: Physical damage reduced by armor value
3. **Damage resistance**: Percentage reduction from buffs
4. **Final damage**: Applied to HP
5. **Post-damage**: Triggers (on-hit effects, death checks)

## 🔄 Turn Order System Deep Dive

### How SPD Handles Variable Speed:

1. **Base Action Cost**: Most actions cost 1.0 time units
2. **Speed Modifiers**: 
   - Haste buff: `spend(time * 0.5)` - acts twice as often
   - Slow debuff: `spend(time * 2.0)` - acts half as often
   - Weapon speed: Fast weapons might `spend(0.8)`, slow ones `spend(1.5)`

3. **Turn Processing**:
```java
// Simplified process loop
while (gameRunning) {
    Actor next = findEarliestActor();
    currentTime = next.time;
    next.act();  // Returns true if turn continues
}
```

### Example Timeline:
```
Time 0.0: Hero attacks (spends 1.0) → next at 1.0
Time 0.0: Fast enemy attacks (spends 0.8) → next at 0.8
Time 0.8: Fast enemy acts again → next at 1.6
Time 1.0: Hero acts again → next at 2.0
Time 1.6: Fast enemy acts → next at 2.4
```

## 🎮 Valuable Mechanics to Adopt

### 1. Ballistica (Line of Sight/Projectiles)
**Location**: `mechanics/Ballistica.java`

Handles:
- Line of sight checks
- Projectile paths
- Collision detection
- Wall penetration options

### 2. PathFinder
**Location**: `com.watabou.utils.PathFinder`

Features:
- A* pathfinding
- Distance calculations
- Neighbor finding (4-way, 8-way)
- Path validation

### 3. Area of Effect
**Location**: `mechanics/ConeAOE.java`, `mechanics/ShadowCaster.java`

Patterns:
- Cone attacks
- Circular explosions
- Line attacks
- Vision/light calculations

## 📋 Proposed Vertical Slices Based on SPD

### VS_009: Time-Unit Turn System (Critical)
**What**: Implement float-based time system like SPD
**Why**: Enables variable speed combat per vision
**Components**:
- TimeScheduler (Actor.process() equivalent)
- Speed modifiers (weapons, buffs)
- Priority system for simultaneous actions

### VS_010: Basic Attack System
**What**: Port SPD's attack pipeline
**Why**: Proven combat formula with depth
**Components**:
- Hit/miss calculation
- Damage rolls with modifiers
- Armor reduction
- Death handling

### VS_011: Buff/Debuff Framework
**What**: Status effect system as time-based actors
**Why**: Core combat depth mechanism
**Components**:
- Buff base class
- Common buffs (Haste, Slow, Poison, Burning)
- Buff stacking/refresh logic
- Visual indicators

### VS_012: Line of Sight System
**What**: Port Ballistica for LoS and projectiles
**Why**: Essential for ranged combat and stealth
**Components**:
- Ray casting
- Obstacle detection
- Field of view calculation
- Fog of war

## 🏗️ Architecture Recommendations

### 1. Adopt Float-Based Time Units
**Current**: Integer TimeUnits
**Recommended**: Float-based like SPD
**Benefit**: Natural speed variations without complex fractions

### 2. Implement Actor Priority Queue
**Current**: Not yet implemented
**Recommended**: Port SPD's actor processing
**Benefit**: Efficient, proven turn order system

### 3. Buffs as Actors
**Current**: Not yet designed
**Recommended**: Buffs inherit from Actor
**Benefit**: Unified time management, automatic cleanup

### 4. Damage Pipeline Pattern
**Current**: Not yet implemented
**Recommended**: Port SPD's multi-stage damage
**Benefit**: Extensible for armor, resistance, shields

## 🔍 Code Extraction Priority

### High Priority (Core Combat):
1. `Actor.java` - Time management system
2. `Char.java` - Character combat core (attack, damage, death)
3. `Buff.java` - Status effect framework
4. `Ballistica.java` - Line of sight/projectiles

### Medium Priority (Combat Features):
1. `Hero.java` - Player-specific mechanics
2. `Mob.java` - Enemy AI framework
3. Selected buffs - Haste, Slow, Poison, Burning
4. `PathFinder.java` - Movement/pathfinding

### Low Priority (Polish):
1. Particle effects
2. Sound triggers
3. Achievement tracking
4. Statistics

## 📝 Integration Strategy

### Phase 1: Time System Migration
1. Create FloatTimeScheduler based on Actor.process()
2. Migrate Grid commands to use float time
3. Add speed property to actors

### Phase 2: Combat Pipeline
1. Port attack calculation
2. Implement damage pipeline
3. Add armor/resistance

### Phase 3: Buff System
1. Create buff framework
2. Implement core buffs
3. Add UI indicators

### Phase 4: Advanced Mechanics
1. Port Ballistica for LoS
2. Add projectile system
3. Implement AoE attacks

## ⚠️ Considerations

### What NOT to Copy:
- UI/Scene management (we use Godot)
- Save system (different architecture)
- Item system (start simpler)
- All 100+ buffs (start with 5-10)

### Architectural Differences:
- SPD uses Java, we use C#
- SPD uses libGDX, we use Godot
- SPD is single-threaded, we must handle Godot threading
- SPD uses 1D arrays for grid, we use 2D

## 🎯 Next Steps

1. **Review with Product Owner**: Validate these mechanics align with vision
2. **Create TD items**: For float-based time system migration
3. **Update Glossary**: Add SPD-inspired terms (Ballistica, Buff, etc.)
4. **Prototype TimeScheduler**: In isolated test project first

---

*This document will evolve as we extract more patterns from SPD and integrate them into Darklands.*