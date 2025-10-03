# Darklands Development Archive - October 2025

**⚠️ CRITICAL: This is an APPEND-ONLY archive. Never delete or overwrite existing entries.**

**Purpose**: Completed and rejected work items for historical reference and lessons learned.

**Created**: 2025-10-02
**Archive Period**: October 2025 (Part 2)
**Previous Archive**: Completed_Backlog_2025-10_Part1.md

## Archive Protocol

### Extraction Status
Items are moved here COMPLETE with all context, then marked for extraction:
- **NOT EXTRACTED** ⚠️ - Full context preserved, patterns not yet extracted
- **PARTIALLY EXTRACTED** 🔄 - Some learnings captured in ADRs/HANDBOOK
- **FULLY EXTRACTED** ✅ - All valuable patterns documented elsewhere

### Format for Completed Items
```markdown
### [Type]_[Number]: Title
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: Date
**Archive Note**: [One-line summary of achievement]
---
[PASTE ENTIRE ORIGINAL ITEM HERE - PRESERVE EVERYTHING]
---
**Extraction Targets**:
- [ ] ADR needed for: [architectural decision]
- [ ] HANDBOOK update: [pattern to document]
- [ ] Test pattern: [testing approach to capture]
```

## Format for Rejected Items
```markdown
### [Type]_[Number]: Title ❌ REJECTED
**Rejected**: Date
**Reason**: Why rejected
**Alternative**: What we did instead
[RESURRECT-IF: Specific conditions that would make this relevant]
```

---

## Completed Items (October 2025 - Part 2)

### VS_008: Slot-Based Inventory System (MVP)
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-02
**Archive Note**: Slot-based inventory (20 slots) with add/remove operations - all 4 phases completed, 23 tests passing (100%), PR #84 merged, manual testing successful

---

