# Darklands Development Backlog


**Last Updated**: 2025-10-06 16:24 (Backlog Assistant: VS_021 archived to Completed_Backlog_2025-10_Part2.md)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 007 (TD_006 created 2025-10-06)
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

**No critical items!** ✅ VS_021 completed and archived, VS_020 unblocked.

---

*Recently completed and archived (2025-10-06):*
- **VS_021**: i18n + Data-Driven Entity Infrastructure (ADR-005 + ADR-006) - 5 phases complete! Translation system (18 keys in en.csv), ActorTemplate system with GodotTemplateService, player.tres template, pre-push validation script, architecture fix (templates → Presentation layer). Bonus: Actor type logging enhancement (IPlayerContext integration). All 415 tests GREEN. ✅ (2025-10-06 16:23) *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---

*Recently completed and archived (2025-10-05):*
- **VS_019**: TileSet-Based Visual Scene + TileSet as Terrain Catalog (SSOT) - All 4 phases complete! TileMapLayer pixel art rendering (terrain), Sprite2D actors with smooth tweening, fog overlay system, 300+ line cleanup. ✅ (2025-10-05)
- **VS_019_FOLLOWUP**: Fix Wall Autotiling (Manual Edge Assignment) - Manual tile assignment for symmetric bitmasks, walls render seamlessly. ✅ (2025-10-05)
- *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

**No important items!** ✅ VS_020 completed and archived.

---

*Recently completed and archived (2025-10-06):*
- **VS_020**: Basic Combat System (Attacks & Damage) - All 4 phases complete! Click-to-attack combat UI, component pattern (Actor + HealthComponent + WeaponComponent), ExecuteAttackCommand with range validation (melee adjacent, ranged line-of-sight), damage application, death handling bug fix. All 428 tests GREEN. Ready for VS_011 (Enemy AI). ✅ (2025-10-06 19:03) *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---

*Recently completed and archived (2025-10-04 19:35):*
- **VS_007**: Time-Unit Turn Queue System - Complete 4-phase implementation with natural mode detection, 49 new tests GREEN, 6 follow-ups complete. ✅ (2025-10-04 17:38)

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*

### TD_006: Refactor Test Scene Actor Tracking (Position Query Warnings)
**Status**: Proposed | **Owner**: Tech Lead | **Size**: S (<4h) | **Priority**: Ideas
**Markers**: [TECHNICAL-DEBT] [TEST-SCENE-ONLY]

**What**: Eliminate position query warnings for dead actors in test scene by dynamically tracking living actors instead of hardcoded ActorIds.

**Why**:
- Test scene currently queries hardcoded `_goblinId`, `_orcId` even after death
- GetActorPositionQueryHandler logs warnings (expected behavior, but noisy)
- Warnings are harmless but clutter logs during testing

**Problem Context** (VS_020 Death Handling Bug):
- Dead actors removed from `IActorPositionService` ✅
- Test scene's `UpdateActorVisibility()` still queries all 3 actors (player, goblin, orc)
- Dead goblin query fails → Warning logged: "Failed to get position for actor 3855cc70..."
- Happens 3x per FOV update (OnFOVCalculated, RestoreCellColor, UpdateActorVisibility)

**Current Behavior**:
```csharp
// TurnQueueTestSceneController.cs
private ActorId _goblinId;  // Hardcoded, never removed when actor dies
private ActorId _orcId;

private void OnFOVCalculated(...) {
    var goblinPosResult = await _mediator.Send(new GetActorPositionQuery(_goblinId));  // ⚠️ Fails if dead
    var orcPosResult = await _mediator.Send(new GetActorPositionQuery(_orcId));
    // ...
}
```

**Proposed Solution**:
```csharp
// Track living actors dynamically
private HashSet<ActorId> _enemyActors = new() { goblinId, orcId };

// Remove on death (subscribe to ActorDiedEvent or check position query success)
private void OnActorDied(ActorId actorId) {
    _enemyActors.Remove(actorId);
}

// Query only living actors
foreach (var enemyId in _enemyActors) {
    var posResult = await _mediator.Send(new GetActorPositionQuery(enemyId));
    // ...
}
```

**Alternative Solutions**:
1. **Downgrade log level**: Change `LogWarning` → `LogDebug` in GetActorPositionQueryHandler (loses production debugging value)
2. **Suppress in test scene**: Catch failures silently (current behavior already does this, just logs warning)
3. **Dynamic tracking** (recommended): Remove dead actor IDs from tracking set

**Trade-offs**:
- ✅ Cleaner logs during testing
- ✅ More realistic production actor tracking pattern
- ❌ Requires event subscription (ActorDiedEvent) or position query success tracking
- ❌ Only affects test scene (production will have proper actor lifecycle management)

**Scope**:
- ✅ Refactor TurnQueueTestSceneController to track living actors dynamically
- ✅ Subscribe to death events or check position query results
- ✅ Remove dead actor IDs from tracking set
- ❌ Change GetActorPositionQueryHandler logging (keep warnings for production debugging)

**Done When**:
- Test scene runs without position query warnings for dead actors
- Dead enemies still disappear from grid correctly
- Combat mode still exits when last enemy dies
- Code demonstrates production-ready actor lifecycle pattern

**Dependencies**: None (VS_020 complete, bug fixed, warnings are cosmetic)

**Tech Lead Decision**:
- **Priority**: Ideas (non-blocking, test scene only)
- **Defer until**: Pattern needed for production actor management (VS_011 Enemy AI?)
- **Alternative**: Accept warnings as "test scene limitation" (harmless, confirms death handling works)

---

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