# ADR-004: Feature-Based Clean Architecture with Event Discipline

**Status**: Approved
**Date**: 2025-09-30
**Last Updated**: 2025-09-30
**Decision Makers**: Tech Lead

**Changelog**:
- 2025-09-30: Added production readiness improvements - Godot threading constraints (CallDeferred), PowerShell CI scripts, EventHandlers/ folder, event contract versioning, tightened Presentation dependencies
- 2025-09-30: Added governance process for Domain/Common/, Command vs Event decision framework, refined Rule 3 to Terminal Subscribers, added future automation considerations
- 2025-09-30: Initial creation - hybrid VSA + Clean Architecture with Five Event Rules

---

## Context

We need to decide how to organize code in a game development context where:

1. **Features are numerous** (Health, Combat, Movement, Inventory, AI, Skills, etc.)
2. **Features share primitives** (ActorId, Health, Position used by multiple systems)
3. **Features must communicate** (Combat affects Health, Health triggers UI updates)
4. **Code must be testable** (without Godot runtime for core logic)
5. **Architecture must scale** (hundreds of features over years of development)

### The Three Architectural Approaches

**1. Pure Clean Architecture** (Layered)
```
Domain/
Application/
Infrastructure/
Presentation/
```

**Pros**: Clear separation of concerns, dependency inversion, testable
**Cons**: Technical grouping obscures features, finding code for "Health System" spans 4 folders

---

**2. Pure Vertical Slice Architecture** (Feature-First)
```
Features/
  CreateCustomer/
  UpdateCustomer/
  DeleteCustomer/
```

**Pros**: Feature independence, easy to find code, rapid development
**Cons**: Massive duplication when primitives are shared (Health used by 7+ systems)

---

**3. Event-Driven Architecture** (Unconstrained)
```
Commands trigger Events
Events trigger more Events
Events trigger side effects
```

**Pros**: Loose coupling, reactive systems
**Cons**: **Event soup** - cascading events create spaghetti, debugging nightmares

---

### The Problem with Pure Approaches

**Game systems violate VSA assumptions**:
- Health isn't a "slice" - it's a **fundamental primitive**
- Combat, Healing, Poison, Regeneration, UI, AI, Achievements all use Health
- Duplicating Health logic 7 times = unmaintainable

**Game systems violate Clean Architecture assumptions**:
- Finding all Health code shouldn't require opening 4 folders
- Features aren't "layers" - they're **capabilities**

**Event-driven without discipline = Event soup**:
```
HealthChangedEvent
→ ActorDiedEvent
  → ShowDeathAnimationEvent
    → PlayDeathSoundEvent
      → UpdateAchievementsEvent
        → ShowAchievementPopupEvent (6 levels deep!)
```

**Debugging this is hell** - which event caused the bug? Impossible to trace.

---

## Decision

We adopt **Feature-Based Clean Architecture with Event Discipline** - a pragmatic hybrid:

1. ✅ **VSA**: Organize by feature (`Features/Combat/`, `Features/Health/`)
2. ✅ **Clean Architecture**: Share domain primitives (`Domain/Common/`)
3. ✅ **Clean Architecture**: Layer separation within each feature
4. ✅ **Event-Driven**: Features communicate via events (intentional coupling)
5. ✅ **Event Discipline**: Five Rules to prevent event soup

---

## Architecture Structure

### Folder Organization

```
src/Darklands.Core/
├── Domain/
│   └── Common/                    # SHARED PRIMITIVES (Clean Architecture)
│       ├── ActorId.cs            # Used by ALL features
│       ├── Health.cs             # Used by ALL features
│       ├── Position.cs           # Used by ALL features
│       └── ...                   # Max ~10 files - resist adding more
│
└── Features/                      # FEATURE ORGANIZATION (VSA)
    ├── Combat/                    # Feature: Combat System
    │   ├── Domain/
    │   │   ├── AttackType.cs
    │   │   └── IDamageCalculator.cs
    │   ├── Application/
    │   │   ├── Commands/
    │   │   │   ├── ExecuteAttackCommand.cs
    │   │   │   └── ExecuteAttackCommandHandler.cs
    │   │   └── Events/
    │   │       └── AttackExecutedEvent.cs
    │   └── Infrastructure/
    │       └── DamageCalculatorService.cs
    │
    ├── Health/                    # Feature: Health Management
    │   ├── Domain/
    │   │   ├── IHealthComponent.cs
    │   │   └── HealthComponent.cs
    │   ├── Application/
    │   │   ├── Commands/
    │   │   │   ├── TakeDamageCommand.cs
    │   │   │   └── TakeDamageCommandHandler.cs
    │   │   ├── Events/
    │   │   │   └── HealthChangedEvent.cs
    │   │   └── EventHandlers/         # C# event handlers (MediatR)
    │   │       ├── HealthChangedLoggingHandler.cs
    │   │       └── HealthChangedAnalyticsHandler.cs
    │   └── Infrastructure/
    │       └── HealthComponentRegistry.cs
    │
    └── Movement/                  # Feature: Movement System
        ├── Domain/
        ├── Application/
        └── Infrastructure/
```

### Key Decisions

| Concern | Decision | Rationale |
|---------|----------|-----------|
| **Folder structure** | Features/ + Domain/Common/ | VSA organization with shared primitives |
| **Feature boundaries** | Each feature has own Commands/Handlers/Events | Independence per VSA principles |
| **Shared primitives** | Domain/Common/ for cross-cutting types | Prevent duplication (Clean Architecture) |
| **Layer separation** | Within each feature folder | Maintain dependency rules (Clean Architecture) |
| **Feature communication** | Via events only | Intentional coupling, async decoupling |

---

## The Five Event Rules (Prevent Event Soup)

### Rule 1: Commands Orchestrate, Events Notify

**Commands do ALL the work**, events are broadcast notifications.

