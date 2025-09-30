# Darklands Development Backlog


**Last Updated**: 2025-09-30 10:58 (VS_002-004 created for skeleton infrastructure)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 001
- **Next TD**: 001
- **Next VS**: 005 


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

### VS_002: Infrastructure - Dependency Injection Foundation [ARCHITECTURE] ✅
**Status**: Ready for Review (Work Complete)
**Owner**: Dev Engineer → User (approval)
**Size**: S (3-4h) ← Simplified after ultrathink (actual: ~3h)
**Priority**: Critical (Foundation for VS_003, VS_004, VS_001)
**Markers**: [ARCHITECTURE] [FRESH-START] [INFRASTRUCTURE]
**Created**: 2025-09-30
**Broken Down**: 2025-09-30 (Tech Lead)
**Simplified**: 2025-09-30 (Dev Engineer ultrathink - removed IServiceLocator interface per ADR-002)
**Completed**: 2025-09-30 13:24 (All 3 phases implemented and tested)

**What**: Set up Microsoft.Extensions.DependencyInjection as the foundation for the application
**Why**: Need DI container before we can inject loggers, event bus, or any services

**Dev Engineer Simplification** (2025-09-30):
After ultrathink analysis, removed unnecessary IServiceLocator interface. ADR-002 shows static ServiceLocator class, not interface implementation. Simplified to 3 phases while maintaining all quality gates.

**Phase 1: GameStrapper** (~2h)
- File: `src/Darklands.Core/Application/Infrastructure/GameStrapper.cs`
- Implements: Initialize(), GetServices(), RegisterCoreServices()
- Includes temporary ITestService for validation
- Tests: Initialization idempotency, service resolution, test service
- Gate: `dotnet test --filter "Category=Phase1"` must pass
- Commit: `feat(VS_002): add GameStrapper with DI foundation [Phase 1/3]`

**Phase 2: ServiceLocator** (~1-2h)
- File: `src/Darklands.Core/Infrastructure/DependencyInjection/ServiceLocator.cs`
- Static class with GetService<T>() and Get<T>() methods
- Returns Result<T> for functional error handling
- Service lifetime examples (Singleton, Transient)
- Tests: Resolution success/failure, lifecycle validation
- Gate: `dotnet test --filter "Category=Phase2"` must pass
- Commit: `feat(VS_002): add ServiceLocator for Godot boundary [Phase 2/3]`

**Phase 3: Godot Test Scene** (~1h)
- Files:
  - `TestScenes/DI_Bootstrap_Test.tscn` (Godot scene)
  - `TestScenes/DIBootstrapTest.cs` (test script)
- Manual test: Button click resolves service, updates label
- Validation: Console shows success messages, no errors
- Commit: `feat(VS_002): add Godot validation scene [Phase 3/3]`

**Done When**:
- ✅ All Core tests pass (dotnet test) - **13/13 PASS**
- ✅ GameStrapper.Initialize() succeeds - **VERIFIED**
- ✅ ServiceLocator.GetService<T>() returns Result<T> - **VERIFIED**
- ✅ Godot test scene works (manual validation) - **SCENE CREATED**
- ✅ No Godot references in Core project (dotnet list package) - **VERIFIED**
- ✅ All 3 phase commits exist in git history - **VERIFIED**

**Depends On**: None (first foundation piece)

**Implementation Notes**:
- ServiceLocator is static class (NOT autoload) - initialized in Main scene root per ADR-002
- ServiceLocator ONLY for Godot _Ready() methods - Core uses constructor injection
- ITestService is temporary—remove after VS_001 complete
- Simplified from 4 phases to 3 by removing unnecessary interface abstraction

**Completion Summary** (2025-09-30 13:24):

✅ **Phase 1 Complete** (commit 9885cb2):
- GameStrapper with Initialize(), GetServices(), RegisterCoreServices()
- 6 tests passing (Category=Phase1)
- Thread-safe, idempotent initialization
- Functional error handling with Result<T>

✅ **Phase 2 Complete** (commit ffb53f9):
- ServiceLocator static class (GetService<T>, Get<T>)
- 7 tests passing (Category=Phase2) - Total: 13 tests
- Godot boundary pattern per ADR-002
- Comprehensive error messages

✅ **Phase 3 Complete** (commit 108f006):
- TestScenes/DI_Bootstrap_Test.tscn created
- TestScenes/DIBootstrapTest.cs with manual validation
- Godot project builds: 0 errors ✅
- No Godot packages in Core verified ✅

