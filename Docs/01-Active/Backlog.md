# Darklands Development Backlog


**Last Updated**: 2025-10-11 04:43 (Dev Engineer: TD_019 Phase 3 complete - All 543 Core tests GREEN; Presentation layer migration deferred)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 010
- **Next TD**: 021
- **Next VS**: 034


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

### VS_032: Equipment Slots System
**Status**: In Progress (Phase 1-4/6 Complete ✅✅✅✅)
**Owner**: Dev Engineer (Phase 5 next - Data-Driven Equipment)
**Size**: L (15-20h total, 6 phases)
**Priority**: Critical (foundation for combat depth, blocks proficiency/armor/AI)
**Markers**: [ARCHITECTURE] [DATA-DRIVEN] [BREAKING-CHANGE]

**What**: Equipment slot system (main hand, off hand, head, torso, legs) - actors can equip items from inventory, equipment affects combat.

**Why**:
- **Combat depth NOW** - Equipment defines capabilities (warrior with sword vs bare hands)
- **Build variety** - Heavy armor vs light armor creates different playstyles
- **Unblocks future** - Proficiency (track weapon usage), Ground Loot (enemies drop equipped items), Enemy AI (gear defines enemy capabilities)
- **Foundation** - Stats/armor/proficiency ALL depend on equipment system

**How** (6-Phase Implementation):

**ARCHITECTURE DECISION**: Equipment = Separate Component (NOT Inventory)
- **Rationale**: Equipment needs slot-based storage (5 named slots), NOT spatial grid. Simpler domain model than 5 separate Inventory entities.
- **Breaking Change**: EquipmentSlotNode (Presentation prototype) currently uses GetInventoryQuery - will be updated to GetEquippedItemsQuery.

**Phase 1: Equipment Domain (Core)** - ✅ **COMPLETE** (2025-10-10 01:50)
- ✅ Create `EquipmentSlot` enum (MainHand, OffHand, Head, Torso, Legs)
- ✅ Create `IEquipmentComponent` interface (EquipItem, UnequipItem, GetEquippedItem, IsSlotOccupied)
- ✅ Create `EquipmentComponent` implementation (Dictionary<EquipmentSlot, ItemId> storage)
- ✅ Two-handed weapon validation (same ItemId in both MainHand + OffHand, atomic unequip)
- ✅ Domain unit tests (20 tests, all GREEN)
- **Implementation Summary**:
  - Feature-based architecture (`Features/Equipment/Domain/`) following ADR-004
  - Two-handed pattern: Store same ItemId in both hands (elegant, no sentinels)
  - Helper method `IsTwoHandedWeaponEquipped()` for clean atomic operations
  - i18n error keys (ADR-005): `ERROR_EQUIPMENT_*` translation keys
  - Result<T> pattern (ADR-003): All failable operations return Result
  - Component inheritance: `IEquipmentComponent : IComponent` for Actor integration
  - Test coverage: Constructor (2), EquipItem (9), UnequipItem (5), GetEquippedItem (3), IsSlotOccupied (2) = 21 scenarios
- **Quality**: All 448 tests GREEN (428 existing + 20 new Equipment tests), zero regressions

**Phase 2: Equipment Commands (Core Application)** - ✅ **COMPLETE** (2025-10-10 02:00)
- ✅ `EquipItemCommand(ActorId, ItemId, EquipmentSlot, IsTwoHanded)` + Handler
  - ATOMIC: Remove from inventory → Add to equipment (rollback on failure)
  - Auto-adds EquipmentComponent if actor doesn't have one
- ✅ `UnequipItemCommand(ActorId, EquipmentSlot)` + Handler
  - ATOMIC: Remove from equipment → Add to inventory (rollback on failure)
  - Captures two-handed state BEFORE unequip (required for rollback)
- ✅ `SwapEquipmentCommand(ActorId, NewItemId, EquipmentSlot, IsTwoHanded)` + Handler
  - ATOMIC: 4-step transaction (unequip old → add old to inv → remove new from inv → equip new)
  - Multi-level rollback (each step can fail, requires restoring all previous steps)