```csharp
// ✅ CORRECT: Command handler orchestrates
public class TakeDamageCommandHandler : IRequestHandler<TakeDamageCommand, Result<DamageResult>>
{
    public async Task<Result<DamageResult>> Handle(...)
    {
        return await GetActor(cmd.ActorId)
            .Bind(actor => actor.TakeDamage(cmd.Amount))
            .Tap(result => {
                // Publish single event with ALL info at the END
                _mediator.Publish(new HealthChangedEvent(
                    ActorId: cmd.ActorId,
                    OldHealth: oldHealth,
                    NewHealth: result.Current,
                    IsDead: result.IsDead,
                    IsCritical: result.IsCritical
                ));
            });
    }
}

// ❌ WRONG: Event handler does work
public class HealthChangedEventHandler : INotificationHandler<HealthChangedEvent>
{
    public async Task Handle(HealthChangedEvent evt, ...)
    {
        // DON'T DO THIS - event soup!
        if (evt.IsDead)
            await _mediator.Publish(new ActorDiedEvent(evt.ActorId));
    }
}
```

**Rationale**: Commands are explicit orchestration points. Events are reactive notifications. Mixing these concerns creates hidden control flow.

---

### Rule 2: Events Are Facts (Past Tense), Not Commands

**Events describe what happened**, not what should happen.

```csharp
// ✅ CORRECT: Describes what happened (past tense)
public record HealthChangedEvent(
    ActorId ActorId,
    float OldHealth,
    float NewHealth,
    bool IsDead,
    bool IsCritical
);

public record AttackExecutedEvent(ActorId AttackerId, ActorId TargetId, float Damage);

// ❌ WRONG: Imperative, sounds like a command
public record UpdateHealthBarEvent(ActorId ActorId);
public record KillActorEvent(ActorId ActorId);
public record DamageTargetEvent(ActorId ActorId, float Amount);
```

**Rationale**: Past-tense naming makes it clear events are notifications. Imperative naming suggests events do work, encouraging event soup.

---

### Rule 3: Event Handlers Are Terminal Subscribers

**Definition**: Event handlers are **terminal subscribers** - they execute terminal operations without publishing more events or sending commands.

**Terminal Operations** (Allowed):
- ✅ **UI updates** - health bars, labels, animations, visual feedback
- ✅ **Logging** - debug logs, analytics events, telemetry
- ✅ **Sound/VFX** - play sound effects, spawn particle effects
- ✅ **Stat tracking** - increment achievements, update counters, record statistics
- ✅ **Local state updates** - cache refresh, UI state changes, temporary values

**Non-Terminal Operations** (FORBIDDEN):
- ❌ **Publishing more events** - cascading events (violates Rule 4)
- ❌ **Sending commands** - orchestration belongs in command handlers
- ❌ **Business logic** - validation, calculations, domain rules
- ❌ **State mutations** - changing domain entities, persisting data

**The Test**: "If I remove all event handlers for this event, does the core system still work correctly?"
- If **YES** → Event handlers are terminal (correct) ✅
- If **NO** → Business logic leaked into handlers (incorrect) ❌

**Examples**:

```csharp
// ✅ CORRECT: Terminal subscriber (UI update)
public partial class HealthBarNode : EventAwareNode
{
    private void OnHealthChanged(HealthChangedEvent evt)
    {
        if (evt.ActorId != _actorId) return;

        // Terminal: UI updates only, no cascading
        UpdateHealthBar(evt.NewHealth, evt.IsCritical);

        if (evt.IsDead)
            Hide();  // Simple visual change
    }
}

// ✅ CORRECT: Terminal subscriber (achievements)
public class AchievementTracker : INotificationHandler<ActorDiedEvent>
{
    public async Task Handle(ActorDiedEvent evt, CancellationToken ct)
    {
        // Terminal: stat tracking, no events/commands published
        await _achievementService.IncrementDeathCount(evt.ActorId);

        // If removed, game still works - achievements are optional
    }
}

// ✅ CORRECT: Terminal subscriber (analytics)
public class AnalyticsLogger : INotificationHandler<HealthChangedEvent>
{
    public async Task Handle(HealthChangedEvent evt, CancellationToken ct)
    {
        // Terminal: logging/analytics only
        await _analytics.TrackEvent("health_changed", new {
            actor = evt.ActorId,
            delta = evt.OldHealth - evt.NewHealth
        });
    }
}

// ✅ CORRECT: Terminal subscriber (sound)
public class CombatSoundManager : INotificationHandler<AttackExecutedEvent>
{
    public async Task Handle(AttackExecutedEvent evt, CancellationToken ct)
    {
        // Terminal: play sound effect
        _audioPlayer.PlaySound("attack_hit");

        // If removed, game still works - sound is polish
    }
}

// ❌ WRONG: Non-terminal (cascading events)
public class DeathHandler : INotificationHandler<ActorDiedEvent>
{
    public async Task Handle(ActorDiedEvent evt, CancellationToken ct)
    {
        // ❌ Publishing another event - cascading!
        await _mediator.Publish(new ShowDeathScreenEvent(evt.ActorId));
    }
}

// ❌ WRONG: Non-terminal (orchestration in handler)
public class HealthChangeHandler : INotificationHandler<HealthChangedEvent>
{
    public async Task Handle(HealthChangedEvent evt, CancellationToken ct)
    {
        if (evt.IsDead)
        {
            // ❌ Business logic in event handler
            var loot = _lootService.GenerateLoot(evt.ActorId);

            // ❌ Sending command - orchestration!
            await _mediator.Send(new DropLootCommand(loot));
        }
    }
}

// ❌ WRONG: Non-terminal (business logic)
public class HealthBarNode : EventAwareNode
{
    private void OnHealthChanged(HealthChangedEvent evt)
    {
        UpdateHealthBar(evt.NewHealth);

        if (evt.IsDead)
        {
            // ❌ All of these should be in TakeDamageCommandHandler
            await _mediator.Send(new KillActorCommand(evt.ActorId));
            await _mediator.Publish(new ShowDeathScreenEvent());
            _soundManager.PlayDeathSound();
            _achievementSystem.IncrementDeaths();
        }
    }
}
```

**Why "Terminal Subscriber" is Better Than "UI Updates Only"**:
- ✅ More accurate: captures the essence (no cascading)
- ✅ Allows legitimate patterns: achievements, sound, analytics
- ✅ Still prevents event soup: no events/commands from handlers
- ✅ Testable: "Does removing this break core logic?" (should be NO)

**Enforcement**: Code review question: "Does this handler publish events or send commands?" If yes → REJECT, move logic to command handler.

**Rationale**: The goal is to prevent cascading events and hidden business logic. "Terminal subscriber" precisely captures this: handlers are leaf nodes that do NOT trigger more events or orchestration.

