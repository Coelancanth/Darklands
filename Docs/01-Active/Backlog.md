# Darklands Development Backlog


**Last Updated**: 2025-10-05 00:32 (Tech Lead: SIMPLIFIED VS_019 scope - removed PCG, focus on TileMapLayer visual upgrade + TileSet SSOT refactoring only, added tree terrain for interior obstacles, M (1-2 days) estimate)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 006 (TD_005 complete, counter unchanged)
- **Next VS**: 019


**Protocol**: Check your type's counter → Use that number → Increment the counter → Update timestamp

## 📖 How to Use This Backlog

### 🧠 Owner-Based Protocol

**Each item has a single Owner persona responsible for decisions and progress.**

#### When You Embody a Persona:
1. **Filter** for items where `Owner: [Your Persona]`
3. **Quick Scan** for other statuses you own (<2 min updates)
4. **Update** the backlog before ending your session
5. **Reassign** owner when handing off to next persona


### Default Ownership Rules
| Item Type | Status | Default Owner | Next Owner |
|-----------|--------|---------------|------------|
| **VS** | Proposed | Product Owner | → Tech Lead (breakdown) |
| **VS** | Approved | Tech Lead | → Dev Engineer (implement) |
| **BR** | New | Test Specialist | → Debugger Expert (complex) |
| **TD** | Proposed | Tech Lead | → Dev Engineer (approved) |

### Pragmatic Documentation Approach
- **Quick items (<1 day)**: 5-10 lines inline below
- **Medium items (1-3 days)**: 15-30 lines inline (like VS_001-003 below)
- **Complex items (>3 days)**: Create separate doc and link here

**Rule**: Start inline. Only extract to separate doc if it grows beyond 30 lines or needs diagrams.

### Adding New Items
```markdown
### [Type]_[Number]: Short Name
**Status**: Proposed | Approved | In Progress | Done
**Owner**: [Persona Name]  ← Single responsible persona
**Size**: S (<4h) | M (4-8h) | L (1-3 days) | XL (>3 days)
**Priority**: Critical | Important | Ideas
**Markers**: [ARCHITECTURE] [SAFETY-CRITICAL] etc. (if applicable)

**What**: One-line description
**Why**: Value in one sentence  
**How**: 3-5 technical approach bullets (if known)
**Done When**: 3-5 acceptance criteria
**Depends On**: Item numbers or None

**[Owner] Decision** (date):  ← Added after ultra-think
- Decision rationale
- Risks considered
- Next steps
```

## 🔥 Critical (Do First)
*Blockers preventing other work, production bugs, dependencies for other features*

---

*Recently completed and archived (2025-10-05):*
- **VS_019**: TileSet-Based Visual Scene + TileSet as Terrain Catalog (SSOT) - All 4 phases complete! TileMapLayer pixel art rendering (terrain), Sprite2D actors with smooth tweening, fog overlay system, 300+ line cleanup. Commits: f64c7de, 59159e5, d9d9a4d, 27b62b2, 896f6d5. Follow-up: Wall autotiling (VS_019_FOLLOWUP). ✅ (2025-10-05)
- **TD_003**: Separate Equipment Slots from Spatial Inventory Container - Created EquipmentSlotNode (646 lines), extracted InventoryRenderHelper (256 lines), cleaned InventoryContainerNode. All 3 phases complete, 359 tests GREEN. ✅ (2025-10-04)
- **TD_004**: Move ALL Shape Logic to Core (SSOT) - Eliminated all 7 business logic leaks from Presentation (164 lines removed, 12% complexity reduction). Fixed SwapItemsCommand double-save bug, eliminated cache-driven anti-pattern. Commits: 4cd1dbe, 49c06e6. ✅ (2025-10-04)
- *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### VS_019: TileSet-Based Visual Scene + TileSet as Terrain Catalog (SSOT) 🎨 ✅
**Status**: Done | **Owner**: Dev Engineer | **Size**: M (1-2 days) | **Priority**: Important
**Markers**: [VISUAL-POLISH] [MOTIVATION] [ARCHITECTURE] [REFACTORING]

**What**: Replace GridTestScene's ColorRect rendering with TileMapLayer, refactor terrain to use TileSet as SSOT (like VS_009 items catalog), maintain existing test scene layout

