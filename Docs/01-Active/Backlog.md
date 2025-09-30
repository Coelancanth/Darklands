# Darklands Development Backlog


**Last Updated**: 2025-10-01 00:48 (All VS_001 bugs fixed and verified: BR_001, BR_002, BR_003 completed + 2 bonus bugs)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 004
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

**No critical items!** ✅

*Recently completed and archived (2025-10-01):*
- **VS_001**: Health System Walking Skeleton - Architectural foundation validated ✅
- **BR_001**: Race Condition - Fixed with WithComponentLock pattern ✅
- **BR_002**: Fire-and-Forget Events - Fixed with async/await ✅
- **BR_003**: Heal Button CQRS Bypass - Removed per YAGNI ✅
- *See: [Completed_Backlog_2025-10.md](../07-Archive/Completed_Backlog_2025-10.md)*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### TD_001: Architecture Enforcement Tests (NetArchTest + Custom)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer (after approval)
**Size**: M (2-3h)
**Priority**: Important (Do AFTER BR_001/002/003 - prevents architectural drift)
**Markers**: [ARCHITECTURE] [TESTING] [POST-MORTEM]
**Created**: 2025-09-30 (VS_004 post-mortem)
**Updated**: 2025-10-01 (Deprioritized below critical bugs)

**What**: Automated tests enforcing ADR-001/ADR-002/ADR-003 architectural rules + VS_004 lessons

**Why**:
- VS_004 revealed MediatR double-registration bug (caught by tests, but needs permanent guard)
- DebugConsole LoggingService not registered (need completeness tests)
- ADR-003 mandates Result<T> pattern but no automated enforcement
- **Tests as living documentation** > static docs (self-updating, enforced, always accurate)
- **NOTE**: Core → Godot already enforced by `.csproj` SDK choice (compile-time beats runtime!)

**How** (Test Categories):

**1. NetArchTest - Naming/Pattern Enforcement** (~1h)
```csharp
// NOTE: Core → Godot dependency already prevented by SDK choice!
// Darklands.Core.csproj uses Microsoft.NET.Sdk (pure C#)
// → Compile-time enforcement is better than runtime tests

// ADR-003: Enforce Result<T> pattern for error handling
[Fact]
public void CommandHandlers_ShouldReturnResult()
{
    // WHY: ADR-003 mandates Result<T> for all operations that can fail
    Types.InAssembly(typeof(IRequest).Assembly)
        .That().ImplementInterface(typeof(IRequestHandler<,>))
        .Should().HaveMethodMatching("Handle", method =>
            method.ReturnType.Name.StartsWith("Result"))
        .GetResult().IsSuccessful.Should().BeTrue();
}

// Namespace organization (if needed)
[Fact]
public void Domain_ShouldNotDependOnApplication()
{
    // WHY: Domain layer must be pure, no application logic
    Types.InNamespace("Darklands.Core.Domain")
        .Should().NotHaveDependencyOnAny(
            "Darklands.Core.Application",
            "Darklands.Core.Infrastructure")
        .GetResult().IsSuccessful.Should().BeTrue();
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

**Tech Lead Decision** (2025-10-01 00:24):
- **Approved**: Scope and priority (do AFTER critical bug fixes)
- **Rejected**: NetArchTest rule for `async void` (high false positive rate - use code review checklist instead)
- **Approved**: NetArchTest for Result<T> pattern enforcement in handlers
- **Approved**: MediatR double-registration tests
- **Approved**: DI completeness tests

**Additional Rules NOT in Scope** (VS_001 post-mortem review):
- `async void` enforcement: Rejected - too many valid Godot event handlers, use code review checklist
- NaN/Infinity validation: Should be in handler code, not architectural test
- EVENT_TOPOLOGY.md existence: Manual governance, not automated test

**Dev Engineer Notes** (after approval):
- NetArchTest 8.x supports .NET 8
- Tests will be fast (<100ms) - just reflection, no runtime overhead
- Living documentation: Test comments reference ADR-001/002/003 + VS_004 post-mortem
- CI integration: Add `--filter "Category=Architecture"` to quick.ps1
- Focus on low false-positive rules (Result<T>, MediatR registration, DI completeness)

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