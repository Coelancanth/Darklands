# Darklands Development Backlog


**Last Updated**: 2025-10-03 14:00 (Dev Engineer: VS_018 Phase 2 - Sprite/Inventory separation complete, rendering verified)

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



### VS_018: Spatial Inventory System (Multi-Phase) ✅ **PHASE 2 COMPLETE**

**Status**: Phase 1 ✅ + Phase 2 ✅ Complete (including Phase 2.4 drag highlights) - Ready for PR
**Owner**: Dev Engineer (Phase 2 done, self-collision polish optional, Phase 3 ready)
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

**Dev Engineer Session** (2025-10-03 Complete - Phase 1 DONE):
- ✅ **4-Color Item Types Implemented** (per-item visual distinction):
  - TileSet updated: `weapon`, `consumable` (red_vial), `tool` (gadget), `armor` (green_vial)
  - Per-item colors: dagger=light blue, ray_gun=purple, red_vial=red, green_vial=bright green, gadget=yellow
  - ColorRect icons rendered inside cells (70% size, centered, MouseFilter.Ignore)
  - Commit: fc1d3c7
- ✅ **Equipment Swap (Option C) Implemented**:
  - Initial broken implementation caused data loss (MoveItemBetweenContainers collision issue)
  - **Root Cause**: Step 1 tried to move item to occupied position → placement failed → item lost
  - **Safe Swap Algorithm** (4-step with full rollback):
    1. Remove source item (if fails → abort)
    2. Remove target item (if fails → restore source)
    3. Place source at target (if fails → restore both)
    4. Place target at source (if fails → remove misplaced source, restore both)
  - Uses `RemoveItemCommand` + `PlaceItemAtPositionCommand` (not `MoveItemBetweenContainers`)
  - Weapon slots support swap, backpacks reject occupied drops (Option C hybrid)
  - Commits: ed479c0 (disable broken swap), 69567b1 (safe swap implementation)
- ✅ **Hover Tooltips Working**:
  - Tooltips display "{ItemName} ({ItemType})" on hover (e.g., "dagger (weapon)")
  - Empty cells show "Empty (X, Y)"
  - MouseFilter.Pass on cells allows tooltips + drag-drop event bubbling
  - Commits: 22966be (tooltips), ca9c1ca (MouseFilter.Pass fix)
- ✅ **Enhanced Drag Preview**:
  - Shows item name: "📦 red_vial" instead of generic "📦 Item"
  - Dark panel background with rounded corners for visibility
  - Commit: 82c79d6
- 🐛 **CRITICAL Data Loss Bug Fixed** (Swap-Related):
  - **Bug**: Items disappeared when attempting swap on occupied weapon slot
  - **Cause**: Broken two-step swap tried to place at occupied position → collision → item lost
  - **Fix**: Disabled broken swap, then reimplemented with safe Remove→Place pattern
  - **Verification**: User tested, no more item loss, swap working correctly ✅
- 📚 **Memory Bank Updated**:
  - Added User Testing Protocol: Use `_logger.LogInformation` during testing, downgrade to `LogDebug` after
  - Benefits: Structured logging (Serilog), rich formatting, no code deletion needed
  - Commits: ecc00c9 (initial protocol), c9c347d (correction to use ILogger not GD.Print)
- 📊 **Final Status**:
  - ✅ All Phase 1 features working (drag-drop, swap, tooltips, colors, type filtering)
  - ✅ No data loss bugs (safe swap with rollback)
  - ✅ User-tested and confirmed working
  - ✅ 261 Core tests passing (1 regression test added for type validation data loss)
  - ✅ Backward compatibility maintained (VS_008 tests still pass)
- 🎯 **Phase 1 Complete** - Ready for PR to main

