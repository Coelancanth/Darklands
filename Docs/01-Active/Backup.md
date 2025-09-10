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