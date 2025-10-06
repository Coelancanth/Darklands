# Darklands Development Backlog


**Last Updated**: 2025-10-06 16:24 (Backlog Assistant: VS_021 archived to Completed_Backlog_2025-10_Part2.md)

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


### VS_020: Basic Combat System (Attacks & Damage)
**Status**: Ready for Testing (Phase 4 complete) | **Owner**: Dev Engineer | **Size**: M (1-2 days) | **Priority**: Important
**Markers**: [PHASE-1-CRITICAL] [BLOCKING]

**What**: Attack commands (melee + ranged), damage application, range validation, manual dummy enemy combat testing

**Progress** (2025-10-06 19:15):
- ✅ **Phase 0 Complete** - Component Pattern Infrastructure (commit 7f299d7)
  - Created component system: IComponent, Actor (container), IHealthComponent, IWeaponComponent
  - Weapon value object: damage, time cost, range, type (Melee/Ranged)
  - IActorRepository + InMemoryActorRepository (two-system tracking)
  - ActorFactory.CreateFromTemplate() with conditional component assembly
  - ActorTemplate extended with weapon properties
  - ActorIdLoggingExtensions enhanced: "shortId [type: Type, name: ACTOR_KEY]"
  - DI registration in GameStrapper
  - All 415 tests GREEN ✅
  - Architecture: Component pattern scales to 50+ actor types (write once, reuse everywhere)
- ✅ **Phase 1 Complete** - Domain Layer (already done in Phase 0)
  - Weapon value object created with all required properties
- ✅ **Phase 2 Complete** - ExecuteAttackCommand (commit 7104295)
  - ExecuteAttackCommand(AttackerId, TargetId) with AttackResult
  - Range validation: Melee (adjacent, Chebyshev distance), Ranged (distance check)
  - Damage application via HealthComponent.TakeDamage()
  - TurnQueue integration: Reschedule attacker with weapon time cost
  - Death handling: Remove defeated actors from queue
  - 10 new tests covering happy path, validation, edge cases
  - All 425 tests GREEN ✅ (415 existing + 10 new)
- ✅ **Phase 3 Complete** - Line-of-Sight Validation (commit a811e41)
  - Integrated IFOVService + GridMap into ExecuteAttackCommandHandler
  - Ranged attacks now validate line-of-sight (FOV visibility check)
  - Walls/obstacles block ranged attacks (tactical positioning matters)
  - Melee attacks bypass FOV check (can attack around corners, in darkness)
  - 3 new tests: LOS blocked, LOS clear, melee independence
  - All 428 tests GREEN ✅ (425 existing + 3 new)
- ✅ **Phase 4 Complete** - Presentation Layer (commit pending)
  - Click-to-attack: Click enemy to attack (replaces click-to-move when targeting enemy)
  - Test scene updated: TurnQueueTestScene.tscn now combat-ready
  - Actors created with health + weapons: Player (100HP/melee), Goblin (30HP/melee), Orc (50HP/ranged)
  - Visual feedback: Damage/death logged to console with emojis (⚔️💥☠️)
  - Death removes enemy from grid (visual disappearance)
  - No button UI needed - intuitive click-to-attack

**Testing Instructions** (godot_project/test_scenes/TurnQueueTestScene.tscn):
1. **Open Scene**: Load TurnQueueTestScene.tscn in Godot Editor → Press F5 to run
2. **Movement**: Left-click floor tiles to move player (blue square) toward enemies
3. **FOV Reveals Enemies**:
   - Goblin (red) at (15,15) appears when you get close (FOV range ~10 tiles)
   - Orc (orange) at (20,20) appears at longer range
4. **Melee Combat** (Player vs Goblin):
   - Move adjacent to Goblin (distance = 1 tile, 8-directional)
   - Left-click Goblin to attack (melee sword, 20 damage)
   - Check console for: `⚔️ Attacking enemy` → `💥 Attack hit! Dealt 20 damage. Target HP: 10`
   - Click Goblin again (2nd hit kills: 10 HP remaining → 0 HP)
   - Check console for: `☠️ Enemy defeated!`
   - Goblin disappears from grid (removed from ActorPositionService)
5. **Ranged Combat** (Orc):
   - Orc has ranged weapon (bow, range 8 tiles)
   - Stand 5-8 tiles away from Orc (within range, line-of-sight clear)
   - Orc should be visible in FOV
   - Try attacking (currently player has melee, will fail with "out of range")
6. **Range Validation**:
   - Try attacking Goblin from 2+ tiles away → Error: "Target out of melee range"
   - Stand behind wall, try attacking through wall → Error: "Target not visible (line-of-sight blocked)"