---

### Rule 4: Max Event Depth = 1 (No Cascading)

**Events must be leaf nodes** in the event graph.

```
✅ Allowed Flow (Depth 1):
Button Click → TakeDamageCommand → TakeDamageCommandHandler → HealthChangedEvent → UI Updates (STOP)

❌ Forbidden Flow (Depth 3+):
Button Click → Command → HealthChangedEvent → ActorDiedEvent → DeathAnimationEvent (❌ EVENT SOUP)
```

**Enforcement**:
- All events must be documented in `EVENT_TOPOLOGY.md`
- Code review checks: Does this event handler publish more events?
- If yes → REJECT, move logic to command handler

**Rationale**: Cascading events create hidden control flow. Max depth = 1 makes all flow explicit and traceable.

---

### Rule 5: Document Event Topology

**Every feature must have `EVENT_TOPOLOGY.md`** documenting event relationships.

```markdown
# Event Topology: Health System

## HealthChangedEvent
**Published by**:
- TakeDamageCommandHandler (Health feature)
- HealCommandHandler (Health feature)
- ApplyPoisonCommandHandler (Status feature)

**Subscribers**:
- HealthBarNode (Presentation - UI update only)
- DamagePopupNode (Presentation - show damage number)
- CombatLogNode (Presentation - log entry)

**Publishes other events**: NONE (leaf event) ✅
**Max Depth**: 1 ✅
**Complexity**: O(n) where n = number of actors with health bars
```

**Rationale**: Explicit documentation makes event coupling visible. Easy to audit for violations.

---

## Event Contract Versioning

**Problem**: Event schemas will evolve. How do we add fields without breaking existing subscribers?

**Policy**: Event contracts follow **non-breaking evolution** principles.

### Allowed Changes (Non-Breaking)

✅ **Adding optional fields** (with defaults):
```csharp
// v1.0
public record HealthChangedEvent(
    ActorId ActorId,
    float OldHealth,
    float NewHealth
);

// v1.1 - Added optional field
public record HealthChangedEvent(
    ActorId ActorId,
    float OldHealth,
    float NewHealth,
    float? MaxHealth = null  // ✅ Optional with default
);

// Existing subscribers still work - they ignore new field
```

✅ **Adding new event types** (creates no breaking changes):
```csharp
// New event for more granular notification
public record HealthCriticalEvent(ActorId ActorId, float CurrentHealth);
```

### Forbidden Changes (Breaking)

❌ **Removing fields**:
```csharp
// v1.0
public record HealthChangedEvent(ActorId ActorId, float OldHealth, float NewHealth);

// v2.0 - BREAKING
public record HealthChangedEvent(ActorId ActorId, float NewHealth);  // ❌ Removed OldHealth
// Existing subscribers expecting OldHealth will break!
```

❌ **Renaming fields**:
```csharp
// v1.0
public record HealthChangedEvent(ActorId ActorId, float NewHealth);

// v2.0 - BREAKING
public record HealthChangedEvent(ActorId ActorId, float CurrentHealth);  // ❌ Renamed
// Existing subscribers using NewHealth will break!
```

❌ **Changing field types**:
```csharp
// v1.0
public record HealthChangedEvent(ActorId ActorId, float NewHealth);

// v2.0 - BREAKING
public record HealthChangedEvent(ActorId ActorId, int NewHealth);  // ❌ Changed type
```

### Migration Strategy for Breaking Changes

If a breaking change is unavoidable:

**Option 1: Create new event (preferred)**:
```csharp
// Keep old event for compatibility
public record HealthChangedEvent(ActorId ActorId, float OldHealth, float NewHealth);

// Create new event with new schema
public record HealthChangedEventV2(ActorId ActorId, HealthSnapshot Before, HealthSnapshot After);

// Publish both for transition period
await _mediator.Publish(new HealthChangedEvent(...));  // Old subscribers
await _mediator.Publish(new HealthChangedEventV2(...));  // New subscribers

// Deprecate old event after migration period
```

**Option 2: Adapter pattern**:
```csharp
// Adapter translates new event to old contract for legacy subscribers
public class HealthChangedEventAdapter : INotificationHandler<HealthChangedEventV2>
{
    private readonly IMediator _mediator;

    public async Task Handle(HealthChangedEventV2 evt, CancellationToken ct)
    {
        // Translate v2 → v1 for legacy subscribers
        await _mediator.Publish(new HealthChangedEvent(
            evt.ActorId,
            evt.Before.Current,
            evt.After.Current
        ));
    }
}
```

### Event Naming as Contract

**Event names are contracts** - treat them as API surface:
- ✅ Use semantic, descriptive names (`HealthChangedEvent`, not `Event1`)
- ✅ Past tense indicates completed action (`HealthChanged`, not `HealthChanging`)
- ✅ Include context in name (`ActorHealthChanged` if ambiguous)
- ❌ Don't abbreviate (`HealthChangedEvent`, not `HCEvent`)

### Deprecation Policy

**When deprecating an event**:
1. Mark event with `[Obsolete("Use HealthChangedEventV2 instead")]`
2. Document migration path in EVENT_TOPOLOGY.md
3. Publish both old and new events for at least 2 releases
4. Remove old event only after all subscribers migrated

**Example**:
```csharp
[Obsolete("Use HealthChangedEventV2 instead. Migrate by MM/YYYY. See EVENT_TOPOLOGY.md")]
public record HealthChangedEvent(ActorId ActorId, float NewHealth);
```

### Validation

**Code Review Questions**:
- [ ] Is this change to an event contract non-breaking?
- [ ] If breaking, is there a migration strategy?
- [ ] Are new fields optional with defaults?
- [ ] Is EVENT_TOPOLOGY.md updated with changes?

---

## Godot Threading Constraints (Critical for Presentation Layer)

**Problem**: Godot requires all UI operations to execute on the main thread. Events published from background threads will crash if event handlers directly update UI.

**Impact**: Any `EventAwareNode` that subscribes to events MUST marshal back to the main thread before touching Godot UI.

### The Constraint

**Godot Thread Safety Rules**:
- ✅ **Main thread**: Can do anything (UI updates, scene tree, node operations)
- ❌ **Background threads**: Cannot touch UI, scene tree, or most Godot APIs
- ⚠️ **Event handlers**: May be invoked on background threads (if event published from async code)

