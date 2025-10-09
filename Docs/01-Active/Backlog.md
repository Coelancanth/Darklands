# Darklands Development Backlog


**Last Updated**: 2025-10-10 02:08 (Dev Engineer: VS_032 Phase 3 complete - GetEquippedItemsQuery/GetEquippedWeaponQuery for UI + combat, 10 query tests GREEN, all 488 tests pass with zero regressions)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 019
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
**Status**: In Progress (Phase 1-3/6 Complete ✅✅✅)
**Owner**: Dev Engineer (Phase 4 next - Equipment Presentation)
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

**Phase 4: Equipment Presentation** - 3-4h
- Update `EquipmentSlotNode.cs`: Replace GetInventoryQuery with GetEquippedItemsQuery
- Create `EquipmentPanelNode.cs`: VBoxContainer with 5 EquipmentSlotNodes (MainHand, OffHand, Head, Torso, Legs)
- Update drag-drop to call EquipItemCommand (not MoveItemBetweenContainersCommand)
- Update `SpatialInventoryTestController.cs`: Replace weapon slot → EquipmentPanelNode

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

**No important items!** ✅ VS_020 completed and archived.

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