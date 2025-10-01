# Darklands Development Backlog


**Last Updated**: 2025-10-01 17:54 (Dev Engineer: VS_006 complete, TD_002 created for debug panel)

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

### TD_002: In-Game Debug Panel with ILogger Integration
**Status**: Proposed
**Owner**: Tech Lead (for approval) → Dev Engineer (for implementation)
**Size**: S (<4h)
**Priority**: Important (affects debugging velocity)
**Markers**: [DEVELOPER-EXPERIENCE] [INFRASTRUCTURE]

**What**: Create in-game debug panel that displays ILogger output with filtering capabilities

**Why**:
- **Current Issue**: ILogger logs go to Serilog backend but not visible in GridTestScene
- Godot Output panel is empty (no messages) despite extensive logging added
- Developers need real-time log visibility during gameplay testing
- Log level filtering (Debug/Info/Warning/Error) speeds up debugging

**How** (Implementation Approach):
- Create `DebugPanelNode.cs` (Godot UI panel, bottom of screen)
- Implement custom `ILoggerProvider` that forwards logs to UI
- Register provider in `GameStrapper` (ADR-001: logging infrastructure)
- Panel features:
  - **Log display**: ScrollContainer with recent N messages (circular buffer)
  - **Level filtering**: Buttons to toggle Debug/Info/Warning/Error visibility
  - **Color coding**: Debug=gray, Info=white, Warning=yellow, Error=red
  - **Toggle visibility**: F12 key to show/hide panel (default: visible in dev builds)
  - **Performance**: Async message queue to avoid blocking game loop

**Done When**:
- ✅ DebugPanelNode integrated in GridTestScene
- ✅ ILogger messages appear in panel in real-time
- ✅ Log level filtering works (can hide Debug, show only Errors, etc.)
- ✅ F12 toggles panel visibility
- ✅ No performance impact (logs don't block rendering)
- ✅ Messages color-coded by severity
- ✅ All 215 existing tests still pass

**Depends On**: None (infrastructure already in place)

**Tech Lead Decision** (2025-10-01 18:06):
- **Status**: ✅ APPROVED with mandatory threading refinement
- **Architecture**: Custom `ILoggerProvider` + DebugPanelNode (Godot UI)
- **Threading Pattern**: ConcurrentQueue + _Process consumption (NOT CallDeferred in provider)
  - **Rationale**: ILogger can be called from ANY thread (async handlers, etc.)
  - **Pattern**: Provider enqueues logs safely → DebugPanelNode consumes queue in _Process() (guaranteed main thread)
  - **Performance**: Zero blocking, queue processed per frame (<1ms overhead)
- **Critical Implementation Details**:
  - Provider uses `ConcurrentQueue<LogEntry>` for thread-safe enqueuing
  - DebugPanelNode calls `SetDebugPanel()` on provider in `_Ready()` (after Godot node ready)
  - UI updates in `_Process()` (always main thread, no CallDeferred needed)
  - Circular buffer (max 100 messages) to prevent memory growth
- **DI Registration**: `services.AddSingleton<ILoggerProvider, DebugPanelLoggerProvider>()` in GameStrapper
- **Size Validation**: 4h estimate confirmed (1h provider + 1.5h UI + 0.5h DI + 1h testing)
- **ADR Compliance**:
  - ✅ ADR-001: ILogger abstraction (not GD.Print)
  - ✅ ADR-002: ServiceLocator only in _Ready() to wire provider → node
  - ✅ ADR-004: Godot threading constraints (ConcurrentQueue + _Process pattern per section "Godot Threading Constraints")
- **Risks**: Minimal (standard producer-consumer pattern, no new architectural patterns)
- **Next Step**: Route to Dev Engineer for implementation

---

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