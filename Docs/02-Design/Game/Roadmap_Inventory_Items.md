# Inventory & Items Roadmap

**Purpose**: High-level roadmap for Darklands inventory and item systems - NeoScavenger-inspired Tetris grid with drag-and-drop interaction.

**Last Updated**: 2025-10-09 23:11 (Product Owner: Extracted inventory roadmap from main roadmap, high-level only)

**Parent Document**: [Roadmap.md](Roadmap.md#inventory--items) - Main project roadmap

---

## Quick Navigation

**Core Systems**:
- [Vision & Philosophy](#vision--philosophy)
- [Current State](#current-state)
- [Completed Features](#completed-features)
- [Planned Features](#planned-features)

**Planned Systems**:
- [Item Stacking](#item-stacking-system-planned)
- [Ground Loot](#ground-loot-system-planned)
- [Nested Containers](#container-system---nested-inventories-planned)
- [Interaction & UX](#ux--interaction-improvements-planned)

---

## Vision & Philosophy

**Vision**: NeoScavenger-inspired spatial inventory - Tetris grid creates meaningful trade-offs (carry weapons OR consumables, not both).

**Philosophy**:
- **Spatial constraints** - Items have shapes (sword 1×3, armor 2×3, potion 1×1)
- **Weight matters** - Carrying capacity affects movement and action speed
- **Looting as puzzle** - Tactical decisions about what to keep vs leave behind
- **Designer empowerment** - TileSet metadata defines items (no code changes)
- **Drag-and-drop UX** - Natural interaction model (NeoScavenger pattern)

**Design Principles**:
1. **Inventory = puzzle** - Limited space forces meaningful choices
2. **TileSet-driven** - Designers add items visually (zero code changes)
3. **Shape-based collision** - Supports complex L/T-shapes (not just rectangles)
4. **Component separation** - Clean boundaries (Core = logic, Presentation = UI)

---

## Current State

**Completed Systems**:
- ✅ **VS_008**: Slot-based inventory MVP (20 slots, add/remove, capacity enforcement)
- ✅ **VS_009**: Item Definition System (TileSet metadata-driven, designer empowerment)
- ✅ **VS_018 Phase 1**: Spatial Tetris inventory (L/T-shapes, drag-drop, rotation, type filtering)

**Missing Systems**:
- ❌ **Item Stacking** - No stack quantity tracking (arrows, consumables)
- ❌ **Equipment Slots** - Can't equip items (see [Roadmap_Stats_Progression.md](Roadmap_Stats_Progression.md))
- ❌ **Ground Loot** - Items can't exist at map positions (enemy drops, dungeon loot)
- ❌ **Nested Containers** - Items can't BE containers (bags, quivers)
- ❌ **Click-to-Pick UX** - Still using drag-drop (Phase 2 improvement)

**Architectural Foundation**:
- ✅ ItemShape value object (coordinate-based masks for L/T-shapes)
- ✅ TileSet custom data layers (item properties without code changes)
- ✅ Component pattern (Actor + IInventoryComponent)
- ✅ Clean separation (Core = logic, Presentation = UI)

---

## Completed Features

### VS_008: Slot-Based Inventory System (MVP)

**Status**: Complete (2025-10-02) | **Size**: M (5-6.5h) | **Tests**: 23 passing

**What**: Simple slot-based inventory (20 slots) with add/remove operations.

**Delivered**:
- 20-slot inventory (fixed capacity)
- ItemId primitive in Domain/Common
- Add/remove operations with capacity enforcement
- UI panel with GridContainer (10×2 slots)
- Query-based refresh (no events in MVP)

**Key Decision**: Inventory stores ItemId references (not Item objects) - clean separation between container logic and item definitions.

**Archive**: [Completed_Backlog_2025-10.md](../../07-Archive/Completed_Backlog_2025-10.md#vs_008-slot-based-inventory-system)

---

### VS_009: Item Definition System (TileSet Metadata-Driven)

**Status**: Complete (2025-10-02) | **Size**: M (6-7h) | **Tests**: 57 passing

**What**: Item catalog using Godot TileSet custom data layers (designer-driven).

**Delivered**:
- TileSet custom data layers (item_name, item_type, max_stack_size)
- TileSetItemRepository auto-discovers items at startup
- ItemSpriteNode renders sprites (TextureRect + KeepAspectCentered)
- Showcase scene displays 10 items with metadata

**Key Decision**: TileSet metadata (not JSON/C# definitions) - designers add items visually in Godot editor with zero code changes, single source of truth for sprites + properties.

**Designer Empowerment**: Add new item = edit TileSet custom data in Inspector (no programmer needed!)

**Archive**: [Completed_Backlog_2025-10.md](../../07-Archive/Completed_Backlog_2025-10.md#vs_009-item-definition-system)

---

### VS_018: Spatial Inventory System (Tetris Grid)

**Status**: Phase 1 Complete (2025-10-04) | **Size**: XL (ongoing) | **Tests**: 359 passing

**What**: NeoScavenger-inspired Tetris inventory with drag-drop, multi-cell items, L/T-shapes, and rotation.

**Phase 1 Delivered**:
- Tetris-style spatial inventory with drag-drop interaction
- Multi-cell items with L/T-shape support (coordinate-based masks)
- 90° rotation during drag (scroll wheel)
- Type filtering (equipment slots reject wrong item types)
- Component separation (EquipmentSlotNode vs InventoryContainerNode)
- TileSet-driven ItemShape encoding ("custom:0,0;1,0;1,1")
- Cell-by-cell collision detection (supports complex shapes)

**Architecture Achievements**:
- ItemShape value object (coordinate masks for L/T-shapes)
- InventoryRenderHelper (DRY rendering across components)
- Zero business logic in Presentation (SSOT in Core)
- All 359 tests GREEN

**Archive**: [Completed_Backlog_2025-10.md](../../07-Archive/Completed_Backlog_2025-10.md#vs_018-spatial-inventory-l-shapes)

---

## Planned Features

### Item Stacking System (PLANNED)

**Status**: Proposed (Deferred) | **Priority**: Ideas (defer until consumables exist)

**What**: Stackable item support (e.g., "5× Branch", "20× Arrow")

**Why**:
- Reduces inventory clutter for consumables and ammo
- Reference: NeoScavenger nStackLimit (branches=5, ammo=20, unique items=1)

**Scope** (high-level):
- Stack limits per item type (configurable in TileSet metadata)
- UI displays stack count overlay on slot visuals
- Split/merge stack operations
- Benefits: Reduces clutter ONLY IF consumables/ammo create problem

**Dependencies**:
- **Blocker**: No consumables/ammo implemented yet!
- **Prerequisite**: Need consumable items (potions, arrows, crafting materials) first

**Product Owner Decision**: Defer until we have items that NEED stacking (currently premature).

---

### Ground Loot System (PLANNED)

**Status**: Proposed | **Priority**: Important (enables dungeon loot, enemy drops)

**What**: Items at map positions (dungeon loot, enemy drops, player discards)

**Why**:
- Foundation for dungeon generation (loot placement)
- Enemy death integration (drop equipped items)
- Player discard (drop unwanted items from inventory)

**Scope** (high-level):
- WorldItemRepository: Track items at map positions
- PickupItem / DropItem commands
- UI: Visual indicators on tiles with loot (sparkle effect, item sprite)
- Integration: Enemy dies → drops equipped items to ground

**Dependencies**:
- **Prerequisite**: VS_032 Equipment System (enemies need equipment to drop!)
- **Integration**: Dungeon generation system (loot placement)

**Blocks**: Nothing (combat works without ground loot, just no drops)

---

### Container System - Nested Inventories (PLANNED)

**Status**: Proposed (Maybe Never) | **Priority**: Ideas (defer indefinitely)

**What**: Items can BE containers (bags, pouches, quivers)

**Why**:
- Organization (ammo in quiver, potions in pouch)
- Specialization (quiver accepts arrows only, waterskin accepts liquids only)
- Reference: NeoScavenger aCapacities ("4x6"), aContentIDs (whitelisted types)

**Scope** (high-level):
- Nested spatial grids (Bag 4×6 can contain Pouch 2×2)
- Container properties (capacity, allowed item types)
- Type filtering (quiver = arrows only, waterskin = liquids only)
- Double-click to open container view

**Dependencies**:
- **Prerequisite**: Item Stacking (containers most useful with stacked consumables)
- **Prerequisite**: Many item types (need variety to make organization worthwhile)

**Product Owner Decision**: **Maybe never implement** - adds complexity without clear player value. Reconsider ONLY IF:
1. Item Stacking implemented AND insufficient for clutter problem
2. Players explicitly request organization features
3. Specialized containers (quiver = +1 arrow range) add tactical depth

**Decision Point**: If Item Stacking reduces inventory clutter sufficiently, nested containers may be unnecessary complexity.

---

### UX & Interaction Improvements (PLANNED)

**Status**: VS_018 Phase 2 (Deferred until Phase 1 playtested) | **Priority**: Important

**What**: UX improvements for spatial inventory - click-to-pick, context menus, tooltips, auto-sort.

**Phase 2 Feature List** (9 improvements):

1. **Click-to-Pick Interaction** (replace drag-drop)
   - Click item → pick up (highlight follows cursor)
   - Click destination → drop item
   - More tactile than drag-drop, easier on trackpads

2. **1v1 Swap Support**
   - Click occupied slot with picked item → swap items (no intermediate drop)
   - Example: Swap weapon in hand directly with weapon in inventory

3. **Item Interaction System**
   - Right-click item → context menu (Use, Split, Examine, Drop)
   - Use consumables from inventory
   - Examine for detailed stats/lore

4. **Stack Operations** (depends on Item Stacking)
   - Split stack (arrows ×20 → ×10 + ×10)
   - Merge stacks (arrows ×5 + arrows ×5 → arrows ×10)
   - Stack count overlay on sprites

5. **Auto-Sort**
   - Sort by type (weapons, consumables, tools)
   - Sort by size (fill gaps efficiently)
   - Sort by value/weight

6. **Container Preview** (depends on Nested Containers)
   - Hover over bag → tooltip shows contents preview (mini grid)
   - Quick view without opening full container

7. **Ground Item Interaction** (depends on Ground Loot)
   - Pick up items from environment (dungeon loot)
   - Visual indicators on tiles with items

8. **Rich Tooltips**
   - Item stats (damage, weight, durability)
   - Comparison tooltips (equipped vs inventory item)
   - Lore/description text

9. **Keyboard Shortcuts**
   - Tab = toggle inventory panel
   - R = rotate item during pick
   - Q = quick-use consumable (health potion)

**Product Owner Decision**: Prioritize features AFTER Phase 1 playtesting feedback - let players tell us what UX pain points matter most!

---

## Integration Points

**Combat System** (VS_020):
- Equipment slots interact with inventory (equip/unequip moves items)
- Ground loot from enemy deaths (equipped items drop to ground)

**Progression System** (VS_032):
- Equipment slots system (main hand, off hand, armor)
- Stats affect carrying capacity (Strength × 3 = kg capacity)
- Weight system affects action time costs

**Dungeon Generation** (Future):
- Ground loot placement (treasure chests, floor items)
- Enemy inventory templates (goblins spawn with crude weapons)

**Crafting System** (Far Future):
- Item consumption (use 2× branches + 1× rope → makeshift spear)
- Durability system (weapons degrade, need repairs)

---

## Next Steps

**Immediate Priority** (Nothing! VS_032 Equipment first):
- ⏳ **Wait for VS_032** (Equipment & Stats System) - Equipment slots needed before ground loot makes sense

**After VS_032 Complete**:
- **Product Owner** decides: Ground Loot system next? Or Enemy AI?
- **Test Specialist** validates: Is spatial inventory fun? Or frustrating?
- **Product Owner** reviews Phase 2 UX list: Which improvements matter most?

**Future Work** (Deferred):
- Item Stacking (ONLY IF consumables create clutter problem)
- Nested Containers (MAYBE NEVER - complexity without clear value)
- VS_018 Phase 2 UX (prioritize after playtesting feedback)

**Product Owner Decisions Needed**:
- Approve Ground Loot system after VS_032?
- Defer Item Stacking until consumables exist?
- Cancel Nested Containers entirely (unnecessary complexity)?

---

**Last Updated**: 2025-10-09 23:11
**Status**: VS_018 Phase 1 complete (spatial grid working!), Phase 2 deferred (UX improvements)
**Owner**: Product Owner (roadmap maintenance), Tech Lead (future breakdowns), Dev Engineer (implementation)

---

*This roadmap provides high-level overview of Darklands inventory and item systems. For technical implementation details, see VS item breakdowns in backlog after Tech Lead approval.*
