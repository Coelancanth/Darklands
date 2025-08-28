# Darklands Development Backlog

**Last Updated**: 2025-08-29 04:05
**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 000
- **Next TD**: 000 
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

### VS_001: Foundation - 3-Project Architecture with DI, Logging & Git Hooks
**Status**: Ready for Dev  
**Owner**: Tech Lead → Dev Engineer
**Size**: XL (4-5 days)  
**Priority**: Critical  
**Markers**: [ARCHITECTURE] [FOUNDATION] [SAFETY]

**What**: Implement ADR-001 architecture with full safety infrastructure from BlockLife
**Why**: Foundation for ALL development - modding support, fast CI, team safety
**How**: 
- Create 3-project structure per ADR-001 (.csproj configs exact from BlockLife)
- Copy/adapt GameStrapper.cs pattern (468 lines) from BlockLife
- Copy .husky folder with all git hooks from BlockLife  
- Implement Serilog with fallback-safe configuration
- Set up Husky.NET in Core.csproj for auto-installing hooks
- Create LogCategory.cs for well-defined contexts
- Walking skeleton: Simple time-unit calculation feature

**Done When**:
- Three projects build with zero warnings
- DI container validates on startup without errors  
- Git hooks prevent direct main push (tested)
- Commit messages enforce conventional format
- Logging works and never crashes (even with bad config)
- Walking skeleton passes all phase tests
- CI runs Core tests in <30 seconds
- README updated with setup instructions

**Depends On**: None

**Tech Lead Decision** (2025-08-29):  
- Architecture approved in ADR-001 after BlockLife analysis
- MUST copy proven patterns exactly - do not reinvent
- Git hooks are pedagogy tools that teach correct workflow
- Simplicity Principle applies: estimate >100 LOC = stop and review
- Follow ADR-002 phased implementation strictly



## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### TD_001: Create Development Setup Documentation
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

**Depends On**: VS_001 (for final structure)



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