- ✅ Command handler integration tests (10 tests, all GREEN)
- **Implementation Summary**:
  - Atomic transactions with rollback on ANY failure (no item loss/duplication)
  - Repository pattern: IActorRepository (reference-based, no SaveAsync), IInventoryRepository (explicit SaveAsync)
  - Two-handed weapon detection before unequip (MainHand.ItemId == OffHand.ItemId)
  - Translation keys: ERROR_EQUIPMENT_* (ADR-005 i18n)
  - CRITICAL logging for item loss scenarios (rollback failures)
- **Quality**: All 478 tests GREEN (448 existing + 30 new Equipment tests), zero regressions

**Phase 3: Equipment Queries (Core Application)** - ✅ **COMPLETE** (2025-10-10 02:08)
- ✅ `GetEquippedItemsQuery(ActorId)` → IReadOnlyDictionary<EquipmentSlot, ItemId>
  - Returns only occupied slots (empty slots not in dictionary - use TryGetValue pattern)
  - No equipment component = empty dictionary (graceful degradation, not error)
  - Two-handed weapons: Same ItemId in both MainHand + OffHand (UI detects via equality)
- ✅ `GetEquippedWeaponQuery(ActorId)` → ItemId
  - Convenience query for combat - returns MainHand weapon only
  - No weapon = ERROR_EQUIPMENT_NO_WEAPON_EQUIPPED (combat interprets as unarmed)
- ✅ Query handler tests (10 tests, all GREEN)
- **Implementation Summary**:
  - Queries return ItemId only (minimal) - Presentation joins with item metadata separately
  - Actor not found = failure (invalid ActorId)
  - No equipment component = GetEquippedItems returns empty dict, GetEquippedWeapon returns failure
- **Quality**: All 488 tests GREEN (448 existing + 40 new Equipment tests), zero regressions

**Phase 4: Equipment Presentation** - ✅ **COMPLETE** (2025-10-10 02:27)
- ✅ Created `EquipmentPanelNode.cs` (257 lines): VBoxContainer managing 5 equipment slots
  - Parent-driven data pattern: Queries `GetEquippedItemsQuery` ONCE (efficient!)
  - Pushes `ItemDto` to child slots via `UpdateDisplay()` method
  - Re-emits signals for cross-container refresh in test controller
- ✅ Refactored `EquipmentSlotNode.cs` (587 lines): Equipment-based (not Inventory-based)
  - **Added**: `EquipmentSlot Slot` property, `UpdateDisplay(ItemDto?)` for parent-driven data
  - **Removed**: `LoadSlotAsync()` self-loading (parent panel owns data queries now)
  - **Simplified**: `_CanDropData()` uses cached `_currentItemId` (80% query reduction!)
  - **Updated**: Commands use `EquipItemCommand`, `UnequipItemCommand`, `SwapEquipmentCommand`
  - **Added**: `ValidateItemTypeForSlot()` - basic weapon/armor type checking
- ✅ Updated `SpatialInventoryTestController.cs`: Replaced single weapon slot → EquipmentPanelNode
  - Panel displays all 5 slots: **MainHand, OffHand, Head, Torso, Legs**
  - Integrated into cross-container refresh system
- ✅ Updated `SpatialInventoryTestScene.tscn`: Increased placeholder height (150×600), updated instructions
- **Implementation Summary**:
  - **Efficiency**: 1 query for all slots (vs 5 queries/refresh) = 80% reduction
  - **Performance**: 0 queries during drag (vs 30-60/sec) - uses cached state
  - **Unidirectional data flow**: Commands up (slot → panel), data down (panel → slots)
  - **Layout fix**: VBoxContainer with proper spacing (separation: 8px, min sizes enforced)
  - **Type validation**: Weapons to MainHand/OffHand, armor to Head/Torso/Legs
- **Quality**: All 488 tests GREEN (448 existing + 40 Equipment tests), zero regressions
- **Manual Testing Ready**: Open SpatialInventoryTestScene, test drag-drop equip/unequip/swap with 5 visible slots

**Phase 5: Data-Driven Equipment (ADR-006)** - 2h
- Add `StartingEquipment` property to ActorTemplate (Dictionary<EquipmentSlot, string>)
- Update ActorFactory to equip starting equipment on spawn
- Update templates: player.tres (iron sword, leather armor), goblin.tres (club, ragged cloth)

**Phase 6: Combat Integration** - 1-2h
- Update ExecuteAttackCommandHandler to use GetEquippedWeaponQuery
- Deprecate WeaponComponent.EquippedWeapon (add [Obsolete] attribute with migration message)

