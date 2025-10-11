# Combat Roadmap

**Purpose**: High-level roadmap for Darklands combat systems - Stoneshard-inspired time-unit tactical combat with Darklands-style armor simulation.

**Last Updated**: 2025-10-09 23:11 (Product Owner: Extracted combat roadmap from main roadmap, high-level only)

**Parent Document**: [Roadmap.md](Roadmap.md#combat) - Main project roadmap

---

## Quick Navigation

**Core Systems**:
- [Vision & Philosophy](#vision--philosophy)
- [Current State](#current-state)
- [Completed Features](#completed-features)
- [Planned Features](#planned-features)

**Planned Systems**:
- [Enemy AI & Vision](#enemy-ai--vision-system-planned)
- [Armor Systems](#armor-systems-planned)
- [Weapon Proficiency](#weapon-proficiency-tracking-planned)

---

## Vision & Philosophy

**Vision**: Stoneshard-inspired time-unit tactical combat - every action costs time, positioning matters, equipment defines capabilities.

**Philosophy**:
- **Time-unit system** - Different actions cost different time (dagger=fast, axe=slow)
- **Positioning matters** - Melee=adjacent, ranged=line-of-sight, walls block attacks
- **Equipment defines capabilities** - No levels, your gear determines combat effectiveness
- **Realistic armor simulation** - Armor layers interact with damage types (Darklands pattern)
- **Asymmetric combat** - Enemies can detect you first, initiate ambushes

**Design Principles**:
1. **Every action has cost** - Time, stamina, positioning (tactical decisions)
2. **Build variety** - Light skirmisher vs heavy tank (equipment choices)
3. **Exploration/combat modes** - Natural transitions via FOV detection
4. **Component-based actors** - Flexible composition (player, enemies, NPCs)

---

## Current State

**Completed Systems**:
- ✅ **VS_007**: Time-Unit Turn Queue (exploration/combat mode transitions, FOV integration)
- ✅ **VS_020**: Basic Combat (click-to-attack, damage, range validation, death handling)

**Missing Systems**:
- ❌ **Enemy AI** - Enemies don't detect player or make autonomous decisions
- ❌ **Stats & Equipment** - No attributes, can't equip items (see [Roadmap_Stats_Progression.md](Roadmap_Stats_Progression.md))
- ❌ **Armor System** - No defense calculation, damage types, or armor layers
- ❌ **Weapon Proficiency** - No skill progression or time cost reduction
- ❌ **Status Effects** - No poison, bleeding, buffs/debuffs

**Architectural Foundation**:
- ✅ Component pattern (Actor + IHealthComponent + IWeaponComponent)
- ✅ Time-unit turn queue (priority queue, player-first tie-breaking)
- ✅ FOV integration (combat entry/exit via detection events)
- ✅ ActorTemplate system (data-driven actor definitions)

---

## Completed Features

### VS_007: Time-Unit Turn Queue System

**Status**: Complete (2025-10-04) | **Size**: L (3 days) | **Tests**: 359 passing (49 new)

**What**: Time-unit combat system with natural exploration/combat mode transitions via turn queue size.

**Delivered**:
- Time-unit turn queue (priority queue, TimeUnits value object)
- Natural mode detection (queue size = combat state, no state machine)
- FOV-based symmetric combat enter/exit (EnemyDetectionEventHandler, CombatEndDetectionEventHandler)
- Movement costs (10 units in combat, instant in exploration)
- Player-first tie-breaking (player acts first when equal time)
- Production-ready logging (Gruvbox semantic highlighting)

**Key Design Decisions**:
- **Turn queue size = combat state** - No separate state machine needed
- **Relative time model** - Resets to 0 per combat session (no drift)
- **Player permanently in queue** - Never fully removed (always ready to act)
- **FOV events drive transitions** - Symmetric enter AND exit (detection-based)
- **Player-centric FOV only** - Enemies don't detect player yet (deferred to VS_011)

**Example Scenario** (Exploration → Combat → Reinforcement → Victory):
```
1. Player clicks distant tile → auto-path starts (exploration mode)
2. Step 3: Goblin appears in FOV → auto-path cancelled (combat starts, time=0)
   Queue: [Player@0, Goblin@0] → Player acts first (tie-breaking)
3. Player moves 1 step (costs 100) → Player@100, Goblin@0
4. Goblin attacks (costs 150) → Goblin@150, Player@100
5. Player moves → Orc appears in FOV (reinforcement!)
   Queue: [Orc@100, Player@200, Goblin@150] → Orc acts next
6. Player defeats Goblin → removed from queue
7. Player defeats Orc → queue=[Player] → combat ends (time resets)
8. Next click resumes auto-path (exploration mode)
```

**Archive**: [Completed_Backlog_2025-10.md](../../07-Archive/Completed_Backlog_2025-10.md) (search "VS_007")

---

### VS_020: Basic Combat System (Attacks & Damage)

**Status**: Complete (2025-10-06) | **Size**: M (1-2 days) | **Tests**: 428 passing

**What**: Click-to-attack combat with component pattern, range validation, damage application, death handling.

**Delivered**:
- Component pattern (Actor + IHealthComponent + IWeaponComponent)
- Click-to-attack UI (tactical target selection)
- ExecuteAttackCommand (range validation, damage application)
- Range validation (melee=adjacent, ranged=line-of-sight)
- Death handling (remove from turn queue + position service)
- ActorTemplate integration (designers configure components in .tres files)

**Key Architecture Decisions**:
- **Component pattern** - Scales to 50+ actor types, reuse across player/enemies/NPCs
- **Tactical positioning** - Melee requires adjacency, ranged requires line-of-sight
- **FOV integration** - Walls block ranged attacks (uses existing FOV system)
- **ActorTemplate compliance** - ADR-006 pattern (designers configure components)

**Combat Flow**:
```
1. Player clicks enemy in range → ExecuteAttackCommand
2. Range validation:
   - Melee: Chebyshev distance ≤ 1 (8 directions)
   - Ranged: Distance check + FOV line-of-sight (walls block)
3. Damage applied via HealthComponent.TakeDamage()
4. Death handling: Remove from turn queue + position service
5. Turn queue advances with weapon time cost
```

**Testing Scene**: TurnQueueTestScene.tscn - Player (100HP/melee), Goblin (30HP/melee), Orc (50HP/ranged)

**Archive**: [Completed_Backlog_2025-10_Part2.md](../../07-Archive/Completed_Backlog_2025-10_Part2.md#vs_020-basic-combat-system)

---

## Planned Features

### Enemy AI & Vision System (PLANNED)

**Status**: Proposed (Next Priority After VS_032) | **Priority**: Important (enables autonomous enemies, ambushes)

**What**: Enemy FOV calculation, asymmetric vision (enemies detect player first), basic AI decision-making (move toward player, attack when in range).

**Why**:
- **Ambush mechanics** - Enemies can detect player around corners, initiate combat first
- **Tactical depth** - Player must consider enemy patrol patterns and vision cones
- **AI foundation** - Enemies make autonomous decisions (move, attack, flee)
- **Reuses VS_007 pattern** - PlayerDetectionEventHandler mirrors EnemyDetectionEventHandler

**Scope** (high-level):
- Enemy perception attributes (vision radius, awareness zones)
- PlayerDetectionEventHandler (enemy sees player → schedule enemy in turn queue)
- Asymmetric combat (enemy detects first, player discovers on move)
- Basic AI decision tree (move toward player if not adjacent, attack if adjacent)
- Awareness zones (only nearby enemies calculate FOV for performance)

**Example Scenario** (Ambush):
```
1. Player auto-paths down hallway (exploration mode)
2. Orc around corner (15 tiles away) - passive, not calculating FOV
3. Player reaches 10 tiles from Orc → enters awareness zone
4. Orc FOV calculated → Player visible → ScheduleActorCommand(Orc)
5. Combat starts (queue = [Player, Orc]), auto-path cancels
6. Player doesn't see Orc yet (wall blocks player FOV)
7. Player moves 1 step → FOV updates → NOW sees Orc ("Orc ambushes you!")
8. Orc's turn → AI decides → Move toward player
```

**Dependencies**:
- **Prerequisite**: VS_007 ✅ (Turn Queue - COMPLETE)
- **Prerequisite**: VS_020 ✅ (Combat System - COMPLETE)
- **Recommended**: VS_032 (Equipment System - enemies need gear for varied combat capabilities)

**Blocks**: Nothing (combat works without AI, just no autonomous enemies)

**Product Owner Note**: Defer until VS_032 complete (enemies need equipment for interesting tactical decisions).

---

### Armor Systems (PLANNED)

**Status**: Proposed (After VS_032 Equipment) | **Priority**: Important (tactical depth)

**Vision**: Darklands-inspired armor simulation - layered armor with damage type interactions.

**Three-Phase Approach**:

#### Phase 1: Simplified Armor System (With VS_032)

**What**: Single armor value (protection + weight).

**Why**: Foundation for tactical combat depth, integrated with VS_032 Equipment.

**Scope**:
- Armor defense bonus (reduces damage)
- Weight affects movement and action time costs (encumbrance)
- Equipment slots provide armor (Torso, Head, Legs)

**Integration**: Part of VS_032 Equipment & Stats System (see [Roadmap_Stats_Progression.md](Roadmap_Stats_Progression.md)).

---

#### Phase 2: Damage Type System (PLANNED)

**What**: Slashing, Piercing, Blunt damage types with armor resistance.

**Why**: Weapon choice matters based on enemy armor type.

**Scope**:
- Weapon damage types (sword=slashing, spear=piercing, mace=blunt)
- Armor resistance percentages (mail resists slashing, weak to piercing)
- Tactical weapon selection (carry dagger AND mace for different enemies)

**Example**:
```
AGAINST CHAINMAIL ENEMY:
- Sword (slashing): 50% damage reduction (10 dmg → 5 dmg)
- Spear (piercing): 25% damage reduction (10 dmg → 7.5 dmg)
- Mace (blunt): 0% reduction (10 dmg → 10 dmg FULL)

TACTICAL DECISION: Switch to mace vs armored foes!
```

**Dependencies**: VS_032 ✅ (Equipment System - weapons need damage type property)

**Blocks**: Layered Armor (builds on damage type foundation)

---

#### Phase 3: Layered Armor System (PLANNED)

**What**: Two armor layers (gambeson + chainmail) with hit location and damage penetration.

**Why**: Deep tactical equipment decisions (Darklands realism).

**Scope**:
- Hit location system (head/torso/limbs have different coverage)
- Two armor layers per location (padding layer + mail layer)
- Damage penetration mechanics (blunt through mail, piercing penetrates layers)
- Individual layer durability (mail breaks, padding exposed)

**Example**:
```
TORSO PROTECTION:
Layer 1: Gambeson (5 defense, light)
Layer 2: Chainmail (15 defense, heavy)

ATTACK SEQUENCE:
1. Enemy sword attack (20 slashing damage)
2. Chainmail resists slashing (50% → 10 damage)
3. Gambeson absorbs 5 damage → 5 damage to health
4. Chainmail durability decreases (99/100 → 98/100)

GEAR DEGRADATION: After 50 hits, chainmail breaks → only gambeson protects!
```

**Dependencies**:
- **Prerequisite**: Simplified Armor System ✅ (VS_032)
- **Prerequisite**: Damage Type System ✅ (Phase 2)

**Product Owner Decision**: **Defer until Phase 2 validated** - Complexity may not be worth tactical depth gain. Playtest damage types first!

---

### Weapon Proficiency Tracking (PLANNED)

**Status**: Proposed (After VS_032) | **Priority**: Important (progression depth)

**What**: Track weapon usage and improve proficiency, reducing action time costs (Darklands + Mount & Blade pattern).

**Why**:
- **No-level progression** - Darklands philosophy (skills improve, not character level)
- **Time-unit synergy** - Proficiency makes actions FASTER (directly visible in turn queue!)
- **Specialization incentives** - Master daggers vs be generalist (build variety)
- **Learning by doing** - Play more, improve through repetition

**Scope** (high-level):
- WeaponProficiency entity (weapon type + skill 0-100)
- RecordWeaponUseCommand (track attacks, award XP)
- CalculateAttackTimeCostQuery (base time × proficiency modifier)
- Proficiency panel UI (progress bars per weapon type)

**Proficiency Formula** (tuned for 2-4 hour runs):
```
Base Time Cost = weapon base (dagger 50, sword 100, axe 150)
Proficiency Reduction = (skill / 100) × 0.30  // Max 30% reduction

Final Time Cost = Base × (1 - Proficiency Reduction)

PROGRESSION EXAMPLE (Dagger):
- Novice (skill 0): 50 units
- Expert (skill 50): 42.5 units (-15% faster)
- Master (skill 100): 35 units (-30% faster)
```

**Skill Gain**:
```
Gain per attack = 1 + (enemy_difficulty × 0.5)
- Rat: +1 skill
- Bandit: +1.5 skill
- Ogre: +2 skill

Time to mastery ≈ 50-70 attacks (5-10 fights per weapon type)
```

**Dependencies**:
- **Prerequisite**: VS_032 ✅ (Equipment System - need weapon types to track)
- **Integration**: Turn Queue (proficiency affects action time costs)

**Blocks**: Nothing (combat works without proficiency, just no progression)

**Product Owner Note**: High priority after VS_032 - directly enhances time-unit combat fun (visible improvement every 2-3 fights).

---

## Integration Points

**Turn Queue System** (VS_007):
- Combat mode detection (FOV events schedule enemies)
- Action time costs (movement, attacks, future: proficiency reduction)
- Natural exploration/combat transitions

**Stats & Equipment** (VS_032):
- Weapon damage from equipment (MainHand item)
- Armor defense from equipment (Torso + Head + Legs)
- Weight affects action time costs (encumbrance system)
- Strength affects damage multiplier

**FOV System** (VS_010):
- Combat entry (enemy detected → schedule in turn queue)
- Combat exit (no enemies visible → combat ends)
- Ranged attack validation (line-of-sight check)
- Enemy AI (vision cones, detection radius)

**Inventory System** (VS_018):
- Equipment slots (equip/unequip weapons/armor)
- Ground loot (enemy death → drop equipped items)
- Weapon switching (tactical loadout changes mid-combat)

---

## Next Steps

**Immediate Priority** (After VS_032 Equipment):
1. ⏳ **Enemy AI & Vision** (VS_011?) - Autonomous enemies, ambush mechanics
2. ⏳ **Weapon Proficiency** - Progression depth, time-unit synergy
3. ⏳ **Damage Type System** - Weapon choice matters (tactical depth)

**After Initial Systems Validated**:
- **Product Owner** decides: Layered Armor? Or sufficient depth already?
- **Test Specialist** validates: Is combat fun? Too complex? Too simple?
- **Tech Lead** reviews: Performance of AI FOV calculations?

**Future Work** (Deferred):
- Layered Armor System (ONLY IF damage types feel shallow after playtesting)
- Status Effects (poison, bleeding, buffs/debuffs)
- Advanced AI (flanking, kiting, cover usage, patrol patterns)
- Stamina System (sprint/dodge costs, regeneration)

**Product Owner Decisions Needed**:
- Approve VS_011 Enemy AI after VS_032?
- Priority: Proficiency OR Damage Types first?
- Defer Layered Armor indefinitely? Or revisit after Phase 2?

---

**Last Updated**: 2025-10-09 23:11
**Status**: VS_007 + VS_020 complete (basic combat working!), VS_011 ready to plan (all dependencies met)
**Owner**: Product Owner (roadmap maintenance), Tech Lead (future breakdowns), Dev Engineer (implementation)

---

*This roadmap provides high-level overview of Darklands combat systems. For technical implementation details, see VS item breakdowns in backlog after Tech Lead approval.*