7. **Expected Results**:
   - ✅ Player can attack adjacent enemies (melee works)
   - ✅ Damage reduces enemy HP (logged to console)
   - ✅ 2 hits kill Goblin (30 HP / 20 damage = 2 attacks)
   - ✅ Dead enemies disappear from grid
   - ✅ Attacks blocked by range/LOS show error messages

**Why**:
- **BLOCKS Phase 1 validation** - cannot prove "time-unit combat is fun" without attacks
- Completes core combat loop: Movement → FOV → Turn Queue → **Attacks** → Health/Death
- Foundation for Enemy AI (VS_011)

**How**:
- **Phase 0 (Foundation - Component Pattern + ADR-006 Integration)** (2-3 hours):
  - **Step 1: Component Infrastructure** (30 min)
    - Create `IComponent` base interface (Domain/Components/)
    - Create `Actor` entity as component container (Dictionary<Type, IComponent>)
    - Implement `AddComponent<T>()`, `GetComponent<T>()`, `HasComponent<T>()` methods
  - **Step 2: Health Component** (30 min)
    - Create `IHealthComponent` interface (TakeDamage, Heal, CurrentHealth, IsAlive)
    - Create `HealthComponent` implementation (wraps Health value object)
    - Unit tests: component isolation, damage reduction, death detection
  - **Step 3: Weapon Component** (30 min)
    - Create `IWeaponComponent` interface (Weapon property, CanAttack validation)
    - Create `WeaponComponent` implementation (wraps Weapon value object)
    - Unit tests: weapon validation, range checks
  - **Step 4: Repository + Factory** (45 min)
    - Create `IActorRepository` interface (GetActor, AddActor, RemoveActor)
    - Create `InMemoryActorRepository` implementation
    - Create `ActorFactory.CreateFromTemplate()` - conditionally adds components based on template
    - Register in DI container (GameStrapper)
  - **Step 5: Logging Integration** (15 min)
    - Update `ActorIdLoggingExtensions.ToLogString()` to use `IActorRepository.GetActor().NameKey`
    - Inject `IActorRepository` into 6 handlers (MoveActor, GetVisibleActors, etc.)
    - Result: Logs show `"8c2de643 [type: Enemy, name: ACTOR_GOBLIN]"`
  - **Deliverable**: Reusable component system - write HealthComponent ONCE, use for player/enemies/bosses/NPCs
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
- Logging Enhancement - ✅ complete (2025-10-06 15:38) - foundation for actor name display

**Tech Lead Decision - Phase 0 Architecture** (2025-10-06 16:17):

**Component Pattern (Chosen) vs Simple Entity**:
- **Decision**: Use component pattern (Actor as component container) instead of simple entity (Actor with properties)
- **Why Components**:
  - ✅ **Massive reusability** - Write `HealthComponent` ONCE, use for player/enemies/bosses/NPCs/merchants (5+ actor types)
  - ✅ **Scales to 50+ actor types** - Roguelikes need many enemies/NPCs, components prevent code duplication
  - ✅ **Flexible composition** - Player has Equipment, enemies don't; Boss has Phases, others don't (mix and match)
  - ✅ **Template integration** - Designer configures components in `.tres` files (HasHealth, HasEquipment flags)
  - ✅ **Matches ADR-002** - Architecture explicitly designed for component pattern (line 62-138)
  - ✅ **Easy to extend** - Add StatusEffectComponent later → ALL actors get buffs/debuffs automatically
- **Why NOT simple entity**: Leads to logic duplication when 5+ actor types need same features (copy-paste Health logic)
- **Trade-off**: +1 hour upfront (components vs properties), but saves 3+ hours after 5th actor type (reuse pays off)

**ADR-006 Compliance**:
- Templates (Infrastructure) → ActorFactory reads template → Creates Actor with components (Domain)
- Actor entity has NO reference to template (data copied during spawn)
- Components are pure C# (zero Godot dependency)

**Architecture Layers**:
- `IComponent` base interface → Domain/Components/
- `IHealthComponent`, `IWeaponComponent` → Domain/Components/
- `Actor` entity (component container) → Domain/Entities/
- `IActorRepository` interface → Application/Repositories/
- `InMemoryActorRepository` → Infrastructure/Repositories/
- `ActorFactory` (template → entity + components) → Application/Factories/

**Two-System Tracking**:
- `IActorRepository` (WHO: name, health, weapon components)
- `IActorPositionService` (WHERE: grid coordinates)
- Both use ActorId as linking key

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