### The Solution: CallDeferred

**All UI event handlers in Godot nodes MUST use `CallDeferred`** to ensure execution on main thread:

```csharp
// ❌ WRONG: Direct UI update (crashes if event from background thread)
public partial class HealthBarNode : EventAwareNode
{
    private void OnHealthChanged(HealthChangedEvent evt)
    {
        // This WILL crash if OnHealthChanged called from background thread!
        UpdateHealthBar(evt.NewHealth);
        HealthBar.Value = evt.NewHealth;  // ❌ Accessing Godot property off main thread
    }
}

// ✅ CORRECT: CallDeferred ensures main thread execution
public partial class HealthBarNode : EventAwareNode
{
    private void OnHealthChanged(HealthChangedEvent evt)
    {
        // Marshal to main thread before touching UI
        CallDeferred(nameof(UpdateHealthBarDeferred), evt.NewHealth, evt.IsCritical);
    }

    private void UpdateHealthBarDeferred(float newHealth, bool isCritical)
    {
        // Now safe - guaranteed to be on main thread
        UpdateHealthBar(newHealth);
        HealthBar.Value = newHealth;
        HealthBar.Modulate = isCritical ? Colors.Red : Colors.Green;
    }
}
```

### Event Bridge Pattern (VS_004)

**The GodotEventBus automatically handles thread marshalling**:

```csharp
// In UIEventForwarder (Core → Godot bridge)
public class UIEventForwarder : INotificationHandler<HealthChangedEvent>
{
    private readonly IGodotEventBus _eventBus;

    public async Task Handle(HealthChangedEvent evt, CancellationToken ct)
    {
        // GodotEventBus.Publish internally uses CallDeferred
        _eventBus.Publish("HealthChanged", evt);  // ✅ Safe - marshalled to main thread
    }
}

// In Godot node
public partial class HealthBarNode : EventAwareNode
{
    protected override void OnHealthChanged(HealthChangedEvent evt)
    {
        // This is ALWAYS on main thread (GodotEventBus guarantees it)
        UpdateHealthBar(evt.NewHealth);  // ✅ Safe
    }
}
```

### When CallDeferred Is Required

