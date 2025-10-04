# Darklands Development Backlog


**Last Updated**: 2025-10-05 00:32 (Tech Lead: SIMPLIFIED VS_019 scope - removed PCG, focus on TileMapLayer visual upgrade + TileSet SSOT refactoring only, added tree terrain for interior obstacles, M (1-2 days) estimate)

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

*Recently completed and archived (2025-10-05):*
- **VS_019**: TileSet-Based Visual Scene + TileSet as Terrain Catalog (SSOT) - All 4 phases complete! TileMapLayer pixel art rendering (terrain), Sprite2D actors with smooth tweening, fog overlay system, 300+ line cleanup. ✅ (2025-10-05)
- **VS_019_FOLLOWUP**: Fix Wall Autotiling (Manual Edge Assignment) - Manual tile assignment for symmetric bitmasks, walls render seamlessly. ✅ (2025-10-05)
- *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### TD_006: Fix Godot Terrain Autotiling - Investigate Symmetric Bitmask Bug
**Status**: Proposed | **Owner**: Dev Engineer | **Size**: M (4-8h) | **Priority**: Nice-to-Have (Quality)
**Markers**: [TECHNICAL-DEBT] [TESTING] [ROOT-CAUSE-ANALYSIS] [GODOT-ENGINE]

**What**: Investigate and fix root cause of Godot terrain autotiling failure for symmetric wall bitmasks, replacing manual tile assignment workaround with proper data-driven autotiling

**Current State** (VS_019_FOLLOWUP workaround):
- Manual position-based tile assignment in `RenderWallsWithAutotiling()` (lines 180-242)
- Works correctly but hardcodes edge/corner logic (not data-driven)
- Problem: Godot's `SetCellsTerrainConnect()` picks wrong tile variants for left/right edges

**Symptom**:
- Left edge (X=0) and right edge (X=29) both rendered with atlas (3,1) "wall_middle_right"
- Expected: Left edge should use (0,1) "wall_middle_left", right edge uses (3,1)
- Visual result: Asymmetric appearance (seam/gap on left wall)

**Hypothesis** (needs testing):
- **Symmetric bitmask patterns** cause ambiguity in Godot's terrain matching algorithm
- Both edges have identical neighbor counts (3 walls, 1 floor) but different directional patterns
- Godot terrain system may require **asymmetric bitmask hints** to distinguish mirrored edges
- Alternative: TileSet configuration might be missing directional metadata

**Root Cause Investigation Plan**:

**Phase 1: Understand Godot Terrain System** (2h)
- [ ] Read Godot 4.x terrain autotiling documentation (official docs + community resources)
- [ ] Study peering bit semantics: Does `right_side` mean "connects to terrain on right" or "is edge facing right"?
- [ ] Test simple case: 2×2 grid with one terrain type, observe which tiles Godot picks
- [ ] Document expected behavior vs actual behavior with diagrams

**Phase 2: Isolate Minimal Reproduction** (1-2h)
- [ ] Create isolated test scene with ONLY walls (no floor/grass/trees)
- [ ] Test with single edge: Does left-only wall pick correct tile?
- [ ] Test with two edges: Do symmetric edges both fail?
- [ ] Compare to working example: Right edge renders correctly - why?
- [ ] Document exact conditions that trigger wrong tile selection

**Phase 3: Test TileSet Configuration Variations** (2h)
- [ ] **Test 1**: Remove peering bits from one edge tile → does autotiling fail gracefully?
- [ ] **Test 2**: Add directional custom data (e.g., `edge_direction: "left"`) → does Godot use it?
- [ ] **Test 3**: Create asymmetric bitmasks (left has 3 bits, right has 4) → does this fix selection?
- [ ] **Test 4**: Try `SetCellsTerrainPath()` instead of `SetCellsTerrainConnect()` → different algorithm?
- [ ] Document which configurations produce correct results

**Phase 4: Root Cause Identification** (1h)
- [ ] Analyze test results to pinpoint exact failure mode
- [ ] Possible root causes to validate:
  - **Godot bug**: Terrain matching algorithm has edge case for symmetric patterns
  - **Bitmask design**: Our configuration is valid but Godot needs additional hints
  - **API misuse**: We're calling SetCellsTerrainConnect incorrectly (wrong params/order)
  - **TileSet limitation**: Godot terrain system doesn't support symmetric edge tiles
- [ ] Write detailed root cause analysis with evidence from tests

**Phase 5: Implement Proper Fix** (2h)
- [ ] **If TileSet config fix**: Update test_terrain_tileset.tres with correct bitmask patterns
- [ ] **If API usage fix**: Refactor RenderWallsWithAutotiling() to use proper Godot API
- [ ] **If Godot limitation**: Document limitation, keep manual assignment (accept tech debt)
- [ ] **If Godot bug**: File upstream bug report with minimal reproduction, use workaround
- [ ] Remove manual tile assignment code if autotiling works
- [ ] Update documentation with findings and solution

**Success Criteria**:
- Root cause fully understood and documented with test evidence
- One of these outcomes:
  1. **Best**: Autotiling works correctly via TileSet/API fix, manual code deleted
  2. **Good**: Confirmed Godot limitation, manual workaround justified with explanation
  3. **Acceptable**: Godot bug filed upstream, workaround documented as temporary
- Knowledge captured for future terrain autotiling work

**Non-Goals**:
- Supporting complex interior wall autotiling (out of scope - border walls only)
- Performance optimization (manual assignment is already fast)
- Visual improvements beyond fixing symmetry

