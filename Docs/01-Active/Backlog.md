# Darklands Development Backlog


**Last Updated**: 2025-10-01 19:12 (Dev Engineer: TD_003 + TD_004 created for logging improvements)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 004
- **Next TD**: 003
- **Next VS**: 007


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

---

*Recently completed and archived (2025-10-01):*
- **VS_006**: Interactive Movement System - A* pathfinding, hover preview, fog of war, ILogger refactor ✅ (2025-10-01 17:54)
- **VS_005**: Grid, FOV & Terrain System - Custom shadowcasting, 189 tests, event-driven integration ✅ (2025-10-01 15:19)
- **VS_001**: Health System Walking Skeleton - Architectural foundation validated ✅
- **BR_001**: Race Condition - Fixed with WithComponentLock pattern ✅
- **BR_002**: Fire-and-Forget Events - Fixed with async/await ✅
- **BR_003**: Heal Button CQRS Bypass - Removed per YAGNI ✅
- **TD_001**: Architecture Enforcement Tests - 10 tests enforcing all 4 ADRs ✅
- *See: [Completed_Backlog_2025-10.md](../07-Archive/Completed_Backlog_2025-10.md)*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*


### TD_002: Refactor Debug Console to .tscn Scene with .tres Config
**Status**: Proposed
**Owner**: Tech Lead (for approval) → Dev Engineer (for implementation)
**Size**: M (4-6h)
**Priority**: Important (affects maintainability + fixes UI bugs)
**Markers**: [DEVELOPER-EXPERIENCE] [REFACTOR]

**What**: Convert procedural Debug Console to proper Godot scene + resource architecture

**Why**:
- **Current Issues**:
  - CheckBoxes not visible (Z-index issues with procedural UI)
  - OptionButton not responding to clicks (mouse filter problems)
  - No game pause when console visible
  - Hard to debug/maintain (200+ lines of UI creation code)
  - No hot-reload for defaults
- **Better Architecture**:
  - Visual scene editing (see layout instantly)
  - Proper Godot containers (auto-layout, responsive)
  - Resource-based config (hot-reloadable defaults)
  - Clean separation (UI in scene, logic in script, config in resource)

**How**:
1. **Create DebugConsole.tscn scene**:
   - Panel (semi-transparent background)
   - MarginContainer (40px margins)
   - VBoxContainer (auto-layout children vertically)
   - OptionButton for log level
   - VBoxContainer for category checkboxes (populated at runtime)

2. **Create DebugConsoleConfig.tres resource**:
   ```csharp
   public partial class DebugConsoleConfig : Resource
   {
       [Export] public LogLevel DefaultLogLevel { get; set; } = LogLevel.Information;
       [Export] public string[] DefaultCategories { get; set; } = Array.Empty<string>();
   }
   ```

3. **Refactor DebugConsoleController.cs**:
   - Remove all CreateUI() procedural code
   - Load DebugConsole.tscn as child scene
   - Load DebugConsoleConfig.tres for defaults
   - Wire up signals (CheckBox.Toggled, OptionButton.ItemSelected)
   - Add pause handling (`GetTree().Paused = _container.Visible`)

4. **Keep JSON for user state**:
   - Loading priority: JSON user overrides → .tres defaults → hardcoded fallback

**Done When**:
- ✅ DebugConsole.tscn exists and loads correctly
- ✅ DebugConsoleConfig.tres hot-reloads (change default level in Editor, see effect)
- ✅ CheckBoxes visible and clickable
- ✅ OptionButton responds to clicks and changes log level
- ✅ Game pauses when F12 opens console
- ✅ User preferences persist via JSON (between sessions)
- ✅ All 215 tests pass

**Depends On**: None

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*

**No items in Ideas section!** ✅

*Future work is tracked in [Roadmap.md](../02-Design/Game/Roadmap.md) with dependency chains and sequencing.*

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