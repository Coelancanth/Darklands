# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 💭 CRITICAL: HONEST FEEDBACK & CHALLENGING AUTHORITY

**YOU MUST BE A CRITICAL THINKING PARTNER** - Your responsibility includes:

### Always Provide Honest Opinions
- **Question complexity** - "Is this really necessary?"
- **Challenge over-engineering** - "There's a simpler way to do this"
- **Suggest alternatives** - "Have you considered..."
- **Point out risks** - "This approach might cause..."
- **Advocate for simplicity** - "Let's start with the minimal solution"

### When to Object (REQUIRED)
```
🚨 STOP and push back when you see:
- Over-engineering simple problems
- Adding complexity without clear benefit
- Creating abstractions "just in case"
- Following process for process sake
- Building enterprise solutions for simple needs
- Adding features not requested
- Premature optimization
```

### How to Give Honest Feedback
```
❌ Bad: "I'll implement what you asked"
✅ Good: "I understand you want X, but have you considered Y? 
         It's simpler and achieves the same goal."

❌ Bad: Silently follow complex instructions
✅ Good: "This feels over-engineered. Can we start with 
         the simple solution and see if it works?"

❌ Bad: Build elaborate solutions
✅ Good: "Before we build a complex system, let's try 
         the 5-line solution first."
```

### Your Obligation to Challenge
- **Question scope creep** - "Do we really need all these features?"
- **Advocate for MVP** - "What's the minimal version that works?"
- **Suggest proven patterns** - "The existing approach handles this"
- **Call out unnecessary complexity** - "This is more complex than needed"
- **Recommend incremental approach** - "Let's build this step by step"

**Remember: Simplicity is sophistication. Your job is to help build the RIGHT solution, not just ANY solution.**

## 🧠 Memory Bank System - ACTIVE MAINTENANCE REQUIRED

**CRITICAL**: Memory Bank must be actively maintained to prevent context loss!

### Memory Bank Files
- **activeContext.md** - Current work state (expires: 7 days)

## 🎯 Core Directive
Do what has been asked; nothing more, nothing less.
- NEVER create files unless absolutely necessary
- ALWAYS prefer editing existing files
- NEVER proactively create documentation unless requested

## 🤖 Streamlined Persona System with v4.0 Auto-Sync

### 🎯 AUTOMATIC EMBODIMENT - ZERO FRICTION
**When user says "embody [persona]", ALWAYS run:**
```bash
./scripts/persona/embody.ps1 [persona]
```

**This script handles EVERYTHING automatically:**
- ✅ Detects and resolves squash merges (no conflicts!)
- ✅ Handles interrupted rebases/merges
- ✅ Preserves uncommitted work
- ✅ Fixes detached HEAD states
- ✅ Cleans up stale branches
- ✅ Updates Memory Bank automatically

**You NEVER need to:**
- Check if there was a squash merge
- Manually resolve conflicts from PRs
- Run separate sync commands
- Worry about git state

### Persona Flow
```
Product Owner → Tech Lead → Dev Engineer → Test Specialist → DevOps
     (WHAT)       (HOW)       (BUILD)        (VERIFY)       (DEPLOY)
                                 ↓               ↓
                          Debugger Expert (FIX COMPLEX ISSUES)
```

### Key Protocol: Suggest, Don't Auto-Execute
**⚠️ CRITICAL**: Personas SUGGEST backlog updates, never auto-invoke backlog-assistant.

**Process**: Persona completes work → Suggests updates → User chooses to execute

## 🏗️ ARCHITECTURE: Clean Architecture with Godot Integration

**MANDATORY**: Follow these architectural boundaries strictly.

### Core Principles (ADR-001, ADR-002, ADR-003, ADR-004)

**Three-Layer Architecture:**
```
┌────────────────────────────────────────────────┐
│ Presentation Layer (Godot C#)                 │
│ - Godot nodes, scenes, UI                     │
│ - Uses ServiceLocator.Get<T>() ✅             │
│ - References: Darklands.Core ✅               │
│ - Project: Darklands.csproj (Godot SDK)       │
└─────────────────┬──────────────────────────────┘
                  │ One-way dependency
                  ↓
┌────────────────────────────────────────────────┐
│ Core Layer (Pure C#)                          │
│ - Domain, Application, Infrastructure         │
│ - Uses constructor injection ✅               │
│ - ZERO Godot dependencies ✅                  │
│ - Project: Darklands.Core.csproj (NET SDK)    │
└────────────────────────────────────────────────┘
```

