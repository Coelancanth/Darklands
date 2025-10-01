# Darklands Development Roadmap

**Purpose**: Sequence the complete [Vision.md](Vision.md) into actionable vertical slices with clear dependencies and milestones.

**Last Updated**: 2025-10-01
**Status**: Planning Phase (nothing built yet except VS_001 health system foundation)

---

## 📊 Overview: Vision to Shippable Game

**Vision**: Darklands (1992) spiritual successor with time-unit combat, skill progression, and emergent narrative

**Roadmap Philosophy**:
1. **Prove the core is fun first** - Time-unit combat must create tactical depth
2. **Incremental complexity** - Add systems only when foundation is solid
3. **Shippable at every phase** - Each phase delivers playable value
4. **Learn before scaling** - Small vertical slices reveal design problems early

---

## 🎯 Four Development Phases

```
Phase 1: Combat Core (3-6 months)
   ↓ Prove: Time-unit combat creates tactical decisions

Phase 2: Itemization & Depth (3-4 months)
   ↓ Prove: Build variety creates replayability

Phase 3: Strategic Layer (4-6 months)
   ↓ Prove: Macro/micro loop integration works

Phase 4: Emergent Narrative (3-4 months)
   ↓ Prove: Origins + events create memorable stories

= 13-20 months to first complete game
```

---

## 🔥 Phase 1: Combat Core (CURRENT FOCUS)

**Goal**: Prove tactical positioning + time-unit combat creates deep decisions

**Revised Approach** (Based on Game Designer feedback):
1. **Positioning First** - Grid + FOV creates tactical foundation (cover, line-of-sight, terrain)
2. **Simple Turn-Based** - Validate positioning mechanics with dummy enemy testing
3. **Add Time Complexity** - Layer time-unit queue on proven positioning system
4. **Add Progression** - Proficiency system rewards mastery

**Success Criteria**:
- Positioning creates meaningful decisions (smoke cover, flanking, range control)
- FOV feels roguelike (exploration, fog of war, vision-based tactics)
- Time-unit layer adds depth without overwhelming complexity
- Proficiency improvement is visible and rewarding (hour 1 vs. hour 10)
- "One more fight" addiction emerges from testing

**Shippable Deliverable**:
- Grid-based arena (30x30 with walls, floors, smoke terrain)
- FOV with fog of war (libtcod-style shadowcasting via GoRogue)
- Turn-based then time-unit combat (incremental complexity)
- 5 sequential encounters with proficiency progression

---

### VS_005: Grid, FOV & Terrain System ⭐ **START HERE**

**Status**: Not Started (Backlog item: VS_005)
**Owner**: Tech Lead (study GoRogue) → Dev Engineer (implementation)
**Size**: M (1-2 days: 4h GoRogue + 4h Godot + 4h dummy enemy)
**Priority**: Critical (tactical foundation for all combat)
**Depends On**: VS_001 (Health System - already complete ✅)

**What**: Grid-based movement with libtcod-style FOV using GoRogue library, terrain variety, and dummy enemy for manual testing

**Why**:
- Positioning creates tactical depth (cover, line-of-sight, ambush mechanics)
- FOV is table-stakes for roguelike feel
- Terrain variety enables strategic choices (hide in smoke, wall cover)
- Dummy enemy validates mechanics before AI complexity
- Foundation for all future combat features

**Scope**:
- **Domain**: `Position` (exists), `TerrainType` enum (Wall/Floor/Smoke), `GridMap` entity
- **Application**: `MoveActorCommand`, `CalculateFOVQuery`, `GetVisibleActorsQuery`
- **Infrastructure**:
  - **GoRogue NuGet Integration** - RecursiveShadowcastingFOV
  - FOVService wraps GoRogue, TransparencyMapAdapter for our GridMap
  - Study GoRogue source code (understand algorithm for future extensions)
