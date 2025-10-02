# Darklands Development Roadmap

**Purpose**: Sequence the complete [Vision.md](Vision.md) into actionable vertical slices with clear dependencies and milestones.

**Last Updated**: 2025-10-02 12:37 (Tech Lead: Enhanced VS_009 with JSON + Godot Editor Plugin architecture)
**Status**: Phase 1 In Progress (VS_001 Health + VS_005 Grid/FOV complete ✅, VS_006 Interactive Movement approved, VS_007 Smart Interruption planned, VS_008 Inventory complete ✅, VS_009 Item System planned)

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

### VS_005: Grid, FOV & Terrain System ✅ **COMPLETE**

**Status**: Complete (2025-10-01)
**Owner**: Dev Engineer (completed)
**Size**: M (completed in 1 day - all 4 phases)
**Priority**: Critical (tactical foundation for all combat)
**Depends On**: VS_001 (Health System - ✅ Complete)

**What**: Grid-based movement with custom shadowcasting FOV implementation (no external dependencies), terrain variety (wall/floor/smoke), event-driven Godot integration

**Why**:
- Positioning creates tactical depth (cover, line-of-sight, ambush mechanics)
- FOV is table-stakes for roguelike feel
- Terrain variety enables strategic choices (hide in smoke, wall cover)
- Dummy enemy validates mechanics before AI complexity
- Foundation for all future combat features

**Original Plan vs. Actual Implementation**:

*See [Completed_Backlog_2025-10.md](../../07-Archive/Completed_Backlog_2025-10.md#vs_005-grid-fov--terrain-system) for detailed comparison of planned vs. actual approach.*

**Key Implementation Differences**:
- ✅ **Custom shadowcasting** (not GoRogue NuGet) - Full ownership, no external dependencies
- ✅ **ColorRect grid** (not TileMap/Kenney tileset) - Simpler, self-contained, perfect alignment
- ✅ **Pure event-driven architecture** - Zero polling, `ActorMovedEvent` + `FOVCalculatedEvent` drive all updates
- ✅ **Complete event pattern** - Events contain both old and new positions (eliminates state duplication in Presentation)

**What Was Delivered** (All 4 Phases Complete):

**Phase 1 - Domain** (41 tests):
- `Position`, `TerrainType` enum (Wall/Floor/Smoke), `GridMap` (30×30)
- Railway-oriented design: `IsPassable()` / `IsOpaque()` use functional composition

**Phase 2 - Application** (28 tests):
- Commands: `MoveActorCommand`, `RegisterActorCommand`, `SetTerrainCommand`
- Queries: `CalculateFOVQuery`, `GetVisibleActorsQuery`, `GetActorPositionQuery`
- Services: `IFOVService`, `IActorPositionService` abstractions

**Phase 3 - Infrastructure** (9 tests):
- `ShadowcastingFOVService` (~220 LOC) - Custom 8-octant recursive shadowcasting
- Referenced libtcod C + GoRogue C# implementations (no NuGet dependency)
- Performance: <10ms for 30×30 grid with obstacles

**Phase 4 - Presentation** (Manual testing):
- `GridTestSceneController.cs` (370 LOC) - Pure reactive, event-driven
- 900 ColorRect nodes (30×30 × 2 layers: terrain + FOV overlay)
- Fog of war: 3-state system (unexplored/explored/visible)
- Controls: Arrow keys (player), WASD (dummy), Tab (switch FOV view)

**Test Coverage**: 189 tests passing, 54ms execution time

**Architecture Highlights**:
- Zero Godot dependencies in Core (ADR-002 compliant)
- Events contain complete facts: `ActorMovedEvent(ActorId, OldPosition, NewPosition)`
- ServiceLocator used ONLY in `_Ready()` (Godot → DI bridge)
- Clean Architecture: Presentation queries Core, no state duplication

**Full Details**: [Completed_Backlog_2025-10.md](../../07-Archive/Completed_Backlog_2025-10.md#vs_005-grid-fov--terrain-system) (lines 541-739)

---

### VS_006: Interactive Movement System ⭐ **APPROVED**

**Status**: Approved (Tech Lead breakdown complete)
**Owner**: Tech Lead → Dev Engineer (for implementation)
**Size**: L (1.5-2 days, 12.5h)
**Priority**: Critical (core gameplay mechanic)
**Depends On**: VS_005 (Grid + FOV - ✅ Complete)

**What**: Point-and-click movement with A* pathfinding (8-directional), visual path preview, tile-to-tile animation, and right-click cancellation

**Why**:
- Natural interaction model (click where you want to go vs. mashing arrow keys)
- Tactical clarity (see path before committing)
- User control (right-click to cancel long paths)
- Foundation for all future targeting/interaction features

**Key Features**:
- ✅ 8-directional pathfinding (matches roguelike standard: NetHack, DCSS, Cogmind, Caves of Qud)
- ✅ A* algorithm with Chebyshev heuristic (diagonal cost = 1.0 per Caves of Qud)
- ✅ Click → preview path → click to confirm → animated movement
- ✅ **Manual cancellation via right-click** (CancellationToken pattern)
- ❌ Auto-interruption (enemy spotted, trap discovered) - deferred to VS_007

**Scope**: See detailed specification in [Backlog.md](../../01-Active/Backlog.md#vs_006)

---

### VS_007: Smart Movement Interruption ⭐ **PLANNED**

**Status**: Proposed (depends on VS_006 completion)
**Owner**: Product Owner → Tech Lead (for breakdown)
**Size**: M (4-6h)
**Priority**: Important (UX polish for core mechanic)
**Depends On**: VS_006 (Interactive Movement - manual cancellation foundation)

**What**: Auto-interrupt movement when tactical situations change (enemy spotted in FOV, trap/loot discovered, dangerous terrain)

**Why**:
- **Safety**: Prevent walking into danger (enemy appears → stop immediately)
- **Discovery**: Don't walk past important items (loot, traps require investigation)
- **Roguelike Standard**: NetHack, DCSS, Cogmind all auto-stop on enemy detection
- **Tactical Awareness**: Game alerts player to changing battlefield conditions

**How** (4-Phase Implementation):
- **Phase 1 (Domain)**: Minimal (reuse existing Position, ActorId)
- **Phase 2 (Application)**: `IMovementStateService` to track active movements, `InterruptMovementCommand`
- **Phase 3 (Infrastructure)**: Movement state tracking (in-memory), interruption policy engine
- **Phase 4 (Presentation)**:
  - Subscribe to `FOVCalculatedEvent` → detect new enemies → trigger interruption
  - UI prompt: "Enemy spotted! Continue moving? [Y/N]" (optional auto-resume)
  - Animation cleanup: Stop Tween gracefully when interrupted

**Interruption Triggers**:
1. **Enemy Detection** (Critical): New enemy appears in FOV → pause movement, alert player
2. **Discovery Events** (Important): Step on tile reveals loot/trap → pause for investigation
3. **Dangerous Terrain** (Future): About to enter fire/acid → confirm before proceeding

**Scope**:
- ✅ Auto-pause when enemy enters FOV during movement
- ✅ UI confirmation prompt ("Continue? [Y/N]")
- ✅ Clean animation stop (no mid-tile glitches)
- ✅ Movement state service tracks active paths
- ❌ Memory of "last seen enemy position" (AI feature, not movement)
- ❌ Configurable interruption settings (add in settings VS later)

**Done When**:
- ✅ Walking across map → enemy appears in FOV → movement stops automatically
- ✅ Prompt appears: "Goblin spotted! Continue moving? [Y/N]"
- ✅ Player presses Y → resumes path, N → cancels remaining movement
- ✅ Animation stops cleanly at current tile (no visual glitches)
- ✅ Manual test: Walk toward hidden enemy behind smoke → movement stops when smoke clears and enemy visible
- ✅ Code review: FOVCalculatedEvent subscriber triggers interruption (event-driven, no polling)

**Architecture Integration**:
- Builds on VS_006's `CancellationToken` foundation (manual cancel becomes "interruption trigger")
- `MoveAlongPathCommand` already respects cancellation → just need external trigger
- Event-driven: `FOVCalculatedEvent` → Check for new enemies → Call `InterruptMovementCommand`

**Phase**: All 4 phases (Domain minimal, Application + Infrastructure core, Presentation UI prompts)

---

### VS_008: Slot-Based Inventory System (MVP) ⭐ **PLANNED**

**Status**: Tech Lead Review Complete (Awaiting Product Owner Approval)
**Owner**: Tech Lead → Dev Engineer (after approval)
**Size**: M (5-6.5 hours across 4 phases)
**Priority**: Important (Core mechanic, parallel with movement)
**Depends On**: None (ActorId already exists in Domain/Common)

**What**: Slot-based inventory (20-slot backpack) for player-controlled actors with add/remove operations, capacity enforcement, and basic UI panel

**Why**:
- Core mechanic for loot management (foundational for roguelikes)
- Zero conflicts with VS_006/007 (parallel development approved)
- MVP philosophy: Slot-based first, spatial grid deferred until proven needed
- Player-only system: NPCs/enemies use equipment slots (separate future VS)

**How** (4-Phase Implementation):
- **Phase 1 (Domain)**: `Inventory` entity (stores `List<ItemId>`), `ItemId` primitive added to Domain/Common
- **Phase 2 (Application)**: `CreateInventoryCommand`, `AddItemCommand`, `RemoveItemCommand`, `GetInventoryQuery` with DTOs
- **Phase 3 (Infrastructure)**: `InMemoryInventoryRepository` (explicit creation via CreateInventoryCommand)
- **Phase 4 (Presentation)**: `InventoryPanelNode` (Godot UI with 20 slot visuals, test buttons)

**Key Architectural Decisions**:
1. **Inventory stores ItemIds** (not Item objects)
   - Enables clean separation: Inventory = container logic, Item = content definition (future VS_009)
   - Parallel development: Item feature can evolve independently
   - Testability: No mocks needed, just `ItemId.NewId()`

2. **Player-controlled actors only** (explicit creation pattern)
   - Player and party members get inventories via `CreateInventoryCommand`
   - NPCs/enemies use equipment slots only (wielded weapon, worn armor) - separate system
   - Loot drops are ground items (ItemId at Position) - separate system
   - Prevents memory waste (no backpacks for 100 enemies)

**Scope**:
- ✅ Explicit inventory creation for player-controlled actors
- ✅ Add/remove items with capacity constraint (20 slots default)
- ✅ Query inventory contents (returns list of ItemIds)
- ✅ UI panel displays slots, capacity label, add/remove test buttons
- ❌ Item definitions (name, sprite, properties) - Deferred to VS_009
- ❌ Spatial grid (tetris placement) - Deferred to VS_017 (if playtesting shows need)
- ❌ Equipment slots system (NPC gear, player worn items) - Separate future VS
- ❌ Ground loot system (items at Position) - Separate future VS
- ❌ Save/load persistence - Deferred to separate Save System VS

**Done When**:
- ✅ Unit tests: 20 tests passing (10 domain, 6 application, 4 infrastructure) <100ms
- ✅ Architecture tests pass (zero Godot dependencies in Darklands.Core)
- ✅ Manual UI test: Add 20 items → All slots filled → Button disables → Error on 21st item
- ✅ Result<T> error handling with descriptive messages ("Inventory is full")

**Full Specification**: See [VS_008_Inventory_Spec.md](../../01-Active/VS_008_Inventory_Spec.md) for detailed implementation guide

---

### Future: Basic Combat System (Turn-Based)

**Status**: Not Yet Planned
**Depends On**: VS_007 (Smart Interruption) OR VS_006 if auto-interruption deferred

**What**: Simple turn-based combat with manually controlled dummy enemy

**Why**:
- Validate positioning tactics before adding AI complexity
- Test FOV integration (ranged attacks require line-of-sight)
- Foundation for AI behavior

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

---

### Future: Tileset & Procedural Generation System

**Status**: Not Yet Planned (Ideas - Visual Polish)
**Depends On**: None (independent of VS_006)

**What**: Replace ColorRect grid with Godot TileMap + Kenney assets, add procedural map generation and autotiling

**Why**:
- Professional appearance (pixel art vs. colored rectangles)
- Visual variety (procedurally generated maps)
- Replayability (different map layouts each time)

**Scope**:
- Kenney Micro Roguelike tileset (8x8 sprites, CC0 license)
- PCG algorithm (BSP trees, cellular automata, or dungeon generation)
- Autotiling rules for seamless wall connections
- GridMap remains single source of truth (Presentation reads from Core)

**Notes**: ColorRect is currently functional - this is polish work that can be deferred until gameplay systems are mature.

---

### Future: Turn-Based AI

**Status**: Not Yet Planned
**Depends On**: Basic Combat System

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

---

### Future: Time-Unit Queue Refactoring

**Status**: Not Yet Planned
**Depends On**: Turn-Based AI

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
- Keep turn-based version in git branch
- If time-unit doesn't add value → revert and simplify
- Only proceed to proficiency if time-unit proves fun

---

### Future: Weapon Proficiency Tracking

**Status**: Not Yet Planned
**Depends On**: Time-Unit Queue (if that system is kept)

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

---

### Future: Arena Combat Integration (Phase 1 Complete)

**Status**: Not Yet Planned
**Depends On**: Combat systems + AI + (optionally) Proficiency

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

**VS_009: Item Definition System (JSON + Editor Plugin)**
- **Core Architecture**: JSON-based item definitions (data/items/*.json)
  - Item properties: name, type, sprite_path, sprite_index, width, height, weight, is_stackable, max_stack_size
  - JsonItemRepository loads JSON directly (zero Godot dependency in Core)
  - Foundation for VS_010 (stacking), VS_011 (equipment), VS_018 (spatial grid shapes)
- **Designer Experience**: Godot Editor Plugin (addons/item_editor/)
  - Custom Inspector for .json files (Odin Inspector-style UX)
  - Visual property editors: Enum dropdown (ItemType), Sprite picker grid, Int sliders (width/height)
  - Conditional visibility: MaxStackSize field appears only when IsStackable = true
  - Validation on save: Width 1-3, Weight > 0, Name non-empty (shows inline errors)
  - Live preview: Renders item sprite at actual size (2×1 sword vs 1×1 potion)
  - Plugin is **optional** - JSON still editable in VS Code with schema autocomplete
- **Incremental Approach**:
  - Phase 1: InMemoryItemRepository (8 hardcoded test items from inventory_ref spritesheet)
  - Phase 2: JsonItemRepository (when >20 items, edit JSON in VS Code)
  - Phase 3: Build Editor Plugin (when designers request visual editor, ~3-4h)
- **Architecture Benefits**:
  - ✅ Single source of truth (JSON files, no .tres conversion)
  - ✅ ADR-002 compliant (Core has zero Godot dependencies)
  - ✅ Version control friendly (text diffs, readable JSON changes)
  - ✅ Modding support (players can add items via JSON)
  - ✅ External tools compatible (Python scripts, web editors, spreadsheets)
- **Reference**: NeoScavenger itemtypes.xml (stack limits), Unity Odin Inspector (custom drawers)

**VS_010: Item Stacking System**
- Stackable item support (e.g., "5× Branch", "20× Arrow")
- Stack limits per item type (configurable in item definitions)
- UI displays stack count overlay on slot visuals
- Commands: AddItemToStack, SplitStack, MergeStacks
- Benefits: Reduces inventory clutter for consumables and ammo
- Reference: NeoScavenger nStackLimit (branches=5, ammo=20, unique items=1)

**VS_011: Equipment Slots System**
- Actor equipment slots (main hand, off hand, head, torso, legs, ring×2)
- Used by ALL actors (player, NPCs, enemies) - not just player-controlled
- Player: Equips items from inventory → equipment slots (affects stats)
- NPCs/Enemies: Spawned with pre-equipped gear (defines combat capabilities)
- Commands: EquipItem, UnequipItem, GetEquippedItems
- Integration: Combat system reads equipment for damage/defense calculations
- Reference: NeoScavenger battlemoves.xml (equipment affects available moves)
- Architecture: Separate from VS_008 Inventory (equipment = worn, inventory = carried)

**VS_012: Ground Loot System**
- Items at map positions (dungeon loot, enemy drops, player discards)
- WorldItemRepository: Dictionary<Position, List<ItemId>>
- Commands: DropItemAtPosition, PickupItemAtPosition, GetItemsAtPosition
- Integration: Player picks up loot → adds to inventory (VS_008)
- Integration: Enemy dies → drops equipped items (VS_011) to ground
- UI: Visual indicators on tiles with loot (sparkle effect, item sprite)
- Foundation for dungeon generation loot placement

**VS_013: Container System (Nested Inventories)**
- Items can BE containers (bags, pouches, quivers)
- Nested spatial grids: Bag (4×6) can contain Pouch (2×2)
- Container properties: capacity grid size, allowed item types
- Type filtering: Quiver accepts arrows only, waterskin accepts liquids only
- Commands: OpenContainer, MoveItemToContainer
- Benefits: Organization (ammo in quiver), specialization (waterproof bags)
- Reference: NeoScavenger aCapacities ("4x6"), aContentIDs (whitelisted types)
- Decision point: May defer if VS_010 stacking reduces inventory clutter sufficiently

**VS_014: Simplified Armor System**
- Single armor layer (protection + weight values)
- Weight affects movement and action time costs
- Foundation for Phase 2 layered armor

**VS_015: Damage Type System**
- Slashing, Piercing, Blunt damage types
- Armor resistance to damage types
- Weapon choice matters based on enemy armor

**VS_016: Layered Armor (Padding + Mail)**
- Two armor layers (gambeson + chainmail)
- Hit location system (head/torso/limbs)
- Damage penetration mechanics

**VS_017: Unique Weapons Collection**
- 10 unique weapons with build-enabling properties
- Angband-style itemization (situational value)
- Constrained randomization (minor stat variance)

**VS_018: Tetris Inventory Upgrade (Optional)**
- Spatial grid inventory (item shapes matter)
- Upgrade from VS_008 slot-based system IF playtesting shows demand
- Item rotation support (2×1 sword can be stored as 1×2)
- Reference: Diablo 2, Resident Evil spatial puzzles
- Decision point: Only implement if players request spatial challenge

**VS_019: Crafting System (NeoScavenger-Style)**
- Grid-based crafting interface (drag items to recipe slots)
- Tool vs. Consumed separation (lighter stays, tinder burns)
- Time-based crafting (recipes take hours, not instant)
- Recipe discovery (try combinations, learn new recipes)
- Reference: NeoScavenger recipes.xml (tool/consumed/destroyed separation)
- Benefits: Survival depth (repair gear, purify water, cook food)

**Integration Milestone**: Phase 2 complete when 3+ viable builds emerge from playtesting

---

## 🗺️ Phase 3: Strategic Layer (Future)

**Goal**: Demonstrate macro/micro loop integration works

**When to Start**: After Phase 2 builds are balanced and fun

**Estimated Duration**: 4-6 months

### Planned Vertical Slices (High-Level)

**VS_020: Overworld Map**
- Simple node-based map (3 towns, 2 dungeons)
- Travel system with time passage
- Location-based encounters

**VS_021: Quest System**
- Basic contracts (clear dungeon, deliver item)
- Quest giver NPCs in towns
- Rewards (money, reputation)

**VS_022: Economy System**
- Money from quests and loot
- Equipment purchases in towns
- Repair costs for gear degradation

**VS_023: Simple Reputation System**
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

**VS_024: Origins System**
- 3-5 starting backgrounds (noble, soldier, peasant, scholar, merchant)
- Affects starting proficiencies, equipment, faction standings
- Defines early viable strategies

**VS_025: Event System**
- Random encounters with branching choices
- Consequences affect character state and reputation
- RimWorld-style procedural storytelling

**VS_026: Full Reputation System (N×N Matrix)**
- Origin-specific quest lines unlock
- Faction relationships create emergent conflicts (enemy of my enemy)
- Dynamic world reacts to player choices
- Reference: NeoScavenger factions.xml (cross-faction reputation matrix)

**VS_027: Character Aging & Time Pressure**
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

**Phase 2** (Itemization & Systems):
- 11 vertical slices × 1-3 days each = 24-38 dev days
- + Balance testing, build diversity, crafting recipes = 10-15 additional days
- **Total: 5-6 months calendar time**

**Phase 3** (Strategic):
- 4 vertical slices × 2-4 days each = 15-30 dev days
- + World integration, economy tuning = 10-15 additional days
- **Total: 4-6 months calendar time**

**Phase 4** (Narrative):
- 4 vertical slices × 2-3 days each = 12-20 dev days
- + Event writing, faction balance = 8-12 additional days
- **Total: 3-4 months calendar time**

**Full Roadmap: 15-23 months to first complete game** (27+ vertical slices)

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

*This roadmap sequences the complete [Vision.md](Vision.md) into 27+ vertical slices across 4 phases, balancing ambition with pragmatic incremental delivery. Each phase proves a core hypothesis before adding complexity.*