**Files Created**:
- src/Darklands.Core/Application/Infrastructure/GameStrapper.cs
- src/Darklands.Core/Infrastructure/DependencyInjection/ServiceLocator.cs
- tests/Darklands.Core.Tests/Application/Infrastructure/GameStrapperTests.cs
- tests/Darklands.Core.Tests/Infrastructure/DependencyInjection/ServiceLocatorTests.cs
- TestScenes/DIBootstrapTest.cs
- TestScenes/DI_Bootstrap_Test.tscn

**Next Steps**:
→ User manual test: Run TestScenes/DI_Bootstrap_Test.tscn in Godot (F6)
→ If satisfied, mark "Done" and proceed to VS_003 (Logging)
→ VS_003, VS_004, VS_001 now unblocked and ready for implementation

---

### VS_003: Infrastructure - Logging System with Runtime Configuration [ARCHITECTURE]
**Status**: Proposed
**Owner**: Product Owner → Tech Lead (breakdown)
**Size**: M (6-8h)
**Priority**: Critical (Prerequisite for debugging all other features)
**Markers**: [ARCHITECTURE] [INFRASTRUCTURE] [DEVELOPER-EXPERIENCE]
**Created**: 2025-09-30

**What**: Production-grade logging system with Microsoft.Extensions.Logging supporting multiple sinks and runtime configuration
**Why**: Need comprehensive logging for development, debugging, and monitoring - must work in both Core C# and Godot contexts

**Architecture Decision**:
- ❌ NOT Godot Autoload (would violate ADR-001 Clean Architecture)
- ✅ **Microsoft.Extensions.Logging abstractions in Core** (portable, industry standard)
- ✅ **Serilog as provider in Presentation** (implementations live in Godot project)
- ✅ Core uses `ILogger<T>` from MS.Extensions.Logging.Abstractions (zero concrete dependencies)
- ✅ Presentation provides Serilog with multiple sinks (Console, Godot RichText, File)
- ✅ Runtime dynamic configuration (change levels/sinks without restart)

**Scope**:
1. **Core Layer (Abstractions Only)**:
   - Uses `ILogger<T>` from Microsoft.Extensions.Logging.Abstractions
   - No concrete logging implementations (enforced by .csproj)

2. **Infrastructure Layer (Serilog Implementation)**:
   - GameStrapper configures Serilog as MS.Extensions.Logging provider
   - Serilog sinks: Console, File, GodotRichText (custom)
   - ILoggingService interface for runtime sink management
   - LoggingService implementation with LoggingLevelSwitch
   - Runtime sink enable/disable/toggle
   - Runtime log level changes (Trace → Debug → Info → Warn → Error)

3. **Presentation Layer (Godot Project)**:
   - Custom Serilog sink: GodotRichTextSink (BBCode formatting)
   - DebugConsoleNode (RichTextLabel for in-game log display)
   - LogSettingsPanel (UI for runtime toggles)
   - Serilog packages live in Darklands.csproj (not Core!)

4. **Tests**:
   - Core code uses ILogger<T> from abstractions only
   - Verify: Core.csproj has NO Serilog packages (only MS.Extensions.Logging.Abstractions)
   - Serilog can write to multiple sinks simultaneously
   - Can enable/disable sinks at runtime
   - Can change log level at runtime

**Rich Format Support**:
```
[color=red][b]ERROR[/b][/color] [10:45:12] ActorService: Actor 123 not found
[color=yellow]WARN[/color] [10:45:13] CombatHandler: Low health warning
[color=cyan]INFO[/color] [10:45:14] GameStrapper: Services initialized
[color=gray]DEBUG[/color] [10:45:15] EventBus: Subscribed to HealthChangedEvent
[color=#666]TRACE[/color] [10:45:16] Handler: Entering ExecuteAttack method
```

**How** (Implementation Order):
1. **Phase 1: Core** (~0.5h)
   - Verify Core.csproj uses ONLY Microsoft.Extensions.Logging.Abstractions
   - NO Serilog packages in Core!
   - Define ILoggingService interface (optional - for runtime config)

2. **Phase 2: Infrastructure - GameStrapper** (~1.5h)
   - Configure Serilog as MS.Extensions.Logging provider
   - Add Serilog sinks: Console, File
   - Register ILogger<T> with DI via `services.AddLogging(builder => builder.AddSerilog())`
   - Add LoggingLevelSwitch for runtime level changes
   - Test: Core handler uses ILogger<T> and logs appear

