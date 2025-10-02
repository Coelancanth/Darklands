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

