### VS_002: Combat Scheduler (Phase 2 - Application Layer)
**Status**: Ready for Dev  
**Owner**: Tech Lead → Dev Engineer
**Size**: S (<4h) - VALIDATED
**Priority**: Important (moved - Grid/Visualization comes first)
**Markers**: [ARCHITECTURE] [PHASE-2]
**Created**: 2025-08-29 14:15

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
- Follows established MediatR patterns from BlockLife

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

**Pattern to Follow**: See BlockLife's `AdvanceTurnCommand` in `src/Features/Turn/Commands/`


### VS_003: Combat Scheduler State Persistence (Phase 3 - Infrastructure)
**Status**: Proposed  
**Owner**: Tech Lead → Dev Engineer
**Size**: M (4-6h)
**Priority**: Critical  
**Markers**: [ARCHITECTURE] [PHASE-3]
**Created**: 2025-08-29 17:12

**What**: Persist scheduler state for save/load and combat recovery
**Why**: Combat must be resumable after save/quit per Vision requirements

**How** (State Management):
- `CombatSchedulerState` class for serializable state
- `CombatSchedulerRepository` for persistence operations
- `SaveSchedulerCommand/LoadSchedulerCommand` for state management
- JSON serialization of actor positions and NextTurn values
- Integration with save system

**Done When**:
- Scheduler state can be saved mid-combat
- Loading restores exact turn order and positions
- Integration tests verify save/load cycle
- Combat can resume exactly where left off

**Acceptance by Phase**:
- Phase 3 (This): State persists correctly to storage
- Phase 4 (Next): UI reflects loaded state properly

**Depends On**: VS_002 (Phase 2 must be complete)

**Implementation Tasks**:
1. **Create Infrastructure folder structure** (10 min)
   - `src/Infrastructure/Combat/`
   - `src/Infrastructure/Combat/Repositories/`
   
2. **Define state types** (45 min)
   - `CombatSchedulerState` - serializable scheduler snapshot
   - `ActorState` - position, NextTurn, stats snapshot
   - `CombatContext` - full combat state container
   
3. **Implement Repository** (90 min)
   - `ICombatSchedulerRepository` interface
   - `JsonCombatSchedulerRepository` implementation
   - Serialization/deserialization logic
   
4. **Create State Commands** (60 min)
   - `SaveCombatStateCommand` - triggers save
   - `LoadCombatStateCommand` - restores state
   - Handlers with error handling
   
5. **Write integration tests** (90 min)
   - Save/load cycle tests
   - State corruption handling
   - Performance with large actor counts


### VS_004: Combat Scheduler UI Display (Phase 4 - Presentation)
**Status**: Proposed  
**Owner**: Tech Lead → Dev Engineer
**Size**: M (4-6h)
**Priority**: Critical
**Markers**: [ARCHITECTURE] [PHASE-4]
**Created**: 2025-08-29 17:12

**What**: Visual display of turn order and time costs in Godot UI
**Why**: Players need to see upcoming turns for tactical decisions

**How** (MVP Pattern):
- `ISchedulerView` interface in Core project
- `SchedulerPresenter` orchestrating display updates
- `SchedulerView` Godot implementation with turn queue
- Visual timeline showing next 5-10 actors
- Time cost preview on hover

**Done When**:
- Turn order displays as vertical/horizontal queue
- Current actor highlighted
- Time costs visible for each queued actor
- Updates smoothly when turns process
- Manual testing confirms UI accuracy

**Acceptance Criteria**:
- Turn order matches internal scheduler state
- Updates happen via CallDeferred (no threading issues)
- Performance smooth with 20+ visible actors
- Clear visual feedback for current turn

**Depends On**: VS_003 (Phase 3 infrastructure)

**Implementation Tasks**:
1. **Create View Interface** (30 min)
   - `src/Features/Combat/Views/ISchedulerView.cs`
   - Define display update methods
   - Async Task signatures for UI operations
   
2. **Implement Presenter** (60 min)
   - `src/Features/Combat/Presenters/SchedulerPresenter.cs`
   - Subscribe to scheduler events
   - Transform data for view consumption
   
3. **Create Godot Scene** (90 min)
   - `godot_project/features/combat/scheduler_display.tscn`
   - Turn queue panel with actor slots
   - Time cost indicators
   
4. **Implement View** (90 min)
   - `godot_project/features/combat/SchedulerView.cs`
   - Implements ISchedulerView
   - CallDeferred for thread-safe updates
   - Animation/transition logic
   
5. **Manual Testing** (60 min)
   - Test with various actor counts
   - Verify visual accuracy
   - Check performance and smoothness
