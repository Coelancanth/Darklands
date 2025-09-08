# Darklands Development Backlog


**Last Updated**: 2025-09-08 22:59 (Added TD_023 for ADR review alignment by Tech Lead)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 002
- **Next TD**: 024  
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

#### 🚨 CRITICAL: VS Items Must Include Architectural Compliance Check
```markdown
**Architectural Constraints** (MANDATORY for VS items):
□ Deterministic: Uses IDeterministicRandom for any randomness (ADR-004)
□ Save-Ready: Entities use records and ID references (ADR-005)  
□ Time-Independent: No wall-clock time, uses turns/actions (ADR-004)
□ Integer Math: Percentages use integers not floats (ADR-004)
□ Testable: Can be tested without Godot runtime (ADR-006)
```

## 🔥 Critical (Do First)
*Blockers preventing other work, production bugs, dependencies for other features*

### TD_020: Implement Deterministic Random Service [ARCHITECTURE] [Score: 90/100]
**Status**: Approved ✅
**Owner**: Dev Engineer
**Size**: M (4-6h)
**Priority**: Critical (Foundation for saves/multiplayer/debugging)
**Markers**: [ARCHITECTURE] [ADR-004] [DETERMINISTIC] [FOUNDATION]
**Created**: 2025-09-08 21:31
**Approved**: 2025-09-08 21:31

**What**: Implement IDeterministicRandom service per ADR-004
**Why**: ALL future features depend on deterministic simulation for saves, debugging, and potential multiplayer

**Problem Statement**:
- Current code uses System.Random and Godot random (non-deterministic)
- Impossible to reproduce bugs from saves
- Multiplayer would desync immediately
- Can't implement reliable save/load without this

**Implementation Tasks**:
1. Create IDeterministicRandom interface with context tracking
2. Implement DeterministicRandom using PCG algorithm
3. Add to GameStrapper.cs DI container
4. Create Fork() method for independent streams
5. Add debug logging for random calls with context

**Done When**:
- IDeterministicRandom service fully implemented
- Registered in GameStrapper.cs
- Unit tests verify deterministic sequences
- Same seed produces identical results
- Fork() creates independent streams
- Context tracking for debugging desyncs

**Depends On**: None (Foundation)

**Tech Lead Decision** (2025-09-08 21:31):
- **AUTO-APPROVED** - Critical foundation per ADR-004
- Without this, saves and debugging are impossible
- Must be implemented before ANY new gameplay features
- Dev Engineer should prioritize immediately

---

### TD_021: Implement Save-Ready Entity Patterns [ARCHITECTURE] [Score: 85/100]
**Status**: Approved ✅
**Owner**: Dev Engineer  
**Size**: M (6-8h)
**Priority**: Critical (Every entity going forward needs this)
**Markers**: [ARCHITECTURE] [ADR-005] [SAVE-SYSTEM] [FOUNDATION]
**Created**: 2025-09-08 21:31
**Approved**: 2025-09-08 21:31

**What**: Refactor ALL domain entities to be save-ready per ADR-005
**Why**: Retrofitting save system later means rewriting entire domain layer

**Problem Statement**:
- Current entities may have circular references
- Some entities might reference Godot types
- No clear separation of persistent vs transient state
- IDs not consistently used for references

**Refactoring Tasks**:
1. Convert all domain entities to records
2. Replace object references with ID references
3. Remove any Godot types from domain layer
4. Implement IPersistentEntity marker interface
5. Separate transient state (animations, cache) from persistent

**Entities to Refactor**:
- Actor (use ActorId references)
- GridState (ensure no circular refs)
- Any status effects or buffs
- Combat state and turn order

**Done When**:
- All domain entities are records or immutable
- No circular object references
- All references use IDs
- No Godot types in domain
- Clear persistent vs transient separation
- Serialization test passes for all entities

**Depends On**: None (Can work in parallel with TD_020)

**Tech Lead Decision** (2025-09-08 21:31):
- **AUTO-APPROVED** - Critical per ADR-005
- Every day we delay makes this harder
- Do this NOW while codebase is small
- Run serialization tests on every entity

---

### TD_022: Implement Core Abstraction Services [ARCHITECTURE] [Score: 75/100]
**Status**: Approved ✅
**Owner**: Dev Engineer
**Size**: L (1-2 days)
**Priority**: Critical (Testing and modding depend on these)
**Markers**: [ARCHITECTURE] [ADR-006] [ABSTRACTION] [SERVICES]
**Created**: 2025-09-08 21:31
**Approved**: 2025-09-08 21:31

