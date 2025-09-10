# Darklands Development Backlog


**Last Updated**: 2025-09-10 15:55 (TD_027 Phase 1 completed - Save system foundation established)

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



<!-- TD_025 completed with cross-platform determinism CI and moved to archive (2025-09-10 13:59) -->

---

<!-- TD_026 completed with property tests and moved to archive (2025-09-09 18:50) -->

### TD_027: Advanced Save Infrastructure [ARCHITECTURE] [Score: 85/100]
**Status**: In Progress 🔄 (Phase 1 Complete - Phase 2 Next)
**Owner**: Dev Engineer
**Size**: L (1-2 days) - 25% complete
**Priority**: Important (Phase 2 - needed before save system)
**Markers**: [ARCHITECTURE] [SAVE-SYSTEM] [ADR-005] [INFRASTRUCTURE]
**Created**: 2025-09-09 17:44
**Phase 1 Completed**: 2025-09-10 15:53

**What**: Production-ready save system infrastructure per enhanced ADR-005
**Why**: Basic save patterns insufficient for production game

**✅ Phase 1 COMPLETED (2025-09-10 15:53)**: Domain Layer - Save Contracts & Models
- ✅ GameState root aggregate with complete persistent state
- ✅ RandomState - Deterministic RNG state per ADR-004
- ✅ SaveContainer, SaveMetadata, SaveSlot - Core save data models
- ✅ ISerializationProvider - Pluggable serialization abstraction (Domain)
- ✅ ISaveValidator - Save data integrity validation contract (Domain)
- ✅ ISaveStorage - Platform-independent filesystem abstraction (Application)
- ✅ IWorldHydrator - Godot scene reconstruction contract (Application)
- ✅ 613/613 tests passing - Zero architecture violations
- ✅ Clean Architecture compliance - Async concerns properly layered

**🚧 Phase 2 NEXT**: Application Layer - Save Commands & Orchestration (Est: 2h)
**Session Goal**: Establish MediatR command/handler pipeline for save operations

**2.1 Save Commands** (`src/Application/SaveSystem/Commands/`):
- `SaveGameCommand(SaveSlot, GameState)` + `SaveGameCommandHandler`
  - Orchestrates: Validate → Serialize → Compress → Store → Notify
  - Returns: `Fin<SaveMetadata>` with save details
- `LoadGameCommand(SaveSlot)` + `LoadGameCommandHandler` 
  - Orchestrates: Load → Validate → Decompress → Deserialize → Hydrate
  - Returns: `Fin<GameState>` for world reconstruction
- `QuickSaveCommand(GameState)` + Handler - F5 functionality
- `AutoSaveCommand(GameState, string reason)` + Handler - Triggered saves

**2.2 Save Queries** (`src/Application/SaveSystem/Queries/`):
- `GetSaveMetadataQuery(SaveSlot)` + Handler - Save slot info for UI
- `ListSavesQuery()` + Handler - All available saves
- `ValidateSaveQuery(SaveSlot)` + Handler - Integrity check
- `GetSaveVersionQuery(SaveSlot)` + Handler - Compatibility check

**2.3 Save Events** (`src/Application/SaveSystem/Events/`):
- `GameSavedEvent(SaveSlot, SaveMetadata)` - Success notification
- `GameLoadedEvent(GameState, SaveMetadata)` - Load completion
- `SaveFailedEvent(SaveSlot, Error)` - Error notification  
- `AutoSaveTriggeredEvent(string reason)` - Auto-save initiation

**2.4 Application Services** (`src/Application/SaveSystem/Services/`):
- `ISaveOrchestrator` - High-level save/load coordination
- `SaveGameOrchestrator` implementation with dependency injection

**Handoff Criteria**: All commands/handlers compile, tests pass, ready for infrastructure

---

**Phase 3**: Infrastructure Layer - Core Implementations (Est: 3h)
**Session Goal**: Provide concrete implementations of all save system contracts

**3.1 Serialization Providers** (`src/Infrastructure/Serialization/`):
- `SystemTextJsonProvider` - Default fast JSON serialization
  - Custom converters: ActorId, GridId, ImmutableCollections, Position
  - Settings: Camel case, ignore nulls, compact output
- `NewtonsoftJsonProvider` - Advanced scenarios (polymorphism, JsonExtensionData)
  - Settings: TypeNameHandling.None, NullValueHandling.Ignore
  - Support for custom converters and extension data

**3.2 Storage Implementations** (`src/Infrastructure/Storage/`):
- `LocalFileStorage` - Cross-platform file operations
  - Atomic writes: temp file + rename pattern
  - Directory creation with proper permissions
  - Error handling with meaningful messages
