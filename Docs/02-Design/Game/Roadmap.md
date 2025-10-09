# Darklands Development Roadmap

**Purpose**: Organize all Darklands features by category for quick reference and planning.

**Last Updated**: 2025-10-09 23:11 (Product Owner: Created Stats/Progression roadmap, VS_032 equipment system proposed)

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
- [Progression](#progression) - Stats, Equipment, Proficiency, Aging
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

**📖 Full Details**: [Roadmap_Combat.md](Roadmap_Combat.md) - Comprehensive combat roadmap

**Vision**: Stoneshard-inspired time-unit tactical combat - every action costs time, positioning matters, equipment defines capabilities.

**Current State**: Foundation complete (turn queue + basic combat working!) - VS_007 + VS_020 delivered time-unit system with click-to-attack.

**Completed Features**:
```
✅ VS_007: Time-Unit Turn Queue (exploration/combat transitions, 359 tests)
✅ VS_020: Basic Combat System (click-to-attack, range validation, 428 tests)
```

**Planned Features**:
```
⏳ Enemy AI & Vision (asymmetric combat, ambushes, autonomous enemies)
⏳ Armor Systems (simple → damage types → layered - progressive depth)
⏳ Weapon Proficiency (use-based progression, time cost reduction)
💡 Status Effects (poison, bleeding, buffs/debuffs - future)
```

### Why Enemy AI Next (After VS_032)?

- **Asymmetric combat** - Enemies detect player first, initiate ambushes
- **Tactical depth** - Player considers enemy vision cones, patrol patterns
- **Reuses VS_007 pattern** - PlayerDetectionEventHandler mirrors EnemyDetectionEventHandler
- **Autonomous enemies** - Make decisions (move toward player, attack, flee)

### Why Defer Layered Armor?

- **VS_032 includes simple armor** - Defense bonus + weight (sufficient for initial depth)
- **Damage types come first** - Weapon choice matters (slashing vs plate, blunt vs mail)
- **Complexity gate** - Layered armor adds depth BUT may not be worth implementation cost
- **Playtest-driven** - Only implement if damage types feel shallow after validation

### Completed Features

**VS_007**: Time-unit turn queue (FOV-driven combat transitions) ✅
**VS_020**: Click-to-attack combat (melee/ranged, death handling) ✅

*See [Roadmap_Combat.md](Roadmap_Combat.md) for full feature details, example scenarios, and integration points.*

---

## Inventory & Items

**📖 Full Details**: [Roadmap_Inventory_Items.md](Roadmap_Inventory_Items.md) - Comprehensive inventory roadmap

**Vision**: NeoScavenger-inspired Tetris inventory - spatial constraints create meaningful trade-offs (carry weapons OR consumables, not both).

**Current State**: Phase 1 complete (Tetris grid working!) - VS_018 delivered spatial inventory with L/T-shapes, drag-drop, rotation.

**Completed Features**:
```
✅ VS_008: Slot-Based Inventory MVP (20 slots, add/remove)
✅ VS_009: TileSet Metadata-Driven Items (designer empowerment!)
✅ VS_018 Phase 1: Spatial Tetris Grid (L/T-shapes, drag-drop, 359 tests)
```

**Planned Features**:
```
⏳ Ground Loot System (enemy drops, dungeon loot)
💡 Item Stacking (DEFERRED until consumables exist)
❓ Nested Containers (MAYBE NEVER - complexity without value?)
⏳ VS_018 Phase 2: UX Improvements (click-to-pick, tooltips, auto-sort)
```

### Why Ground Loot Next (After VS_032)?

- **Blocks dungeon generation**: Can't place loot without ground item system
- **Blocks enemy drops**: Dead enemies need to drop equipped items
- **Foundation for economy**: Pick up items → sell in town

### Why Defer Item Stacking?

- **No consumables yet**: Nothing to stack (no potions, arrows, crafting materials)
- **Premature optimization**: Building for future needs we haven't validated
- **Revisit after**: Consumable items implemented AND clutter problem proven

### Completed Features

**VS_008**: Slot-based inventory MVP (20 slots, capacity enforcement) ✅
**VS_009**: TileSet metadata-driven items (designer empowerment) ✅
**VS_018 Phase 1**: Spatial Tetris grid (L/T-shapes, 359 tests GREEN) ✅

*See [Roadmap_Inventory_Items.md](Roadmap_Inventory_Items.md) for full feature details and architecture decisions.*

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

**📖 Full Details**: [Roadmap_Stats_Progression.md](Roadmap_Stats_Progression.md) - Comprehensive technical roadmap

**Vision**: Darklands (1992) inspired skill-based progression - no experience levels, equipment defines capabilities, use-based proficiency, aging creates time pressure.

**Current State**: Foundation ready (Actor components, combat system, inventory) - Missing stats/equipment integration!

**Priority Systems**:
```
CRITICAL (VS_032): Equipment & Stats System - 12-16h
  Phase 1: Character Attributes (STR, DEX, END, INT)
  Phase 2: Equipment Slots (main hand, off hand, armor)
  Phase 3: Stat Modifiers (equipment affects combat)
  Phase 4: Weight System (heavy armor = slower actions)

IMPORTANT (After VS_032): Proficiency System - 12-16h
  - Weapon skill progression (use sword, get better at swords)
  - Action time reduction (50 skill = -15% attack time)
  - Specialization incentives (master daggers vs generalist)

IDEAS (Far Future): Character Aging - 6-8h
  - Stat degradation over time
  - Natural run duration (2-4 hours before retirement)
```

### Why Equipment First?

- **Combat depth NOW**: Armor choices affect defense, weapon choices affect damage
- **Build variety**: Heavy tank vs light skirmisher (emergent playstyles)
- **Vision alignment**: Darklands core pillar (equipment defines capabilities, no levels)
- **Unblocks proficiency**: Need equipment system before tracking weapon skill progression

### Completed Features

*None yet - VS_032 is first progression system*

---

## World Generation

**📖 Full Details**: [Roadmap_World_Generation.md](Roadmap_World_Generation.md) - Comprehensive technical roadmap

**Vision**: Dwarf Fortress-inspired procedural worldgen (plate tectonics → geology → climate → hydrology → biomes → resources)

**Current State**: Phase 1 ~80% complete (Elevation + Temperature + Precipitation pipeline ✅ COMPLETE!)

**Three-Phase Roadmap**:
```
Phase 1: Core Pipeline (MVP) - 80% complete
  ✅ Stage 1: Tectonic Foundation (VS_024)
  ✅ Stage 2: Atmospheric Climate (VS_025-028 COMPLETE!)
  🔄 Stage 3: Hydrological Processes (VS_029 particle erosion in progress)
  ⏳ Stage 4: Biome Classification

Phase 2: Hydrology Extensions - Not started
  - Swamps, creek visualization, slope maps

Phase 3: Geology & Resources (DF-Inspired) - Design phase
  - Volcanoes, minerals, thermal erosion, extended biomes
```

### Completed Features

**VS_024: Plate Tectonics & Elevation** ✅ COMPLETE (2025-10-08)
- Native C++ integration (1.0s for 512×512)
- Dual-heightmap architecture (raw + post-processed)
- Quantile-based thresholds (adaptive per-world)
- Archive: [Completed_Backlog_2025-10_Part2.md](../../07-Archive/Completed_Backlog_2025-10_Part2.md)

**VS_025: Temperature Simulation** ✅ COMPLETE (2025-10-08)
- 4-component algorithm (latitude + noise + distance-to-sun + mountain cooling)
- 4-stage debug visualization
- Per-world climate variation (hot/cold planets)
- Archive: [Completed_Backlog_2025-10_Part3.md](../../07-Archive/Completed_Backlog_2025-10_Part3.md) (search "VS_025")

**VS_026: Base Precipitation** ✅ COMPLETE (2025-10-08)
- 3-stage algorithm (noise + temperature curve + renormalization)
- WorldEngine exact port (gamma=2.0, curveBonus=0.2)
- Archive: [Completed_Backlog_2025-10_Part3.md](../../07-Archive/Completed_Backlog_2025-10_Part3.md) (search "VS_026")

**VS_027: Rain Shadow Effect** ✅ COMPLETE (2025-10-08)
- Latitude-based prevailing winds (Polar Easterlies / Westerlies / Trade Winds)
- Orographic blocking (max 20 cells ≈ 1000km trace)
- Real-world desert patterns (Sahara, Gobi, Atacama)
- Archive: [Completed_Backlog_2025-10_Part3.md](../../07-Archive/Completed_Backlog_2025-10_Part3.md) (search "VS_027")

**VS_028: Coastal Moisture Enhancement** ✅ COMPLETE (2025-10-09)
- Distance-to-ocean BFS + exponential decay (O(n) flood fill)
- Maritime vs continental climate patterns (Seattle vs Spokane)
- Completes atmospheric climate pipeline (FINAL PRECIPITATION MAP ready!)
- 495/495 tests GREEN (100%)
- Archive: [Completed_Backlog_2025-10_Part3.md](../../07-Archive/Completed_Backlog_2025-10_Part3.md) (search "VS_028")

### In Progress

**VS_029: Particle-Based Erosion & Rivers** 🔄 NEXT (20-28h estimate)
- SimpleHydrology algorithm (particle physics with momentum field feedback)
- Precipitation-weighted seeding + scale-aware parameters
- Natural meandering rivers (emergent from physics, not algorithmic)
- River/lake extraction (continuous discharge → discrete markers)
- **Time savings vs original plan**: Replaces watermap simulation (saves 3-4h!)
- Details: [Roadmap_World_Generation.md](Roadmap_World_Generation.md#vs_029-particle-based-hydraulic-erosion--rivers--planned)

**VS_031: WorldGen Debug Panel** ⏳ AFTER VS_029 (6-8h estimate) - **ESSENTIAL**
- Real-time semantic parameter tuning (RiverDensity, Meandering, ValleyDepth, Speed)
- Stage-based incremental regeneration (0.5s erosion-only vs 2s full world)
- Scale-aware normalization (presets work on any map size)
- Preset system (Earth, Mountains, Desert, Islands)
- Details: [Roadmap_World_Generation.md](Roadmap_World_Generation.md#vs_031-worldgen-debug-panel-real-time-parameter-tuning--planned)

**Irrigation → Humidity → Biomes** ⏳ PHASE 1 COMPLETION
- ~~Watermap simulation~~ **REPLACED** (VS_029 discharge map serves as watermap!)
- Moisture spreading from discharge (21×21 kernel)
- 48 biome types (temperature + humidity)
- Details: [Roadmap_World_Generation.md](Roadmap_World_Generation.md#stage-3-hydrological-processes)

**Phase 2-3: Extended Features** (DF-Inspired Depth)
- Swamps (poor drainage detection)
- Volcanoes (plate boundaries, geothermal heat)
- Geology layers (rock types, ore veins)
- Minerals & resources (geology-based economy)
- Details: [Roadmap_World_Generation.md](Roadmap_World_Generation.md#phase-3-geology--resources-df-inspired-depth)

### Performance

**Current**: 1.61s for 512×512 world (Phase 1-2 complete)
**Target**: <2s total (Phase 1-4 complete)
**Projected**: 1.93s (with optimizations)

### Standalone Worldgen Game Potential

**Vision**: Extract worldgen as standalone exploration game (separate from Darklands tactical RPG)
**Timeline**: +2-3 weeks after Phase 3 complete
**Details**: [Roadmap_World_Generation.md](Roadmap_World_Generation.md#standalone-worldgen-game-potential)

---

## Strategic Map & History

**Vision**: Two-map architecture (UnReal World, Dwarf Fortress, RimWorld pattern)

**Architecture**:
```
┌────────────────────────────────────────────────┐
│ Worldgen Layer (Terrain Foundation)           │
│ - Plate tectonics, climate, rivers, biomes    │
│ - Geology, minerals, volcanoes                │
│ - Standalone game: Explore generated worlds   │
└─────────────────┬──────────────────────────────┘
                  │ Provides terrain data
                  ↓
┌────────────────────────────────────────────────┐
│ Strategic Layer (Civilization)                 │
│ - Settlements (placed on biomes/resources)    │
│ - Factions, territorial control, wars         │
│ - History simulation (500 years)              │
│ - Economy (settlement buildings → resources)  │
└─────────────────┬──────────────────────────────┘
                  │ Zoom in
                  ↓
┌────────────────────────────────────────────────┐
│ Tactical Layer (Grid Combat)                   │
│ - VS_001-021 systems (combat, inventory, AI)  │
│ - Encounters at strategic locations           │
└────────────────────────────────────────────────┘
```

**Current State**:
- ✅ Tactical layer complete (VS_001-VS_021)
- 🔄 Worldgen layer in progress (Phase 1 ~75% complete)
- ⏳ Strategic layer planned (blocked by worldgen completion)

**Worldgen Integration**: See [Roadmap_World_Generation.md](Roadmap_World_Generation.md#strategic-layer-integration) for settlement placement algorithms

---

### Settlement Economy System (PLANNED - Legends Mod Pattern)

**What**: Dynamic settlement system inspired by Battle Brothers Legends Mod

**Integration with Worldgen**:
- **Attached Buildings** driven by biome + geology data:
  - Coastal (ocean biome): Fishing Huts, Harbors
  - Mountains (high elevation): Iron Mines, Stone Quarries
  - Volcanic Mountains (Phase 3): Gold Mines (rare), Obsidian Quarries
  - Forests (forest biome): Lumber Camps, Hunter's Cabins
  - Swamps (Phase 2): Herbalist Groves (rare plants)
  - Farmland (temperate + fertile): Breweries, Orchards
  - Desert (arid biome): Incense Dryers, Spice Markets

- **Resource Production** based on geology (Phase 3):
  - Iron Mines: Require sedimentary/metamorphic rock
  - Gold Mines: Require igneous rock + volcanic proximity
  - Stone Quarries: Any high-elevation mountain

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

**Dependencies**:
- Worldgen Phase 1 (biomes) ✅ Planned
- Worldgen Phase 3 (geology, volcanoes) → Enables mineral-driven economy

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