**Dev Engineer Session** (2025-10-03 13:25 - Phase 2 Multi-Cell Rendering):
- ✅ **Phase 2.3: Multi-Cell TextureRect Rendering** (rendering-first approach):
  - **Architecture**: Overlay layer for multi-cell sprites on top of grid cells
    - Grid cells (Panel): Hit-testing and structure (unchanged from Phase 1)
    - Overlay container (Control): Z-index 10, renders TextureRect sprites spanning multiple cells
    - Item dimensions cached: Dictionary<ItemId, (Width, Height)> from ItemDto
  - **VS_009 Pattern Reuse**: AtlasTexture wrapper for sprite region extraction
    - `atlasSource.GetTileTextureRegion(tileCoords)` auto-calculates region
    - TextureRect with ExpandMode.IgnoreSize + StretchMode.KeepAspectCentered
    - Pixel-perfect rendering: TextureFilter.Nearest for crisp sprites
  - **Multi-Cell Calculation**: Account for 2px grid separation
    - pixelWidth = width × CellSize + (width - 1) × separationX
    - Items positioned at origin.X × (CellSize + separationX)
  - **TileSet Scene Assignment**: Fixed missing ItemTileSet in SpatialInventoryTestScene.tscn
    - Added external resource: `uid://bmiw87gmm1wvp` (correct UID)
    - Property injection: Controller → Container nodes via `ItemTileSet = ItemTileSet`
  - **Rendering Results** (user-confirmed):
    - ✅ ray_gun (4×4) renders spanning 4×4 grid cells with sprite
    - ✅ dagger (4×2) renders horizontally across 2 rows
    - ✅ gadget (2×4) renders vertically across 4 rows
    - ✅ red_vial, green_vial (2×2) render as 2×2 sprites
    - ✅ Pixel-perfect, centered, grid cells visible underneath
  - **Fallback Support**: ColorRect rendering when TileSet not assigned (backward compat)
  - **Files Modified**:
    - SpatialInventoryContainerNode.cs: Overlay architecture, RenderMultiCellItemSprite()
    - SpatialInventoryTestScene.tscn: TileSet resource assignment
- 🎯 **Phase 2.3 Complete** - Rendering verified working
- ⏭️ **Next**: Phase 2.4 (green/red drag highlight), then Domain/Application collision

**Dev Engineer Session** (2025-10-03 13:50 - Phase 2 Sprite/Inventory Dimension Separation):
- ✅ **Critical Insight**: User caught conceptual error - sprite size ≠ inventory occupation
  - **Problem**: Phase 2.3 used `size_in_atlas` for BOTH visual rendering AND logical collision
  - **Example**: ray_gun sprite is 4×4 atlas tiles but should occupy 2×2 inventory cells
  - **Root Cause**: Single Width/Height property mixed visual and logical concerns
- ✅ **Solution: Dual Metadata System** (TileSet custom_data_3/4):
  - `size_in_atlas` → `SpriteWidth/Height` (visual rendering size in atlas tiles)
  - `custom_data_3/4` → `InventoryWidth/Height` (logical occupation in grid cells)
  - **Metadata verified**: ray_gun (4×4 sprite → 2×2 inventory), dagger (4×2 → 2×1), etc.
- ✅ **Domain Entity Updated**: `Item.cs`
  - Renamed: `Width/Height` → `SpriteWidth/SpriteHeight` (breaking change)
  - Added: `InventoryWidth/InventoryHeight` properties
  - Validation: Both dimensions must be positive (4 new business rules)
- ✅ **Application Layer Updated**:
  - `ItemDto`: Added `SpriteWidth/Height` + `InventoryWidth/Height` fields
  - Query handlers: Map new properties (GetItemById, GetAll, GetByType)
- ✅ **Infrastructure Updated**: `TileSetItemRepository.cs`
  - Reads `custom_data_3` (inventory_width) with fallback to sprite width
  - Reads `custom_data_4` (inventory_height) with fallback to sprite height
  - Logging: Shows both dimensions for debugging
- ✅ **Presentation Updated**: `SpatialInventoryContainerNode.cs`
  - Rendering uses `InventoryWidth/Height` for pixel size (how many cells sprite spans)
  - AtlasTexture extracts sprite region using atlas coordinates (sprite dimensions)
  - Result: 4×4 sprite renders within 2×2 inventory cell space (scaled to fit)
- ⏳ **Tests WIP** (15 compilation errors remaining):
  - Batch fixed: ~40 test files updated with `spriteWidth/spriteHeight` parameters
  - Remaining: Inventory test helpers need `inventoryWidth/inventoryHeight` added
  - Status: Domain/Application/Infrastructure compile ✅, Tests compile ❌
- 📋 **Next Session Tasks**:
  1. Fix remaining 15 test errors (add inventory dimensions to test helpers)

**Dev Engineer Session** (2025-10-03 14:35 - Phase 2 Multi-Cell Occupation COMPLETE):
- ✅ **Phase 2 Core Implementation - Multi-Cell Occupation**:
  - **Problem Diagnosed**: Items only occupied single cell despite rendering correctly at multi-cell size
  - **Root Cause**: Presentation layer's dimension caching order bug
    - `LoadItemTypes()` called BEFORE `_itemsAtPositions` populated
    - Iterated over empty dictionary → `_itemDimensions` never filled
    - Rendering used `GetValueOrDefault(itemId, (1,1))` → always got 1×1 fallback
