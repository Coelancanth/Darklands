# Darklands Development Backlog


**Last Updated**: 2025-09-30 19:51 (VS_004 Tech Lead architectural refinements complete)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 001
- **Next TD**: 002
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


### VS_004: Infrastructure - Event Bus System [ARCHITECTURE]
**Status**: Approved (Tech Lead refined after Dev Engineer review)
**Owner**: Dev Engineer (ready to implement)
**Size**: S (4-4.5h)
**Priority**: Critical (Prerequisite for Core → Godot communication)
**Markers**: [ARCHITECTURE] [INFRASTRUCTURE] [ADR-002]
**Created**: 2025-09-30
**Updated**: 2025-09-30 (Tech Lead: Architectural refinements based on Dev feedback)

**What**: GodotEventBus to bridge MediatR domain events to Godot nodes
**Why**: Core domain logic needs to notify Godot UI of state changes without coupling

**Architecture** (per ADR-002):
- **IGodotEventBus** (interface) → Core/Infrastructure/Events (abstraction)
- **GodotEventBus** (implementation) → Presentation/Infrastructure/Events (needs Godot.Node)
- **UIEventForwarder<T>** → Bridges MediatR → GodotEventBus (auto-registered via open generics)
- **EventAwareNode** → Godot base class with auto-unsubscribe lifecycle

**How** (Refined Phased Implementation):

**Phase 1: Domain** (~15 min)
- Create `Core/Domain/Events/TestEvent.cs` (record implementing INotification)
- Simple test event for validation: `TestEvent(string Message)`
- No tests needed (just a DTO)

**Phase 2: Infrastructure** (~2.5h) **[CRITICAL: ADR-002 Compliance]**
- **Core layer:**
  - `Core/Infrastructure/Events/IGodotEventBus.cs` (interface only)
- **Presentation layer:**
  - `Presentation/Infrastructure/Events/GodotEventBus.cs`
    - **Strong references** for subscribers (explicit lifecycle via EventAwareNode)
    - Lock-protected subscription dictionary (thread safety)
    - CallDeferred for thread marshalling
    - No cleanup needed (explicit unsubscribe in _ExitTree)
  - `Presentation/Infrastructure/Events/UIEventForwarder.cs`
    - Generic INotificationHandler<TEvent> bridge
- **DI Registration (in GameStrapper or Main._Ready):**
  - `services.AddSingleton<IGodotEventBus, GodotEventBus>()`
  - `services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>))` ← Open generic auto-registration
- **Tests:**
  - Subscribe/Unsubscribe/UnsubscribeAll mechanics
  - PublishAsync notifies all active subscribers
  - Unsubscribed nodes no longer notified
  - Error in one handler doesn't break others
  - UIEventForwarder integration: MediatR.Publish → GodotEventBus
  - Category="Phase2"

**Phase 3: Presentation** (~1.5h)
- Create `Presentation/Components/EventAwareNode.cs`
  - Resolves IGodotEventBus via ServiceLocator in _Ready()
  - **Calls UnsubscribeAll(this) in _ExitTree()** (explicit lifecycle)
  - Child classes override SubscribeToEvents()
- Create test scene: `Presentation/Scenes/Tests/TestEventBusScene.tscn`
  - TestEventListener : EventAwareNode
  - Button → Publishes TestEvent via MediatR
  - Label updates when TestEvent received
- **Manual Test:**
  - Click button → label updates instantly
  - Check logs: MediatR.Publish → UIEventForwarder → GodotEventBus → Subscriber
  - Close scene → verify UnsubscribeAll called in logs

**Done When**:
- ✅ Build succeeds: `dotnet build`
- ✅ Tests pass: `./scripts/core/build.ps1 test --filter "Category=Phase2"`
- ✅ TestEventBusScene manual test passes (button click → label updates)
- ✅ No Godot types in Core project (compile-time enforced)
- ✅ Logs show complete event flow: MediatR → UIEventForwarder → GodotEventBus → Subscribers
- ✅ CallDeferred prevents threading errors (verified manually)
- ✅ EventAwareNode prevents leaks via explicit unsubscribe (verified in logs)
- ✅ Code committed: `feat: event bus system [VS_004]`

