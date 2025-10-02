# Darklands Development Backlog


**Last Updated**: 2025-10-03 02:29 (Dev Engineer: VS_018 Phase 1 - Critical bug fixed, weapon swap pending design decision)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 004
- **Next TD**: 003
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

*Recently completed and archived (2025-10-02):*
- **VS_009**: Item Definition System - TileSet metadata-driven catalog, 57 tests, auto-discovery, TextureRect rendering ✅ (2025-10-02 23:01)
- **VS_008**: Slot-Based Inventory System - 20-slot backpack, add/remove operations, 23 tests, PR #84 merged ✅ (2025-10-02 12:10)
- **TD_002**: Debug Console Scene Refactor - Scene-based UI, pause isolation, ILogger integration ✅ (2025-10-01 20:37)
- **VS_006**: Interactive Movement System - A* pathfinding, hover preview, fog of war, ILogger refactor ✅ (2025-10-01 17:54)
- **VS_005**: Grid, FOV & Terrain System - Custom shadowcasting, 189 tests, event-driven integration ✅ (2025-10-01 15:19)
- **VS_001**: Health System Walking Skeleton - Architectural foundation validated ✅
- **BR_001**: Race Condition - Fixed with WithComponentLock pattern ✅
- **BR_002**: Fire-and-Forget Events - Fixed with async/await ✅
- **BR_003**: Heal Button CQRS Bypass - Removed per YAGNI ✅
- **TD_001**: Architecture Enforcement Tests - 10 tests enforcing all 4 ADRs ✅
- *See: [Completed_Backlog_2025-10.md](../07-Archive/Completed_Backlog_2025-10.md)*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

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
  - Animation cleanup: Stop Tween gracefully when interrupted

**Interruption Triggers**:
1. **Enemy Detection** (Critical): New enemy appears in FOV → pause movement
2. **Discovery Events** (Important): Step on tile reveals loot/trap → pause for investigation
3. **Dangerous Terrain** (Future): About to enter fire/acid → confirm before proceeding

**Scope**:
- ✅ Auto-pause when enemy enters FOV during movement
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



### VS_018: Spatial Inventory System (Multi-Phase) ⭐ **PHASE 1 - BUG FIXED, SWAP PENDING**

**Status**: Phase 1 98% Complete - Critical bug fixed, weapon swap design decision needed
**Owner**: Dev Engineer → Product Owner (for swap scope decision)
**Size**: XL (12-16h across 4 phases, Phase 1 = 4-5h)
**Priority**: Important (Phase 2 foundation - enhances VS_008 slot-based inventory)
**Depends On**: VS_008 (Slot-Based Inventory ✅), VS_009 (Item Definitions ✅)
**Markers**: [ARCHITECTURE] [UX-CRITICAL] [BACKWARD-COMPATIBLE]

**What**: Upgrade slot-based inventory (VS_008) to spatial grid system with drag-drop, multi-container support, type filtering, and progressive complexity (Phase 1: interactions → Phase 4: complex shapes)

**Why**:
- **User Experience**: Drag-drop is more intuitive than "Add/Remove" buttons (matches Diablo 2, Resident Evil, Tarkov UX expectations)
- **Multi-Container**: Backpack + weapon slot + equipment slots (each with different rules)
- **Type Safety**: Weapon slots only accept weapons (prevents invalid placements)
- **Foundation**: Spatial grid enables item weight distribution, container nesting (future VS_013)
- **Incremental**: 4 phases from simple (1×1 items) → complex (L-shapes + rotation)

**How** (4-Phase Incremental Design):

**Phase 1: Interaction Mechanics** (4-5h) **← START HERE**
- **Goal**: Validate drag-drop UX feels good before adding spatial complexity
- **Domain**:
  - `GridPosition` value object (X, Y coordinates)
  - Enhance `Inventory` entity: Add `_itemPositions: Dictionary<ItemId, GridPosition>`, `_gridWidth`, `_gridHeight`, `ContainerType` enum
  - Keep existing `AddItem()` for backward compatibility (auto-places at first free position)
  - New methods: `PlaceItemAt()`, `CanPlaceAt()`, `GetItemPosition()`, `IsPositionFree()`
  - Type filtering: `ContainerType.WeaponOnly` rejects non-weapon items
- **Application**:
  - Commands: `PlaceItemAtPositionCommand`, `MoveItemBetweenContainersCommand`, `RemoveItemAtPositionCommand`
  - Queries: `CanPlaceItemAtQuery`, `GetItemAtPositionQuery`
  - Enhanced `InventoryDto`: Add GridWidth, GridHeight, ContainerType, ItemPlacements dictionary