- `GodotSaveStorage` - Godot-specific implementation
  - Uses `OS.GetUserDataDir()` + "saves/" subdirectory  
  - Integrates with Godot's file system APIs
  - Platform-specific path handling (Windows/Linux/Mac)

**3.3 Save Validation** (`src/Infrastructure/Validation/`):
- `SaveValidator` - Integrity and compliance validation
  - SHA-256 checksum calculation and verification
  - Save container structure validation
  - GameState ADR-005 compliance checking
  - Version compatibility validation

**3.4 Compression Services** (`src/Infrastructure/Compression/`):
- `GZipCompressionService` - Standard compression
  - Compress GameState to byte arrays
  - Configurable compression levels
  - Error handling for corrupt data

**3.5 World Hydration** (`src/Infrastructure/Hydration/`):
- `GodotWorldHydrator` - Scene reconstruction from GameState
  - Phase-based hydration: Grid → Actors → Positions → Transient
  - Progress reporting for large saves
  - Error recovery and validation
- `TransientStateFactory` - Recreate runtime-only data
- Supporting hydrator classes for specific entity types

**3.6 Migration Pipeline** (`src/Infrastructure/Migration/`):
- `ISaveMigration` interface for version upgrades
- `SaveMigrator` - Chains migrations from old → current version
- `SaveVersion1Migration` - Example migration for future use

**3.7 DI Registration** (`src/Infrastructure/DependencyInjection/`):
- Update `GameStrapper.cs` with all save system services
- Proper service lifetimes (Singleton for stateless, Scoped for stateful)
- Configuration options for serialization/compression

**Handoff Criteria**: Save/load roundtrip works with test data, all quality gates pass

---

**Phase 4**: Presentation Layer - UI Integration (Est: 1h)  
**Session Goal**: Connect save system to Godot UI and provide user-facing functionality

**4.1 Save Game Presenter** (`src/Presentation/Presenters/`):
- `SaveGamePresenter` - Manages save/load UI interactions
  - F5/F9 hotkey handling via input service
  - Save slot selection and metadata display
  - Progress indicators for long operations
  - Error message display with user-friendly text

**4.2 Godot Scene Integration** (`godot_project/scenes/ui/`):
- Wire save menu scenes to presenter
- Connect F5 (QuickSave) and F9 (QuickLoad) inputs
- Save slot UI with metadata display (turn, time, screenshot)
- Progress bars and loading indicators

**4.3 Input System Integration**:
- Register F5/F9 hotkeys in input map
- Connect hotkeys to save/load commands via presenter
- Handle input during gameplay vs menu states

**4.4 Error Handling & User Feedback**:
- User-friendly error messages for save failures
- Confirmation dialogs for overwrite operations
- Recovery options for corrupted saves
- Auto-save notifications (subtle, non-intrusive)

**4.5 Manual Testing Checklist**:
- [ ] F5 quick save creates save file
- [ ] F9 quick load restores game state correctly
- [ ] Save menu shows metadata (turn, time, actors)
- [ ] Manual save slots work (Save 1-5)
- [ ] Auto-save triggers correctly
- [ ] Error messages appear for failures
- [ ] Large saves show progress indicators
- [ ] Cross-platform paths work correctly

**Handoff Criteria**: All UI functionality working, F5/F9 hotkeys functional, user testing complete

---

**📋 Multi-Session Protocol**:

**Session Handoff Requirements**:
- Each phase must compile and pass all tests before handoff
- Memory bank entry updated with current progress and next steps  
- Commit with proper phase marker: `feat(save): TD_027 Phase X - Description [Phase X/4]`
- Todo list updated to reflect phase completion
- Any blocking issues documented with resolution steps

**Quality Gates Per Phase**:
- **Phase 2**: All MediatR handlers registered, commands compile, pipeline tested
- **Phase 3**: Save/load roundtrip functional, compression working, validation passing  
- **Phase 4**: Manual testing checklist 100% complete, production-ready

**Rollback Strategy**:
- Each phase committed separately for safe rollback points
- Failed integration can revert to previous working phase
- Branch protection ensures main branch stability

**Done When (Overall TD_027)**:
- ✅ All infrastructure interfaces defined (Phase 1 ✅)
- All MediatR commands/handlers implemented (Phase 2)
- Reference implementations complete (Phase 3)
- Save/load works with test data (Phase 3)
- Migration pipeline tested (Phase 3)
- Platform differences abstracted (Phase 3)  
- F5/F9 hotkeys working (Phase 4)
- Manual testing checklist 100% complete (Phase 4)
- Production-ready save system operational

**Dependencies**:
- ✅ TD_021 (Save-Ready entities) - Complete
- ✅ MediatR pipeline functional - Complete
- ✅ GameStrapper DI system - Complete
- Clean Architecture compliance maintained throughout

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