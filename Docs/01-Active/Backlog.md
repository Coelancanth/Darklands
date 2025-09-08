# Darklands Development Backlog


**Last Updated**: 2025-09-08 14:42

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 002
- **Next TD**: 017  
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

### TD_012: Remove Static Callbacks from ExecuteAttackCommandHandler [ARCHITECTURE] [Score: 90/100]
**Status**: Approved ✅
**Owner**: Dev Engineer
**Size**: S (2-3h)
**Priority**: Critical (Breaks testability and creates hidden dependencies)
**Markers**: [ARCHITECTURE] [ANTI-PATTERN] [TESTABILITY]
**Created**: 2025-09-08 14:42

**What**: Replace static mutable callbacks with proper event bus or MediatR notifications
**Why**: Static callbacks break testability, create hidden dependencies, and prevent parallel test execution

**Problem Statement**:
- ExecuteAttackCommandHandler uses `public static Action<>? OnActorDeath/OnActorDamaged`
- Static mutable state makes testing difficult
- Hidden coupling between handler and UI layer
- Cannot run tests in parallel due to shared state

**How**:
- Create domain events: `ActorDiedEvent`, `ActorDamagedEvent` as INotification
- Publish via MediatR: `await _mediator.Publish(new ActorDiedEvent(...))`
- Subscribe in presenters via INotificationHandler<T>
- Remove all static callback fields

**Done When**:
- Zero static mutable fields in ExecuteAttackCommandHandler
- Events published through MediatR pipeline
- Presenters receive events via handlers
- Tests can run in parallel without interference
- No regression in UI updates

**Depends On**: None

**Tech Lead Decision** (2025-09-08 14:45):
- **APPROVED WITH HIGH PRIORITY** - Critical architectural flaw affecting testability
- Static mutable callbacks violate fundamental OOP principles
- MediatR notifications are the correct pattern (already in our pipeline)
- Implementation: Create ActorDiedEvent/ActorDamagedEvent as INotification
- Route to Dev Engineer for immediate implementation

---

### TD_013: Extract Test Data from Production Presenters [SEPARATION] [Score: 85/100]
**Status**: Approved ✅
**Owner**: Dev Engineer
**Size**: S (3-4h)
**Priority**: Critical (Test code in production)
**Markers**: [SEPARATION-OF-CONCERNS] [TESTING]
**Created**: 2025-09-08 14:42

**What**: Remove hardcoded test actor creation from ActorPresenter
**Why**: Production code contains test/demo data, violating separation of concerns

**Problem Statement**:
- ActorPresenter.InitializeTestPlayer() creates hardcoded test actors
- Static TestPlayerId field exposes test state
- Presenter creating domain objects violates SRP
- Makes it impossible to start with different scenarios

**How**:
- Create IActorFactory service for actor creation
- Move test setup to separate TestScenarioService
- Inject scenario service only in development/test builds
- Use application commands to create actors properly

**Done When**:
- No test data in ActorPresenter
- Actor creation through proper application services
- Test scenarios injectable via configuration
- Clean separation between production and test code

**Depends On**: None

**Tech Lead Decision** (2025-09-08 14:45):
- **APPROVED WITH HIGH PRIORITY** - Test data in production is serious anti-pattern
- Violates Clean Architecture separation of concerns
- Solution: IActorFactory + TestScenarioService pattern is correct
- Enables flexible scenario testing and maintains boundaries
- Route to Dev Engineer (implement after TD_012)

---

## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*


### TD_015: Reduce Logging Verbosity and Remove Emojis [PRODUCTION] [Score: 60/100]
**Status**: Approved ✅
**Owner**: Dev Engineer
**Size**: S (2h)
**Priority**: Important (Production readiness)
**Markers**: [LOGGING] [PRODUCTION]
**Created**: 2025-09-08 14:42

**What**: Clean up excessive logging and remove emoji decorations
**Why**: Info-level logs too verbose, emojis inappropriate for production

**Problem Statement**:
- Info logs contain step-by-step execution details
- Emojis in production logs (💗 ✅ 💀 ⚔️)
- Makes log analysis and parsing difficult
- Log files grow too quickly

**How**:
- Move verbose logs from Information to Debug level
- Remove all emoji characters from log messages
- Keep Information logs for significant events only
- Add structured logging properties instead of string interpolation

**Done When**:
- No emojis in any log messages
- Information logs only for important events
- Debug logs contain detailed execution flow
- Log output reduced by >50% at Info level

**Depends On**: None

**Tech Lead Decision** (2025-09-08 14:45):
- **APPROVED** - Clean logging essential for production
- Emojis inappropriate for professional logs
- Simple log level adjustments, no architectural changes
- Low-risk, high-value cleanup work
- Route to Dev Engineer (can be done anytime)

---

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

### TD_016: Split Large Service Interfaces (ISP) [ARCHITECTURE] [Score: 50/100]
**Status**: Deferred 🟪
**Owner**: Tech Lead (for future review)
**Size**: M (4-6h)
**Priority**: Backup (Not urgent)
**Markers**: [ARCHITECTURE] [SOLID]
**Created**: 2025-09-08 14:42

**What**: Split IGridStateService and IActorStateService into query/command interfaces
**Why**: Large interfaces violate Interface Segregation Principle

**How**:
- Split IGridStateService → IGridQueryService + IGridCommandService
- Split IActorStateService → IActorQueryService + IActorCommandService
- Update all consumers to use appropriate interface
- Maintain backward compatibility with composite interface

**Done When**:
- Separate query and command interfaces
- Each interface has single responsibility
- No breaking changes to existing code
- Clear separation of read/write operations

**Depends On**: None

**Tech Lead Decision** (2025-09-08 14:45):
- **DEFERRED TO BACKUP** - Valid but not urgent
- Score understated (actually 70/100 due to breaking change risk)
- Not blocking current work, risk outweighs benefit now
- When implemented: use composite interfaces for backward compatibility
- Revisit after critical items complete

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