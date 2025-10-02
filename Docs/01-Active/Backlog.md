# Darklands Development Backlog


**Last Updated**: 2025-10-02 12:37 (Tech Lead: Added VS_009 Item Definition System + VS_018 Spatial Grid Inventory)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 004
- **Next TD**: 003
- **Next VS**: 010


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

**No critical items!** ✅

---

*Recently completed and archived (2025-10-02):*
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

### VS_009: Item Definition System (Data-Driven Foundation) ⭐ **PLANNED**

**Status**: Proposed (Tech Lead breakdown complete)
**Owner**: Tech Lead → Product Owner (for approval)
**Size**: M (7-9 hours across 4 phases - Phase 4 enhanced for reference UI)
**Priority**: Important (Phase 2 foundation - blocks VS_010, VS_011, VS_012, VS_018 multi-cell)
**Depends On**: None (ItemId already exists in Domain/Common)

**What**: Define Item entities with properties (name, type, sprite, size) and repository abstraction. UI matches Unity reference screenshot (8×8 grid + equipment slots). Initially uses in-memory storage with 6 hardcoded test items; migrates to JSON when >20 items needed.

**Why**:
- **Foundation for Phase 2**: All itemization features need Item properties (stacking, equipment, spatial grid shapes)
- **VS_018 Enhancement**: Spatial inventory needs Item.Width/Height for multi-cell shapes (2×1 swords, 2×2 armor)
- **Designer-Friendly**: Repository abstraction allows JSON migration when content scales
- **Visual Polish**: Item sprites from existing asset (inventory_ref/inventory.png spritesheet)
- **Incremental Approach**: Start simple (in-memory), scale later (JSON) when proven needed

**How** (4-Phase Implementation):

**Phase 1 - Domain** (~2h):
- `Item` entity (in Features/Item/Domain):
  - `ItemId` (already exists in Domain/Common ✅)
  - `Name` (string) - "Rusty Dagger", "Health Potion"
  - `ItemType` (enum) - Weapon, Armor, Consumable, Material, Quest
  - `SpritePath` (string) - "res://assets/inventory_ref/inventory.png"
  - `SpriteIndex` (int) - Index into spritesheet (0-7 from reference asset)
  - `Width`, `Height` (int) - Grid size (1×1 potion, 2×1 sword, 2×2 gadget)
  - `Weight` (float) - For future encumbrance system
  - `IsStackable` (bool) - True for consumables/materials
  - `MaxStackSize` (int) - Default 1, consumables 5, materials 20
- `ItemType` enum: Weapon, Armor, Consumable, Material, Quest
- `SpriteCoordinates` value object: Encapsulates sprite_path + sprite_index
- Factory method: `Item.Create()` with validation (Result<Item>)
  - Width/Height must be 1-3 (reasonable grid sizes)
  - MaxStackSize must be 1-99
  - SpritePath must be non-empty
- **Tests**: 12 unit tests (<100ms)
  - Valid item creation
  - Validation failures (invalid width, negative stack size, etc.)
  - Value object tests for SpriteCoordinates

**Phase 2 - Application** (~2h):
- `IItemRepository` abstraction:
  - `Task<Item?> GetByIdAsync(ItemId id)` - Fetch single item
  - `Task<IReadOnlyList<Item>> GetAllAsync()` - All items (for admin/debug)
  - `Task<IReadOnlyList<Item>> GetByTypeAsync(ItemType type)` - Filter by type
- Queries (read-only, no commands yet):
  - `GetItemQuery(ItemId)` → `ItemDto`
  - `GetAllItemsQuery` → `List<ItemDto>`
  - `GetItemsByTypeQuery(ItemType)` → `List<ItemDto>` (e.g., all weapons)
- `ItemDto` (for UI consumption):
  - All Item properties as primitives
  - Used by Presentation to render item visuals
- **Tests**: 8 query handler tests (<500ms)
  - GetItem returns correct DTO
  - GetAllItems returns all items
  - GetByType filters correctly

