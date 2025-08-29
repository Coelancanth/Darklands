# Darklands Development Backlog

**Last Updated**: 2025-08-29 17:13
**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 001
- **Next TD**: 004  
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

### VS_005: Grid and Player Visualization (Phase 1 - Domain)
**Status**: Proposed  
**Owner**: Tech Lead → Dev Engineer
**Size**: S (<2h)
**Priority**: Critical (FOUNDATIONAL)
**Markers**: [ARCHITECTURE] [PHASE-1] [MVP]
**Created**: 2025-08-29 17:16

**What**: Define grid system and position domain models
**Why**: Foundation for ALL combat visualization and interaction

**Domain Models**:
- `Grid` - 2D battlefield representation
- `Position` - (x,y) coordinate value object  
- `Tile` - Single grid space with terrain properties
- `Movement` - Position change with validation

**Done When**:
- Grid can be created with specified dimensions
- Positions validated within grid bounds
- Movement paths can be calculated
- 100% unit test coverage
- All tests run in <100ms

**Phase Gates**:
- Phase 1 (This): Pure domain models, no dependencies
- Phase 2 (VS_006): Movement commands and queries
- Phase 3 (VS_007): Grid state persistence
- Phase 4 (VS_008): Godot scene and sprites

**Implementation Tasks**:
1. **Create Grid domain folder** (5 min)
   - `src/Domain/Grid/`
   
2. **Implement Position value object** (20 min)
   - Immutable (x,y) coordinates
   - Factory method with validation
   - Equality and comparison operators
   
3. **Implement Tile** (20 min)
   - Position, terrain type, passability
   - Occupant tracking (Option<ActorId>)
   
4. **Implement Grid** (30 min)
   - 2D array of tiles
   - Bounds checking
   - Get/Set tile methods
   - Neighbor calculation
   
5. **Implement Movement** (30 min)
   - Path validation
   - Distance calculation (Manhattan/Euclidean)
   - Line of sight checking
   
6. **Write comprehensive tests** (30 min)
   - Grid creation and bounds
   - Position validation
   - Movement paths
   - Edge cases


### VS_006: Player Movement Commands (Phase 2 - Application)
**Status**: Proposed  
**Owner**: Tech Lead → Dev Engineer
**Size**: S (<3h)
**Priority**: Critical (FOUNDATIONAL)
**Markers**: [ARCHITECTURE] [PHASE-2] [MVP]
**Created**: 2025-08-29 17:16

**What**: Commands for player movement on grid
**Why**: Enable player interaction with grid system

**Commands & Handlers**:
- `MoveActorCommand` - Request position change
- `GetGridStateQuery` - Retrieve current grid
- `ValidateMovementQuery` - Check if move is legal
- `CalculatePathQuery` - Find path between positions

**Done When**:
- Actor can move to valid positions
- Invalid moves return proper errors (Fin<T>)
- Path finding works for simple cases
- Handler tests pass in <500ms

**Depends On**: VS_005 (Domain models)

**Implementation Tasks**:
1. **Create Application structure** (10 min)
   - `src/Application/Grid/Commands/`
   - `src/Application/Grid/Queries/`
   
2. **Define Commands** (30 min)
   - `MoveActorCommand` with ActorId, TargetPosition
   - Validation rules (bounds, passability, occupation)
   
3. **Define Queries** (30 min)
   - `GetGridStateQuery` returns grid snapshot
   - `ValidateMovementQuery` checks legality
   - `CalculatePathQuery` with simple pathfinding
   
4. **Implement Handlers** (60 min)
   - MoveActorCommandHandler with validation
   - Query handlers with grid access
   - Error handling with Fin<T>
   
5. **Write handler tests** (60 min)
   - Valid/invalid moves
   - Concurrent move handling
   - Query accuracy


### VS_007: Grid State Persistence (Phase 3 - Infrastructure)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: M (4h)
**Priority**: Critical (FOUNDATIONAL)
**Markers**: [ARCHITECTURE] [PHASE-3] [MVP]
**Created**: 2025-08-29 17:16

**What**: Persist grid and actor positions
**Why**: Enable save/load of combat state

**Infrastructure**:
- `GridRepository` - Save/load grid state
- `GridState` - Serializable grid snapshot
- JSON serialization format
- Integration with save system

**Done When**:
- Grid state persists to JSON
- Loading recreates exact grid state
- Actor positions preserved
- Integration tests verify save/load

**Depends On**: VS_006 (Application layer)

