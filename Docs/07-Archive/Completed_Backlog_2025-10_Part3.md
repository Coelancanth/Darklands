# Darklands Development Archive - October 2025

**⚠️ CRITICAL: This is an APPEND-ONLY archive. Never delete or overwrite existing entries.**

**Purpose**: Completed and rejected work items for historical reference and lessons learned.

**Created**: 2025-10-08
**Archive Period**: October 2025 (Part 4)
**Previous Archive**: Completed_Backlog_2025-10_Part2.md

## Archive Protocol

### Extraction Status
Items are moved here COMPLETE with all context, then marked for extraction:
- **NOT EXTRACTED** ⚠️ - Full context preserved, patterns not yet extracted
- **PARTIALLY EXTRACTED** 🔄 - Some learnings captured in ADRs/HANDBOOK
- **FULLY EXTRACTED** ✅ - All valuable patterns documented elsewhere

### Format for Completed Items
```markdown
### [Type]_[Number]: Title
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: Date
**Archive Note**: [One-line summary of achievement]
---
[PASTE ENTIRE ORIGINAL ITEM HERE - PRESERVE EVERYTHING]
---
**Extraction Targets**:
- [ ] ADR needed for: [architectural decision]
- [ ] HANDBOOK update: [pattern to document]
- [ ] Test pattern: [testing approach to capture]
```

## Format for Rejected Items
```markdown
### [Type]_[Number]: Title ❌ REJECTED
**Rejected**: Date
**Reason**: Why rejected
**Alternative**: What we did instead
[RESURRECT-IF: Specific conditions that would make this relevant]
```

---

## Completed Items (October 2025 - Part 4)

### TD_012: WorldMap Visualization - Dynamic Legends
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08
**Archive Note**: Fixed WorldMapLegendNode to properly display color keys for each view mode, moved to upper-left with PanelContainer, reordered view modes (ColoredElevation as default), dynamic legend content per view mode (RawElevation 3-band, ColoredElevation 7-band terrain gradient, Plates 10-color). All 433 tests GREEN.

---

