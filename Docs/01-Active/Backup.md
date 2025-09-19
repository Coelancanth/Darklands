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



### TD_066: Architectural Boundary Enforcement Tests
**Status**: ✅ APPROVED
**Owner**: Dev Engineer
**Size**: S (2-3h)
**Priority**: Important - Prevents future violations
**Created**: 2025-09-19 03:33 (Tech Lead - from lessons learned)
**Markers**: [ARCHITECTURE] [TESTING] [QUALITY]

**What**: Add NetArchTest rules to enforce Clean Architecture boundaries
**Why**: TD_061's 12+ hour struggle was caused by presenters violating layer boundaries

**Technical Approach**:
```csharp
[Test]
public void Presenters_Should_Not_Be_Handlers() {
    Types.InNamespace("Presentation.Presenters")
        .Should().NotImplementInterface(typeof(INotificationHandler<>))
        .GetResult().IsSuccessful.Should().BeTrue();
}

[Test]
public void Handlers_Must_Be_In_Application_Layer() {
    Types.That().ImplementInterface(typeof(IRequestHandler<,>))
        .Should().ResideInNamespace("Application")
        .GetResult().IsSuccessful.Should().BeTrue();
}
```

**Done When**:
- [ ] Test enforcing presenters aren't handlers
- [ ] Test enforcing handlers in Application layer
- [ ] Test enforcing domain independence
- [ ] Tests run in CI pipeline

---

### TD_067: MediatR Registration Pattern Documentation
**Status**: ✅ APPROVED
**Owner**: Dev Engineer
**Size**: S (2h)
**Priority**: Important - Prevents future DI issues
**Created**: 2025-09-19 03:33 (Tech Lead - from post-mortem)
**Markers**: [DOCUMENTATION] [MEDIATR] [DI] [PATTERNS]

**What**: Document and enforce correct MediatR registration patterns
**Why**: TD_061 failed due to registration order issues and handler lifetime confusion

**Documentation to Create**:
1. **MediatR Best Practices** guide
2. **Registration order** rules
3. **Handler lifetime** guidelines
4. **Anti-patterns** to avoid

**Done When**:
- [ ] Helper method for standardized registration
- [ ] Documentation with examples
- [ ] Update existing registration code

---

### TD_068: DI Registration Order Standardization
**Status**: ✅ APPROVED
**Owner**: DevOps Engineer
**Size**: S (3h)
**Priority**: Important - Prevents registration conflicts
**Created**: 2025-09-19 03:33 (Tech Lead - from root cause analysis)
**Markers**: [DI] [INFRASTRUCTURE] [PATTERNS]

**What**: Standardize and enforce DI registration order across all projects
**Why**: Registration order conflicts caused BR_022 and wasted 12+ hours

**Enforcement**:
1. Create startup analyzer for order issues
2. Add build-time warnings for violations
3. Unit test to verify registration order

**Done When**:
- [ ] Standardized registration methods
- [ ] Order enforcement tests
- [ ] Update all ServiceConfiguration files

---

### TD_069: Persona Protocol ADR Compliance Update
**Status**: ✅ APPROVED
**Owner**: Tech Lead → All Personas
**Size**: M (4-5h)
**Priority**: Critical - Prevents future violations
**Created**: 2025-09-19 03:33 (Tech Lead - from lessons learned)
**Markers**: [DOCUMENTATION] [ARCHITECTURE] [PROCESS]

**What**: Update all persona protocols to include ADR compliance checks
**Why**: TD_061 violation could have been prevented with proper protocol checks

**Updates Required**:
- Architecture review checklists
- Smell detection guidelines
- Phase 2 review requirements
- ADR compliance verification

**Done When**:
- [ ] All persona protocols updated
- [ ] Review checklists added
- [ ] All personas acknowledge

---

### TD_070: Dynamic Movement Control (Smooth Rerouting)
**Status**: Proposed - Follow-up to TD_065
**Owner**: Tech Lead → Dev Engineer
**Size**: S (2-3h)
**Priority**: Important - Quality of life improvement
**Created**: 2025-09-19 17:49 (Tech Lead - from TD_065 review)
**Markers**: [MOVEMENT] [UX] [DOMAIN]

**What**: Add smooth destination changing without movement interruption
**Why**: Current design requires cancel-then-restart which causes movement stuttering

**Problem Statement**:
- TD_065 supports `CancelMovement()` and `InterruptMovement()`
- Changing destination requires: stop → calculate new path → start
- This creates visible "hiccup" in movement
- Players expect smooth rerouting (like Battle Brothers, XCOM)

**Technical Approach**:
```csharp
// Add to Actor class
public void ChangeDestination(Path newPath)
{
    if (ActivePath != null)
    {
        var oldDestination = ActivePath.FinalDestination;
        ActivePath = newPath;  // Seamless transition
        _domainEvents.Add(new DestinationChangedEvent(
            Id, Position, oldDestination, newPath.FinalDestination));
    }
    else
    {
        StartMovement(newPath);
    }
}
```

**Implementation Pattern**:
- Domain: Add `ChangeDestination()` method to Actor
- Application: Update `MoveActorCommandHandler` to detect rerouting
- Events: Create `DestinationChangedEvent`
- UI: No change needed (already handles path updates)