**Testing Strategy**:

**Automated Tests** (Phases 1-3): `./scripts/core/build.ps1 test --filter "Category=Equipment"`
- 35-45 unit/integration tests (Domain 15-20, Commands 12-15, Queries 5-8, Integration 5-7)
- Core business logic coverage (atomic operations, rollback, two-handed validation)

**Manual Validation** (Phases 4-6): [SpatialInventoryTestScene.tscn](../../godot_project/test_scenes/SpatialInventoryTestScene.tscn)
- Test Scene: Already has EquipmentSlotNode prototype + drag-drop infrastructure
- 7 Manual Test Scenarios:
  1. Equip from Inventory (drag sword → MainHand)
  2. Unequip to Inventory (drag from MainHand → backpack, spatial placement preserved)
  3. Swap Equipment (drag new weapon to occupied slot, atomic swap)
  4. Two-Handed Weapon (occupies MainHand + OffHand, blocks shield equip)
  5. Slot Type Filtering (drag potion → MainHand, red highlight rejection)
  6. Equipment Panel Display (5 slots visible, starting equipment, tooltips)
  7. Combat Integration (equipped weapon damage, unequipped = error)
- **Deferred**: Inventory keybinding ('I' key) - not needed for test scene validation, add in main game UX

**Done When**:
- ✅ Actor has IEquipmentComponent with 5 slots
- ✅ Warrior can equip sword from inventory → MainHand slot (EquipItemCommand)
- ✅ Unequip sword → returns to inventory with spatial placement
- ✅ Two-handed weapon validation (requires MainHand + OffHand both empty)
- ✅ ActorTemplate.tres configures starting equipment (player.tres, goblin.tres)
- ✅ EquipmentPanelNode displays 5 slots with equipped items in SpatialInventoryTestScene
- ✅ Combat system uses GetEquippedWeaponQuery (not direct WeaponComponent)
- ✅ Tests: 35-45 automated tests GREEN + 7 manual test scenarios validated
- ✅ All 428+ existing tests GREEN (no regressions)

**Depends On**: VS_018 ✅ (Spatial Inventory), VS_009 ✅ (Item System)
**Blocks**: Stats/Attributes, Proficiency System, Ground Loot, Enemy AI
**Enables**: Manual item creation phase (10-20 items) → validates VS_033 Item Editor need

**Product Owner Decision** (2025-10-10): Thin scope - Equipment Slots ONLY. Defer stats/attributes/fatigue to separate VS items after this validated. After VS_032 complete, designer creates 10-20 items manually (Phase 1) to validate Item Editor need before building it.

**Tech Lead Decision** (2025-10-10):
- **Equipment ≠ Inventory**: Slot-based component (simpler) vs spatial grid inventories (over-engineered for equipment)
- **Breaking Change Acceptable**: EquipmentSlotNode is prototype (TD_003), not production code - refactor to new architecture is clean migration
- **Phased Approach**: 6 phases enforce Core-first discipline (Domain → Application → Presentation → Data-Driven)
- **Test Coverage**: 35-45 tests ensure atomic operations, rollback, two-handed validation
- **Size Revision**: M (6-8h) → L (15-20h) - original estimate underestimated 6-phase scope + tests
- **Integration Strategy**: Deprecate WeaponComponent.EquippedWeapon (not remove) - migration path for existing combat code

---

## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

*Recently completed and archived (2025-10-11 05:26):*
- **TD_019**: Inventory-First Architecture (InventoryId Primary Key) - All 5 phases complete! Redesigned from Actor-centric (1:1) to Inventory-First (independent inventories with optional owner). InventoryId primary key, 16 commands/queries updated, 543 Core tests GREEN, 5 Presentation files updated with 3 runtime drag-drop bugs fixed, all obsolete methods removed. Unlocks squad-based gameplay, loot containers, shared inventories, cross-actor equip operations. ✅ (2025-10-11 05:21) *See: [Completed_Backlog_2025-10_Part3.md](../07-Archive/Completed_Backlog_2025-10_Part3.md) for full archive*

---

