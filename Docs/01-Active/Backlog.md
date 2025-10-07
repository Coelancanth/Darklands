# Darklands Development Backlog


**Last Updated**: 2025-10-08 07:20 (Tech Lead: Finalized VS_024 - 4 post-processing steps, triple heightmap with raw rendering)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 018
- **Next VS**: 026


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

### VS_024: WorldGen Pipeline Stage 1 - Elevation Post-Processing & Normalization (WorldEngine Quality)
**Status**: Approved
**Owner**: Tech Lead → Dev Engineer
**Size**: M (~6-8h)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PIPELINE] [STAGE-1] [FOUNDATION] [WORLDENGINE-COMPLETE]

**What**: Implement Stage 1 of world generation pipeline: WorldEngine elevation post-processing (4 steps: add_noise, fill_ocean, harmonize_ocean, sea_depth) + normalization to [0, 1] range + ocean mask generation with triple-heightmap architecture preserving both original and post-processed baselines

**Why**: Foundation for ALL downstream pipeline stages (temperature, precipitation, biomes). WorldEngine post-processing produces high-quality baseline: varied terrain (noise), realistic oceans (flood-fill + smoothing), depth maps. Normalized elevation enables consistent algorithms. Preserves BOTH original native AND post-processed baselines for comparison/validation.

**How** (validated via ultra-think 2025-10-08):

**Triple-Heightmap Architecture** (KEY INSIGHT - renders at each processing stage):
```
Original Heightmap (raw native [0-20])       → ColoredOriginalElevation (native, unmodified)
Post-Processed Heightmap (raw [0-20])        → ColoredPostProcessedElevation (after 4 steps, raw range)
Post-Processed Normalized [0, 1]             → ColoredNormalizedElevation (validation) + Stages 2+
```

**Stage 1 Algorithm** (4 WorldEngine steps, ~150 lines ported):

**Step 1: Post-Processing** (4 algorithms, skip center_land + place_oceans_at_map_borders):

1. **add_noise_to_elevation()**:
   - Clone original heightmap (preserve original!)
   - Add coherent Perlin noise to CLONED heightmap
   - Prevents monotone terrain, adds variety
   - Result: Varied terrain (~10% noise variation)

2. **fill_ocean()** (BFS flood fill):
   - Start from border cells below sea level
   - Flood-fill connected regions
   - Mark all reachable cells as ocean
   - Result: `oceanMask` (true=ocean, false=land)

3. **harmonize_ocean()**:
   - Smooth ocean floor (reduce jaggedness)
   - Calculate elevation class thresholds (deep/shallow/shore)
   - Result: Realistic ocean bathymetry

4. **sea_depth()**:
   - Calculate normalized depth below sea level
   - Modulate by distance to nearest land
   - Anti-alias depth transitions
   - Result: `seaDepth` map (for future ocean rendering)

**Step 2: Normalization** (ONE normalization - only post-processed):

**Post-Processed Normalization**:
- Scan post-processed heightmap (after 4 steps above)
- Calculate min/max elevation
- Normalize: `normalized[y, x] = (postProcessed[y, x] - min) / (max - min)` → [0, 1]
- **Original heightmap NOT normalized** (render raw with quantiles)

**Step 3: Output Assembly**:
- `Heightmap` (raw native [0-20]) - unchanged, preserved for rendering original
- `PostProcessedHeightmap` (raw [0-20]) - after 4 steps, NOT normalized (for rendering post-processed)
- `NormalizedHeightmap` (normalized [0, 1]) - post-processed + normalized (for validation + Stages 2+)
- `OceanMask` - flood-filled (not simple threshold!)
- `SeaDepth` - normalized depth map (optional, for future ocean views)

**WorldGenerationResult DTO Update**:
```csharp
public record WorldGenerationResult
{
    // NATIVE BASELINE (never modified - render original)
    public float[,] Heightmap { get; init; }  // Raw [0-20] from C++ library - SACRED!

    // POST-PROCESSED RAW (Stage 1 output - render post-processed comparison)
    public float[,]? PostProcessedHeightmap { get; init; }  // Raw [0-20] after 4 steps ← NEW!

    // POST-PROCESSED NORMALIZED (validation + downstream stages)
    public float[,]? NormalizedHeightmap { get; init; }  // [0, 1] normalized post-processed ← NEW!
    public bool[,]? OceanMask { get; init; }             // Flood-filled ocean ← NEW!
    public float[,]? SeaDepth { get; init; }             // Normalized depth map (optional) ← NEW!

    public uint[,] PlatesMap { get; init; }
    public float[,]? TemperatureMap { get; init; }       // Stage 2 (VS_025)
    public PlateSimulationResult RawNativeOutput { get; init; }
}
```