**Status**: Done (2025-10-08)
**Owner**: Dev Engineer
**Size**: S (~2h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [UI] [VISUALIZATION] [TECHNICAL-DEBT]

**What**: Fix WorldMapLegendNode to properly display color keys for each view mode, move to upper-left, reorder view modes with ColoredElevation as default

**Why**: Current legend renders but not optimally positioned. Essential for understanding terrain colors. User requested ColoredElevation as primary view.

**Implementation Summary**:
- ✅ Moved legend to upper-left corner (anchor system: 10px from top-left)
- ✅ Added PanelContainer background for visibility
- ✅ Implemented dynamic legend content per view mode:
  - RawElevation: 3-band grayscale (black/gray/white)
  - ColoredElevation: **7-band terrain gradient** (deep ocean → peaks)
  - Plates: "Each color = unique plate" (10 plates)
- ✅ Removed Plates from UI dropdown (kept ColoredElevation + RawElevation only)
- ✅ Reordered dropdown: ColoredElevation first, RawElevation second
- ✅ Changed default view mode to ColoredElevation in all nodes
- ✅ Legend updates dynamically when switching views
- ✅ All 433 tests GREEN

**Color Legend Details** (ColoredElevation):
1. Deep Blue → Deep ocean
2. Blue → Ocean
3. Cyan → Shallow water
4. Green → Grass/Lowlands
5. Yellow-Green → Hills
6. Yellow → Mountains
7. Brown → Peaks

**Completed**: 2025-10-08 05:53 by Dev Engineer

---

**Extraction Targets**:
- [ ] HANDBOOK update: Dynamic UI content pattern (legend updates based on view mode)
- [ ] HANDBOOK update: Godot anchor system usage (position UI via anchors, not hardcoded coords)
- [ ] Reference implementation: WorldMapLegendNode as template for mode-dependent UI
- [ ] UX pattern: Color legend design for scientific visualization (quantile-based terrain mapping)

---

### TD_013: WorldMap Visualization - Fix Colored Elevation Rendering
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08
**Archive Note**: Fixed colored elevation view bug - quantile-based color mapping used raw heightmap values instead of normalized [0,1] range, causing only 2 visible bands (ocean/land). Added normalization step before quantile calculation. All 7 color bands now visible (deep ocean → peaks). Matches map_drawing.cpp reference implementation exactly. All 433 tests GREEN.

---

**Status**: Done (2025-10-08)
**Owner**: Dev Engineer
**Size**: S (~2h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [VISUALIZATION] [TECHNICAL-DEBT] [BUG]

**What**: Fix colored elevation view - currently shows only blue/brown, missing intermediate colors (cyan, green, yellow)

**Why**: Quantile-based color mapping implementation has a bug. Only shows ocean (blue) and land (brown), no intermediate terrain colors (shallows, grass, hills, mountains).

**Root Cause Found**:
- ColoredElevation used **raw heightmap values** directly (e.g., [-0.3, 1.6] range)
- Reference implementation expects **normalized [0, 1]** range
- Quantiles collapsed to only 2 visible bands (ocean/land) instead of 7
- First gradient band used hardcoded `0.0f` min, but heightmap could be negative!

**Implementation Summary**:
- ✅ Added normalization step (find min/max, normalize to [0,1]) before quantile calculation
- ✅ Created `normalizedHeightmap` array for quantile processing
- ✅ Quantiles now calculated on normalized data (matches reference behavior)
- ✅ All 7 color bands now visible: deep ocean → ocean → shallows → grass → hills → mountains → peaks
- ✅ Matches `map_drawing.cpp` reference implementation exactly
- ✅ All 433 tests GREEN

**Key Insight**:
The bug was architectural - raw vs normalized data mismatch. `RawElevation` renderer already normalized correctly, but `ColoredElevation` skipped this step. Fix: Apply same normalization pattern.

**Completed**: 2025-10-08 05:48 by Dev Engineer

---

**Extraction Targets**:
- [ ] ADR needed for: Data normalization requirement for quantile-based visualization
- [ ] HANDBOOK update: Quantile calculation pattern (always normalize input data first)
- [ ] HANDBOOK update: Root cause analysis - architectural mismatches (raw vs normalized data)
- [ ] Test pattern: Visual rendering validation (reference implementation comparison)
- [ ] Bug pattern: Hardcoded assumptions in gradient mapping (0.0 min breaks with negative values)

---

### TD_015: WorldMap Persistence - Disk Serialization
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08
**Archive Note**: Binary serialization/deserialization of world data with manual save/load UI. Binary format (~2.1 MB per 512×512 world) with magic number "DWLD" and version header. Created WorldMapSerializationService, added Save/Load buttons to WorldMapUINode. Save directory: user://worldgen_saves/, filename convention: world_{seed}.dwld. Format validation working, status feedback in UI. All 433 tests GREEN.

---

**Status**: Done (2025-10-08)
**Owner**: Dev Engineer
**Size**: M (~4h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PERFORMANCE] [SERIALIZATION] [TECHNICAL-DEBT]

**What**: Binary serialization/deserialization of world data with manual save/load UI

**Why**: Users can save generated worlds to disk and reload them later (testing, iteration, sharing seeds)

**Implementation Approach** (NO auto-cache):
- **Manual save/load** buttons in UI (user-triggered, not automatic)
- **Binary format** for compact storage (~2.1 MB per 512×512 world)
- **Versioned format** with magic number for validation
- **Simple workflow**: Generate → Save → Load later

**Binary Format**:
```
Header (16 bytes):
- Magic: "DWLD" (4 bytes) - File type identifier
- Version: uint32 (4 bytes) - Format versioning
- Seed: int32 (4 bytes) - Original generation seed
- Reserved: 4 bytes - Future expansion

Data Section:
- Width/Height: uint32 each (8 bytes)
- Heightmap: float[h, w] row-major (4 bytes × cells)
- PlatesMap: uint[h, w] row-major (4 bytes × cells)
```

**Implementation Summary**:
- ✅ Created WorldMapSerializationService (binary I/O)
- ✅ Added Save/Load buttons to WorldMapUINode (horizontal layout)
- ✅ Wired signals in WorldMapOrchestratorNode
- ✅ Save directory: `user://worldgen_saves/` (auto-created)
- ✅ Filename convention: `world_{seed}.dwld`
- ✅ Format validation (magic number, version check)
- ✅ Status feedback in UI ("Saved: world_42.dwld")
- ✅ All 433 tests GREEN

**User Workflow**:
1. Generate world (seed=42, 3-5s wait)
2. Click "Save World" → `user://worldgen_saves/world_42.dwld` created (~2.1 MB)
3. Close/reopen scene
4. Click "Load World" → Instant load from disk (~100ms)

**Why No Auto-Cache**:
- Simpler: User explicitly saves what they want to keep
- Clearer: No hidden cache management/eviction logic
- Flexible: User controls disk usage (delete .dwld files to clean up)

**Completed**: 2025-10-08 06:05 by Dev Engineer

---

**Extraction Targets**:
- [ ] ADR needed for: Manual save/load vs auto-cache architecture (explicit user control)
- [ ] ADR needed for: Binary format design (versioned header, magic number validation)
- [ ] HANDBOOK update: Godot file I/O pattern (user:// directory for persistent storage)
- [ ] HANDBOOK update: Binary serialization pattern (row-major array layout, format versioning)
- [ ] Reference implementation: WorldMapSerializationService as template for game state persistence
- [ ] UX pattern: Manual save/load workflow (user-controlled persistence, no hidden caching)
- [ ] Performance insight: Binary vs text formats (~2.1 MB binary vs ~10+ MB CSV for 512×512 world)

---
