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

### Completed_Backlog_002.md (Lines 1001-2000)
**Item Range**: Core gameplay implementation (VS_005-008, TD_005)
**Notable Items**:
- VS_005-008: Grid and Player system implementation series
- TD_005: Actor Movement Visual Update Bug
- Core presentation layer work

### Completed_Backlog_003.md (Lines 2001-3000)
**Item Range**: Mid-development items and refactoring
**Notable Items**:
- Various TD items and VS implementations
- System consolidation work
- Bug fixes and improvements

### Completed_Backlog_004.md (Lines 3001-3974)
**Item Range**: Recent development including architecture decisions
**Contains**: Most recent 974 items
**Notable Items**:
- Recent TD items (TD_049, TD_052, TD_055)
- Architecture decision implementations
- Latest bug fixes and improvements

### Completed_Backlog.md (Current Active - Empty)
**Status**: Fresh archive ready for new completions
**Next Split**: At 1000 lines → Completed_Backlog_005.md

## 🔍 How to Find Items

### By Item Number
- **Early items** (low numbers): Start with 001, then 002
- **Recent items** (high numbers): Check 004 first, then 003
- **Very recent**: May be in 004 or active file

### By Feature Area
- **Architecture Foundation**: 001 (VS_001, TD_011, Clean Architecture)
- **Vision/FOV**: 001 (VS_011, BR_007 vision-related threading)
- **Combat System**: 001 (VS_010a/b, BR_001 determinism)
- **Movement/Grid**: Check 002, 003
- **Logger System**: 001 (TD_038 complete rewrite)
- **Recent TD items**: Check 004

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

- **Total Archived Items**: ~25 major items (BR, TD, VS items)
- **Archive Files**: 4 complete + 1 active
- **Average Lines per Archive**: 1,000 (except 004: 974)
- **Largest Archive**: 1,000 lines (001-003)
- **Archive 001 Content**: Foundation architecture, vision system, combat basics
- **Current Active**: Empty (ready for new items)

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
*Last Updated: 2025-09-16 22:15 - After historical archive split*