**Visualization Integration** (3 colored elevation views):
1. **View Mode Enum Updates**:
   ```csharp
   public enum MapViewMode
   {
       ColoredOriginalElevation,          // Quantile colors on ORIGINAL (raw [0-20]) ← NEW!
       ColoredPostProcessedElevation,     // Quantile colors on POST-PROCESSED (raw [0-20]) ← NEW!
       ColoredNormalizedElevation,        // Quantile colors on NORMALIZED ([0, 1]) ← NEW! (validation)
       Plates,
       Temperature,                       // VS_025 will add
       SeaDepth                           // Optional: ocean depth visualization (future)
   }
   ```

2. **Renderer Changes** (WorldMapRendererNode.cs):
   - Change signature: `SetWorldData(WorldGenerationResult data)` (not PlateSimulationResult!)
   - `RenderColoredElevation()` accepts `float[,] heightmap` parameter (normalizes internally if needed)
   - `ColoredOriginalElevation` calls `RenderColoredElevation(data.Heightmap)` ← ORIGINAL RAW!
   - `ColoredPostProcessedElevation` calls `RenderColoredElevation(data.PostProcessedHeightmap!)` ← POST-PROCESSED RAW!
   - `ColoredNormalizedElevation` calls `RenderColoredElevation(data.NormalizedHeightmap!)` ← NORMALIZED!

3. **Legend Updates** (WorldMapLegendNode.cs):
   - `ColoredOriginalElevation` legend: "Original Elevation (native output, raw)"
   - `ColoredPostProcessedElevation` legend: "Post-Processed Elevation (noise + smooth oceans, raw)"
   - `ColoredNormalizedElevation` legend: "Normalized Elevation (post-processed, [0,1])"
   - ColoredPostProcessed vs ColoredNormalized should look identical (validation)

4. **Probe Updates** (WorldMapProbeNode.cs):
   - Show original (raw): `"Original: {rawHeight:F2}"`
   - Show post-processed (raw): `"PostProc: {postProcHeight:F2}"`
   - Show normalized: `"Normalized: {normalized:F3}"`
   - Show ocean status: `"Ocean: {isOcean}"` (flood-filled)
   - Show sea depth: `"Depth: {depth:F2}"` (if ocean, optional)

5. **UI Updates** (WorldMapUINode.cs):
   - Add "Colored Original" button (original raw, native baseline)
   - Add "Colored Post-Processed" button (post-processed raw, see noise/smoothing effect)
   - Add "Colored Normalized" button (normalized [0,1], validation - should match post-processed)

**Orchestrator Changes** (WorldMapOrchestratorNode.cs):
- Pass full `WorldGenerationResult` to renderer (not `RawNativeOutput`!)
- Update serialization to save/load full result

**Pipeline Changes** (GenerateWorldPipeline.cs):
```csharp
// Stage 1: WorldEngine elevation post-processing (4 steps, NO center_land/place_oceans)
var original = nativeResult.Value.Heightmap;

// Post-process (4 steps: add_noise, fill_ocean, harmonize_ocean, sea_depth)
var postProcessed = ElevationPostProcessor.Process(
    heightmap: original,  // Clone internally to preserve original!
    seaLevel: parameters.SeaLevel,
    seed: parameters.Seed);

// postProcessed contains:
//   - ProcessedHeightmap (after 4 steps, still raw [0-20] range)
//   - OceanMask (flood-filled)
//   - SeaDepth (optional)

// Normalize ONLY post-processed (original stays raw for rendering)
var normalized = NormalizeHeightmap(postProcessed.ProcessedHeightmap);

return new WorldGenerationResult(
    heightmap: original,                              // ← Keep raw native (render original)!
    platesMap: nativeResult.Value.PlatesMap,
    rawNativeOutput: nativeResult.Value,
    postProcessedHeightmap: postProcessed.ProcessedHeightmap,  // ← NEW! Raw post-processed (render)
    normalizedHeightmap: normalized,                  // ← NEW! Normalized (validation + Stages 2+)
    oceanMask: postProcessed.OceanMask,              // ← NEW! Flood-filled
    seaDepth: postProcessed.SeaDepth,                // ← NEW! Optional
    temperatureMap: null                             // ← Stage 2 (VS_025)
);
```

**Done When**:
1. ✅ **WorldEngine post-processing complete** (4 algorithms ported, ~150 lines):
   - `add_noise_to_elevation()` - Perlin variation added
   - `fill_ocean()` - BFS flood fill ocean detection
   - `harmonize_ocean()` - ocean floor smoothed
   - `sea_depth()` - depth map calculated
   - SKIPPED: center_land, place_oceans_at_map_borders (not needed)