**What**: Implement abstraction services per ADR-006 Selective Abstraction
**Why**: These specific services need abstraction for testing, platform differences, and modding

**Services to Implement**:
1. **IAudioService** (Priority 1)
   - PlaySound(SoundId, position)
   - SetMusicTrack(MusicId)
   - SetBusVolume(bus, volume)

2. **IInputService** (Priority 1)
   - IsActionPressed(InputAction)
   - GetMousePosition()
   - Observable<InputEvent> stream

3. **ISettingsService** (Priority 2)
   - Get<T>(SettingKey<T>)
   - Set<T>(SettingKey<T>, value)
   - Save/Load settings

**Implementation Notes**:
- Start with interfaces and mock implementations
- Then create Godot bridge implementations
- Register all in GameStrapper.cs
- DO NOT abstract UI controls, particles, tweens (per ADR-006)

**Done When**:
- All three services have interfaces defined
- Godot implementations created
- Mock implementations for testing
- Registered in DI container
- Basic unit tests using mocks
- Presentation layer uses services

**Depends On**: None (Can start immediately)

**Tech Lead Decision** (2025-09-08 21:31):
- **AUTO-APPROVED** - Core abstractions per ADR-006
- These enable testing and future modding
- Start with Audio and Input (highest value)
- Settings can be slightly delayed if needed

---

### TD_023: Review and Align Implementation with Enhanced ADRs [ARCHITECTURE] [Score: 70/100]
**Status**: Approved ✅
**Owner**: Tech Lead
**Size**: M (4-6h)
**Priority**: Critical (Must align before implementation begins)
**Markers**: [ARCHITECTURE] [ADR-REVIEW] [STRATEGIC] [SCOPE-MANAGEMENT]
**Created**: 2025-09-08 22:59
**Approved**: 2025-09-08 22:59

**What**: Strategic review of ADR enhancements and alignment of existing TD items with new specifications
**Why**: ADRs 004, 005, 006, 011, 012 received substantial professional-grade enhancements that may change implementation scope and requirements

**Enhanced ADR Changes Requiring Review**:

**ADR-004 (Deterministic Simulation) Enhancements**:
- Unbiased range generation (rejection sampling)
- Stable FNV-1a hashing for cross-platform fork derivation
- Comprehensive input validation with edge case handling
- Enhanced diagnostics (Stream, RootSeed properties)
- Cross-platform CI testing requirements
- Architecture tests for non-determinism prevention
- Microsoft.Extensions.Logging alignment

**ADR-005 (Save-Ready Architecture) Enhancements**:
- IStableIdGenerator interface for deterministic-friendly ID creation
- Enhanced recursive type validation for save readiness
- Pluggable serialization provider (Newtonsoft.Json support)
- World Hydration/Rehydration process specification
- ModData extension points for mod-friendly entities
- ISaveStorage abstraction for filesystem independence
- Save migration pipeline with discrete steps
- Architecture tests for Godot type prevention

**ADR-006 (Selective Abstraction) Enhancements**:
- Core value types (CoreVector2) to prevent Godot leakage
- IGameClock abstraction added to decision matrix
- Architecture tests for dependency enforcement
- Enhanced testing examples with NetArchTest
- Expanded abstraction decision matrix

**ADR-011/012 (Bridge Patterns) Enhancements**:
- Improved service integration patterns
- Enhanced error handling approaches
- Better DI integration examples

**Strategic Questions for Review**:
1. **Scope Impact**: Do TD_020, TD_021, TD_022 need scope adjustments for enhanced requirements?
2. **Split Decision**: Should complex enhancements become separate TD items (e.g., architecture tests, cross-platform CI)?
3. **Priority Sequencing**: Which enhanced features are Phase 1 vs Phase 2 implementations?
4. **Implementation Complexity**: Are complexity scores (90/85/75) still accurate with enhancements?
5. **Resource Allocation**: Do we need additional specialist input (DevOps for CI, Test Specialist for architecture tests)?

**Done When**:
- All four enhanced ADRs reviewed for implementation impact
- TD_020, TD_021, TD_022 scope validated or adjusted
- Decision made on splitting complex enhancements into separate items
- Implementation priority and sequence confirmed
- Resource requirements validated (Dev Engineer vs multi-persona)
- Any new TD items created for deferred enhancements
- Updated complexity scores if needed

**Depends On**: Review of enhanced ADR-004, ADR-005, ADR-006, ADR-011, ADR-012

