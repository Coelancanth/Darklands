# Darklands Development Roadmap

**Purpose**: Organize all Darklands features by category for quick reference and planning.

**Last Updated**: 2025-10-08 (VS_025 Temperature System Complete - WorldGen Stage 2)

---

## Quick Navigation

**Tactical Layer (Local Map - Grid Combat):**
- [Core Systems](#core-systems) - Health, i18n, Data-Driven Infrastructure
- [Grid & Vision](#grid--vision) - FOV, Terrain, Fog of War
- [Movement](#movement) - Pathfinding, Click-to-Move
- [Combat](#combat) - Turn Queue, Attacks, AI, Armor
- [Inventory & Items](#inventory--items) - Spatial Grid, Equipment, Crafting
- [Visual & Rendering](#visual--rendering) - Camera, TileSet, Animations

**Strategic Layer (World Map - Macro Scale):**
- [World Generation](#world-generation) - Plate Tectonics, Elevation, Temperature, Climate
- [Strategic Map & History](#strategic-map--history) - WorldEngine, Factions, Civilization History

**Meta Systems:**
- [Progression](#progression) - Proficiency, Aging
- [Economy & Quests](#economy--quests) - Trading, Quest System
- [Narrative](#narrative) - Origins, Events, Reputation
- [Development Info](#development-info) - Estimates, Milestones, Risks

---

## Vision Overview

**Vision**: Darklands (1992) spiritual successor with time-unit combat, skill progression, and emergent narrative

**Roadmap Philosophy**:
1. **Prove the core is fun first** - Time-unit combat must create tactical depth
2. **Incremental complexity** - Add systems only when foundation is solid
3. **Shippable at every phase** - Each phase delivers playable value
4. **Learn before scaling** - Small vertical slices reveal design problems early

**Development Approach**: 4 development phases over 13-20 months
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

## Core Systems

### VS_001: Health System (COMPLETE)

**Status**: Complete | **Tests**: Foundational system
**Owner**: Dev Engineer

**Delivered**: Health value object, TakeDamageCommand, death detection

**Archive**: Earlier archive file

---

### VS_021: i18n + Data-Driven Entity Infrastructure (COMPLETE)

**Status**: Complete (2025-10-06) | **Size**: L (2-3 days, all 5 phases) | **Tests**: 415 passing
**Owner**: Dev Engineer

**Delivered**: Translation system (18 keys in en.csv), ActorTemplate system with GodotTemplateService, player.tres template, pre-push validation script, architecture fix (templates moved to Presentation layer for Godot API access)

**What Was Built**:
- **ADR-005**: Internationalization Architecture (translation key discipline, tr() in Presentation, keys in Domain)
- **ADR-006**: Data-Driven Entity Design (Godot Resources for templates, three execution phases: design-time/startup/runtime)
- **Phase 1**: Translation infrastructure (18 keys: ACTOR_*, UI_*, ERROR_*), en.csv file, project.godot configuration
- **Phase 2**: ActorTemplate + GodotTemplateService (startup template loading, validation, caching)
- **Phase 3**: Actor name logging enhancement (IPlayerContext integration, ActorIdLoggingExtensions uses repository)
- **Phase 4**: Pre-push validation script (checks translation keys exist, prevents broken i18n)
- **Phase 5**: Architecture fix (moved templates to Presentation layer - correct ADR-002 compliance)

**Key Architecture Decisions**:
- **Translation Key Discipline**: Domain returns keys ("ACTOR_GOBLIN"), Presentation translates via tr()
- **Three Execution Phases**: Design-time (designer creates .tres) → Startup (load templates) → Runtime (create entities)
- **Template ≠ Entity**: Templates are cookie cutters (Infrastructure), entities are cookies (Domain, no template dependency)
- **Hot-Reload Works**: Edit .tres → save → test in <5 seconds (designer empowerment)

**Integration**:
- All actor creation now uses ActorFactory.CreateFromTemplate() with .tres files
- Logging shows: "8c2de643 [type: Enemy, name: ACTOR_GOBLIN]" (fully translated context)
- Validation prevents broken keys at pre-push (fail-fast on missing translations)

**Archive**: [Completed_Backlog_2025-10_Part2.md](../../07-Archive/Completed_Backlog_2025-10_Part2.md) (search "VS_021")

---

## Grid & Vision

### VS_005: Grid, FOV & Terrain System (COMPLETE)

**Status**: Complete (2025-10-01) | **Size**: M (1 day, all 4 phases) | **Tests**: 189 passing
**Owner**: Dev Engineer

**Delivered**: 30×30 grid with custom shadowcasting FOV (no external dependencies), terrain types (wall/floor/smoke), fog of war system, event-driven architecture with `ActorMovedEvent` + `FOVCalculatedEvent`, ColorRect rendering

**Architecture**: Zero Godot dependencies in Core, pure functional domain logic, ServiceLocator only in `_Ready()`

**Archive**: [Completed_Backlog_2025-10_Part1.md](../../07-Archive/Completed_Backlog_2025-10_Part1.md#vs_005-grid-fov--terrain-system) (lines 541-739)

---

## Movement

### VS_006: Interactive Movement System (COMPLETE)

**Status**: Complete (2025-10-01) | **Size**: L (1.5 days, all 4 phases) | **Tests**: 215 passing
**Owner**: Dev Engineer

**Delivered**: A* pathfinding (8-directional, Chebyshev heuristic), hover-based path preview, click-to-move with Tween animation, right-click cancellation (CancellationToken pattern), fog of war integration, ILogger refactoring from LoggingService

**Architecture**: Pathfinding service in Infrastructure, command handlers use async/await, ServiceLocator only in `_Ready()`

**Archive**: [Completed_Backlog_2025-10_Part1.md](../../07-Archive/Completed_Backlog_2025-10_Part1.md#vs_006-interactive-movement-system) (lines 740+)

---

## Combat

### VS_007: Time-Unit Turn Queue System (COMPLETE)

**Status**: Complete (2025-10-04) | **Size**: L (3 days, all 4 phases) | **Tests**: 359 passing (49 new)
**Owner**: Dev Engineer

**Delivered**: Time-unit combat system with natural exploration/combat mode detection via turn queue size, FOV-based symmetric enter/exit transitions, movement costs (10 units/move), production-ready logging with Gruvbox semantic highlighting

**What Was Built**:
- **Phase 1 (Domain)**: `TimeUnits` value object, `TurnQueue` aggregate with priority queue + player-first tie-breaking, `ScheduledActor` record
- **Phase 2 (Application)**: `ScheduleActorCommand`, `RemoveActorFromQueueCommand`, `IsInCombatQuery`, `IsActorScheduledQuery`, `EnemyDetectionEventHandler` (FOV→schedule enemies), `CombatEndDetectionEventHandler` (FOV cleared→exit combat)
- **Phase 3 (Infrastructure)**: `InMemoryTurnQueueRepository`, `PlayerContext` service, DI registration in GameStrapper
- **Phase 4 (Presentation)**: Input routing (combat=single-step, exploration=auto-path), test scene with player/enemies, bug fixes (async race conditions, path preview)

**Key Design Decisions**:
- Turn queue size = combat state (no separate state machine)
- Relative time model (resets to 0 per combat session)
- Player permanently in queue (never fully removed)
- FOV events drive both combat enter AND exit (symmetric pattern)
- Movement costs: 10 units in combat, instant in exploration
- Player-centric FOV only (enemies don't detect player—deferred to VS_011)

**Example Scenario** (Exploration → Combat → Reinforcement → Victory):
```
1. Player clicks distant tile → auto-path starts (exploration mode, time=N/A)
2. Step 3: Goblin appears in FOV → auto-path cancelled (combat starts, time=0)
   Queue: [Player@0, Goblin@0] → Player acts first (tie-breaking)
3. Player moves 1 step (costs 100) → Player@100, Goblin@0
4. Goblin attacks (costs 150) → Goblin@150, Player@100
5. Player moves → Orc appears in FOV (reinforcement!)
   Queue: [Orc@100, Player@200, Goblin@150] → Orc acts next (just appeared)
6. Orc attacks (costs 150) → Orc@250
7. Player defeats Goblin → removed from queue
8. Player defeats Orc → queue=[Player] → combat ends (time resets)
9. Next click resumes auto-path (exploration mode)
```

**Done When**: Scenario above works end-to-end. Zero changes to existing handlers.

---

### VS_020: Basic Combat System (Attacks & Damage) (COMPLETE)

**Status**: Complete (2025-10-06) | **Size**: M (1-2 days, all 4 phases) | **Tests**: 428 passing
**Owner**: Dev Engineer

**Delivered**: Click-to-attack combat UI, component pattern (Actor + HealthComponent + WeaponComponent), ExecuteAttackCommand with range validation (melee adjacent, ranged line-of-sight), damage application via HealthComponent, death handling (actors removed from turn queue AND position service), tactical positioning matters for range/FOV

**What Was Built**:
- **Phase 0**: Component Infrastructure (IComponent, Actor container, IHealthComponent, IWeaponComponent, ActorRepository, ActorFactory with template integration)
- **Phase 1**: Weapon value object (damage, time cost, range, weapon type)
- **Phase 2**: ExecuteAttackCommand with range validation, damage application, death handling, turn queue integration
- **Phase 3**: Line-of-sight validation for ranged attacks (FOV integration), melee bypasses FOV
- **Phase 4**: Click-to-attack UI, test scene with player/enemies, visual feedback, death handling bug fix (RemoveActor() from position service)

**Key Architecture Decisions**:
- Component pattern chosen over simple entity (scales to 50+ actor types, reuse across player/enemies/NPCs)
- ActorTemplate integration (ADR-006 compliance - designers configure components in .tres files)
- Tactical combat: Position matters (melee=adjacent, ranged=line-of-sight), walls block ranged attacks

**Combat Flow**:
1. Player clicks enemy in range → ExecuteAttackCommand
2. Range validation: Melee (Chebyshev distance ≤ 1), Ranged (distance check + FOV line-of-sight)
3. Damage applied via HealthComponent.TakeDamage()
4. Death handling: Remove from turn queue + position service
5. Turn queue advances with weapon time cost

**Testing**: TurnQueueTestScene.tscn - Click-to-attack combat with Player (100HP/melee), Goblin (30HP/melee), Orc (50HP/ranged)

**Archive**: [Completed_Backlog_2025-10_Part2.md](../../07-Archive/Completed_Backlog_2025-10_Part2.md#vs_020-basic-combat-system) (lines 1148-1297)

---

### Enemy AI & Vision System (PLANNED - Next Priority)

**Status**: Ready to Plan (all dependencies complete)
**Owner**: Product Owner → Tech Lead (for breakdown)
**Size**: L (2-3 days, all 4 phases)
**Priority**: Important (enables asymmetric combat, ambushes, autonomous enemies)
**Depends On**: VS_007 (Turn Queue - COMPLETE), VS_020 (Combat System - COMPLETE)

**What**: Enemy FOV calculation, asymmetric vision (enemy sees you before you see them), basic AI decision-making (move toward player, attack when in range)

**Why**:
- **Ambush mechanics**: Enemies can detect player around corners, initiate combat first
- **Tactical depth**: Player must consider enemy patrol patterns and vision cones
- **AI foundation**: Enemies make autonomous decisions (move, attack, flee)
- **Reuses VS_007**: Parallel detection (PlayerDetectionEventHandler mirrors EnemyDetectionEventHandler)

**How**:
- **Phase 1**: Enemy perception attributes (vision radius, awareness zones)
- **Phase 2**: `PlayerDetectionEventHandler` (subscribes to enemy FOV events), `DecideEnemyActionQuery` (AI decision tree)
- **Phase 3**: Enemy FOV calculation (triggered by awareness zones), AI behavior states (passive, alerted, combat)
- **Phase 4**: Enemy activation zones (distance-based), enemy turn execution (move toward player OR attack)

**Scope**:
- YES: Enemy FOV calculation (when within awareness radius of player)
- YES: PlayerDetectionEventHandler (enemy sees player → schedule enemy)
- YES: Asymmetric combat (enemy detects first, player discovers on move)
- YES: Basic AI (move toward player if not adjacent, attack if adjacent)
- YES: Awareness zones (only nearby enemies calculate FOV for performance)
- NO: Advanced AI (flanking, kiting, cover usage - future)
- NO: Behavior trees (simple decision tree for MVP)
- NO: Patrol patterns (enemies stationary until activated)

**Example Scenario** (Ambush):
```
1. Player auto-paths down hallway (exploration mode)
2. Orc around corner (15 tiles away) - passive, not calculating FOV
3. Player reaches 10 tiles from Orc → enters awareness zone
4. Orc FOV calculated → Player visible → ScheduleActorCommand(Orc)
5. Combat starts (queue = [Player, Orc]), auto-path cancels
6. Player doesn't see Orc yet (wall blocks player FOV)
7. Player moves 1 step → FOV updates → NOW sees Orc ("Orc ambushes you!")
8. Orc's turn → DecideEnemyActionQuery → Move toward player
```

**Done When**: Scenario above works. Enemies detect player independently. Basic AI (approach + attack). Zero refactoring of VS_007 turn queue (event-driven addition).

---

### Simplified Armor System (PLANNED)

**What**: Single armor layer (protection + weight values)

**Why**: Foundation for tactical combat depth

**Scope**:
- Weight affects movement and action time costs
- Foundation for future layered armor system

---

### Damage Type System (PLANNED)

**What**: Slashing, Piercing, Blunt damage types

**Why**: Weapon choice matters based on enemy armor

**Scope**:
- Armor resistance to damage types
- Creates tactical weapon selection decisions

---

### Layered Armor System (PLANNED)

**What**: Two armor layers (gambeson + chainmail)

**Why**: Deep tactical equipment decisions

**Scope**:
- Hit location system (head/torso/limbs)
- Damage penetration mechanics
- Depends on: Simplified Armor System, Damage Type System

---

### Weapon Proficiency Tracking (PLANNED)

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
- Unit tests: Attack with dagger → skill increases, time cost reduces
- Integration tests: 50 sword attacks → 15% faster
- Manual playtest: Progression feels rewarding (visible every 2-3 fights)

---

## Inventory & Items

### VS_008: Slot-Based Inventory System (MVP) (COMPLETE)

**Status**: Complete (2025-10-02) | **Size**: M (5-6.5h, all 4 phases) | **Tests**: 23 passing
**Owner**: Dev Engineer

**Delivered**: Slot-based inventory (20 slots), `ItemId` primitive in Domain/Common, add/remove operations with capacity enforcement, UI panel with GridContainer (10×2 slots), query-based refresh (no events in MVP)

**Key Decision**: Inventory stores `ItemId` (not Item objects) - enables clean separation between container logic and item definitions (VS_009)

**Architecture**: Explicit creation pattern (`CreateInventoryCommand`), player-controlled actors only, InMemoryInventoryRepository with auto-creation, ServiceLocator only in `_Ready()`

**Archive**: [Completed_Backlog_2025-10.md](../../07-Archive/Completed_Backlog_2025-10.md#vs_008-slot-based-inventory-system) (lines 47-163)

---

### VS_009: Item Definition System (TileSet Metadata-Driven) (COMPLETE)

**Status**: Complete (2025-10-02) | **Size**: M (6-7h, all 4 phases) | **Tests**: 57 passing
**Owner**: Dev Engineer

**Delivered**: Item catalog using Godot TileSet custom data layers (item_name, item_type, max_stack_size), `TileSetItemRepository` auto-discovers items at startup, `ItemSpriteNode` (TextureRect) renders sprites with KeepAspectCentered, showcase scene displays 10 items with metadata

**Key Decision**: TileSet metadata (not JSON/C# definitions) - designers add items visually in Godot editor with zero code changes, single source of truth for sprites + properties

**Architecture**: Domain stores primitives (atlas coords), Infrastructure reads TileSet custom data layers, Core has zero Godot dependencies (ADR-002)

**Archive**: [Completed_Backlog_2025-10.md](../../07-Archive/Completed_Backlog_2025-10.md#vs_009-item-definition-system) (lines 166-312)

---

### VS_018: Spatial Inventory System (COMPLETE)

**Status**: Phase 1 Complete (2025-10-04) | **Size**: XL (ongoing) | **Tests**: 359 passing
**Owner**: Dev Engineer

**Phase 1 Delivered** (TD_003, TD_004):
- Tetris-style spatial inventory with **drag-drop** interaction
- Multi-cell items with L/T-shape support (coordinate-based masks)
- 90° rotation during drag (scroll wheel)
- Type filtering (equipment slots reject wrong item types)
- Component separation: `EquipmentSlotNode` (swap-focused) vs `InventoryContainerNode` (Tetris grid)
- TileSet-driven ItemShape encoding ("custom:0,0;1,0;1,1")
- Cell-by-cell collision detection (supports complex shapes)

**Architecture Achievements**:
- ItemShape value object with coordinate masks
- InventoryRenderHelper (DRY rendering across components)
- Zero business logic in Presentation (SSOT in Core)
- All 359 tests GREEN

**Archive**: [Completed_Backlog_2025-10.md](../../07-Archive/Completed_Backlog_2025-10.md#vs_018-spatial-inventory-l-shapes) (Phase 1 documentation)

**Phase 2 TODO - UX & Interaction Improvements**:

1. **Click-to-Pick Interaction** (replace drag-drop)
   - Click item → pick up (highlight follows cursor)
   - Click destination → drop item
   - More tactile than drag-drop, easier on trackpads

2. **1v1 Swap Support**
   - Click occupied slot with picked item → swap items (no intermediate drop needed)
   - Example: Swap weapon in hand directly with weapon in inventory
   - Preserves shapes, validates types

3. **Item Interaction System**
   - Right-click item → context menu (Use, Split, Examine, Drop)
   - Use consumables (potions, food) from inventory
   - Examine for detailed stats/lore

4. **Nested Containers**
   - Items can BE containers (bags, pouches, quivers)
   - Double-click bag → open container view (nested inventory grid)
   - Type filtering (quiver accepts arrows only)
   - Reference: NeoScavenger aCapacities

5. **Stack Support**
   - Stackable items (arrows ×20, branches ×5)
   - Stack count overlay on sprites
   - Split/merge stack operations

6. **Auto-Sort**
   - Sort by type (weapons, consumables, tools)
   - Sort by size (fill gaps efficiently)
   - Sort by value/weight

7. **Container Preview**
   - Hover over bag → tooltip shows contents preview (mini grid)
   - Quick view without opening full container

8. **Ground Item Interaction**
   - Pick up items from environment (dungeon loot)
   - Visual indicators on tiles with items

9. **Rich Tooltips**
   - Item stats (damage, weight, durability)
   - Comparison tooltips (equipped vs inventory item)
   - Lore/description text

**Next Steps**: Prioritize TODOs after Phase 1 playtesting feedback

---

### Item Stacking System (PLANNED)

**What**: Stackable item support (e.g., "5× Branch", "20× Arrow")

**Scope**:
- Stack limits per item type (configurable in item definitions)
- UI displays stack count overlay on slot visuals
- Commands: AddItemToStack, SplitStack, MergeStacks
- Benefits: Reduces inventory clutter for consumables and ammo
- Reference: NeoScavenger nStackLimit (branches=5, ammo=20, unique items=1)

---

### Equipment Slots System (PLANNED)

**What**: Actor equipment slots (main hand, off hand, head, torso, legs, ring×2)

**Why**: Used by ALL actors (player, NPCs, enemies) - not just player-controlled

**Scope**:
- Player: Equips items from inventory → equipment slots (affects stats)
- NPCs/Enemies: Spawned with pre-equipped gear (defines combat capabilities)
- Commands: EquipItem, UnequipItem, GetEquippedItems
- Integration: Combat system reads equipment for damage/defense calculations
- Reference: NeoScavenger battlemoves.xml (equipment affects available moves)
- Architecture: Separate from VS_008 Inventory (equipment = worn, inventory = carried)

---

### Ground Loot System (PLANNED)

**What**: Items at map positions (dungeon loot, enemy drops, player discards)

**Scope**:
- WorldItemRepository: Dictionary<Position, List<ItemId>>
- Commands: DropItemAtPosition, PickupItemAtPosition, GetItemsAtPosition
- Integration: Player picks up loot → adds to inventory (VS_008)
- Integration: Enemy dies → drops equipped items to ground
- UI: Visual indicators on tiles with loot (sparkle effect, item sprite)
- Foundation for dungeon generation loot placement

---

### Container System - Nested Inventories (PLANNED)

**What**: Items can BE containers (bags, pouches, quivers)

**Scope**:
- Nested spatial grids: Bag (4×6) can contain Pouch (2×2)
- Container properties: capacity grid size, allowed item types
- Type filtering: Quiver accepts arrows only, waterskin accepts liquids only
- Commands: OpenContainer, MoveItemToContainer
- Benefits: Organization (ammo in quiver), specialization (waterproof bags)
- Reference: NeoScavenger aCapacities ("4x6"), aContentIDs (whitelisted types)
- Decision point: May defer if Item Stacking reduces inventory clutter sufficiently

---

### Crafting System (NeoScavenger-Style) (PLANNED)

**What**: Grid-based crafting interface (drag items to recipe slots)

**Scope**:
- Tool vs. Consumed separation (lighter stays, tinder burns)
- Time-based crafting (recipes take hours, not instant)
- Recipe discovery (try combinations, learn new recipes)
- Reference: NeoScavenger recipes.xml (tool/consumed/destroyed separation)
- Benefits: Survival depth (repair gear, purify water, cook food)

---

### Unique Weapons Collection (PLANNED)

**What**: 10 unique weapons with build-enabling properties

**Scope**:
- Angband-style itemization (situational value)
- Constrained randomization (minor stat variance)
- Creates build variety and replayability

---

## Visual & Rendering

### Camera System (PLANNED - Infrastructure)

**What**: Camera2D for scalable viewport and player tracking

**Why**:
- Current test scene: 30×30 grid at 48px = 1440×1440 pixels (fits screen NOW, but won't scale)
- Larger dungeons (50×50, 100×100) require camera to follow player off-screen
- Professional feel: Player-centered camera with smooth transitions
- Foundation for screen shake, zoom, cutscenes

**Scope**:
- Add Camera2D to TurnQueueTestScene.tscn
- Configure follow behavior (player stays centered or uses drag margins)
- Enable smoothing for professional camera movement
- Zero changes to combat/movement logic (presentation-only)

**Implementation Approach**:
- Phase 1: Basic camera following player position (30 minutes)
- Phase 2: Smooth camera with drag margins (15 minutes)
- Phase 3: Zoom support for testing large maps (15 minutes)

**Priority**: Infrastructure for scalability - should be added BEFORE large map testing

---

### TileSet Migration & Autotiling (PLANNED)

**What**: Replace ColorRect grid rendering with TileMapLayer + TileSet autotiling

**Why**:
- Performance: TileMap optimized for grid rendering (ColorRect = UI elements abused for game grid)
- Designer empowerment: Visual scene editing in Godot editor (aligns with ADR-006 philosophy)
- Professional appearance: Autotiling for seamless wall connections
- Foundation for pixel art sprites (Kenney assets)

**Current State**:
- TurnQueueTestScene.tscn has TileMapLayer nodes DEFINED but UNUSED
- Controller creates ColorRect grid programmatically (lines 136-182 in TurnQueueTestSceneController.cs)
- Terrain/FOV/Actor layers exist as ColorRect arrays

**Migration Scope**:
- Replace ColorRect grid creation with TileMapLayer.set_cell() API
- Use existing TerrainLayer/FOVLayer nodes from scene
- Keep actor overlay as Sprite2D nodes (better for moving entities than tiles)
- Update rendering methods: SetCellColor() → SetCell() with tile IDs
- Preserve all fog of war logic (3-state system: unexplored, explored, visible)

**Estimated Effort**: 2-3 hours (refactoring existing rendering, preserving all combat/FOV logic)

**Dependencies**:
- None (can be done independently of gameplay systems)
- Recommended: Add Camera first (infrastructure)

**Decision Point**: Defer UNTIL Enemy AI (next priority) complete OR performance becomes issue

---

### Procedural Map Generation (PLANNED)

**What**: PCG algorithms for dungeon/map generation

**Why**:
- Visual variety (no more hardcoded test maps)
- Replayability (different layouts each playthrough)
- Testing scalability at various map sizes

**Scope**:
- PCG algorithm selection: BSP trees, cellular automata, or drunkard's walk
- Integration with existing GridMap/terrain system (GridMap = SSOT in Core)
- Room/corridor generation with guaranteed connectivity
- Parametric generation (room sizes, density, complexity)

**Dependencies**:
- Camera System (required for large generated maps)
- Recommended: TileSet Migration first (better visual feedback during generation testing)

**Notes**: Pure generation FIRST (no sprites), then visual polish with TileSet

---

### TileSet Visual Assets (PLANNED)

**What**: Pixel art sprites for terrain using Kenney assets

**Why**: Professional appearance (pixel art vs. colored rectangles)

**Scope**:
- Kenney Micro Roguelike tileset (8x8 sprites, CC0 license)
- Autotiling rules for seamless wall connections
- Terrain variants (floor, wall, smoke/bushes)

**Dependencies**: TileSet Migration complete (uses TileMapLayer API)

**Notes**: Separate from procedural generation - can use ColorRects OR tiles with PCG

---

### Attack Animations (PLANNED)

**What**: Visual feedback for combat actions

**Scope**:
- Weapon swing animations
- Damage numbers/effects
- Death animations
- Currently: Instant damage with console logging

---

## Progression

### Weapon Proficiency Tracking (PLANNED)

*See Combat section above - listed here for cross-reference*

---

### Character Aging & Time Pressure (PLANNED)

**What**: Character ages over time with stat degradation

**Why**: Creates natural run duration (2-4 hours before retirement/death)

**Scope**:
- Stat degradation with age (realism)
- Time pressure mechanic
- Natural end condition for runs

---

## World Generation

**Vision**: Physics-driven procedural world generation using WorldEngine algorithms (plate tectonics → elevation → climate → biomes)

**Philosophy**: Incremental pipeline - one algorithm at a time, fully tested and visualized before moving to next stage

**Current State**: Stage 1-2 complete (Elevation + Temperature). Ready for Stage 3-4 (Water/Erosion).

---

### VS_024: WorldGen Pipeline Stage 1 - Plate Tectonics & Elevation (COMPLETE)

**Status**: Complete (2025-10-08) | **Size**: M (~6h) | **Tests**: 433 passing
**Owner**: Dev Engineer

**Delivered**: Native plate tectonics simulation (C++ WorldEngine integration), elevation post-processing (quantile-based normalization + smoothing), dual-stage debug visualization (Original vs Post-Processed), serialization (Format v2 cache), WorldMapOrchestratorNode architecture

**What Was Built**:
- **Phase 1**: Native simulation integration (C++ DLL, P/Invoke marshalling)
- **Phase 2**: Elevation post-processing (quantile thresholds, Gaussian smoothing)
- **Phase 3**: Multi-stage visualization (2 view modes, legends, probe, UI)
- **Phase 4**: Serialization system (Format v2 with backward compat)

**Key Architecture Decisions**:
- **Native simulation**: 83% of generation time (1.2s total for 512×512) - C# port not worth it
- **Quantile normalization**: Adapts to per-world elevation distribution (realistic thresholds)
- **Dual visualization**: Original vs Post-Processed (trivial debugging of normalization)
- **Format v2 cache**: Stores both raw + processed elevation (0ms reload)

**Performance**: <1.5s for 512×512 world (native sim 1.0s + post-processing 0.2s)

**Archive**: [Completed_Backlog_2025-10_Part2.md](../../07-Archive/Completed_Backlog_2025-10_Part2.md) (search "VS_024")

---

### VS_025: WorldGen Pipeline Stage 2 - Temperature Simulation (COMPLETE)

**Status**: Complete (2025-10-08) | **Size**: S (~5h) | **Tests**: 447 passing
**Owner**: Dev Engineer

**Delivered**: Physics-based temperature calculation (4-component algorithm: latitude+tilt, noise, distance-to-sun, mountain-cooling), 4-stage debug visualization, per-world climate variation (hot/cold planets, shifted equator), normalized [0,1] output for future biome classification

**What Was Built**:
- **Phase 0**: Cache disable flag for development (5min)
- **Phase 1**: Core algorithm (MathUtils: Interp/SampleGaussian, TemperatureCalculator with 4-stage output) (1h)
- **Phase 2**: Pipeline integration (WorldGenerationResult properties, Stage 2 in GenerateWorldPipeline) (0.5h)
- **Phase 3**: Multi-stage visualization (4 MapViewMode enums, RenderTemperatureMap, legends, probe, UI) (1.5h)
- **Phase 4**: Visual validation + noise fix (FBm fractal + frequency scaling) (0.75h)

**Key Architecture Decisions**:
- **4-component algorithm**: Latitude (92% weight) + noise (8%) + distance-to-sun + mountain-cooling (WorldEngine validated)
- **Per-world parameters**: AxialTilt and DistanceToSun (Gaussian-distributed) create planet variety
- **RAW elevation + thresholds**: Mountain cooling uses PostProcessedHeightmap (raw values), not normalized
- **Normalized output [0,1]**: Internal format for biome classification (Stage 6), UI converts to °C via TemperatureMapper
- **Multi-stage debug**: 4 view modes isolate each component (trivial bug detection)
- **No threading**: Native sim dominates (83%), temperature ~60-80ms (YAGNI)

**Temperature Formula**:
```csharp
// 1. Latitude factor (92%) with axial tilt + distance-to-sun
float latitudeFactor = Interp(y_scaled, [tilt-0.5, tilt, tilt+0.5], [0, 1, 0]);

// 2. Coherent noise (8%) - FBm fractal for climate variation
float n = FastNoiseLite.GetNoise2D(...);  // OpenSimplex2, 8 octaves, freq=128.0

// 3. Combined base temperature
float t = (latitudeFactor * 12f + n * 1f) / 13f / distanceToSun;

// 4. Mountain-only cooling (RAW elevation > MountainLevel threshold)
if (rawElevation > mountainLevel) {
    t *= altitude_factor;  // 0.033 at extreme peaks (97% cooling)
}
```

**Visual Validation**:
- ✅ Latitude Only: Smooth horizontal bands, equator shifts with axial tilt
- ✅ With Noise: Subtle fuzzy climate variation (8% contribution, realistic!)
- ✅ With Distance: Hot/cold planet variation (inverse-square law working)
- ✅ Final: Mountains blue at ALL latitudes (elevation cooling working!)

**Performance**: <1.5s for 512×512 (no regression from VS_024)

**Archive**: [Backlog.md](../../01-Active/Backlog.md) (search "VS_025" - recently moved to Ideas/Done)

---

### TD_020: Physics-Based Thermal Diffusion (DEFERRED)

**Status**: Proposed → **Deferred until Water/Erosion complete**
**Owner**: Tech Lead
**Size**: M (~4-6h when implemented)
**Priority**: Ideas (blocked by missing water data)

**What**: Iterative heat diffusion simulation with heterogeneous thermal mass (water=high, land=elevation-based) to create realistic temperature gradients and coastal moderation

**Why Deferred**:
- **Blocking dependency**: Requires water/land mask from Stage 3-4 (not yet implemented)
- **Incomplete physics**: Land-only diffusion (without water) provides only 50% of value
- **Refactor risk**: Implementing now = doing it twice (land-only, then add water later)
- **Current maps "good enough"**: Sharp mountain boundaries not blocking biome classification (Stage 6)

**Decision (Tech Lead 2025-10-08)**:
- **Defer until VS_022 Stages 3-4 complete** (Water Table + Erosion)
- Implement diffusion as Stage 5 with **full** land/water/elevation data
- Single implementation path (no refactor debt)
- Get coastal moderation + mountain smoothing in one shot

**Future Scope (when water exists)**:
```csharp
// Thermal mass calculation (elevation + water)
float CalculateThermalMass(float normalizedElevation, bool isWater) {
    if (isWater) return 1.0f;  // Water: high thermal mass (slow temp change)
    return 0.5f - (normalizedElevation * 0.3f);  // Land: elevation-based
    // Lowlands: 0.5 (moderate), Mountains: 0.2 (low, fast change)
}

// Iterative diffusion (10 iterations)
for (int iter = 0; iter < 10; iter++) {
    foreach (cell in grid) {
        float weightedAvgNeighborTemp = WeightedAverage(neighbors, thermalMass);
        float influence = diffusionRate * (1.0f - thermalMass[cell]);
        T_new[cell] = T_old[cell] + influence * (weightedAvgNeighborTemp - T_old[cell]);
    }
}
```

**Expected Benefits (when implemented)**:
- ✅ Smooth mountain temperature gradients (no sharp boundaries)
- ✅ Coastal moderation (milder temperatures near water)
- ✅ Elevation-based thermal mass (mountains stay cold, realistic physics)
- ✅ Performance: ~80-120ms for 10 iterations (acceptable overhead)

**Cross-Reference**: See [tmp.md](../../../tmp.md) for full TD_020 proposal (Tech Lead review 2025-10-08)

---

### VS_022: WorldGen Pipeline - Incremental Post-Processing (PLANNED)

**Status**: Proposed (Stage 1-2 complete, ready for Stage 3+)
**Owner**: Product Owner → Tech Lead (breakdown)
**Size**: XL (multi-phase, build incrementally)
**Priority**: Ideas

**What**: Continue world generation pipeline with remaining stages: water table, erosion, precipitation, biomes, resources

**Why**: Each stage builds on previous (elevation → temperature → water → erosion → biomes → economy)

**Remaining Stages** (after VS_024 + VS_025):

**Stage 3: Water Table & Rivers** (NEXT)
- Sea level calculation (quantile-based threshold)
- Lake detection (elevation basins below sea level)
- River generation (flow from high → low elevation, Dijkstra paths to ocean/lakes)
- Output: Water mask (bool[,]), river network (List<RiverSegment>)

**Stage 4: Erosion Simulation**
- River erosion (carve valleys along river paths)
- Coastal erosion (smooth coastlines)
- Mountain weathering (reduce extreme peaks slightly)
- Output: Eroded elevation map (more realistic terrain)

**Stage 5: Thermal Diffusion** (TD_020 - AFTER Stage 3-4 complete)
- Physics-based heat diffusion with water/land thermal mass
- Coastal moderation + smooth mountain gradients
- Output: Diffused temperature map (realistic climate)

**Stage 6: Precipitation & Climate**
- Rain shadow effect (mountains block moisture)
- Distance from water (coastal = wet, inland = dry)
- Temperature influence (hot = evaporation, cold = snow)
- Output: Precipitation map (mm/year)

**Stage 7: Biome Classification**
- 48 biome types (WorldEngine catalog)
- Classification based on: elevation, temperature, precipitation
- Biome transitions (smooth gradients, not hard borders)
- Output: Biome map (BiomeType[,])

**Stage 8: Resource Distribution**
- Biome-driven resource placement (iron in mountains, fish near coast)
- Strategic resource rarity (gold, gems, rare herbs)
- Foundation for settlement economy (Legends Mod pattern)
- Output: Resource map (Dictionary<Position, ResourceType>)

**Philosophy**: One stage at a time, fully tested + visualized before next. Multi-stage debug rendering for each (like VS_024/VS_025).

**Dependencies**:
- Stage 3-4: VS_024 elevation complete ✅, VS_025 temperature complete ✅
- Stage 5: VS_022 Stages 3-4 complete (water data required)
- Stage 6-8: Sequential (each depends on previous)

---

## Strategic Map & History

**Vision**: Two-map architecture (UnReal World, Dwarf Fortress, RimWorld pattern)

```
Strategic Layer (Macro)          Tactical Layer (Micro)
┌──────────────────────┐         ┌──────────────────────┐
│ WorldEngine Map      │         │ Grid Combat (30×30+) │
│ 512×512 world tiles  │ ◄─────► │ Turn-based tactical  │
│ Factions, History    │  Zoom   │ FOV, Positioning     │
│ Travel, Quests       │  In/Out │ Looting, Encounters  │
└──────────────────────┘         └──────────────────────┘
```

**Current State**: Tactical layer complete (VS_001-VS_021). Strategic layer experimental.

---

### WorldEngine Integration (IN PROGRESS - Incremental Port)

**What**: Incremental port of WorldEngine physics-driven algorithms from Python to C# for runtime generation

**Status**: **Stage 1-2 COMPLETE** (VS_024 + VS_025) - See [World Generation](#world-generation) section for details

**Completed**:
- ✅ **Stage 1**: Plate tectonics + elevation (VS_024) - Native C++ integration via P/Invoke
- ✅ **Stage 2**: Temperature simulation (VS_025) - Full C# port, 4-component algorithm

**Next Steps**:
- **Stage 3-4**: Water table + erosion (VS_022) - Next priority
- **Stage 5**: Thermal diffusion (TD_020) - Deferred until water data exists
- **Stage 6-8**: Precipitation, biomes, resources - Sequential implementation

**Why**:
- Realistic geography (plate tectonics, climate, rivers) creates strategic depth
- Seed-based reproducibility for balanced gameplay
- 48 biome types provide terrain-driven economy (Legends Mod pattern)
- Runtime generation eliminates Python dependency for distribution

**Approach**: Incremental pipeline (one stage at a time), fully tested + visualized before moving to next
**Performance**: <1.5s for 512×512 world (native sim + post-processing)

---

### Settlement Economy System (PLANNED - Legends Mod Pattern)

**What**: Dynamic settlement system inspired by Battle Brothers Legends Mod

**Core Concepts**:

**Wealth & Tier Upgrades**:
- Settlements have wealth (0-100%) that grows/shrinks based on economy
- Organic progression: Hamlet → Village → Town → City
- Player actions affect growth (completing quests increases wealth, raids decrease it)

**Settlement Status Effects** (20+ temporary conditions):
- Economic: Ambushed Trade Routes, Prosperous, Conquered
- Military: Besieged, Raided Recently
- Events: Plague, Harvest Festival, Archery Contest
- Effects modify prices, recruit availability, quest options

**Attached Buildings** (Terrain-Driven):
- Buildings determined by WorldEngine biome data
- Coastal: Fishing Huts, Harbors
- Mountains: Iron Mines, Stone Quarries, Gold Mines (rare)
- Forests: Lumber Camps, Hunter's Cabins, Herbalist Groves
- Farmland: Breweries, Orchards, Beekeepers
- Desert: Incense Dryers, Spice Markets
- Each building produces resources and attracts specific recruits

**Integration**: Settlements placed on WorldEngine map, buildings driven by biome/elevation data

**Reference**: Battle Brothers Legends Mod settlement system (wealth-driven upgrades, dynamic statuses, terrain economy)

---

### Civilization History Simulation (PLANNED - Dwarf Fortress Pattern)

**What**: Generate 500 years of procedural faction history

**Why**:
- Dungeons have backstory (ruined cities from historical wars)
- Faction relations shaped by historical events
- Quest hooks from legends ("Recover lost crown from Battle of Redforge, Year 423")
- Emergent narrative from procedural events

**Approach**: Simulate wars, territorial expansion, legendary figures, artifact creation
**Output**: History ledger, ruined cities (dungeon sites), faction relationships, legendary items

---

### Strategic Travel & Tactical Bridge (PLANNED)

**What**: Travel system linking strategic map (512×512 tiles) to tactical encounters (30×30 grids)

**Gameplay**:
- Click destination on world map (town, dungeon, wilderness)
- Travel with time passage (terrain affects speed: mountains slower, roads faster)
- Random encounters during travel (faction patrols, bandits, wildlife)
- Arrive → zoom into tactical combat (seamless scene transition)
- Complete encounter → return to strategic map

**Reference**: UnReal World zoom in/out, Dwarf Fortress embark screen, RimWorld world tiles

---

## Economy & Quests

### Quest System (PLANNED)

**What**: Dynamic quest generation from settlement context and historical events

**Scope**:
- Quests tied to settlement statuses (relieve siege, cure plague, defend from raid)
- Historical quests from civilization simulation (recover lost artifacts, avenge betrayals)
- Faction-aligned quest givers in settlements
- Quest types: Clear dungeon, recover artifact, escort, assassination

**Integration**: Settlement status effects + history ledger create contextual quests

---

### Economy System (PLANNED)

**What**: Resource-based economy driven by settlement buildings and status effects

**Scope**:
- Trade goods produced by attached buildings (fish, iron ore, lumber, spices)
- Settlement status modifies prices (Besieged = +100% food, Prosperous = -10% all)
- Faction reputation affects prices and access
- Equipment purchases, repairs, consumables

**Integration**: Settlement wealth system + attached buildings + Legends Mod status effects

---

## Narrative

### Origins System (PLANNED)

**What**: 3-5 starting backgrounds (noble, soldier, peasant, scholar, merchant)

**Scope**:
- Affects starting proficiencies, equipment, faction standings
- Defines early viable strategies
- Creates different playthroughs

---

### Event System (PLANNED)

**What**: Random encounters with branching choices

**Scope**:
- Consequences affect character state and reputation
- RimWorld-style procedural storytelling
- Creates memorable "remember when..." moments

---

### Reputation System - N×N Matrix (PLANNED)

**What**: Complex inter-faction relationship system

**Scope**:
- N×N reputation matrix (each faction has opinion of every other faction)
- Origin-specific quest lines unlock based on starting faction
- Dynamic relationships: Enemy of my enemy becomes ally
- Player actions affect multiple faction standings simultaneously
- Reference: NeoScavenger factions.xml (cross-faction reputation matrix)

**Example**:
```
Player helps Dwarves → +reputation with Dwarves
                    → -reputation with Orcs (dwarf enemies)
                    → +reputation with Humans (dwarf allies)
```

**Depends On**: Strategic Map Phase 2 (faction placement and basic relations)

---

## Development Info

### Development Velocity Estimates

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

### Success Milestones

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

### Risk Management

**Risk 1: Time-Unit Combat Isn't Fun**
- **Mitigation**: Extensive playtesting after VS_007, pivot to simpler turn-based if needed

**Risk 2: Proficiency Grind Feels Tedious**
- **Mitigation**: Tunable formulas, balance for 2-4 hour runs

**Risk 3: Scope Creep (Kitchen Sink Syndrome)**
- **Mitigation**: Strict phase gates - don't start Phase 2 until Phase 1 proves fun

**Risk 4: Layered Armor Too Complex**
- **Mitigation**: Phase 2 starts with simple armor, only add layers if needed

**Risk 5: Solo Development Burnout**
- **Mitigation**: Shippable milestones every phase create motivation, early playtesting validates direction

---

### Next Actions

**Immediate (Product Owner)**:
1. Review reorganized roadmap structure
2. Decide on next priority: Enemy AI & Vision System (natural next step)
3. Create detailed VS specification when ready

**After Approval (Tech Lead)**:
1. Review architecture implications
2. Approve implementation approach
3. Hand off to Dev Engineer for phased implementation

**Current Status**: Awaiting user approval of category-based roadmap structure

---

*This roadmap organizes all Darklands features by category for quick reference. Completed items retain their VS_XXX numbers. Planned items are organized by feature area without assigned numbers.*
