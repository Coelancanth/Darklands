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