**Tech Lead Decision** (2025-09-08 22:59):
- **AUTO-APPROVED** - Critical strategic review before implementation
- Must complete before Dev Engineer starts TD_020/021/022
- Enhanced ADRs significantly more comprehensive than original versions
- Risk of implementation drift without alignment review
- 4-6 hours well-spent to ensure we build the right architecture

---



### TD_018: Integration Tests for C# Event Infrastructure [TESTING] [Score: 65/100]
**Status**: Approved ✅
**Owner**: Test Specialist
**Size**: M (4-6h)
**Priority**: Important (Prevent DI/MediatR integration failures)
**Markers**: [TESTING] [INTEGRATION] [MEDIATR] [EVENT-BUS]
**Created**: 2025-09-08 16:40
**Approved**: 2025-09-08 20:15

**What**: Integration tests for MediatR→UIEventBus pipeline WITHOUT Godot runtime
**Why**: TD_017 post-mortem revealed 5 cascade failures that pure C# integration tests could catch

**Problem Statement**:
- TD_017 incident had 5 failures, 3 were pure C# infrastructure issues
- Current unit tests with mocks don't catch DI lifecycle problems
- MediatR auto-discovery conflicts not covered by tests
- WeakReference cleanup behavior untested
- Thread safety of event bus never validated

**Integration Test Definition** (for this codebase):
> Tests that verify REAL interaction between C# components (MediatR, UIEventBus, UIEventForwarder, DI container) WITHOUT mocking these infrastructure pieces, but WITHOUT requiring Godot runtime.