**Benefits**:
- **Data-driven**: Designers can add new terrain edges without C# code changes
- **Maintainable**: Autotiling handles edge cases automatically (T-junctions, corners, etc.)
- **Extensible**: Solution applies to future terrain types (ice walls, lava edges, etc.)
- **Educational**: Deep understanding of Godot terrain system benefits team

**Risks**:
- **MEDIUM**: May discover Godot terrain system has fundamental limitations
- **LOW**: Significant time investment for polish feature (not blocking core gameplay)
- **MITIGATION**: Timebox investigation to 8 hours, accept workaround if no solution found

**Dependencies**: None (VS_019 + VS_019_FOLLOWUP complete, walls functional)

**Defer Conditions** (when to skip):
- Phase 1 validation reveals this is critical path work (move to VS_020 Combat instead)
- Investigation exceeds 8 hour timebox without clear solution
- Godot terrain system fundamentally can't handle symmetric edges (accept tech debt)

---

### VS_020: Basic Combat System (Attacks & Damage)
**Status**: Approved | **Owner**: Tech Lead → Dev Engineer | **Size**: M (1-2 days) | **Priority**: Important
**Markers**: [PHASE-1-CRITICAL] [BLOCKING]

**What**: Attack commands (melee + ranged), damage application, range validation, manual dummy enemy combat testing

**Why**:
- **BLOCKS Phase 1 validation** - cannot prove "time-unit combat is fun" without attacks
- Completes core combat loop: Movement → FOV → Turn Queue → **Attacks** → Health/Death
- Foundation for Enemy AI (VS_011)

**How**:
- **Phase 1 (Domain)**: `Weapon` value object (damage, time cost, range, weapon type enum)
- **Phase 2 (Application)**: `ExecuteAttackCommand` (attacker, target, weapon), range validation (melee=adjacent, ranged=FOV line-of-sight), integrates with existing `TakeDamageCommand` from VS_001
- **Phase 3 (Infrastructure)**: Attack validation service (checks adjacency for melee, FOV visibility for ranged)
- **Phase 4 (Presentation)**: Attack button UI (enabled when valid target in range), manual dummy control (WASD for enemy, Arrow keys for player)

**Scope**:
- ✅ Melee attacks (adjacent tiles only, 8-directional)
- ✅ Ranged attacks (FOV line-of-sight validation, max range)
- ✅ Weapon time costs (integrate with TurnQueue from VS_007)
- ✅ Death handling (actor reaches 0 health → removed from queue)
- ❌ Enemy AI (dummy is manually controlled for testing)
- ❌ Multiple weapon types (just "sword" and "bow" for testing)
- ❌ Attack animations (instant damage for now)

**Done When**:
- Player can attack dummy enemy (melee when adjacent, ranged when visible)
- Dummy can attack player (manual WASD control)
- Health reduces on hit, actor dies at 0 HP
- Combat feels tactical (positioning matters for range/line-of-sight)
- Time costs advance turn queue correctly
- Can complete full combat: engage → attack → victory/defeat

**Dependencies**: VS_007 (Turn Queue - ✅ complete)
**Next Step**: After combat feels fun → VS_011 (Enemy AI uses these attack commands)

---

### VS_021: Internationalization (i18n) Infrastructure
**Status**: Approved | **Owner**: Tech Lead → Dev Engineer | **Size**: S-M (4-8 hours) | **Priority**: Important
**Markers**: [ARCHITECTURE] [TECHNICAL-DEBT-PREVENTION]

**What**: Godot i18n infrastructure with translation key discipline (architecture only, English translations only for now)

**Why**:
- Prevents catastrophic late-stage refactoring (10x cost if deferred)
- Aligns perfectly with Clean Architecture (Domain returns keys, Presentation calls `tr()`)
- Near-zero ongoing cost (just habit like using `Result<T>`)
- **Defers actual translation work** until Phase 1 validated (smart risk management)

**How**:
- **Phase 1**: Create `translations/` folder, configure Godot Project Settings → Localization, create `en.csv` with English keys
- **Phase 2**: Refactor existing UI text to use `tr("UI_*")` keys (buttons, labels in test scenes)
- **Phase 3**: Add `name_key` to Actor entity (e.g., `"ACTOR_PLAYER"`, `"ACTOR_GOBLIN"`), update logging to use `tr(actor.name_key)`
- **Phase 4**: Document pattern in CLAUDE.md (all new UI must use keys, Domain returns keys not strings)

**Scope**:
- ✅ Translation file structure (`translations/en.csv`)
- ✅ Godot localization configuration
- ✅ Refactor existing UI to use keys
- ✅ Actor display names use keys (fixes "random code" in logs)
- ✅ Architectural pattern documented
- ❌ Chinese/Japanese translations (deferred until Phase 1 validated)
- ❌ Pluralization support (`tr_n()` - add when needed)
- ❌ Cultural adaptation (future work)

**Done When**:
- All UI text uses `tr("UI_KEY")` pattern
- Logs show `"Player attacks Goblin"` instead of `"Actor_a3f attacks Actor_b7d"`
- `en.csv` contains all current keys
- CLAUDE.md documents i18n discipline for future work
- Zero hardcoded user-facing strings in codebase
- Adding Chinese later = just create `zh_CN.csv` (no code changes)

**Dependencies**: None (can be done parallel with VS_019/020)
**Integration**: Works with VS_020 (attack messages use keys)

---

*Recently completed and archived (2025-10-04 19:35):*
- **VS_007**: Time-Unit Turn Queue System - Complete 4-phase implementation with natural mode detection, 49 new tests GREEN, 6 follow-ups complete. ✅ (2025-10-04 17:38)

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