# Darklands Development Backlog


**Last Updated**: 2025-10-06 15:15 (Dev Engineer: VS_021 complete - i18n + template infrastructure delivered, all 5 phases implemented, 415 tests GREEN, pre-push validation active, VS_020 unblocked)

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

**No critical items!** ✅ VS_021 completed, VS_020 unblocked.

---

*Recently completed and archived (2025-10-06):*
- **VS_021**: i18n + Data-Driven Entity Infrastructure (ADR-005 + ADR-006) - 5 phases complete! Translation system (18 keys in en.csv), ActorTemplate system with GodotTemplateService, player.tres template, pre-push validation script, architecture fix (templates → Presentation layer). All 415 tests GREEN. ✅ (2025-10-06 15:15)

**What**: Combined implementation of internationalization (i18n) and data-driven entity templates using Godot Resources

**Why**:
- **Prevents double refactoring** - Implementing both together is 40% more efficient than separate (no rework)
- **Natural integration** - Templates store translation keys (NameKey), i18n translates them (synergistic design)
- **Optimal timing** - Small codebase now (1-2 days), exponentially harder after VS_020 adds multiple entity types (3-4 days+)
- **Blocks VS_020** - Combat should use template-based entities from day one (clean architecture)

**How** (5 Phases):

**Phase 1: i18n Foundation** (ADR-005, 2-3 hours)
- Create `godot_project/translations/` directory structure
- Create `en.csv` with initial UI/entity keys (`UI_ATTACK`, `ACTOR_PLAYER`, etc.)
- Configure Godot Project Settings → Localization → Import Translation
- Refactor existing UI nodes to use `tr()` pattern (buttons, labels)
- Document i18n discipline in CLAUDE.md (all new UI must use keys)

**Phase 2: Template Infrastructure** (ADR-006, 3-4 hours)
- Create `Infrastructure/Templates/IIdentifiableResource.cs` interface (compile-time safety)
- Create `Infrastructure/Templates/ActorTemplate.cs` ([GlobalClass], implements IIdentifiableResource)
  - Properties: Id, NameKey, DescriptionKey, MaxHealth, Damage, MoveSpeed, Sprite, Tint
- Create `Infrastructure/Services/ITemplateService<T>` abstraction
- Create `Infrastructure/Services/GodotTemplateService<T>` (fail-fast loading, constraint: IIdentifiableResource)
- Register in DI container (GameStrapper._Ready())
- Create `res://data/entities/` directory in Godot project

**Phase 3: First Template + Integration** (1-2 hours)
- Create `player.tres` in Godot Editor (Inspector)
  - Id = "player"
  - NameKey = "ACTOR_PLAYER"
  - MaxHealth = 100, Damage = 10
  - Sprite = res://sprites/player.png
- Add `ACTOR_PLAYER,Player` to `translations/en.csv`
- Update entity spawning code to use `ITemplateService.GetTemplate("player")`
- Create Actor entity from template data (template.NameKey → actor.NameKey)
- Verify i18n works: `tr(actor.NameKey)` displays "Player"
- Test hot-reload: Edit player.tres → Ctrl+S → instant update (no recompile)

**Phase 4: Validation Scripts** (2-3 hours)
- Create `scripts/validate-templates.sh`:
  - Check all template NameKey values exist in en.csv
  - Check template IDs are unique (no duplicates)
  - Check stats are valid (MaxHealth > 0, Damage >= 0)
  - Exit 1 if any validation fails (fail-fast)
- Add to `.github/workflows/ci.yml` (run on PR, fail build on invalid templates)
- Add to `.husky/pre-commit` hook (fast local feedback)
- Test with broken template (missing NameKey) → ensure validation catches it

**Phase 5: Migration + Cleanup** (1-2 hours)
- Refactor existing entity creation to use templates (remove hardcoded factories)
- Update unit tests to mock `ITemplateService<T>` (no Godot dependency)
- Update integration tests to use real .tres files
- Remove old hardcoded entity factory code
- Verify all tests GREEN (dotnet test)

**Done When**:
- All UI text uses `tr("UI_*")` pattern (zero hardcoded strings in presentation)
- Entity names come from `.tres` templates (zero hardcoded entities in code)
- `en.csv` contains all keys (UI labels + entity names)
- Logs show translated names: `"Player attacks Goblin"` (not `"Actor_a3f attacks Actor_b7d"`)
- Designer can create new entity in < 5 minutes (create .tres in Inspector → test in-game)
- Hot-reload works (edit template → save → see changes without restart)
- Validation scripts run in CI (broken templates = failed build)
- CLAUDE.md documents both patterns (i18n discipline + template usage)
- All tests GREEN (unit tests mock ITemplateService, integration tests use real .tres)
- VS_020 (Combat) can use clean template-based entities from day one

**Dependencies**: None (can start immediately)
**Blocks**: VS_020 (Combat) - should use template-based entities, not hardcoded factories

**Tech Lead Decision** (2025-10-06):
- **Combining ADR-005 + ADR-006 is the correct architectural move**
- Prevents rework: Doing i18n without templates means refactoring entity names twice (once now, once when templates added)
- Timing is optimal: Codebase is small (5-10 entity references), cost curve is exponential
- After VS_020 adds combat entities (weapons, enemies), migration cost increases 3x
- Trade-off: +1 day now, saves -3 days later (net +2 days efficiency gain)
- Risk mitigation: Both ADRs are approved, proven patterns (Godot Resources + tr() are standard)

---

*Recently completed and archived (2025-10-05):*
- **VS_019**: TileSet-Based Visual Scene + TileSet as Terrain Catalog (SSOT) - All 4 phases complete! TileMapLayer pixel art rendering (terrain), Sprite2D actors with smooth tweening, fog overlay system, 300+ line cleanup. ✅ (2025-10-05)
- **VS_019_FOLLOWUP**: Fix Wall Autotiling (Manual Edge Assignment) - Manual tile assignment for symmetric bitmasks, walls render seamlessly. ✅ (2025-10-05)
- *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*


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

**Dependencies**:
- VS_007 (Turn Queue) - ✅ complete
- VS_021 (i18n + Templates) - ✅ complete (2025-10-06)

**Next Step**: After combat feels fun → VS_011 (Enemy AI uses these attack commands)

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