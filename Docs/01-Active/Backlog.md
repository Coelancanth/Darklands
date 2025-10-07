# Darklands Development Backlog


**Last Updated**: 2025-10-08 04:15 (Dev Engineer: Added VS_023-029 for WorldGen foundation fixes)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 012
- **Next VS**: 030


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

**No critical items!** ✅ VS_021 completed and archived, VS_020 unblocked.

---

*Recently completed and archived (2025-10-06):*
- **VS_021**: i18n + Data-Driven Entity Infrastructure (ADR-005 + ADR-006) - 5 phases complete! Translation system (18 keys in en.csv), ActorTemplate system with GodotTemplateService, player.tres template, pre-push validation script, architecture fix (templates → Presentation layer). Bonus: Actor type logging enhancement (IPlayerContext integration). All 415 tests GREEN. ✅ (2025-10-06 16:23) *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---

*Recently completed and archived (2025-10-05):*
- **VS_019**: TileSet-Based Visual Scene + TileSet as Terrain Catalog (SSOT) - All 4 phases complete! TileMapLayer pixel art rendering (terrain), Sprite2D actors with smooth tweening, fog overlay system, 300+ line cleanup. ✅ (2025-10-05)
- **VS_019_FOLLOWUP**: Fix Wall Autotiling (Manual Edge Assignment) - Manual tile assignment for symmetric bitmasks, walls render seamlessly. ✅ (2025-10-05)
- *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

**No important items!** ✅ VS_020 completed and archived.

---

*Recently completed and archived (2025-10-06):*
- **VS_020**: Basic Combat System (Attacks & Damage) - All 4 phases complete! Click-to-attack combat UI, component pattern (Actor + HealthComponent + WeaponComponent), ExecuteAttackCommand with range validation (melee adjacent, ranged line-of-sight), damage application, death handling bug fix. All 428 tests GREEN. Ready for VS_011 (Enemy AI). ✅ (2025-10-06 19:03) *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---

*Recently completed and archived (2025-10-04 19:35):*
- **VS_007**: Time-Unit Turn Queue System - Complete 4-phase implementation with natural mode detection, 49 new tests GREEN, 6 follow-ups complete. ✅ (2025-10-04 17:38)

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*

### VS_022: World Generation Pipeline (Incremental Post-Processing)
**Status**: Proposed
**Owner**: Product Owner → Tech Lead (breakdown)
**Size**: XL (multi-phase, build incrementally)
**Priority**: Ideas
**Markers**: [WORLDGEN] [ARCHITECTURE] [INCREMENTAL]

**What**: Build post-processing pipeline on top of native plate tectonics output - elevation normalization, climate, erosion, biomes - **one algorithm at a time** with proper testing

**Why**: Current system outputs raw heightmap only. Need processed terrain data (ocean masks, climate zones, biomes) for gameplay. Clean foundation (native-only) established in refactor commit f84515d.

**Current State** (2025-10-08):
- ✅ Native library wrapper working (heightmap + plates)
- ✅ Modular visualization (5 focused nodes, ~700 lines)
- ✅ 433 tests GREEN
- ✅ Clean architecture (no premature complexity)
- ❌ No post-processing (intentional - start simple!)

