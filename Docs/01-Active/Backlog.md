# Darklands Development Backlog


**Last Updated**: 2025-09-10 09:05 (TD_021 Phase 2 Complete: Test Migration & Application Compatibility)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 002
- **Next TD**: 031  
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

<!-- TD_020 completed with property tests and moved to archive (2025-09-09 18:50) -->

<!-- TD_021 completed Phase 3 and moved to archive (2025-09-10 09:35) -->


---

<!-- TD_022 completed and moved to archive (2025-09-10 10:33) -->

---

<!-- TD_023 completed and moved to archive (2025-09-09 17:50) -->

### TD_024: Architecture Tests for ADR Compliance [TESTING] [Score: 85/100]
**Status**: Proposed 📋
**Owner**: Test Specialist
**Size**: M (6-8h)
**Priority**: Critical (Foundation - prevents regression)
**Markers**: [TESTING] [ARCHITECTURE] [ADR-COMPLIANCE] [FOUNDATION]
**Created**: 2025-09-09 17:44

**What**: Implement architecture tests to enforce ADR compliance at compile/test time
**Why**: Prevent architectural drift and regression; enforce boundaries automatically

**Problem Statement**:
- No automated enforcement of architectural boundaries
- Developers could accidentally violate ADR decisions
- Manual code reviews miss subtle violations
- Regression risk increases as team grows

**Implementation Tasks**:
1. **NetArchTest setup** for assembly dependency rules
2. **Prohibit Godot types** in Core assemblies (ADR-006)
3. **Enforce deterministic patterns** - flag System.Random usage (ADR-004)
4. **Validate save-ready entities** - no events/delegates in domain (ADR-005)
5. **Check abstraction boundaries** - Core can't reference Presentation
6. **Stable sorting enforcement** - flag unstable OrderBy usage
7. **Fixed-point validation** - flag float usage in gameplay logic

**Done When**:
- Architecture test project created and integrated
- All ADR rules have corresponding tests
- Tests run in CI pipeline
- Violations fail the build
- Clear error messages guide developers

**Depends On**: Understanding of ADR-004, ADR-005, ADR-006

---

### TD_025: Cross-Platform Determinism CI Pipeline [DEVOPS] [Score: 75/100]
**Status**: Proposed 📋
**Owner**: DevOps Engineer
**Size**: M (4-6h)
**Priority**: Important (Phase 2 - after core implementation)
**Markers**: [DEVOPS] [CI-CD] [DETERMINISM] [CROSS-PLATFORM]
**Created**: 2025-09-09 17:44

**What**: CI pipeline to verify deterministic simulation across platforms
**Why**: Ensure saves/multiplayer work identically on Windows/Linux/macOS

**Problem Statement**:
- Determinism might break across different platforms
- No automated verification of cross-platform consistency
- Manual testing won't catch subtle platform differences
- Multiplayer/saves could fail silently

**Implementation Tasks**:
1. **GitHub Actions matrix** for Windows, Linux, macOS
2. **Seed-based determinism tests** - same seed must produce identical results
3. **Sequence verification** - 10,000+ random draws must match byte-for-byte
4. **Performance benchmarks** - track deterministic operations speed
5. **Save compatibility tests** - saves must load across platforms
6. **Automated regression detection** - flag any determinism breaks

**Done When**:
- CI runs on all three platforms
- Determinism tests pass consistently
- Performance tracked and reported
- Failures block PR merges
- Clear diagnostics for failures

**Depends On**: TD_020 (Deterministic Random implementation)

---

<!-- TD_026 completed with property tests and moved to archive (2025-09-09 18:50) -->

### TD_027: Advanced Save Infrastructure [ARCHITECTURE] [Score: 85/100]
**Status**: Proposed 📋
**Owner**: Dev Engineer
**Size**: L (1-2 days)
**Priority**: Important (Phase 2 - needed before save system)
**Markers**: [ARCHITECTURE] [SAVE-SYSTEM] [ADR-005] [INFRASTRUCTURE]
**Created**: 2025-09-09 17:44