- **Infrastructure**: No changes (InMemoryInventoryRepository already stores Inventory entities)
- **Presentation** (Focus):
  - `SpatialInventoryTestScene.tscn`: 2 backpacks (different sizes) + 1 weapon slot + item spawn palette
  - `SpatialInventoryContainerNode.cs`: Renders grid, handles drag-drop via Godot's `_GetDragData`/`_CanDropData`/`_DropData`
  - `DraggableItemNode.cs`: Visual drag preview, source inventory tracking
  - `ItemTooltipNode.cs`: Shows item name on hover (simple Label overlay)
  - **All items treated as 1×1** (multi-cell in Phase 2)
- **Tests**: 25-30 tests
  - Domain: GridPosition validation, PlaceItemAt collision (1×1), type filtering
  - Application: Command handlers (placement, movement, removal), query validation
  - Manual: Drag item from Backpack A → Backpack B, drag weapon → weapon slot (success), drag potion → weapon slot (rejected)

**Phase 2: Multi-Cell Rectangles** (3-4h)
- **Goal**: Items occupy Width×Height cells (2×1 sword takes 2 adjacent slots)
- **Domain**: Enhance collision detection to check all occupied cells
- **Application**: Update `CanPlaceItemAtQuery` to validate rectangle fits
- **Presentation**: Render items spanning multiple cells, snap to grid
- **NO rotation yet** (sword is always 2×1, cannot become 1×2)
- **Tests**: 15-20 additional tests (multi-cell collision, boundary checks)

**Phase 3: Rotation Support** (2-3h)
- **Goal**: Rotate items 90° (2×1 sword → 1×2 sword)
- **Domain**: Add `Rotation` enum (Degrees0, Degrees90, Degrees180, Degrees270), swap Width↔Height logic
- **Application**: `RotateItemCommand`, rotation state persistence
- **Presentation**: Right-click or R key to rotate, visual rotation animation
- **Tests**: 10-15 tests (rotation state, dimension swapping, collision after rotation)

**Phase 4: Complex Shapes** (3-4h)
- **Goal**: L-shapes, T-shapes via bool[] masks (Tetris-style)
- **Domain**: `ItemShape` value object (bool[,] grid), per-cell collision
- **Infrastructure**: Shape metadata storage (JSON file: `data/item_shapes/*.json` OR TileSet custom data string encoding)
- **Presentation**: Render complex shapes, rotation affects shape orientation
- **Tests**: 20-25 tests (shape parsing, complex collision, L-shape rotation)

**Backward Compatibility (CRITICAL)**:
- ✅ VS_008 tests MUST still pass (existing `AddItem()` API preserved)
- ✅ `Inventory.Create(id, capacity)` → maps to `Inventory.Create(id, gridWidth: capacity/4, gridHeight: 4)`
- ✅ Existing slot-based scenes (InventoryPanelNode) continue working
- ✅ New overload: `Inventory.Create(id, gridWidth, gridHeight, type)` for spatial containers

**Scope** (Phase 1 ONLY):
- ✅ Drag-drop between 2 backpacks (different grid sizes: 10×6 and 8×8)
- ✅ Weapon slot (1×4 grid) with type filter (rejects non-weapon items)
- ✅ Hover tooltip displays item name
- ✅ Visual feedback: Valid drop (green highlight), invalid drop (red highlight)
- ✅ Item spawn palette (test UI to create items for dragging)
- ✅ All items treated as 1×1 (multi-cell deferred to Phase 2)
- ❌ Multi-cell placement (Phase 2)
- ❌ Item rotation (Phase 3)
- ❌ Complex shapes (Phase 4)
- ❌ Container nesting/bags (future VS_013)
- ❌ Weight-based capacity limits (future feature)

**Done When** (Phase 1):
- ✅ Domain tests: 15 tests passing (<100ms)
  - GridPosition validation (negative coords fail)
  - PlaceItemAt with 1×1 collision detection
  - Type filtering (weapon slot rejects "item" type)
  - Backward compat: AddItem() auto-places at first free position
- ✅ Application tests: 12 tests passing (<500ms)
  - PlaceItemAtPositionCommandHandler (success, collision, out-of-bounds)
  - MoveItemBetweenContainersCommandHandler (inter-container movement)
  - CanPlaceItemAtQuery (returns true/false for validation)
- ✅ Manual UI test (SpatialInventoryTestScene.tscn):
  - Drag item from palette → Backpack A → Item appears at grid position
  - Drag item from Backpack A → Backpack B → Item moves successfully
  - Drag weapon from palette → Weapon slot → Success (green highlight)
  - Drag potion from palette → Weapon slot → Rejected (red highlight + error message)
  - Hover over item → Tooltip shows item name
  - Drag item to occupied cell → Red highlight, drop fails
