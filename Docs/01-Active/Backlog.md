# Darklands Development Backlog


**Last Updated**: 2025-10-03 22:33 (Dev Engineer: BR_003 - L-Shape Bug Fixed - Application layer handlers updated to use Phase 4 API)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 005
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

*Recently completed and archived (2025-10-03):*
- **BR_003**: L-Shape Collision Bug - PlaceItemAtPositionCommandHandler & MoveItemBetweenContainersCommandHandler converted width×height to rectangles, destroying L-shapes. Fixed by using `item.Shape` (Phase 4 API) in all placement paths. Root cause: Application layer handlers called backward-compatible Phase 2 signature. Impact: L-shapes now preserve 3-cell structure through placement, movement, and rollback. All 359 tests GREEN. ✅ (2025-10-03 22:33)
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



### VS_018: Spatial Inventory System (Multi-Phase) ✅ **PHASE 3 COMPLETE**

**Status**: Phase 1 ✅ + Phase 2 ✅ + Phase 3 ✅ (Ready for Phase 4)
**Owner**: Product Owner (decide on Phase 4 timing)
**Size**: XL (12-16h across 4 phases) | **Actual**: Phase 1 (6h), Phase 2 (5h), Phase 3 (6h)
**Priority**: Important (Core gameplay mechanic)
**Depends On**: VS_008 (Slot-Based Inventory ✅), VS_009 (Item Definitions ✅)
**Markers**: [ARCHITECTURE] [UX-CRITICAL] [BACKWARD-COMPATIBLE]

**What**: Tetris-style spatial inventory with drag-drop, multi-cell items, rotation, and type filtering

**Why**:
- **UX**: Drag-drop more intuitive than buttons (Diablo 2, Resident Evil, Tarkov standard)
- **Multi-Container**: Backpack + weapon slots with different validation rules
- **Type Safety**: Equipment slots only accept matching item types
- **Incremental**: 4 phases ensures each step is testable and shippable

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

**Phase 3: Rotation Support** (2-3h) ✅ **COMPLETE** (2025-10-03 19:11)
- **Goal**: Rotate items 90° (2×1 sword → 1×2 sword)
- **Domain**: ✅ `Rotation` enum, `RotationHelper`, dimension swapping, collision validation
- **Application**: ✅ `RotateItemCommand`, `MoveItemBetweenContainersCommand` with rotation
- **Presentation**: ✅ Mouse scroll during drag, sprite rotation, highlight updates, extreme transparency solution
- **Tests**: ✅ 13 new rotation tests (348/348 passing)
- **ALL FEATURES WORKING** ✅:
  1. ✅ **Rotation Persistence** (2025-10-03 19:11): Static `_sharedDragRotation` variable preserves rotation across container moves
  2. ✅ **Equipment Slot Reset** (2025-10-03 19:11): Equipment slots reset rotation to Degrees0 for visual consistency
  3. ✅ **Z-Order Rendering** (2025-10-03 19:11): Extreme transparency (25% opacity) makes highlights visible but non-obscuring
  4. ✅ **Double Rotation**: One scroll = one 90° rotation (`Pressed` check added)
  5. ✅ **Ghost Highlights**: `Free()` instead of `QueueFree()` for immediate cleanup
  6. ✅ **Drag-Drop Visual Artifact**: Direct node references via `Dictionary<ItemId, Node>`
  7. ✅ **Drag Preview Centering**: Offset container centers cursor at sprite center

**✨ Phase 3 Final Solution Summary**:
- **Core**: Static shared rotation state for cross-container drag-drop
- **UI**: Mouse scroll rotation, sprite PivotOffset rotation, extreme transparency highlights (25%)
- **Tests**: 13 new rotation tests, all passing (348/348 total)
- **Key Lessons**:
  - Godot's drag-drop state is container-local → use static variables for cross-container communication
  - Control node z-ordering unreliable → pragmatic transparency workaround (25% opacity)
  - Direct node references beat string matching (O(1) lookup, no async issues)
  - Equipment slots reset rotation to Degrees0 for standard orientation display

**Phase 4: Complex Shapes** (8h total) **← ✅ COMPLETE** (2025-10-03 21:43)
- **Goal**: L-shapes, T-shapes via coordinate-based masks (Tetris-style)
- **Status**: **100% COMPLETE** - All 359 tests GREEN, L-shape collision working!

