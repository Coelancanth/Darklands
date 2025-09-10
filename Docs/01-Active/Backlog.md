# Darklands Development Backlog


**Last Updated**: 2025-09-10 16:45 (TD_018 Integration Tests completed and archived)

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



---


### TD_013: Extract Test Data from Production Presenters [SEPARATION] [Score: 40/100] ✅ COMPLETED
**Status**: Done ✅ (2025-09-10 18:20)
**Owner**: Dev Engineer
**Size**: S (2-3h actual: ~2h)
**Priority**: Critical (Test code in production)
**Markers**: [SEPARATION-OF-CONCERNS] [SIMPLIFICATION]
**Created**: 2025-09-08 14:42
**Completed**: 2025-09-10 18:20

**What**: Extract test actor creation to simple IActorFactory
**Why**: ActorPresenter contains 90+ lines of hardcoded test setup, violating SRP

**✅ IMPLEMENTATION COMPLETE**:
- **IActorFactory interface**: Clean abstraction with CreatePlayer/CreateDummy methods
- **ActorFactory implementation**: Direct service injection (simpler than MediatR commands)
- **ActorPresenter refactored**: All test initialization code removed (-133 lines)
- **GridPresenter updated**: Uses factory.PlayerId instead of static reference
- **Static TestPlayerId eliminated**: No global state dependencies
- **DI integration**: Registered as singleton in GameStrapper

**✅ RESULTS ACHIEVED**:
- **Clean separation**: Zero test code in production presenters
- **Architecture compliance**: Proper dependency injection and interface abstractions
- **Quality maintained**: 632/632 tests passing, zero warnings
- **Complexity reduced**: From 85/100 to 40/100 as planned
- **Code reduction**: Net -54 lines total (134 removed, 80 added)

**Dev Engineer Decision** (2025-09-10 18:20):
- **SIMPLER APPROACH SUCCESSFUL** - Direct service injection over MediatR commands
- **Clean Architecture achieved** - Test logic completely extracted from presenters
- **Production ready** - Comprehensive error handling with Fin<T> patterns
- **Maintainable** - Simple factory pattern easy to extend and test

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



<!-- TD_017 and TD_019 moved to permanent archive (2025-09-09 17:53) -->

---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*