### TD_020: Extract Drag-Drop Handler (Reduce InventoryContainerNode Complexity)
**Status**: Proposed
**Owner**: Tech Lead (design) → Dev Engineer (implementation)
**Size**: M (4-6h - refactoring with test coverage)
**Priority**: Important (improves maintainability, reduces complexity)
**Markers**: [REFACTORING] [TECHNICAL-DEBT] [SRP-VIOLATION]

**What**: Extract drag-drop logic from `InventoryContainerNode` (1,284 lines) into dedicated `InventoryDragDropHandler` class.

**Why**:
- **God Object Smell**: `InventoryContainerNode` has ~50 methods with ~8 responsibilities (Godot lifecycle, drag-drop, rotation, rendering, highlighting, async ops, events, state)
- **Single Responsibility Principle**: Drag-drop logic (~400-500 lines) is independent concern, pollutes container node
- **Maintainability**: Changes to drag-drop require navigating 1,284-line file, risk breaking unrelated features
- **Testability**: Cannot unit test drag-drop logic in isolation (coupled to Godot node lifecycle)
- **Reusability**: Equipment slots duplicate drag-drop patterns (shared handler eliminates duplication)

**Current State Analysis**:
```
InventoryContainerNode.cs: 1,284 lines ⚠️
├─ Drag-drop logic: ~400-500 lines (35-40%)
│  ├─ _GetDragData() + CreateDragPreview()
│  ├─ _CanDropData() + highlight calculations
│  ├─ _DropData() + MoveItemAsync/UnequipItemAsync
│  └─ Rotation handling (_Input scroll wheel)
├─ Rendering logic: ~200-250 lines
├─ Async operations: ~150-200 lines
├─ Event handling: ~100 lines
└─ Godot lifecycle: ~100 lines

Core Layer: ✅ CLEAN (15 small command/query files, exemplar CQRS)
```

**Proposed Design**:
```csharp
// NEW: Components/Inventory/InventoryDragDropHandler.cs (~400 lines)
/// <summary>
/// Handles drag-drop operations for inventory containers.
/// Shared across InventoryContainerNode and EquipmentSlotNode for consistent UX.
/// </summary>
public partial class InventoryDragDropHandler
{
    private readonly InventoryContainerNode _container;  // Reference to parent node
    private readonly IMediator _mediator;
    private readonly ILogger<InventoryDragDropHandler> _logger;

    // Shared state (static for cross-container drags)
    internal static Rotation _sharedDragRotation = default;
    internal static TextureRect? _sharedDragPreviewSprite = null;

    public InventoryDragDropHandler(
        InventoryContainerNode container,
        IMediator mediator,
        ILogger<InventoryDragDropHandler> logger)
    {
        _container = container;
        _mediator = mediator;
        _logger = logger;
    }

    // Drag operations (extracted from InventoryContainerNode)
    public Variant GetDragData(Vector2 atPosition) { /* 80 lines */ }
    public bool CanDropData(Vector2 atPosition, Variant data) { /* 120 lines */ }
    public void DropData(Vector2 atPosition, Variant data) { /* 150 lines */ }
    public void HandleRotationInput(InputEventMouseButton mouseButton) { /* 60 lines */ }
    public void CreateRotatableDragPreview(ItemId itemId) { /* 80 lines */ }
    public void CancelDrag() { /* 20 lines */ }
}

// UPDATED: InventoryContainerNode.cs (~850 lines - 33% smaller!)
public partial class InventoryContainerNode : PanelContainer
{
    private InventoryDragDropHandler _dragHandler = null!;

    public override void _Ready()
    {
        base._Ready();
        // ... existing initialization ...
        _dragHandler = new InventoryDragDropHandler(this, _mediator, _logger);
    }

    // Delegate to handler (thin wrappers)
    public override Variant _GetDragData(Vector2 atPosition) =>
        _dragHandler.GetDragData(atPosition);

    public override bool _CanDropData(Vector2 atPosition, Variant data) =>
        _dragHandler.CanDropData(atPosition, data);

    public override void _DropData(Vector2 atPosition, Variant data) =>
        _dragHandler.DropData(atPosition, data);

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseButton mouseButton)
        {
            _dragHandler.HandleRotationInput(mouseButton);  // Rotation extraction
        }
    }
}
```

