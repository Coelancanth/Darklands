# Backlog Archive Index

**Last Updated**: 2025-10-04 12:47
**Current Active Archive**: Completed_Backlog_2025-10.md (Lines: 1219/1000)

## Archive Files (Newest First)

### Completed_Backlog_2025-10.md (✅ ACTIVE - October 2025 Part 2)
- **Created**: 2025-10-02 12:17 (after rotation)
- **Line Count**: 1219/1000 - ROTATION NEEDED ⚠️
- **Date Range**: 2025-10-02 to 2025-10-04
- **Status**: ✅ Active (accepting new items, rotation recommended)
- **Extraction Status**: 0/10 extracted (10 NOT EXTRACTED ⚠️)

**Items**:
- **VS_008**: Slot-Based Inventory System (20 slots, ItemId pattern, 23 tests, PR #84 merged) [NOT EXTRACTED ⚠️]
- **VS_009**: Item Definition System (TileSet metadata-driven, 57 tests, TextureRect rendering, auto-discovery) [NOT EXTRACTED ⚠️]
- **VS_018**: Spatial Inventory System - L-Shapes (4-phase implementation, 359 tests, ItemShape encoding, OccupiedCells collision) [NOT EXTRACTED ⚠️]
- **BR_003**: L-Shape Collision Bug (shape conversion to rectangles) [NOT EXTRACTED ⚠️]
- **BR_004**: Presentation Layer Validation Duplication (architectural violation, 200+ lines removed) [NOT EXTRACTED ⚠️]
- **BR_005**: Cross-Container L-Shape Highlight Inaccuracy (ItemDto evolution to Phase 4) [NOT EXTRACTED ⚠️]
- **BR_006**: Cross-Container Rotation Highlights (mouse warp hack for UI updates) [NOT EXTRACTED ⚠️]
- **BR_007**: Equipment Slot Visual Issues (1×1 highlight override, sprite centering) [NOT EXTRACTED ⚠️]
- **TD_004**: Move ALL Shape Logic to Core (SSOT) (7 leaks eliminated, 164 lines removed, cache anti-pattern fixed, ADR-002 updated) [NOT EXTRACTED ⚠️]
- **TD_005**: Persona & Protocol Updates (Root Cause First, UX Pattern Recognition, Requirement Clarification added to dev-engineer.md) [NOT EXTRACTED ⚠️]

### Completed_Backlog_2025-10_Part1.md (🔒 SEALED - October 2025 Part 1)
- **Created**: 2025-10-01 00:48
- **Rotated**: 2025-10-02 12:17
- **Final Line Count**: 1,072 (sealed at rotation)
- **Date Range**: 2025-10-01 to 2025-10-01 20:37
- **Status**: 🔒 Sealed (read-only)
- **Extraction Status**: 1/8 partially extracted (7 NOT EXTRACTED ⚠️, 1 PARTIALLY EXTRACTED 🔄)

**Items**:
- **VS_001**: Health System Walking Skeleton (phased implementation pattern, 101 tests, 5 bugs fixed) [PARTIALLY EXTRACTED 🔄]
- **BR_001**: Race Condition in HealthComponent Mutations (WithComponentLock pattern) [NOT EXTRACTED ⚠️]
- **BR_002**: Fire-and-Forget Event Publishing (async/await fix) [NOT EXTRACTED ⚠️]
- **BR_003**: Heal Button Bypasses CQRS (YAGNI deletion + 2 bonus bugs) [NOT EXTRACTED ⚠️]
- **TD_001**: Architecture Enforcement Tests (NetArchTest + 10 tests enforcing 4 ADRs) [NOT EXTRACTED ⚠️]
- **VS_005**: Grid, FOV & Terrain System (custom shadowcasting, 189 tests, event-driven Godot integration) [NOT EXTRACTED ⚠️]
- **VS_006**: Interactive Movement System (A* pathfinding, hover preview, fog of war, 215 tests) [NOT EXTRACTED ⚠️]
- **TD_002**: Debug Console Scene Refactor (scene-based UI, pause isolation, ILogger integration) [NOT EXTRACTED ⚠️]

### Completed_Backlog.md (📦 ARCHIVED - September 2025)
- **Created**: 2025-09-30
- **Line Count**: 1173/1000 (over capacity - READ ONLY)
- **Date Range**: 2025-09-30 to 2025-09-30
- **Status**: 📦 Archived (rotated out, read-only reference)

**Items**:
- **VS_002**: Infrastructure - Dependency Injection Foundation (DI container with GameStrapper, ServiceLocator bridge for Godot)
- **VS_003**: Infrastructure - Logging System with Category-Based Filtering (Serilog + 3 sinks + F12 debug console)
- **VS_004**: Infrastructure - Event Bus System (GodotEventBus bridges MediatR to Godot UI, validates ADR-002)

## Quick Reference

**Find an Item**:
1. Check Archive_Index.md for which file contains the item
2. Open that specific archive file (newest first: Completed_Backlog_2025-10.md)
3. Use Ctrl+F to search for item ID

**Current Active Capacity**: 1219/1000 lines (OVER CAPACITY - rotation needed)
**Total Archived Items**: 21 (18 in Oct 2025, 3 in Sept 2025)