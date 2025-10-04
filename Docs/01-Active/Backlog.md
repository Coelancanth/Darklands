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