**Latest Progress** (2025-10-05 02:13):
- ✅ **Phase 1 COMPLETE** [f64c7de] - Core refactoring, TerrainType enum deleted
- ✅ **Phase 2 COMPLETE** [59159e5, d9d9a4d] - TileSetTerrainRepository, wall_stone configured
- ✅ **Phase 3 COMPLETE** [27b62b2] - TileMapLayer pixel art rendering + fog fixes
  - GridTestSceneController renders via TileMapLayer.SetCell() with 6× scale
  - Fixed rendering bugs: Z-index layering, scale mismatch (8px tiles → 48px grid)
  - Replaced smoke → grass terrain (atlas 5:4), added to TileSet custom data
  - Fixed FOV/fog overlay for dark floor tiles (ColorRect transparent, fog alpha 0.4)
  - Tree terrain configured (impassable + opaque), 5 trees at Y=15
  - All 415 tests GREEN - Presentation changes don't affect Core
- ✅ **Phase 4 COMPLETE** (2025-10-05 02:13) - Actor Sprite2D rendering with smooth movement
  - Replaced ColorRect actors with pixel art Sprite2D nodes (Player atlas 5:0, Dummy atlas 4:0)
  - Implemented smooth movement tweening (100ms Godot Tween animation)
  - Actors render at Z=20 (above fog overlay), visibility controlled by FOV
  - Deleted deprecated ColorRect actor layer (_actorCells array, 900 nodes eliminated)
  - Code cleanup: 150 lines net reduction (deleted SetCellColor, RestoreCellColor, color constants)
  - Pixel art rendering: terrain (TileMapLayer) + actors (Sprite2D) + fog (ColorRect overlay)
- **Status**: ✅ VS_019 COMPLETE! TileSet SSOT working, pixel art rendering polished, fog readable
- **Follow-up Work**: Wall autotiling fix (SetCellsTerrainConnect batch processing - see VS_019_FOLLOWUP below)