3. **Phase 3: Infrastructure - Custom Godot Sink** (~2h)
   - Create GodotRichTextSink : ILogEventSink (Serilog custom sink)
   - BBCode formatting for rich colors
   - CallDeferred for thread-safe UI updates
   - Register in GameStrapper: `.WriteTo.GodotRichText()`
   - LoggingService for runtime sink enable/disable

4. **Phase 4: Presentation - UI** (~2h)
   - DebugConsoleNode (RichTextLabel scene for logs)
   - LogSettingsPanel (dropdowns: log level, checkboxes: sinks)
   - Wire to LoggingService for runtime config
   - Manual test: Toggle sinks, change levels at runtime

**Done When**:
- ✅ Core code can use ILogger<T> via constructor injection
- ✅ Logs appear in System.Console with colors
- ✅ Logs appear in Godot in-game console with rich BBCode formatting
- ✅ Logs persist to file
- ✅ Can toggle each sink on/off at runtime (immediate effect)
- ✅ Can change log level at runtime (Trace/Debug/Info/Warn/Error)
- ✅ Tests verify Core has no Godot dependencies
- ✅ In-game debug window shows logs beautifully formatted
- ✅ Code committed with message: "feat: logging system with runtime config [VS_003]"

**Depends On**: VS_002 (needs DI to inject ILogger)

**Tech Lead Notes**:
- **CRITICAL**: Core uses `ILogger<T>` from Microsoft.Extensions.Logging.**Abstractions** ONLY
- **CRITICAL**: All Serilog packages go in Darklands.csproj (Godot project), NOT Core!
- Pattern: Core → Abstraction, Infrastructure/Presentation → Implementation
- Serilog acts as provider: `services.AddLogging(builder => builder.AddSerilog(Log.Logger))`
- Custom sink: GodotRichTextSink : ILogEventSink (Serilog interface)
- GodotRichTextSink must use CallDeferred for thread-safe UI updates
- LogSettingsPanel should be accessible via F12 or debug menu
- Reference: Same pattern as ASP.NET Core uses Serilog

---

### VS_004: Infrastructure - Event Bus System [ARCHITECTURE]
**Status**: Proposed
**Owner**: Product Owner → Tech Lead (breakdown)
**Size**: S (4-6h)
**Priority**: Critical (Prerequisite for Core → Godot communication)
**Markers**: [ARCHITECTURE] [INFRASTRUCTURE]
**Created**: 2025-09-30

**What**: GodotEventBus to bridge MediatR domain events to Godot nodes
**Why**: Core domain logic needs to notify Godot UI of state changes without coupling

**Scope**:
1. **Infrastructure Layer**:
   - GodotEventBus (subscribes to MediatR INotification)
   - Thread marshalling to main thread (CallDeferred)
   - EventAwareNode base class (subscribe/unsubscribe pattern)
   - Automatic cleanup on node disposal

2. **Tests**:
   - Can publish event from Core
   - Godot node receives event
   - Thread marshalling works correctly
   - Unsubscribe prevents memory leaks

**How** (Implementation Order):
1. **Phase 1: Domain** (~1h)
   - Define event interfaces
   - Simple test event (TestEvent)

2. **Phase 2: Application** (~1h)
   - GodotEventBus implements INotificationHandler<T>
   - Event subscription registry

3. **Phase 3: Infrastructure** (~2h)
   - Thread marshalling implementation
   - EventAwareNode base class
   - Automatic unsubscribe on _ExitTree()

4. **Phase 4: Presentation** (~1h)
   - Simple test scene with EventAwareNode
   - Publish test event from Core
   - Verify node receives event on main thread
   - Manual test: Event updates Godot UI correctly

**Done When**:
- ✅ Can publish MediatR notification from Core
- ✅ Godot nodes receive events via EventBus.Subscribe<T>()
- ✅ Events delivered on main thread (no threading issues)
- ✅ EventAwareNode auto-unsubscribes on disposal
- ✅ Tests verify no memory leaks from subscriptions
- ✅ Simple test scene demonstrates Core → Godot event flow
- ✅ Code committed with message: "feat: event bus system [VS_004]"

**Depends On**: VS_002 (needs DI)

---

