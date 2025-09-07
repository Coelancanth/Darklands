# Darklands Development Backlog


**Last Updated**: 2025-09-07 16:13

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 001
- **Next TD**: 010  
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

### VS_002: Combat Scheduler (Phase 2 - Application Layer)
**Status**: Ready for Dev
**Owner**: Dev Engineer ← ASSIGNED 2025-09-07 15:57 (Product Owner decision)
**Size**: S (<4h) - VALIDATED
**Priority**: Critical (Core combat system foundation)
**Markers**: [ARCHITECTURE] [PHASE-2]
**Created**: 2025-08-29 14:15
**Moved to Important**: 2025-09-07 15:57

**What**: Priority queue-based timeline scheduler for traditional roguelike turn order
**Why**: Core combat system foundation - all combat features depend on this

**How** (SOLID but Simple):
- CombatScheduler class with SortedSet<ISchedulable> (20 lines)
- ISchedulable interface with Guid Id AND NextTurn properties
- ScheduleActorCommand/Handler for MediatR integration
- ProcessTurnCommand/Handler for game loop
- TimeComparer using both time AND Id for deterministic ordering

**Done When**:
- Actors execute in correct time order (fastest first)
- Unique IDs ensure deterministic tie-breaking
- Time costs from Phase 1 determine next turn
- Commands process through MediatR pipeline
- 100+ actors perform without issues
- Comprehensive unit tests pass

**Acceptance by Phase**:
- Phase 2 (This): Commands schedule/process turns correctly
- Phase 3 (Next): State persists between sessions
- Phase 4 (Later): UI displays turn order

**Depends On**: ~~VS_001~~ (COMPLETE 2025-08-29) + ~~BR_001~~ (COMPLETE 2025-08-29) - Now unblocked

**Product Owner Notes** (2025-08-29):
- Keep it ruthlessly simple - target <100 lines of logic
- Use existing TimeUnit comparison operators for sorting
- No event systems or complex patterns
- Standard priority queue algorithm from any roguelike
- **CRITICAL**: Every actor MUST have unique Guid Id for deterministic tie-breaking

**Tech Lead Decision** (2025-08-29 16:30):
- **APPROVED** - Technical approach is sound (Complexity: 2/10)
- SortedSet is correct data structure for O(log n) operations
- Guid tie-breaking ensures determinism for testing/replay
- Follows established MediatR patterns

**Implementation Tasks**:
1. **Create Application folder structure** (5 min)
   - `src/Application/Combat/Commands/`
   - `src/Application/Combat/Queries/`
   
2. **Define core types** (30 min)
   - `ISchedulable` interface with `Guid Id` and `TimeUnit NextTurn`
   - `TimeComparer : IComparer<ISchedulable>` with tie-breaking
   - `CombatScheduler` class wrapping `SortedSet<ISchedulable>`
   
3. **Implement Commands** (45 min)
   - `ScheduleActorCommand : IRequest<Fin<Unit>>` with Id, NextTurn
   - `ProcessNextTurnCommand : IRequest<Fin<Option<Guid>>>`
   - `GetSchedulerQuery : IRequest<Fin<IReadOnlyList<ISchedulable>>>`
   
4. **Implement Handlers** (60 min)
   - `ScheduleActorCommandHandler` - adds to scheduler
   - `ProcessNextTurnCommandHandler` - pops next actor
   - `GetSchedulerQueryHandler` - returns ordered list
   
5. **Write comprehensive tests** (90 min)
   - Deterministic ordering tests with same TimeUnit
   - Performance test with 1000+ actors
   - Command/handler integration tests
   - Edge cases (empty scheduler, duplicate times)

**Pattern to Follow**: See established `AdvanceTurnCommand` pattern in `src/Features/Turn/Commands/`

### VS_010a: Actor Health System (Foundation)
**Status**: Ready for Dev ← SPLIT from VS_010 2025-09-07 16:13 (Tech Lead decision)
**Owner**: Dev Engineer
**Size**: S (1 day)
**Priority**: Critical (Required for all combat)
**Markers**: [ARCHITECTURE] [FOUNDATION]
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
- Phase 1: Health domain model with validation
- Phase 2: Damage/Heal commands process correctly
- Phase 3: Actor state persists in service
- Phase 4: Health bars display in scene

**Depends On**: None (foundational)

**Tech Lead Decision** (2025-09-07 16:13):
- **APPROVED** - Split from oversized VS_010 (was 4-5 days)
- Complexity: 3/10 - Standard domain modeling
- Risk: Low - Well-understood pattern
- Must complete before VS_010b and VS_010c

### VS_010b: Basic Melee Attack
**Status**: Ready for Dev ← SPLIT from VS_010 2025-09-07 16:13 (Tech Lead decision)
**Owner**: Dev Engineer (after VS_010a)
**Size**: M (1.5 days)
**Priority**: Critical (Core combat mechanic)
**Markers**: [ARCHITECTURE] [COMBAT]
**Created**: 2025-09-07 16:13

**What**: Execute melee attacks with scheduler integration and damage
**Why**: First actual combat mechanic using the time-unit system

**How**:
- AttackAction domain validation (adjacency, target alive)
- ExecuteAttackCommand with damage calculation
- Integration with DamageActorCommand from VS_010a
- Reschedule attacker with action time cost
- Remove dead actors from scheduler

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

### VS_010c: Dummy Combat Target
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
- Knight sprite with health bar
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