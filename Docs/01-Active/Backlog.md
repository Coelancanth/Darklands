# Darklands Development Backlog


**Last Updated**: 2025-10-02 10:48 (Tech Lead: Added VS_008 Inventory System with full specification)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 004
- **Next TD**: 003
- **Next VS**: 009


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

*Recently completed and archived (2025-10-01):*
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

### VS_008: Slot-Based Inventory System (MVP) ⭐ **AWAITING APPROVAL**

**Status**: Tech Lead Review Complete (Awaiting Product Owner Approval)
**Owner**: Tech Lead → Dev Engineer (after approval)
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

**Phase**: All 4 phases (Domain minimal, Application + Infrastructure core, Presentation UI prompts

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