**Benefits**:
- ✅ **Reduced Complexity**: 1,284 → ~850 lines (33% reduction!)
- ✅ **Single Responsibility**: Container node focuses on rendering/lifecycle, handler focuses on drag-drop
- ✅ **Testability**: Can unit test drag-drop logic independently (mock container interface)
- ✅ **Reusability**: `EquipmentSlotNode` can reuse same handler (eliminate 200 lines of duplication)
- ✅ **Maintainability**: Changes to drag-drop isolated to single file, reduced regression risk

**How** (3-Phase Incremental Extraction):

**Phase 1: Extract Drag State + Preview (2h)**
- Create `InventoryDragDropHandler` class skeleton
- Move `_sharedDragRotation`, `_sharedDragPreviewSprite` to handler (static fields)
- Move `CreateRotatableDragPreview()` method (80 lines)
- Update `InventoryContainerNode` to delegate preview creation
- Run tests: All 488 tests GREEN ✅

**Phase 2: Extract Drag Operations (2h)**
- Move `_GetDragData()`, `_CanDropData()`, `_DropData()` to handler
- Move `HandleRotationInput()` logic from `_Input()`
- Update container node to delegate all drag operations
- Add internal helper methods to handler (cell position calculation, highlight logic)
- Run tests: All 488 tests GREEN ✅

**Phase 3: Refactor Equipment Slot (1-2h)**
- Update `EquipmentSlotNode` to use shared `InventoryDragDropHandler`
- Remove duplicated drag-drop code (~200 lines)
- Verify equipment drag-drop still works (BR_008, BR_009 tests)
- Run tests: All 488 tests GREEN ✅

**Done When**:
- ✅ `InventoryDragDropHandler` class created (~400 lines)
- ✅ `InventoryContainerNode` reduced to ~850 lines (33% smaller)
- ✅ `EquipmentSlotNode` reuses handler (200 lines eliminated)
- ✅ All drag-drop functionality preserved (no behavior changes)
- ✅ All tests GREEN (488+ tests, zero regressions)
- ✅ Equipment drag-drop still works (BR_008, BR_009 validation)

**Depends On**: None (can start immediately, or defer until after TD_019)

**Sequencing Decision**:
- **Option A**: Do BEFORE TD_019 (smaller container node easier to refactor for Inventory-First)
- **Option B**: Do AFTER TD_019 (avoid merge conflicts, but TD_019 touches 1,284-line file)
- **Recommended**: **Option A** - Extract drag-drop handler first (makes TD_019 easier)

**Risks**:
- **Regression Risk**: Drag-drop is complex (~400 lines) - must verify all scenarios work (inventory→inventory, equipment→inventory, cross-container, rotation)
- **Static State**: Shared static fields (`_sharedDragRotation`) make testing harder (need careful cleanup between tests)
- **Godot Coupling**: Handler still tightly coupled to Godot types (Vector2, Variant, InputEvent) - cannot fully unit test

**Tech Lead Decision** (2025-10-10 03:50):
- **Approved for implementation**: Clear SRP violation, measurable complexity reduction (33%)
- **Sequence BEFORE TD_019**: Smaller files easier to refactor
- **Next Steps**:
  1. Complete VS_032 Phase 5-6 first (avoid conflicts)
  2. Implement TD_020 (Extract Drag-Drop Handler)
  3. Then implement TD_019 (Inventory-First) on cleaner codebase
- **Timeline**: Target after VS_032 done (~2-3 days from now)

---

*Recently completed and archived (2025-10-06):*
- **VS_020**: Basic Combat System (Attacks & Damage) - All 4 phases complete! Click-to-attack combat UI, component pattern (Actor + HealthComponent + WeaponComponent), ExecuteAttackCommand with range validation (melee adjacent, ranged line-of-sight), damage application, death handling bug fix. All 428 tests GREEN. Ready for VS_011 (Enemy AI). ✅ (2025-10-06 19:03) *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---

*Recently completed and archived (2025-10-04 19:35):*
- **VS_007**: Time-Unit Turn Queue System - Complete 4-phase implementation with natural mode detection, 49 new tests GREEN, 6 follow-ups complete. ✅ (2025-10-04 17:38)

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*

### VS_033: MVP Item Editor (Weapons + Armor Focus)
**Status**: Proposed (Build AFTER manual item creation phase)
**Owner**: Product Owner → Tech Lead (breakdown) → Dev Engineer (implement)
**Size**: L (15-20h)
**Priority**: Ideas (deferred until designer pain validated)
**Markers**: [TOOLING] [DESIGNER-UX]