### 🚨 CRITICAL: Dependency Rules (ENFORCED AT COMPILE-TIME)

**IN CORE (Darklands.Core.csproj):**
```csharp
// ✅ ALLOWED - Portable abstractions
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// ❌ FORBIDDEN - Will not compile!
using Godot;  // ERROR: Type or namespace 'Godot' could not be found
```

**IN PRESENTATION (Darklands.csproj - Godot):**
```csharp
// ✅ ALLOWED - Can reference both
using Godot;
using Darklands.Core.Application.Commands;
using Darklands.Core.Infrastructure;

// ✅ Use ServiceLocator ONLY in _Ready()
public override void _Ready()
{
    _mediator = ServiceLocator.Get<IMediator>();  // OK here!
}
```

### ServiceLocator Pattern - When to Use

**✅ USE in Presentation Layer (_Ready() methods):**
```csharp
// Godot instantiates nodes via scene loading, not DI
public override void _Ready()
{
    base._Ready();

    // ServiceLocator bridges Godot → DI container
    _mediator = ServiceLocator.Get<IMediator>();
    _logger = ServiceLocator.Get<ILogger<HealthBarNode>>();
}
```

**❌ NEVER USE in Core Layer:**
```csharp
// ❌ WRONG - Core uses constructor injection
public class ExecuteAttackCommandHandler
{
    // ✅ CORRECT - Explicit dependencies
    public ExecuteAttackCommandHandler(
        ILogger<ExecuteAttackCommandHandler> logger,
        IActorRepository actors)
    {
        _logger = logger;
        _actors = actors;
    }
}
```

### Why ServiceLocator at Godot Boundary is OK

**Context Matters:**
- ❌ Service Locator in business logic = Anti-pattern (hides dependencies, breaks testability)
- ✅ Service Locator at framework boundary = Pragmatic (Godot constraint, isolated to _Ready())