### VS_001: Architectural Skeleton - Health System Walking Skeleton [ARCHITECTURE]
**Status**: Proposed
**Owner**: Product Owner → Tech Lead (breakdown)
**Size**: S (4-6h)
**Priority**: Critical (Validates architecture with real feature)
**Markers**: [ARCHITECTURE] [WALKING-SKELETON] [END-TO-END]
**Created**: 2025-09-30
**Updated**: 2025-09-30 (Reduced scope - DI/Logger/EventBus now separate)

**What**: Implement minimal health system to validate complete architecture end-to-end
**Why**: Prove the architecture works with a real feature after infrastructure is in place

**Context**:
- VS_002, VS_003, VS_004 provide foundation (DI, Logger, EventBus)
- This is the FIRST REAL FEATURE using that foundation
- Follow "Walking Skeleton" pattern: thinnest possible slice through all layers
- Validates ADR-001, ADR-002, ADR-003 work together

**Scope** (Minimal Health System):
1. **Domain Layer** (Pure C#):
   - Health value object with Create/Reduce/Increase
   - IHealthComponent interface
   - HealthComponent implementation
   - Use Result<T> for all operations

2. **Application Layer** (CQRS):
   - TakeDamageCommand + Handler (uses ILogger<T>, publishes events)
   - HealthChangedEvent (INotification)
   - Simple in-memory component registry

3. **Infrastructure Layer**:
   - Register health services in GameStrapper
   - ComponentRegistry implementation

4. **Presentation Layer** (Godot):
   - Simple test scene with one actor sprite
   - HealthComponentNode (EventAwareNode, shows health bar)
   - Button to damage actor
   - Verify: Click button → Command → Event → Health bar updates

5. **Tests**:
   - Health value object tests (validation, reduce, increase)
   - TakeDamageCommandHandler tests (with mocked logger)
   - Integration test: Command → Event flow

**NOT in Scope** (defer to later):
- Grid system
- Movement
- Complex combat mechanics
- Multiple actors
- AI
- Turn system
- Fancy UI

**How** (Implementation Order):
1. **Phase 1: Domain** (~1h)
   - Create Health value object with tests
   - Create IHealthComponent + HealthComponent
   - Tests: Health.Create, Reduce, validation with Result<T>

2. **Phase 2: Application** (~2h)
   - TakeDamageCommand + Handler (inject ILogger, IMediator)
   - HealthChangedEvent (INotification)
   - ComponentRegistry service
   - Tests: Handler logic with mocked logger and registry

3. **Phase 3: Infrastructure** (~1h)
   - Register services in GameStrapper
   - ComponentRegistry implementation
   - Verify DI resolution works

4. **Phase 4: Presentation** (~2h)
   - Simple scene (1 sprite + health bar + damage button)
   - HealthComponentNode extends EventAwareNode
   - Subscribe to HealthChangedEvent
   - Wire button click → Send TakeDamageCommand
   - Manual test: Click button → see logs → health bar updates

**Done When**:
- ✅ Build succeeds (dotnet build)
- ✅ Core tests pass (100% pass rate)
- ✅ Godot project loads without errors
- ✅ Can click "Damage" button and health bar updates smoothly
- ✅ Logs appear in debug console showing command execution
- ✅ No Godot references in Darklands.Core project
- ✅ GodotEventBus routes HealthChangedEvent correctly
- ✅ CSharpFunctionalExtensions Result<T> works end-to-end
- ✅ All 3 ADRs validated with working code
- ✅ Code committed with message: "feat: health system walking skeleton [VS_001]"

**Depends On**: VS_002 (DI), VS_003 (Logger), VS_004 (EventBus)

**Product Owner Notes** (2025-09-30):
- This is the FOUNDATION - everything else builds on this
- Keep it MINIMAL - resist adding features
- Validate architecture first, optimize later
- Success = simple but complete end-to-end flow

**Acceptance Test Script**:
```
1. Run: dotnet build src/Darklands.Core/Darklands.Core.csproj
   Expected: Build succeeds, no warnings

2. Run: dotnet test tests/Darklands.Core.Tests/Darklands.Core.Tests.csproj
   Expected: All tests pass

3. Open Godot project
   Expected: No errors in console

4. Run test scene
   Expected: See sprite with health bar above it

5. Click "Damage" button
   Expected: Health bar decreases, animation plays

6. Click repeatedly until health reaches 0
   Expected: Sprite disappears or "Dead" appears
```

---




---

## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*



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



---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*