**What**: Minimal viable Godot EditorPlugin for creating weapon/armor ItemTemplates with auto-wired i18n and component-based UI.

**Why**:
- **Eliminate CSV pain** - Designer never manually edits CSV files (auto-generates translation keys, auto-syncs en.csv)
- **Component selection UI** - Check boxes for Equippable + Weapon/Armor (vs manual SubResource creation in Inspector)
- **Validation before save** - Catch errors (duplicate keys, missing components) BEFORE runtime
- **80% of content** - Weapons + armor are most items, validates tooling investment

**Strategic Phasing** (do NOT skip ahead):
1. **Phase 1: Validate Pain** (2-4h manual work) - REQUIRED FIRST
   - After VS_032 complete, designer creates 10-20 items manually in Godot Inspector
   - Designer documents pain points: "Inspector tedious", "CSV editing error-prone", "No validation until runtime"
   - **Trigger**: Designer reports "Inspector workflow is too tedious for 20+ items"

2. **Phase 2: Build MVP** (15-20h) - ONLY if Phase 1 pain validated
   - Build EditorPlugin with 5 core features (see "How" below)
   - Focus: Weapons + armor ONLY (defer consumables/tools to Phase 3)
   - Effort: 15-20h (vs 30h full Item Editor by deferring advanced features)

3. **Phase 3: Expand** (when >30 items exist) - Future
   - Add consumables, tools, containers support
   - Add balance tools (DPS calculator, usage tracking)
   - Add batch operations (create 10 variants)

**How** (MVP scope - 5 core features):
1. **Component Selection UI** (3-4h) - Checkboxes: ☑ Equippable, ☑ Weapon, ☑ Armor (auto-show properties)
2. **Quick Templates** (2-3h) - Presets: "Weapon" (Equippable+Weapon), "Armor" (Equippable+Armor), "Shield" (Equippable+Armor+Weapon)
3. **Auto-Wired i18n** (4-5h) - Designer types "Iron Sword" → auto-generates ITEM_IRON_SWORD → auto-writes en.csv (ZERO manual CSV editing!)
4. **Component Validation** (3-4h) - "Weapon requires Equippable", "Duplicate key ITEM_IRON_SWORD", offer auto-fix
5. **Live Preview** (2-3h) - Show sprite + stats + translation key preview

**Deferred for MVP** (add in Phase 3):
- ❌ Balance comparison (DPS calculator, power curves)
- ❌ Usage tracking (which ActorTemplates use this item)
- ❌ Batch operations (create N variants)
- ❌ Consumables/tools support (weapons + armor = 80% of content)

**Done When** (Phase 2 - MVP):
- ✅ Designer creates iron_sword.tres in 2 minutes (vs 5+ minutes Inspector)
- ✅ Component selection via checkboxes (no manual SubResource creation)
- ✅ Zero manual CSV editing (auto-generates ITEM_IRON_SWORD, writes to en.csv)
- ✅ Validation before save (catches duplicate keys, missing components)
- ✅ Works for weapons + armor (can create sword, plate armor, shield)
- ✅ Designer reports: "Item Editor is MUCH faster than Inspector"

**Depends On**:
- VS_032 ✅ (must be complete - validates equipment system works)
- Phase 1 manual item creation (10-20 items) - validates pain is REAL, not hypothetical

**Blocks**: Nothing (tooling is parallel track - doesn't block gameplay features)

**Product Owner Decision** (2025-10-10):
- **Do NOT build until Phase 1 pain validated** - Must create 10-20 items manually first to validate:
  1. Component-based ItemTemplate architecture works (can actors equip items?)
  2. Inspector workflow pain is REAL (not hypothetical)
  3. CSV editing pain is REAL (manual key management sucks)
- **Rationale**: Tools solve REAL pain, not imaginary pain. Must feel pain before building solution.
- **Risk mitigation**: If Phase 1 shows "Inspector is workable", defer Item Editor and create more items (avoid 15-20h investment for low ROI).
- **Scope discipline**: MVP focuses weapons + armor (80% of content). Defer consumables/tools until 30+ items exist (avoid premature generalization).

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