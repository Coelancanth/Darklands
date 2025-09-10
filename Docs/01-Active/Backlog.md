# Darklands Development Backlog


**Last Updated**: 2025-09-10 21:02 (TD_031 TimeUnit TU refactor completed and archived)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 002
- **Next TD**: 032  
- **Next VS**: 014 


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

### VS_011: Modal Combat Movement System (Stoneshard-style)
**Status**: Approved  
**Owner**: Dev Engineer
**Size**: M (6h total - simpler than original)
**Priority**: Critical
**Created**: 2025-09-10 19:03
**Updated**: 2025-09-10 20:08
**Tech Breakdown**: Revised for modal combat

**What**: Two-mode movement system - adjacent-only in combat, pathfinding in exploration
**Why**: Creates tactical depth where movement vs action is a meaningful choice

**Design Decision**: Modal combat like Stoneshard
- **Combat Mode**: Move to adjacent tile OR use skill (costs full turn)
- **Exploration Mode**: Click anywhere for A* pathfinding
- **Kiting Enabled**: Intentional design - trade damage for positioning

**Implementation Plan**:
- **Phase 1**: Domain model (1h)
  - GameMode enum (Combat/Exploration)
  - Fixed action costs (Move=100 TU, Attack=100 TU, Wait=100 TU)
  - Adjacent position validation only
  
- **Phase 2**: Application handlers (1.5h)
  - CombatMoveCommand (adjacent only, full turn cost)
  - ExploreMoveCommand (pathfinding, no combat)
  - Mode switching logic
  
- **Phase 3**: Infrastructure (1.5h)
  - Simple adjacent checker for combat
  - A* pathfinding for exploration only
  - State management for mode transitions
  
- **Phase 4**: UI differentiation (2h)
  - Combat Mode: Highlight 8 adjacent tiles only
  - Exploration Mode: Full pathfinding preview
  - Clear mode indicator in UI
  - Action feedback: "Move: 100 TU" (not distance-based)

**Key Simplifications**:
- NO complex movement cost calculations
- NO movement range based on stats
- Every combat action = 100 TU base (modified by speed)
- Adjacent movement only in combat (8 tiles max)

**Tactical Implications**:
- Kiting costs 50% damage output (move turns vs attack turns)
- Positioning matters more (can't reposition freely)
- Terrain becomes critical (dead ends are dangerous)
- Speed affects turn frequency, not movement range

**Done When**:
- [Combat Mode] Click adjacent tile → move there (100 TU cost)
- [Combat Mode] Can't click non-adjacent tiles
- [Exploration Mode] Click anywhere → pathfind there
- Mode indicator clearly shows current mode
- Combat auto-triggers when enemies nearby
- Message log: "Player moved north (100 TU)"

**Architectural Constraints** (MANDATORY):
☑ Deterministic: Fixed 100 TU cost for all combat moves
☑ Save-Ready: Position stored as grid coordinates (x,y)
☑ Time-Independent: Turn-based, not real-time
☑ Integer Math: No float calculations needed
☑ Testable: Modal logic easy to unit test

**Depends On**: TD_031 (TimeUnit fix to use TU not milliseconds)
**Next Step**: Fix TimeUnit first, then implement Phase 1

---



## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

<!-- TD_031 moved to permanent archive (2025-09-10 21:02) - TimeUnit TU refactor completed successfully -->

### VS_013: Basic Enemy AI
**Status**: Proposed
**Owner**: Product Owner → Tech Lead
**Size**: M (4-8h)  
**Priority**: Important
**Created**: 2025-09-10 19:03

**What**: Simple but effective enemy AI for combat testing
**Why**: Need opponents to validate combat system and create gameplay loop
**How**:
- Decision tree for action selection (move/attack/wait)
- Target prioritization (closest/weakest/most dangerous)
- Basic pathfinding to reach targets
- Flee behavior when low health
**Done When**:
- Enemies move towards player intelligently
- Enemies attack when in range
- AI makes decisions based on game state
- Different enemy types show different behaviors
- AI actions integrate with scheduler

**Architectural Constraints** (MANDATORY):
☑ Deterministic: AI decisions based on seeded random
☑ Save-Ready: AI state fully serializable
☑ Time-Independent: Decisions based on game state not time
☑ Integer Math: All AI calculations use integers
☑ Testable: AI logic can be unit tested

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