**Done When**:
- [ ] `ChangeDestination()` method added to Actor
- [ ] `DestinationChangedEvent` created and handled
- [ ] Command handler uses smart rerouting logic
- [ ] Movement transitions smoothly when destination changes
- [ ] Tests verify no position jump or reset
- [ ] Player can click new destination while moving

**Dependencies**: Requires TD_065 complete (domain movement foundation)

---

### TD_071: GameLoop Architecture Documentation (ADR)
**Status**: ✅ COMPLETE
**Owner**: Tech Lead
**Size**: XS (1h)
**Priority**: Important - Architecture documentation
**Created**: 2025-09-19 20:15 (Dev Engineer - from TD_065 Phase 3 discussion)
**Completed**: 2025-09-19 19:26 (Tech Lead - ADR-024 created)
**Markers**: [ARCHITECTURE] [DOCUMENTATION] [GAMELOOP]

**What**: Create ADR documenting GameLoop vs Engine Loop vs Scheduler architecture
**Why**: Critical architectural decision that affects determinism and save/replay

**Problem Statement**:
- Unclear separation between Godot's frame loop and game logic loop
- Need to document why we use TimeUnits not milliseconds
- Clarify relationship between GameLoop, Scheduler, and TimeUnit system

**Technical Approach**:
- Document why custom GameLoop is industry standard for turn-based games
- Explain TimeUnit as universal time currency
- Clarify GameLoop advances by small increments (1 TU), not large chunks
- Show how Scheduler tracks WHO acts, GameLoop manages WHEN to tick

**Done When**:
- [x] ADR created explaining GameLoop architecture
- [x] TimeUnit vs real-time distinction documented
- [x] Scheduler vs GameLoop responsibilities clarified
- [x] Industry examples included (Battle Brothers, XCOM, etc.)

**Dependencies**: Insights from TD_065 Phase 3 implementation

**TECH LEAD COMPLETION** (2025-09-19 19:26):
✅ Created comprehensive ADR-024 documenting:
- TimeUnit-based game loop architecture
- Complete separation from Godot's frame loop
- Clear distinction: GameLoop manages WHEN, Scheduler manages WHO
- Industry examples from Battle Brothers, XCOM, DCSS, ToME
- Integration patterns with existing systems (TD_065, ADR-023)
- Configuration examples and implementation guidance

📄 **Deliverable**: `Docs/03-Reference/ADR/ADR-024-gameloop-architecture.md`

---

### TD_072: UIEventBus FIFO Queue with Concurrent Processing
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: M (6h)
**Priority**: Important - Event ordering affects entire system
**Created**: 2025-09-19 21:16 (Tech Lead - from ADR-024 review discussions)
**Markers**: [ARCHITECTURE] [EVENTS] [CONCURRENCY] [ADR-010]

**What**: Implement proper FIFO event queue with support for concurrent processing of independent events
**Why**: Current UIEventBus lacks ordering guarantees and could process events out of sequence, breaking causality

**Problem Statement**:
- Events can be processed out of order, breaking cause-and-effect relationships
- Example: ActorMoved → ActorAttacked → ActorMoved could process as Move→Move→Attack
- No event sequencing for animations (move animation could start after death animation)
- But also need concurrent processing for performance (particle updates shouldn't block combat events)

**Technical Approach**:
```csharp
public class UIEventBus : IUIEventBus
{
    // Event queue with sequence numbers
    private readonly PriorityQueue<QueuedEvent, long> _eventQueue;
    private long _sequenceNumber = 0;

    // Channel-based concurrency (independent streams)
    private readonly Dictionary<EventChannel, Queue<QueuedEvent>> _channels;

    public async Task PublishAsync<TEvent>(TEvent evt, EventChannel channel = EventChannel.Default)
    {
        var queued = new QueuedEvent
        {
            Event = evt,
            Sequence = Interlocked.Increment(ref _sequenceNumber),
            Channel = channel
        };

        // Events in same channel are FIFO
        // Events in different channels can process concurrently
        _channels[channel].Enqueue(queued);
    }
}

// Usage:
await PublishAsync(new ActorMovedEvent(), EventChannel.Combat);     // FIFO with other combat
await PublishAsync(new ParticleEvent(), EventChannel.Visual);       // Concurrent with combat
await PublishAsync(new UIUpdateEvent(), EventChannel.Interface);    // Concurrent with both
```

**Architectural Constraints**:
☑ Deterministic: Event ordering must be reproducible
☑ Thread-Safe: Multiple producers, safe concurrent processing
☑ Performance: Independent events should process in parallel
☑ Testable: Can verify FIFO ordering in tests

**Implementation Plan**:
1. Add event sequencing with atomic counter
2. Implement channel-based queues for independent event streams
3. Add configurable concurrency level per channel
4. Preserve FIFO within channels, allow parallelism across channels
5. Update ADR-010 to document new guarantees

**Done When**:
- [ ] FIFO ordering guaranteed within event channels
- [ ] Concurrent processing across independent channels
- [ ] Sequence numbers for debugging/tracing
- [ ] Unit tests verify ordering and concurrency
- [ ] Performance tests show improved throughput
- [ ] ADR-010 updated with new architecture
- [ ] No race conditions or event reordering bugs

**Dependencies**: None - can be implemented independently

**TECH LEAD NOTES** (2025-09-19 21:16):
- Critical for animation sequencing and game logic consistency
- Channels allow us to separate concerns (combat vs UI vs particles)
- Must maintain backward compatibility with existing event publishers
- Consider using System.Threading.Channels for implementation

---