2. ✅ **Triple heightmap working**:
   - `Heightmap` field unchanged (raw native [0-20], render original)
   - `PostProcessedHeightmap` field populated (raw [0-20], render post-processed)
   - `NormalizedHeightmap` field populated ([0, 1], validation + Stages 2+)
   - `OceanMask` field populated (flood-filled, not threshold!)
   - `SeaDepth` field populated (optional, for future ocean views)

3. ✅ **3 colored elevation views working**:
   - ColoredOriginalElevation: Quantile colors on original raw (native baseline)
   - ColoredPostProcessedElevation: Quantile colors on post-processed raw (see noise/smoothing)
   - ColoredNormalizedElevation: Quantile colors on normalized (validation - matches post-processed!)

4. ✅ **Visual validation**:
   - ColoredOriginalElevation vs ColoredPostProcessedElevation: Should differ (post-processing adds noise/smoothing)
   - ColoredPostProcessedElevation vs ColoredNormalizedElevation: Should be **identical** (proves normalization correct)
   - Visual QA: Post-processed has varied terrain (noise), smoother oceans, flood-filled ocean mask

5. ✅ **Architecture clean**:
   - Renderer accepts `WorldGenerationResult` (not `PlateSimulationResult`)
   - Orchestrator passes full pipeline output
   - `ElevationPostProcessor.cs` exists with 4 WorldEngine algorithms
   - `GenerateWorldPipeline.cs` has clear Stage 1 section with TODO Stage 2
   - All 433 tests remain GREEN

6. ✅ **Memory acceptable**:
   - +3 MB for `PostProcessedHeightmap` + `NormalizedHeightmap` + `SeaDepth` (512×512 world)
   - Total cache size ~9-12 MB (acceptable)

**Depends On**: VS_023 (GenerateWorldPipeline architecture) ✅ Complete

**Tech Lead Decision** (2025-10-08 07:20 - Final: 4 Steps + Raw Rendering):
- **Triple heightmap**: Preserve raw native (render original) + raw post-processed (render post-processed) + normalized post-processed (validation + Stages 2+). Memory cost acceptable (+3 MB).
- **Three render targets**: Original (raw [0-20]), PostProcessed (raw [0-20]), Normalized ([0, 1]).
- **View modes**: 3 colored elevation variants (OriginalColor, PostProcessedColor, NormalizedColor) - all use quantile renderer.
- **WorldEngine selective**: 4 post-processing steps included (add_noise, fill_ocean, harmonize, sea_depth). SKIP center_land + place_oceans_at_map_borders (not needed).
- **Size estimate**: M (~5-6h) due to 4 algorithm ports (~150 lines) + single normalization + 3 view modes.
- **Quality + Comparison**: Render BOTH original and post-processed (raw) for visual comparison. Normalized validates post-processing correctness.
- **Architecture shift**: Renderer signature changes to `WorldGenerationResult` - affects Orchestrator, tests (~3 files).
- **Validation strategy**:
  - Toggle OriginalColor ↔ PostProcessedColor: See post-processing impact (noise, smoother oceans)
  - Toggle PostProcessedColor ↔ NormalizedColor: Should be **identical** (proves normalization correct)
- **References**: WorldEngine `basic.py` (elevation post-processing module) - port 4 algorithms to C# `ElevationPostProcessor.cs`.
- **Next steps**: Dev Engineer implements 4 post-processing algorithms → normalization → 3 view modes → visual comparison + validation, then unblocks VS_025 (Temperature uses NormalizedHeightmap).

---

### VS_025: WorldGen Pipeline Stage 2 - Temperature Simulation
**Status**: Approved
**Owner**: Tech Lead → Dev Engineer
**Size**: S (~3-4h)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PIPELINE] [STAGE-2] [CLIMATE]

**What**: Implement Stage 2 of world generation pipeline: temperature map calculation using latitude + noise + elevation cooling, with Temperature view mode visualization

**Why**: Temperature map needed for biome classification (Stage 6) and strategic terrain decisions. Foundation for climate-based gameplay mechanics.

**How** (validated via ultra-think 2025-10-08):

**Three-Component Temperature Algorithm** (WorldEngine proven pattern):
1. **Latitude Banding** (60% of variation):
   ```csharp
   float latitude = y / (float)height;  // [0,1] (0=north pole, 0.5=equator, 1=south)
   float baseTemp = 30f * Mathf.Cos((latitude - 0.5f) * Mathf.Pi);  // [-30°C, +30°C]
   ```

