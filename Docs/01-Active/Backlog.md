# Darklands Development Backlog


**Last Updated**: 2025-09-30 19:43 (VS_004 Dev Engineer review complete)

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


### VS_004: Infrastructure - Event Bus System [ARCHITECTURE]
**Status**: Approved (Tech Lead breakdown complete)
**Owner**: Tech Lead → Dev Engineer (implement)
**Size**: S (5-6h)
**Priority**: Critical (Prerequisite for Core → Godot communication)
**Markers**: [ARCHITECTURE] [INFRASTRUCTURE] [ADR-002]
**Created**: 2025-09-30
**Updated**: 2025-09-30 (Tech Lead: Detailed breakdown with ADR-002 alignment)

**What**: GodotEventBus to bridge MediatR domain events to Godot nodes
**Why**: Core domain logic needs to notify Godot UI of state changes without coupling

**Architecture** (per ADR-002):
- **IGodotEventBus** (interface) → Core/Infrastructure/Events (abstraction)
- **GodotEventBus** (implementation) → Presentation/Infrastructure/Events (needs Godot.Node)
- **UIEventForwarder<T>** → Bridges MediatR → GodotEventBus
- **EventAwareNode** → Godot base class with auto-unsubscribe lifecycle

**How** (Phased Implementation):

**Phase 1: Domain** (~30 min)
- Create `Core/Domain/Events/TestEvent.cs` (record implementing INotification)
- Simple test event for validation: `TestEvent(string Message)`
- No tests (just a DTO)

**Phase 2: Application** (~1h)
- Create `Core/Application/Commands/PublishTestEventCommand.cs` + Handler
- Handler publishes TestEvent via IMediator.Publish()
- Tests: Verify handler publishes event (mock IMediator)
- Category="Phase2"

**Phase 3: Infrastructure** (~2.5h) **[CRITICAL: ADR-002 Compliance]**
- **Core layer:**
  - `Core/Infrastructure/Events/IGodotEventBus.cs` (interface only)
- **Presentation layer:**
  - `Presentation/Infrastructure/Events/GodotEventBus.cs`
    - WeakReference<object> for subscribers (memory safety)
    - Lock-protected subscription dictionary (thread safety)
    - CallDeferred for thread marshalling
    - Auto-cleanup of dead references
  - `Presentation/Infrastructure/Events/UIEventForwarder.cs`
    - Generic INotificationHandler<TEvent> bridge
- **DI Registration:**
  - Register in Main._Ready() (not Core): `services.AddSingleton<IGodotEventBus, GodotEventBus>()`
  - Register forwarder: `services.AddSingleton<INotificationHandler<TestEvent>>(...)`
- **Tests:**
  - Subscribe/Unsubscribe/UnsubscribeAll
  - PublishAsync notifies all subscribers
  - WeakReference cleanup (freed node no longer notified)
  - Error in one handler doesn't break others
  - Category="Phase3"

**Phase 4: Presentation** (~1.5h)
- Create `Presentation/Components/EventAwareNode.cs`
  - Resolves IGodotEventBus via ServiceLocator in _Ready()
  - Calls UnsubscribeAll(this) in _ExitTree()
  - Child classes override SubscribeToEvents()
- Create test scene: `Presentation/Scenes/Tests/TestEventBusScene.tscn`
  - TestEventListener : EventAwareNode
  - Button → PublishTestEventCommand
  - Labels update when TestEvent received
- **Manual Test:**
  - Click button → labels update
  - Check logs: Command → Event → Subscriber flow
  - Close scene → verify UnsubscribeAll called

**Done When**:
- ✅ Build succeeds: `dotnet build`
- ✅ Tests pass: `./scripts/core/build.ps1 test --filter "Category=Phase2|Category=Phase3"`
- ✅ TestEventBusScene manual test passes (button click → label updates)
- ✅ No Godot types in Core project (compile-time enforced)
- ✅ Logs show complete event flow
- ✅ WeakReference prevents memory leaks (verified in tests)
- ✅ CallDeferred prevents threading errors (verified manually)
- ✅ Code committed: `feat: event bus system [VS_004]`

**Depends On**: VS_002 (DI), VS_003 (Logging)

**Tech Lead Decision** (2025-09-30):
- **Interface Segregation**: IGodotEventBus in Core enables testability without Godot dependencies
- **WeakReference Critical**: Godot nodes can be freed anytime (QueueFree, scene change) - strong refs = memory leaks
- **CallDeferred Required**: Godot UI must be updated on main thread - events can be published from any thread
- **UIEventForwarder Pattern**: MediatR auto-discovers INotificationHandler<T>, one forwarder per event type
- **Risk**: Godot 4 C# CallDeferred with lambdas - if issues arise, use queue + _Process() approach
- **Next Steps**: Dev Engineer implements phases 1-4 sequentially, commits after each phase passes tests

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