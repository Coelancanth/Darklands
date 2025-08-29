# Darklands Development Backlog

**Last Updated**: 2025-08-29 14:19
**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 001
- **Next TD**: 000 
- **Next VS**: 003 

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

### BR_001: Remove Float/Double Math from Combat System
**Status**: New  
**Owner**: Product Owner → Debugger Expert
**Size**: S (<4h)
**Priority**: Critical (Blocks VS_002)
**Markers**: [ARCHITECTURE] [DETERMINISM] [SAFETY-CRITICAL]
**Created**: 2025-08-29 14:19

**What**: Replace all floating-point math in TimeUnitCalculator with integer arithmetic
**Why**: Float math causes non-deterministic behavior, save/load issues, and platform inconsistencies

**The Problem**:
```csharp
// Current DANGEROUS implementation in TimeUnitCalculator.cs:
var agilityModifier = 100.0 / agility;  // DOUBLE - non-deterministic!
var encumbranceModifier = 1.0 + (encumbrance * 0.1);  // DOUBLE!
var finalTime = (int)Math.Round(baseTime * agilityModifier * encumbranceModifier);
```

**The Fix**:
- Replace with scaled integer math (multiply by 100 or 1000)
- Use integer division with proper rounding
- Ensure all operations are deterministic
- No Math.Round() or floating operations

**Done When**:
- Zero float/double types in Domain layer
- All calculations use integer math
- Tests prove deterministic results
- Same inputs ALWAYS produce same outputs
- Property-based tests validate integer math

**Impact if Not Fixed**:
- Save/load will have desyncs
- Replays impossible
- Platform-specific bugs
- Multiplayer would desync
- "Unreproducible" bug reports

**Depends On**: None (but blocks VS_002)

### VS_002: Combat Timeline Scheduler (Phase 2 - Application Layer)
**Status**: Proposed  
**Owner**: Product Owner → Tech Lead
**Size**: S (<4h)
**Priority**: Critical
**Markers**: [ARCHITECTURE] [PHASE-2]
**Created**: 2025-08-29 14:15

**What**: Priority queue-based timeline scheduler for traditional roguelike turn order
**Why**: Core combat system foundation - all combat features depend on this

**How** (SOLID but Simple):
- Timeline class with SortedSet<ISchedulable> (20 lines)
- ISchedulable interface with Guid Id AND NextTurn properties
- ScheduleActorCommand/Handler for MediatR integration
- ProcessTurnCommand/Handler for game loop
- TimeComparer using both time AND Id for deterministic ordering

**Done When**:
- Actors execute in correct time order (fastest first)
- Unique IDs ensure deterministic tie-breaking
- Time costs from Phase 1 determine next turn
- Commands process through MediatR pipeline
- 100+ actors perform without issues
- Comprehensive unit tests pass

**Acceptance by Phase**:
- Phase 2 (This): Commands schedule/process turns correctly
- Phase 3 (Next): State persists between sessions
- Phase 4 (Later): UI displays turn order

**Depends On**: VS_001 (COMPLETE) + BR_001 (float math fix)

**Product Owner Notes** (2025-08-29):
- Keep it ruthlessly simple - target <100 lines of logic
- Use existing TimeUnit comparison operators for sorting
- No event systems or complex patterns
- Standard priority queue algorithm from any roguelike
- **CRITICAL**: Every entity MUST have unique Guid Id for deterministic tie-breaking



## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### TD_001: Create Development Setup Documentation [Score: 45/100]
**Status**: Proposed  
**Owner**: Tech Lead → DevOps Engineer
**Size**: S (<4h)  
**Priority**: Important  
**Markers**: [DOCUMENTATION] [ONBOARDING]

**What**: Document complete development environment setup based on BlockLife patterns
**Why**: Ensure all developers/personas have identical, working environment
**How**: 
- Document required tools (dotnet SDK, Godot 4.4.1, PowerShell/bash)
- Copy BlockLife's scripts structure
- Document git hook installation process
- Create troubleshooting guide for common setup issues

**Done When**:
- SETUP.md created with step-by-step instructions
- Script to verify environment works
- Fresh clone can be set up in <10 minutes
- All personas can follow guide successfully

**Depends On**: ~~VS_001~~ (COMPLETE 2025-08-29) - Now unblocked



## 🗄️ Backup (Complex Features for Later)
*Advanced mechanics postponed until core loop is proven fun*


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
*Single Source of Truth for all BlockLife development work. Simple, maintainable, actually used.*