2. **Coherent Noise** (25% of variation):
   ```csharp
   var noise = new SimplexNoise(seed);
   float noiseValue = noise.GetNoise2D(x * 0.01f, y * 0.01f) * 10f;  // ±10°C
   ```

3. **Elevation Cooling** (15% of variation):
   ```csharp
   float elevation = normalizedHeightmap[y, x];  // [0, 1] from Stage 1
   float elevationCooling = elevation * 30f;     // Lapse rate ~6.5°C/km
   ```

4. **Combined**:
   ```csharp
   temperatureMap[y, x] = baseTemp + noiseValue - elevationCooling;
   // Result: [-60°C, +40°C] (poles/peaks cold, equator/lowlands warm)
   ```

**Deferred Features** (YAGNI validated):
- ❌ **Heat diffusion**: Needs ocean currents for realism - local averaging is fake physics
- ❌ **Wind effects**: Belongs in Precipitation stage (affects rain shadow, not temperature)
- ✅ **Simple pattern**: WorldEngine proves 85% realism, 20× less code

**Visualization Integration** (add Temperature view):
1. **Renderer** (WorldMapRendererNode.cs):
   - Add `RenderTemperature(float[,] temperatureMap)` method
   - 5-stop color gradient:
     ```
     Blue   (-40°C) → Cyan (-20°C) → Green (0°C) → Yellow (+20°C) → Red (+40°C)
     ```

2. **Legend** (WorldMapLegendNode.cs):
   ```csharp
   case MapViewMode.Temperature:
       AddLegendEntry("Blue", ..., "-40°C (Frozen)");
       AddLegendEntry("Cyan", ..., "-20°C (Cold)");
       AddLegendEntry("Green", ..., "0°C (Mild)");
       AddLegendEntry("Yellow", ..., "+20°C (Warm)");
       AddLegendEntry("Red", ..., "+40°C (Hot)");
   ```

3. **Probe** (WorldMapProbeNode.cs):
   - Display temperature on hover: `"Temp: {temp:F1}°C"`

4. **UI** (WorldMapUINode.cs):
   - Add "Temperature" view mode button

**Pipeline Changes** (GenerateWorldPipeline.cs):
```csharp
// Stage 2: Temperature calculation
var temperatureMap = TemperatureCalculator.Calculate(
    normalizedHeightmap: result.NormalizedHeightmap!,
    width: result.Width,
    height: result.Height,
    seed: parameters.Seed);

return result with { TemperatureMap = temperatureMap };
```

**Performance** (multi-threading decision):
- ❌ **NO threading**: Native sim dominates (83% of 1.2s total), temperature only ~60ms
- ✅ Auto-cache solves iteration (0ms reload) > threading (11% savings)
- ✅ Simple = fast enough (<1.5s total for 512×512)

**Done When**:
1. ✅ **Temperature map populated**:
   - `WorldGenerationResult.TemperatureMap` has values in °C (real units)
   - Range: -60°C (high peaks at poles) to +40°C (lowlands at equator)

2. ✅ **Algorithm correct**:
   - Latitude gradient: Poles cold (-30°C base), equator warm (+30°C base)
   - Elevation cooling: Mountains colder than lowlands at same latitude
   - Noise variation: Subtle ±10°C variation (no banding artifacts)

3. ✅ **Visualization working**:
   - Temperature view mode renders 5-stop gradient
   - Legend shows 5 temperature bands with °C labels
   - Probe displays temperature on hover

4. ✅ **Quality gates**:
   - Visual validation: Poles blue, equator red, mountains blue at all latitudes
   - No performance regression (still <1.5s for 512×512 total)
   - All 433 tests remain GREEN

**Depends On**: VS_024 (Elevation Normalization) - needs `NormalizedHeightmap` for elevation cooling

**Tech Lead Decision** (2025-10-08 06:52):
- **Algorithm**: Match WorldEngine (latitude + noise + elevation). NO heat diffusion (fake physics). NO wind (wrong layer).
- **Noise inclusion**: YES - trivial cost (~10ms), prevents banding, matches proven pattern.
- **Simplicity**: 3 components = elegant, 85% realism sufficient for strategy game.
- **Performance**: Skip threading (YAGNI), cache solves iteration speed.
- **Next steps**: Dev Engineer implements after VS_024 complete, uses `NormalizedHeightmap` for elevation cooling.

---

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

**Prerequisite Issues** (now TD_012-014):
Before starting pipeline phases, fix visualization foundation technical debt discovered during testing.

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