**Depends On**: VS_002 (DI), VS_003 (Logging)

**Tech Lead Decision** (2025-09-30 - After Dev Engineer Review):

**✅ ACCEPTED All Dev Engineer Simplifications:**

1. **Strong References > WeakReferences**
   - **Why**: EventAwareNode guarantees `_ExitTree()` fires before GC (node must be in tree to subscribe via `_Ready()`)
   - **Simpler**: No cleanup logic, no dead reference checks, no GC timing uncertainty
   - **More Debuggable**: Leaks are VISIBLE if someone bypasses EventAwareNode (teaches correct usage)
   - **Dev Engineer was right**: Explicit lifecycle is better than "clever" automatic cleanup

2. **UIEventForwarder Open Generic Registration**
   - **Already in ADR-002:362** - Dev Engineer independently discovered the right pattern!
   - **Zero Boilerplate**: MediatR auto-resolves `UIEventForwarder<TEvent>` for ANY `INotification`
   - **Standard Pattern**: `services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>))`

3. **Eliminate Phase 2 (PublishTestEventCommand)**
   - **Dev Engineer was right**: Testing that `Publish()` calls `Publish()` tests MediatR, not our code
   - **Better Tests**: Direct GodotEventBus tests + UIEventForwarder integration tests
   - **Saves 1h**: No throwaway command/handler code

**Architecture Rationale:**
- **Interface Segregation**: IGodotEventBus in Core enables testability without Godot dependencies
- **CallDeferred Required**: Godot UI must be updated on main thread - events can be published from any thread
- **EventAwareNode Pattern**: Enforces correct subscription lifecycle (subscribe in `_Ready()`, unsubscribe in `_ExitTree()`)
- **Risk Mitigation**: If Godot 4 C# CallDeferred with lambdas causes issues, fallback to event queue + `_Process()` approach

**Time Savings**: 4-4.5h (from 5.5h) by eliminating over-testing

**Dev Engineer Review** (2025-09-30 19:43):

*Initial Reaction*: Skeptical - felt like YAGNI (over-engineering before we need it)

*After Reading ADR-002*: **Opinion reversed** - EventBus is justified and needed now.

**✅ What Convinced Me**:
1. **ADR-002 Decision Matrix** (lines 630-644): Clear distinction between Godot Signals (local/visual) vs EventBus (cross-system domain events)
2. **ActorDiedEvent Example**: Shows the real problem - multiple unrelated systems (UI, Combat, World, Quest, Audio) need to react to same domain event. Without EventBus = tight coupling nightmare.
3. **VS_001 Health System** will immediately need this - even "minimal" health needs multiple subscribers (health bar, combat log, death detection, audio)

**EventBus is the RIGHT solution for routing domain state changes to multiple unrelated systems.**

**⚠️ Implementation Concerns** (Honest Technical Feedback):

1. **WeakReference Cleanup Strategy**:
   - *Question*: When do we clean up dead references? On every Publish (perf hit)? Periodically (complexity)?
   - *Alternative*: Explicit unsubscribe in _ExitTree() with strong refs (visible leaks easier to debug)
   - *Recommendation*: Trust Tech Lead's judgment if they've seen Godot memory leaks in practice

2. **UIEventForwarder Boilerplate**:
   - *Concern*: Manual registration for every event type = maintenance burden
   - *Question*: Can we auto-register? `services.AddSingleton(typeof(INotificationHandler<>), typeof(UIEventForwarder<>))`
   - *Recommendation*: Investigate MediatR generic registration before implementing

3. **Phase 2 Over-Testing**:
   - *Concern*: PublishTestEventCommand tests that calling Publish() calls Publish() (testing MediatR, not our code)
   - *Recommendation*: Skip Phase 2, test GodotEventBus directly in Phase 3 (saves 1h, less throwaway code)

**Proposed Simplifications**:
- ✅ Keep overall architecture (validated by ADR-002)
- ⚠️ Skip Phase 2 (merge testing into Phase 3)
- 🔍 Investigate UIEventForwarder auto-registration
- 💭 Consider explicit unsubscribe vs WeakReference (debuggability trade-off)
- ⏱️ Estimated: 4-4.5h (vs 5.5h original)