**Status**: Done ✅ (2025-10-02 12:10, PR #84 merged)
**Owner**: Dev Engineer (completed all 4 phases)
**Size**: M (5-6.5h across 4 phases)
**Priority**: Important (Core mechanic, parallel with movement)
**Depends On**: None (ActorId already exists)
**Markers**: [ARCHITECTURE] [DATA-DRIVEN]

**What**: Slot-based inventory (20-slot backpack) with add/remove operations, capacity enforcement, and basic UI panel

**Why**:
- **Core Mechanic**: Loot management is fundamental to roguelikes
- **Foundation**: Equipment, crafting, trading all depend on inventory
- **Parallel Development**: Zero conflicts with VS_006/007 (Movement systems)
- **MVP Philosophy**: Simplest inventory that provides value (defer tetris complexity)

**How** (4-Phase Implementation):
- **Phase 1 (Domain)**: `Inventory` entity stores `List<ItemId>`, `ItemId` primitive added to Domain/Common
- **Phase 2 (Application)**: `AddItemCommand`, `RemoveItemCommand`, `GetInventoryQuery` with DTOs
- **Phase 3 (Infrastructure)**: `InMemoryInventoryRepository` (auto-creates with default capacity 20)
- **Phase 4 (Presentation)**: `InventoryPanelNode` (Godot UI with 20 slot visuals, test buttons)

**Key Architectural Decision**: Inventory stores ItemIds (not Item objects)
- Enables clean separation: Inventory = container logic, Item = content definition (future VS_009)
- Parallel development: Item feature can evolve independently
- Testability: No mocks needed, just `ItemId.NewId()`

**Scope**:
- ✅ Add/remove items with capacity constraint (20 slots)
- ✅ Query inventory contents (returns list of ItemIds)
- ✅ UI panel displays slots, capacity label, add/remove test buttons
- ✅ Result<T> error handling ("Inventory is full", "Item not found")
- ❌ Item definitions (name, sprite, properties) - Deferred to VS_009
- ❌ Spatial grid (tetris placement) - Deferred to VS_017 (if playtesting shows need)
- ❌ Equipment slots (weapon, armor) - Separate future VS
- ❌ Save/load persistence - Deferred to separate Save System VS

**Done When**:
- ✅ Unit tests: 20 tests passing (10 domain, 6 application, 4 infrastructure) <100ms
- ✅ Architecture tests pass (zero Godot dependencies in Darklands.Core)
- ✅ Manual UI test: Add 20 items → All slots filled → Button disables → Error on 21st item
- ✅ ServiceLocator used ONLY in _Ready() (ADR-002 compliant)
- ✅ Result<T> error handling with descriptive messages

**Full Specification**: See [VS_008_Inventory_Spec.md](VS_008_Inventory_Spec.md) for complete implementation details (1137 lines including code examples, tests, and architecture rationale)

**Tech Lead Decision** (2025-10-02):
- **Architecture validated**: ItemId separation enables clean feature boundaries
- **Slot-based first**: Defer tetris complexity until playtesting proves demand (Shattered Pixel Dungeon proves slot-based is sufficient)
- **Explicit creation pattern**: Inventory requires `CreateInventoryCommand` (only player-controlled actors get inventories, not NPCs/enemies)
- **No events in MVP**: UI queries on-demand, defer events until cross-feature needs emerge
- **Future-proof**: Design supports party members / multiplayer (each controlled actor can have separate inventory)
- **Risks**: None - Orthogonal to movement systems, proven architecture from VS_001/005/006
- **Next steps**: Await Product Owner approval, then hand off to Dev Engineer

**Architecture Clarification** (2025-10-02):
- **Who needs inventory**: Player-controlled actors ONLY (player, companions in multiplayer)
- **NPCs/Enemies**: Equipment slots only (future VS) - what they're wielding/wearing, not a backpack
- **Loot drops**: Separate ground item system (future VS) - items at Position on map
- **Explicit creation**: Must call `CreateInventoryCommand(actorId, capacity)` to give actor an inventory

**Dev Engineer Progress**:
- **✅ Phase 1 Complete** - Domain layer implemented (2025-10-02 11:53)
  - Implemented: ItemId (Domain/Common), InventoryId, Inventory entity with business rules
  - Tests: 10 test methods, 12 test executions, 100% pass rate (15ms)
  - Key insight: Namespace collision resolved using `InventoryEntity` alias (test namespace contains "Inventory" segment)
  - Architecture: Zero Godot dependencies, Result<T> for all operations, capacity enforcement (max 100 slots)
- **✅ Phase 2 Complete** - Application layer implemented (2025-10-02 11:56)
  - Implemented: AddItemCommand/RemoveItemCommand with handlers, GetInventoryQuery with DTO, IInventoryRepository interface
  - Tests: 7 handler tests (3 add, 2 remove, 2 query), 100% pass rate (2ms)
  - Key insight: Repository interface in Application (Dependency Inversion Principle), implementation in Infrastructure
  - Railway-oriented: Handlers use `.Bind()`, `.Map()`, `.Tap()` for functional composition
- **✅ Phase 3 Complete** - Infrastructure layer implemented (2025-10-02 12:01)
  - Implemented: DI registration in GameStrapper (AddSingleton pattern)
  - Tests: 4 repository tests (auto-creation, persistence, deletion), 100% pass rate (3ms)
  - Key insight: Repository is reference-type, so second GetByActorIdAsync returns same instance (no SaveAsync needed for in-memory)
- **✅ Phase 4 Complete** - Presentation layer implemented (2025-10-02 12:10)
  - Implemented: InventoryPanelNode.cs with ServiceLocator pattern, InventoryTestScene.tscn
  - UI: GridContainer (10×2 = 20 slots), Add/Remove test buttons, dynamic slot visuals (green filled, gray empty)
  - Key insight: No events in MVP - UI queries GetInventoryQuery after commands, ServiceLocator only in _Ready()
  - **Manual Testing Required**: Open Godot → Run InventoryTestScene.tscn → Click Add Item 20 times → Verify button disables at 20/20

**Manual Testing Results** (2025-10-02):
- ✅ All 20 slots display correctly (10×2 grid layout)
- ✅ Add Item button adds items, slots fill green progressively
- ✅ Capacity label updates correctly (0/20 → 20/20)
- ✅ Add Item button disables at 20/20 capacity
- ✅ Error displayed when attempting 21st item: "Inventory is full"
- ✅ Remove Item button removes items, slots empty progressively
- ✅ UI bug discovered and fixed: Panel disposal crash when closing scene (used QueueFree() instead of Free())

**Post-Mortem Created**: [VS_008_Inventory_System_Post_Mortem.md](../05-Post-Mortems/VS_008_Inventory_System_Post_Mortem.md)
- **Wins**: Perfect phased execution (all 4 phases on schedule), zero architecture violations, ItemId pattern proved highly effective
- **Challenges**: Minor UI bug (panel disposal), namespace collision (test namespace), reference-type repository behavior
- **Lessons**: Domain/Common primitives work brilliantly for cross-feature types, query-based UI simpler than events for MVP

**Final Stats**:
- **Tests**: 23 total (10 domain, 7 application, 4 infrastructure, 2 integration), 100% pass rate, 20ms execution
- **Files Created**: 15+ (across Domain/Application/Infrastructure/Presentation layers)
- **PR**: #84 merged to main (squash merge)
- **Architecture Compliance**: 100% (ADR-001/002/003/004 all validated)

---

**Extraction Targets**:
- [ ] ADR needed for: ItemId pattern - when to use primitive IDs vs full entities
- [ ] HANDBOOK update: Phased implementation success pattern (3 consecutive VS using same approach)
- [ ] HANDBOOK update: Query-based UI vs event-driven UI (trade-offs, when to use each)
- [ ] Test pattern: Namespace collision resolution strategies (using aliases in tests)
- [ ] Reference implementation: Inventory as template for future container-style features

---

### VS_009: Item Definition System (TileSet Metadata-Driven)
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-02 23:01
**Archive Note**: Item catalog system using Godot TileSet custom data layers - all 4 phases completed, 57 tests passing (100%), sprites rendering correctly via TextureRect, auto-discovery working

---

**Status**: Done (2025-10-02 23:01 - All 4 phases complete, sprites rendering correctly, 54 tests passing)
**Owner**: Dev Engineer (completed)
**Size**: M (6-7h actual across 4 phases)
**Priority**: Important (Phase 2 foundation - blocks VS_010, VS_011, VS_012, VS_018)
**Depends On**: None (ItemId exists in Domain/Common, inventory.png spritesheet available)

**What**: Define Item entities using **Godot's TileSet custom data layers** to store ALL item properties (name, type, weight, stackable) as metadata. Designer adds items **visually in TileSet editor** with zero code changes. Repository auto-discovers items from TileSet at startup.

**Why**:
- **100% Designer-Friendly**: Artists add items via visual TileSet editor (no C# code, no recompilation!)
- **Single Source of Truth**: All item data (sprites + properties) lives in one TileSet resource
- **Godot-Native**: Uses TileSet custom data layers (standard Godot feature for tile metadata)
- **Eliminates Hardcoding**: No C# item definitions, no JSON needed - TileSet IS the database
- **Clean Architecture**: Core stores primitives (atlas coords), Infrastructure reads TileSet metadata

**How** (Data-First: TileSet → Code):

**Phase 0 - TileSet Setup** (~1.5h - Designer/Contract Definition):
- **Use existing** `assets/inventory_ref/item_sprites.tres` TileSet (already has tiles with size_in_atlas!)
- **Add 3 custom data layers** in Godot TileSet editor:
  1. `item_name` (String) - "Ray Gun", "Baton", "Green Vial", etc.
  2. `item_type` (String) - "Weapon", "Consumable", "Quest", "UI"
  3. `max_stack_size` (Int) - 1 (not stackable), 5-20 (stackable)
- **Paint metadata** on each tile (visual editor):
  - Select tile → Inspector → Custom Data section → fill values
  - Example: Tile (6,0) = {name: "Ray Gun", type: "Weapon", weight: 1.2, max_stack: 1}
  - Example: Tile (X,Y) = {name: "Green Vial", type: "Consumable", weight: 0.1, max_stack: 5}
- **Width/Height**: Already stored in TileSet's `size_in_atlas` (no custom data needed!)
  - Read via `atlasSource.GetTileSizeInAtlas(coords)` in code
- **Output**: TileSet resource with metadata contract established (4 custom layers)
- **Validation**: Open TileSet in editor, verify all tiles have custom data

**Phase 1 - Domain** ✅ **COMPLETE** (2025-10-02):
- `Item` entity with primitives (no Godot types):
  - Atlas coords (int x, y), name, type, width, height, max_stack_size
  - Computed property: `bool IsStackable => MaxStackSize > 1`
- Factory: `Item.Create()` with comprehensive validation:
  - 7 validation rules (atlas coords, name, type, dimensions, stack size)
  - Returns Result<Item> for functional error handling
- Tests: 23 unit tests passing in 17ms (<100ms requirement met!)
  - Happy path (valid items, stackable vs non-stackable)
  - Atlas coordinate validation (negative, zero, positive)
  - Name/type validation (empty, whitespace)
  - Dimension validation (Theory tests with InlineData)
  - Stack size validation + IsStackable computed property

**Phase 2 - Application** ✅ **COMPLETE** (2025-10-02):
- IItemRepository interface (GetById, GetAll, GetByType)
  - Synchronous methods (catalog data loaded once at startup)
  - DIP: Interface in Application, implementation in Infrastructure
- ItemDto for decoupling Presentation from Domain entities
- 3 Query/Handler pairs with MediatR:
  - GetAllItemsQuery: Retrieve full item catalog
  - GetItemByIdQuery: Lookup single item
  - GetItemsByTypeQuery: Filter by type (weapon, item, UI)
- Tests: 18 unit tests passing in 56ms (<500ms requirement met!)
  - Happy paths (valid queries, DTO mapping)
  - Edge cases (empty catalogs, no matches, missing items)
  - Repository failures (railway-oriented error propagation)
  - DTO integrity (all properties map correctly, IsStackable preserved)

**Phase 3 - Infrastructure** ✅ **COMPLETE** (2025-10-02):
- `TileSetItemRepository` in Infrastructure/ (Godot project, not Core):
  - Lives in Presentation layer (Godot SDK) - ADR-002 compliance
  - Constructor accepts TileSet (loaded by DI container at startup)
  - Auto-discovery: Enumerates tiles, reads custom data + size_in_atlas
  - Extracts primitives, calls Item.Create() (keeps Domain Godot-free)
  - Caches in Dictionary<ItemId, Item> for O(1) GetById lookups
  - Caches full list for O(1) GetAll returns
  - GetByType: LINQ filtering with case-insensitive matching
- Tests: 13 contract tests passing in <10ms (<2s requirement met!):
  - GetById: Exists, not exists, O(1) performance with 100 items
  - GetAll: With items, empty catalog
  - GetByType: Type filtering, case-insensitive, no matches, empty/whitespace validation
  - In-memory test double validates IItemRepository contract
  - Real TileSet integration validated manually in Phase 4

**Phase 4 - Presentation** ✅ **COMPLETE** (2025-10-02):
- `ItemSpriteNode.cs` in Components/:
  - Extends Sprite2D for item rendering
  - DisplayItem(itemId): Queries catalog via MediatR, renders sprite
  - DisplayItemDto(dto): Direct rendering for batch operations
  - Uses TileSet GetTileTextureRegion() for atlas coordinates
  - Configurable scale (default 1.0, showcase uses 2.0x)
- `ItemShowcaseController.cs` in godot_project/test_scenes/:
  - Loads all items via GetAllItemsQuery
  - Creates UI panels programmatically (PanelContainer + VBoxContainer)
  - Displays: sprite (2x scale), name label, metadata (type, size, stack)
  - Demonstrates catalog auto-discovery working end-to-end
- **DI Registration** in Main.cs:
  - Loads item_sprites.tres using GodotObject.Load<TileSet>()
  - Registers TileSetItemRepository with factory lambda
  - Repository auto-discovers items on construction (logged to console)
- **Scope validation**: Item catalog foundation only
  - ✅ Item sprites render from TileSet
  - ✅ Metadata displays (name, type, size, stack size)
  - ✅ Auto-discovery works (add tile to TileSet → appears in showcase)
  - ❌ NOT implementing spatial grid inventory (deferred to VS_018)
  - ❌ NOT implementing drag-drop (deferred to VS_018)

**Done When**: ✅ **ALL CRITERIA MET** (2025-10-02 23:01)
- ✅ **Phase 0**: item_sprites.tres has 3 custom data layers + metadata on 10 items
- ✅ **Phases 1-3**: 57 tests pass (23+18+16) in <100ms total
  - Item.Create() with 7 validations, ItemDto mapping, repository contract
- ✅ **Phase 4**: ItemSpriteNode (TextureRect) + ItemShowcaseController + DI registration complete
  - Manual test verified: ItemShowcaseScene.tscn displays 10 items with correct sprites + metadata
  - All sprites centered correctly in uniform 200×240px slots (TextureRect.KeepAspectCentered)
- ✅ **Acceptance Test**: Add new tile to TileSet → restart → auto-appears (zero code!)
- ✅ **Scope Validation**: Item catalog foundation only, NOT spatial inventory
- ✅ **ADR-002**: Core has zero Godot dependencies (2-project architecture enforces)

**Implementation Notes** (2025-10-02 23:01):
- **Rendering Fix**: Switched from `Sprite2D` (Node2D) to `TextureRect` (Control) for proper UI layout
  - Root cause: Control containers (CenterContainer) only layout Control children, not Node2D children
  - Solution: TextureRect with StretchMode.KeepAspectCentered automatically centers/scales sprites
  - Lesson: Always match node hierarchy to container type (Node2D for game world, Control for UI)

**Architecture Decision** (2025-10-02 21:15 - Updated with data-first approach):
- ✅ **TileSet custom data layers** = Godot-native metadata storage
- ✅ **Lesson learned**: Always search for Godot features BEFORE designing custom solutions!
- ✅ **Phase reversal**: TileSet metadata defines contract (Phase 0), code implements to contract (Phase 1-4)
- ✅ **Data-first workflow**: Designer establishes metadata structure → Developer reads known fields
- ✅ **Eliminates**: Hardcoded C# definitions, JSON files, guesswork about data structure
- ✅ **Designer workflow**: Visual editor only, zero programmer dependency
- **Next step**: Designer completes Phase 0 (add custom data layers) → Product Owner approval → Dev Engineer implementation

**Blocks**: VS_010 (Stacking), VS_011 (Equipment), VS_012 (Loot), VS_018 (Spatial Inventory)

---

**Extraction Targets**:
- [ ] ADR needed for: TileSet custom data layers as metadata storage pattern (Godot-native data-driven design)
- [ ] ADR needed for: Node hierarchy matching (Node2D vs Control in container layouts)
- [ ] HANDBOOK update: Data-first workflow pattern (designer defines contract → developer implements)
- [ ] HANDBOOK update: TextureRect vs Sprite2D decision matrix (UI vs game world rendering)
- [ ] Test pattern: Repository contract testing with in-memory test doubles
- [ ] Reference implementation: TileSet metadata-driven repository as template for asset-backed catalogs

---

### VS_018: Spatial Inventory System (Multi-Phase) - L-Shapes
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-03 22:48
**Archive Note**: Complete 4-phase implementation of spatial inventory with L-shape support - TileSet-driven ItemShape encoding (custom:x,y;...), rotation support, collision detection via OccupiedCells iteration, comprehensive testing (359 total, 45 L-shape specific), found and fixed BR_003/BR_004/BR_005 during validation, feature fully operational
---

**Status**: Phase 1 ✅ + Phase 2 ✅ + Phase 3 ✅ + Phase 4 ✅ **COMPLETE**
**Owner**: Dev Engineer (all 4 phases completed)
**Size**: XL (12-16h planned across 4 phases) | **Actual**: Phase 1 (6h), Phase 2 (5h), Phase 3 (6h), Phase 4 (8h)
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

**Phase 1: Interaction Mechanics** (6h actual) ✅ **COMPLETE** (2025-10-03)
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
- **Core**: GridPosition, ContainerType, spatial Inventory, Commands/Queries (261 tests passing)
- **UI**: Drag-drop working, tooltips, 4-color item types, equipment swap, type filtering
- **Lessons**:
  - Mouse filter hierarchy critical for Godot drag events (`Pass` vs `Stop` vs `Ignore`)
  - Defense-in-depth for data loss: Validate type in BOTH `_CanDropData` AND handler
  - Safe swap algorithm: Remove→Remove→Place→Place with full rollback at each step

**Phase 2: Multi-Cell Rectangles** (5h actual) ✅ **COMPLETE** (2025-10-03)
- **Goal**: Items occupy Width×Height cells (2×1 sword takes 2 adjacent slots)
- **Domain**: Enhance collision detection to check all occupied cells
- **Application**: Update `CanPlaceItemAtQuery` to validate rectangle fits
- **Presentation**: Render items spanning multiple cells, snap to grid
- **NO rotation yet** (sword is always 2×1, cannot become 1×2)
- **Tests**: 15-20 additional tests (multi-cell collision, boundary checks)
- **Core**: Multi-cell AABB collision, dimension override for equipment slots, intra-container rollback
- **UI**: Multi-cell TextureRect rendering (overlay architecture), green/red drag highlights
- **Lessons**:
  - **Sprite ≠ Inventory dimensions**: 4×4 sprite can occupy 2×2 grid (dual metadata critical)
  - **Equipment slot UX**: Override dimensions to 1×1 in handlers (Diablo 2 pattern)
  - **Self-collision**: Check `occupyingItemId != draggedItemId` to allow same-position drops
  - **Signal-based sync**: Broadcast `InventoryChanged` to all containers for cross-container moves

**Phase 3: Rotation Support** (6h actual) ✅ **COMPLETE** (2025-10-03 19:11)
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

**Phase 4: Complex Shapes** (8h actual) ✅ **COMPLETE** (2025-10-03 21:43)
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

---

**Extraction Targets**:
- [ ] ADR needed for: ItemShape value object pattern (coordinate-based shape representation)
- [ ] ADR needed for: OccupiedCells collision detection (iterate actual cells, not bounding boxes)
- [ ] ADR needed for: TileSet custom data layers for complex shape encoding
- [ ] HANDBOOK update: Godot drag-drop system patterns (cross-container state management)
- [ ] HANDBOOK update: Static shared state pattern for cross-container drag operations
- [ ] HANDBOOK update: Control node transparency for z-ordering workarounds
- [ ] Test pattern: Shape rotation math validation (coordinate transformation tests)
- [ ] Reference implementation: Spatial inventory as template for grid-based systems

---

### BR_003: L-Shape Collision Bug
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-03 22:33
**Archive Note**: PlaceItemAtPositionCommandHandler & MoveItemBetweenContainersCommandHandler converted width×height to rectangles, destroying L-shapes. Fixed by using `item.Shape` (Phase 4 API) in all placement paths. Root cause: Application layer handlers called backward-compatible Phase 2 signature. Impact: L-shapes now preserve 3-cell structure through placement, movement, and rollback. All 359 tests GREEN. (Commit: 48db266)
---

**Status**: Fixed (included in VS_018 Phase 4 completion)
**Owner**: Dev Engineer
**Priority**: Critical (blocked VS_018 validation)
**Found During**: VS_018 Phase 4 manual testing

**Bug**: PlaceItemAtPositionCommandHandler & MoveItemBetweenContainersCommandHandler converted width×height to rectangles, destroying L-shapes

**Root Cause**: Application layer handlers called backward-compatible Phase 2 signature (`PlaceItemAt(width, height, rotation)`) instead of Phase 4 shape-aware API

**Impact**: L-shapes treated as bounding box rectangles (2×2 = 4 cells) instead of actual shape (3 cells)

**Fix**: Updated all placement paths to use `item.Shape` directly:
- PlaceItemAtPositionCommandHandler: `inventory.PlaceItemWithShape(item.Shape, rotatedShape)`
- MoveItemBetweenContainersCommandHandler: Same pattern for source/target placement
- Rollback paths: Use stored shapes from repository

**Result**: L-shapes now preserve 3-cell structure through placement, movement, and rollback. All 359 tests GREEN.

**Commit**: 48db266

---

**Extraction Targets**:
- [ ] HANDBOOK update: Backward compatibility trap - old signatures can silently convert complex types to simple types
- [ ] Test pattern: Integration tests must validate actual collision cells, not just "placement succeeded"

---

### BR_004: Presentation Layer Validation Duplication
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-03 22:48
**Archive Note**: Presentation layer (_CanDropData, UpdateDragHighlightsAtPosition) duplicated collision logic, iterating bounding box instead of OccupiedCells. Result: UI blocked dagger placement in L-shape empty corner. Fixed by delegating ALL validation to CanPlaceItemAtQuery (Core). Removed 200+ lines of duplicated business logic from Presentation. Architectural compliance: Presentation now thin display layer, Core owns all validation. Memory Bank updated with "Presentation Layer Responsibilities" guidelines. All 359 tests GREEN. (Commit: 20f5d2a)
---

**Status**: Fixed (included in VS_018 Phase 4 completion)
**Owner**: Dev Engineer
**Priority**: Critical (architectural violation + UX bug)
**Found During**: VS_018 Phase 4 manual testing

**Bug**: Presentation layer (_CanDropData, UpdateDragHighlightsAtPosition) duplicated collision logic, iterating bounding box instead of OccupiedCells. Result: UI blocked dagger placement in L-shape empty corner (cell 0,1).

**Root Cause**: Presentation layer tried to "optimize" by pre-validating placement client-side, duplicating Core's collision logic. Duplication used AABB rectangle iteration instead of OccupiedCells, diverging from Core's actual logic.

**Impact**:
- UI shows red highlight for valid placements (false negatives)
- Dagger (1×1) rejected from empty L-shape corner
- Architectural violation: Business logic leaked into Presentation

**Fix**:
1. Removed ALL collision logic from SpatialInventoryContainerNode
2. _CanDropData: Only check inventory exists, delegate to CanPlaceItemAtQuery
3. UpdateDragHighlightsAtPosition: Query Core for each cell, render result
4. Removed 200+ lines of duplicated validation code

**Result**:
- Presentation is now thin display layer (ADR-002 compliant)
- Core owns ALL validation logic (single source of truth)
- UI correctly shows green highlights for valid L-shape placements
- All 359 tests GREEN

**Memory Bank Update**: Added "Presentation Layer Responsibilities" section to activeContext.md:
- ✅ DO: Query Core for validation, render results
- ❌ DON'T: Duplicate business logic, implement collision detection

**Commit**: 20f5d2a

---

**Extraction Targets**:
- [ ] ADR needed for: Presentation layer boundaries (no business logic, query-driven validation)
- [ ] HANDBOOK update: "Optimizing by duplication" anti-pattern
- [ ] HANDBOOK update: Single Source of Truth enforcement (validation must be in Core)
- [ ] Reference implementation: Thin presentation layer pattern (delegate all logic to Core)

---

### BR_005: Cross-Container L-Shape Highlight Inaccuracy
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-03 23:00
**Archive Note**: When dragging L-shapes between containers, highlights showed 1×1 (original) or 2×2 rectangle (first fix) instead of accurate 3-cell L-shape. Root cause: ItemDto (Phase 2 DTO) only exposed InventoryWidth/Height, not ItemShape. Fixed by evolving ItemDto to Phase 4: added ItemShape property, updated all query handlers (GetItemById, GetAll, GetByType) to map Shape. RenderDragHighlight now uses actual Shape for cross-container drags. Result: Pixel-perfect L-shape highlighting for both within-container and cross-container scenarios. All 359 tests GREEN. (Commit: a9146f1)
---

**Status**: Fixed (included in VS_018 Phase 4 completion)
**Owner**: Dev Engineer
**Priority**: Critical (UX regression from BR_004 fix)
**Found During**: BR_004 validation testing

**Bug**: When dragging L-shapes between containers, highlights showed 1×1 (original) or 2×2 rectangle (first fix) instead of accurate 3-cell L-shape.

**Root Cause**: ItemDto (Phase 2 DTO) only exposed InventoryWidth/Height, not ItemShape. Cross-container drag relied on GetItemByIdQuery → ItemDto, losing shape information.

**Impact**:
- Cross-container drag highlights incorrect (bounding box rectangles)
- Within-container drag correct (had access to Item entity directly)
- Inconsistent UX between same-container and cross-container operations

**Evolution Path**:
1. **Original (BR_004)**: Presentation duplicated collision logic → rejected valid placements
2. **First Fix (BR_004)**: Delegate to Core, but ItemDto only had width/height → 2×2 highlights
3. **Final Fix (BR_005)**: Evolve ItemDto to Phase 4 - add ItemShape property

**Fix**:
1. Added `ItemShape Shape` property to ItemDto
2. Updated all query handlers to map Shape:
   - GetItemByIdQueryHandler
   - GetAllItemsQueryHandler
   - GetItemsByTypeQueryHandler
3. Updated RenderDragHighlight to use dto.Shape.OccupiedCells
4. Removed temporary workaround (rectangle fallback)

**Result**:
- Pixel-perfect L-shape highlighting for all scenarios
- Cross-container and within-container rendering now identical
- ItemDto fully Phase 4 compliant (includes complex shapes)
- All 359 tests GREEN

**Commit**: a9146f1

---

**Extraction Targets**:
- [ ] HANDBOOK update: DTO evolution pattern (add properties incrementally as features require)
- [ ] HANDBOOK update: Phased DTO design (Phase 2 DTOs may need Phase 4 properties later)
- [ ] Test pattern: Cross-boundary data integrity tests (ensure DTOs preserve all domain data)

---

### BR_006: Cross-Container Rotation Highlights
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-04
**Archive Note**: Highlights didn't rotate during cross-container drag when scrolling mouse wheel. Root cause: _Input() fires on source container only, _CanDropData() (which renders highlights) needs mouse movement to trigger. Fixed with mouse warp hack: Input.WarpMouse(mousePos + Vector2(0.1, 0)) triggers _CanDropData on target container without visible cursor jump. Result: Rotation now updates highlights instantly during cross-container drag. (Commit: a31f043)
---

**Status**: Fixed
**Owner**: Dev Engineer
**Priority**: Important (UX polish)
**Found During**: VS_018 final validation testing

**Bug**: Highlights didn't rotate during cross-container drag when scrolling mouse wheel

**Root Cause**: Godot's drag-drop event architecture:
- `_Input()` fires on source container (where drag started)
- `_CanDropData()` fires on target container (where mouse currently is)
- Mouse wheel scroll handled in _Input() (updates rotation)
- But _CanDropData() only re-runs when mouse MOVES
- Result: Rotation changes, but target highlights don't update until mouse moves

**Impact**:
- Cross-container rotation appears "stuck" (confusing UX)
- Within-container rotation works (same node handles both events)
- Workaround: User moves mouse slightly → highlights update

**Fix**: Mouse warp hack in _Input():
```csharp
// After updating rotation, force target container's _CanDropData to re-run
var mousePos = GetViewport().GetMousePosition();
Input.WarpMouse(mousePos + new Vector2(0.1f, 0)); // Warp 0.1px (invisible to user)
```

**How It Works**:
1. User scrolls wheel over target container
2. Source container's _Input() catches event, updates rotation
3. Warp mouse 0.1px (triggers mouse motion event)
4. Target container's _CanDropData() re-runs (sees new rotation)
5. Highlights render with updated rotation

**Result**:
- Rotation updates highlights instantly during cross-container drag
- 0.1px movement imperceptible to users
- No visible cursor jump or stutter

**Trade-offs**:
- ✅ Pragmatic solution (works within Godot's event model)
- ❌ Slightly hacky (relies on side-effect of WarpMouse)
- ✅ Zero performance impact (single call per scroll)
- ✅ Invisible to users (0.1px below perception threshold)

**Commit**: a31f043

---

**Extraction Targets**:
- [ ] HANDBOOK update: Godot drag-drop event architecture (source vs target container event handling)
- [ ] HANDBOOK update: Mouse warp trick for forcing UI updates
- [ ] HANDBOOK update: When to use pragmatic hacks vs architectural solutions

---

### BR_007: Equipment Slot Visual Issues
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-04
**Archive Note**: Equipment slots showed L-shape highlights (3 cells) instead of 1×1 slot highlights. Root cause: UpdateDragHighlightsAtPosition used item's actual shape (L-shape) for equipment slots. Fixed by overriding rotatedShape to ItemShape.CreateRectangle(1,1) for ContainerType.Equipment. Also fixed item sprite centering in equipment slots (TextureRect now uses CustomMinimumSize = cell size for proper centering). Result: Equipment slots show single-cell highlights, items center correctly. (Commit: ca936bf)
---

**Status**: Fixed
**Owner**: Dev Engineer
**Priority**: Important (UX bug in equipment slots)
**Found During**: VS_018 final validation testing

**Bug**: Equipment slots showed L-shape highlights (3 cells) instead of 1×1 slot highlights. Also, item sprites didn't center correctly in equipment slots.

**Root Cause**:
1. UpdateDragHighlightsAtPosition used item's actual shape (L-shape) for all containers
2. Equipment slots display items as 1×1 (Diablo 2 pattern), but highlights rendered actual multi-cell shape
3. TextureRect auto-sizing caused sprites to fill entire slot (not centered)

**Impact**:
- Equipment slot highlights show 3-cell L-shapes (visually confusing)
- Highlights extend beyond slot boundaries (overlaps adjacent slots)
- Item sprites not centered in equipment slots (different from backpack)

**Fix**:
1. **Highlight Override**: Check container type in UpdateDragHighlightsAtPosition:
```csharp
var rotatedShape = containerType == ContainerType.Equipment
    ? ItemShape.CreateRectangle(1, 1)  // Force 1×1 for equipment slots
    : item.Shape.RotateClockwise(rotation);  // Use actual shape for backpacks
```

2. **Sprite Centering**: Set TextureRect.CustomMinimumSize = cell size:
```csharp
itemTextureRect.CustomMinimumSize = new Vector2(cellSize, cellSize);
itemTextureRect.ExpandMode = TextureRect.ExpandModeEnum.FitToHeight;
itemTextureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
```

**Result**:
- Equipment slots show single-cell highlights (1×1)
- Item sprites center correctly in equipment slots
- Consistent visual behavior between backpack and equipment slots
- All 359 tests GREEN

**Commit**: ca936bf

---

**Extraction Targets**:
- [ ] HANDBOOK update: Equipment slot display pattern (1×1 visual override for multi-cell items)
- [ ] HANDBOOK update: TextureRect centering with CustomMinimumSize
- [ ] HANDBOOK update: Container-specific rendering logic (when to override item properties)

---

