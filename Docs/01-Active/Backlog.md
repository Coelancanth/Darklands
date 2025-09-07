# Darklands Development Backlog


**Last Updated**: 2025-09-07 19:22

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 001
- **Next TD**: 011  
- **Next VS**: 011 


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




## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*


### VS_010a: Actor Health System (Foundation) [Score: 100/100]
**Status**: Done (Debugger Expert, 2025-09-07 19:22)
**Owner**: Debugger Expert
**Size**: S (1 day implementation + debugging)
**Priority**: Critical (Required for all combat)
**Markers**: [ARCHITECTURE] [FOUNDATION] [COMPLETE]
**Created**: 2025-09-07 16:13

**What**: Add health/damage foundation to Actor domain model
**Why**: Can't have attacks without health to damage - foundational requirement

**How**:
- Health value object with Current/Maximum/IsDead
- Actor domain model with Health property
- DamageActorCommand and HealActorCommand
- IActorStateService extends IGridStateService
- Health bar UI component in scene

**Done When**:
- Actors have persistent health values
- Damage/heal commands modify health correctly
- Death sets IsDead flag
- Health displays in UI with bar visualization
- All health scenarios covered by tests

**Acceptance by Phase**:
- ✅ Phase 1: Health domain model with validation (COMPLETE - commit 91b6273)
- ✅ Phase 2: Damage/Heal commands process correctly (COMPLETE - commit d9a8b1b)
- ✅ Phase 3: Actor state persists in service (COMPLETE - 2025-09-07 17:25)
- ✅ Phase 4: Health bars display in scene (COMPLETE - 2025-09-07 19:22)

**Phase 1 Deliverables** (2025-09-07 17:05):
- ✅ Health.cs - Immutable value object with validation
- ✅ Actor.cs - Domain model with integrated health
- ✅ DamageActorCommand & HealActorCommand created
- ✅ Comprehensive unit tests (50+ test cases)
- ✅ Zero build warnings, 232/233 tests passing
- ✅ Expected MediatR test failure (handlers needed Phase 2)

**Phase 2 Deliverables** (2025-09-07 17:11):
- ✅ IActorStateService interface for health management
- ✅ DamageActorCommandHandler with error handling
- ✅ HealActorCommandHandler with business rules
- ✅ 16 comprehensive handler test scenarios
- ✅ Zero build warnings, 239/249 tests passing
- ✅ Expected DI test failures (service implementation needed Phase 3)

**Phase 3 Deliverables** (2025-09-07 17:25):
- ✅ InMemoryActorStateService - Complete infrastructure implementation
- ✅ DI registration in GameStrapper - Singleton lifecycle management
- ✅ Thread-safe state management with ConcurrentDictionary
- ✅ Functional error handling with LanguageExt v5 patterns
- ✅ All 249 tests passing - Zero build warnings, complete DI resolution

**Phase 4 Completion Summary** (2025-09-07 19:22):
- ✅ **RESOLVED**: Health bar display issue via presenter coordination pattern
- ✅ Implemented SetHealthPresenter() coordination between ActorPresenter ↔ HealthPresenter
- ✅ Fixed Godot node initialization (moved from constructor to _Ready())
- ✅ Health bars now display with colors, numbers, and movement tracking
- ✅ Added visual polish: background borders, dynamic colors, health text display
- ✅ Created comprehensive post-mortem (PM_001) for architectural learning extraction

**Final Implementation**:
- Health bars display above actors with proper colors (Green/Yellow/Red)
- Shows "current/maximum" text (e.g., "100/100") 
- Tracks actor movement with synchronized positioning
- Proper Godot lifecycle usage with scene tree integration
- Cross-presenter coordination pattern established for future features

**Depends On**: None (foundational)

**Tech Lead Decision** (2025-09-07 16:13):
- **APPROVED** - Split from oversized VS_010 (was 4-5 days)
- Complexity: 3/10 - Standard domain modeling
- Risk: Low - Well-understood pattern
- Must complete before VS_010b and VS_010c

### VS_010b: Basic Melee Attack [Score: 85/100] ✅
**Status**: COMPLETE ← IMPLEMENTING 2025-09-07 20:25 (Dev Engineer)
**Owner**: Dev Engineer
**Size**: M (1.5 days) 
**Priority**: Critical (Core combat mechanic)
**Markers**: [ARCHITECTURE] [COMBAT]
**Created**: 2025-09-07 16:13

**What**: Execute melee attacks with scheduler integration and damage
**Why**: First actual combat mechanic using the time-unit system

**Implementation Progress**:
- ✅ **Phase 1**: Domain validation (AttackValidation with adjacency rules)
- ✅ **Phase 2**: Application handlers (ExecuteAttackCommandHandler with service coordination)  
- ✅ **Phase 3**: Infrastructure integration (full DI container + end-to-end testing)
- ✅ **Phase 4**: Presentation layer (UI feedback + animations)

**Phase 4 Completed** (2025-09-07 20:25):
- IAttackView interface for animations and visual effects
- AttackPresenter implementing IAttackFeedbackService with MVP pattern
- Combat logging through enhanced logger (⚔️ 💀 ❌ emoji indicators)
- Clean Architecture feedback system with optional presentation injection
- 281/281 tests passing, complete presentation layer integration

