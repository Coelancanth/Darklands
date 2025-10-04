# Darklands Development Backlog


**Last Updated**: 2025-10-04 12:47 (Backlog Assistant: TD_004 archived - 7 leaks eliminated, 164 lines removed, architectural compliance achieved)

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
- **TD_004**: Move ALL Shape Logic to Core (SSOT) - Eliminated all 7 business logic leaks from Presentation (164 lines removed, 12% complexity reduction). Fixed SwapItemsCommand double-save bug, eliminated cache-driven anti-pattern. Commits: 4cd1dbe, 49c06e6. ✅ (2025-10-04)
- **TD_005**: Persona & Protocol Updates - Updated dev-engineer.md with Root Cause First Principle, UX Pattern Recognition, Requirement Clarification Protocol. ✅ (2025-10-04)
- **BR_007**: Equipment Slot Visual Issues - Fixed 1×1 highlight override and sprite centering for equipment slots. ✅ (2025-10-04)
- **BR_006**: Cross-Container Rotation Highlights - Fixed with mouse warp hack for cross-container drag updates. ✅ (2025-10-04)
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

### TD_003: Separate Equipment Slots from Spatial Inventory Container 🔄

**Status**: ✅ APPROVED (2025-10-04, **REFINED POST-TD_004**) - Ready for Dev Engineer
**Owner**: Tech Lead → Dev Engineer
**Size**: M (8-12h realistic estimate, **REDUCED** from 12-16h after TD_004)
**Priority**: Important (Component Reusability + Single Responsibility)
**Depends On**: None (can start immediately)
**Markers**: [ARCHITECTURE] [SEPARATION-OF-CONCERNS] [COMPONENT-DESIGN] [POST-TD_004-REFINED]

**What**: Refactor `SpatialInventoryContainerNode` into two separate components: `InventoryContainerNode` (Tetris grid) and `EquipmentSlotNode` (single-item swap)

**Why** (JUSTIFICATION UPDATED after TD_004 analysis):

**🔍 CRITICAL DISCOVERY**: TD_004 already moved 500+ lines of business logic to Core!
- Presentation now has ONLY 3 equipment-specific conditionals (lines 482, 870-893, 927)
- Business logic (1×1 override, swap, centering) already in Core queries/commands
- **Separation justification shifted from "remove complexity" to "reusability + focused components"**

**PRIMARY: Component Reusability** (Character Sheet Requirement)
- Character sheet needs 6 equipment slots (helmet, chest, weapon, shield, ring×2)
- Current: 6 instances × 1293 lines = **7758 lines loaded** for simple swap UX
- After: 6 instances × ~400 lines = **2400 lines** (saves **5358 lines** of dead code!)
- Equipment slots are core roguelike feature (NOT YAGNI - required for MVP)

**SECONDARY: Single Responsibility Principle**
- **Equipment Slot** (Diablo 2 pattern): Swap-only UX, type filter, centered display, NO rotation
- **Inventory Grid** (Tetris pattern): Multi-cell placement, rotation, L-shape collision, cross-container drag
- These are fundamentally different interaction patterns (like Button vs LineEdit)

**How** (REFINED Implementation - Avoids Duplication):