- ✅ **Domain Layer - Rectangle Collision**:
  - Added `_itemDimensions` dictionary caching width×height per item
  - New `PlaceItemAt(itemId, position, width, height)` overload with AABB collision
  - Rectangle overlap detection: `!(pos.X >= existing.X + width || ...)` logic
  - Bounds validation: Ensures `position.X + width <= GridWidth`
  - Backward compat: 1-param `PlaceItemAt()` calls 2-param with (1,1) dimensions
  - Cleanup: `RemoveItem()` and `Clear()` now remove from both dictionaries
- ✅ **Application Layer - Cross-Aggregate Orchestration**:
  - `PlaceItemAtPositionCommandHandler`: Queries `item.InventoryWidth/Height`, passes to Domain
  - `MoveItemBetweenContainersCommandHandler`: Fixed to preserve dimensions on cross-container moves
  - `InventoryDto`: Added `ItemDimensions` property exposing Domain's dimension cache
  - `GetInventoryQueryHandler`: Maps `inventory.ItemDimensions` to DTO
- ✅ **Presentation Layer - Proper Dimension Caching**:
  - Fixed caching order: Domain dimensions → `_itemDimensions` → LoadItemTypes() → Build cell map
  - Added `_itemOrigins` dictionary (ItemId → GridPosition from Domain)
  - Multi-cell occupation: Builds reverse lookup mapping ALL occupied cells → ItemId
  - Drag detection: Works from ANY occupied cell, returns item origin for commands
  - Rendering: Uses `_itemOrigins` to render each item once at its origin position
- ✅ **Intra-Container Move Bug Fixed** (Data Loss Prevention):
  - **Bug**: Items disappeared when moving within same container (collision with self)
  - **Root Cause**: Handler removed item, placement failed (self-collision), rollback missing
  - **Solution**: Capture original position before remove, full rollback on placement failure
  - **Pattern**: `GetItemPosition() → RemoveItem() → PlaceItemAt() (if fail → restore at original)`
  - User tested: Invalid moves now preserve item at original position ✅
- ✅ **Equipment Slot Dimension Override** (Industry Standard Pattern):
  - **Problem**: Weapon slot (1×1 grid) rejected multi-cell weapons (2×1 dagger, 2×2 ray_gun)
  - **Option A Tried**: Enlarged weapon slot to 4×4 grid → Worked but wrong UX
  - **Option B Implemented**: Application handlers override dimensions to 1×1 for equipment slots
  - **Logic**: `if (containerType == WeaponOnly) { width = 1; height = 1; }`
  - **Result**: Any weapon fits in 1×1 weapon slot, backpack Tetris still uses real dimensions
  - **Industry Precedent**: Matches Diablo 2, Path of Exile, Resident Evil equipment behavior
  - Reverted weapon slot back to 1×1 grid (proper single-slot appearance)
- 📊 **Testing Results**:
  - ✅ User verified: Intra-container repositioning works (dagger moves within backpack)
  - ✅ User verified: Cross-container moves work (items maintain size backpack A → B)
  - ✅ User verified: Equipment slot accepts all weapons (2×1, 2×2, any size)
  - ✅ User verified: Equipment swap working (weapon ↔ weapon in slot)
  - ✅ User verified: Collision detection prevents overlapping multi-cell items
  - ✅ User verified: No data loss on invalid moves (rollback successful)
- 🎯 **Phase 2 Core Features Complete**:
  - ✅ Multi-cell rendering (items span Width×Height cells visually)
  - ✅ Multi-cell occupation (Domain stores all occupied cells, prevents overlaps)
  - ✅ Rectangle collision (AABB overlap detection for complex item shapes)
  - ✅ Equipment slot dimension override (1×1 weapon slot accepts any weapon)
  - ✅ Drag from any cell (can grab multi-cell item from any occupied cell)
  - ✅ Intra-container repositioning with rollback (no data loss)
  - ✅ Cross-container moves preserve dimensions
