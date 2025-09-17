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


## 💡 Future Ideas - Chain 4 Dependencies

*Features and systems to consider when foundational work is complete*

**DEPENDENCY CHAIN**: All future ideas are Chain 4 - blocked until prerequisites complete:
- ✅ Chain 1 (Architecture Foundation): TD_046 → MUST COMPLETE FIRST
- ⏳ Chain 2 (Movement/Vision): VS_014 → VS_012 → VS_013
- ⏳ Chain 3 (Technical Debt): TD_035
- 🚫 Chain 4 (Future Features): Cannot start until Chains 1-3 complete

### IDEA_001: Life-Review/Obituary System
**Status**: Future Consideration
**Owner**: Unassigned
**Size**: L (2-3 days)
**Priority**: Ideas
**Created**: 2025-09-12

**What**: Battle Brothers-style obituary and company history system
**Why**: Creates narrative and emotional attachment to characters
**How**: 
- Track all character events (battles, injuries, level-ups, deaths)
- Generate procedural obituaries for fallen characters
- Company timeline showing major events
- Statistics and achievements per character
**Technical Approach**: 
- Separate IGameHistorian system (not debug logging)
- SQLite or JSON for structured event storage
- Query system for generating reports
**Reference**: ADR-007 Future Considerations section

### IDEA_002: Economy Analytics System  
**Status**: Future Consideration
**Owner**: Unassigned
**Size**: M (1-2 days)
**Priority**: Ideas
**Created**: 2025-09-12

**What**: Track economic metrics for balance analysis
**Why**: Balance item prices, loot tables, and gold flow
**How**:
- Record all transactions (buy/sell/loot/reward)
- Aggregate metrics (avg gold per battle, popular items)
- Export reports for balance decisions
**Technical Approach**:
- Separate IEconomyTracker system (not debug logging)
- Aggregated analytics database
- Periodic report generation
**Reference**: ADR-007 Future Considerations section

### IDEA_003: Player Analytics Dashboard
**Status**: Future Consideration  
**Owner**: Unassigned
**Size**: L (3-4 days)
**Priority**: Ideas
**Created**: 2025-09-12

**What**: Comprehensive player behavior analytics
**Why**: Understand difficulty spikes, player preferences, death patterns
**How**:
- Heat maps of death locations
- Progression funnel analysis
- Play session patterns
- Difficulty curve validation
**Technical Approach**:
- Separate IPlayerAnalytics system (not debug logging)
- Event stream processing
- Visual dashboard for analysis
**Reference**: ADR-007 Future Considerations section


### TD_061: Camera Follow During Movement Animation
**Status**: Not Started
**Owner**: Unassigned
**Size**: S (1-2h estimate)
**Priority**: High - UX improvement
**Created**: 2025-09-17 20:35 (Dev Engineer)
**Markers**: [CAMERA] [MOVEMENT] [UX]

**What**: Make camera follow player smoothly during movement animation
**Why**: Currently camera only updates on click destination, not during movement

**Problem Statement**:
- Player moves cell-by-cell with smooth animation (TD_060 complete)
- Camera jumps to destination immediately on click
- Player can move off-screen during animation
- Poor UX when moving long distances

**Proposed Solution**:
1. GridView subscribes to actor position updates during animation
2. Camera smoothly interpolates to follow actor
3. Optional: Camera leads slightly ahead on path for better visibility
4. Ensure camera doesn't jitter with tween updates

**Technical Approach**:
- Hook into ActorView's tween updates
- Update camera position per frame or per cell
- Use smooth camera interpolation (lerp)
- Consider viewport boundaries