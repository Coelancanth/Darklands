# Darklands Development Backlog


**Last Updated**: 2025-10-04 20:46 (Tech Lead: REVISED VS_019 to use TileSet as SSOT - applies VS_009 item catalog pattern to terrain, 7-phase breakdown with Core/Infrastructure refactoring, designer-editable terrain properties, future-proof architecture)

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

*Recently completed and archived (2025-10-04):*
- **TD_003**: Separate Equipment Slots from Spatial Inventory Container - Created EquipmentSlotNode (646 lines), extracted InventoryRenderHelper (256 lines), cleaned InventoryContainerNode. All 3 phases complete, 359 tests GREEN. ✅ (2025-10-04)
- **TD_004**: Move ALL Shape Logic to Core (SSOT) - Eliminated all 7 business logic leaks from Presentation (164 lines removed, 12% complexity reduction). Fixed SwapItemsCommand double-save bug, eliminated cache-driven anti-pattern. Commits: 4cd1dbe, 49c06e6. ✅ (2025-10-04)
- **TD_005**: Persona & Protocol Updates - Updated dev-engineer.md with Root Cause First Principle, UX Pattern Recognition, Requirement Clarification Protocol. ✅ (2025-10-04)
- **BR_007**: Equipment Slot Visual Issues - Fixed 1×1 highlight override and sprite centering for equipment slots. ✅ (2025-10-04)
- *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### VS_019: TileSet-Based Visual Scene + TileSet as Terrain Catalog (SSOT) 🎨
**Status**: Approved | **Owner**: Dev Engineer | **Size**: M-L (2-2.5 days) | **Priority**: Important
**Markers**: [VISUAL-POLISH] [MOTIVATION] [RESEARCH-FIRST] [ARCHITECTURE] [REFACTORING]

**What**: Create new TileMap-based test scene using Godot terrain system, AND refactor terrain to use TileSet as SSOT (like VS_009 items catalog)