**Phase 3 - Infrastructure** (~2.5h):
- `InMemoryItemRepository` with 6 items (matches Unity reference screenshot):
  ```csharp
  // Based on assets/inventory_ref/inventory.png + Unity reference UI:
  new Item(ItemId.NewId(), "Ray Gun", ItemType.Weapon, sprite:0, width:2, height:1, weight:1.2f, stackable:false),
  new Item(ItemId.NewId(), "Stun Baton", ItemType.Weapon, sprite:1, width:2, height:1, weight:1.5f, stackable:false),
  new Item(ItemId.NewId(), "Red Vial", ItemType.Consumable, sprite:2, width:1, height:1, weight:0.1f, stackable:true, maxStack:5),
  new Item(ItemId.NewId(), "Green Vial", ItemType.Consumable, sprite:3, width:1, height:1, weight:0.1f, stackable:true, maxStack:5),
  new Item(ItemId.NewId(), "Strange Gadget", ItemType.Quest, sprite:4, width:2, height:2, weight:2.0f, stackable:false),
  new Item(ItemId.NewId(), "Dagger", ItemType.Weapon, sprite:6, width:1, height:1, weight:0.8f, stackable:false)
  // Note: Removed items 5 and 7 to match reference screenshot's visible items
  ```
- Register IItemRepository in GameStrapper DI container
- **Tests**: 6 repository tests (<2s)
  - GetById returns correct item
  - GetAll returns 6 items
  - GetByType(Weapon) returns 3 weapons (Ray Gun, Stun Baton, Dagger)
  - GetByType(Consumable) returns 2 consumables (Red Vial, Green Vial)
  - GetById with invalid ItemId returns null