**✅ Shape Editor Foundation COMPLETE** (2025-10-03 20:30):
- **Infrastructure**:
  - ✅ `ItemShapeResource.cs`: Godot Resource with Width/Height + int[] Cells (0=empty, 1=filled)
  - ✅ Dynamic array resize: Changing Width/Height auto-generates Cells array
  - ✅ Default behavior: All cells start checked (filled rectangle)
  - ✅ `ToEncoding()`: Converts to "rect:WxH" (optimized) or "custom:x,y;..." (coordinates)
- **Editor Plugin**:
  - ✅ `ItemShapeEditorPlugin.cs`: Custom EditorInspectorPlugin intercepts "Cells" property
  - ✅ Visual checkbox grid: Replaces flat int[] array with GridContainer of CheckBoxes
  - ✅ Click to toggle cells (1=filled, 0=empty)
  - ✅ Dynamic grid resize: Width×Height changes instantly update checkbox count
  - ✅ Designer workflow: TileSet Custom Data Layer (Type: Object) → Assign ItemShapeResource
- **Test Data**:
  - ✅ `ray_gun` configured as L-shape test case (2×2 bounding box, 3 occupied cells)
  - ✅ Cells = [1, 1, 0, 1] → Visual: `[✓][✓]` / `[ ][✓]` (L-shape)
  - ✅ Encoding: "custom:0,0;1,0;1,1" (validates complex shape end-to-end)
- **Files**: `addons/item_shape_editor/` (plugin), `assets/inventory_ref/item_sprites.tres` (L-shape config)

**✅ Core Architecture Refactor COMPLETE** (2025-10-03 21:25, 6h actual):

**1. Domain Layer** ✅:
- ✅ **ItemShape value object** (`Domain/Common/ItemShape.cs`, 194 lines)
  - `IReadOnlyList<GridPosition> OccupiedCells` (SSOT for collision)
  - `int Width, int Height` (bounding box metadata)
  - `CreateRectangle(width, height)` factory (generates all W×H cells)
  - `CreateFromEncoding(encoding, width, height)` factory (parses "rect:WxH" or "custom:x,y;...")
  - `RotateClockwise()` transformation (rotates coordinates, swaps Width↔Height)
  - **19 comprehensive tests** (rectangles, L-shapes, rotation math) - ALL GREEN (23ms)
- ✅ **Item entity refactored** (`Features/Item/Domain/Item.cs`)
  - Added `ItemShape Shape` property (SSOT)
  - Backward-compat convenience properties: `InventoryWidth => Shape.Width`, `InventoryHeight => Shape.Height`
  - Dual factories: `Create()` (legacy, rectangles) + `CreateWithShape()` (Phase 4, complex shapes)
  - **Zero breaking changes** (23 existing Item tests pass)

**2. Infrastructure Layer** ✅:
- ✅ **TileSet shape parsing** (`Infrastructure/TileSetItemRepository.cs`, +50 lines)
  - Reads `item_shape` custom data → `ItemShapeResource.ToEncoding()` → `ItemShape.CreateFromEncoding()`
  - Fallback: Legacy `inventory_width/height` → `ItemShape.CreateRectangle()`
  - Test: `ray_gun` L-shape (encoding: "custom:0,0;1,0;1,1") parses to 3 OccupiedCells

**3. Application Layer - Collision Refactored** ✅ **(CRITICAL ACHIEVEMENT)**:
- ✅ **Replaced AABB rectangle collision with OccupiedCells iteration** (`Inventory/Domain/Inventory.cs`)
  - New private method: `PlaceItemWithShape(itemId, pos, baseWidth, baseHeight, shape, rotation)`
  - Builds `HashSet<GridPosition>` of ALL occupied cells in inventory (reconstructs shapes for all items)
  - Checks each new item's OccupiedCells against existing occupied cells (**cell-by-cell, NOT bounding box!**)
  - Bounds checking: `foreach (offset in shape.OccupiedCells)` validates each cell individually
  - **Backward compatibility**: Public `PlaceItemAt(width, height, rotation)` converts to rectangle shape internally
  - **354/354 existing tests pass** (920ms) ✅

