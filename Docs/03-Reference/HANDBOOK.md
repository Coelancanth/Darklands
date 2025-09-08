# Darklands Developer Handbook

**Last Updated**: 2025-08-29 16:52  
**Purpose**: Single source of truth for daily development - everything you need in one place  
**Based On**: Established HANDBOOK.md patterns (proven approach)

## 🔍 Quick Reference Protocol - Find What You Need FAST

### When You Need Help With...

**🐛 Debugging an Issue:**
1. **DI/namespace errors** → Jump to [Common Bug Patterns](#-common-bug-patterns)
2. **Test failures** → Check [Testing.md](Testing.md) troubleshooting
3. **Build errors** → See [Critical Gotchas](#critical-gotchas)

**💻 Writing Code:**
1. **New feature** → Follow [ADR-002 Phased Implementation](ADR/ADR-002-phased-implementation-protocol.md)
2. **Time-unit combat** → Copy patterns from future `src/Features/Combat/TimeUnit/`
3. **Architecture questions** → [Core Architecture](#-core-architecture)

**🔧 Common Tasks:**
1. **Run tests** → [Quick Commands](#-quick-commands)
2. **Switch personas** → [Persona System](#persona-system)
3. **Create PR** → [Branch Naming](#branch-naming-convention)

**❓ "How Do I...?" Questions:**
- **Use LanguageExt v5?** → [LanguageExt Usage Guide](LanguageExt-Usage-Guide.md) ⭐⭐⭐⭐⭐
- **Handle errors?** → [Error Handling](#-error-handling-with-languageext-v5) - Fin<T> everywhere!
- **Route work?** → [Persona Routing](#-persona-routing)
- **Add DI service?** → [GameStrapper Pattern](#gamestrapper-pattern)

**🚨 Emergency Fixes:**
- **First-time setup** → Run `./scripts/setup/verify-environment.ps1`
- **30+ test failures** → Check DI registration in GameStrapper.cs first
- **Handler not found** → Namespace MUST be `Darklands.Core.*` for MediatR discovery
- **DI container fails** → Check namespace matches pattern
- **Build/test issues** → Run `./scripts/fix/common-issues.ps1`
- **Tests pass but game won't compile** → Use `./scripts/core/build.ps1 test` not `test-only`

## 📍 Navigation

- **Need a term definition?** → [Glossary.md](Glossary.md) - MANDATORY terminology
- **Major architecture decision?** → [ADR Directory](ADR/)
- **Testing guide?** → [Testing.md](Testing.md) (to be created)
- **First-time setup?** → [Development Environment Setup](#-development-environment-setup)
- **Everything else?** → It's in this handbook

---

## 📖 Glossary Enforcement Protocol

**CRITICAL**: [Glossary.md](Glossary.md) is our Single Source of Truth (SSOT) for ALL terminology.

### 🚫 Absolute Requirements

**BEFORE writing ANY code or documentation:**
1. **Check Glossary FIRST** - if unsure what to call something
2. **Use EXACT terms** - no synonyms, no variations
3. **Add missing terms** - update Glossary before use in code

### ⚖️ Enforcement Rules

**For All Personas:**
- ❌ **REJECT** work items using incorrect terminology
- ❌ **BLOCK** PRs with non-Glossary terms in public APIs
- ✅ **REQUIRE** Glossary updates before new terms enter codebase

**Code Reviews Must Check:**
- Class names match Glossary (e.g., `Actor` not `Character`)
- Method names follow conventions (e.g., `NextTurn` not `TurnTime`)
- Public APIs use exact Glossary terminology
- Comments and documentation align with vocabulary

### 🔍 Common Violations to Watch For

| ❌ Wrong Term | ✅ Correct Term | Context |
|-------------|---------------|---------|
| Player | Character | Persistent player avatar |
| Character | Actor | Combat entities (use Actor) |
| Entity | Actor | Game objects that take turns |
| Unit | Actor | Anything that can act |
| Queue | Scheduler | Turn order management |
| Timeline | Scheduler | Combat sequence coordination |
| TurnManager | Scheduler | Combat sequence coordination |
| TurnOrder | Scheduler | Initiative system |
| TurnTime | NextTurn | When actor acts again |
| NextAction | NextTurn | Scheduling property |
| Speed | Agility | Actor attribute |
| Duration | Time Cost | Action requirements |

### 📋 Pre-Coding Checklist

Before implementing ANY feature:
- [ ] Read relevant Glossary sections
- [ ] Verify all planned class names exist in Glossary  
- [ ] Check method names follow conventions
- [ ] Confirm property names match exactly
- [ ] Update Glossary if new terms needed

### 🏗️ Architecture Integration

**The Glossary determines:**
- All public API naming
- Domain model structure  
- Interface definitions
- Command/Query naming patterns
- Event and notification names

**Example Enforcement:**
```csharp
// ✅ CORRECT - follows Glossary
public class ScheduleActorCommand : IRequest<Fin<Unit>>
{
    public Guid ActorId { get; init; }
    public TimeUnit NextTurn { get; init; }
}

// ❌ WRONG - violates Glossary
public class ScheduleEntityCommand : IRequest<Fin<Unit>>  // "Entity" not in Glossary
{
    public Guid CharacterId { get; init; }  // "Character" deprecated
    public TimeUnit TurnTime { get; init; }  // "TurnTime" not allowed
}
```

---

## 🛠️ Development Environment Setup

### Quick Start (< 10 minutes)

**New to Darklands?** Run this one command for automated setup:
```bash
git clone https://github.com/yourusername/darklands.git
cd darklands
./scripts/setup/verify-environment.ps1
```

**Already have the repo?** Just verify your environment:
```bash
./scripts/setup/verify-environment.ps1
```

### Prerequisites

**Required Tools:**
- **.NET SDK 8.0+**: [Download](https://dotnet.microsoft.com/download/dotnet/8.0) or `winget install Microsoft.DotNet.SDK.8`
- **Git**: [Download](https://git-scm.com/) or `winget install Git.Git`
- **Godot 4.4.1**: [Download](https://godotengine.org/download/windows/) (add to PATH)

**Optional but Recommended:**
- **PowerShell 7+**: `winget install Microsoft.PowerShell`
- **GitHub CLI**: `winget install GitHub.cli`
- **Visual Studio Code**: `winget install Microsoft.VisualStudioCode`

### Verification Checklist

After running the setup script, you should see:
```
✅ .NET SDK 8.0+ installed
✅ Git installed and configured
✅ Project structure validated
✅ NuGet packages restored
✅ Git hooks (Husky) installed
✅ Build system working
✅ All tests passing (107/107)
✅ Setup complete - ready for development!
```

### Manual Setup (If Script Fails)

**Install Git Hooks:**
```bash
dotnet tool restore
dotnet husky install
```

**Build and Test:**
```bash
# Core library (fast)
dotnet build src/Darklands.Core.csproj
dotnet test tests/Darklands.Core.Tests.csproj

# Full build including Godot
dotnet build Darklands.csproj
```

**Common Issues:**
- **"dotnet: command not found"** → Install .NET SDK 8.0+
- **"godot: command not found"** → Add Godot to PATH
- **Tests failing** → Run `./scripts/fix/common-issues.ps1`
- **Hooks not working** → Run `chmod +x .husky/*` (Unix/Mac)

### Performance Expectations

After proper setup:
- **Build time**: 2-3 seconds
- **Test time**: < 1.5 seconds (107 tests)
- **Pre-commit hook**: < 0.5 seconds
- **Fresh clone to working**: < 10 minutes

### Environment Verification Script

The `./scripts/setup/verify-environment.ps1` script performs:
1. **Prerequisites check** - .NET, Git, optional tools
2. **Project validation** - Required files and structure
3. **Package restore** - NuGet dependencies
4. **Hook installation** - Git hooks via Husky
5. **Build verification** - Core and test projects
6. **Test execution** - Full test suite
7. **Auto-fixes** - Attempts to fix common issues
8. **Clear instructions** - Exact commands for manual fixes

**Script Features:**
- ✅ **Smart detection** - Checks all requirements
- ✅ **Auto-fix capability** - Installs missing components
- ✅ **Clear feedback** - Shows exactly what's wrong
- ✅ **Zero-friction** - One command setup

---

## 🏗️ Core Architecture

### Clean Architecture + MVP Pattern (ADR-001)
- **Core Layer** (`src/Darklands.Core.csproj`): Pure C# business logic, NO Godot
- **Application Layer**: Commands, Queries, Handlers (MediatR)
- **Godot Integration** (`Darklands.csproj`): Views, Presenters, Scene files
- **Test Layer** (`tests/Darklands.Core.Tests.csproj`): Fast unit tests

### Critical Architecture Rules
1. **Core has ZERO Godot references** - Enables modding without Godot
2. **Everything returns Fin<T>** - No exceptions cross boundaries
3. **DI validates on startup** - Catches wiring errors immediately
4. **Serilog never crashes** - Fallback-safe logging configuration

### Time-Unit Combat System (Core Feature)
```
Every action has a time cost in units:
- Move 1 tile: 100 units
- Quick dagger: 50 units
- Heavy sword: 150 units
- Reload crossbow: 200 units

Faster units/actions get more "turns"
```

### Grid Coordinate System
```
Y↑
3 □ □ □ □
2 □ □ □ □  
1 □ □ □ □
0 □ □ □ □
  0 1 2 3 → X

- Origin: (0,0) at bottom-left
- X increases rightward, Y increases upward
- Godot aligned: Y+ is up
```

### GameStrapper Pattern
**CRITICAL**: Follow established GameStrapper.cs pattern (468 lines)
- Full DI container setup with validation
- Fallback-safe Serilog configuration
- Service lifetime management
- MediatR pipeline registration

---

## 🚨 Error Handling with LanguageExt v5

### ADR-008: Functional Error Handling (CRITICAL)

**We use LanguageExt v5.0.0-beta-54** with strict functional error handling:

#### ❌ FORBIDDEN in Domain/Application/Presentation
```csharp
// NEVER DO THIS - Anti-pattern!
try {
    var result = DoSomething();
    return result;
} catch (Exception ex) {
    _logger.Error(ex, "Failed");
    throw;  // or return default
}
```

#### ✅ REQUIRED Pattern
```csharp
// Domain Layer - Pure functions
public static Fin<Position> Move(Position from, Direction dir) =>
    IsValidMove(from, dir)
        ? Pure(from.Move(dir))
        : Fail(Error.New($"Invalid move from {from} to {dir}"));

// Application Layer - Orchestration  
public Task<Fin<Unit>> Handle(MoveCommand cmd, CancellationToken ct) =>
    from actor in GetActor(cmd.ActorId)
    from newPos in Move(actor.Position, cmd.Direction)
    from _ in UpdatePosition(cmd.ActorId, newPos)
    select unit;

// Presentation Layer - UI handling
await ProcessMove(position).Match(
    Succ: _ => View.ShowSuccess("Moved!"),
    Fail: error => View.ShowError(error.Message)
);
```

### Key v5 Breaking Changes
- **Try<T> REMOVED** → Use `Eff<T>`
- **TryAsync<T> REMOVED** → Use `IO<T>`
- **Result<T> REMOVED** → Use `Fin<T>`
- **EitherAsync<L,R> REMOVED** → Use `EitherT<L, IO, R>`

### When to Use Each Type
| Scenario | Type | Example |
|----------|------|---------|
| Can fail | `Fin<T>` | `Fin<Grid> LoadGrid()` |
| Might not exist | `Option<T>` | `Option<Actor> FindActor(id)` |
| Multiple errors | `Validation<Error, T>` | Form validation |
| Side effects | `Eff<T>` | Database operations |
| Async I/O | `IO<T>` | File/network operations |

### Infrastructure Boundaries (ONLY place for try/catch)
```csharp
// ONLY at true system boundaries
public override void _Ready() {
    try {
        InitializeGame();  // Godot entry point
    } catch (Exception ex) {
        GD.PrintErr($"Fatal: {ex}");  // Last resort
    }
}
```

### Essential Imports
```csharp
using LanguageExt;
using static LanguageExt.Prelude;  // Pure, Fail, Some, None, etc.
```

### Error Creation
```csharp
// Business errors (expected)
Error.New("User not found");
Error.New(404, "Resource not found");

// System errors (exceptional)
Error.New(new InvalidOperationException("Unexpected"));

// Multiple errors
Error.Many(error1, error2);
// or
var combined = error1 + error2;
```

**Full Guide**: [LanguageExt-Usage-Guide.md](LanguageExt-Usage-Guide.md)  
**Architecture Decision**: [ADR-008](ADR/ADR-008-functional-error-handling.md)

---

## 🎯 Phased Implementation Protocol (ADR-002)

### MANDATORY for ALL Features

#### Phase 1: Domain Model
- Pure C# business logic, zero dependencies
- Comprehensive unit tests
- **GATE**: 100% tests passing, >80% coverage
- **Speed**: <100ms
- **Commit**: `feat(combat): domain model [Phase 1/4]`

#### Phase 2: Application Layer
- CQRS commands and handlers
- Fin<T> error handling
- Mocked repositories only
- **GATE**: All handler tests passing
- **Speed**: <500ms
- **Commit**: `feat(combat): command handlers [Phase 2/4]`

#### Phase 3: Infrastructure
- Real services and state management
- Integration tests
- **GATE**: Integration tests passing
- **Speed**: <2s
- **Commit**: `feat(combat): infrastructure [Phase 3/4]`

#### Phase 4: Presentation
- MVP presenters and view interfaces
- Godot UI wiring
- **GATE**: Manual testing in editor
- **Commit**: `feat(combat): presentation [Phase 4/4]`

### Phase Rules
1. **HARD GATE**: Cannot proceed until current phase GREEN
2. **NO SHORTCUTS**: Even "simple" features follow all phases
3. **DOCUMENTATION**: Each phase in commit message
4. **REVIEW**: Tech Lead validates phase completion

---

## 💻 Development Workflow

### Branch Naming Convention
- **Feature**: `feat/VS_001-description`
- **Bug Fix**: `fix/BR_001-description`
- **Tech Debt**: `tech/TD_001-description`
- **Use underscores**: `VS_001` not `vs-001` (matches Backlog.md)

### Persona System
- **Architecture**: Single repository with context management
- **Switching**: `/clear` → `embody [persona]`
- **Script**: `./scripts/persona/embody.ps1 [persona]`
- **Context**: `.claude/memory-bank/active/[persona].md`
- **Workflow**: Sequential solo dev - commit before switching

### Quick Commands

```bash
# Setup and verification
./scripts/setup/verify-environment.ps1     # Full environment check
./scripts/fix/common-issues.ps1           # Auto-fix common problems
./scripts/core/build.ps1 test             # Build + test (safe to commit)

# Build and test
dotnet build src/Darklands.Core.csproj    # Core only (fast)
dotnet test tests/Darklands.Core.Tests.csproj  # Unit tests
dotnet build Darklands.csproj              # Full with Godot

# Test by phase (future)
dotnet test --filter "Category=Domain"
dotnet test --filter "Category=Application"
dotnet test --filter "Category=Integration"
```

---

## 🎯 Persona Routing

### Quick Decision Matrix

| Work Type | Goes To | Key Responsibility | Don't Send To |
|-----------|---------|-------------------|---------------|
| New Features | Product Owner | Creates VS items | Tech Lead (until defined) |
| Technical Planning | Tech Lead | Breaks down, approves TD | Dev Engineer (direct) |
| Implementation | Dev Engineer | Builds features | Test Specialist (too early) |
| Testing | Test Specialist | Creates tests, finds bugs | Dev Engineer (for strategy) |
| Complex Bugs (>30min) | Debugger Expert | Deep investigation | Dev Engineer (timebox exceeded) |
| CI/CD & Tools | DevOps Engineer | Automation, infrastructure | Dev Engineer (for scripts) |

### Work Item Types
- **VS_xxx**: Vertical Slice (new feature) - Product Owner creates
- **BR_xxx**: Bug Report (defect) - Test Specialist creates
- **TD_xxx**: Technical Debt (improvement) - Tech Lead approves

### Handoff Triggers

| From | To | When |
|------|-----|------|
| Product Owner | Tech Lead | VS item fully defined |
| Tech Lead | Dev Engineer | Technical approach documented |
| Dev Engineer | Test Specialist | Implementation complete |
| Dev Engineer | Debugger Expert | 30min debugging exceeded |
| Any Persona | Product Owner | Requirements unclear |

---

## 🚨 MANDATORY: LanguageExt Error Handling Protocol

### The Golden Rules

**NEVER use try/catch for business logic!** Use LanguageExt patterns exclusively.

#### Rule 1: Business Logic → LanguageExt
```csharp
// ✅ CORRECT - All business operations return Fin<T>
public Fin<Player> MovePlayer(Position from, Position to) =>
    from valid in ValidateMove(from, to)
    from updated in UpdatePosition(valid.player, to)
    from events in TriggerMoveEvents(updated)
    select events.player;

// ❌ WRONG - Never throw for business logic
public void MovePlayer(Position from, Position to) {
    if (!IsValidMove(from, to)) 
        throw new InvalidMoveException(); // NO!
}
```

#### Rule 2: Infrastructure Only → try/catch
```csharp
// ✅ CORRECT - Infrastructure concerns only
private Fin<ServiceProvider> BuildServiceProvider() {
    try {
        return FinSucc(services.BuildServiceProvider());
    } catch (Exception ex) {
        return FinFail<ServiceProvider>(Error.New("DI setup failed", ex));
    }
}

// ❌ WRONG - Presenter logic should use Fin<T>
private void OnClick(Position pos) {
    try {
        _mediator.Send(new MoveCommand(pos)); // Should return Fin<T>!
    } catch (Exception ex) {
        _logger.Error(ex, "Move failed");
    }
}
```

#### Rule 3: Always Chain with Bind/Match
```csharp
// ✅ CORRECT - Functional composition
public async Task HandleMoveAsync(MovePlayerCommand cmd) {
    var result = await _gameService.MovePlayer(cmd.From, cmd.To);
    result.Match(
        Succ: move => _logger.Information("Player moved to {Position}", move.NewPosition),
        Fail: error => _logger.Warning("Move failed: {Error}", error.Message)
    );
}

// ❌ WRONG - Not handling failure case
public async Task HandleMoveAsync(MovePlayerCommand cmd) {
    var result = await _gameService.MovePlayer(cmd.From, cmd.To);
    var move = result.ThrowIfFail(); // NO! Never throw
}
```

### Current Codebase Issues TO FIX

**FOUND IN AUDIT - These must be converted:**

1. **TimeUnitCalculator.cs:247** - Domain logic using try/catch
2. **All Presenter classes** - UI interaction using try/catch  
3. **Any new code** - Must use LanguageExt patterns

### Conversion Examples

**Before (WRONG):**
```csharp
// ❌ Current presenter pattern - MUST CHANGE
private void OnTileClick(Position position) {
    try {
        _mediator.Send(new MovePlayerCommand(position));
        _logger.Information("Move attempted");
    } catch (Exception ex) {
        _logger.Error(ex, "Move failed");
    }
}
```

**After (CORRECT):**
```csharp
// ✅ Proper LanguageExt pattern
private async Task OnTileClick(Position position) {
    var result = await _mediator.Send(new MovePlayerCommand(position));
    result.Match(
        Succ: move => {
            _logger.Information("Player moved to {Position}", move.NewPosition);
            RefreshUI(move);
        },
        Fail: error => _logger.Warning("Move failed: {Error}", error.Message)
    );
}
```

### When try/catch IS Allowed

**Infrastructure/System Concerns ONLY:**
- DI container setup (GameStrapper.cs) ✅
- Logger configuration ✅  
- File system operations ✅
- Third-party library exceptions you can't control ✅

**NEVER for:**
- Validation errors ❌
- Business rule violations ❌
- Expected domain failures ❌
- UI interaction failures ❌
- Network/database operations ❌ (use Fin<T>)

## 🔨 Extracted Production Patterns

### Pattern: Value Object Factory with Validation (VS_001)
**Problem**: Public constructors allow invalid state (`new TimeUnit(-999)`)
**Solution**: Private constructor + validated factory methods
```csharp
// Implementation pattern from VS_001
public record TimeUnit {
    private TimeUnit(int value) { Value = value; }
    
    public static Fin<TimeUnit> Create(int value) {
        if (value < 0 || value > 10000) 
            return FinFail<TimeUnit>(Error.New($"TimeUnit must be 0-10000, got {value}"));
        return FinSucc(new TimeUnit(value));
    }
    
    // For known-valid compile-time values only
    public static TimeUnit CreateUnsafe(int value) => new(value);
}
```
**Key**: Impossible to create invalid instances in production

### Pattern: Thread-Safe DI Container (VS_001)
**Problem**: Static fields cause race conditions during initialization
**Solution**: Double-checked locking pattern
```csharp
// Implementation from GameStrapper.cs fix
private static volatile IServiceProvider? _instance;
private static readonly object _lock = new();

public static IServiceProvider Instance {
    get {
        if (_instance == null) {
            lock (_lock) {
                if (_instance == null) {
                    _instance = BuildServiceProvider();
                }
            }
        }
        return _instance;
    }
}
```
**Key**: Thread-safe singleton without performance penalty

### Pattern: Cross-Presenter Coordination (VS_010a)
**Problem**: Isolated presenters can't coordinate cross-cutting features (health bars)
**Solution**: Explicit presenter coordination via setter injection
```csharp
// Implementation from VS_010a health system
public class ActorPresenter : PresenterBase<IActorView> {
    private HealthPresenter? _healthPresenter;
    
    // Coordination injection
    public void SetHealthPresenter(HealthPresenter healthPresenter) {
        _healthPresenter = healthPresenter;
    }
    
    private async Task CreateActor(ActorId id, Position position) {
        var actor = await _actorService.CreateActor(id);
        await _view.DisplayActor(id, position);
        
        // Coordinate with health presenter
        _healthPresenter?.HandleActorCreated(id, actor.Health);
    }
}
```
**Key**: Cross-cutting features need explicit presenter coordination

### Pattern: Optional Feedback Service (VS_010b)
**Problem**: Presentation layer feedback without violating Clean Architecture
**Solution**: Optional service injection in handlers
```csharp
// From VS_010b combat system
public interface IAttackFeedbackService {
    void OnAttackSuccess(ActorId attacker, ActorId target, int damage);
    void OnAttackFailed(ActorId attacker, string reason);
}

public class ExecuteAttackCommandHandler {
    private readonly IAttackFeedbackService? _feedbackService;
    
    public ExecuteAttackCommandHandler(
        IAttackFeedbackService? feedbackService = null) {
        _feedbackService = feedbackService; // Null in tests, real in production
    }
    
    public Fin<Unit> Handle(ExecuteAttackCommand cmd) {
        // Business logic...
        _feedbackService?.OnAttackSuccess(attacker, target, damage);
        return FinSucc(Unit.Default);
    }
}
```
**Key**: Optional injection maintains testability while enabling rich UI

### Pattern: Death Cascade Coordination (VS_010b)
**Problem**: Actor death requires coordinated cleanup across multiple systems
**Solution**: Ordered removal from all systems
```csharp
// Death cascade order matters!
private void HandleActorDeath(ActorId actorId) {
    // 1. Remove from scheduler (no more turns)
    _scheduler.RemoveActor(actorId);
    
    // 2. Remove from grid (free position)
    _gridService.RemoveActor(actorId);
    
    // 3. Remove from scene (visual cleanup)
    _view.RemoveActor(actorId);
    
    // 4. Remove from state (memory cleanup)
    _actorService.RemoveActor(actorId);
}
```
**Key**: Order prevents orphaned state and null references

### Pattern: Godot Node Lifecycle (VS_010a)
**Problem**: Node initialization in constructor fails - tree not ready
**Solution**: Always use _Ready() for node initialization
```csharp
// WRONG - Constructor too early
public partial class HealthView : Node2D {
    public HealthView() {
        _healthBar = GetNode<ProgressBar>("HealthBar"); // CRASH!
    }
}

// CORRECT - _Ready() when tree ready
public partial class HealthView : Node2D {
    private ProgressBar? _healthBar;
    
    public override void _Ready() {
        _healthBar = GetNode<ProgressBar>("HealthBar"); // Safe
        _label = GetNode<Label>("HealthLabel");
    }
}
```
**Key**: Godot node tree isn't ready until _Ready() is called

### Pattern: Queue-Based CallDeferred (TD_011)
**Problem**: Shared fields cause race conditions in Godot UI updates
**Solution**: Queue-based deferred processing
```csharp
// Implementation from ActorView.cs fix
private readonly Queue<ActorCreationData> _pendingCreations = new();
private readonly object _queueLock = new();

public void DisplayActor(ActorId id, Position pos) {
    lock (_queueLock) {
        _pendingCreations.Enqueue(new(id, pos));
    }
    CallDeferred(nameof(ProcessPendingCreations));
}

private void ProcessPendingCreations() {
    lock (_queueLock) {
        while (_pendingCreations.Count > 0) {
            var data = _pendingCreations.Dequeue();
            // Safe UI update on main thread
        }
    }
}
```
**Key**: Thread-safe UI updates without races

## 🧪 Testing Patterns

### Property-Based Testing (VS_001)
**When**: Mathematical operations, invariants, determinism
**Tool**: FsCheck
```csharp
[Property]
public Property TimeUnitCalculation_ShouldBeDeterministic() {
    return Prop.ForAll<int, int, int>(
        (baseTime, agility, encumbrance) => {
            var result1 = Calculate(baseTime, agility, encumbrance);
            var result2 = Calculate(baseTime, agility, encumbrance);
            return result1 == result2;  // Always identical
        }
    ).QuickCheckThrowOnFailure();
}
```
**Key**: 1000+ iterations prove mathematical correctness

### LanguageExt Testing Patterns

#### Testing Fin<T> Results
```csharp
// ✅ Test success
result.IsSucc.Should().BeTrue();
result.IfSucc(value => value.TimeUnits.Should().Be(expected));

// ✅ Test failure
result.IsFail.Should().BeTrue();
result.IfFail(error => error.Message.Should().Contain("invalid"));

// ✅ Pattern matching
result.Match(
    Succ: value => value.Damage.Should().BeGreaterThan(0),
    Fail: error => Assert.Fail($"Expected success: {error}")
);
```

#### Testing Option<T>
```csharp
option.IsSome.Should().BeTrue();
option.IfSome(combatant => {
    combatant.Health.Should().BeGreaterThan(0);
    combatant.TimeUnits.Should().BeLessThan(1000);
});
```

**Key Rule**: Everything returns `Fin<T>` - no exceptions thrown

---

## 🔴 The Simplicity Principle (MOST CRITICAL)

**Before writing ANY code, ask**: "Can I add one condition to existing code?"
- **Red flag**: Solution > 100 lines for a "simple" feature
- **Example**: Merge pattern needed 5 lines, not 369 lines of new recognizer
- **Enforcement**: Estimate LOC before coding, >100 = mandatory design review
- **Metric**: 257 lines of focused code vs 500+ with new abstractions

## 📋 Pre-Coding Checklist (MANDATORY)

Before writing ANY code:
- [ ] **Check Glossary.md** for correct terminology
- [ ] **Question arbitrary requirements** (e.g., "exactly 3" - why not 3+?)
- [ ] **Look for existing patterns** to reuse
- [ ] **Estimate lines of code** (>100 = review first)
- [ ] **Trace data flow** for new fields (Domain → Effect → Notification → View)

## 🚫 Anti-Patterns to Avoid

### 🚨 ROOT CAUSE #1: Convenience Over Correctness
**The most dangerous anti-pattern - choosing easy over correct**
```csharp
// ❌ WRONG - Float math for convenience → non-determinism
var modifier = 100.0 / agility;  // FLOAT! Platform inconsistencies!
var final = Math.Round(baseTime * modifier);  // Rounding varies!

// ✅ CORRECT - Integer arithmetic for determinism
var final = (baseTime * 100 * (10 + encumbrance)) / (agility * 10);
```

```csharp
// ❌ WRONG - Async because it's "modern" → race conditions  
Task.Run(async () => await View.DisplayActorAsync(...));  // Actor 1
Task.Run(async () => await View.DisplayActorAsync(...));  // Actor 2 overwrites!

// ✅ CORRECT - Sequential for turn-based games
scheduler.GetNextActor();
ProcessAction();
UpdateUI();  // One at a time, no races
```

### 🚨 ROOT CAUSE #2: Duplicate State Sources
**Violating Single Source of Truth creates sync nightmares**
```csharp
// ❌ WRONG - Position stored in 3 places!
public class Actor {
    public Position Position { get; set; }  // Duplicate #1
}
public class GridService {
    Dictionary<ActorId, Position> _positions;  // Duplicate #2
}
public class ActorService {
    Dictionary<ActorId, Actor> _actors;  // Contains #1!
}

// ✅ CORRECT - SSOT Architecture
public class Actor {
    // NO position - just health/stats
}
public class GridService {
    // ONLY source for positions
    Dictionary<ActorId, Position> _positions;
}
```

### 🚨 ROOT CAUSE #3: Architecture/Domain Mismatch
**Using patterns that fight the problem domain**
```csharp
// ❌ WRONG - Async patterns in sequential domain
public async Task<Fin<Result>> ProcessTurnAsync() {
    await Task.Run(() => ...);  // Why async for turn-based?
}

// ✅ CORRECT - Match pattern to domain
public Fin<Result> ProcessTurn() {
    // Turn-based = sequential = synchronous
    return ExecuteAction();
}
```

### ❌ Direct Godot Access from Domain
```csharp
// WRONG - Never reference Godot in Core
public class CombatService {
    private Node2D _sprite; // NO!
}

// RIGHT - Use interfaces
public class CombatService {
    private readonly ICombatView _view;
}
```

### ❌ Skipping Phased Implementation
```csharp
// WRONG - Building UI first
public partial class CombatUI : Control {
    // Starting with UI before domain model
}

// RIGHT - Start with domain
public record TimeUnit(int Value);
public static class TimeUnitCalculator { }
```

### ❌ Exceptions Instead of Fin<T>
```csharp
// WRONG - Throwing exceptions
if (invalid) throw new ArgumentException();

// RIGHT - Return Fin<T>
if (invalid) return FinFail<Result>(Error.New("Invalid"));
```

### ❌ Creating Files Without Need
- Never create documentation proactively
- Always prefer editing existing files
- Only create files when explicitly required

### ❌ GUID Stability Issues
```csharp
// DANGEROUS - New GUID each access
public Guid Id => RequestedId ?? Guid.NewGuid();

// SAFE - Cached stable value
private readonly Lazy<Guid> _id = new(() => Guid.NewGuid());
public Guid Id => RequestedId ?? _id.Value;
```

### ❌ Missing Validation Before State Change
```csharp
// WRONG - Direct state change
await _gridService.RemoveBlock(id);

// RIGHT - Validate first
var validation = await _validator.ValidateRemoval(id);
if (validation.IsFail) return validation.Error;
await _gridService.RemoveBlock(id);
```

---

## 📚 Technical Debt Patterns

### Pattern: Documentation as Code (TD_001)
**Problem**: Separate setup docs drift from reality
**Solution**: Integrate into daily reference (HANDBOOK.md)
- Single source of truth
- Used daily = stays current
- <10 minute setup achieved

### Pattern: Glossary SSOT Enforcement (TD_002)
**Problem**: Inconsistent terminology causes bugs
**Solution**: Strict glossary adherence
- Even small fixes matter ("combatant" → "Actor")
- Prevents confusion at scale
- Enforced in code reviews

### Pattern: Interface-First Design (TD_003)
**Problem**: Implementation before contract
**Solution**: Define interfaces before coding
```csharp
// Define contract first
public interface ISchedulable {
    ActorId Id { get; }
    Position Position { get; }
    TimeUnit NextTurn { get; }
}
// Then implement...
```

### Pattern: Grid System Design (VS_005)
**Problem**: Need efficient grid storage and pathfinding
**Solution**: 1D array with row-major ordering
```csharp
// From VS_005 implementation
public record Grid {
    private readonly Tile[] _tiles; // 1D array for cache efficiency
    
    private int GetIndex(Position pos) => pos.Y * Width + pos.X;
    
    public Fin<Tile> GetTile(Position pos) {
        if (!IsInBounds(pos)) return FinFail<Tile>(Error.New("Out of bounds"));
        return FinSucc(_tiles[GetIndex(pos)]);
    }
}
```
**Key**: 1D arrays are faster than 2D, row-major ordering for cache locality

### Pattern: CQRS with Auto-Discovery (VS_006)
**Problem**: Clean command/query separation with automatic handler registration
**Solution**: MediatR with namespace-based discovery
```csharp
// Commands return Fin<Unit>, Queries return Fin<T>
public class MoveActorCommand : IRequest<Fin<Unit>> { }
public class GetGridStateQuery : IRequest<Fin<GridState>> { }

// Handler auto-discovered by namespace
namespace Darklands.Core.Application.Grid.Commands {
    public class MoveActorCommandHandler : IRequestHandler<MoveActorCommand, Fin<Unit>> { }
}
```
**Key**: Namespace MUST be Darklands.Core.* for auto-discovery

### Pattern: List vs SortedSet for Scheduling (VS_002)
**Problem**: Need priority queue that allows duplicates
**Solution**: List with binary search insertion
```csharp
// From VS_002 scheduler
public class CombatScheduler {
    private readonly List<ISchedulable> _timeline = new();
    
    public void Schedule(ISchedulable entity) {
        var index = _timeline.BinarySearch(entity, _comparer);
        if (index < 0) index = ~index;
        _timeline.Insert(index, entity); // Allows duplicates!
    }
}
```
**Key**: SortedSet prevents duplicates, List allows rescheduling

### Pattern: Composite Query Service (TD_009)
**Problem**: Need data from multiple services
**Solution**: Composite service queries both
```csharp
// From TD_009 SSOT refactor
public class CombatQueryService : ICombatQueryService {
    public Fin<CombatView> GetCombatView(ActorId id) {
        var actorResult = _actorService.GetActor(id);
        var positionResult = _gridService.GetPosition(id);
        
        return actorResult.Bind(actor =>
            positionResult.Map(pos => 
                new CombatView(actor, pos)));
    }
}
```
**Key**: Each service owns its domain, composite combines

### Pattern: Library Migration Strategy (TD_004)
**Problem**: Breaking changes block builds
**Solution**: Systematic migration approach
```csharp
// LanguageExt v4 → v5 patterns
Error.New(code, msg) → Error.New($"{code}: {msg}")
.ToSeq() → Seq(collection.AsEnumerable())
Seq1(x) → [x]
```
**Process**: Fix compilation → Test → Refactor patterns

## 🔥 Common Bug Patterns

### 🎯 Pattern: Integer-Only Arithmetic for Determinism
**When**: Any game system requiring reproducible behavior
**Why**: Float math causes platform inconsistencies, save/load desyncs
```csharp
// ✅ CORRECT Integer Pattern (from BR_001 fix)
public static int CalculateTimeUnits(int baseTime, int agility, int encumbrance) {
    // Scale by 100 for precision, round at boundaries
    var numerator = baseTime * 100 * (10 + encumbrance);
    var denominator = agility * 10;
    return (numerator + denominator/2) / denominator;  // Integer division with rounding
}
```
**Key**: Multiply by powers of 10, do math, divide back down

### 🎯 Pattern: SSOT Service Architecture  
**When**: Multiple services need same data
**Why**: Prevents state synchronization bugs
```csharp
// ✅ CORRECT SSOT Pattern (from TD_009 fix)
public interface IGridStateService {
    Fin<Position> GetActorPosition(ActorId id);  // ONLY source for positions
}
public interface IActorStateService {
    Fin<Actor> GetActor(ActorId id);  // ONLY source for actor stats
}
public interface ICombatQueryService {
    Fin<CombatView> GetCombatView(ActorId id);  // Composes from both
}
```
**Key**: Each service owns specific domain, composite services query both

### 🎯 Pattern: Sequential Turn Processing
**When**: Turn-based game mechanics
**Why**: Async creates race conditions in inherently sequential systems
```csharp
// ✅ CORRECT Sequential Pattern (from TD_011 fix)
public class GameLoopCoordinator {
    public void ProcessTurn() {
        var actor = _scheduler.GetNextActor();      // Step 1
        var action = GetPlayerAction(actor);        // Step 2
        ExecuteAction(actor, action);               // Step 3
        UpdateUI();                                  // Step 4
        // ONE actor, ONE action, ONE update - NO concurrency
    }
}
```
**Key**: Complete one actor fully before starting next

### Namespace Mismatch Breaking MediatR Discovery (CRITICAL)
```csharp
// ❌ WRONG - Silent failure, handler won't be discovered
namespace Darklands.Features.Combat
public class AttackHandler : IRequestHandler<AttackCommand, Fin<Result>>

// ✅ CORRECT - Will be auto-discovered by MediatR
namespace Darklands.Core.Features.Combat
public class AttackHandler : IRequestHandler<AttackCommand, Fin<Result>>
```
**Impact**: Handlers outside `Darklands.Core.*` namespace are invisible to MediatR
**Symptom**: 30+ test failures from single namespace error

### Notification Layer Completeness
```csharp
// ❌ WRONG - Updates model but view never knows
stateService.UpdateCombat(position);

// ✅ CORRECT - Model update + view notification
stateService.UpdateCombat(position);
_mediator.Publish(new CombatStateChangedNotification(position, DateTime.UtcNow));
```
**Impact**: Without notifications, UI becomes disconnected from game state

### All Entry Points Must Trigger Mechanics
```csharp
// ❌ WRONG - Only handles one trigger
public class ProcessAfterAttackHandler : INotificationHandler<AttackCompletedNotification>

// ✅ CORRECT - Need handlers for ALL triggers
public class ProcessAfterAttackHandler : INotificationHandler<AttackCompletedNotification>
public class ProcessAfterMoveHandler : INotificationHandler<MoveCompletedNotification>
public class ProcessAfterSpellHandler : INotificationHandler<SpellCompletedNotification>
```

### DI Container Issues
**Symptom**: "Unable to resolve service"  
**Fix**: Check registration in GameStrapper.cs, verify namespace

### MediatR Handler Not Found
**Symptom**: "No handler registered"  
**Fix**: Namespace must be `Darklands.Core.*`, check assembly scanning

### Godot Compilation Errors
**Symptom**: Build fails with Godot errors  
**Fix**: Ensure `<Compile Remove="src\**" />` in Darklands.csproj

### Modern C# Required Properties
**Issue**: Assuming constructor parameters
**Fix**: Use `required` with `init` properties
```csharp
public class CombatState {
    public required string Name { get; init; }
    public required int TimeUnits { get; init; }
}
```

### Test Framework Detection
**Issue**: Using wrong mocking library
**Fix**: Run `grep -r "using Moq" tests/` first to verify

---

## 📐 Critical Gotchas

1. **Namespace = Class Name**: Causes resolution hell
2. **Missing DI Registration**: ValidateOnBuild catches this
3. **Wrong Fin<T> Pattern**: Always use Match or IfSucc/IfFail
4. **Godot in Core**: Breaks modding, fails architecture tests
5. **Skipping Phases**: Creates integration nightmares

---

## 🚀 Quick Start for New Features

1. **Product Owner**: Create VS_xxx in Backlog.md
2. **Tech Lead**: Break down into phases, approve approach
3. **Dev Engineer**: 
   - Phase 1: Domain model + tests
   - Phase 2: Commands/handlers + tests
   - Phase 3: Services + integration tests
   - Phase 4: UI/presenters + manual test
4. **Test Specialist**: Validate implementation
5. **All**: Follow commit convention `[Phase X/4]`

---

## ✅ Trust but Verify Protocol (10-Second Rule)

When subagents complete work, perform quick checks:

```bash
# File modified?
git status | grep HANDBOOK.md

# Content added?
grep "TD_001" Backlog.md

# Status changed?
grep "Status: Completed" Backlog.md
```

**Common False Completions**:
- Partial updates (some changes missed)
- Wrong location (added to wrong section)
- Format issues (broken template)

## 🎯 Git Hooks for Safety (CRITICAL)

### Required Hooks (Standard .husky/ configuration)

1. **pre-commit**: Educational guidance
   - Atomic commit reminder
   - Branch alignment check (VS_001 on branch = VS_001 in commit)
   - Work type consistency (feat commits on feat branches)

2. **commit-msg**: Format enforcement
   - Conventional commits required
   - Work item tracking in scope
   - 72 character limit

3. **pre-push**: Protection layers
   - **BLOCKS** direct push to main
   - Runs build and tests
   - **Memory Bank reminder** (critical for personas)
   - Branch freshness check

### Installation
```bash
# Install Husky.NET (auto-installs with dotnet restore)
dotnet tool restore
dotnet husky install

# Verify hooks are active
git config --get core.hookspath  # Should return .husky
```

## 📚 Lessons Learned (From Production Experience)

### Critical Time Wasters (Extracted from 16+ completed items)
1. **Float math for convenience**: 4 hours fixing + risk of save corruption (BR_001)
2. **Async in turn-based game**: 13 hours complete refactor (TD_011)
3. **Duplicate state sources**: 6+ hours fixing position sync (TD_009)
4. **Namespace issues**: 45+ minutes debugging MediatR discovery
5. **Wrong test framework assumptions**: 30 minutes fixing Moq vs NSubstitute
6. **GUID instability**: 2 hours tracking down inconsistent IDs
7. **Missing notifications**: UI disconnected from state changes
8. **Over-engineering**: 369 lines when 5 would work

### Root Causes (80% of issues stem from these)
1. **Convenience Over Correctness** (~35 hours wasted)
   - Choosing float over integer
   - Using async because "modern"
   - Taking shortcuts that become roadblocks

2. **Duplicate State Sources** (~12 bugs prevented)
   - Position in Actor AND GridService
   - Visual state separate from logical
   - No clear ownership model

3. **Architecture/Domain Mismatch** (~40% complexity reduction possible)
   - Async patterns in sequential games
   - Complex patterns for simple problems
   - Fighting the domain instead of embracing it

### What Actually Works
- **Start simple**: Add one condition before new abstraction
- **Integer arithmetic**: Always for game logic requiring determinism
- **SSOT architecture**: Each service owns one domain
- **Sequential processing**: For turn-based mechanics
- **Phase-based implementation**: Domain→Application→Infrastructure→Presentation
- **List<T> over SortedSet**: When duplicates needed (combat scheduling)
- **Trace data flow**: Domain → Effect → Notification → View
- **Estimate first**: >100 LOC = stop and review
- **Test defaults MUST match production**: Prevents subtle bugs
- **Slow is smooth, smooth is fast**: Ultra-think before coding

## 📚 References

- **Established HANDBOOK**: Our template and proven patterns
- **ADR-001**: Strict Model-View Separation
- **ADR-002**: Phased Implementation Protocol
- **GameStrapper.cs**: DI container pattern (established approach)
- **Clean Architecture**: Uncle Bob's principles
- **LanguageExt Docs**: Functional patterns in C#

---

*"Build from the domain outward, test at every layer, never compromise architecture"*

---

## Document History

This handbook incorporates critical lessons from:
- Established HANDBOOK.md (889 lines of production wisdom)
- 14+ critical gotchas discovered through experience
- Post-mortem extractions from 2025-08-27
- Git hook safety patterns proven to prevent errors

**Key Addition**: Simplicity Principle, Trust but Verify, Git Hooks, Lessons Learned