**Implementation Tasks**:
1. **Create Infrastructure structure** (10 min)
   - `src/Infrastructure/Grid/`
   - `src/Infrastructure/Grid/Repositories/`
   
2. **Define state types** (30 min)
   - `GridState` - serializable grid
   - `TileState` - serializable tile
   - `ActorPositionState` - actor locations
   
3. **Implement Repository** (90 min)
   - `IGridRepository` interface
   - `JsonGridRepository` implementation
   - Serialization logic
   
4. **Create persistence commands** (60 min)
   - `SaveGridStateCommand`
   - `LoadGridStateCommand`
   - Error handling for corruption
   
5. **Write integration tests** (60 min)
   - Full save/load cycle
   - Large grid performance
   - State corruption handling


### VS_008: Grid Scene and Player Sprite (Phase 4 - Presentation)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer  
**Size**: L (6-8h)
**Priority**: Critical (FOUNDATIONAL)
**Markers**: [ARCHITECTURE] [PHASE-4] [MVP]
**Created**: 2025-08-29 17:16

**What**: Visual grid with player sprite and click-to-move
**Why**: First visible, interactive game element

**Godot Implementation**:
- Grid scene with tilemap
- Player sprite (simple colored square initially)
- Click detection and coordinate conversion
- Movement animation
- Camera following player

**Done When**:
- Grid renders with visible tiles
- Player sprite displays at position
- Click on tile moves player
- Movement animates smoothly
- Camera follows player

**Depends On**: VS_007 (Infrastructure)

**Implementation Tasks**:
1. **Create View Interfaces** (30 min)
   - `IGridView` - grid display contract
   - `IPlayerView` - player display contract
   - Async methods for UI updates
   
2. **Implement Presenters** (90 min)
   - `GridPresenter` - orchestrates grid display
   - `PlayerPresenter` - manages player visuals
   - Event subscriptions
   
3. **Create Godot scenes** (120 min)
   - `grid.tscn` - TileMap node setup
   - `player.tscn` - Sprite2D for player
   - `combat_scene.tscn` - Combined scene
   
4. **Implement Views** (120 min)
   - `GridView.cs` - Godot grid implementation
   - `PlayerView.cs` - Sprite control
   - Click detection and coordinate mapping
   - CallDeferred for thread safety
   
5. **Add interactions** (90 min)
   - Mouse click detection
   - Grid coordinate conversion
   - Movement command dispatch
   - Animation tweening
   
6. **Manual testing** (60 min)
   - Click accuracy
   - Movement smoothness
   - Edge case handling




## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### TD_002: Fix CombatAction Terminology [Score: 1/10]
**Status**: Proposed  
**Owner**: Tech Lead → Dev Engineer
**Size**: S (<10 min)  
**Priority**: Important  
**Created**: 2025-08-29 17:09

**What**: Replace "combatant" with "Actor" in CombatAction.cs documentation
**Why**: Glossary SSOT enforcement - maintain consistent terminology
**How**: Simple find/replace in XML comments
**Done When**: All references to "combatant" replaced with "Actor"
**Complexity**: 1/10 - Documentation only change

### TD_003: Add Position to ISchedulable Interface [Score: 2/10]
**Status**: Proposed  
**Owner**: Tech Lead → Dev Engineer  
**Size**: S (<30 min)
**Priority**: Important
**Created**: 2025-08-29 17:09

**What**: Add Position property to ISchedulable for grid-based combat
**Why**: Actors need positions on combat grid per Vision requirements
**How**: 
- Add `Position Position { get; }` to ISchedulable
- Update VS_002 implementation to include Position
**Done When**: ISchedulable includes Position, VS_002 updated
**Complexity**: 2/10 - Simple interface addition
**Depends On**: VS_002 (implement together)

### TD_001: Create Development Setup Documentation [Score: 45/100]
**Status**: COMPLETE ✅  
**Owner**: DevOps Engineer (COMPLETED 2025-08-29 14:54)
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
- ✅ Setup documentation integrated into HANDBOOK.md
- ✅ Script to verify environment works (verify-environment.ps1)
- ✅ Fresh clone can be set up in <10 minutes
- ✅ All personas can follow guide successfully
- ✅ Single source of truth for all development information

**Depends On**: ~~VS_001~~ (COMPLETE 2025-08-29) - Now unblocked

**DevOps Engineer Decision** (2025-08-29 15:00):
- Consolidated setup documentation into HANDBOOK.md instead of separate SETUP.md
- Eliminated redundancy - one source of truth for all development guidance
- Setup information is now part of daily development reference
- All requirements met with improved maintainability



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