# Darklands Development Backlog


**Last Updated**: 2025-10-04 20:15 (Product Owner: Refined VS_019 with research-first approach, scene duplication, Godot terrain system embrace, optional PCG)

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
- **TD_003**: Separate Equipment Slots from Spatial Inventory Container - Created EquipmentSlotNode (646 lines), extracted InventoryRenderHelper (256 lines), cleaned InventoryContainerNode. All 3 phases complete, 359 tests GREEN. ✅ (2025-10-04)
- **TD_004**: Move ALL Shape Logic to Core (SSOT) - Eliminated all 7 business logic leaks from Presentation (164 lines removed, 12% complexity reduction). Fixed SwapItemsCommand double-save bug, eliminated cache-driven anti-pattern. Commits: 4cd1dbe, 49c06e6. ✅ (2025-10-04)
- **TD_005**: Persona & Protocol Updates - Updated dev-engineer.md with Root Cause First Principle, UX Pattern Recognition, Requirement Clarification Protocol. ✅ (2025-10-04)
- **BR_007**: Equipment Slot Visual Issues - Fixed 1×1 highlight override and sprite centering for equipment slots. ✅ (2025-10-04)
- *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### VS_019: TileSet-Based Visual Scene (Research-Driven) 🎨
**Status**: Approved | **Owner**: Dev Engineer | **Size**: M (1.5-2 days) | **Priority**: Important
**Markers**: [VISUAL-POLISH] [MOTIVATION] [RESEARCH-FIRST]

**What**: Create new TileMap-based test scene using Godot native terrain system, duplicating (not modifying) GridTestScene.tscn as foundation

**Why**:
- Visual progress after infrastructure-heavy VS_007 (restore motivation)
- Professional appearance vs prototype ColorRect
- Learn Godot's native terrain/metadata features (like VS_009's TileSet metadata success)
- Foundation for future PCG work (optional stretch goal)
- **NOT blocking Phase 1 validation** (acknowledged polish work)

**How**:
- **Phase 0 (Research)**: Study Godot terrain system, TileSet metadata, TileMapLayer features, PCG integration points
  - Review VS_009 approach (TileSet custom data layers for items)
  - Research: Terrain sets, physics layers, navigation integration
  - Identify: What can be data-driven vs hardcoded?
- **Phase 1**: Duplicate GridTestScene.tscn → TileMapTestScene.tscn (preserve working prototype)
- **Phase 2**: Configure `colored_tilemap.png` with Godot terrain system (metadata-driven terrain types: wall, floor, smoke)
- **Phase 3**: Replace terrain rendering (ColorRect → TileMapLayer with terrain sets), coordinate mapping (Core Position → TileMap cells)
- **Phase 4**: Replace actor sprites (Sprite2D using tileset atlas regions)
- **Phase 5 (Optional)**: Basic PCG if research reveals easy integration (timebox to 4 hours max)

**Scope**:
- ✅ Research Godot native features (terrain system, metadata, TileMapLayer)
- ✅ Duplicate GridTestScene.tscn (preserve original)
- ✅ TileSet with terrain sets (not just atlas coords)
- ✅ **Autotiling via terrain sets** (Godot handles edge/corner matching automatically)
- ✅ Metadata-driven terrain properties (like VS_009 items)
- ✅ TileMapLayer rendering with terrain awareness
- ✅ Sprite2D actors using tileset texture regions
- ✅ Coordinate mapping (Core Position ↔ TileMap cells)
- ⚠️ **OPTIONAL**: Basic PCG (cellular automata or simple BSP) if time permits
- ❌ Animations (static sprites only)
- ❌ Advanced PCG (multi-room dungeons, BSP trees—defer to future VS)
- ❌ Navigation mesh integration (defer to movement/pathfinding work)

**Done When**:
- New TileMapTestScene.tscn exists (GridTestScene.tscn unchanged)
- Scene uses Godot terrain system (not manual tile placement)
- **Autotiling works**: Walls connect seamlessly (edge/corner matching automatic)
- Terrain metadata defines properties (walkable, blocks vision, etc.)
- Player/enemies are recognizable pixel art sprites
- FOV overlay still works visually
- All 359 tests still GREEN (zero Core changes)
- Scene looks "game-like" instead of prototype
- **OPTIONAL**: If PCG implemented, can generate simple random map on scene load

**Dependencies**: None (Presentation-only changes)
**Risks**:
- Research reveals terrain system is complex (fallback: simple atlas coords like original plan)
- PCG scope creep (strict 4-hour timebox, cut if not making progress)

**Research Questions** (answer before implementation):
1. How does Godot terrain system map to our TerrainType enum (Wall, Floor, Smoke)?
2. Can terrain metadata drive Core logic (like TileSet item metadata in VS_009)?
3. How to configure autotiling terrain sets (bit masks for edges/corners)?
4. What's the simplest PCG algorithm that integrates with TileMapLayer?
5. Do we need custom data layers, or is terrain set system sufficient?

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