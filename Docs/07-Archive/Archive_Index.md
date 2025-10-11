# Backlog Archive Index

**Last Updated**: 2025-10-11 05:28
**Current Active Archive**: Completed_Backlog_2025-10_Part4.md (Lines: 39/2000)

## Archive Files (Newest First)

### Completed_Backlog_2025-10_Part4.md (✅ ACTIVE - October 2025 Part 4)
- **Created**: 2025-10-11 05:28 (after rotation)
- **Line Count**: 186/2000
- **Date Range**: 2025-10-11 to 2025-10-11
- **Status**: ✅ Active (1814 lines remaining)
- **Extraction Status**: 0/1 extracted (1 NOT EXTRACTED ⚠️)

**Items**:
- **VS_032**: Equipment Slots System (Phases 1-4/6 Complete - Domain, Commands, Queries, Presentation layers with 40 new tests GREEN, 488 total, parent-driven data pattern 80% query reduction, atomic operations with rollback, two-handed weapon validation) [NOT EXTRACTED ⚠️]

### Completed_Backlog_2025-10_Part3.md (🔒 SEALED - October 2025 Part 3)
- **Created**: 2025-10-08 06:09 (after rotation)
- **Rotated**: 2025-10-11 05:28
- **Final Line Count**: 1779 (sealed at rotation)
- **Date Range**: 2025-10-08 to 2025-10-11
- **Status**: 🔒 Sealed (read-only)
- **Extraction Status**: 0/11 extracted (11 NOT EXTRACTED ⚠️)

**Items**:
- **TD_012**: WorldMap Visualization - Dynamic Legends (Fixed legend positioning, 7-band color key, reordered view modes with ColoredElevation as default, 433 tests GREEN) [NOT EXTRACTED ⚠️]
- **TD_013**: WorldMap Visualization - Fix Colored Elevation Rendering (Fixed quantile bug via normalization, all 7 color bands visible, matches reference implementation, 433 tests GREEN) [NOT EXTRACTED ⚠️]
- **TD_015**: WorldMap Persistence - Disk Serialization (Binary format with magic number, manual save/load UI, user://worldgen_saves/ directory, 433 tests GREEN) [NOT EXTRACTED ⚠️]
- **VS_023**: WorldGen Pipeline - GenerateWorldPipeline Architecture (Three-layer architecture Handler→Pipeline→Simulator, WorldGenerationResult DTO with optional post-processing fields, IWorldGenerationPipeline abstraction, 433 tests GREEN) [NOT EXTRACTED ⚠️]
- **TD_018**: Upgrade World Serialization to Format v2 (Post-processed data, thresholds, ocean mask, sea depth, backward compatibility, bit-packing, -45 lines orchestrator cleanup, 433 tests GREEN) [NOT EXTRACTED ⚠️]
- **VS_024**: WorldGen Pipeline Stage 1 - Elevation Post-Processing & Real-World Mapping (4 WorldEngine algorithms, quantile thresholds, dual-heightmap, meters mapping, 3 colored views, 433 tests GREEN) [NOT EXTRACTED ⚠️]
- **VS_025**: WorldGen Pipeline Stage 2 - Temperature Simulation (4-component algorithm: latitude+tilt 92%, noise 8%, distance-to-sun, mountain-cooling, 4-stage debug visualization, per-world climate variation, noise config bug fix, 447 tests GREEN) [NOT EXTRACTED ⚠️]
- **VS_026**: WorldGen Stage 3 - Base Precipitation (3-stage algorithm: noise 6 octaves → gamma curve → renormalization, WorldEngine exact match, 3-stage debug viz, quantile thresholds, 457 tests GREEN) [NOT EXTRACTED ⚠️]
- **VS_027**: WorldGen Stage 4 - Rain Shadow Effect (Latitude-based prevailing winds, orographic blocking, Sahara/Gobi/Atacama patterns, 2-stage viz, 481/482 tests GREEN 99.8%) [NOT EXTRACTED ⚠️]
- **VS_028**: WorldGen Stage 5 - Coastal Moisture Enhancement (Distance-to-ocean BFS, exponential decay, elevation resistance, maritime vs continental climates, 2-stage viz, 495/495 tests GREEN 100%) [NOT EXTRACTED ⚠️]
- **TD_019**: Inventory-First Architecture (InventoryId primary key, ActorId? OwnerId, repository redesign, 16 commands/queries updated, 543 tests GREEN, 5 Presentation files updated with 3 runtime drag-drop bugs fixed, obsolete methods removed) [NOT EXTRACTED ⚠️]

### Completed_Backlog_2025-10_Part2.md (🔒 SEALED - October 2025 Part 3)
- **Created**: 2025-10-04 14:15 (after rotation)
- **Rotated**: 2025-10-08 06:09
- **Final Line Count**: 1,340 (sealed at rotation)
- **Date Range**: 2025-10-04 to 2025-10-06
- **Status**: 🔒 Sealed (read-only)
- **Extraction Status**: 0/6 extracted (6 NOT EXTRACTED ⚠️)

**Items**:
- **TD_003**: Separate Equipment Slots from Spatial Inventory Container (EquipmentSlotNode 646 lines, InventoryRenderHelper 256 lines, renamed InventoryContainerNode, 359 tests GREEN) [NOT EXTRACTED ⚠️]
- **VS_007**: Time-Unit Turn Queue System (4-phase implementation, 49 new tests, 6 follow-ups complete: vision constants, FOV-based combat exit, movement cost 10 units, production log formatting) [NOT EXTRACTED ⚠️]
- **VS_019**: TileSet-Based Visual Scene + TileSet as Terrain Catalog (SSOT) (4 phases complete, TileMapLayer rendering, Sprite2D actors, fog system, 300+ line cleanup, commits: f64c7de, 59159e5, d9d9a4d, 27b62b2, 896f6d5) [NOT EXTRACTED ⚠️]
- **VS_019_FOLLOWUP**: Fix Wall Autotiling (Manual Edge Assignment) (Manual tile assignment for symmetric bitmasks, position-based logic, visual symmetry achieved, commit: 0885cbd) [NOT EXTRACTED ⚠️]
- **VS_021**: i18n + Data-Driven Entity Infrastructure (ADR-005 + ADR-006) (5 phases complete, 18 translation keys, ActorTemplate system, validation scripts, architecture fix to Presentation layer, 7 commits, 415 tests GREEN) [NOT EXTRACTED ⚠️]
- **VS_020**: Basic Combat System (Attacks & Damage) (4 phases complete, click-to-attack UI, component pattern, ExecuteAttackCommand, range/LOS validation, death handling bug fix, 428 tests GREEN) [NOT EXTRACTED ⚠️]

### Completed_Backlog_2025-10.md (🔒 SEALED - October 2025 Part 2)
- **Created**: 2025-10-02 12:17 (after rotation)
- **Rotated**: 2025-10-04 14:15
- **Final Line Count**: 1,218 (sealed at rotation)
- **Date Range**: 2025-10-02 to 2025-10-04
- **Status**: 🔒 Sealed (read-only)
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
2. Open that specific archive file (newest first: Completed_Backlog_2025-10_Part2.md)
3. Use Ctrl+F to search for item ID

**Current Active Capacity**: 186/2000 lines (1814 lines remaining)
**Total Archived Items**: 32 (29 in Oct 2025, 3 in Sept 2025)
**Archive Files**: 6 total (1 active, 5 sealed)