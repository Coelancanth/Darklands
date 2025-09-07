# Darklands Development Backlog


**Last Updated**: 2025-09-07 12:41

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 001
- **Next TD**: 007  
- **Next VS**: 009 


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




### VS_008: Grid Scene and Player Sprite (Phase 4 - Presentation) [Score: 85/100]

**Status**: Ready for Completion ← UPDATED 2025-09-07 12:41 (TD_005 fixed blocking bug)  
**Owner**: Dev Engineer (Final testing required) 
**Size**: L (5h code complete, ~1h scene setup remaining)
**Priority**: Critical (FOUNDATIONAL)
**Markers**: [ARCHITECTURE] [PHASE-4] [MVP]
**Created**: 2025-08-29 17:16
**Updated**: 2025-08-30 17:30

**What**: Visual grid with player sprite and click-to-move interaction
**Why**: First visible, interactive game element - validates complete MVP architecture stack

**✅ PHASE 4 CODE IMPLEMENTATION COMPLETE**:

**✅ Phase 4A: Core Presentation Layer - DELIVERED (3h actual)**
- ✅ `src/Presentation/PresenterBase.cs` - MVP base class with lifecycle hooks
- ✅ `src/Presentation/Views/IGridView.cs` - Clean grid abstraction (no Godot deps)
- ✅ `src/Presentation/Views/IActorView.cs` - Actor positioning interface  
- ✅ `src/Presentation/Presenters/GridPresenter.cs` - Full MediatR integration
- ✅ `src/Presentation/Presenters/ActorPresenter.cs` - Actor movement coordination

**✅ Phase 4B: Godot Integration Layer - DELIVERED (2h actual)**  
- ✅ `Views/GridView.cs` - TileMapLayer implementation with click detection
- ✅ `Views/ActorView.cs` - ColorRect-based actor rendering with animation
- ✅ `GameManager.cs` - Complete DI bootstrap and MVP wiring
- ✅ Click-to-move pipeline: Mouse → Grid coords → MoveActorCommand → Actor movement

**✅ QUALITY VALIDATION**:
- ✅ All 123 tests pass - Zero regression in existing functionality
- ✅ Zero Godot references in src/ folder - Clean Architecture maintained
- ✅ Proper MVP pattern - Views, Presenters, Application layer separation
- ✅ Thread-safe UI updates via CallDeferred
- ✅ Comprehensive error handling with LanguageExt Fin<T>

**🚨 BLOCKING ISSUE IDENTIFIED (2025-08-30 20:51)**:
- **Problem**: Actor movement visual update bug - blue square stays at (0,0) visually
- **Symptom**: Click-to-move shows "Success at (1, 1): Moved" but actor doesn't move visually
- **Root Cause**: ActorView.cs MoveActorAsync/MoveActorNodeDeferred visual update methods
- **Impact**: Core functionality broken - logical movement works but visual feedback fails
- **Severity**: BLOCKS all interactive gameplay testing

**🎮 PREVIOUSLY COMPLETED: GODOT SCENE SETUP**:

**Required Scene Structure**:
```
res://scenes/combat_scene.tscn
├── Node2D (CombatScene) + attach GameManager.cs
│   ├── Node2D (Grid) + attach GridView.cs  
│   │   └── TileMapLayer + [TileSet with 16x16 tiles]
│   └── Node2D (Actors) + attach ActorView.cs
```

**TileSet Configuration**:
- Import `tiles_city.png` with Filter=OFF, Mipmaps=OFF for pixel art
- Create TileSet resource with 16x16 tile size  
- Assign 4 terrain tiles for: Open, Rocky, Water, Highlight
- Update GridView.cs tile ID constants if needed

**Final Success Criteria**:
- Grid renders 10x10 tiles with professional tileset graphics
- Blue square player appears at position (0,0)  
- Click on tiles → smooth player movement via CQRS pipeline
- Console shows success/error messages for movement validation

**Dev Engineer Achievement** (2025-08-30 17:30):
- Complete MVP architecture delivered: Domain → Application → Presentation
- 8 new files implementing full interactive game foundation
- Zero architectural compromises - production-ready code quality
- Foundation established for all future tactical combat features

**Next Session**: Fix visual update bug in ActorView.cs, then complete gameplay testing

**[Owner] Decision Required** (Dev Engineer):
- Priority: HIGH - Core functionality broken
- Investigate: MoveActorAsync and MoveActorNodeDeferred methods
- Expected Fix Time: <2 hours
- Blocker Resolution: Visual position sync with logical position




## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### TD_006: Re-enable Smooth Movement Animation [Score: 25/100]
**Status**: Proposed
**Owner**: Tech Lead (Approval Required)
**Size**: S (2-4h)
**Priority**: Low (Visual Polish)
**Markers**: [ENHANCEMENT] [ANIMATION] [UI-POLISH]
**Created**: 2025-09-07 12:37

**What**: Re-enable tween-based smooth movement animation for actors
**Why**: Current implementation uses instant position changes; smooth animation improves game feel

**Problem Details**:
- Tween animation fails silently in deferred call context
- No error messages but animation doesn't execute
- Tween.Finished callback never triggers
- Currently using direct position assignment as workaround

**Technical Approach Options**:
1. Fix tween execution in CallDeferred context
2. Use Godot's AnimationPlayer node instead of tweens
3. Implement frame-based lerping in _Process method
4. Create coroutine-based movement system

**Done When**:
- Actor moves smoothly over 0.3s to target position
- Animation works reliably in all contexts
- No visual jumping or stuttering
- Maintains current functionality

**Depends On**: None - Enhancement to existing working system

**[Tech Lead] Decision** (Pending):
- Assess priority vs other work
- Choose technical approach
- Assign when core features complete





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
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*