**Why**:
- Visual progress after infrastructure-heavy VS_007 (restore motivation)
- Professional appearance vs prototype ColorRect
- **Architect terrain like items (VS_009 pattern)**: TileSet = catalog, Infrastructure loads → Core domain objects
- Designer-editable terrain properties (add lava/ice/water without C# changes)
- Future-proof: Supports movement_cost, damage_per_turn, terrain effects
- Foundation for PCG work (optional stretch goal)
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

**Phase 0: Research & Risk Mitigation (1-2 hours)** - REQUIRED FIRST STEP
- Answer 5 critical questions below
- Test terrain set configuration in Godot editor (3x3 bitmask patterns)
- Review VS_009 GodotItemRepository pattern (apply to terrain)
- Identify fallback: Use terrain sets OR simple atlas coords (if too complex)
- Assess PCG effort: Attempt (4h max) OR skip entirely
- **Done When**: Can configure basic terrain set, understand `TileMapLayer.set_cell()` API, decided on terrain sets vs fallback, decided on PCG attempt

**Phase 1: Core Refactoring - TerrainDefinition Domain Model (1 hour)**
- Create `src/Darklands.Core/Features/Grid/Domain/TerrainDefinition.cs`:
  ```csharp
  public record TerrainDefinition(
      TerrainId Id,        // int or GUID
      string Name,         // "Floor", "Wall", "Smoke", "Lava"...
      bool IsPassable,     // Can actors walk through?
      bool IsOpaque        // Blocks vision?
  );
  ```
- Create `TerrainId` value object (simple int wrapper or GUID)
- Create `ITerrainRepository` interface in Application layer:
  ```csharp
  public interface ITerrainRepository
  {
      IReadOnlyDictionary<TerrainId, TerrainDefinition> LoadAllTerrains();
      TerrainDefinition GetById(TerrainId id);
  }
  ```
- **DELETE** `TerrainTypeExtensions.IsPassable()` and `.IsOpaque()` methods (replaced by TerrainDefinition properties)
- Update `GridMap` to use `TerrainDefinition` instead of hardcoded `TerrainType` enum logic
- Update FOV/pathfinding services to use `terrainDef.IsOpaque` and `terrainDef.IsPassable`
- **Done When**: Core compiles, old TerrainType extension methods deleted, all terrain logic uses TerrainDefinition properties

**Phase 2: Infrastructure - GodotTerrainRepository (1-2 hours)**
- Create `src/Darklands.Core/Features/Grid/Infrastructure/Repositories/GodotTerrainRepository.cs`
- Implement `ITerrainRepository`:
  ```csharp
  public class GodotTerrainRepository : ITerrainRepository
  {
      private readonly TileSet _tileSet;
      private readonly Dictionary<TerrainId, Vector2I> _atlasMapping;

      public IReadOnlyDictionary<TerrainId, TerrainDefinition> LoadAllTerrains()
      {
          // Iterate TileSet atlas tiles
          // Read custom data: terrain_name, is_passable, is_opaque
          // Build TerrainDefinition objects
          // Cache TerrainId → Vector2I atlas coords for rendering
      }

      public Vector2I GetAtlasCoords(TerrainId id) => _atlasMapping[id];
  }
  ```
- Register in `GameStrapper.cs`: `services.AddSingleton<ITerrainRepository, GodotTerrainRepository>()`
- **Architectural Guardrail**: Infrastructure can reference Godot (TileSet, Vector2I), Core CANNOT
- **Done When**: Repository loads terrain definitions from TileSet, Core queries repository (no Godot deps in Core)

**Phase 3: Scene Duplication (15 minutes)**
- Duplicate `GridTestScene.tscn` → `TileMapTestScene.tscn`
- Duplicate `GridTestSceneController.cs` → `TileMapTestSceneController.cs`
- Update script reference in new scene
- **Architectural Guardrail**: Do NOT modify original GridTestScene.tscn
- **Done When**: New scene runs identically to original

**Phase 4: TileSet Configuration with Custom Data (1-2 hours)**
- Create `tilemap_tileset.tres` resource (or duplicate grid_tileset.tres)
- Configure atlas source from `colored_tilemap.png`
- **Add custom data layers** (TileSet = SSOT for terrain properties):
  - `terrain_id` (int): 0, 1, 2, 3... (maps to TerrainId)
  - `terrain_name` (String): "Floor", "Wall", "Smoke"
  - `is_passable` (bool): true/false
  - `is_opaque` (bool): true/false
  - (Future: `movement_cost` (float), `damage_per_turn` (int))
- Paint custom data values for each tile in atlas
- **OPTION A (Preferred)**: Configure terrain sets for autotiling (3x3 bitmask patterns)
- **OPTION B (Fallback)**: Use simple atlas coordinates
- **Done When**: TileSet configured with full terrain properties, can manually paint terrain in editor

**Phase 5: TileMapLayer Integration (2-3 hours)**
- Replace `ColorRect[,] _gridCells` with `TileMapLayer _terrainLayer` reference
- Inject `ITerrainRepository` via ServiceLocator in `_Ready()`
- Implement coordinate helpers: `PositionToTileCoord(Position) → Vector2I`, `TileCoordToPosition(Vector2I) → Position`
- Update terrain rendering:
  ```csharp
  var terrainDef = _terrainRepo.GetById(terrainId);
  var atlasCoords = _terrainRepo.GetAtlasCoords(terrainId);
  _terrainLayer.SetCell(tileCoord, sourceId, atlasCoords);
  ```
- Update FOV overlay (keep ColorRect approach OR explore TileMapLayer modulation)
- **Done When**: Terrain renders via TileMapLayer, FOV works, fog of war works, autotiling applies (if terrain sets used)

**Phase 6: Actor Sprites (1 hour)**
- Replace `Sprite2D` nodes with atlas texture regions from `colored_tilemap.png`
- Update actor rendering: `_playerSprite.Position = _terrainLayer.MapToLocal(PositionToTileCoord(newPosition))`
- **Done When**: Player and dummy render as pixel art sprites, movement works

**Phase 7 (OPTIONAL): Basic PCG (4 hour timebox)** - SKIP IF PHASE 0 REVEALS >4H EFFORT
- Implement cellular automata algorithm (30m)
- Initialize GridMap from generated terrain using `TerrainDefinition` catalog (30m)
- Render generated map via TileMapLayer (30m)
- Test random cave-like map generation (30m)
- Add "Regenerate Map" button (30m)
- **Timebox Rule**: If ANY task exceeds estimate, STOP and skip PCG
- **Done When**: Can generate random map on scene load OR skipped (acceptable outcome)

**Scope**:
- ✅ **Core refactoring**: Create TerrainDefinition domain model, delete hardcoded IsPassable/IsOpaque extension methods
- ✅ **Infrastructure**: GodotTerrainRepository reads TileSet → creates Core domain objects (VS_009 pattern)
- ✅ **TileSet as SSOT**: Custom data layers store terrain properties (terrain_name, is_passable, is_opaque)
- ✅ Research Godot native features (terrain system, metadata, TileMapLayer)
- ✅ Duplicate GridTestScene.tscn (preserve original)
- ✅ TileSet with terrain sets (OR fallback to simple atlas coords if complex)
- ✅ **Autotiling via terrain sets** (Godot handles edge/corner matching automatically)
- ✅ TileMapLayer rendering with TerrainDefinition → visual mapping
- ✅ Sprite2D actors using tileset texture regions
- ✅ Coordinate mapping in Presentation (Core Position ↔ TileMap Vector2I)
- ⚠️ **OPTIONAL**: Basic PCG (cellular automata) if time permits (4h max)
- ❌ Animations (static sprites only)
- ❌ Advanced PCG (multi-room dungeons, BSP trees—defer to future VS)
- ❌ Navigation mesh integration (defer to movement/pathfinding work)

**Done When**:
- **Core refactored**: TerrainDefinition replaces hardcoded TerrainType logic, ITerrainRepository interface exists
- **Infrastructure created**: GodotTerrainRepository loads terrain catalog from TileSet (like VS_009 items)
- **TileSet configured**: Custom data layers define terrain properties (is_passable, is_opaque, terrain_name)
- New TileMapTestScene.tscn exists (GridTestScene.tscn unchanged)
- Scene uses Godot terrain system (terrain sets OR simple atlas coords)
- **Autotiling works** (if terrain sets used): Walls connect seamlessly
- TileSet is SOURCE OF TRUTH for ALL terrain properties (gameplay + visual)
- Core has zero hardcoded terrain logic (data-driven via TerrainDefinition)
- Player/enemies are recognizable pixel art sprites
- FOV overlay still works visually
- All 359 tests GREEN (Core refactoring maintains behavior)
- Scene looks "game-like" instead of prototype
- **OPTIONAL**: If PCG implemented, can generate random map on scene load

**Dependencies**: None (VS_009 pattern already proven)

**Risks**:
- **MEDIUM**: Core refactoring breaks existing tests (mitigation: incremental refactoring, run tests after each change)
- **MEDIUM**: PCG scope creep (mitigation: strict 4-hour timebox, mark optional, recommend cellular automata)
- **MEDIUM**: Terrain set configuration complexity (mitigation: research phase identifies tutorials, fallback to simple atlas coords)
- **LOW**: TileSet custom data API (mitigation: proven in VS_009, same pattern)
- **LOW**: Coordinate mapping bugs (mitigation: trivial X→X, Y→Y mapping, easy visual verification)

**Research Questions** (answer in Phase 0 before implementation):
1. How does VS_009 GodotItemRepository work? **Review and apply same pattern to terrain**
2. How to configure autotiling terrain sets (3x3 bitmask patterns)? Tutorial: gamedevartisan.com
3. What's the simplest PCG algorithm? **Cellular automata (30 min implementation)**
4. Terrain sets OR simple atlas coords? **Decide in Phase 0 based on complexity**
5. TerrainId as int or GUID? **Recommend int for simplicity (0=Floor, 1=Wall, 2=Smoke)**

**Tech Lead Decision** (2025-10-04 20:45):
- **APPROVED: TileSet as SSOT** - Same proven pattern as VS_009 items catalog
- **Scope expanded**: +2-3 hours for Core/Infrastructure refactoring (worth it to avoid future technical debt)
- **Benefits**: Designer-editable terrain, future-proof (lava/ice/water), consistent architecture, moddable
- **Research-first approach**: Phase 0 reviews VS_009 pattern before implementing
- **Fallback strategy**: Can use simple atlas coords if terrain sets too complex
- **PCG optional**: Strict 4h timebox, skip if not making progress (does NOT block "Done When")
- **Risk management**: Incremental refactoring with test verification after each phase
- **Next steps**: Dev Engineer executes Phase 0 research (review VS_009, test terrain sets), then proceeds with Phase 1-7

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