**FEATURE COMPLETE**: All "Done When" criteria satisfied ✅

**Done When**:
- Can attack adjacent enemies only
- Damage reduces target health
- Time cost affects attacker's next turn
- Death removes actor from scheduler
- Attack animations and feedback work
- Combat log shows attack messages

**Acceptance by Phase**:
- Phase 1: Attack validation logic (adjacency, alive)
- Phase 2: ExecuteAttackCommand processes correctly
- Phase 3: Coordinates scheduler and state updates
- Phase 4: Visual feedback and animations

**Depends On**: VS_010a (Health system), VS_002 (Scheduler)

**Tech Lead Decision** (2025-09-07 16:13):
- Complexity: 5/10 - Scheduler integration adds complexity
- Risk: Medium - Death cascade needs careful handling
- Pattern: Follow MoveActorCommand for command structure

### VS_010c: Dummy Combat Target [Score: 75/100]
**Status**: Ready for Dev ← SPLIT from VS_010 2025-09-07 16:13 (Tech Lead decision)
**Owner**: Dev Engineer (can parallel with VS_010b)
**Size**: XS (0.5 days)
**Priority**: Critical (Testing/visualization)
**Markers**: [TESTING] [SCENE]
**Created**: 2025-09-07 16:13

**What**: Static enemy target in grid scene for combat testing
**Why**: Need something visible to attack and test combat mechanics

**How**:
- DummyActor with health but no AI (NextTurn = Maximum)
- SpawnDummyCommand places at grid position
- Registers in scheduler (but never processes turn)
- brown sprite with health bar
- Death animation on zero health

**Done When**:
- Dummy appears at grid position (5,5) on scene start
- Has visible health bar above sprite
- Takes damage from player attacks
- Shows hit flash on damage
- Fades out when killed
- Respawns on scene reload

**Acceptance by Phase**:
- Phase 1: DummyActor domain model
- Phase 2: SpawnDummyCommand places in grid
- Phase 3: Registers in all services
- Phase 4: Sprite with health bar in scene

**Depends On**: VS_010a (Health system), VS_008 (Grid scene)

**Tech Lead Decision** (2025-09-07 16:13):
- Complexity: 2/10 - Minimal logic, mostly scene setup
- Risk: Low - Simple static entity
- Note: Becomes reusable prefab for future enemies



## 🗄️ Backup (Complex Features for Later)
*Advanced mechanics postponed until core loop is proven fun*

### TD_007: Presenter Wiring Verification Protocol [ARCHITECTURE] [Score: 70/100]
**Status**: Proposed ← MOVED TO BACKUP 2025-09-07 15:57 (Product Owner priority decision)
**Owner**: Tech Lead (Architecture decision needed)
**Size**: M (4-6h including tests and documentation)
**Priority**: Deferred (Focus on combat mechanics first)
**Markers**: [ARCHITECTURE] [TESTING] [POST-MORTEM-ACTION]
**Created**: 2025-09-07 13:36

**What**: Establish mandatory wiring verification protocol for all presenter connections
**Why**: TD_005 post-mortem revealed 2-hour debug caused by missing GridPresenter→ActorPresenter wiring

**Problem Statement** (from Post-Mortem lines 58-64):
- GridPresenter wasn't calling ActorPresenter.HandleActorMovedAsync()
- Silent failure - no compile error, no runtime error, just missing behavior
- Manual wiring in GameManager is error-prone and untested

**Proposed Solution**:
1. **Mandatory Wiring Tests**: Every presenter pair MUST have wiring verification tests
2. **Compile-Time Safety**: Consider IPresenterCoordinator interface for type-safe wiring
3. **Runtime Verification**: Add VerifyWiring() method called in GameManager._Ready()
4. **Test Pattern**: Create WiringAssert helper for consistent wiring test assertions

**Implementation Tasks**:
- [ ] Create presenter wiring test suite (PresenterCoordinationTests.cs)
- [ ] Add IPresenterCoordinator interface for type-safe wiring
- [ ] Implement VerifyWiring() runtime check in GameManager
- [ ] Document wiring test pattern in testing guidelines
- [ ] Add wiring tests to CI pipeline gate

**Done When**:
- All existing presenter pairs have wiring tests
- Runtime verification catches missing wiring on startup
- CI fails if wiring tests are missing for new presenters
- Documentation explains the wiring test pattern

**Depends On**: None (can start immediately)

**Tech Lead Analysis** (2025-09-07):
- **Complexity Score**: 4/10 (Well-understood problem with clear solution)
- **Pattern Match**: Similar to DI container validation pattern
- **Risk**: None - purely additive safety measures
- **ROI**: HIGH - Prevents hours of debugging for minutes of test writing
- **Decision**: APPROVED for immediate implementation

**Product Owner Decision** (2025-09-07 15:57):
- **DEFERRED TO BACKUP** - Combat mechanics take priority
- Important infrastructure but not blocking core game loop
- Revisit after VS_002 and VS_010a/b/c are complete

 **Decision**: APPROVED for immediate implementation

---

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
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*