**Proposed Incremental Approach:**
1. **Phase 1: Elevation Normalization** (S, ~4h)
   - Normalize raw heightmap to [0, 1] range
   - Calculate dynamic sea level threshold (Otsu's method)
   - Add ocean mask generation
   - Tests: Verify normalization, sea level calculation

2. **Phase 2: Climate - Temperature** (M, ~6h)
   - Temperature calculation (latitude + elevation cooling)
   - Temperature map visualization
   - Tests: Temperature gradient validation

3. **Phase 3: Climate - Precipitation** (M, ~6h)
   - Precipitation calculation (with rain shadow)
   - Precipitation map visualization
   - Tests: Precipitation patterns

4. **Phase 4: Hydraulic Erosion** (L, ~12h)
   - River generation (flow accumulation)
   - Valley carving around rivers
   - Lake formation
   - Tests: River connectivity, erosion effects

5. **Phase 5: Hydrology** (M, ~8h)
   - Watermap (droplet simulation)
   - Irrigation (moisture spreading)
   - Humidity (combined moisture metric)
   - Tests: Flow patterns, moisture distribution

6. **Phase 6: Biome Classification** (M, ~6h)
   - Holdridge life zones model
   - Biome map generation
   - Biome visualization + legends
   - Tests: Biome distribution validation

**Technical Principles:**
- ✅ **One algorithm at a time** - No big-bang integration
- ✅ **Test coverage for each phase** - Regression protection
- ✅ **Visual validation** - Probe + view modes for each stage
- ✅ **Algorithm independence** - Each phase self-contained
- ✅ **ADR documentation** - Capture design decisions

**References:**
- [TD_009: Pipeline Gap Analysis](../08-Learnings/WorldEngine/TD_009-Pipeline-Gap-Analysis.md) - WorldEngine algorithms inventory
- [TD_011 completion notes](../08-Learnings/WorldEngine/TD_011-completion-notes.md) - Sea level bug + cleanup lessons
- Refactor commit: `f84515d` (removed 5808 lines, modular nodes)

**Done When:**
- All 6 phases complete with tests
- Each algorithm has visual validation mode
- Biome map renders correctly
- Performance acceptable (<10s for 512×512 world)
- Documentation updated with architecture decisions

**Depends On**: None (foundation ready)

**Next Steps:**
1. Product Owner: Review and approve scope
2. Tech Lead: Break down Phase 1 into detailed tasks
3. Dev Engineer: Implement Phase 1 (elevation normalization)

**Prerequisite Issues** (VS_023-029):
Before starting pipeline phases, fix visualization foundation issues discovered during testing.

---

### VS_023: WorldMap Visualization - Dynamic Legends
**Status**: Proposed
**Owner**: Dev Engineer
**Size**: S (~2h)
**Priority**: Ideas
**Markers**: [WORLDGEN] [UI] [VISUALIZATION]

**What**: Fix WorldMapLegendNode to properly display color keys for each view mode

**Why**: Current legend renders but may not update correctly when switching views. Essential for understanding what colors mean.

**Current Issue**:
- Legend node exists but not verified working
- Need proper color swatches + labels
- Should update dynamically on view mode change

**Done When**:
- Legend displays for RawElevation (black/gray/white gradient)
- Legend displays for Plates ("Each color = unique plate")
- Legend updates when switching view modes
- Visual verification in Godot

---

### VS_024: WorldMap Visualization - Raw Elevation Colored Rendering
**Status**: Proposed
**Owner**: Dev Engineer
**Size**: S (~3h)
**Priority**: Ideas
**Markers**: [WORLDGEN] [VISUALIZATION]

**What**: Add RawElevationColored view mode using WorldEngine color gradient

**Why**: Grayscale is hard to read. WorldEngine-style coloring (blue ocean → green land → brown mountains → white peaks) is much clearer.

**Technical Approach**:
- Add `RawElevationColored` to MapViewMode enum
- Implement rendering in WorldMapRendererNode
- Use WorldEngine elevation colorization (without needing ocean mask)
- Alternative: Simple blue-to-green gradient for [0,1] normalized values

**Done When**:
- New view mode in dropdown
- Colors match WorldEngine style (or reasonable approximation)
- Visual clarity improvement verified

---

### ~~VS_025: WorldMap UI - Flexible Layout System~~ ✅ COMPLETE
**Status**: Done (2025-10-08)
**Owner**: Dev Engineer
**Size**: M (~3h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [UI] [GODOT]

**What**: Make WorldMapUINode layout configurable in Godot scene editor (not hardcoded positions)

**Why**: Current UI positions are hardcoded in C# (line 52: `Position = new Vector2(10, 10)`). Should use Godot anchors/containers for player-configurable layouts.

**Implementation Summary**:
- ✅ Moved UI to upper-right using anchor system (AnchorLeft=1, AnchorRight=1)
- ✅ Added PanelContainer for visible background
- ✅ Fixed scene hierarchy - moved UI/Legend to CanvasLayer (critical fix!)
- ✅ Removed hardcoded `Position = new Vector2(10, 10)` from C#
- ✅ UI now positioned via .tscn anchors, fully customizable in editor
- ✅ All 433 tests GREEN

**Key Insight**: Control nodes must be in CanvasLayer (not parented under Node) to render correctly in Godot's hierarchy.

**Completed**: 2025-10-08 05:02 by Dev Engineer

---

### VS_026: WorldMap Persistence - Disk Serialization
**Status**: Proposed
**Owner**: Dev Engineer
**Size**: M (~6h)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PERFORMANCE] [SERIALIZATION]

**What**: Serialize generated world data to disk by seed, quick-load if file exists (avoid regeneration)

**Why**: Generation takes 3-5 seconds for 512×512. Restarting scene with same seed should instant-load from disk cache.

**Technical Approach**:
```csharp
// WorldMapOrchestratorNode.cs
private const string CACHE_DIR = "user://worldgen_cache/";

private async Task GenerateWorldAsync(int seed)
{
    string cachePath = $"{CACHE_DIR}world_{seed}.dat";

    // Try load from disk
    if (FileAccess.FileExists(cachePath))
    {
        var cached = LoadWorldFromDisk(cachePath);
        _renderer?.SetWorldData(cached, ...);
        _logger?.LogInformation("Loaded world from cache: seed={Seed}", seed);
        return; // Instant!
    }

    // Generate new
    var result = await _mediator.Send(new GenerateWorldCommand(seed));
    SaveWorldToDisk(result.Value, cachePath);
    _logger?.LogInformation("Generated and cached world: seed={Seed}", seed);
    // ...
}

private void SaveWorldToDisk(PlateSimulationResult data, string path)
{
    using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
    // Serialize: heightmap, plates map (binary format for speed)
    file.Store32((uint)data.Width);
    file.Store32((uint)data.Height);
    // Store heightmap as binary...
    // Store plates as binary...
}
```

**Considerations**:
- File format: Binary (fast) or JSON (debuggable)? → Binary for performance
- Disk usage: ~2MB per world (512×512), manageable
- Cache invalidation: Version number in header to detect format changes
- Cache management: Auto-cleanup old files? Max cache size limit?

**Done When**:
- World serialized to `user://worldgen_cache/world_{seed}.dat`
- Loading from disk = instant (<100ms)
- Cache hit/miss logged for verification
- Works across game sessions (persistent)
- Binary format documented

---

### ~~VS_027: WorldMap Probe - Fix Cell Inspection & Highlight~~ ✅ COMPLETE
**Status**: Done (2025-10-08)
**Owner**: Dev Engineer
**Size**: M (~4h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [UI] [DEBUGGING]

**What**: Fix WorldMapProbeNode cell detection and add visual highlight preview

**Why**: Probe had coordinate transformation bug (showing 1147,760 for 512×512 map). Needed visual feedback and better UX.

**Implementation Summary**:
- ✅ Fixed coordinate transformation bug: `GetGlobalMousePosition()` instead of `GetViewport().GetMousePosition()`
- ✅ Replaced 'P' key with click-to-probe (left mouse button)
- ✅ Added yellow 1×1 pixel ColorRect highlight (follows cursor)
- ✅ Increased camera zoom limit to 20x (highlight visible at high zoom)
- ✅ Implemented hold-to-pan mode (200ms threshold, prevents highlight jitter)
- ✅ Enhanced logger output with structured data (X, Y, Elevation, PlateId, ViewMode)
- ✅ Fixed input filtering (_Input() vs _UnhandledInput() conflict)
- ✅ All 433 tests GREEN

**Technical Details**:
- Coordinate transform: `GetGlobalMousePosition()` → `AffineInverse()` → cell coords (accounts for camera zoom/pan)
- Hold-to-pan: 200ms threshold via `_Process()` timer, logs "Pan mode activated"
- Probe/Camera coordination: Probe checks `camera.IsPanning` to skip highlight updates during pan
- Input order: Both use `_Input()` for reliable event processing

**Key Bug Fix**: Used `GetViewport().GetMousePosition()` (screen space) instead of `GetGlobalMousePosition()` (world space), causing massive coordinate offsets after camera transforms.

**Completed**: 2025-10-08 05:21 by Dev Engineer

---

### ~~VS_028: WorldMap Camera - Mouse Zoom & Drag Pan~~ ✅ COMPLETE
**Status**: Done (2025-10-08)
**Owner**: Dev Engineer
**Size**: M (~3h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [UI] [CAMERA]

**What**: Add mouse wheel zoom and middle-mouse drag to pan camera

**Why**: Current Camera2D exists but has no controls. Essential for exploring large 512×512 maps.

**Implementation Summary** (Option B: Dedicated Camera Node):
- ✅ Created WorldMapCameraNode.cs - dedicated camera controller
- ✅ Mouse wheel zoom in/out with limits (0.5x min, 4.0x max)
- ✅ Middle mouse drag panning (compensated for zoom level)
- ✅ Camera reset on world regeneration
- ✅ Integrated with orchestrator, wired to Camera2D
- ✅ All 433 tests GREEN

**Technical Details**:
- Zoom speed: 0.1 (10% per scroll)
- Pan compensation: `Position -= motion.Relative / Zoom` (consistent feel at all zoom levels)
- Input handling: Uses `_UnhandledInput()` + `SetInputAsHandled()` to prevent event bubbling
- Modular design: Follows WorldGen node pattern (Renderer, Probe, UI, Camera)

**Completed**: 2025-10-08 05:06 by Dev Engineer

---

### VS_029: WorldGen Pipeline - GenerateWorldPipeline Architecture
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: M (~6h)
**Priority**: Ideas
**Markers**: [WORLDGEN] [ARCHITECTURE] [FOUNDATION]

**What**: Create GenerateWorldPipeline class that orchestrates post-processing stages, calling NativePlateSimulator directly

**Why**: Need clear architecture for incremental pipeline phases (VS_022). Pipeline should call native sim, then apply stages one by one.

**Architectural Decision**:
```csharp
// NEW: WorldGenerationResult (pipeline output)
public record WorldGenerationResult(
    float[,] Heightmap,           // Normalized [0,1]
    bool[,]? OceanMask,           // Optional (Phase 1)
    float[,]? TemperatureMap,     // Optional (Phase 2)
    // ... add fields as phases progress

    PlateSimulationResult NativeOutput  // Keep raw data
);

// NEW: GenerateWorldPipeline
public class GenerateWorldPipeline
{
    private readonly NativePlateSimulator _nativeSim;

    public Result<WorldGenerationResult> Generate(PlateSimulationParams p)
    {
        // Call native directly
        var nativeResult = _nativeSim.Generate(p);

        // Stage 1: Normalize (when Phase 1 implemented)
        // var normalized = NormalizeElevation(nativeResult.Heightmap);

        // Stage 2: Ocean mask (when Phase 1 implemented)
        // var oceanMask = CalculateOceanMask(normalized);

        return new WorldGenerationResult(
            nativeResult.Value.Heightmap,  // Raw for now
            OceanMask: null,  // TODO: Phase 1
            TemperatureMap: null,  // TODO: Phase 2
            NativeOutput: nativeResult.Value
        );
    }
}
```

**Naming Decisions**:
- ✅ Keep `PlateSimulationParams` (native library params)
- ✅ Keep `PlateSimulationResult` (raw native output)
- ✅ Add `WorldGenerationResult` (pipeline output with post-processing)
- ✅ Add `GenerateWorldPipeline` (orchestrator)

**Done When**:
- GenerateWorldPipeline class created
- Calls NativePlateSimulator directly
- Returns WorldGenerationResult with optional fields
- Clear separation: native types vs pipeline types
- Ready for Phase 1 implementation
- Update GenerateWorldCommand to use pipeline

**Depends On**: None (foundation work)

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