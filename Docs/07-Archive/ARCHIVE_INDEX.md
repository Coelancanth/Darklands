# Archive Index - Quick Reference

## 📚 Archive Organization

Archives are split when they reach **1000 lines** to maintain readability and performance.
**Current Status**: 3,974 historical items split across 4 archives + 1 active

## 📁 Current Archives

### Completed_Backlog_001.md (Lines 1-1000)
**Date Range**: 2025-08-29 to 2025-09-15
**Major Items**:
- **BR_007**: Concurrent Collection Access Error - Fixed critical threading bug with ConcurrentDictionary
- **TD_038**: Complete Logger System Rewrite - Eliminated 4+ parallel logging systems, fixed 36 test failures
- **VS_011**: Vision/FOV System - Complete shadowcasting FOV with fog of war (6h implementation)
- **VS_001**: Foundation Architecture - 3-project Clean Architecture with DI/logging/git hooks
- **BR_001**: Remove Float Math - Eliminated non-deterministic floating-point, established integer-only domain
- **TD_011**: Async/Concurrent Mismatch - Fixed fundamental async anti-pattern in turn-based game (13h fix)
- **VS_010a/b**: Actor Health & Combat System - Complete health/damage foundation with melee attacks
- **TD_001-004**: Early technical debt cleanup and API fixes

### Completed_Backlog_002.md (957 lines)
**Date Range**: 2025-09-07 to 2025-09-09
**Major Items**:
- **VS_005**: Grid and Player Visualization (Phase 1 Domain) - Complete grid system foundation
- **VS_006**: Player Movement Commands (Phase 2 Application) - CQRS implementation with MediatR
- **TD_005**: Fix Actor Movement Visual Update Bug - Critical visual sync bug resolved
- **VS_008**: Grid Scene and Player Sprite (Phase 4 Presentation) - Complete MVP architecture validation
- **TD_008**: Godot Console Serilog Sink - Rich formatting, eliminated dual logging anti-pattern
- **VS_002**: Combat Scheduler (Phase 2) - Priority queue-based timeline scheduler
- **TD_009**: Remove Position from Actor Domain Model - Clean architecture separation with ICombatQueryService
- **VS_010c**: Dummy Combat Target - Complete testing target with enhanced death system
- **TD_012**: Remove Static Callbacks - Event-driven architecture replacing static handlers
- **TD_023**: Strategic ADR Review - Created 6 new TD items (TD_024-029) for production gaps
- **TD_017**: UI Event Bus Architecture - Complete MediatR integration replacing static router
- **TD_019**: Fix embody script squash merge handling - Zero-friction automation restored
- **TD_020**: Deterministic Random Service - Foundation with property-based tests
- **TD_026**: Determinism Hardening - Production-grade with rejection sampling and FNV-1a hashing
- **TD_015**: Reduce Logging Verbosity - Production readiness, emoji removal

### Completed_Backlog_003.md (1,001 lines)
**Date Range**: 2025-09-09 to 2025-09-11
**Major Items**:
- **TD_030**: Fix Code Formatting CI/Local Inconsistency - Developer experience improvement
- **TD_021**: Save-Ready Entity Patterns (All 4 Phases) - Complete architectural milestone
- **TD_022**: Core Abstraction Services - Audio/Input/Settings with production-quality mocks
- **TD_024**: Architecture Tests for ADR Compliance - 40 total tests with NetArchTest integration
- **TD_025**: Cross-Platform Determinism CI Pipeline - GitHub Actions matrix workflow
- **TD_018**: Integration Tests for C# Event Infrastructure - 34 tests preventing DI/MediatR failures
- **TD_013**: Extract Test Data from Production Presenters - Clean IActorFactory abstraction
- **VS_011**: Vision/FOV System with Shadowcasting - Complete foundation for combat/AI/stealth
- **BR_002**: Shadowcasting FOV Edge Cases - 75% implementation deemed sufficient
- **TD_033**: Shadowcasting Edge Cases Investigation - Root cause analysis, deferred as acceptable
- **TD_034**: Consolidate HealthView into ActorView - Eliminated 790 lines of phantom code
- **TD_036**: Global Debug System - F12-toggleable debug window with runtime controls

### Completed_Backlog_004.md (200+ lines, partial)
**Date Range**: 2025-09-11 to 2025-09-16
**Major Items Visible**:
- **BR_005**: Debug Window Log Level Filtering - Fixed logging SSOT violation
- **TD_039**: Remove Task.Run Violations - Sequential execution patterns for ADR-009 compliance
- **TD_040**: Fixed-Point Math for Determinism - 16.16 Fixed-point arithmetic replacing doubles
- **TD_041**: Production-Ready DI Lifecycle Management - ConditionalWeakTable scope management
- **TD_042**: Replace Over-Engineered DDD Main - Simplified clean architecture approach
- **TD_046**: Clean Architecture Project Separation - 4-project structure with MVP enforcement
- **TD_055**: Document Real Implementation Experience - Phase completion documentation protocol
- **TD_052**: Restrict Backlog-Assistant to Archive Operations - Process clarity improvement
- **TD_049**: Size-Based Backlog Archive Protocol - Archive management at 1000-line threshold