**Our Approach:**
- Core uses constructor injection (testable, explicit dependencies)
- Presentation uses ServiceLocator bridge (adapts Godot's instantiation model)
- Result: Clean Core + Pragmatic Godot integration

**See**: [ADR-002: Godot Integration Architecture](Docs/03-Reference/ADR/ADR-002-godot-integration-architecture.md)

### Error Handling - CSharpFunctionalExtensions

**ALWAYS use Result<T> for operations that can fail:**
```csharp
// ✅ CORRECT - Functional error handling
public Result<Health> TakeDamage(float amount) =>
    CurrentHealth.Reduce(amount)
        .Tap(newHealth => CurrentHealth = newHealth);

// ❌ WRONG - Exceptions for business logic
public Health TakeDamage(float amount)
{
    if (amount <= 0) throw new ArgumentException();
    // ...
}
```

**See**: [ADR-003: Functional Error Handling](Docs/03-Reference/ADR/ADR-003-functional-error-handling.md)

### Feature Organization - Feature-Based Clean Architecture

**Hybrid approach** combining VSA + Clean Architecture:

```
src/Darklands.Core/
├── Domain/Common/              # Shared primitives (used by 3+ features)
│   ├── ActorId.cs
│   ├── Health.cs
│   └── Position.cs            # Max ~10 files
│
└── Features/                   # Feature organization (VSA)
    ├── Combat/
    │   ├── Domain/            # Layer separation (Clean Architecture)
    │   ├── Application/
    │   └── Infrastructure/
    └── Health/
        ├── Domain/
        ├── Application/
        └── Infrastructure/
```

**The Five Event Rules** (Prevent Event Soup):
1. **Commands Orchestrate, Events Notify** - All work in handlers, events are facts
2. **Events Are Facts** - Past tense (HealthChangedEvent), not commands
3. **Event Handlers Are Terminal Subscribers** - No cascading events/commands, allows UI/sound/analytics
4. **Max Event Depth = 1** - No cascading events (leaf nodes only)
5. **Document Event Topology** - EVENT_TOPOLOGY.md per feature

**See**: [ADR-004: Feature-Based Clean Architecture](Docs/03-Reference/ADR/ADR-004-feature-based-clean-architecture.md)

### Logging - Microsoft.Extensions.Logging + Serilog

**Core uses ONLY abstractions:**
```csharp
// Core/Application/Commands/ExecuteAttackCommandHandler.cs
using Microsoft.Extensions.Logging;  // ✅ Abstraction

public class ExecuteAttackCommandHandler
{
    private readonly ILogger<ExecuteAttackCommandHandler> _logger;

    // ✅ Constructor injection with ILogger<T>
    public ExecuteAttackCommandHandler(
        ILogger<ExecuteAttackCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result> Handle(ExecuteAttackCommand cmd)
    {
        _logger.LogInformation("Executing attack from {AttackerId}", cmd.AttackerId);
        // ...
    }
}
```

**Infrastructure/Presentation provides Serilog implementation:**
- Core.csproj: Microsoft.Extensions.Logging.**Abstractions** only
- Darklands.csproj: Serilog packages + MS.Extensions.Logging
- GameStrapper configures Serilog as logging provider

### Test Documentation Standards

**Comment test INTENT (WHY), not implementation (WHAT):**

**✅ DO Comment:**
- **Business Rules**: `// WHY: Zero-damage attacks are valid (status effects without damage)`
- **Bug Regressions**: `// REGRESSION BR_042: Sequential attacks didn't stack damage`
- **Edge Cases**: `// EDGE CASE: Godot float precision causes near-zero health`
- **Architecture Constraints**: `// ARCHITECTURE: Core must have zero Godot dependencies`
- **Performance Requirements**: `// PERFORMANCE: Must complete in <100ms for smooth gameplay`

**❌ DON'T Comment:**
- Obvious behavior (test name explains all)
- AAA labels (visual separation via blank lines sufficient)
- Restating what code already shows

**Test Naming Convention:**
```
MethodName_Scenario_ExpectedBehavior

Examples:
- Handle_ValidAttack_ShouldReduceTargetHealth
- Handle_AttackWithNegativeDamage_ShouldReturnFailure
- Handle_ConcurrentAttacks_ShouldNeverResultInNegativeHealth
```

**Decision Rule**: Will a developer 6 months from now understand WHY this test exists? If no → add comment.

## 📅 IMPORTANT: Date-Sensitive Documents

**ALWAYS run `date` command first when creating or updating:**
- Memory Bank files (`.claude/memory-bank/active/*.md`)
- Session logs (`.claude/memory-bank/session-log.md`)
- Post-mortems
- Backlog updates with completion dates
- Release notes
- Any document with timestamps

```bash
# Run this FIRST, ALWAYS:
date  # Get current date/time before creating/updating dated documents

# Then use that timestamp in your updates
# Example: "**Last Updated**: 2025-08-24 01:59"
```

**Automated in embody.ps1:**
- Script captures timestamp at start: `$scriptStartTime = Get-Date`
- Uses consistent timestamp throughout execution
- Prevents stale timestamps when script runs for extended time

## 🚦 Quality Gates & CI/CD

### Test Execution
```bash
# MANDATORY before committing (build + all tests)
./scripts/core/build.ps1 test

# Feature-specific testing
./scripts/core/build.ps1 test --filter "Category=Combat"
./scripts/core/build.ps1 test --filter "Category=Health"
./scripts/core/build.ps1 test --filter "Category=Inventory"

# Alternative: Run tests directly (skip Godot build validation)
dotnet test tests/Darklands.Core.Tests/Darklands.Core.Tests.csproj
```

## 📖 Git Workflow with Smart Sync

### 🎯 NEW: Automatic Sync Resolution
**ALWAYS use `git sync` instead of manual pull/rebase:**
```bash
git sync  # Automatically detects and handles squash merges, conflicts, etc.
```

**Or use the PR workflow:**
```bash
pr create   # Create PR from current branch
pr merge    # Merge PR and auto-sync dev/main
pr sync     # Same as git sync
pr status   # Check PR and sync status
```

**Essential**: Check branch status before starting work: `./scripts/git/branch-status-check.ps1`

## 📦 PR Merge Strategy

### Default: Squash and Merge
When merging PRs to main, use **Squash and merge** by default:
```bash
# Via GitHub UI: Select "Squash and merge" 
# Via CLI:
gh pr merge --squash --delete-branch
```

**When to use**: 90% of PRs (feature implementations, bug fixes with WIP commits)
**When NOT to use**: Large refactors with meaningful intermediate steps

## Important Instruction Reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.