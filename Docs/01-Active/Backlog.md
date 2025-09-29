# Darklands Development Backlog


**Last Updated**: 2025-09-08 17:32 (TD_019 completed by DevOps Engineer)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 001
- **Next TD**: 001
- **Next VS**: 002 


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

### VS_01: Architectural Skeleton - Walking Skeleton Implementation [ARCHITECTURE] [Score: 30/100]
**Status**: Proposed
**Owner**: Product Owner → Tech Lead (breakdown)
**Size**: M (6-8h)
**Priority**: Critical (Foundation for all other work)
**Markers**: [ARCHITECTURE] [FRESH-START] [WALKING-SKELETON]
**Created**: 2025-09-30

**What**: Implement minimal walking skeleton following new ADR-001, ADR-002, ADR-003 architecture
**Why**: Fresh start with clean architecture - need proven foundation before building features

**Context**:
- Deleted ALL existing source code for fresh start
- Created 3 new ADRs defining clean architecture
- Need to prove architecture works end-to-end with simplest possible feature
- Follow "Walking Skeleton" pattern: thinnest possible slice through all layers

**Scope** (Minimal Health System):
1. **Domain Layer** (Pure C#):
   - Health value object with Create/Reduce/Increase
   - IHealthComponent interface
   - HealthComponent implementation

2. **Application Layer** (CQRS):
   - TakeDamageCommand + Handler
   - HealthChangedEvent
   - Simple in-memory component registry

3. **Infrastructure Layer**:
   - GameStrapper (DI container setup)
   - GodotEventBus implementation
   - EventAwareNode base class
   - ServiceLocatorBridge
   - SelectMany extensions for CSharpFunctionalExtensions

4. **Presentation Layer** (Godot):
   - Simple test scene with one actor
   - HealthComponentNode (shows health bar)
   - Button to damage actor
   - Verify: Click button → Health bar updates

5. **Tests**:
   - Health value object tests
   - TakeDamageCommandHandler tests
   - EventBus subscription tests

**NOT in Scope** (defer to later):
- Grid system
- Movement
- Combat mechanics
- Multiple actors
- AI
- Turn system
- Complex UI

**How** (Implementation Order):
1. **Phase 1: Domain** (~2h)
   - Create Health value object with tests
   - Create IHealthComponent + HealthComponent
   - Tests: Health.Create, Reduce, validation

2. **Phase 2: Application** (~2h)
   - TakeDamageCommand + Handler
   - HealthChangedEvent
   - ComponentRegistry service
   - Tests: Handler logic with mocked registry

3. **Phase 3: Infrastructure** (~2h)
   - GameStrapper with DI setup
   - GodotEventBus + UIEventForwarder
   - EventAwareNode base class
   - SelectMany extensions

4. **Phase 4: Presentation** (~2h)
   - Simple scene (1 sprite + health bar + damage button)
   - HealthComponentNode
   - Wire everything together
   - Manual test: Click button → health bar updates

**Done When**:
- ✅ Build succeeds (dotnet build)
- ✅ Core tests pass (dotnet test)
- ✅ Godot project loads without errors
- ✅ Can click "Damage" button and health bar updates
- ✅ No Godot references in Darklands.Core project
- ✅ GodotEventBus routes events correctly
- ✅ CSharpFunctionalExtensions Result<T> works end-to-end
- ✅ All 3 ADRs validated with working code
- ✅ Code committed with message: "feat: architectural skeleton [VS_011]"

**Depends On**: None (fresh start)

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