# Darklands Development Backlog


**Last Updated**: 2025-10-04 08:38 (Product Owner/Dev Engineer: Completed TD_005 - Root Cause First Principle, UX Pattern Recognition, Requirement Clarification Protocol added to dev-engineer.md)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 006 (TD_005 complete, counter unchanged)
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

*Recently completed and archived (2025-10-04):*
- **TD_005**: Persona & Protocol Updates - Updated dev-engineer.md with Root Cause First Principle (159 lines), UX Pattern Recognition (25 lines), Requirement Clarification Protocol (38 lines). Approved protocol update approach over adding UI/UX Designer persona. ✅ (2025-10-04)
- **BR_007**: Equipment Slot Visual Issues - Equipment slots showed L-shape highlights (3 cells) instead of 1×1. Fixed by overriding rotatedShape to ItemShape.CreateRectangle(1,1) for equipment slots. Also fixed sprite centering. ✅ (2025-10-04)
- **BR_006**: Cross-Container Rotation Highlights - Highlights didn't rotate during cross-container drag. Fixed with mouse warp hack (0.1px movement triggers _CanDropData). ✅ (2025-10-04)
- *See: [Completed_Backlog_2025-10.md](../07-Archive/Completed_Backlog_2025-10.md) for full archive*

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

### TD_003: Separate Equipment Slots from Spatial Inventory Container

**Status**: ✅ APPROVED (2025-10-04) - Ready for Dev Engineer
**Owner**: Tech Lead → Dev Engineer
**Size**: L (12-16h realistic estimate)
**Priority**: Important (Component Reusability + Complexity Reduction)
**Depends On**: None (can start immediately)
**Markers**: [ARCHITECTURE] [SEPARATION-OF-CONCERNS] [COMPONENT-DESIGN]

**What**: Refactor `SpatialInventoryContainerNode` into two separate components: `InventoryContainerNode` (Tetris grid) and `EquipmentSlotNode` (single-item swap)

**Why** (Component Reusability - VALID):
- **Reusability**: Character sheet needs 6 equipment slots - current design forces each to carry 800 lines of dead Tetris code
- **Single Responsibility Violation**: One class handles TWO different UI patterns (Tetris grid + Diablo 2 swap)
- **Future Features Blocked**: Cannot add helmet/chest/ring slots without instantiating 1372-line God Class
- **Complexity**: Equipment slot special cases scattered (4 `if (isEquipmentSlot)` branches in different methods)