**Phase 1: Create EquipmentSlotNode from Scratch (3-4h)**
- Build NEW component (don't copy-delete from existing - cleaner approach!)
- Features:
  - Simplified drag-drop (no rotation handling, no cross-container mouse warp hack)
  - Swap detection → `SwapItemsCommand` (Core handles ALL business logic)
  - Centered sprite scaling → `GetItemRenderPositionQuery` (Core provides centering rule)
  - 1×1 highlight → `CalculateHighlightCellsQuery` (Core provides cells)
- Result: ~400 lines, swap-focused

**Phase 2: Extract InventoryRenderHelper (2-3h)** ⭐ NEW STEP
- Create static helper class: `Components/Inventory/InventoryRenderHelper.cs`
- Extract shared rendering methods (~200 lines):
  - `RenderItemSprite(item, position, cellSize, shouldCenter)` - Sprite scaling + rotation
  - `CreateDragPreview(item, rotation)` - AtlasTexture setup
  - `RenderHighlight(cells, isValid, cellSize)` - Green/red highlights
- **BOTH components call helper** → ZERO duplication!

**Phase 3: Clean InventoryContainerNode (1-2h)**
- Remove equipment slot conditionals (lines 482, 870-893, 927 - only 3!)
- Rename: `SpatialInventoryContainerNode.cs` → `InventoryContainerNode.cs`
- Update rendering to use `InventoryRenderHelper`
- Result: ~800 lines, Tetris-focused

**Phase 4: Documentation & Testing (2-3h)**
- Update test scene: weapon slot uses `EquipmentSlotNode`
- Regression: All 359 tests GREEN + manual drag-drop validation
- Documentation: Update component selection guide

**Tech Lead Decision** (2025-10-04, POST-TD_004): **APPROVED - Reusability Justification**

**Ultra-Refined Approval Rationale**:

1. **TD_004 Changed the Landscape** 🔍
   - Business logic moved to Core: `CalculateHighlightCellsQuery`, `SwapItemsCommand`, `GetItemRenderPositionQuery`
   - Presentation has ONLY 3 equipment-specific paths (down from 7+ before TD_004!)
   - **Separation no longer about complexity** - it's about **reusability + focused components**

2. **Reusability is CRITICAL** ✅
   - 6 equipment slots × 1293 lines = 7758 lines (current design - wasteful!)
   - 6 equipment slots × 400 lines = 2400 lines (after separation - efficient!)
   - **Saves 5358 lines of dead Tetris code** in character sheet
   - Equipment is core roguelike feature (not speculative future work)

3. **Single Responsibility Still Valid** ✅
   - Equipment slots don't need: rotation, multi-cell collision, cross-container drag hacks
   - Inventory grids don't need: swap logic, centering rules, 96px cells
   - Currently: BOTH paths in same 1293-line file (confusing for maintenance)

4. **Duplication Risk MITIGATED** 🛡️
   - `InventoryRenderHelper` shares ~200 lines of rendering logic
   - No base class needed (static helper simpler than inheritance)
   - Equipment and Inventory call same helper methods (DRY maintained)

5. **Estimate REDUCED** 💰
   - **Before TD_004**: 12-16h (included extracting business logic)
   - **After TD_004**: 8-12h (business logic already in Core!)
   - Benefit: Character sheet becomes trivial, testing simplified

**Implementation Checklist** (Dev Engineer):

**✅ Phase 1: Create EquipmentSlotNode (3-4h)** - ✅ **COMPLETED** 2025-10-04 13:43
- [x] Create `Components/Inventory/EquipmentSlotNode.cs` (646 lines, built from scratch)
- [x] Implement: Drag-drop (swap-focused), centered rendering, type validation
- [x] Call Core queries: `CanPlaceItemAtQuery`, `SwapItemsCommand`, `MoveItemBetweenContainersCommand`
- [x] NO rotation, NO multi-cell complexity (equipment-specific UX)
- [x] Update test scene: `SpatialInventoryTestController.cs` uses EquipmentSlotNode for weapon slot
- [x] Build verified: All projects compile (0 warnings, 0 errors)
- [x] **Bug fixes**:
  - Fixed swap validation (allow drops on occupied slots for weapon-to-weapon swap)
  - Fixed shape restoration (use item catalog shape, not 1×1 override from equipment slot storage)
- [x] E2E tested: ✅ All scenarios pass (move to empty, swap weapons, cross-container, shape preservation)
- [x] Commits:
  - `b40e1ac` - Initial EquipmentSlotNode creation
  - `5672923` - Fix swap detection timing
  - `[pending]` - Fix swap validation + shape restoration

**Dev Engineer Progress** (2025-10-04 13:43) - ✅ PHASE 1 COMPLETE:
- **Created**: EquipmentSlotNode (646 lines vs SpatialInventoryContainerNode 1293 lines - **50% reduction**)
- **Simplified**: No GridContainer, no rotation, no cross-container hacks
- **Swap Support**: Full weapon-to-weapon swap with shape preservation ✅
- **Bug fixes** (3 critical issues resolved):
  1. **Validation rejection**: `_CanDropData()` now allows occupied slot drops (checks if swap, validates type only)
  2. **Shape restoration**: `SwapItemsCommand` retrieves original L/T-shape from item catalog (not 1×1 storage override)
  3. **Timing issue**: Query inventory in `_DropData()` instead of relying on stale cached `_currentItemId`
- **E2E Verified**: Move, swap, cross-container, multi-cell shape preservation - ALL WORKING ✅
- **Next**: Phase 2 - Extract InventoryRenderHelper (DRY principle for shared rendering code)

**✅ Phase 2: Extract InventoryRenderHelper (2-3h)**
- [ ] Create `Components/Inventory/InventoryRenderHelper.cs` (static class, ~200 lines)
- [ ] Extract methods: `RenderItemSprite`, `CreateDragPreview`, `RenderHighlight`
- [ ] Update `EquipmentSlotNode` to use helper
- [ ] Commit: `refactor(inventory): Extract InventoryRenderHelper for DRY [TD_003 Phase 2/4]`

**✅ Phase 3: Clean InventoryContainerNode (1-2h)**
- [ ] Delete equipment conditionals (lines 482, 870-893, 927)
- [ ] Update rendering to use `InventoryRenderHelper`
- [ ] Rename: `SpatialInventoryContainerNode.cs` → `InventoryContainerNode.cs`
- [ ] Verify: `grep "isEquipmentSlot" Components/Inventory/InventoryContainerNode.cs` returns 0
- [ ] Commit: `refactor(inventory): Remove equipment logic from InventoryContainerNode [TD_003 Phase 3/4]`

**✅ Phase 4: Documentation & Testing (2-3h)**
- [ ] Update test scene: weapon slot uses `EquipmentSlotNode`
- [ ] Manual test: Swap dagger ↔ ray_gun, drag across containers
- [ ] Regression: `./scripts/core/build.ps1 test` (all 359 tests GREEN)
- [ ] Update component selection guide (when to use EquipmentSlotNode vs InventoryContainerNode)
- [ ] Commit: `docs(inventory): Update component selection guide [TD_003 Phase 4/4]`

**Done When**:
- ✅ `EquipmentSlotNode` exists (~400 lines, swap-focused)
- ✅ `InventoryRenderHelper` exists (~200 lines, shared rendering)
- ✅ `InventoryContainerNode` cleaned (~800 lines, Tetris-focused)
- ✅ Test scene uses `EquipmentSlotNode` for weapon slot
- ✅ All 359 tests GREEN + manual drag-drop validation passes
- ✅ `grep "isEquipmentSlot" InventoryContainerNode.cs` returns 0
- ✅ Documentation updated (component selection guide)

**Risk Mitigation**:
- ⚠️ Duplication: `InventoryRenderHelper` shares rendering logic (DRY maintained)
- ⚠️ Test scene breakage: Update incrementally (weapon slot first, verify, then backpacks)
- ⚠️ Regression: Extensive manual testing + 359 automated tests

**Success Metrics**:
- Character sheet becomes trivial (drop 6 `EquipmentSlotNode`s in scene editor)
- Memory footprint reduced (5358 lines saved for equipment-heavy UIs)
- Component responsibilities clear (swap vs Tetris placement)
- Testing simplified (equipment tests isolated from inventory tests)

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