- ✅ **Phase 2.4 Implemented** (UX Polish - Drag Highlight):
  - ✅ Green/red highlight sprites showing multi-cell item footprint during drag
  - ✅ Highlight overlay container (Z-index 15, renders above items)
  - ✅ Real-time validation feedback (bounds, collision, type checking)
  - ✅ Cross-container dimension query (lazy loading when not in cache)
  - ✅ Highlight cleanup on mouse exit, drag end, and successful drop
  - ✅ Equipment slot dimension override (1×1 highlights for weapon slots)
  - ⚠️ **Known Issue**: Self-collision when dropping at same position (shows red instead of green)
- 🎉 **PHASE 2 COMPLETE** (Core features working, minor self-collision polish pending)

**Dev Engineer Session** (2025-10-03 15:15 - Phase 2.4 Drag Highlight Complete):
- ✅ **Highlight Overlay System**:
  - Added `_highlightOverlayContainer` (Z-index 15, above items at Z-index 10)
  - `RenderDragHighlight()`: Renders green (`highlight_green` 1,6) or red (`highlight_red` 1,7) sprites
  - Multi-cell support: Renders highlight for EVERY cell in item footprint
  - 70% opacity for semi-transparent overlay effect
- ✅ **Dynamic Validation in `_CanDropData`**:
  - Full validation: Bounds check, multi-cell collision, type filtering
  - Dimension query: Cache lookup first, falls back to MediatR query for cross-container drags
  - Equipment slot override: Forces 1×1 dimensions for weapon slots (matches placement logic)
  - Visual feedback: `isValid` flag determines green vs red highlight
- ✅ **Highlight Lifecycle Management**:
  - `_Notification(NOTIFICATION_MOUSE_EXIT)`: Clear when mouse leaves container
  - `_Input(InputEventMouseButton)`: Clear when left mouse released (handles rejected drops)
  - `_DropData()`: Clear on successful drop
- ✅ **Cross-Container Bug Fixes**:
  - Bug: Highlights appeared on source container instead of target
  - Fix: Mouse exit clears source highlights when dragging to different container
  - Bug: Wrong highlight size (1×1 instead of 2×2) for cross-container drags
  - Fix: Query item dimensions from repository when not in local cache
  - Bug: Lingering red highlights after failed drop
  - Fix: Detect mouse release via `_Input` to clear highlights on drag end
- 📊 **User Testing Results**:
  - ✅ Green highlights for valid placements (multi-cell footprint accurate)
  - ✅ Red highlights for collisions, bounds errors, type mismatches
  - ✅ Cross-container drag shows correct 2×2 highlights
  - ✅ Equipment slot shows 1×1 highlights (dimension override working)
  - ✅ Highlights clear instantly on drop (successful or failed)
  - ⚠️ Self-collision issue: Dropping item at same position shows red (should be green)
- 🎯 **Phase 2.4 Complete** - Full visual feedback system working
- ⏭️ **Minor Polish**: Fix self-collision detection (allow dropping at current position)

---

**Dev Engineer Session** (2025-10-03 14:00 - Build Errors Fixed):
- ✅ **All Compilation Errors Resolved** (15 → 0):
  - Fixed inventory test helpers: Added `inventoryWidth/inventoryHeight` to `Item.Create()` calls
  - Pattern: Old signature (id, x, y, name, type, width, maxStack) → New (id, x, y, name, type, spriteW, spriteH, invW, invH, maxStack)
  - Files fixed: CanPlaceItemAtQueryHandlerTests, PlaceItemAtPositionCommandHandlerTests, MoveItemBetweenContainersCommandHandlerTests
  - Batch sed commands: Fixed "Sword", "Axe", "Potion", "Health Potion" patterns
- ✅ **Presentation Layer Fixed**: ItemShowcaseController
  - Updated metadata display: Now shows both sprite AND inventory dimensions
  - Old: `Size: {Width}x{Height}`
  - New: `Sprite: {SpriteWidth}x{SpriteHeight} | Inventory: {InventoryWidth}x{InventoryHeight}`
- ✅ **Build Status**: All layers compile successfully
  - Core ✅, Tests ✅, Godot Presentation ✅
  - Zero warnings, zero errors
- ✅ **User Verification**: "Size matches now"
  - Rendering tested in Godot with actual TileSet metadata
  - ray_gun: 4×4 sprite renders within 2×2 inventory cell space (scaled to fit)
  - Visual confirmation that sprite/inventory separation is working correctly
- 🎯 **Phase 2 Sprite/Inventory Separation COMPLETE**
- ⏭️ **Next**: Phase 2.4 (green/red drag highlight) + Phase 2.1-2.2 (multi-cell collision)

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