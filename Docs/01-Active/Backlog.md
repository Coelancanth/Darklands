# Darklands Development Backlog


**Last Updated**: 2025-10-04 00:23 (Dev Engineer: Added TD_003-005 based on VS_018 lessons learned)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 006
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
- **BR_007**: Equipment Slot Visual Issues - Equipment slots showed L-shape highlights (3 cells) instead of 1×1. Fixed by overriding rotatedShape to ItemShape.CreateRectangle(1,1) for equipment slots. Also fixed sprite centering. ✅ (2025-10-04)
- **BR_006**: Cross-Container Rotation Highlights - Highlights didn't rotate during cross-container drag. Fixed with mouse warp hack (0.1px movement triggers _CanDropData). ✅ (2025-10-04)
- **BR_005**: Cross-Container L-Shape Highlight Inaccuracy - ItemDto evolved to Phase 4 (added ItemShape property). Pixel-perfect L-shape highlighting achieved. ✅ (2025-10-03)
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

**Status**: Proposed (needs Tech Lead approval)
**Owner**: Tech Lead → Dev Engineer (after approval)
**Size**: M (6-8h)
**Priority**: Important (Reduces complexity, fixes swap bugs)
**Depends On**: None (can start immediately)
**Markers**: [ARCHITECTURE] [SEPARATION-OF-CONCERNS]

**What**: Refactor `SpatialInventoryContainerNode` into two separate components: `InventoryContainerNode` (Tetris grid) and `EquipmentSlotNode` (single-item swap)

**Why**:
- **Bug Source**: Current unified approach causes equipment slot swap failures (BR_008 - items overlapping)
- **Complexity**: Equipment slot special cases scattered throughout 1372-line file (centering, 1×1 override, collision skip)
- **SSOT Violation**: Two different UX patterns (Tetris vs Diablo 2 swap) in one component
- **Maintainability**: Each component simpler, easier to test independently

**How**:
- **InventoryContainerNode** (Tetris grid, ~800 lines):
  - Multi-cell placement with L-shape collision
  - Rotation support via scroll wheel
  - Cross-container drag-drop
  - Highlight rendering (full shape)
- **EquipmentSlotNode** (Single-item swap, ~400 lines):
  - Always 1×1 logical placement
  - Swap-only UX (no collision check, always valid if type matches)
  - Centered sprite rendering
  - Single-cell highlight (always)
- **Shared**: Reuse drag preview creation, atlas texture loading

**Done When**:
- ✅ Equipment slot swap works (dagger ↔ ray_gun with no overlap)
- ✅ Each component <500 lines (separation achieved)
- ✅ All 359 tests still GREEN (backward compatibility)
- ✅ No equipment slot special cases in InventoryContainerNode (grep for "isEquipmentSlot" returns 0)

**Tech Lead Decision** (date): [Pending]

---

### TD_004: Move Highlight Logic to Core (SSOT)

**Status**: Proposed (needs Tech Lead discussion)
**Owner**: Tech Lead → Dev Engineer (after approval)
**Size**: M (4-6h)
**Priority**: Important (Architectural compliance)
**Depends On**: None
**Markers**: [ARCHITECTURE] [ADR-002]

**What**: Move highlight shape calculation from Presentation to Core, create `CalculateHighlightShapeQuery`

**Why**:
- **Business Logic Leak**: RenderDragHighlight (lines 1009-1104) duplicates shape rotation math from Core
- **SSOT Violation**: Equipment slot 1×1 override exists in BOTH Presentation AND Core
- **ADR-002**: Presentation should only render, not calculate (thin display layer)
- **Future Risk**: Highlight logic can diverge from placement logic

**How** (Discussion Needed):
- **Option A**: Query returns `List<GridPosition>` of cells to highlight
  - Core: `CalculateHighlightCellsQuery(itemId, position, rotation, containerType)` → cells
  - Presentation: Render TextureRect at each returned cell (dumb rendering)
  - Pro: Zero business logic in Presentation
  - Con: Extra round-trip for every mouse move
- **Option B**: Keep highlight rendering in Presentation (pragmatic)
  - Accept thin violation: Highlight is UI concern (visual feedback only)
  - Ensure: Use same rotation math as Core (shared RotationHelper)
  - Pro: No performance cost
  - Con: Duplication risk

**Done When** (if Option A approved):
- ✅ `CalculateHighlightCellsQuery` in Core returns cell positions
- ✅ Presentation removes rotation math (uses query result only)
- ✅ Equipment slot logic ONLY in Core (Presentation agnostic)
- ✅ All 359 tests GREEN

**Tech Lead Decision** (date): [Pending - requires architectural discussion]

---

### TD_005: Persona & Protocol Updates

**Status**: Proposed (needs Product Owner approval)
**Owner**: Product Owner
**Size**: S (2-3h)
**Priority**: Important (Process improvement)
**Depends On**: None
**Markers**: [PROCESS] [PERSONA]

**What**: Update persona system and protocols based on BR_006/007 lessons

**Why**:
- **Root Cause Gap**: Recent bugs (rotation highlights, equipment visuals) were workarounds/patches, not root cause fixes
- **UX Blind Spot**: No persona owns user experience validation (UI/UX designer missing)
- **Requirement Clarity**: Need protocol: "Repeat user requirement back" before implementation

**How**:
1. **Create UI/UX Designer Persona** (optional):
   - Responsibilities: Validate UX patterns, suggest simplifications, challenge complexity
   - When invoked: Before implementing UI features (VS_018 would have benefited)
   - Example: "Equipment slots + Tetris grid = different UX, should separate" (earlier detection)

2. **Update Dev Engineer Protocol**:
   - **Before coding**: Repeat requirement back to user in own words
   - **During investigation**: Always ask "What's the root cause?" (not just "What's the quick fix?")
   - **Pattern**: Workaround → Document as TD → Schedule proper fix

3. **Update Memory Bank** (`dev-engineer.md`):
   - Add: "Root Cause First" principle (BR_006 mouse warp = workaround, separation = fix)
   - Add: "UX Pattern Recognition" (detect when combining incompatible patterns)

**Done When**:
- ✅ UI/UX Designer persona created (if approved) OR protocol updated to include UX validation
- ✅ Dev Engineer protocol includes "repeat requirement" step
- ✅ Memory Bank updated with "Root Cause First" principle
- ✅ Test run: Next UI bug → persona asks "root cause?" before patch

**Product Owner Decision** (date): [Pending]

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