**Phase 4 - Presentation** (~3.5h - Enhanced for Reference UI):
- **UI Layout** (matches Unity reference screenshot):
  - Main backpack: 8×8 grid (64 cells, left panel)
  - Container view: 4×8 grid (32 cells, middle panel, for VS_012 Ground Loot)
  - Equipment slots: 2×4 vertical slots (right panels, for VS_011 Equipment)
  - Item info bar: Bottom panel shows selected item name
  - Dark navy background (#1a1f3a), grid cell borders (#404560)

- `ItemSpriteNode.cs` (Godot UI component):
  - Loads spritesheet: `GD.Load<Texture2D>("res://assets/inventory_ref/inventory.png")`
  - Extracts sprite via `AtlasTexture` with pixel-perfect regions:
    - Ray Gun (sprite 0): 2×1 cells = Region(0, 0, 64, 32)
    - Stun Baton (sprite 1): 2×1 cells = Region(64, 0, 64, 32)
    - Red Vial (sprite 2): 1×1 cell = Region(0, 32, 32, 32)
    - Green Vial (sprite 3): 1×1 cell = Region(32, 32, 32, 32)
    - Gadget (sprite 4): 2×2 cells = Region(64, 32, 64, 64)
    - Dagger (sprite 6): 1×1 cell = Region(0, 64, 32, 32)
  - Renders at cell size (32px per grid cell)
  - Multi-cell items span correct dimensions

- `InventoryGridUI.cs` (Container UI):
  - GridContainer with fixed cell size (32×32 px)
  - TextureRect background for each cell (dark navy)
  - Border drawn via StyleBoxFlat (1px, lighter navy)
  - Mouse hover highlights cell (subtle glow)
  - Click to select item → updates bottom info bar

- `ItemInfoBar.cs` (Bottom panel):
  - Label shows selected item: "Ray Gun" (from screenshot reference)
  - Simple dark background with centered text
  - Updates on item selection/hover

- `ItemTestScene.tscn` (Demo matching reference UI):
  - 8×8 main grid (left) with items placed:
    - Ray Gun (2×1) at position (4, 0)
    - Stun Baton (2×1) at position (1, 1)
    - Red Vials (1×1) at positions (0, 0), (2, 0), (0, 1)
    - Green Vials (1×1) at positions (2, 2), (6, 2), (0, 7), (6, 7)
    - Gadgets (2×2) at positions (3, 3), (5, 4)
    - Daggers (1×1) at positions (1, 6), (2, 7)
  - 4×8 container grid (middle) - empty for now (VS_012)
  - 2×4 equipment slots (right) - Weapon slot with Stun Baton, Gadget slot with Gadget (2×2)
  - Item info bar (bottom) - "Ray Gun" label

- **Manual Testing**:
  - UI matches reference screenshot layout (8×8 + 4×8 + equipment slots)
  - Multi-cell items render correctly (2×1 weapons, 2×2 gadgets)
  - Grid cells have visible borders (navy theme)
  - Click item → name appears in bottom info bar
  - Items from inventory_ref spritesheet display pixel-perfect

**Scope** (Incremental MVP):
- ✅ Item entity with essential properties (name, sprite, size, type, weight, stacking)
- ✅ Repository abstraction (IItemRepository for future JSON migration)
- ✅ In-memory storage (8 hardcoded items from reference spritesheet)
- ✅ Sprite rendering from inventory_ref/inventory.png
- ✅ Width/Height for VS_018 spatial grid integration
- ✅ Stacking properties for VS_010 integration
- ❌ JSON item definitions - **Deferred until >20 items needed** (YAGNI)
- ❌ Item commands (Create/Update/Delete) - Items are read-only initially (designed by devs, not players)
- ❌ Damage/defense stats - VS_011 Equipment Slots adds combat properties
- ❌ Durability system - Future VS
- ❌ Rarity/quality tiers - Future VS
- ❌ Item effects/modifiers - Future VS

**Done When**:
- ✅ Unit tests: 27 tests passing (12 domain + 9 application + 6 infrastructure) <3s total
- ✅ Architecture tests pass (zero Godot dependencies in Darklands.Core)
- ✅ Manual UI test (matches Unity reference screenshot):
  - 8×8 main backpack grid renders with dark navy theme
  - 4×8 container grid (middle panel) renders empty (VS_012 placeholder)
  - 2×4 equipment slots (right panels) render with example items
  - Multi-cell items display correctly:
    - Ray Gun (2×1) spans 2 horizontal cells
    - Stun Baton (2×1) spans 2 horizontal cells
    - Gadget (2×2) spans 2×2 cell square
    - Daggers/Vials (1×1) occupy single cells
  - Grid cells have visible borders (#404560 on #1a1f3a background)
  - Click item → "Ray Gun" appears in bottom info bar
  - All sprites match inventory_ref/inventory.png pixel-perfect
- ✅ Integration test: GetItemQuery via MediatR returns ItemDto with all properties
- ✅ Code review: Item entity has zero Godot references (ADR-002 compliant)

**Architecture Decisions**:

1. **Why In-Memory Instead of JSON from Start?**
   - **YAGNI**: 8 test items don't justify JSON complexity
   - **Type Safety**: C# item definitions are refactor-friendly (rename properties, IDE support)
   - **Incremental**: IItemRepository abstraction allows JSON swap when >20 items (designer-friendly at scale)
   - **Testability**: Hardcoded items = predictable tests, no file I/O mocking
   - **Migration Path**: When ready, implement JsonItemRepository, register in DI, zero business logic changes

2. **Why Sprite Index Instead of Sprite Rect?**
   - **Simplicity**: Index (0-7) is easier to manage than pixel coordinates
   - **Spritesheet Assumption**: 32×32 grid (common standard, matches inventory.png)
   - **Flexibility**: Can switch to Rect later if non-uniform sprites needed
   - **Designer-Friendly**: "sprite: 4" is clearer than "rect: (64, 32, 32, 32)"

3. **Why Width/Height in Item (Not Inventory Concern)?**
   - **Intrinsic Property**: A sword IS 2×1 (item characteristic, not inventory logic)
   - **Reusable**: Equipment slots might care about size (2×2 shield too big for ring slot)
   - **VS_018 Integration**: Spatial inventory queries Item.Width/Height for collision detection
   - **Domain Modeling**: "Large items take more space" is item property, not container behavior

4. **Why ItemType Enum (Not String)?**
   - **Type Safety**: Compile-time validation (can't typo "Weapn")
   - **Queryable**: GetByType(ItemType.Weapon) is clear intent
   - **Extensible**: Add new types (Ammo, Key, Food) without breaking existing items
   - **Future**: Equipment slots can restrict by type (weapon slot accepts ItemType.Weapon only)

**ADR Compliance Checklist**:
- ✅ **ADR-002** (Godot Integration): Item entity in Darklands.Core, zero Godot deps, ServiceLocator only in ItemSpriteNode._Ready()
- ✅ **ADR-003** (Functional Error Handling): Item.Create() returns Result<Item>, validates width/height/stack size
- ✅ **ADR-004** (Feature-Based Clean Architecture): Features/Item/{Domain, Application, Infrastructure}, not Domain/Common bloat
- ✅ **Clean Architecture**: IItemRepository in Application layer, InMemoryItemRepository in Infrastructure

**Integration Points**:

**VS_018 (Spatial Grid Inventory):**
```csharp
// In AddItemAtPositionCommand handler:
var item = await _itemRepository.GetByIdAsync(command.ItemId);
var canPlace = inventory.CanPlaceAt(position, item.Value.Width, item.Value.Height);
```

**VS_010 (Item Stacking):**
```csharp
// In AddItemToStackCommand handler:
var item = await _itemRepository.GetByIdAsync(command.ItemId);
if (!item.Value.IsStackable) return Result.Failure("Item is not stackable");
if (stack.Count >= item.Value.MaxStackSize) return Result.Failure("Stack is full");
```

**VS_011 (Equipment Slots):**
```csharp
// In EquipItemCommand handler:
var item = await _itemRepository.GetByIdAsync(command.ItemId);
if (item.Value.ItemType != ItemType.Weapon) return Result.Failure("Can only equip weapons in weapon slot");
```

**Future JSON Migration** (When >20 Items):
```csharp
// data/items/weapons.json:
[
  {
    "id": "item_rusty_dagger",
    "name": "Rusty Dagger",
    "type": "Weapon",
    "sprite_path": "res://assets/items/weapons.png",
    "sprite_index": 0,
    "width": 1, "height": 1,
    "weight": 0.8,
    "is_stackable": false
  }
]

// JsonItemRepository.cs (Infrastructure):
public async Task<Item?> GetByIdAsync(ItemId id)
{
    var json = await File.ReadAllTextAsync("data/items/weapons.json");
    var dtos = JsonSerializer.Deserialize<List<ItemDto>>(json);
    return dtos.FirstOrDefault(dto => dto.Id == id.ToString())?.ToEntity();
}

// GameStrapper.cs change:
services.AddSingleton<IItemRepository, JsonItemRepository>(); // Swap implementation, zero business logic changes
```

**Tech Lead Decision** (2025-10-02):
- ✅ **Approved architecture**: Feature-based Item system with repository abstraction
- ✅ **Scope validated**: In-memory initially (8 items), JSON deferred until proven need (>20 items)
- ✅ **Complexity**: M-sized (6-8h estimate reasonable for foundation + sprite integration)
- ✅ **Risk**: Low (straightforward entity, no complex algorithms, reference asset available)
- ✅ **Priority**: **Implement BEFORE VS_018** - Spatial inventory enhanced by real item sprites
- **Next step**: Product Owner approval, then Dev Engineer implements all 4 phases

**Blocks**:
- VS_010 (Item Stacking) - Needs IsStackable, MaxStackSize properties
- VS_011 (Equipment Slots) - Needs ItemType filtering
- VS_012 (Ground Loot) - Needs Item.Sprite for tile visuals
- VS_018 multi-cell items - Needs Item.Width/Height for 2×1 swords, 2×2 armor

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

### VS_018: Spatial Grid Inventory (Tetris-Style) ⭐ **PLANNED**

**Status**: Proposed (Tech Lead breakdown complete)
**Owner**: Tech Lead → Product Owner (for approval)
**Size**: L (1.5-2 days, ~12h across 4 phases)
**Priority**: Important (Phase 2 itemization feature)
**Depends On**:
- VS_008 (Slot-based Inventory ✅ - extends this)
- VS_009 (Item Definition System - optional for multi-cell shapes)

**What**: Extend VS_008's slot-based inventory to support Tetris-style spatial grid placement (Diablo 2, Resident Evil), where item position matters. Initially supports 1×1 items; multi-cell shapes deferred to VS_009 integration.

**Why**:
- **Spatial Puzzle**: Inventory management becomes tactical (optimize space usage)
- **Realism**: Large items (swords, shields) take more space than potions
- **Player Expression**: Multiple valid organization strategies
- **Genre Standard**: Diablo 2, Path of Exile, Resident Evil all use spatial grids
- **Backwards Compatible**: Extends VS_008 without breaking existing slot-based mode

**How** (4-Phase Evolutionary Extension):

**Phase 1 - Domain** (~3h):
- `Position2D` value object (in Domain/Common): Grid coordinates (X, Y)
- `InventoryMode` enum: `Slot` (existing) vs `Spatial` (new)
- Extend `Inventory` entity:
  - Mode-specific storage: `List<ItemId>` (Slot) OR `Dictionary<Position2D, ItemId>` (Spatial)
  - Factory methods: `CreateSlotBased()` (existing) + `CreateSpatial(width, height)` (new)
  - Spatial operations: `AddItemAt(position)`, `GetItemAt(position)`, `FindFreeSpace()`, `IsOccupied(position)`
  - Collision detection: Prevent overlapping placements (1×1 items for Phase 1)
- **Tests**: 15 unit tests (<100ms)
  - Slot mode tests still pass (backwards compatibility ✅)
  - Spatial mode: AddItemAt success/collision, GetItemAt, FindFreeSpace, grid boundaries

**Phase 2 - Application** (~3h):
- Commands:
  - `AddItemAtPositionCommand` - Place item at specific grid coordinates
  - `MoveItemCommand` - Move item to new position (drag-drop support)
- Queries:
  - `GetInventoryGridQuery` - Returns grid state for rendering
  - `CanPlaceAtQuery` - Validates position before placement (drag-drop preview)
  - `FindFreeSpaceQuery` - Auto-find valid position for quick-add
- DTOs:
  - `InventoryGridDto`: Width, Height, Mode, Dictionary<Position2D, ItemId>
  - `ItemPlacementDto`: Position + ItemId (for UI rendering)
- **Tests**: 10 handler tests (<500ms)

**Phase 3 - Infrastructure** (~1.5h):
- No major changes (InMemoryInventoryRepository already stores Inventory entity)
- Possibly add helper methods for grid serialization (if persistence needed)
- **Tests**: 5 integration tests (<2s)

**Phase 4 - Presentation** (~4.5h):
- Extend `InventoryPanelNode.cs`:
  - Mode detection: Render slot list OR spatial grid
  - Grid rendering: 8×4 ColorRect grid (Diablo 2 dimensions)
  - Cell highlighting: Hover → show green (valid) or red (occupied/invalid)
- `InventoryDragDropController.cs` (new):
  - Mouse drag: Pick up item → show ghost sprite
  - Snap to grid: Round mouse position to nearest cell
  - Drop validation: Green highlight → place item, Red → cancel
  - Right-click: Cancel drag operation
- Visual polish:
  - Item sprites centered in cells
  - Drop shadow during drag
  - Occupied cells have item visual
- **Manual Testing**:
  - Drag item to empty space → green highlight → drop → item placed
  - Drag item to occupied cell → red highlight → release → item returns
  - Fill 8×4 grid → no free space → new item drag shows red everywhere

**Scope** (Incremental Approach):
- ✅ Dual-mode inventory (Slot OR Spatial, chosen at creation)
- ✅ Spatial grid: 8×4 default (32 cells, similar to Diablo 2 small backpack)
- ✅ 1×1 item placement (all items occupy single cell initially)
- ✅ Drag-drop interaction with visual feedback
- ✅ Collision detection (can't place on occupied cell)
- ✅ Auto-find free space (for quick-add button)
- ✅ Backwards compatible (existing slot-based tests pass)
- ❌ Multi-cell items (2×1 sword, 2×2 armor) - **Requires VS_009 Item.Shape property**
- ❌ Item rotation (90°, 180°, 270°) - Deferred until multi-cell items exist
- ❌ Container nesting (bag-in-bag) - Separate VS (VS_013)
- ❌ Auto-sort/organize button - Nice-to-have, deferred
- ❌ Variable grid sizes per inventory - Fixed 8×4 for Phase 2

**Done When**:
- ✅ Unit tests: 30 tests passing (15 domain + 10 application + 5 infrastructure) <2s total
- ✅ Architecture tests pass (zero Godot dependencies in Darklands.Core)
- ✅ Backwards compatibility: All VS_008 slot-based tests still pass (no regressions)
- ✅ Manual UI test (Spatial mode):
  - Create spatial inventory → 8×4 grid renders
  - Drag item to cell (3, 2) → green highlight → drop → item placed at (3, 2)
  - Drag second item to (3, 2) → red highlight → release → item returns to original position
  - Fill all 32 cells → drag new item → all cells red → cannot place
  - Right-click during drag → item returns to source position
- ✅ Manual UI test (Slot mode - regression check):
  - Slot-based inventory still works (no visual changes)
  - Add/remove operations unchanged

**Architecture Decisions**:

1. **Why Dual-Mode Instead of Separate Entity?**
   - Single source of truth (no duplication)
   - Shared operations (Clear, Contains) don't need reimplementation
   - Future flexibility (convert slot → spatial if needed)
   - Clean: Mode chosen at creation via factory method

2. **Why 1×1 Items Initially?**
   - **Decouples from VS_009** (Item.Shape is Item property, not Inventory concern)
   - **Incremental complexity** (spatial grid logic first, multi-cell later)
   - **Parallel development** (VS_009 can evolve Item system independently)
   - **Testable** (no need to mock Item entities, just use Position2D)

3. **Why Position2D in Domain/Common?**
   - **Reusable** (other features may need grid positions: terrain, units, tiles)
   - **Primitive** (simple value object, no feature-specific logic)
   - **Separation** (Position is universal, ItemPlacement is inventory-specific)

4. **Reference Implementation Lessons** (Unity Diablo 2 Inventory):
   - ✅ **Separation of concerns**: Manager (logic) + Renderer (UI) + Controller (interaction)
   - ✅ **Shape abstraction**: `InventoryShape` class with bool array for custom shapes
   - ✅ **Collision detection**: `Overlaps()` method checks if item shapes intersect
   - ✅ **Event-driven**: Callbacks for onItemAdded, onItemRemoved, onItemDropped
   - ✅ **Provider pattern**: `IInventoryProvider` abstracts storage (easily swap implementations)
   - 🔄 **Adapted for our architecture**: Used Clean Architecture layers instead of Unity MonoBehaviours

**Integration with VS_009 (Future)**:
When Item Definition System lands:
1. Add `ItemShape` to Item entity (width, height, bool array)
2. Update `AddItemAt()` to query Item.Shape for multi-cell collision
3. Add rotation support (rotate shape 90° before placement check)
4. Update UI to render multi-cell items spanning grid cells

**Tech Lead Decision** (2025-10-02):
- ✅ **Approved architecture**: Dual-mode extension of VS_008 (not replacement)
- ✅ **Scope validated**: 1×1 items sufficient for Phase 1, VS_009 enables multi-cell later
- ✅ **Complexity**: L-sized (12h estimate reasonable for 4 phases + backwards compat testing)
- ✅ **Risk**: Low (extends proven VS_008, well-tested reference implementation exists)
- **Next step**: Product Owner approval, then prioritize vs VS_007 (movement interruption)

**Depends On**:
- VS_008 (Slot-based Inventory) ✅ Complete
- VS_009 (Item Definition System) - Optional (only for multi-cell shapes)

**Blocks**:
- VS_013 (Container System) - Needs spatial grid for nested inventories
- Full Phase 2 itemization milestone

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