**Scenarios requiring CallDeferred**:
- ✅ Direct MediatR event handlers in Godot nodes (if subscribed in C#)
- ✅ Any callback from async/await code that touches UI
- ✅ Timer callbacks that update UI
- ✅ Signal handlers from background threads

**Scenarios where CallDeferred is NOT needed**:
- ❌ GodotEventBus subscribers (already handled by bridge)
- ❌ `_Ready()`, `_Process()`, `_PhysicsProcess()` (always main thread)
- ❌ Signal handlers from main thread

### Testing for Thread Safety

**Symptoms of threading violations**:
```
ERROR: Condition "!is_inside_tree()" is true
ERROR: Can't change this state while flushing queries
ERROR: Attempted to add child that is not in the tree
CRASH: Access violation in Node::get_tree()
```

**How to test**:
1. Publish events from `Task.Run(() => _mediator.Publish(...))`
2. Watch for crashes or Godot errors
3. Add thread ID logging: `GD.Print($"Thread: {Thread.CurrentThread.ManagedThreadId}")`

### Rule Enforcement

**Code Review Question**: "Does this Godot node event handler use CallDeferred?"
- If touching Godot UI directly → REJECT unless guaranteed main thread

**Architecture Test** (future TD item):
```csharp
[Fact]
public void GodotNodes_EventHandlers_ShouldUseCallDeferred()
{
    // Scan for EventAwareNode subclasses
    // Check event handler methods for direct Godot property access
    // Warn if CallDeferred not used
}
```

### Summary

| Context | Thread | Solution |
|---------|--------|----------|
| GodotEventBus subscriber | Main (guaranteed) | No CallDeferred needed ✅ |
| Direct MediatR handler in Godot node | Unknown (may be background) | Use CallDeferred ⚠️ |
| `_Process()` / `_Ready()` | Main (guaranteed) | No CallDeferred needed ✅ |
| Async callback touching UI | Background | Use CallDeferred ⚠️ |

**Golden Rule**: When in doubt, use `CallDeferred`. It's a no-op if already on main thread, but prevents crashes if called from background thread.

---

## Dependency Rules

### Within a Feature

```
Feature/Domain/
  ↑
Feature/Application/
  ↑
Feature/Infrastructure/
  ↑
Presentation Layer (Godot)
```

**Rules**:
- Application can depend on Domain
- Infrastructure can depend on Application + Domain
- Presentation can depend on Application interfaces, Domain, and its own feature's Infrastructure
- **Presentation CANNOT depend on other features' internals** (use events/commands instead)
- **Domain cannot depend on anything** (except Domain/Common/)

**Clarification**: "Presentation can depend on everything" was too broad. Tightened to prevent Godot nodes from directly accessing arbitrary infrastructure from other features, which would create hidden coupling.

### Between Features

```
Features/Combat/ → Events → Features/Health/
Features/Health/ → Events → Features/UI/
Features/AI/ → Events → Features/Movement/
```

**Rules**:
- Features communicate **only via events** (no direct references)
- Events are the **contract** between features
- Breaking event contracts = breaking change

### Shared Primitives

```
Domain/Common/Health.cs ← Used by all features
```

**Rules**:
- Domain/Common/ should be **< 10 files**
- Only add types used by **3+ features**
- If only 2 features need it → duplicate it (prefer independence)
- Breaking changes to Domain/Common/ affect **entire codebase** (use carefully)

### Domain/Common/ Governance Process

**Critical**: Changes to `Domain/Common/` have codebase-wide impact. A formal governance process prevents political conflicts and ensures only truly shared primitives enter Common.

**Rule**: Adding or modifying `Domain/Common/` types requires approval from **two core developers from different feature teams**.

#### Adding a New Type to Domain/Common/

**Criteria** (ALL must be met):
1. ✅ Type is used by **3+ features** (or will be within current milestone)
2. ✅ Type is a **primitive** (no dependencies beyond other Common types)
3. ✅ Type is **stable** (breaking changes unlikely in next 6 months)
4. ✅ Duplicating this type across features would cause **significant maintenance burden**

**Process**:
1. Create PR with new type + justification document
2. Tag PR with `domain-common-change` label (for visibility)
3. Require approval from 2+ developers from different feature teams
4. Tech Lead has final veto/approval authority
5. Update Domain/Common/ file count check (must stay < 10)

**Justification Template**:
```markdown
## Adding [TypeName] to Domain/Common/

**Used by features**: [Feature1], [Feature2], [Feature3]
**Dependencies**: [List dependencies or "None"]
**Stability**: [Why this won't change frequently]
**Duplication cost**: [Why duplicating across features is worse]
**Impact**: [How many files will reference this]
```

#### Refactoring from Feature to Common

**When**: When a third feature needs a type currently in a feature folder.

**Process**:
1. Create refactoring task (TD_XXX) with impact analysis
2. Get approval from feature teams that currently use this type
3. Move type to Domain/Common/ in separate PR (not mixed with feature work)
4. Update all references in dependent features
5. Validate Domain/Common/ size < 10 files

**Example**:
```
Initially: Features/Combat/AttackType.cs (only Combat uses it)
Later: Features/AI/ needs AttackType
Decision: Still only 2 features → duplicate it (prefer independence)
Even Later: Features/Achievements/ needs AttackType
Decision: Now 3 features → refactor to Domain/Common/AttackType.cs
```

#### Red Flags (Requires Extra Scrutiny)

**REJECT if**:
- ⚠️ Type has > 5 dependencies (not primitive enough)
- ⚠️ Type changes frequently (breaking changes in last 3 months)
- ⚠️ Type is "just convenient" but not truly needed by 3+ features
- ⚠️ Adding this type would bring Domain/Common/ to 10+ files (need justification for expansion)
- ⚠️ Type contains Godot dependencies (violates ADR-002)

#### Enforcement

**Automated Check** (CI pipeline):

**PowerShell** (Windows/cross-platform):
```powershell
# scripts/validate-domain-common.ps1
$files = (Get-ChildItem 'src/Darklands.Core/Domain/Common' -Filter *.cs -Recurse).Count
if ($files -gt 10) {
    Write-Error "ERROR: Domain/Common/ has $files files (max 10)"
    Write-Error "See ADR-004 governance process for adding types"
    exit 1
}
Write-Host "✓ Domain/Common/ validation passed ($files/10 files)"
```

**Bash** (Linux/macOS):
```bash
# scripts/validate-domain-common.sh
FILES=$(find src/Darklands.Core/Domain/Common -name "*.cs" | wc -l)
if [ $FILES -gt 10 ]; then
    echo "ERROR: Domain/Common/ has $FILES files (max 10)"
    echo "See ADR-004 governance process for adding types"
    exit 1
fi
echo "✓ Domain/Common/ validation passed ($FILES/10 files)"
```

**Code Review Checklist**:
- [ ] PR has `domain-common-change` label?
- [ ] Justification document provided?
- [ ] 2+ reviewers from different feature teams approved?
- [ ] Domain/Common/ still < 10 files after this change?
- [ ] Type is truly primitive (minimal dependencies)?

---

## Feature Communication: Commands vs Events

**Critical Decision**: When should features use Commands vs Events to communicate?

**The Problem**: Developers will ask "Combat needs to damage an actor - do I send `TakeDamageCommand` or publish `AttackExecutedEvent`?" Without clear guidance, teams make inconsistent choices, leading to architectural drift.

### Decision Framework

| Scenario | Pattern | Example | Rationale |
|----------|---------|---------|-----------|
| **Feature A requires Feature B to do something** | Send Command | Combat → `Send(TakeDamageCommand)` → Health | Explicit intent, result needed, synchronous flow |
| **Feature A notifies that something happened** | Publish Event | Health → `Publish(HealthChangedEvent)` → UI/Logs | Broadcast, multiple subscribers, async, fire-and-forget |
| **Operation MUST execute (part of transaction)** | Send Command | Attack → Damage → Death (atomic) | Transaction boundary, all-or-nothing, rollback on failure |
| **Operation is optional/reactive** | Publish Event | Death → Achievement update (optional) | Subscriber may not exist, no impact on core flow |
| **Caller needs result/confirmation** | Send Command | `Result<DamageResult>` from TakeDamageCommand | Caller validates success/failure, handles errors |
| **Caller doesn't care about result** | Publish Event | HealthChangedEvent (fire and forget) | No return value needed, async processing |
| **Multiple independent reactions needed** | Publish Event | HealthChanged → [UI, Logs, Sound, Analytics] | Fan-out to multiple subscribers |
| **Single specific action required** | Send Command | ExecuteAttack → TakeDamage | Direct request-response |

### Rule of Thumb

**Ask yourself**: "Do I **need** this to happen, or am I **notifying** that something happened?"
- **Need it to happen** → Command (request)
- **Notifying it happened** → Event (fact)

### Examples

#### ✅ CORRECT: Combat → Health via Command

**Scenario**: Combat feature needs Health feature to execute damage application

```csharp
// Features/Combat/Application/Commands/ExecuteAttackCommandHandler.cs
public class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand, Result<AttackResult>>
{
    private readonly IMediator _mediator;

    public async Task<Result<AttackResult>> Handle(ExecuteAttackCommand cmd, CancellationToken ct)
    {
        // 1. Calculate damage (Combat's responsibility)
        var damage = CalculateDamage(cmd.AttackerId, cmd.TargetId);

        // 2. Send command to Health feature (synchronous request)
        var damageResult = await _mediator.Send(new TakeDamageCommand(cmd.TargetId, damage));

        // 3. Check result (can handle failure)
        if (damageResult.IsFailure)
            return Result.Failure<AttackResult>($"Attack failed: {damageResult.Error}");

        // 4. Create attack result
        return Result.Success(new AttackResult(cmd.AttackerId, cmd.TargetId, damage));
    }
}
```

**Why Command**: Combat **needs** damage to be applied. It's part of the attack transaction. Combat cares about the result (did it succeed?).

---

#### ✅ CORRECT: Health → UI via Event

**Scenario**: Health changes, UI/logs/sound should update

```csharp
// Features/Health/Application/Commands/TakeDamageCommandHandler.cs
public class TakeDamageCommandHandler : IRequestHandler<TakeDamageCommand, Result<DamageResult>>
{
    private readonly IMediator _mediator;

    public async Task<Result<DamageResult>> Handle(TakeDamageCommand cmd, CancellationToken ct)
    {
        // 1. Apply damage (Health's responsibility)
        var result = await ApplyDamage(cmd.ActorId, cmd.Amount);

        // 2. Publish event (async notification)
        await _mediator.Publish(new HealthChangedEvent(
            ActorId: cmd.ActorId,
            OldHealth: result.OldHealth,
            NewHealth: result.NewHealth,
            IsDead: result.IsDead
        ));

        // 3. Return result (doesn't wait for subscribers)
        return Result.Success(result);
    }
}
```

**Why Event**: Health **notifies** that something happened. Health doesn't care who listens (UI, logs, analytics). Subscribers are optional. Async processing.

---

#### ❌ WRONG: Combat → Health via Event

```csharp
// DON'T DO THIS
public class ExecuteAttackCommandHandler
{
    public async Task<Result> Handle(...)
    {
        var damage = CalculateDamage(...);

        // ❌ Publishing event when we need something done
        await _mediator.Publish(new DamageTargetEvent(targetId, damage));

        // Problem: How do we know if damage was applied?
        // Problem: What if no one is listening?
        // Problem: Can't handle failure - it's fire-and-forget

        return Result.Success();  // This is a lie - we don't know if it succeeded
    }
}
```

**Why Wrong**: Combat **needs** damage to happen. Event is fire-and-forget. Combat can't validate success/failure. If Health feature isn't subscribed, damage silently fails.

---

#### ❌ WRONG: Health → UI via Command

```csharp
// DON'T DO THIS
public class TakeDamageCommandHandler
{
    public async Task<Result> Handle(...)
    {
        var result = await ApplyDamage(...);

        // ❌ Sending command to UI
        await _mediator.Send(new UpdateHealthBarCommand(result.NewHealth));

        return Result.Success(result);
    }
}
```

**Why Wrong**: Health shouldn't know about UI. UI is **one of many** potential subscribers (logs, sound, analytics). Command creates tight coupling. Event allows multiple independent subscribers.

---

### When Both Might Work (Choose Command)

**Scenario**: Combat → Movement (disable movement during attack)

**Option 1: Command** (Preferred)
```csharp
// Combat needs to ensure movement is disabled
var result = await _mediator.Send(new DisableMovementCommand(actorId, duration: 1.0f));
if (result.IsFailure)
    return Result.Failure("Could not disable movement");
```

**Option 2: Event** (Acceptable if movement disable is optional)
```csharp
// Combat notifies that attack started, Movement subscribes
await _mediator.Publish(new AttackStartedEvent(actorId, duration: 1.0f));
```

**Guideline**: If failure to disable movement should **abort the attack**, use Command. If it's just a "nice to have" visual polish, Event is fine.

---

## When to Use Each Pattern

### Use Feature Folder When:
- ✅ Code is specific to one capability (Combat logic only used by Combat)
- ✅ Feature has its own commands/handlers/events
- ✅ < 3 other features need this code

### Use Domain/Common/ When:
- ✅ Type is used by **3+ features** (Health, ActorId, Position)
- ✅ Type is a **primitive** (no dependencies)
- ✅ Breaking changes require coordination across features

### Use Events When:
- ✅ Features need to react to changes (UI updates on health change)
- ✅ Async decoupling is desired (don't wait for UI to update)
- ✅ Multiple subscribers need same notification

### Use Commands When:
- ✅ User action triggers work (button click → damage actor)
- ✅ Orchestration of multiple steps (get actor → validate → apply damage → publish event)
- ✅ Transaction boundaries (all-or-nothing operations)

---

## Examples

### Example 1: Health System (VS_001)

**Structure**:
```
Domain/Common/Health.cs          # Shared primitive (used by 7+ features)

Features/Health/
├── Domain/
│   ├── IHealthComponent.cs
│   └── HealthComponent.cs
├── Application/
│   ├── Commands/TakeDamageCommand.cs
│   └── Events/HealthChangedEvent.cs
└── Infrastructure/
    └── HealthComponentRegistry.cs
```

**Why this structure**:
- `Health.cs` in Common: Used by Combat, Healing, Poison, Regeneration, UI, AI, Achievements
- `HealthComponent.cs` in Features/Health: Only Health feature needs component abstraction
- `TakeDamageCommand`: Orchestrates damage application
- `HealthChangedEvent`: Notifies subscribers (UI, logs)

**Event Flow**:
```
Button Click
→ TakeDamageCommand
→ TakeDamageCommandHandler (orchestrates)
→ HealthChangedEvent (notification)
→ HealthBarNode.OnHealthChanged() (simple UI update)
```

**Event Depth**: 1 ✅
**Complexity**: O(1) ✅
**Testable**: Yes (mock registry, no Godot) ✅

---

### Example 2: Combat Affecting Health (Cross-Feature)

**Structure**:
```
Features/Combat/
└── Application/Commands/ExecuteAttackCommandHandler.cs
    → Sends TakeDamageCommand (crosses feature boundary via command)

Features/Health/
└── Application/Commands/TakeDamageCommandHandler.cs
    → Publishes HealthChangedEvent
```

**Why this works**:
- Combat doesn't directly modify Health (respects feature boundary)
- Combat sends `TakeDamageCommand` (explicit request via MediatR)
- Health feature owns health modification logic
- Events notify interested parties (UI, logs)

**Event Flow**:
```
ExecuteAttackCommand
→ ExecuteAttackCommandHandler
→ Sends TakeDamageCommand (feature boundary crossing)
→ TakeDamageCommandHandler
→ Publishes HealthChangedEvent
→ UI updates
```

**Why this is better than direct calls**:
- ✅ Feature independence (Combat doesn't reference Health internals)
- ✅ Testable (mock MediatR, verify TakeDamageCommand sent)
- ✅ Auditable (all cross-feature calls go through MediatR)

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Event Soup (Cascading Events)

```csharp
// ❌ WRONG: Event triggers more events
public class HealthChangedEventHandler : INotificationHandler<HealthChangedEvent>
{
    public async Task Handle(HealthChangedEvent evt, ...)
    {
        if (evt.IsDead)
            await _mediator.Publish(new ActorDiedEvent(evt.ActorId));  // ❌ Cascade
    }
}

public class ActorDiedEventHandler : INotificationHandler<ActorDiedEvent>
{
    public async Task Handle(ActorDiedEvent evt, ...)
    {
        await _mediator.Publish(new ShowDeathScreenEvent());  // ❌ Another cascade
    }
}
```

**Why this is bad**:
- Hidden control flow (where does death handling start?)
- Impossible to trace (which event caused the bug?)
- Fragile (changing one event breaks others)

**Correct approach**:
```csharp
// ✅ CORRECT: Command orchestrates everything
public class TakeDamageCommandHandler
{
    public async Task<Result> Handle(...)
    {
        var result = await actor.TakeDamage(amount);

        if (result.Value.IsDead)
        {
            // Do ALL death handling here (explicit orchestration)
            _actorRegistry.Remove(actor.Id);
            _achievementSystem.IncrementDeaths();
            _soundManager.PlayDeathSound();
        }

        // Single event with ALL info
        await _mediator.Publish(new HealthChangedEvent(...));

        return Result.Success();
    }
}
```

---

### Anti-Pattern 2: Shared Everything (Domain Bloat)

```csharp
// ❌ WRONG: Everything in Domain/Common/
Domain/Common/
├── ActorId.cs
├── Health.cs
├── Position.cs
├── AttackType.cs          // Only Combat uses this
├── MovementSpeed.cs       // Only Movement uses this
├── InventorySlot.cs       // Only Inventory uses this
└── ... (50 files)
```

**Why this is bad**:
- Breaking changes to Domain/Common/ affect entire codebase
- Defeats purpose of feature independence
- Unclear what's truly "common"

**Correct approach**:
```csharp
// ✅ CORRECT: Only truly shared primitives
Domain/Common/
├── ActorId.cs      # Used by ALL features
├── Health.cs       # Used by 7+ features
└── Position.cs     # Used by 5+ features

Features/Combat/Domain/
└── AttackType.cs   # Combat-specific, keep it there

Features/Movement/Domain/
└── MovementSpeed.cs  # Movement-specific, keep it there
```

**Rule of thumb**: If < 3 features use it, keep it in the feature folder.

---

### Anti-Pattern 3: Feature God Objects

```csharp
// ❌ WRONG: Feature does everything
Features/Combat/
└── Application/
    └── Commands/
        └── ExecuteAttackCommandHandler.cs  (500 lines)
            - Gets attacker
            - Gets target
            - Validates range
            - Calculates damage
            - Applies damage
            - Handles death
            - Updates achievements
            - Plays sounds
            - Shows animations
```

**Why this is bad**:
- Single Responsibility Principle violated
- Hard to test (too many concerns)
- Feature boundaries blurred

**Correct approach**:
```csharp
// ✅ CORRECT: Single responsibility per handler
Features/Combat/Application/Commands/
└── ExecuteAttackCommandHandler.cs  (50 lines)
    - Validates range
    - Calculates damage
    - Sends TakeDamageCommand (delegates to Health feature)
    - Publishes AttackExecutedEvent

Features/Health/Application/Commands/
└── TakeDamageCommandHandler.cs
    - Applies damage
    - Handles death
    - Publishes HealthChangedEvent

Presentation/Nodes/
└── CombatAnimationNode.cs
    - OnAttackExecuted → Play animation
```

**Each handler does ONE thing** (Single Responsibility Principle).

---

## Consequences

### Positive

✅ **Feature Independence**: Easy to find all code for a feature
✅ **Reduced Duplication**: Shared primitives in Domain/Common/
✅ **Testability**: Layer separation enables testing without Godot
✅ **Event Discipline**: Five Rules prevent event soup
✅ **Scalability**: Hundreds of features without namespace collisions
✅ **Debuggability**: Commands + max depth = 1 makes flow traceable
✅ **Intentional Coupling**: Events document feature relationships

### Negative

❌ **Not Pure VSA**: Shared Domain/Common/ violates pure VSA independence
❌ **Not Pure Clean Architecture**: Features cross-cut layers
❌ **Event Overhead**: MediatR + Event Bus adds complexity vs direct calls
❌ **Learning Curve**: Team must understand hybrid approach

### Neutral

➖ **Pragmatic Over Pure**: We chose maintainability over architectural purity
➖ **Game-Specific**: This may not apply to CRUD web apps

---

## Enforcement

### Code Review Checklist

**Feature Organization**:
- [ ] Is this truly a shared primitive? (used by 3+ features?)
- [ ] Is Domain/Common/ still < 10 files?
- [ ] Does each feature have its own Commands/Handlers/Events?

**Event Discipline**:
- [ ] Does this event handler publish more events? (Rule 4 violation)
- [ ] Is this event past-tense? (Rule 2)
- [ ] Is this event handler < 10 lines? (Rule 3)
- [ ] Is EVENT_TOPOLOGY.md updated? (Rule 5)

**Dependency Rules**:
- [ ] Does Domain depend on anything? (should be zero)
- [ ] Do features reference each other directly? (should use events/commands)

### Automated Checks

```bash
# Check Domain/Common/ file count
files=$(find src/Darklands.Core/Domain/Common -name "*.cs" | wc -l)
if [ $files -gt 10 ]; then
    echo "ERROR: Domain/Common/ has $files files (max 10)"
    exit 1
fi

# Check for event handlers publishing events
grep -r "Publish.*Event" Features/*/Application/EventHandlers/ && {
    echo "WARNING: Event handler may be publishing events (Rule 4 violation)"
}
```

### Architecture Tests (TD_001)

```csharp
[Fact]
public void EventHandlers_ShouldNotPublishMoreEvents()
{
    // WHY: Rule 4 - Max event depth = 1
    var eventHandlers = Types.InAssembly(typeof(HealthChangedEvent).Assembly)
        .That().ImplementInterface(typeof(INotificationHandler<>))
        .GetTypes();

    foreach (var handler in eventHandlers)
    {
        var publishCalls = handler.GetMethods()
            .SelectMany(m => m.GetMethodBody()?.GetILAsByteArray() ?? Array.Empty<byte>())
            .Where(Contains_Publish_Opcode);

        publishCalls.Should().BeEmpty(
            $"{handler.Name} publishes events (violates Rule 4)");
    }
}
```

---

## Alternatives Considered

### 1. Pure Vertical Slice Architecture

**Rejected**: Duplicating Health logic across 7 features is unmaintainable.

**Example**:
```
Features/Combat/Health.cs       # Duplicate 1
Features/Healing/Health.cs      # Duplicate 2
Features/Poison/Health.cs       # Duplicate 3
... (7 duplicates)
```

**Problem**: Changing health calculation requires updating 7 files. Guaranteed to diverge.

---

### 2. Pure Clean Architecture (Layered)

**Rejected**: Finding all Health code requires opening 4 folders (Domain/, Application/, Infrastructure/, Presentation/).

**Example**:
```
Domain/Health.cs
Application/HealthService.cs
Application/TakeDamageCommand.cs
Infrastructure/HealthRepository.cs
Presentation/HealthBarNode.cs
```

**Problem**: Feature cohesion is lost. Related code scattered across technical layers.

---

### 3. Unconstrained Event-Driven

**Rejected**: Event soup creates unmaintainable cascading chains.

**Example**:
```
HealthChangedEvent → ActorDiedEvent → ShowDeathScreenEvent → ...
(6 levels deep, impossible to debug)
```

**Problem**: Hidden control flow, fragile cascades, debugging nightmares.

---

## Success Metrics

✅ **Feature isolation**: Can delete a feature folder without breaking others
✅ **Shared primitive count**: Domain/Common/ stays < 10 files
✅ **Event depth**: No events exceed depth = 1
✅ **Governance compliance**: Domain/Common/ changes follow 2-reviewer approval process
✅ **Command/Event clarity**: Developers can articulate why they chose command vs event
✅ **Terminal subscribers**: All event handlers are terminal (no cascading)
✅ **Test coverage**: Core logic testable without Godot
✅ **Build time**: < 5s for incremental builds
✅ **Onboarding time**: New developers find code in < 5 minutes

---

## Future Improvements

### Automated Event Topology Generation

**Current State**: `EVENT_TOPOLOGY.md` is manually maintained per feature.

**Limitation**: Manual maintenance doesn't scale beyond 50-100 events. As the codebase grows to hundreds of features with hundreds of events, manual documentation becomes a maintenance burden and risks becoming stale.

**Future Enhancement**: Use static analysis or reflection to auto-generate event graphs:

**Potential Approaches**:
1. **Roslyn Analyzers** (Compile-Time)
   - Parse MediatR `IRequest<>` and `INotification` registrations
   - Detect `IRequestHandler<>` and `INotificationHandler<>` implementations
   - Build event publisher → subscriber graph
   - Validate Rule 4 (max depth = 1) at compile time
   - Generate compiler warnings for violations

2. **Reflection-Based** (Runtime)
   - Scan assemblies for MediatR handlers at startup
   - Build runtime event dependency graph
   - Export to Graphviz/Mermaid for visualization
   - Integrate with health checks endpoint

3. **Architecture Tests** (Test-Time)
   - Extend TD_001 architecture tests
   - Use `NetArchTest` to enforce event patterns
   - Generate EVENT_TOPOLOGY.md from test results
   - Fail CI if topology has changed without documentation update

**Example Tool**:
```csharp
// scripts/generate-event-topology.ps1
public class EventTopologyGenerator
{
    public async Task<EventGraph> GenerateTopology()
    {
        var handlers = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetInterfaces().Any(IsNotificationHandler));

        var graph = new EventGraph();

        foreach (var handler in handlers)
        {
            var eventType = GetHandledEventType(handler);
            var publishesEvents = AnalyzeHandlerForPublish(handler);

            if (publishesEvents.Any())
            {
                // Rule 4 violation detected!
                throw new ArchitectureViolation($"{handler.Name} publishes events");
            }

            graph.AddSubscriber(eventType, handler);
        }

        return graph;
    }
}
```

**When to Implement**: After 20+ features (estimated 6-12 months), when manual maintenance becomes burden.

**Benefits**:
- ✅ Always up-to-date documentation
- ✅ Automatic Rule 4 enforcement
- ✅ Visual event flow diagrams
- ✅ Impact analysis for event changes

**Deferred Rationale**: Don't over-engineer for theoretical future problems. Manual `EVENT_TOPOLOGY.md` is sufficient for first 20 features. Re-evaluate automation when pain points emerge.

---

## References

- [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/) - Jimmy Bogard
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin
- [Event-Driven Architecture Anti-Patterns](https://codeopinion.com/beware-anti-patterns-in-event-driven-architecture/) - CodeOpinion
- [ADR-001: Clean Architecture Foundation](./ADR-001-clean-architecture-foundation.md)
- [ADR-002: Godot Integration Architecture](./ADR-002-godot-integration-architecture.md)
- [ADR-003: Functional Error Handling](./ADR-003-functional-error-handling.md)

---

## Decision Log

**2025-09-30 (v1.2)**: Added production readiness improvements based on operational review:
- **Godot threading constraints**: Documented CallDeferred requirement for UI event handlers (prevents crashes)
- **Cross-platform CI**: Added PowerShell scripts alongside bash (Windows compatibility)
- **EventHandlers/ folder**: Added to canonical structure for consistency
- **Event versioning policy**: Non-breaking evolution rules (add fields, don't remove/rename)
- **Presentation dependencies**: Tightened from "everything" to "Application + own Infrastructure" (prevents hidden coupling)

**2025-09-30 (v1.1)**: Added critical governance improvements based on expert architectural review:
- Domain/Common/ governance process with 2-reviewer approval requirement
- Command vs Event decision framework with explicit scenarios
- Refined Rule 3 from "UI Updates Only" to "Terminal Subscribers" (more accurate, allows achievements/sound/analytics)
- Future automation considerations for EVENT_TOPOLOGY.md (deferred until 20+ features)

**2025-09-30 (v1.0)**: Initial decision after ultrathink analysis of VSA vs Clean Architecture trade-offs. Feature-Based Clean Architecture with Five Event Rules chosen as pragmatic hybrid for game development context.

---

**Remember**: This is not pure VSA, not pure Clean Architecture, not unconstrained event-driven. It's a **pragmatic hybrid** optimized for **game development** where features share primitives but remain independent.