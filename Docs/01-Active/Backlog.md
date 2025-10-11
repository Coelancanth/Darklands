# Darklands Development Backlog


**Last Updated**: 2025-10-11 04:43 (Dev Engineer: TD_019 Phase 3 complete - All 543 Core tests GREEN; Presentation layer migration deferred)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 010
- **Next TD**: 020
- **Next VS**: 034


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

*Recently completed and archived (2025-10-11 18:01):*
- **VS_032**: Equipment Slots System (Phases 1-4/6 Complete) - Equipment system core foundation complete! Equipment as separate component architecture (not Inventory), 5 slots (MainHand, OffHand, Head, Torso, Legs), atomic operations with multi-level rollback, two-handed weapon validation, parent-driven data pattern (80% query reduction), 40 new tests GREEN (488 total). Phases 5-6 remaining: Data-Driven Equipment templates, Combat Integration. ✅ (2025-10-11 18:01) *See: [Completed_Backlog_2025-10_Part4.md](../07-Archive/Completed_Backlog_2025-10_Part4.md) for full archive*

---

## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

*Recently completed and archived (2025-10-11 05:26):*
- **TD_019**: Inventory-First Architecture (InventoryId Primary Key) - All 5 phases complete! Redesigned from Actor-centric (1:1) to Inventory-First (independent inventories with optional owner). InventoryId primary key, 16 commands/queries updated, 543 Core tests GREEN, 5 Presentation files updated with 3 runtime drag-drop bugs fixed, all obsolete methods removed. Unlocks squad-based gameplay, loot containers, shared inventories, cross-actor equip operations. ✅ (2025-10-11 05:21) *See: [Completed_Backlog_2025-10_Part3.md](../07-Archive/Completed_Backlog_2025-10_Part3.md) for full archive*

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*

### VS_033: MVP Item Editor (Weapons + Armor Focus)
**Status**: Proposed (Build AFTER manual item creation phase)
**Owner**: Product Owner → Tech Lead (breakdown) → Dev Engineer (implement)
**Size**: L (15-20h)
**Priority**: Ideas (deferred until designer pain validated)
**Markers**: [TOOLING] [DESIGNER-UX]

**What**: Minimal viable Godot EditorPlugin for creating weapon/armor ItemTemplates with auto-wired i18n and component-based UI.

**Why**:
- **Eliminate CSV pain** - Designer never manually edits CSV files (auto-generates translation keys, auto-syncs en.csv)
- **Component selection UI** - Check boxes for Equippable + Weapon/Armor (vs manual SubResource creation in Inspector)
- **Validation before save** - Catch errors (duplicate keys, missing components) BEFORE runtime
- **80% of content** - Weapons + armor are most items, validates tooling investment

**Strategic Phasing** (do NOT skip ahead):
1. **Phase 1: Validate Pain** (2-4h manual work) - REQUIRED FIRST
   - After VS_032 complete, designer creates 10-20 items manually in Godot Inspector
   - Designer documents pain points: "Inspector tedious", "CSV editing error-prone", "No validation until runtime"
   - **Trigger**: Designer reports "Inspector workflow is too tedious for 20+ items"

2. **Phase 2: Build MVP** (15-20h) - ONLY if Phase 1 pain validated
   - Build EditorPlugin with 5 core features (see "How" below)
   - Focus: Weapons + armor ONLY (defer consumables/tools to Phase 3)
   - Effort: 15-20h (vs 30h full Item Editor by deferring advanced features)

3. **Phase 3: Expand** (when >30 items exist) - Future
   - Add consumables, tools, containers support
   - Add balance tools (DPS calculator, usage tracking)
   - Add batch operations (create 10 variants)

**How** (MVP scope - 5 core features):
1. **Component Selection UI** (3-4h) - Checkboxes: ☑ Equippable, ☑ Weapon, ☑ Armor (auto-show properties)
2. **Quick Templates** (2-3h) - Presets: "Weapon" (Equippable+Weapon), "Armor" (Equippable+Armor), "Shield" (Equippable+Armor+Weapon)
3. **Auto-Wired i18n** (4-5h) - Designer types "Iron Sword" → auto-generates ITEM_IRON_SWORD → auto-writes en.csv (ZERO manual CSV editing!)
4. **Component Validation** (3-4h) - "Weapon requires Equippable", "Duplicate key ITEM_IRON_SWORD", offer auto-fix
5. **Live Preview** (2-3h) - Show sprite + stats + translation key preview

**Deferred for MVP** (add in Phase 3):
- ❌ Balance comparison (DPS calculator, power curves)
- ❌ Usage tracking (which ActorTemplates use this item)
- ❌ Batch operations (create N variants)
- ❌ Consumables/tools support (weapons + armor = 80% of content)

**Done When** (Phase 2 - MVP):
- ✅ Designer creates iron_sword.tres in 2 minutes (vs 5+ minutes Inspector)
- ✅ Component selection via checkboxes (no manual SubResource creation)
- ✅ Zero manual CSV editing (auto-generates ITEM_IRON_SWORD, writes to en.csv)
- ✅ Validation before save (catches duplicate keys, missing components)
- ✅ Works for weapons + armor (can create sword, plate armor, shield)
- ✅ Designer reports: "Item Editor is MUCH faster than Inspector"

**Depends On**:
- VS_032 ✅ (must be complete - validates equipment system works)
- Phase 1 manual item creation (10-20 items) - validates pain is REAL, not hypothetical

**Blocks**: Nothing (tooling is parallel track - doesn't block gameplay features)

**Product Owner Decision** (2025-10-10):
- **Do NOT build until Phase 1 pain validated** - Must create 10-20 items manually first to validate:
  1. Component-based ItemTemplate architecture works (can actors equip items?)
  2. Inspector workflow pain is REAL (not hypothetical)
  3. CSV editing pain is REAL (manual key management sucks)
- **Rationale**: Tools solve REAL pain, not imaginary pain. Must feel pain before building solution.
- **Risk mitigation**: If Phase 1 shows "Inspector is workable", defer Item Editor and create more items (avoid 15-20h investment for low ROI).
- **Scope discipline**: MVP focuses weapons + armor (80% of content). Defer consumables/tools until 30+ items exist (avoid premature generalization).

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