**Verdict**: **Implement VS_004** - Architecture is sound, minor implementation simplifications won't compromise quality.

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

### TD_001: Architecture Enforcement Tests (NetArchTest + Custom)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer (after approval)
**Size**: M (2-3h)
**Priority**: Important (Prevents architectural drift)
**Markers**: [ARCHITECTURE] [TESTING] [POST-MORTEM]
**Created**: 2025-09-30 (VS_004 post-mortem)

**What**: Automated tests enforcing ADR-001/ADR-002/ADR-003 architectural rules + VS_004 lessons

**Why**:
- VS_004 revealed MediatR double-registration bug (caught by tests, but needs permanent guard)
- DebugConsole LoggingService not registered (need completeness tests)
- ADR-001 says "Core cannot depend on Godot" but no automated enforcement
- **Tests as living documentation** > static docs (self-updating, enforced, always accurate)

**How** (Test Categories):

**1. NetArchTest - Dependency Rules** (~1h)
```csharp
// ADR-001: Core independence from Godot
[Fact]
public void Core_ShouldNotDependOnGodot()
{
    Types.InAssembly(typeof(IGodotEventBus).Assembly)
        .Should().NotHaveDependencyOn("Godot")
        .GetResult().IsSuccessful.Should().BeTrue();
}

// ADR-001: Core independence from Presentation
[Fact]
public void Core_ShouldNotDependOnPresentation()
{
    Types.InAssembly(typeof(IGodotEventBus).Assembly)
        .That().ResideInNamespace("Darklands.Core")
        .Should().NotHaveDependencyOn("Darklands.Infrastructure")
        .GetResult().IsSuccessful.Should().BeTrue();
}

// ADR-003: Result<T> usage (future - optional)
[Fact]
public void PublicMethods_InCommandHandlers_ShouldReturnResult()
{
    // Enforce Result<T> pattern from ADR-003
}
```

**2. MediatR Registration Tests** (~30min)
```csharp
// VS_004 POST-MORTEM: Prevent double-registration
[Fact]
public void MediatR_ShouldNotDoubleRegisterHandlers()
{
    // LESSON: assembly scan + open generic = 2x registration
    var services = new ServiceCollection();
    ConfigureServicesLikeMain(services);

    var handlers = services.BuildServiceProvider()
        .GetServices<INotificationHandler<TestEvent>>();

    handlers.Should().HaveCount(1,
        "UIEventForwarder should only be registered once (open generic pattern)");
}
```

**3. DI Registration Completeness** (~30min)
```csharp
// VS_004 POST-MORTEM: DebugConsole failed to resolve LoggingService
[Fact]
public void AllAutoloadDependencies_ShouldBeRegistered()
{
    // LESSON: Autoloads using ServiceLocator need dependencies in Main.cs
    var services = new ServiceCollection();
    ConfigureServicesLikeMain(services);
    var provider = services.BuildServiceProvider();

    provider.GetService<LoggingService>()
        .Should().NotBeNull("DebugConsole requires LoggingService");
    provider.GetService<IGodotEventBus>()
        .Should().NotBeNull("EventAwareNode requires IGodotEventBus");
}
```

**Done When**:
- ✅ NetArchTest package added to test project
- ✅ ArchitectureTests.cs created with dependency rules
- ✅ MediatRRegistrationTests.cs validates no double-registration
- ✅ DICompletenessTests.cs validates autoload dependencies
- ✅ All architecture tests passing (added to CI pipeline)
- ✅ Category="Architecture" for easy filtering
- ✅ Tests committed with comments explaining VS_004 post-mortem lessons

**Depends On**: None (VS_004 complete)

**Tech Lead Decision** (awaiting approval):
- [ ] Approve scope and priority
- [ ] Add NetArchTest to test infrastructure?
- [ ] Any additional architectural rules to enforce?

**Dev Engineer Notes** (after approval):
- NetArchTest 8.x supports .NET 8
- Tests will be fast (<100ms) - just reflection, no runtime overhead
- Living documentation: Test comments reference ADR-001/002/003 + VS_004 post-mortem
- CI integration: Add `--filter "Category=Architecture"` to quick.ps1

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