- **Presentation**:
  - Godot TileMap (30x30 grid, 3 terrain tiles)
  - FOV visualization overlay, fog of war shader
  - Dummy enemy controls (Arrow keys = player, WASD = dummy, Tab = switch FOV display)

**Terrain Types** (Phase 1):
- **Wall** (`#`) - Opaque (blocks vision + movement)
- **Floor** (`.`) - Transparent (passable, see through)
- **Smoke/Bush** (`*`) - Opaque (blocks vision) + Passable (can walk through)

**FOV Specifics** (libtcod via GoRogue):
- Algorithm: Recursive shadowcasting (libtcod SHADOW mode)
- Vision radius: 8 tiles (standard roguelike)
- Fog of war: Unexplored (hidden), Explored (darker), Visible (full brightness)

**NOT in Scope**:
- ❌ Pathfinding (manual movement only)
- ❌ Semi-transparent smoke (treat as fully opaque for Phase 1)
- ❌ Multiple light sources / vision radii
- ❌ Procedural map generation (hand-craft test map)
- ❌ Time costs (simple turn-based, add in VS_008)
- ❌ AI behavior (dummy is manually controlled)

**Done When**:
- ✅ GoRogue package added, RecursiveShadowcastingFOV source studied
- ✅ Unit tests: Wall blocks movement + vision, Smoke blocks vision only, Vision radius = 8 tiles
- ✅ Integration tests: MoveActorCommand → FOV recalculates, GetVisibleActorsQuery uses FOV
- ✅ Godot scene: 30x30 test map, player + dummy controllable, FOV visualization works
- ✅ Manual validation: Hide player behind smoke → dummy's FOV doesn't include player tile
- ✅ Fog of war persists correctly (explored tiles stay darker when not visible)

**Phase**: 1 (Domain) → 2 (Application) → 3 (Infrastructure + GoRogue) → 4 (Presentation)