**4. Test Coverage**:
- ✅ Domain: 19 ItemShape tests (rotation math, encoding parsing, L-shape validation)
- ✅ Integration: 5 L-shape placement tests (RED - see "Remaining Work" below)
- ✅ Regression: 354 existing tests GREEN (backward compatibility verified)

**Key Architectural Victory**:
```
Rectangle (2×3): Iterates 6 OccupiedCells → occupies 6 cells ✅
L-shape (2×2 box, 3 cells): Iterates 3 OccupiedCells → occupies 3 cells only ✅
Empty cell (0,1) in L-shape: NOT in OccupiedCells → FREE for other items! ✅
```

**✅ Storage Layer Refactored** (2025-10-03 21:36):
- ✅ Replaced `_itemDimensions` with `_itemShapes: Dictionary<ItemId, ItemShape>`
- ✅ Added `ItemShapes` public property (new), kept `ItemDimensions` for backward compat (computed)
- ✅ Updated collision reconstruction to use stored shapes (preserves L-shapes!)
- ✅ Signature change: `PlaceItemWithShape(baseShape, rotatedShape)` instead of `(width, height)`

**✅ Compilation Fixes Complete** (2025-10-03 21:43):
- ✅ Fixed PlaceItemAt backward-compat overload (creates rectangle shapes)
- ✅ Fixed parameter references (shape → rotatedShape in 2 locations)
- ✅ Fixed storage line (uses baseShape parameter directly)
- ✅ Replaced all 5 _itemDimensions references with _itemShapes
- ✅ Refactored RotateItem to use OccupiedCells collision
- ✅ Fixed nullable reference warnings in TileSetItemRepository
- ✅ **Build succeeded: 0 errors, 0 warnings**
- ✅ **All 359 tests GREEN** (354 existing + 19 ItemShape + 5 L-shape + 1 new inventory test)

**Files Modified**:
- **Domain**: `ItemShape.cs` (NEW, 194 lines), `Item.cs` (refactored, +60 lines)
- **Infrastructure**: `TileSetItemRepository.cs` (+50 lines shape parsing)
- **Application**: `Inventory.cs` (collision: -50 AABB, +120 OccupiedCells)
- **Tests**: `ItemShapeTests.cs` (NEW, 19 tests), `InventoryLShapeTests.cs` (NEW, 5 tests - RED)

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

**Key Architecture Decisions** (Tech Lead, 2025-10-02):
- **Phased approach**: UX first (Phase 1) → Complexity incrementally (Phases 2-4)
- **Backward compatibility**: VS_008 API preserved, spatial additive (zero breaking changes)
- **Drag-drop**: Godot built-in system (`_GetDragData`/`_CanDropData`/`_DropData`)
- **GridPosition**: Shared value object in Domain/Common (reusable across features)
- **Type filtering**: Enum-based (extensible for future equipment slot types)

**✅ Phase 1 Complete** (2025-10-03, 6h actual):
- **Core**: GridPosition, ContainerType, spatial Inventory, Commands/Queries (261 tests passing)
- **UI**: Drag-drop working, tooltips, 4-color item types, equipment swap, type filtering
- **Lessons**:
  - Mouse filter hierarchy critical for Godot drag events (`Pass` vs `Stop` vs `Ignore`)
  - Defense-in-depth for data loss: Validate type in BOTH `_CanDropData` AND handler
  - Safe swap algorithm: Remove→Remove→Place→Place with full rollback at each step

**✅ Phase 2 Complete** (2025-10-03, 5h actual):
- **Core**: Multi-cell AABB collision, dimension override for equipment slots, intra-container rollback
- **UI**: Multi-cell TextureRect rendering (overlay architecture), green/red drag highlights
- **Lessons**:
  - **Sprite ≠ Inventory dimensions**: 4×4 sprite can occupy 2×2 grid (dual metadata critical)
  - **Equipment slot UX**: Override dimensions to 1×1 in handlers (Diablo 2 pattern)
  - **Self-collision**: Check `occupyingItemId != draggedItemId` to allow same-position drops
  - **Signal-based sync**: Broadcast `InventoryChanged` to all containers for cross-container moves

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