**What**: Production-ready save system infrastructure per enhanced ADR-005
**Why**: Basic save patterns insufficient for production game

**Problem Statement**:
- No abstraction for ID generation strategy
- Missing filesystem abstraction for platform differences
- No pluggable serialization for advanced scenarios
- World hydration process undefined
- No save migration strategy

**Infrastructure Tasks**:
1. **IStableIdGenerator** interface and implementations
2. **ISaveStorage** abstraction for filesystem operations
3. **Pluggable ISerializationProvider** with Newtonsoft support
4. **World Hydration/Rehydration** process for Godot scene rebuild
5. **Save migration pipeline** with version detection
6. **ModData extension** points on all entities
7. **Recursive validation** for nested generic types

**Done When**:
- All infrastructure interfaces defined
- Reference implementations complete
- Save/load works with test data
- Migration pipeline tested
- Platform differences abstracted

**Depends On**: TD_021 (Save-Ready entities)

---

### TD_028: Core Value Types and Boundaries [ARCHITECTURE] [Score: 70/100]
**Status**: Proposed 📋
**Owner**: Dev Engineer
**Size**: S (2-4h)
**Priority**: Critical (Prevents Godot leakage)
**Markers**: [ARCHITECTURE] [BOUNDARIES] [ADR-006] [CORE-TYPES]
**Created**: 2025-09-09 17:44

**What**: Core value types to prevent framework leakage into domain
**Why**: Godot types in Core would break saves, testing, and architecture

**Problem Statement**:
- Godot Vector2/Vector3 could leak into Core
- No IGameClock abstraction for deterministic time
- Missing boundary enforcement utilities
- Conversion overhead not optimized

**Implementation Tasks**:
1. **CoreVector2/CoreVector3** value types in Domain
2. **Efficient conversion** utilities to/from Godot types
3. **IGameClock** abstraction for game time
4. **Boundary validation** helpers for type checking
5. **Performance tests** for conversion overhead
6. **Usage examples** in documentation

**Done When**:
- Core types defined and tested
- Conversion utilities optimized
- IGameClock implemented
- No Godot types in Core
- Performance acceptable (<1% overhead)

**Depends On**: Understanding of ADR-006 boundaries

---

### TD_029: Roslyn Analyzers for Forbidden Patterns [TOOLING] [Score: 60/100]
**Status**: Proposed 📋
**Owner**: Tech Lead
**Size**: M (6-8h)
**Priority**: Nice to Have (Phase 3 - quality of life)
**Markers**: [TOOLING] [ANALYZERS] [COMPILE-TIME] [ENFORCEMENT]
**Created**: 2025-09-09 17:44

**What**: Custom Roslyn analyzers to catch forbidden patterns at compile time
**Why**: Catch violations immediately, not in tests or code review

**Problem Statement**:
- Architecture tests only run during test phase
- Developers get late feedback on violations
- Some patterns hard to catch in tests
- Want immediate IDE feedback

**Analyzer Tasks**:
1. **System.Random detector** - flag any usage in gameplay code
2. **Godot type detector** - flag in Core/Application layers
3. **Float arithmetic detector** - flag in combat/gameplay logic
4. **Unstable sort detector** - flag OrderBy without ThenBy
5. **Event/delegate detector** - flag in domain entities
6. **IDE integration** - warnings/errors in Visual Studio/Rider

**Done When**:
- Analyzer project created
- All forbidden patterns detected
- IDE shows warnings immediately
- Build integration complete
- Documentation for developers

**Depends On**: TD_024 (Architecture tests define the rules)

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

<!-- TD_030 completed and moved to archive (2025-09-09 20:11) -->

---

<!-- TD_015 completed and moved to archive (2025-09-09 19:56) -->

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



<!-- TD_017 and TD_019 moved to permanent archive (2025-09-09 17:53) -->

---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*