- ✅ VS_008 regression tests: All 23 existing tests still pass (backward compatibility verified)
- ✅ Architecture tests: Zero Godot dependencies in Darklands.Core (ADR-002 compliance)

**Tech Lead Decision** (2025-10-02 23:37):
- **Phased approach validated**: Interaction mechanics (Phase 1) → Multi-cell (Phase 2) → Rotation (Phase 3) → Shapes (Phase 4)
- **Backward compatibility**: VS_008 slot-based API preserved, spatial is additive evolution
- **Container type filtering**: Enum-based system (General/WeaponOnly/ConsumableOnly) extensible for future slots
- **Drag-drop architecture**: Godot's built-in `_GetDragData`/`_CanDropData`/`_DropData` (simpler than custom mouse tracking)
- **GridPosition as value object**: Shared primitive in Domain/Common (reusable for map positions, crafting grids)
- **Shape metadata strategy**: Defer to Phase 4, choose JSON vs TileSet string encoding based on designer feedback
- **Migration risk**: LOW - backward compat overloads + existing tests ensure no VS_008 regressions
- **Phase 1 focus**: UX validation (does drag-drop feel better than buttons?) before multi-cell complexity
- **Blocks**: VS_010 (Stacking - needs spatial positions), VS_013 (Containers - nested grids)
- **Next steps**: Await Product Owner approval, then hand off Phase 1 to Dev Engineer

**Dev Engineer Progress** (2025-10-03 00:36 - Core + Tests):
- ✅ **Phase 1 Core Implementation Complete** (260/260 tests passing)
- ✅ Domain Layer: GridPosition, ContainerType enum, spatial Inventory entity (Dictionary-based storage)
- ✅ Application Layer: Commands (PlaceItemAt, MoveItemBetween), Queries (CanPlaceItemAt), enhanced InventoryDto
- ✅ Backward Compatibility: All 23 VS_008 tests pass, Create(capacity) → square root grid mapping (20→5×4, 100→10×10)
- ✅ Type Filtering: WeaponOnly containers reject non-weapon items (validated in handlers)
- ✅ Build: Both Core + Godot projects compile successfully
- 📊 **Test Coverage**:
  - Domain: 154 tests (<100ms) including 13 new spatial tests
  - Application: 106 tests (<500ms) including 9 new command/query tests
  - Total: 260 Phase 1+2 tests passing, 313/314 full suite (1 pre-existing flaky test)
- 🎯 **Architecture Wins**:
  - Single source of truth: Dictionary primary storage, Items property computed for backward compat
  - Cross-aggregate orchestration: Type filtering in Application handlers (Domain stays decoupled)
  - Repository enhancement: SaveAsync now handles entity replacement (no-op → update dictionary)

**Dev Engineer Progress** (2025-10-03 01:55 - Drag-Drop Partially Working):
- ✅ **Phase 1 Core Complete** (260/260 tests passing)
  - Domain: GridPosition, ContainerType, spatial Inventory entity
  - Application: Commands (PlaceItemAt, MoveItemBetween), Queries (CanPlaceItemAt)
  - Infrastructure: InMemoryInventoryRepository with RegisterInventoryForActor
- ✅ **Phase 1 Presentation Built**:
  - SpatialInventoryTestScene.tscn: 3 containers (2 backpacks + weapon slot)
  - SpatialInventoryContainerNode: Grid rendering, drag-drop implementation
  - EquipmentSlotNode: Single-slot design for weapon (not grid-based)
  - Item population: 4 test items pre-loaded (2 weapons in Backpack A, 2 items in Backpack B)
- 🎨 **Visual Enhancements Added**:
  - Color coding: Weapons = blue (0.2, 0.4, 0.8), Items = green (0.2, 0.8, 0.4)
  - Cell highlighting: Occupied cells visually distinct from empty cells
  - Mouse filter enabled: MouseFilter.Stop on all grid cells
- 📊 **Comprehensive Logging Added**:
  - _GetDragData: Logs click position, grid position, item lookup
  - _CanDropData: Logs drop validation checks
  - _DropData: Logs GUID parsing, command dispatch
  - Item type queries: Logs type resolution for color coding