### Completed_Backlog_005.md (Current Active - 4 items)
**Date Range**: 2025-09-17 to present
**Status**: Contains TD_047, TD_057, TD_058, VS_014 (2025-09-17)
**Major Items**:
- **TD_047**: Strategic Error Handling Boundaries - Unified Application layer to pure Fin<T> functional patterns
- **TD_057**: Fix Nested MediatR Handler Anti-Pattern - Eliminated handler-to-handler calls with IDamageService
- **TD_058**: Fix MediatR Pipeline Behavior Registration Order - ErrorHandlingBehavior now outermost wrapper
- **VS_014**: A* Pathfinding Foundation - Complete 4-phase implementation with runtime fixes and real-time preview
**Next Split**: At 1000 lines → Completed_Backlog_006.md

## 🔍 How to Find Items

### By Item Number
- **Early items** (low numbers): Start with 001, then 002
- **Recent items** (high numbers): Check 004 first, then 003
- **Very recent**: May be in 004 or active file

### By Feature Area
- **Architecture Foundation**: 001 (VS_001, TD_011, Clean Architecture), 004 (TD_046 4-project separation)
- **Vision/FOV**: 003 (VS_011 complete shadowcasting system, BR_002/TD_033 edge cases)
- **Combat System**: 001 (VS_010a/b), 002 (VS_002 Combat Scheduler, VS_010c Dummy Target)
- **Movement/Grid**: 002 (VS_005/006/008 complete grid and movement system)
- **Logger System**: 001 (TD_038 complete rewrite), 002 (TD_008 Godot Serilog sink)
- **Determinism**: 002 (TD_020 Deterministic Random, TD_026 Hardening), 004 (TD_040 Fixed-point math)
- **Event Architecture**: 002 (TD_017 UI Event Bus), 003 (TD_018 Integration Tests)
- **Save System**: 003 (TD_021 Save-Ready Entity Patterns - all 4 phases)
- **DevOps/CI**: 002 (TD_019 embody script fixes), 003 (TD_025 Cross-Platform CI, TD_030 formatting)

### Search Commands
```bash
# Search all archives for specific item
grep -r "TD_042" Docs/07-Archive/

# Search specific archive
grep "VS_001" Docs/07-Archive/Completed_Backlog_001.md

# Find which archive contains an item
grep -l "BR_007" Docs/07-Archive/Completed_Backlog_*.md
```

## 📊 Archive Statistics

- **Total Archived Items**: ~51+ major items (BR, TD, VS items)
- **Archive Files**: 4 complete + 1 active
- **Archive Sizes**:
  - **001**: 1,000 lines (Foundation & Combat)
  - **002**: 957 lines (Grid System & Event Architecture)
  - **003**: 1,001+ lines (Save System & Vision/FOV)
  - **004**: 975+ lines (Clean Architecture & Process)
  - **Current**: ~200+ lines (Error Handling)
- **Key Content**:
  - **001**: Foundation architecture, async fixes, logger rewrite
  - **002**: Complete grid/movement system, event bus, deterministic random
  - **003**: Save-ready entities, vision/FOV, abstraction services
  - **004**: 4-project separation, DDD replacement, process improvements
  - **Current**: Strategic error handling boundaries (TD_047)

## 🔄 Archive Workflow (AUTO-ROTATION)

The backlog-archiver agent automatically:
1. **Monitors** active archive size
2. **Rotates** at 1000 lines to next numbered file
3. **Creates** fresh active archive
4. **Updates** this index (manually)

## 🏷️ Quick Search Tags

Use these patterns to find related items:
- `[ARCHITECTURE]` - Major architecture decisions
- `[COMBAT]` - Combat system items
- `[VISION]` - Vision/FOV system
- `[MOVEMENT]` - Pathfinding and movement
- `[TECHNICAL-DEBT]` - TD items
- `[BUG]` - Bug reports and fixes

## 📈 Archive Health

### Current Status: ✅ HEALTHY
- All archives under 1000 lines
- Active archive ready for new items
- Search performance optimal
- Git operations fast

### Maintenance Notes
- **Last Split**: 2025-09-16 (3,974 → 4 archives)
- **Auto-Rotation**: Enabled at 1000 lines
- **Next Review**: When archive 005 is created

---
*Last Updated: 2025-09-17 10:20 - Fixed TD_047 indexing, updated backlog-assistant requirements*