**Scope** (C# Infrastructure Only):
1. **MediatR Pipeline Tests**
   - Real MediatR → UIEventForwarder → UIEventBus flow
   - Handler auto-discovery validation
   - No duplicate handler registration
   
2. **DI Container Tests**
   - Service lifetime verification (singleton vs transient)
   - Registration conflict detection
   - Container initialization order
   
3. **UIEventBus Infrastructure**
   - WeakReference cleanup with mock subscribers (not Godot nodes)
   - Concurrent event publishing thread safety
   - Multiple subscriber scenarios
   
4. **NOT Testing** (Requires GDUnit/Manual):
   - Actual Godot node lifecycle
   - CallDeferred thread marshalling  
   - UI presenter updates
   - Scene tree integration

**Done When**:
- Integration test suite covers C# event infrastructure
- Tests use real DI container and MediatR pipeline (no mocks)
- Concurrent publishing scenarios validated
- WeakReference memory management verified
- All tests run in CI without Godot dependency
- Would have caught 3/5 issues from TD_017 incident

**Depends On**: None (TD_017 already complete)

**Tech Lead Decision** (2025-09-08 20:15):
- **APPROVED WITH FOCUSED SCOPE** - Pure C# integration tests only
- 80% value for 20% complexity (no Godot runtime needed)
- Catches critical DI/MediatR issues that caused TD_017 incident
- Defer Godot UI testing to future GDUnit initiative
- Test Specialist should implement immediately after current work

---


### TD_013: Extract Test Data from Production Presenters [SEPARATION] [Score: 40/100]
**Status**: Approved ✅
**Owner**: Dev Engineer
**Size**: S (2-3h)
**Priority**: Critical (Test code in production)
**Markers**: [SEPARATION-OF-CONCERNS] [SIMPLIFICATION]
**Created**: 2025-09-08 14:42
**Revised**: 2025-09-08 20:35

**What**: Extract test actor creation to simple IActorFactory
**Why**: ActorPresenter contains 90+ lines of hardcoded test setup, violating SRP

**Problem Statement**:
- ActorPresenter.InitializeTestPlayer() creates hardcoded test actors
- Static TestPlayerId field exposes test state globally
- Presenter directly creating domain objects violates Clean Architecture
- 90+ lines of test initialization code in production presenter

**How** (SIMPLIFIED APPROACH):
- Create simple IActorFactory interface with CreatePlayer/CreateDummy methods
- Factory internally uses existing MediatR commands (follow SpawnDummyCommand pattern)
- Each scene handles its own initialization in _Ready()
- Remove ALL test code from ActorPresenter
- NO TestScenarioService needed (over-engineering)

**Implementation**:
```csharp
// Simple factory interface
public interface IActorFactory
{
    Task<Fin<ActorId>> CreatePlayerAsync(Position position, string name = "Player");
    Task<Fin<ActorId>> CreateDummyAsync(Position position, int health = 50);
}

// Scene decides what it needs
public override void _Ready() 
{
    await _actorFactory.CreatePlayerAsync(new Position(0, 0));
    await _actorFactory.CreateDummyAsync(new Position(5, 5));
}
```

**Done When**:
- No test initialization code in presenters
- IActorFactory handles all actor creation via commands
- Each scene initializes its own actors
- Static TestPlayerId completely removed
- Clean separation achieved with minimal complexity

**Depends On**: None

**Tech Lead Decision** (2025-09-08 20:35):
- **REVISED TO SIMPLER APPROACH** - TestScenarioService is over-engineering
- Simple IActorFactory + scene-driven init is sufficient
- Follows YAGNI principle - don't build what we don't need
- Reduces complexity from 85/100 to 40/100
- Same result, half the code, easier to maintain

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

## 📦 Archive (Completed Items)
*Recently completed work for reference and knowledge transfer*

### TD_017: Implement UI Event Bus Architecture [ARCHITECTURE] [Score: 65/100] ✅
**Status**: Done
**Owner**: Dev Engineer
**Size**: L (2-3 days)
**Priority**: Critical (Foundation for 200+ future events)
**Markers**: [ARCHITECTURE] [ADR-010] [EVENT-BUS] [MEDIATR]
**Created**: 2025-09-08 16:40
**Completed**: 2025-09-08 19:38

**What**: Implement UI Event Bus pattern to replace static event router
**Why**: Current static approach won't scale to 200+ events and violates SOLID

**✅ IMPLEMENTATION COMPLETED** (All 4 Phases + 5 Critical Issues Fixed):

**Phase 1-4: Core Architecture** ✅
- Created complete UI Event Bus architecture with IUIEventBus interface
- Implemented UIEventForwarder<T> for automatic MediatR event forwarding
- Built WeakReference-based subscription system preventing memory leaks
- Integrated EventAwareNode base class for Godot lifecycle management

**Critical Issues Resolved**:
1. **MediatR Auto-Discovery Conflict** - Removed old GameManagerEventRouter entirely
2. **Missing Base Class Calls** - Fixed base._Ready() and base._ExitTree() calls
3. **Race Condition** - Restructured initialization order (DI first, then EventBus)
4. **CallDeferred Misuse** - Simplified to direct invocation (already on main thread)
5. **Duplicate Registration** - Removed manual UIEventForwarder registration

**Final Architecture**:
```
Domain Event → MediatR → UIEventForwarder<T> → UIEventBus → GameManager → UI Update
```

**Results**:
- ✅ Health bars update correctly when actors take damage
- ✅ Dead actors removed from UI immediately
- ✅ No more static router errors
- ✅ All 232 tests passing with zero warnings
- ✅ Modern event-driven architecture fully operational

**Post-Mortem**: [TD_017 Implementation Issues](../../06-PostMortems/Inbox/2025-09-08-td017-ui-event-bus-implementation.md)

**References**: [ADR-010](../03-Reference/ADR/ADR-010-ui-event-bus-architecture.md)

### TD_019: Fix embody script squash merge handling with hard reset strategy ✅
**Status**: Done
**Owner**: DevOps Engineer  
**Size**: M (4-6h)
**Priority**: Important (Developer friction)
**Markers**: [DEVOPS] [AUTOMATION] [GIT]
**Created**: 2025-09-08 17:00
**Completed**: 2025-09-08 17:31

**What**: Fix embody.ps1 script's squash merge handling with simplified reset strategy
**Why**: Script fails when PRs are squash merged, causing sync failures and manual intervention

**✅ IMPLEMENTATION COMPLETED**:
1. **Hard Reset Strategy**: Modified Handle-MergedPR() in sync-core.psm1 to use `git reset --hard origin/main` instead of problematic `git pull origin main --ff-only`
2. **Enhanced Pre-push**: Added dotnet format verification/auto-fix to pre-push hook to prevent verify-local-fixes CI failures
3. **Safety Preserved**: Maintains existing stash/restore logic for uncommitted changes
4. **Zero Friction Achieved**: Eliminates manual `git reset --hard origin/main` interventions

**Impact Delivered**:
- ✅ Squash merge handling works without sync failures
- ✅ Persona switching flows smoothly after PR merges  
- ✅ Enhanced format verification prevents CI failures
- ✅ Saves ~5-10 minutes per PR merge per developer
- ✅ branch-status-check.ps1 remains functional for awareness

**DevOps Engineer Decision** (2025-09-08 17:31):
- **COMPLETED** with elegant hard reset solution
- Both Handle-MergedPR() fix and pre-push format verification deployed
- Zero-friction automation philosophy maintained
- All tests pass, ready for production use

---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*