**Why**:
- Visual progress after infrastructure-heavy VS_007 (restore motivation)
- Professional pixel art appearance vs prototype ColorRect
- **Architect terrain like items (VS_009 pattern)**: TileSet = catalog, Infrastructure loads → Core domain objects
- Designer-editable terrain properties (add lava/ice/water without C# changes)
- Future-proof: Supports movement_cost, damage_per_turn, terrain effects
- Autotiling for walls creates polished look
- **NOT blocking Phase 1 validation** (acknowledged polish + architecture improvement)

**🎯 ARCHITECTURAL PATTERN: TileSet as SSOT** (Same as VS_009 Items):
```
TileSet (Godot)                  Infrastructure (Bridge)           Core (Pure C#)
┌─────────────────┐             ┌──────────────────────┐          ┌─────────────────────┐
│ Custom Data:    │   reads     │ GodotTerrain-        │ returns  │ TerrainDefinition   │
│ - terrain_name  │ ─────────>  │ Repository           │ ──────>  │ record              │
│ - is_passable   │             │                      │          │ - Name              │
│ - is_opaque     │             │ Implements:          │          │ - IsPassable        │
│ - atlas_x/y     │             │ ITerrain-            │          │ - IsOpaque          │
│                 │             │ Repository           │          │                     │
└─────────────────┘             └──────────────────────┘          └─────────────────────┘
   (SOURCE OF                      (Godot → Core)                    (No Godot deps!)
    TRUTH)                          Bridge Layer                      Pure domain
```

**Key Principle**: TileSet stores ALL terrain properties (gameplay + visual), Infrastructure reads TileSet → creates Core domain objects, Core NEVER touches Godot.

**How** (Refined 7-Phase Breakdown - TileSet SSOT Approach):

**Phase 0: TileSet Configuration** - ✅ COMPLETED (2025-10-05)
- ✅ Autotiling VALIDATED - test_terrain_tileset.tres shows working 3x3 bitmask patterns for walls
- ✅ Custom data layers configured: `name`, `can_pass`, `can_see_through` (TileSet as SSOT)
- ✅ Terrain tiles defined: floor (1:1), wall autotiling (terrain_set_0), smoke (15:3), tree (5:5)
- ✅ Tree tile needs `can_pass=false` and `can_see_through=false` configuration
- **Next**: Review VS_009 GodotItemRepository pattern, then proceed to Phase 1

**Phase 1: Core Refactoring - TerrainDefinition Domain Model** - ✅ **COMPLETE** (2025-10-05 01:21, commit f64c7de)
- ✅ Created `TerrainDefinition` immutable record with CanPass/CanSeeThrough properties
- ✅ Created `ITerrainRepository` interface (Application layer, DIP)
- ✅ Created `StubTerrainRepository` (hardcoded Floor/Wall/Smoke catalog for Phase 1)
- ✅ Refactored `GridMap` to store `TerrainDefinition` objects (not strings - zero-cost access!)
- ✅ Updated `SetTerrainCommand` to accept terrain name strings
- ✅ **DELETED** `TerrainType` enum and `TerrainTypeExtensions`
- ✅ Updated GameStrapper DI registration (factory pattern for GridMap)
- ✅ Updated ALL tests (GridMapTests, MoveActorTests, FOVTests, ShadowcastingTests)
- ✅ Updated Presentation layer (GridTestScene, TurnQueueTestScene)
- ✅ **All 415 tests GREEN** - 100% behavioral compatibility maintained

**Phase 2: Infrastructure - TileSetTerrainRepository** - ✅ **COMPLETE** (2025-10-05 01:39, commit 59159e5)
- ✅ Created TileSetTerrainRepository (Infrastructure/TileSetTerrainRepository.cs)
- ✅ Follows VS_009 TileSetItemRepository pattern exactly
- ✅ Auto-discovers terrains from TileSet atlas source 4
- ✅ Reads custom data layers: name (layer 0), can_pass (layer 1), can_see_through (layer 2)
- ✅ Validates name required, defaults can_pass/can_see_through to true if missing
- ✅ Registered in Main.cs ConfigureServices (Presentation loads TileSet via GD.Load)
- ✅ Removed StubTerrainRepository from GameStrapper (ITerrainRepository now in Main.cs)
- ✅ **All 415 tests GREEN** - DIP validated (Core unchanged despite implementation swap)

**Phase 3: Upgrade GridTestScene to TileMapLayer In-Place (2-3 hours)** - No duplication!
- **Step 1 (15 min)**: Add `TileMapLayer` node to existing GridTestScene.tscn
  - Assign `test_terrain_tileset.tres` to TileMapLayer
  - Keep ColorRect arrays temporarily (terrain + FOV overlay)
  - Commit: "feat(rendering): Add TileMapLayer to GridTestScene (ColorRect still active)"
- **Step 2 (1 hour)**: Update terrain initialization to use TileMapLayer
  - Inject `ITerrainRepository` via ServiceLocator in `_Ready()`
  - Update `InitializeGameState()` to render via TileMapLayer:
    ```csharp
    // OLD (Phase 1-2 breaks this):
    await _mediator.Send(new SetTerrainCommand(pos, TerrainType.Wall));

    // NEW (works with refactored Core):
    await _mediator.Send(new SetTerrainCommand(pos, "wall"));
    var atlasCoords = _terrainRepo.GetAtlasCoords("wall");
    _terrainLayer.SetCell(new Vector2I(x, y), sourceId: 4, atlasCoords);
    ```
  - Replace interior walls (Y=15, X=5-9) with "tree" terrain
  - Commit: "feat(rendering): Render terrain via TileMapLayer (ColorRect deprecated)"
- **Step 3 (30 min)**: Delete ColorRect terrain layer
  - Delete `_gridCells[,]` array declaration and initialization
  - Delete `RestoreTerrainColor()` method (hardcoded terrain logic)
  - Keep `_fovCells[,]` ColorRect array (FOV overlay still needed!)
  - Commit: "refactor(rendering): Remove deprecated ColorRect terrain layer"
- **Step 4 (30 min)**: Fix compilation errors and update pathfinding
  - Update `IsPassable()` method to query repository instead of hardcoded checks
  - Fix any lingering `TerrainType` references
  - Run tests: `./scripts/core/build.ps1 test`
  - Commit: "fix(rendering): Update pathfinding to use TerrainRepository"
- **Architectural Note**: Core refactoring (Phase 1-2) breaks original ColorRect scene anyway - no preservation possible
- **Safety Net**: Git branch + incremental commits provide rollback capability (better than scene duplication)
- **Done When**: GridTestScene renders via TileMapLayer, ColorRect terrain deleted, walls auto-tile, trees at Y=15, FOV/fog works, all tests GREEN

**Phase 4: Actor Sprites with Sprite2D (OPTIONAL - 1-2 hours)** - Can defer if time-constrained
- Load `test_actor_tileset.tres` in `_Ready()` (actor catalog with custom data: name)
- Create `PlayerSprite` and `DummySprite` Sprite2D nodes in scene
- Configure texture regions from actor TileSet atlas dynamically:
  ```csharp
  // Use actor TileSet as texture source (NOT a second TileMapLayer!)
  var actorAtlas = actorTileSet.GetSource(0) as TileSetAtlasSource;
  _playerSprite.Texture = actorAtlas.Texture;
  _playerSprite.RegionRect = GetActorRegion(actorAtlas, "player"); // Atlas 5:0
  ```
- Implement `GetActorRegion(atlas, actorName)` helper to find atlas coords by custom data name
- Update `OnActorMoved` event handler to tween Sprite2D positions (smooth movement animation)
- Remove `_actorCells[,]` ColorRect array (replaced by Sprite2D nodes)
- Set `ZIndex = 20` to render actors above terrain and FOV overlay
- **Done When**: Player/dummy render as pixel art sprites from actor TileSet, smooth movement tweening works, or SKIP if ColorRect sufficient

**Scope**:
- ✅ **Core refactoring**: TerrainDefinition with CanPass/CanSeeThrough, delete TerrainType enum (breaks ColorRect scene - acceptable)
- ✅ **Infrastructure**: GodotTerrainRepository reads TileSet custom data (VS_009 pattern)
- ✅ **Terrain TileSet as SSOT**: Custom data layers (name, can_pass, can_see_through) - **CONFIGURED**
- ✅ **Autotiling for walls** - **VALIDATED** (terrain_set_0 with 9 bitmask patterns)
- ✅ **In-place upgrade**: Modify GridTestScene directly (NO scene duplication - YAGNI principle)
- ✅ TileMapLayer rendering replaces ColorRect terrain layer (FOV ColorRect overlay preserved)
- ✅ Maintain existing test scene layout (30×30 grid, border walls, smoke patches, interior obstacles)
- ✅ Add tree terrain (simple tile, no autotiling) - replaces interior walls for visual variety
- ✅ Incremental commits for safety (4-step migration with rollback points)
- ⚠️ **OPTIONAL**: Actor sprites via Sprite2D nodes using `test_actor_tileset.tres` (player, dummy, zombie, beholder)
- ⚠️ **OPTIONAL**: Smooth movement tweening for actors (can use instant ColorRect if time-constrained)
- ❌ Scene duplication (ColorRect prototype has no value after TileMapLayer works - maintenance burden)
- ❌ Second TileMapLayer for actors (use Sprite2D instead - actors are dynamic, not grid-locked)
- ❌ PCG (defer to future VS - focus on visual upgrade only)
- ❌ Animations (static sprites only)
- ❌ Navigation mesh integration (defer to movement/pathfinding work)

**Done When**:
- **Core refactored**: TerrainDefinition with CanPass/CanSeeThrough replaces TerrainType enum (breaks old ColorRect scene - acceptable)
- **Infrastructure created**: GodotTerrainRepository loads 4 terrain types from TileSet (floor, walls, smoke, tree)
- **TileSet configured**: Tree tile has can_pass=false, can_see_through=false - **COMPLETE**
- **GridTestScene upgraded in-place**: TileMapLayer replaces ColorRect terrain layer (no duplicate scene created)
- **Walls auto-tile seamlessly** via terrain_set_0 (9 bitmask patterns) - **VALIDATED WORKING**
- Trees replace interior walls (Y=15, X=5-9) - provides visual variety
- TileSet is SOURCE OF TRUTH for ALL terrain properties (no hardcoded logic in Core)
- FOV/fog of war still works (ColorRect overlay preserved, only terrain layer replaced)
- All 359 tests GREEN (Core refactoring maintains behavior)
- Scene looks pixel-art polished instead of ColorRect prototype
- Movement, pathfinding, FOV all functional with new rendering
- Git history shows 4 incremental commits (rollback safety at each step)

**Dependencies**: None (VS_009 pattern already proven)

**Risks**:
- **MEDIUM**: Core refactoring breaks existing tests (mitigation: incremental refactoring, run tests after each phase)
- ~~**CRITICAL**: Floor tile `is_opaque=true` breaks FOV~~ - **RESOLVED: can_see_through=true configured**
- ~~**MEDIUM**: Terrain set configuration complexity~~ - **RESOLVED: Autotiling validated working**
- **LOW**: TileSet custom data API (mitigation: VS_009 pattern proven, same approach)
- **LOW**: FOV overlay integration (mitigation: keep ColorRect approach, proven working)

**Tech Lead Decision** (2025-10-05 00:32):
- **SCOPE SIMPLIFIED: PCG removed** - Focus on visual upgrade + architecture refactoring only
- **Rationale**: TileSet SSOT + TileMapLayer rendering provides immediate visual improvement without PCG complexity
- **Evidence**: Autotiling validated, custom data layers configured (name, can_pass, can_see_through)
- **Size revised**: M (1-2 days) - removed 2-3h PCG work, simplified to 4-phase implementation
- **Tree terrain added**: Simple tile (5:5) replaces interior walls, adds visual variety (no autotiling needed)
- **In-place upgrade decision (YAGNI principle)**:
  - NO scene duplication - Core refactoring breaks ColorRect scene anyway (TerrainType enum deletion)
  - Maintaining two scenes = doubled maintenance for every future feature (VS_020 Combat, etc.)
  - Git branches provide better safety net than scene duplication (rollback any commit)
  - ColorRect prototype has zero value after TileMapLayer works (nobody will use it)
  - Incremental commits (4 steps) provide rollback points during migration
- **Actor TileSet pattern clarified**: Use `test_actor_tileset.tres` as sprite catalog for Sprite2D nodes (NOT a second TileMapLayer)
  - TileMapLayer = static grid-aligned terrain (walls, floors, trees)
  - Sprite2D = dynamic entities with smooth movement (player, enemies)
  - Both follow SSOT pattern but consumed differently (SetCell vs RegionRect)
- **Benefits**: Professional pixel-art look, designer-editable terrain, consistent VS_009 architecture, no dead code maintenance
- **PCG deferred**: Can add as future VS after visual foundation established
- **Next steps**: Dev Engineer reviews VS_009 GodotItemRepository, implements Phases 1-4 with incremental commits

---

### VS_019_FOLLOWUP: Fix Wall Autotiling (Godot Terrain API)
**Status**: Proposed | **Owner**: Dev Engineer | **Size**: S (<4h) | **Priority**: Important (Polish)
**Markers**: [VISUAL-POLISH] [TECHNICAL-DEBT]

**What**: Implement proper wall autotiling using Godot's SetCellsTerrainConnect API for seamless corner rendering

**Why**:
- Current walls show individual brick tiles at corners instead of seamless connected walls
- TileSet terrain_set_0 is already configured with bitmask patterns (9 variants validated)
- VS_019 uses SetCell() which bypasses terrain system autotiling logic

**How**:
- **Problem**: SetCell(pos, sourceId, atlasCoords) places tiles manually, ignoring terrain bitmask
- **Solution**: Collect all wall positions FIRST, then call SetCellsTerrainConnect(allWallCells, terrainSet: 0, terrain: 0)
- **Pattern**: Batch processing required - Godot analyzes neighbors for entire set, not individual cells
- **Implementation**:
  ```csharp
  // Collect all wall positions during initialization
  var wallPositions = new Godot.Collections.Array<Vector2I>();
  for (edges...) {
      await _mediator.Send(new SetTerrainCommand(pos, "wall_stone"));
      wallPositions.Add(new Vector2I(pos.X, pos.Y));
  }
  // THEN apply autotiling to entire set
  _terrainLayer.SetCellsTerrainConnect(wallPositions, terrainSet: 0, terrain: 0);
  ```

**Done When**:
- Wall corners render seamlessly (L-shaped corners, T-junctions, etc.)
- Border walls connect properly at all 4 corners
- Interior tree obstacles (Y=15) still render correctly
- Floor, grass, trees unaffected (direct SetCell still used)

**Dependencies**: None (VS_019 complete, autotiling is polish)

**Effort**: 2-3 hours (refactor InitializeGameState terrain loop, test corner rendering)

---

### VS_020: Basic Combat System (Attacks & Damage)
**Status**: Approved | **Owner**: Tech Lead → Dev Engineer | **Size**: M (1-2 days) | **Priority**: Important
**Markers**: [PHASE-1-CRITICAL] [BLOCKING]

**What**: Attack commands (melee + ranged), damage application, range validation, manual dummy enemy combat testing

**Why**:
- **BLOCKS Phase 1 validation** - cannot prove "time-unit combat is fun" without attacks
- Completes core combat loop: Movement → FOV → Turn Queue → **Attacks** → Health/Death
- Foundation for Enemy AI (VS_011)

**How**:
- **Phase 1 (Domain)**: `Weapon` value object (damage, time cost, range, weapon type enum)
- **Phase 2 (Application)**: `ExecuteAttackCommand` (attacker, target, weapon), range validation (melee=adjacent, ranged=FOV line-of-sight), integrates with existing `TakeDamageCommand` from VS_001
- **Phase 3 (Infrastructure)**: Attack validation service (checks adjacency for melee, FOV visibility for ranged)
- **Phase 4 (Presentation)**: Attack button UI (enabled when valid target in range), manual dummy control (WASD for enemy, Arrow keys for player)

**Scope**:
- ✅ Melee attacks (adjacent tiles only, 8-directional)
- ✅ Ranged attacks (FOV line-of-sight validation, max range)
- ✅ Weapon time costs (integrate with TurnQueue from VS_007)
- ✅ Death handling (actor reaches 0 health → removed from queue)
- ❌ Enemy AI (dummy is manually controlled for testing)
- ❌ Multiple weapon types (just "sword" and "bow" for testing)
- ❌ Attack animations (instant damage for now)

**Done When**:
- Player can attack dummy enemy (melee when adjacent, ranged when visible)
- Dummy can attack player (manual WASD control)
- Health reduces on hit, actor dies at 0 HP
- Combat feels tactical (positioning matters for range/line-of-sight)
- Time costs advance turn queue correctly
- Can complete full combat: engage → attack → victory/defeat

**Dependencies**: VS_007 (Turn Queue - ✅ complete)
**Next Step**: After combat feels fun → VS_011 (Enemy AI uses these attack commands)

---

### VS_021: Internationalization (i18n) Infrastructure
**Status**: Approved | **Owner**: Tech Lead → Dev Engineer | **Size**: S-M (4-8 hours) | **Priority**: Important
**Markers**: [ARCHITECTURE] [TECHNICAL-DEBT-PREVENTION]

**What**: Godot i18n infrastructure with translation key discipline (architecture only, English translations only for now)

**Why**:
- Prevents catastrophic late-stage refactoring (10x cost if deferred)
- Aligns perfectly with Clean Architecture (Domain returns keys, Presentation calls `tr()`)
- Near-zero ongoing cost (just habit like using `Result<T>`)
- **Defers actual translation work** until Phase 1 validated (smart risk management)

**How**:
- **Phase 1**: Create `translations/` folder, configure Godot Project Settings → Localization, create `en.csv` with English keys
- **Phase 2**: Refactor existing UI text to use `tr("UI_*")` keys (buttons, labels in test scenes)
- **Phase 3**: Add `name_key` to Actor entity (e.g., `"ACTOR_PLAYER"`, `"ACTOR_GOBLIN"`), update logging to use `tr(actor.name_key)`
- **Phase 4**: Document pattern in CLAUDE.md (all new UI must use keys, Domain returns keys not strings)

**Scope**:
- ✅ Translation file structure (`translations/en.csv`)
- ✅ Godot localization configuration
- ✅ Refactor existing UI to use keys
- ✅ Actor display names use keys (fixes "random code" in logs)
- ✅ Architectural pattern documented
- ❌ Chinese/Japanese translations (deferred until Phase 1 validated)
- ❌ Pluralization support (`tr_n()` - add when needed)
- ❌ Cultural adaptation (future work)

**Done When**:
- All UI text uses `tr("UI_KEY")` pattern
- Logs show `"Player attacks Goblin"` instead of `"Actor_a3f attacks Actor_b7d"`
- `en.csv` contains all current keys
- CLAUDE.md documents i18n discipline for future work
- Zero hardcoded user-facing strings in codebase
- Adding Chinese later = just create `zh_CN.csv` (no code changes)

**Dependencies**: None (can be done parallel with VS_019/020)
**Integration**: Works with VS_020 (attack messages use keys)

---

*Recently completed and archived (2025-10-04 19:35):*
- **VS_007**: Time-Unit Turn Queue System - Complete 4-phase implementation with natural mode detection, 49 new tests GREEN, 6 follow-ups complete. ✅ (2025-10-04 17:38)

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*

**No items in Ideas section!** ✅

*Future work is tracked in [Roadmap.md](../02-Design/Game/Roadmap.md) with dependency chains and sequencing.*

---

## 📋 Quick Reference

**Priority Decision Framework:**
1. **Blocking other work?** → 🔥 Critical
2. **Current milestone?** → 📈 Important  
3. **Everything else** → 💡 Ideas

**Work Item Types:**
- **VS_xxx**: Vertical Slice (new feature) - Product Owner creates
- **BR_xxx**: Bug Report (investigation) - Test Specialist creates, Debugger owns
- **TD_xxx**: Technical Debt (refactoring) - Anyone proposes → Tech Lead approves

*Notes:*
- *Critical bugs are BR items with 🔥 priority*
- *TD items need Tech Lead approval to move from "Proposed" to actionable*



---



---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*