- ✅ **DRAG-DROP BREAKTHROUGH** (BR_004 Resolution):
  - **Root Cause Found**: Mouse filter hierarchy blocking events at multiple levels
  - **Fix 1**: Scene placeholders (.tscn) - Removed `mouse_filter = 2` (IGNORE) from BackpackA/BackpackB nodes
  - **Fix 2**: Container internals - Changed VBoxContainer/GridContainer from IGNORE → PASS
  - **Fix 3**: Grid cells - Set `MouseFilter = Stop` on Panel cells to receive clicks
  - **Fix 4**: Grid rebuild bug - Only create cells once (check `GetChildCount() == 0`)
- 🎉 **Working Features**:
  - ✅ Backpack A ↔ Backpack B drag-drop functional
  - ✅ Color coding working (weapons=blue, items=green)
  - ✅ 4 test items pre-loaded and visible
  - ✅ Capacity counts updating correctly (e.g., "Backpack B (3/64)")
  - ✅ Container expansion bug fixed (cells no longer duplicate on reload)
  - ✅ Comprehensive logging showing drag events flowing correctly
- ✅ **CROSS-CONTAINER SYNC FIX** (2025-10-03 02:11):
  - **Bug**: Ghost items persisted in source container after drag-drop to another container
  - **Root Cause**: Only target container refreshed display, source container unaware of change
  - **Solution**: Signal-based broadcast refresh system
    - `SpatialInventoryContainerNode` emits `InventoryChanged` signal after successful move
    - `SpatialInventoryTestController` subscribes to all container signals
    - On signal: Calls `RefreshDisplay()` on ALL containers (broadcast sync)
  - **Architecture**: Decoupled (containers don't know about each other), extensible (add containers via subscription)
  - **Result**: Both source and target containers update correctly after cross-container drag-drop ✅
  - **Commits**: fd854c6 (fix), c9aff62 (weapon slot + logging cleanup)
- ✅ **WEAPON SLOT DRAG-DROP FIX** (2025-10-03 02:16):
  - **Problem**: EquipmentSlotNode not receiving drag-drop events despite identical code
  - **Solution**: Replaced with 1×1 `SpatialInventoryContainerNode` (pattern reuse)
  - **Result**: Weapon slot now functional with type filtering ✅
  - **Commit**: c9aff62
- 🐛 **CRITICAL BUG FIXED - Data Loss** (2025-10-03 02:23):
  - **Severity**: DATA LOSS - Items disappeared when dragging non-weapon to weapon slot
  - **Bug Sequence**:
    1. User drags potion → weapon slot (type mismatch)
    2. `_CanDropData` returns true (only checked position, not type)
    3. `MoveItemBetweenContainersCommand` removes item from source
    4. Type validation fails (WeaponOnly rejects potion)
    5. Command returns failure, item never added to target
    6. **Result**: Item removed from source, not in target = LOST 💥
  - **Root Cause**: Type validation happened AFTER `RemoveItem()` call
  - **Fix - Defense in Depth**:
    - **Presentation Layer**: `_CanDropData` now validates type (cache + MediatR fallback)
    - **Application Layer**: Type validation moved BEFORE `RemoveItem()`
  - **Testing**:
    - ✅ 261 Core tests passing (260 → 261 with new regression test)
    - ✅ `Handle_FailedTypeValidation_ShouldNotRemoveItemFromSource` verifies data preservation
  - **Commits**: b502561 (fix), dcfdb23 (regression test)
- 🔄 **REMAINING WORK** (Final 2%):
  - **Feature**: Weapon swap functionality
  - **Current Behavior**: Drag weapon onto occupied weapon slot → Red highlight, drop fails
  - **User Request**: Support swapping (drag weapon onto equipped weapon → swap positions)
  - **Additional Request**: 4+ item types for visual distinction (more color coding)
- ❓ **DESIGN DECISION NEEDED** (Next Session):
  - **Question**: What should swap scope be?
    - **Option A**: Weapon slot ↔ Weapon slot only (equipment-specific swap)
    - **Option B**: Any occupied position → Any occupied position (universal swap)
    - **Option C**: Equipment slots support swap, backpacks do not (hybrid)
  - **Considerations**:
    - **UX Precedent**: Diablo 2, Resident Evil support equipment swaps
    - **Complexity**: Universal swap requires atomicity (what if 2nd placement fails?)
    - **Phase Scope**: Is swap Phase 1 (core UX) or Phase 2 (enhancement)?
- ⏭️ **Next Session Tasks**:
  1. **Manual test data loss fix** in Godot (verify items no longer disappear)
  2. **Design decision**: Weapon swap scope (Options A/B/C above)
  3. **Implement swap** (if approved for Phase 1)
  4. **Add item type variety** (4+ types with color coding)
  5. **Final Phase 1 validation** (all acceptance criteria met)

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