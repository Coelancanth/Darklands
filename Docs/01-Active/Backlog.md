# Darklands Development Backlog


**Last Updated**: 2025-10-02 22:28 (Dev Engineer: VS_009 Phase 3 complete - TileSetItemRepository + 13 contract tests)

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

### VS_009: Item Definition System (TileSet Metadata-Driven) ⭐ **READY FOR APPROVAL**

**Status**: Proposed (Tech Lead architecture complete - TileSet custom data approach)
**Owner**: Tech Lead → Product Owner (for approval)
**Size**: M (6-7h across 4 phases - simpler with Godot-native metadata!)
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

**Phase 4 - Presentation** (~1h - Simple Sprite Display):
- `ItemSpriteNode.cs`: Renders item sprite using TileSet
  ```csharp
  public void DisplayItem(Item item)
  {
      var atlasSource = (TileSetAtlasSource)ItemTileSet.GetSource(0);
      Texture = atlasSource.Texture;
      RegionEnabled = true;
      RegionRect = atlasSource.GetTileTextureRegion(new Vector2I(item.AtlasX, item.AtlasY));
      // Sprite shows at native size - no grid spanning logic needed
  }
  ```
- **Demo scene**: Item showcase (NOT spatial inventory!)
  - VBoxContainer or simple grid
  - Load all items from repository → display sprites + names
  - Verify items auto-discovered from TileSet
- **Scope**: Just prove items load and render correctly
  - ❌ NOT implementing spatial grid (that's VS_018)
  - ❌ NOT implementing drag-drop (that's VS_018)
  - ✅ Just: Item catalog works, sprites display
- **Tests**: All items render, metadata displays correctly (name, weight, etc.)

**Done When**:
- ✅ **Phase 0**: item_sprites.tres has 4 custom data layers + metadata on all tiles
- ✅ **Phase 1-3**: 24 tests pass (<3s total)
  - Item.CreateFromTileSet reads 4 custom fields + size_in_atlas
  - Item.IsStackable computed correctly (true when max_stack > 1)
  - Repository auto-discovers all items from TileSet
- ✅ **Phase 4**: Item showcase scene displays all items with sprites + metadata
- ✅ **Acceptance Test**: Add "Silver Dagger" tile to TileSet with metadata → auto-appears in showcase (zero code!)
- ✅ **Scope Validation**: VS_009 provides item catalog foundation, NOT spatial inventory UI
- ✅ ADR-002: Core has zero Godot dependencies

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