**How** (Implementation Plan):
- **EquipmentSlotNode** (Single-item swap, ~400 lines) - **Build FIRST**:
  - Always 1×1 logical placement (ignores item's actual shape)
  - Swap-only UX (no collision check, always valid if type matches)
  - Centered sprite rendering (items appear centered regardless of size)
  - Single-cell highlight (always green 1×1, never red)
  - Simpler drag-drop (no rotation, no cross-container complexity)

- **InventoryContainerNode** (Tetris grid, ~800 lines) - **Extract SECOND**:
  - Multi-cell placement with L-shape collision
  - Rotation support via scroll wheel
  - Cross-container drag-drop
  - Highlight rendering (full shape, green/red validation)
  - Remove ALL `if (isEquipmentSlot)` branches (grep returns 0)

- **Shared Base Class** (Optional, evaluate during implementation):
  - Drag preview creation (atlas texture loading)
  - Common properties (CellSize, ItemTileSet, Mediator)
  - Decision: Only extract if duplication >100 lines

**Tech Lead Decision** (2025-10-04): **APPROVED - Component Design Pattern**

**Approval Rationale** (Reversed from initial rejection):

1. **Reusability is the Real Justification** ✅
   - Character sheet needs 6 equipment slots (helmet, chest, weapon, shield, ring×2)
   - Current approach: 6 instances × 1372 lines = 8232 lines loaded for simple swap UX
   - After separation: 6 instances × 400 lines = 2400 lines (saves 5832 lines of dead code)
   - **This isn't premature optimization - it's proper component design**

2. **Single Responsibility Principle** ✅
   - Equipment Slot: Type validation, swap, centered display
   - Inventory Grid: Spatial placement, rotation, L-shape collision
   - These are as different as `Button` vs `LineEdit` - don't unify them!

3. **Testing Benefits** ✅
   - Equipment tests: Swap rollback, type filtering, centering (simple, focused)
   - Inventory tests: Multi-cell placement, rotation, cross-container drag (complex, isolated)
   - Current: Every test runs through BOTH code paths (confusing, slow)

4. **Cost is Justified** 💰
   - Realistic estimate: 12-16h (includes test scene updates, regression testing, docs)
   - Benefit: Future character sheet feature becomes trivial (drop 6 EquipmentSlotNodes in scene)
   - ROI: Every future equipment feature gets simpler, faster to test

5. **Initial Rejection Was Wrong** 🙏
   - I focused on "bug fixing" instead of "component design"
   - User's challenge was correct: These should be separate reusable nodes
   - **Lesson learned**: YAGNI doesn't apply when designing reusable UI components

**Implementation Approach** (Recommended Order):

**Phase 1: Extract EquipmentSlotNode (4-6h)**
1. Copy SpatialInventoryContainerNode → EquipmentSlotNode.cs
2. Delete ALL Tetris-specific code (rotation, multi-cell, highlights)
3. Simplify to: 1×1 swap, centered sprite, type validation only
4. Update test scene: Replace weapon slot with new EquipmentSlotNode
5. Manual test: Swap dagger ↔ ray_gun
6. Commit: `refactor(inventory): Extract EquipmentSlotNode for reusability [TD_003 Phase 1/2]`

**Phase 2: Clean InventoryContainerNode (4-6h)**
1. Delete ALL equipment slot special cases from SpatialInventoryContainerNode
2. Rename file: SpatialInventoryContainerNode.cs → InventoryContainerNode.cs
3. Update backpack test scene: Use new InventoryContainerNode name
4. Verify: `grep -r "isEquipmentSlot" Components/` returns 0 results
5. Commit: `refactor(inventory): Remove equipment logic from InventoryContainerNode [TD_003 Phase 2/2]`

**Phase 3: Documentation & Cleanup (2-4h)**
1. Update ADR-002 (or create ADR-006): "Equipment Slot vs Inventory Grid Component Separation"
2. Update HANDBOOK: Component selection guide (when to use which)
3. Update Memory Bank (dev-engineer.md): Reusability pattern example
4. Regression test ALL inventory features (manual + 359 automated tests)

**Done When**:
- ✅ EquipmentSlotNode exists (~400 lines, focused on swap UX)
- ✅ InventoryContainerNode cleaned (~800 lines, zero equipment special cases)
- ✅ All 359 tests GREEN (backward compatibility)
- ✅ Test scene uses EquipmentSlotNode for weapon slot (manual test passes)
- ✅ Grep for "isEquipmentSlot" in Components/ returns 0 results
- ✅ Documentation updated (ADR + HANDBOOK + Memory Bank)

**Risk Mitigation**:
- ⚠️ Test scene breakage: Update scenes incrementally (weapon slot first, then backpack)
- ⚠️ Drag-drop regression: Extensive manual testing after each phase
- ⚠️ Shared code duplication: Only extract base class if >100 lines duplicated

**Success Metrics**:
- Character sheet feature becomes trivial (drop EquipmentSlotNodes in scene editor)
- Equipment slot bugs isolated to 400-line file (not 1372-line God Class)
- Each component testable independently (faster test cycles)

---

### TD_004: Move ALL Shape Calculation Logic to Core (SSOT) - **EXPANDED SCOPE**

**Status**: 🚧 IN PROGRESS (Dev Engineer - 2025-10-04)
**Owner**: Dev Engineer
**Size**: L (12-16h realistic - **expanded from M after full file analysis**)
**Priority**: Critical (Architectural compliance + **7 logic leaks found**)
**Depends On**: None (but **SHOULD DO BEFORE TD_003** - see sequencing note)
**Markers**: [ARCHITECTURE] [ADR-002] [SSOT] [SYSTEMIC-VIOLATION]

**Progress** (2025-10-04):
- ✅ Leak #1: CalculateHighlightCellsQuery implemented + all tests GREEN (5/5)
- ✅ GridOffset value object created
- 🚧 Leak #2-4, #7: GetItemRenderDataQuery (next)
- ⏳ Leak #5: SwapOrMoveItemCommand (pending)
- ⏳ Leak #6: Remove CanAcceptItemType dead code (pending)
- ⏳ Phase 2: Replace Presentation logic (pending)
- ⏳ Phase 3: Documentation + regression tests (pending)

**What**: Move **ALL shape calculation and business logic** from Presentation to Core (not just highlights!)

**Why** (COMPREHENSIVE Analysis - **7 Logic Leaks Found**):

After ultra-careful analysis of all 1372 lines, identified **SEVEN distinct business logic violations**:

**Logic Leak #1: Highlight Shape Calculation** (Lines 1057-1075) - *Original TD_004 scope*
- ❌ Presentation rotates `ItemShape` (business logic)
- ❌ Presentation applies equipment slot "1×1 override" rule (business rule)
- **Should be**: Core calculates, Presentation renders

**Logic Leak #2: Occupied Cell Calculation** (Lines 640-683) - ***NEW - Major violation!***
- ❌ Presentation calculates which cells items occupy (collision detection!)
- ❌ Duplicates Core's `Inventory.PlaceItemAt()` logic
- **Impact**: 43 lines of business logic rotating shapes, iterating cells
- **Should be**: Query Core for occupied cells, don't recalculate

**Logic Leak #3: Equipment Slot Centering** (Lines 853-871) - ***NEW***
- ❌ Presentation decides "equipment slots center items" (business rule!)
- ❌ Calculates center position using rotation math (business logic)
- **Should be**: Core provides render position, Presentation just uses pixel coords

**Logic Leak #4: Equipment Slot Detection** (Lines 478, 855, 1069) - ***NEW - Inconsistent!***
- ❌ Business rule "What is an equipment slot?" scattered in 3 places
- ❌ **INCONSISTENT**: Line 478 uses `ContainerType.WeaponOnly`, lines 855/1069 add grid size check
- **Should be**: Core property `ContainerType.IsEquipmentSlot`, Presentation reads it

**Logic Leak #5: Swap Logic** (Lines 476-491, 1122-1202) - ***NEW - 78 lines!***
- ❌ Presentation decides swap vs move based on container type (business decision)
- ❌ Implements entire swap algorithm with rollback (domain logic!)
- **Should be**: Core command handles swap/move decision and implementation

**Logic Leak #6: Type Validation** (Lines 1248-1258) - *Partial (dead code?)*
- ❌ Business rule "weapon slots only accept weapons" in Presentation
- ⚠️ Already delegated to `CanPlaceItemAtQuery` at line 368, but method exists anyway
- **Should be**: Remove dead code or centralize

**Logic Leak #7: Fallback Rectangle Calculation** (Lines 663-674) - ***NEW***
- ❌ Same as Leak #2 - calculating occupied cells for items without shapes
- **Should be**: Core handles all shape types (L-shapes AND rectangles)

**ROOT CAUSE**: Cache-Driven Architecture Anti-Pattern
- Lines 600-627 cache Core data in Presentation (`_itemShapes`, `_itemRotations`, `_itemDimensions`)
- Presentation then **recalculates business logic** using cached data
- **Should be**: Query Core for **results**, not cache Core's **state**

**Architectural Decision: What is "Presentation" vs "Logic"?**

**Presentation Layer** (Thin Display):
- ✅ Capture user input (mouse position → GridPosition)
- ✅ Send queries/commands to Core
- ✅ **Render** sprites at positions Core provides
- ✅ Handle Godot APIs (TextureRect, AtlasTexture, pixel coordinates)
- ❌ Calculate business logic (shape rotation, equipment overrides)
- ❌ Make business decisions (which cells to highlight)

**Core Layer** (Business Logic):
- ✅ Determine WHAT cells to highlight (based on item shape + rotation + container rules)
- ✅ Apply equipment slot overrides (business rule: 1×1 display)
- ✅ Rotate item shapes (spatial logic)
- ✅ Validate placement (collision, bounds, type compatibility)

**Current Violation** (Lines 1057-1075):
```csharp
// ❌ PRESENTATION doing BUSINESS LOGIC
var rotatedShape = baseShape.RotateClockwise(); // Core logic!
if (isEquipmentSlot)
    rotatedShape = ItemShape.CreateRectangle(1, 1).Value; // Business rule!
```

**Approved Solution: Option A** (Query-Based - Pure Separation):

**Core: New Query + Handler**
```csharp
// Application/Queries/Inventory/CalculateHighlightCellsQuery.cs
public record CalculateHighlightCellsQuery(
    ActorId ContainerId,
    ItemId ItemId,
    GridPosition Position,
    Rotation Rotation
) : IRequest<Result<List<GridPosition>>>;

// Handler calculates absolute cell positions using Core logic
public class CalculateHighlightCellsQueryHandler
{
    public async Task<Result<List<GridPosition>>> Handle(...)
    {
        var inventory = await _repo.GetByActorId(cmd.ContainerId);
        var item = await _items.GetById(cmd.ItemId);

        // Apply rotation (Core logic)
        var rotatedShape = ApplyRotation(item.Shape, cmd.Rotation);

        // Equipment slot override (Core business rule)
        if (inventory.ContainerType == ContainerType.WeaponOnly)
            rotatedShape = ItemShape.CreateRectangle(1, 1).Value;

        // Return absolute cell positions
        return rotatedShape.OccupiedCells
            .Select(offset => new GridPosition(
                cmd.Position.X + offset.X,
                cmd.Position.Y + offset.Y))
            .ToList();
    }
}
```

**Presentation: Dumb Rendering**
```csharp
// SpatialInventoryContainerNode.cs (simplified!)
private async void RenderDragHighlight(GridPosition origin, ItemId itemId, Rotation rotation, bool isValid)
{
    // Ask Core: "Which cells should I highlight?"
    var query = new CalculateHighlightCellsQuery(OwnerActorId.Value, itemId, origin, rotation);
    var result = await _mediator.Send(query);

    if (result.IsFailure) return;

    // ONLY presentation: Render sprites at positions Core calculated
    foreach (var cellPos in result.Value)
    {
        var highlight = CreateHighlightSprite(cellPos, isValid ? Colors.Green : Colors.Red);
        _highlightOverlayContainer.AddChild(highlight);
    }
}
```

**Why Option A Over Option B?**

1. **Performance is NOT a blocker**:
   - Query overhead: ~0.5ms per call
   - Mouse move events: ~30/sec during drag
   - Total cost: 15ms/sec out of 16.67ms/frame budget = **3% overhead** (imperceptible)
   - Measurement: Async query from in-memory repository is <1ms

2. **SSOT Principle**:
   - Equipment slot override in ONE place (Core)
   - Shape rotation in ONE place (Core)
   - Change "equipment slots show full shape" → update ONE file

3. **ADR-002 Compliance**:
   - Presentation becomes truly thin (just rendering)
   - Zero business logic in Presentation layer
   - Aligns with architectural vision

4. **Testability**:
   - Highlight calculation testable without Godot
   - Test: "Equipment slot returns 1×1 cells, not L-shape"
   - Test: "Rotated L-shape returns correct cells"

5. **Complements TD_003**:
   - TD_003 removes equipment slot special cases from Presentation
   - TD_004 completes the cleanup (removes highlight calculation too)
   - Result: Zero `if (isEquipmentSlot)` branches in Presentation

**Implementation Plan** (EXPANDED - Fixes ALL 7 Leaks):

**Phase 1: Create Core Queries (6-8h)**

Create THREE new queries to eliminate ALL shape calculation from Presentation:

1. **`CalculateHighlightCellsQuery`** (Leak #1 - Original scope)
   - Input: `(ContainerId, ItemId, Position, Rotation)`
   - Output: `List<GridPosition>` of cells to highlight
   - Logic: Rotate shape, apply equipment slot override, return absolute positions
   - Tests: Equipment slots → 1×1, L-shapes → correct cells, rotation → updated positions

2. **`GetItemRenderDataQuery`** (Leaks #2, #3, #4, #7 - **NEW**)
   - Input: `(ContainerId, ItemId)`
   - Output: `ItemRenderDto { OccupiedCells, RenderPosition, IsEquipmentSlot, Rotation }`
   - Logic: Calculate occupied cells (handles L-shapes AND rectangles), determine render position (centered for equipment slots, origin for grid), expose equipment slot flag
   - Tests: Equipment slots → centered position, Grid → origin position, L-shapes → only occupied cells (not bounding box)
   - **Replaces**: Lines 640-683 (occupied cells), 853-871 (centering), equipment slot detection scattered throughout

3. **`SwapOrMoveItemCommand`** (Leak #5 - **NEW**)
   - Input: `(SourceContainer, SourceItem, SourcePos, TargetContainer, TargetPos, Rotation)`
   - Output: `Result` (success/failure with rollback)
   - Logic: Determine swap vs move based on container type, implement with rollback
   - Tests: Equipment slot + occupied → swap, Grid + free → move, Failure → rollback
   - **Replaces**: Lines 476-491 (swap decision), 1122-1202 (swap implementation)

**Phase 2: Update Presentation - Remove ALL Business Logic (4-6h)**

1. **Delete Leak #1**: Replace `RenderDragHighlight` (lines 1057-1075) with `CalculateHighlightCellsQuery`
2. **Delete Leak #2**: Replace occupied cell calculation (lines 640-683) with `GetItemRenderDataQuery`
3. **Delete Leak #3**: Replace centering logic (lines 853-871) with `RenderPosition` from query
4. **Delete Leak #4**: Remove all `isEquipmentSlot` checks, use `IsEquipmentSlot` from query
5. **Delete Leak #5**: Replace swap decision + implementation (476-491, 1122-1202) with `SwapOrMoveItemCommand`
6. **Delete Leak #6**: Remove `CanAcceptItemType` method (dead code - already delegated to Core)
7. **Delete Leak #7**: Fallback rectangle logic removed by `GetItemRenderDataQuery` (handles all shapes)

**Result**: Presentation goes from **1372 lines → ~800 lines** (500+ lines of business logic deleted!)

**Phase 3: Documentation & Testing (2-3h)**
1. Update ADR-002: Add comprehensive "Presentation/Logic Boundary" section with all 7 leak examples
2. Document in dev-engineer.md: Cache-Driven Architecture anti-pattern
3. Regression test ALL inventory features (manual + 359 automated tests)

**Done When** (EXPANDED - All 7 Leaks Fixed):
- ✅ **THREE Core queries created**: `CalculateHighlightCellsQuery`, `GetItemRenderDataQuery`, `SwapOrMoveItemCommand`
- ✅ **15+ unit tests** covering all queries (rotation, equipment slots, L-shapes, swap/move, centering)
- ✅ **500+ lines deleted** from Presentation (business logic removed)
- ✅ **Grep verifications**:
  - `grep "isEquipmentSlot" Components/` → 0 results (equipment slot detection centralized)
  - `grep "RotateClockwise" Components/` → 0 results (no shape rotation in Presentation)
  - `grep "SwapItemsSafeAsync" Components/` → 0 results (swap logic moved to Core)
- ✅ **All 359 tests GREEN** (backward compatibility maintained)
- ✅ **Manual tests pass**:
  - Drag L-shape item → correct occupied cells highlighted
  - Equipment slot → items centered, 1×1 highlight
  - Swap in equipment slot → uses Core command (no rollback logic in Presentation)
  - Rotate during drag → highlights update correctly
- ✅ **ADR-002 updated**: Comprehensive "Presentation/Logic Boundary" section with all 7 leak examples + anti-pattern documentation
- ✅ **Presentation file size**: ~800 lines (down from 1372) - pure rendering only

**CRITICAL: Sequencing with TD_003**

**REVERSE RECOMMENDATION**: **DO TD_004 BEFORE TD_003** ✅

**Why sequencing changed after comprehensive analysis**:

**Original thinking** (your question): TD_004 first = cleaner separation
**New finding**: TD_004 **MUST** go first because:

1. **TD_004 removes equipment slot logic from Presentation** (Leaks #3, #4, #5)
   - Equipment slot centering (853-871)
   - Equipment slot detection (478, 855, 1069)
   - Equipment slot swap logic (1122-1202)

2. **TD_003 splits components**
   - If we split BEFORE TD_004 → equipment slot logic **duplicated** into BOTH components
   - If we do TD_004 FIRST → equipment slot logic **already in Core**, split is clean

3. **Concrete example**:
   ```
   WRONG ORDER (TD_003 → TD_004):
   1. Split file → EquipmentSlotNode + InventoryContainerNode
   2. BOTH components have centering logic (lines 853-871 duplicated)
   3. BOTH components have swap logic (lines 1122-1202 duplicated)
   4. TD_004 must update BOTH files (2× work, 2× risk)

   RIGHT ORDER (TD_004 → TD_003):
   1. TD_004: Move ALL equipment slot logic to Core
   2. File shrinks: 1372 → ~800 lines (clean!)
   3. Split ~800 line file → both components query Core (zero duplication!)
   4. TD_003 becomes simple file copy + delete operations
   ```

**Updated Sequence**:
```
Week 1: TD_004 (Remove business logic) → 1372 lines becomes ~800 lines
Week 2: TD_003 (Split components) → ~800 line file splits into 400+400 cleanly
Result: BOTH components are thin display layers with zero business logic
```

**Success Metrics** (EXPANDED):
- ✅ **Presentation layer**: 1372 lines → ~800 lines after TD_004 (before TD_003)
- ✅ **Zero business logic** in Presentation (verified by grep commands above)
- ✅ **All equipment slot rules** changeable in ONE place (Core)
- ✅ **TD_003 becomes simpler**: Split clean 800-line file instead of messy 1372-line file

**Tech Lead Decision** (2025-10-04): **APPROVED - Expanded Scope + Sequencing Change**

**Rationale**:
- **EXPANDED**: Found 7 logic leaks (not just 1!) - systemic architectural problem
- **SEQUENCING**: TD_004 MUST precede TD_003 to avoid duplicating equipment slot logic
- **SIZE**: M → L (5-7h → 12-16h) due to 3 queries instead of 1
- **PRIORITY**: Important → **Critical** due to 500+ lines of business logic in Presentation
- **ROI**: 500+ lines deleted = massive complexity reduction
- Performance cost still negligible (<1ms per query × 3 queries = <3ms total)

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