**Reference**:
- [GoRogue FOV Implementation](https://github.com/Chris3606/GoRogue/blob/master/GoRogue/FOV/RecursiveShadowcastingFOV.cs)
- [libtcod Shadowcasting Tutorial](http://roguebasin.com/index.php?title=FOV_using_recursive_shadowcasting)
- [RogueBasin: Field of Vision](http://roguebasin.com/index.php?title=Field_of_Vision)

---

### VS_006: Basic Combat System (Turn-Based with Dummy Enemy)

**Status**: Not Started
**Owner**: Dev Engineer
**Size**: S (4-6 hours)
**Priority**: Critical (validate FOV + positioning mechanics)
**Depends On**: VS_005 (Grid + FOV)

**What**: Simple turn-based combat with manually controlled dummy enemy

**Why**:
- Validate positioning tactics before adding AI complexity
- Test FOV integration (ranged attacks require line-of-sight)
- Manual dummy control lets us test edge cases (behind smoke, around corners)
- Foundation for AI in VS_007

**Scope**:
- **Domain**: `Weapon` value object (damage, no time costs yet)
- **Application**: `ExecuteAttackCommand` (uses VS_001 TakeDamageCommand), range checking
- **Infrastructure**: Attack validation (melee = adjacent, ranged = line-of-sight from FOV)
- **Presentation**:
  - Attack button appears when valid target in range
  - Turn indicator ("Player Turn" / "Dummy Turn")
  - Still using WASD to manually control dummy

**Attack Types** (Phase 1):
- **Melee**: Must be adjacent (8 tiles: orthogonal + diagonal)
- **Ranged**: Requires line-of-sight from FOV (max range 5 tiles)

**Turn-Based Flow**:
```
1. Player Turn: Move (arrow keys) OR Attack (if enemy in range/sight)
2. Dummy Turn: Manual control (WASD) OR Attack (if player in range/sight)
3. Repeat until one actor reaches 0 health
```

**NOT in Scope**:
- ❌ Time costs (still simple alternating turns)
- ❌ AI behavior (dummy is manually controlled)
- ❌ Weapon proficiency (add in VS_009)
- ❌ Multiple weapons (just sword for now)

**Done When**:
- ✅ Unit tests: Melee attack validates adjacency, Ranged attack validates FOV line-of-sight
- ✅ Integration tests: Attack → health reduces (uses VS_001 system)
- ✅ Godot scene: Attack button enabled/disabled based on range + FOV
- ✅ Manual validation:
  - Can't ranged attack through smoke (FOV blocks line-of-sight)
  - Can melee attack enemy in smoke (adjacency check only)
  - Turn-based flow feels tactical (positioning before attacking)

**Phase**: 1 (Domain) → 2 (Application) → 3 (Infrastructure) → 4 (Presentation)

---

### VS_007: Turn-Based AI

**Status**: Not Started
**Owner**: Dev Engineer
**Size**: S (4-6 hours)
**Priority**: Important (replace dummy with AI)
**Depends On**: VS_006 (Basic Combat)

**What**: Simple AI that uses FOV and makes turn-based tactical decisions

**Why**:
- Validate positioning creates tactical depth (AI chases when visible, patrols when not)
- Prove turn-based combat is fun before adding time-unit complexity
- Foundation for time-aware AI in VS_008

**Scope** (Minimal Viable AI):
- **Domain**: `AIBehavior` enum (Aggressive, Defensive, Balanced)
- **Application**: `CalculateAIActionCommand` (returns move or attack decision)
- **Infrastructure**: Simple decision tree (no pathfinding yet)

**AI Decision Logic**:
```
IF player visible (in AI's FOV):
  IF player in attack range:
    - Aggressive: Always attack
    - Defensive: Attack if health > 50%, else move away
    - Balanced: Attack if can kill player this turn
  ELSE:
    - Move toward player (closest adjacent tile)

IF player not visible:
  - Patrol (random adjacent tile) OR Idle
```

**NOT in Scope**:
- ❌ Pathfinding around obstacles (just move toward player, may get stuck)
- ❌ Tactical positioning (flanking, cover usage)
- ❌ Memory (doesn't remember last seen player position)
- ❌ Time-aware decisions (add in VS_008)

**Done When**:
- ✅ Unit tests: Aggressive AI attacks when player in range + visible
- ✅ Unit tests: Defensive AI retreats when health < 50%
- ✅ Unit tests: AI doesn't chase player if not in FOV
- ✅ Integration tests: AI uses CalculateFOVQuery to check visibility
- ✅ Manual playtest:
  - Hide behind smoke → AI doesn't chase (can't see you)
  - Step into AI's FOV → AI immediately moves toward you
  - 3 enemy types feel different (rat aggressive, bandit balanced, ogre defensive)

**Phase**: 2 (Application) → 3 (Infrastructure) only (no new domain/UI)

---

### VS_008: Time-Unit Queue Refactoring

**Status**: Not Started
**Owner**: Tech Lead (architecture) → Dev Engineer (refactor)
**Size**: M (1-2 days)
**Priority**: Important (adds timing tactical layer)
**Depends On**: VS_007 (Turn-Based AI)

**What**: Replace alternating turns with priority queue based on action time costs

**Why**:
- **Decision point**: Does turn-based positioning need timing complexity?
- If VS_007 playtesting shows positioning alone creates depth → maybe time-unit is YAGNI
- If turn-based feels too simple → time-unit adds "fast vs powerful" decisions
- This is a **refactoring** of working systems, not net-new features

**Scope**:
- **Domain**: `ActionQueue` entity, `TimeCost` value object
- **Application**: `EnqueueActionCommand`, `ProcessNextActionQuery`
- **Infrastructure**: Refactor turn manager to use priority queue
- **Presentation**: Queue visualization (timeline showing next 5 actions)

**Time Costs** (Phase 1 baseline):
- **Movement**: 100 units per tile
- **Melee Attack**: 100 units (sword baseline)
- **Ranged Attack**: 75 units (faster, less damage)
- **Wait**: 50 units (skip turn, regain initiative)

**Refactoring Tasks**:
1. Add time costs to existing MoveActorCommand
2. Add time costs to existing ExecuteAttackCommand
3. Replace turn manager with priority queue
4. Update AI to be time-aware (don't waste fast actions)
5. Add queue visualization UI

**NOT in Scope**:
- ❌ Weapon variety yet (just melee/ranged, add proficiency in VS_009)
- ❌ Variable time passage (environmental effects)
- ❌ Action interrupts/cancellation

**Done When**:
- ✅ Unit tests: Priority queue orders actions correctly by time
- ✅ Integration tests: Fast actions get more turns (rat attacks 2x before ogre attacks 1x)
- ✅ UI test: Queue timeline shows upcoming 5 actions
- ✅ Manual playtest: Time-unit adds tactical depth vs. turn-based (A/B test with VS_007)
- ✅ **Decision**: Keep time-unit OR revert to turn-based if complexity doesn't justify value

**Phase**: Refactoring (touches all 4 layers)

**Risk Mitigation**:
- Keep VS_007 turn-based version in git branch
- If time-unit doesn't add value → revert and skip VS_009/010
- Only proceed to proficiency if time-unit proves fun

---

### VS_009: Weapon Proficiency Tracking

**Status**: Not Started
**Owner**: Dev Engineer
**Size**: M (1-2 days)
**Priority**: Important (progression system)
**Depends On**: VS_008 (Time-Unit Queue) - **ONLY if time-unit kept after playtesting**

**What**: Track weapon usage and improve proficiency, reducing action time costs

**Why**:
- No-level progression system (Darklands philosophy)
- Proficiency makes time-unit system more rewarding (see improvement via faster actions)
- Creates specialization incentives (master daggers vs be generalist)

**Scope**:
- **Domain**: `WeaponProficiency` entity (weapon type + skill 0-100)
- **Application**: `RecordWeaponUseCommand`, `CalculateAttackTimeCostQuery`
- **Infrastructure**: Proficiency repository (in-memory)
- **Presentation**: Proficiency panel (progress bars per weapon type)

**Proficiency Formula** (tuned for 2-4 hour runs):
```
Base Time Cost = weapon base (dagger 50, sword 100, axe 150)
Proficiency Reduction = (skill / 100) × 0.30  // Max 30% at skill 100

Final Time Cost = Base × (1 - Proficiency Reduction)

Example:
- Dagger novice (skill 0): 50 units
- Dagger expert (skill 50): 42.5 units (-15%)
- Dagger master (skill 100): 35 units (-30%)
```

**Skill Gain**:
```
Gain per attack = 1 + (enemy_difficulty × 0.5)
- Rat: +1 skill
- Bandit: +1.5 skill
- Ogre: +2 skill

Time to mastery ≈ 50-70 attacks (5-10 fights)
```

**Done When**:
- ✅ Unit tests: Attack with dagger → skill increases, time cost reduces
- ✅ Integration tests: 50 sword attacks → 15% faster
- ✅ Manual playtest: Progression feels rewarding (visible every 2-3 fights)

**Phase**: 1 (Domain) → 2 (Application) → 3 (Infrastructure) → 4 (Presentation)

---

### VS_010: Arena Combat Integration (Phase 1 Complete)

**Status**: Not Started
**Owner**: Dev Engineer
**Size**: S (4-6 hours)
**Priority**: Important (playable milestone)
**Depends On**: VS_009 (Proficiency) OR VS_007 (if time-unit skipped)

**What**: Fully playable arena with 5 waves, complete progression loop

**Why**:
- First shippable milestone (can gather real user feedback)
- Validates Phase 1 hypothesis: "Is tactical combat fun?"
- Decision point: Proceed to Phase 2 OR pivot

**Scope**:
- **Presentation**: Arena scene with:
  - 30x30 grid (walls + smoke terrain)
  - Player + 3 weapon types (dagger/sword/axe)
  - Enemy spawner (5 waves: rat → rat×2 → bandit → bandit×2 → ogre)
  - FOV visualization + fog of war
  - If VS_008 kept: Queue timeline, proficiency panel
  - If VS_008 skipped: Just turn indicator, simpler UI

**Victory/Defeat**:
- Victory: Survive 5 waves, show stats (proficiency gains, time survived)
- Defeat: Show progress, encourage retry with different strategy

**Done When**:
- ✅ Manual playtest: Complete run feels fun, creates tactical decisions
- ✅ FOV + terrain create interesting positioning puzzles
- ✅ If time-unit: Timing adds depth, proficiency feels rewarding
- ✅ If turn-based: Positioning alone creates sufficient depth
- ✅ Players want to retry ("one more run" addiction)

**Phase**: 4 (Presentation integration)

**Playtest Questions**:
1. Do you make meaningful tactical decisions?
2. Does FOV + smoke create interesting scenarios?
3. [If time-unit] Does timing layer add enough value?
4. [If proficiency] Does progression feel rewarding?
5. Do you want to play again? Why?

**Phase 1 Decision Point**:
- ✅ **Proceed to Phase 2** if playtesters report combat is fun
- ⚠️ **Pivot** if combat feels shallow or tedious

---

## 🎨 Phase 2: Itemization & Depth (Future)

**Goal**: Prove build variety creates different playstyles and replay value

**When to Start**: After VS_007 playtesting confirms combat is fun

**Estimated Duration**: 3-4 months

### Planned Vertical Slices (High-Level)

**VS_008: Simplified Armor System**
- Single armor layer (protection + weight values)
- Weight affects movement and action time costs
- Foundation for Phase 2 layered armor

**VS_009: Tetris Inventory UI**
- Spatial grid inventory (item shapes matter)
- Weight capacity and movement speed integration
- Loot management puzzle

**VS_010: Damage Type System**
- Slashing, Piercing, Blunt damage types
- Armor resistance to damage types
- Weapon choice matters based on enemy armor

**VS_011: Layered Armor (Padding + Mail)**
- Two armor layers (gambeson + chainmail)
- Hit location system (head/torso/limbs)
- Damage penetration mechanics

**VS_012: Unique Weapons Collection**
- 10 unique weapons with build-enabling properties
- Angband-style itemization (situational value)
- Constrained randomization (minor stat variance)

**VS_013: NeoScavenger Interaction Panel**
- Drag-and-drop interaction grid
- Item + skill combinations
- Context-aware actions (location matters)

**Integration Milestone**: Phase 2 complete when 3+ viable builds emerge from playtesting

---

## 🗺️ Phase 3: Strategic Layer (Future)

**Goal**: Demonstrate macro/micro loop integration works

**When to Start**: After Phase 2 builds are balanced and fun

**Estimated Duration**: 4-6 months

### Planned Vertical Slices (High-Level)

**VS_014: Overworld Map**
- Simple node-based map (3 towns, 2 dungeons)
- Travel system with time passage
- Location-based encounters

**VS_015: Quest System**
- Basic contracts (clear dungeon, deliver item)
- Quest giver NPCs in towns
- Rewards (money, reputation)

**VS_016: Economy System**
- Money from quests and loot
- Equipment purchases in towns
- Repair costs for gear degradation

**VS_017: Simple Reputation System**
- 2-3 factions with standing values
- Reputation affects prices and quest availability
- Foundation for Phase 4 emergent narrative

**Integration Milestone**: Phase 3 complete when macro → micro → macro loop feels cohesive

---

## 📖 Phase 4: Emergent Narrative (Future)

**Goal**: Create "memorable run stories" like RimWorld

**When to Start**: After Phase 3 macro/micro integration works

**Estimated Duration**: 3-4 months

### Planned Vertical Slices (High-Level)

**VS_018: Origins System**
- 3-5 starting backgrounds (noble, soldier, peasant, scholar, merchant)
- Affects starting proficiencies, equipment, faction standings
- Defines early viable strategies

**VS_019: Event System**
- Random encounters with branching choices
- Consequences affect character state and reputation
- RimWorld-style procedural storytelling

**VS_020: Full Reputation System**
- Origin-specific quest lines unlock
- Faction relationships create emergent conflicts
- Dynamic world reacts to player choices

**VS_021: Character Aging & Time Pressure**
- Character ages over time
- Stat degradation with age (realism)
- Creates natural run duration (2-4 hours before retirement/death)

**Integration Milestone**: Phase 4 complete when players share "remember when..." stories

---

## 📊 Development Velocity Estimates

**Assumptions**:
- Working solo or small team
- Part-time development (10-20 hours/week)
- Includes testing, iteration, polish

**Phase 1** (Combat Core):
- 6 vertical slices × 1-2 days each = 12-18 dev days
- + Integration, playtesting, tuning = 6-10 additional days
- **Total: 3-6 months calendar time**

**Phase 2** (Itemization):
- 6 vertical slices × 1-3 days each = 15-25 dev days
- + Balance testing, build diversity = 8-12 additional days
- **Total: 3-4 months calendar time**

**Phase 3** (Strategic):
- 4 vertical slices × 2-4 days each = 15-30 dev days
- + World integration, economy tuning = 10-15 additional days
- **Total: 4-6 months calendar time**

**Phase 4** (Narrative):
- 4 vertical slices × 2-3 days each = 12-20 dev days
- + Event writing, faction balance = 8-12 additional days
- **Total: 3-4 months calendar time**

**Full Roadmap: 13-20 months to first complete game**

---

## 🎯 Success Milestones

**Phase 1 Success**:
- 10+ playtesters report "combat is fun"
- Players retry to try different weapon specializations
- "One more fight" addiction emerges

**Phase 2 Success**:
- 3+ distinct builds emerge (dagger assassin, armored tank, balanced warrior)
- Players share build guides on forums
- Loot decisions create meaningful trade-offs

**Phase 3 Success**:
- Players complete full runs (macro → micro → macro loop)
- Economy creates interesting choices (buy armor vs. stockpile potions)
- Reputation system creates long-term consequences

**Phase 4 Success**:
- Players share memorable run stories
- Origins create different early-game experiences
- Speedrunning community emerges

---

## ⚠️ Risk Management

### Risk 1: Time-Unit Combat Isn't Fun
**Mitigation**: Extensive playtesting after VS_007, pivot to simpler turn-based if needed

### Risk 2: Proficiency Grind Feels Tedious
**Mitigation**: Tunable formulas in VS_005, balance for 2-4 hour runs

### Risk 3: Scope Creep (Kitchen Sink Syndrome)
**Mitigation**: Strict phase gates - don't start Phase 2 until Phase 1 proves fun

### Risk 4: Layered Armor Too Complex
**Mitigation**: Phase 2 starts with simple armor (VS_008), only add layers if needed

### Risk 5: Solo Development Burnout
**Mitigation**: Shippable milestones every phase create motivation, early playtesting validates direction

---

## 📋 Next Actions

**Immediate (Product Owner)**:
1. ✅ Review roadmap with user for approval
2. Create VS_002 detailed specification using VerticalSlice_Template.md
3. Add VS_002 to Backlog.md with Critical priority

**After Approval (Tech Lead)**:
1. Review VS_002 architecture implications
2. Approve implementation approach
3. Hand off to Dev Engineer for phased implementation

**Current Status**: Awaiting user approval of roadmap structure and Phase 1 scope

---

*This roadmap sequences the complete [Vision.md](Vision.md) into 20+ vertical slices across 4 phases, balancing ambition with pragmatic incremental delivery. Each phase proves a core hypothesis before adding complexity.*
