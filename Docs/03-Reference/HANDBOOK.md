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

## 🧪 Testing Patterns

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

## 🔥 Common Bug Patterns

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

### Critical Time Wasters
1. **Namespace issues**: 45+ minutes debugging MediatR discovery
2. **Wrong test framework assumptions**: 30 minutes fixing Moq vs NSubstitute
3. **GUID instability**: 2 hours tracking down inconsistent IDs
4. **Missing notifications**: UI disconnected from state changes
5. **Over-engineering**: 369 lines when 5 would work

### What Actually Works
- **Start simple**: Add one condition before new abstraction
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