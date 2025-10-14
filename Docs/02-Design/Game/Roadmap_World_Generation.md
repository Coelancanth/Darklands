# World Generation Roadmap

**Purpose**: Detailed technical roadmap for Darklands world generation system - Dwarf Fortress-inspired procedural worldgen with physics-driven simulation.

**Last Updated**: 2025-10-09 01:09 (Tech Lead: Created standalone worldgen roadmap with 3-phase pipeline + DF features)

**Parent Document**: [Roadmap.md](Roadmap.md#world-generation) - Main project roadmap

---

## Quick Navigation

**Core Pipeline (Phase 1)**:
- [Vision & Philosophy](#vision--philosophy)
- [Current State](#current-state)
- [Stage 1: Tectonic Foundation](#stage-1-tectonic-foundation)
- [Stage 2: Atmospheric Climate](#stage-2-atmospheric-climate)
- [Stage 3: Hydrological Processes](#stage-3-hydrological-processes)
- [Stage 4: Biome Classification](#stage-4-biome-classification)

**Extensions (Phase 2-3)**:
- [Phase 2: Hydrology Extensions](#phase-2-hydrology-extensions-quick-wins)
- [Phase 3: Geology & Resources](#phase-3-geology--resources-df-inspired-depth)
- [Standalone Worldgen Game](#standalone-worldgen-game-potential)
- [Strategic Layer Integration](#strategic-layer-integration)

---

## Vision & Philosophy

**Vision**: Dwarf Fortress-inspired procedural worldgen with physics-driven simulation (plate tectonics → geology → climate → hydrology → biomes → resources). Designed as **standalone game** foundation + Darklands strategic layer integration.

**Philosophy**:
- **Incremental pipeline** - One algorithm at a time, fully tested and visualized before moving to next stage
- **Physics-first** - Realistic causality (elevation affects temperature, temperature affects precipitation, precipitation creates rivers)
- **Designer empowerment** - Visual debugging (multi-stage view modes, probe, legends) for rapid iteration
- **Modular architecture** - Each stage standalone, no big-bang integration

**Design Principles**:
1. **Prove the problem exists** - Don't add features until we see generated worlds need them
2. **WorldEngine validation** - Use proven algorithms where available, innovate only when necessary
3. **Performance budget** - <2s for 512×512 world generation (fast iteration)
4. **Seed reproducibility** - Same seed = same world (balanced gameplay, bug reproduction)

---

## Current State

**Completed Stages**:
- ✅ **Stage 1**: Plate Tectonics & Elevation (VS_024) - Native C++ integration, dual-heightmap architecture
- ✅ **Stage 2a-2c**: Atmospheric Climate (VS_025-027) - Temperature, base precipitation, rain shadow
- 🔄 **Stage 2d**: Coastal Moisture (VS_028) - IN PROGRESS (completes atmospheric climate)

**Test Coverage**: 481/482 tests GREEN (99.8% pass rate)

**Performance**: <1.5s for 512×512 world (native sim 1.0s + post-processing 0.5s)

**Next Priority**: Complete Phase 1 core pipeline (VS_028 → VS_029 → Watermap → Humidity → Biomes)

---

## Three-Phase Roadmap

```
┌─────────────────────────────────────────────────────────────┐
│ PHASE 1: CORE PIPELINE (MVP - Current Focus)               │
│ Goal: Realistic worlds with climate + rivers + biomes      │
│ Timeline: 2-3 weeks                                         │
│ Status: 70% complete (Stage 1-2 done, Stage 3-4 remaining) │
└─────────────────────────────────────────────────────────────┘

Stage 1: Tectonic Foundation ✅ COMPLETE
Stage 2: Atmospheric Climate ✅ 75% COMPLETE (coastal moisture remaining)
Stage 3: Hydrological Processes 🔄 NEXT (VS_029: erosion + rivers)
Stage 4: Biome Classification ⏳ PLANNED (48 biome types)

┌─────────────────────────────────────────────────────────────┐
│ PHASE 2: HYDROLOGY EXTENSIONS (Quick Wins)                 │
│ Goal: Enhanced water features (swamps, creek visualization)│
│ Timeline: 1 week                                            │
│ Status: Not started (blocked by Phase 1)                   │
└─────────────────────────────────────────────────────────────┘

- Swamp biome classification (low elevation + high humidity + flat terrain)
- Creek/river/main river visual distinction (line thickness, colors)
- Slope map calculation (foundation for thermal erosion + swamp detection)

┌─────────────────────────────────────────────────────────────┐
│ PHASE 3: GEOLOGY & RESOURCES (DF-Inspired Depth)           │
│ Goal: Standalone worldgen game features                    │
│ Timeline: 3-4 weeks                                         │
│ Status: Not started (design phase)                         │
└─────────────────────────────────────────────────────────────┘

Stage 0: Geological Foundation (NEW)
  - Plate Boundary Detection
  - Volcanic System
  - Geology Layers (rock types)
  - Ore Veins & Minerals
  - Thermal Erosion

Stage 5: Extended Biome Types
  - Volcanic biomes
  - Mineral-rich biomes
  - Swamp subtypes

Stage 6: Resource Distribution
  - Mineral deposits (geology-based)
  - Vegetation resources (biome-based)
  - Strategic resources
```

---

## Stage 1: Tectonic Foundation

### VS_024: Plate Tectonics & Elevation Post-Processing ✅ COMPLETE

**Status**: Complete (2025-10-08) | **Size**: M (~6h) | **Tests**: 433 passing
**Owner**: Dev Engineer

**Delivered**:
- Native plate tectonics simulation (C++ WorldEngine integration via P/Invoke)
- Elevation post-processing (quantile-based normalization + Gaussian smoothing)
- Dual-stage debug visualization (Original vs Post-Processed heightmaps)
- Serialization system (Format v2 with backward compatibility)
- WorldMapOrchestratorNode architecture (5 modular nodes, ~700 lines)

**Key Architecture Decisions**:
- **Native simulation**: 83% of generation time - C# port not worth complexity
- **Quantile normalization**: Adapts to per-world elevation distribution (realistic thresholds per world)
- **Dual visualization**: Original vs Post-Processed (trivial debugging of normalization effects)
- **Format v2 cache**: Stores both raw + processed elevation (0ms reload for iteration)

**Implementation Phases**:
- **Phase 1**: Native simulation integration (C++ DLL, P/Invoke marshalling, pointer safety)
- **Phase 2**: Elevation post-processing (quantile thresholds: sea/hill/mountain/peak, Gaussian smoothing)
- **Phase 3**: Multi-stage visualization (2 view modes, legends with real-world meters, probe with threshold display, UI dropdown)
- **Phase 4**: Serialization system (Format v2 cache with version detection, backward compat with v1)

**Outputs**:
- `OriginalHeightmap` - Raw plate tectonics output [0.1-20 RAW units]
- `PostProcessedHeightmap` - Smoothed + normalized [0.1-20 RAW units]
- `ElevationThresholds` - Quantile-based per-world (SeaLevel, HillLevel, MountainLevel, PeakLevel)
- `OceanMask` - BFS flood-fill from borders (bool[,])

**Performance**: <1.5s for 512×512 world (native sim 1.0s + post-processing 0.2s + visualization 0.3s)

**🐛 Known Issues** (Discovered 2025-10-13):

**Issue 1: Ocean Mask Missing Inland Water**
- **Problem**: `FillOcean()` algorithm uses BFS flood-fill from map **borders only**, intentionally excluding landlocked seas/lakes
- **Impact**: River flow algorithms need ALL water bodies (ocean + lakes), not just border-connected ocean
- **Root Cause**: `ElevationPostProcessor.cs:161` comment states: *"Ensures only cells connected to border oceans are marked as ocean (prevents landlocked seas)"*
- **Decision**: Defer fix until VS_029 (erosion/rivers) - will implement **Option 2: Separate Ocean/Water Masks**

**Planned Fix (Option 2 - Proper Architecture)**:
```csharp
public record PostProcessingResult {
    public bool[,] OceanMask { get; init; }    // Border-connected ocean ONLY (for gameplay distinction)
    public bool[,] WaterMask { get; init; }    // Ocean + enclosed lakes (for river flow algorithms)
    // ...
}
```

**Issue 2: Ocean Mask Stale After Harmonization** ✅ FIXED (2025-10-13)
- **Problem**: `FillOcean()` runs on line 83, but `HarmonizeOcean()` (line 86) modifies heightmap AFTER mask creation
- **Impact**: Cells raised above threshold by smoothing remain marked as ocean (visual/probe mismatch)
- **Example**: Cell at elevation 0.29 → smoothed to 1.6 → 160m above sea level BUT still marked as ocean
- **Root Cause**: Ocean mask created before final heightmap state
- **Fix Applied**: Re-run `FillOcean()` after `HarmonizeOcean()` to reflect final heightmap
```csharp
// Algorithm 2: Flood-fill ocean detection (preliminary pass)
var oceanMask = FillOcean(heightmap, seaLevel);

// Algorithm 3: Smooth ocean floor (modifies elevations!)
HarmonizeOcean(heightmap, oceanMask);

// Algorithm 2B: Re-run flood-fill AFTER harmonization (NEW)
oceanMask = FillOcean(heightmap, seaLevel);  // Now accurate!
```

**Validation Tool Added**: `MapViewMode.OceanMask` debug view (binary blue/white visualization) exposes both issues clearly.

**Archive**: [Completed_Backlog_2025-10_Part2.md](../../07-Archive/Completed_Backlog_2025-10_Part2.md) (search "VS_024")

---

## Stage 2: Atmospheric Climate

**Goal**: Complete precipitation pipeline with all geographic effects applied (ready for erosion/rivers)

**Philosophy**: Separate atmospheric processes (instantaneous) from geological processes (millions of years)

### VS_025: Temperature Simulation ✅ COMPLETE

**Status**: Complete (2025-10-08) | **Size**: S (~5h) | **Tests**: 447 passing
**Owner**: Dev Engineer

**Delivered**:
- Physics-based temperature calculation (4-component algorithm)
- 4-stage debug visualization (isolate each component for validation)
- Per-world climate variation (hot/cold planets, shifted equator via axial tilt)
- Normalized [0,1] output for biome classification

**Temperature Formula** (WorldEngine-validated):
```csharp
// 1. Latitude factor (92%) with axial tilt + distance-to-sun
float latitudeFactor = Interp(y_scaled, [tilt-0.5, tilt, tilt+0.5], [0, 1, 0]);

// 2. Coherent noise (8%) - FBm fractal for climate variation
float n = FastNoiseLite.GetNoise2D(...);  // OpenSimplex2, 8 octaves, freq=128.0

// 3. Combined base temperature
float t = (latitudeFactor * 12f + n * 1f) / 13f / distanceToSun;

// 4. Mountain-only cooling (RAW elevation > MountainLevel threshold)
if (rawElevation > mountainLevel) {
    t *= altitude_factor;  // 0.033 at extreme peaks (97% cooling)
}
```

**Key Insights**:
- **4-stage visualization**: LatitudeOnly → WithNoise → WithDistance → Final (isolates each component)
- **Per-world parameters**: AxialTilt (0.4-0.6, Gaussian) + DistanceToSun (0.8-1.2, Gaussian) = planet variety
- **RAW elevation**: Uses PostProcessedHeightmap raw values [0.1-20], not normalized [0,1]
- **Normalized output**: [0,1] for biome classification, UI converts to °C via TemperatureMapper

**Outputs**:
- `LatitudeOnlyTemperatureMap` - Pure latitude bands [0,1]
- `WithNoiseTemperatureMap` - + coherent noise variation [0,1]
- `WithDistanceTemperatureMap` - + distance-to-sun factor [0,1]
- `FinalTemperatureMap` - + mountain cooling [0,1]

**Archive**: [Backlog.md](../../01-Active/Backlog.md) (search "VS_025")

---

### VS_026: Base Precipitation (Noise + Temperature Curve) ✅ COMPLETE

**Status**: Complete (2025-10-08) | **Size**: S (~3.5h) | **Tests**: 457 passing
**Owner**: Dev Engineer

**Delivered**:
- 3-stage precipitation algorithm (WorldEngine exact port)
- Multi-stage debug visualization (isolate noise vs temperature shaping)
- Quantile-based thresholds (adaptive per-world classification)
- Physics-validated gamma curve (cold = less evaporation)

**Three-Stage Algorithm** (WorldEngine `precipitation.py`):
```csharp
// Stage 1: Base noise field (6 octaves coherent noise)
float n = FastNoiseLite.GetNoise2D(x * n_scale, y * n_scale);  // OpenSimplex2, freq=384
float baseNoise = (n + 1.0f) * 0.5f;  // [-1,1] → [0,1]

// Stage 2: Temperature gamma curve (physics-based shaping)
float t = temperatureMap[y, x];  // [0,1] from VS_025
float gamma = 2.0f;              // Quadratic relationship (WorldEngine default)
float curveBonus = 0.2f;         // Minimum 20% precip at polar regions
float curve = MathF.Pow(t, gamma) * (1.0f - curveBonus) + curveBonus;
float tempShaped = baseNoise * curve;  // Arctic: 20%, Tropical: 100%

// Stage 3: Renormalization (stretch to [0,1] after temperature shaping)
float min = tempShaped.Min(), max = tempShaped.Max();
float final = (tempShaped - min) / (max - min);  // Restore full dynamic range
```

**Key Physics**:
- **Gamma curve**: Cold air holds less moisture (quadratic relationship t²)
- **Curve bonus**: Prevents zero precipitation in Arctic (realistic - even poles have snow!)
- **Renormalization**: Restores full [0,1] range after temperature compression

**Outputs**:
- `NoiseOnlyPrecipitationMap` - Pure coherent noise [0,1]
- `TemperatureShapedPrecipitationMap` - × gamma curve [compressed range]
- `FinalBasePrecipitationMap` - Renormalized [0,1]
- `PrecipitationThresholds` - Quantile-based (Low: 30th, Medium: 70th, High: 95th percentiles)

**Archive**: [Backlog.md](../../01-Active/Backlog.md) (search "VS_026")

---

### VS_027: Rain Shadow Effect (Orographic Blocking) ✅ COMPLETE

**Status**: Complete (2025-10-08) | **Size**: S (~3h) | **Tests**: 481/482 GREEN (99.8%)
**Owner**: Dev Engineer

**Delivered**:
- Latitude-based prevailing winds (Polar Easterlies / Westerlies / Trade Winds)
- Orographic blocking via upwind mountain trace (max 20 cells ≈ 1000km)
- Accumulative precipitation reduction (5% per mountain, max 80% blocking)
- Real-world desert patterns validated (Sahara, Gobi, Atacama)

**Two-Stage Algorithm** (Earth's atmospheric circulation):
```csharp
// 1. Get prevailing wind for latitude (KEY: per-row wind direction!)
float normalizedLatitude = (float)y / (height - 1);  // 0=South Pole, 0.5=Equator, 1=North Pole
Vector2 wind = PrevailingWinds.GetWindDirection(normalizedLatitude);
// Polar Easterlies (60°-90°): westward (-1, 0)
// Westerlies (30°-60°): eastward (+1, 0)
// Trade Winds (0°-30°): westward (-1, 0)

// 2. Trace UPWIND for mountain barriers (max 20 cells)
for (int step = 1; step <= 20; step++) {
    int upwindX = x - (int)(wind.X * step);  // Opposite to wind direction
    if (upwindElevation > currentElevation + threshold) {
        mountainBlocking += 0.05f;  // 5% per mountain
    }
}

// 3. Apply rain shadow (cap at 80% reduction)
float rainShadowFactor = MathF.Max(0.2f, 1f - mountainBlocking);
precipWithShadow[y, x] = basePrecip[y, x] * rainShadowFactor;
```

**Real-World Validation**:
- ✅ **Sahara Desert** (20°N): Trade winds (westward) + Atlas Mountains → dry interior east of mountains
- ✅ **Gobi Desert** (45°N): Westerlies (eastward) + Himalayas → dry leeward side west of plateau
- ✅ **Atacama Desert** (23°S): Trade winds (westward) + Andes → driest place on Earth

**Outputs**:
- `BasePrecipitationMap` - Input from VS_026 (for visual comparison)
- `WithRainShadowPrecipitationMap` - After orographic blocking [0,1]

**Archive**: [Backlog.md](../../01-Active/Backlog.md) (search "VS_027")

---

### VS_028: Coastal Moisture Enhancement (Distance-to-Ocean) ✅ COMPLETE

**Status**: Complete (2025-10-09) | **Actual Size**: S (~3h) | **Priority**: Completed
**Owner**: Dev Engineer → Archived

**What**: Enhanced precipitation near oceans using **distance-to-ocean BFS + exponential decay** (continentality effect), completing atmospheric climate pipeline.

**Why**: Continental interiors are significantly drier than coasts in reality (Sahara interior vs West Africa coast, central Asia vs maritime climates). Created **FINAL PRECIPITATION MAP** ready for erosion/rivers (VS_029).

**Three-Stage Algorithm** (physics-validated):
```csharp
// 1. Input: Rain shadow precipitation from VS_027
float[,] rainShadowPrecipitation = result.WithRainShadowPrecipitationMap;

// 2. BFS distance-to-ocean calculation (O(n), handles complex coastlines)
int[,] distanceToOcean = BFSFromOceanCells(oceanMask);
// Seed: All ocean cells at distance 0
// Propagate: 4-directional neighbors, increment distance by 1

// 3. Exponential moisture decay (realistic atmospheric physics)
const float maxCoastalBonus = 0.8f;        // 80% increase at coast
const float decayRange = 30f;              // 30 cells ≈ 1500km penetration
const float elevationResistance = 0.02f;   // Mountains resist coastal penetration

for (each land cell) {
    float dist = distanceToOcean[y, x];
    float coastalBonus = 0.8f * MathF.Exp(-dist / 30f);  // Exponential decay

    // Elevation resistance: Mountain plateaus stay dry
    float elevationFactor = 1f - MathF.Min(1f, elevation * 0.02f);

    // Apply coastal enhancement
    precipFinal[y, x] = rainShadowPrecip[y, x] * (1f + coastalBonus * elevationFactor);
}
```

**Key Physics Insights**:
- ✅ **Exponential decay**: Realistic atmospheric moisture drop-off (not linear!)
- ✅ **1500km penetration**: Matches real-world maritime climates (30 cells at 512×512 = ~50km/cell)
- ✅ **80% coastal bonus**: Maritime climates 2× wetter than interior (e.g., Seattle vs Spokane)
- ✅ **Elevation resistance**: Tibetan Plateau stays dry despite ocean proximity (altitude blocks moisture)
- ✅ **BFS distance**: Handles complex coastlines naturally (islands, peninsulas, inland seas)

**Real-World Validation**:
- ✅ **West Africa Coast** (wet) vs **Sahara Interior** (dry) - Same latitude, different distance
- ✅ **Pacific Northwest** (wet) vs **Great Basin** (dry) - Coastal vs continental climate
- ✅ **UK Maritime** (wet) vs **Central Asia** (dry) - Ocean proximity dominates

**Implementation Completed**:
- ✅ **Phase 0**: Updated WorldGenerationResult DTOs - Added FinalPrecipitationMap property
- ✅ **Phase 1**: Core algorithm implemented (TDD) - BFS distance + exponential decay + elevation resistance
- ✅ **Phase 2**: Pipeline integration complete - Stage 2d in GenerateWorldPipeline
- ✅ **Phase 3**: Visualization complete - MapViewMode.PrecipitationFinal, legends, probe, UI

**Results**:
- ✅ `FinalPrecipitationMap` - After all geographic effects (base + rain shadow + coastal) [0,1]
- ✅ **ATMOSPHERIC CLIMATE PIPELINE COMPLETE!** Ready for VS_029 erosion/rivers
- ✅ Maritime vs continental patterns realistic (Seattle wet, Spokane dry)
- ✅ 495/495 tests GREEN (100%)
- ✅ Performance: <20ms for coastal enhancement (512×512)

**Unblocks**: VS_029 (erosion can now use FINAL precipitation for realistic river spawning)

**Depended On**: VS_027 ✅ (rain shadow precipitation input)

**Archive**: [Completed_Backlog_2025-10_Part3.md](../../07-Archive/Completed_Backlog_2025-10_Part3.md) (search "VS_028")

---

## Stage 3: Hydrological Processes

**Goal**: Generate realistic water features (rivers, lakes, valleys) using FINAL precipitation from Stage 2

**Philosophy**: Slow geological processes (millions of years) that modify terrain based on atmospheric climate

### VS_029: Particle-Based Hydraulic Erosion & Rivers ⏳ PLANNED

**Status**: Proposed (Architecture Finalized) | **Size**: L (20-28h estimate) | **Priority**: After VS_028
**Owner**: Tech Lead → Dev Engineer (implement)

**What**: Generate rivers and carve realistic valleys using **particle-based hydraulic erosion simulation** (SimpleHydrology algorithm + improvements) - precipitation-weighted particles with momentum field feedback create emergent meandering rivers.

**Why**:
- **Better physics** than greedy tracing - particles simulate actual water flow with momentum/inertia
- **Natural meandering** - field feedback creates emergent river curves (not algorithmic straight paths)
- **Proven algorithm** - SimpleHydrology (Nick McDonald, 2020-2023) validated with real screenshots
- **Simpler architecture** - 2 phases (simulate → extract) vs 4 phases (accumulate → trace → carve → polish)
- **Lower risk** - Real C++ implementation to port (not theoretical design)

**Key Architectural Decisions**:

**1. SimpleHydrology Foundation + 3 Improvements**:
```
✅ Port: Particle physics (gravity, momentum, erosion, deposition)
✅ Port: Momentum field feedback (particles align with prevailing flow)
✅ Port: Discharge field tracking (continuous flow accumulation)
✅ NEW: Precipitation-weighted seeding (spawn more in wet highlands)
✅ NEW: Scale-aware parameter normalization (semantic params work on ANY map size)
✅ NEW: River/lake extraction (continuous fields → discrete WaterType markers)
```

**2. Two-Layer Parameter System** (solves interdependency problem):
```csharp
// HIGH-LEVEL (Designer Interface - semantic params [0-1])
public class SemanticHydrologyParams {
    [Range(0, 1)] public float RiverDensity = 0.5f;      // How many rivers?
    [Range(0, 1)] public float RiverMeandering = 0.5f;   // How curvy?
    [Range(0, 1)] public float ValleyDepth = 0.5f;       // How carved?
    [Range(0, 1)] public float ErosionSpeed = 0.5f;      // Fast/slow sim?

    // LOW-LEVEL (Physics Engine - derived automatically!)
    public PhysicsParams ToPhysics(MapContext context) {
        // Scale-aware normalization (works on 256×256, 512×512, 1024×1024)
        float sizeRatio = context.MapSize / 512f;

        return new PhysicsParams {
            // SCALED parameters (adjust for map size)
            MaxParticleAge = (int)(Lerp(100, 500, ErosionSpeed) * sizeRatio),
            ParticlesPerCycle = (int)(Lerp(100, 5000, RiverDensity) * sizeRatio * sizeRatio),
            MomentumTransfer = Lerp(0.1f, 2.0f, RiverMeandering) * sizeRatio,
            Gravity = Lerp(2.0f, 0.5f, RiverMeandering) / sizeRatio,

            // ROUGHNESS-adjusted (terrain-aware)
            Entrainment = Lerp(2.0f, 20.0f, ValleyDepth) * (context.TerrainRoughness / 0.15f),

            // FIXED parameters (naturally scale-independent)
            DepositionRate = Lerp(0.3f, 0.05f, ValleyDepth),
            EvaporationRate = 0.001f,
            MinVolume = 0.01f,
            FieldSmoothingRate = 0.1f
        };
    }
}
```

**3. Particle Physics Algorithm** (SimpleHydrology `water.h` port):
```csharp
// Each particle lifecycle (~100-500 steps):
public bool SimulateParticle(
    Particle p,
    float[,] heightmap,
    float[,] discharge,      // Flow accumulation field
    float[,] momentumX,      // Prevailing flow direction X
    float[,] momentumY)      // Prevailing flow direction Y
{
    // 1. Gravity force (terrain slope)
    var gradient = ComputeGradient(heightmap, p.Position);
    p.Speed += Gravity * -gradient / p.Volume;

    // 2. Momentum field feedback (KEY: particles align with existing flow!)
    var fieldMomentum = new Vector2(momentumX[p.Position], momentumY[p.Position]);
    if (fieldMomentum.Length() > 0 && p.Speed.Length() > 0) {
        float alignment = Vector2.Dot(fieldMomentum.Normalized(), p.Speed.Normalized());
        p.Speed += MomentumTransfer * alignment / (p.Volume + discharge[p.Position]) * fieldMomentum;
    }

    // 3. Move particle (normalized speed)
    p.Position += p.Speed.Normalized() * CellSize;

    // 4. Track discharge and momentum (build fields for next particles!)
    discharge[p.Position] += p.Volume;
    momentumX[p.Position] += p.Volume * p.Speed.X;
    momentumY[p.Position] += p.Volume * p.Speed.Y;

    // 5. Erosion/deposition (physics-based terrain modification)
    float heightDiff = heightmap[p.Position] - heightmap[NextPosition(p)];
    float sedimentCapacity = (1.0f + Entrainment * discharge[p.Position]) * heightDiff;
    float sedimentChange = DepositionRate * (sedimentCapacity - p.Sediment);

    p.Sediment += sedimentChange;
    heightmap[p.Position] -= sedimentChange;

    // 6. Evaporate
    p.Volume *= (1.0f - EvaporationRate);
    p.Sediment /= (1.0f - EvaporationRate);

    // Termination checks
    if (p.Age > MaxParticleAge || p.Volume < MinVolume || OutOfBounds(p.Position)) {
        heightmap[p.Position] += p.Sediment;  // Deposit remaining sediment
        return false;
    }

    p.Age++;
    return true;  // Continue simulating
}

// Field smoothing (exponential moving average creates persistent channels)
for each cell:
    discharge[cell] = 0.9f * discharge[cell] + 0.1f * dischargeTrack[cell];
    momentumX[cell] = 0.9f * momentumX[cell] + 0.1f * momentumXTrack[cell];
    momentumY[cell] = 0.9f * momentumY[cell] + 0.1f * momentumYTrack[cell];
```

**4. River/Lake Extraction** (continuous fields → discrete markers):
```csharp
// After particle simulation, classify cells by discharge + momentum
public enum WaterType { Dry, Creek, Stream, River, Lake }

public WaterType ClassifyWater(float discharge, float momentumMag, float height) {
    if (discharge < 0.001f) return WaterType.Dry;

    // Lake: High discharge + low momentum (water pools, doesn't flow!)
    if (discharge > 0.1f && momentumMag < 0.05f)
        return WaterType.Lake;

    // Rivers: Threshold by flow volume
    if (discharge > 0.5f) return WaterType.River;
    if (discharge > 0.1f) return WaterType.Stream;
    if (discharge > 0.01f) return WaterType.Creek;

    return WaterType.Dry;
}

// Extract river paths and lake regions (connected components)
public HydrologyFeatures ExtractFeatures(ErosionResult erosion) {
    var waterMap = ClassifyCells(erosion.DischargeMap, erosion.MomentumMaps);
    var rivers = ExtractRivers(waterMap, erosion);   // Trace high-discharge paths
    var lakes = ExtractLakes(waterMap, erosion);     // Floodfill low-momentum regions
    return new HydrologyFeatures(waterMap, rivers, lakes);
}
```

**Key Outputs**:
- `ErodedHeightmap` - Valleys carved naturally by particle erosion [0.1-20 RAW units]
- `DischargeMap` - Continuous flow accumulation [0-1+] (serves as watermap!)
- `MomentumXMap`, `MomentumYMap` - Prevailing flow direction [-1, +1]
- `WaterMap` - Discrete markers per cell (Dry/Creek/Stream/River/Lake)
- `Rivers` - List<River> (extracted paths + metadata)
- `Lakes` - List<Lake> (extracted regions + metadata)

**Critical Architectural Decision**:
**Particles spawn weighted by FINAL PRECIPITATION (VS_028 output)**, not uniform random!

**Why this matters**:
```
Leeward Desert (FINAL precip = 0.32):
- Particle density: LOW (few particles spawned)
- Rivers: RARE (insufficient erosion to form channels)
✓ REALISTIC

Windward Coastal Mountain (FINAL precip = 0.9):
- Particle density: HIGH (many particles spawned)
- Rivers: DENSE (strong erosion creates valleys)
✓ REALISTIC
```

**Implementation Phases** (C++ → C# port):

**Phase 1: Particle Physics** (~8h)
- Port `Drop` struct from `water.h` (gravity, momentum, erosion, deposition)
- Port field tracking (discharge, momentumX, momentumY)
- Unit tests: Single particle lifecycle (physics correctness)

**Phase 2: Field Management** (~3h)
- Add `ErosionFields` DTO (discharge, momentum maps)
- Implement exponential smoothing (field persistence)
- Unit tests: Multi-particle interactions (field feedback)

**Phase 3: Precipitation-Weighted Seeding** (~2h)
- Weighted random sampling from FINAL precipitation map
- More particles in wet highlands (vs uniform SimpleHydrology)
- Unit tests: Seeding distribution matches precipitation

**Phase 4: Scale-Aware Parameters** (~3h)
- Implement `ScaleAwareHydrologyParams.ToPhysics()`
- Auto-detect `MapContext` (size, roughness)
- Unit tests: Cross-scale consistency (256/512/1024)

**Phase 5: River/Lake Extraction** (~4h)
- Classify cells (continuous → discrete WaterType)
- Extract river paths (trace high-discharge connected components)
- Extract lake regions (floodfill low-momentum pools)
- Unit tests: Classification accuracy, feature extraction

**Phase 6: Integration & Tuning** (~4-6h)
- Wire into pipeline after VS_028
- Use VS_031 debug panel for parameter tuning
- Visual validation (meandering rivers, lakes in basins)
- Performance optimization (<500ms for 512×512)

**Done When**:
1. Particles simulate with physics (gravity + momentum field + erosion)
2. Rivers meander naturally (field feedback creates curves, not straight greedy paths!)
3. Discharge map serves as flow accumulation (no separate watermap needed)
4. WaterMap classifies cells (Dry/Creek/Stream/River/Lake)
5. Rivers/lakes extracted as discrete features (gameplay integration)
6. Scale-aware params work on 256×256, 512×512, 1024×1024 (same presets!)
7. All 495+ existing tests GREEN + 18-20 new particle/erosion tests
8. Performance <500ms for 512×512 (particle simulation + extraction)

**Depends On**: VS_028 ✅ (FINAL precipitation required for realistic particle seeding)

**Blocks**:
- Irrigation & Humidity (uses discharge map as watermap - no separate simulation needed!)
- Biomes (uses eroded terrain + humidity)
- Swamps (uses discharge + momentum for drainage detection)

**Benefits Over Original Greedy Approach**:
- ✅ **8-10h time savings** (20-28h vs 28-40h for greedy + polish)
- ✅ **Beautiful rivers immediately** (no intermediate "functional but ugly" phase)
- ✅ **Proven algorithm** (SimpleHydrology screenshots validate quality)
- ✅ **Better physics** (erosion causes rivers, not rivers cause erosion)
- ✅ **Simpler codebase** (3 core algorithms vs 7)

---

### VS_031: WorldGen Debug Panel (Real-Time Parameter Tuning) ⏳ PLANNED

**Status**: Proposed (Implement after VS_029) | **Size**: M (6-8h estimate) | **Priority**: ESSENTIAL for VS_029
**Owner**: Product Owner → Dev Engineer (implement)

**What**: Real-time parameter tuning UI panel for world generation with **incremental stage-based regeneration** - instantly see the impact of semantic parameter changes without full world regeneration.

**Why**:
- **VS_029 has 4 semantic parameters** that need tuning (RiverDensity, RiverMeandering, ValleyDepth, ErosionSpeed)
- **Guess-compile-test cycle is slow** - Changing parameters requires regenerating entire world (~2s)
- **Visual feedback critical** - Particle erosion parameters interact in non-obvious ways (momentum vs gravity tradeoff!)
- **Artist-friendly** - Non-programmers can discover optimal values through experimentation
- **25× faster iteration** - 0.2s stage-3-only regen vs 2s full world regen

**Key Features**:

**1. Stage-Based Cache Architecture** (only regenerate changed stages):
```csharp
public class WorldGenCache {
    // Stage 1: Plate Tectonics (~1.5s to regenerate)
    public float[,] PostProcessedHeightmap { get; set; }
    public bool[,] OceanMask { get; set; }

    // Stage 2: Climate (~0.1s)
    public float[,] TemperatureMap { get; set; }
    public float[,] FinalPrecipitationMap { get; set; }

    // Stage 3: Erosion (VS_029 - ~0.5s) ✨
    public float[,] ErodedHeightmap { get; set; }
    public float[,] DischargeMap { get; set; }
    public List<River> Rivers { get; set; }
    public List<Lake> Lakes { get; set; }

    // Future stages...
}

// Only regenerate stages AFTER the changed parameter
public void RegenerateFromStage(WorldGenStage stage, WorldGenParams newParams) {
    switch (stage) {
        case WorldGenStage.Erosion:
            // ✨ FAST! Reuses cached heightmap + climate (0.5s!)
            var erosion = _erosionProcessor.Process(
                _cache.PostProcessedHeightmap,
                _cache.OceanMask,
                _cache.FinalPrecipitationMap,
                newParams.SemanticErosion);  // Only erosion params changed!

            _cache.ErodedHeightmap = erosion.ErodedHeightmap;
            _cache.Rivers = erosion.Rivers;
            _cache.Lakes = erosion.Lakes;
            break;
    }

    _worldView.DisplayWorld(_cache);  // Update visualization immediately
}
```

**2. Semantic Parameter UI** (hide physics complexity):
```
┌─────────────────────────────────────────────────────────┐
│ WorldGen Debug Panel                                    │
├─────────────────────────────────────────────────────────┤
│ Map Context (Auto-Detected):                           │
│   Size: 512×512  Roughness: 0.147  Scale: 1.0×        │
│                                                         │
│ Stage 3: Erosion & Rivers ⚙️                           │
│ ┌─────────────────────────────────────────────────┐   │
│ │ River Density     [====|====] 0.50              │   │
│ │ River Meandering  [==|======] 0.70 (curvy!)     │   │
│ │ Valley Depth      [======|==] 0.30              │   │
│ │ Erosion Speed     [===|=====] 0.60              │   │
│ │                                                  │   │
│ │ [▼] Show Physics Parameters (Expert Mode)       │   │
│ │   → gravity: 0.65  momentumTransfer: 1.80       │   │
│ │   → entrainment: 8.4  depositionRate: 0.135     │   │
│ │   → particles: 2800  maxAge: 400                │   │
│ │                                                  │   │
│ │ Presets: [Earth] [Mountains] [Desert] [Custom]  │   │
│ │                                                  │   │
│ │ [Regenerate Stage 3+] (~0.5s) ✨                │   │
│ └─────────────────────────────────────────────────┘   │
│                                                         │
│ Simulate Different Scale:                              │
│   Test Map Size: [256] [512✓] [1024]                  │
│   (Preview how params scale without regenerating)      │
└─────────────────────────────────────────────────────────┘
```

**3. Preset System** (ship with expert-tuned presets):
```json
// presets/earth_like.json
{
  "name": "Earth-Like Rivers",
  "description": "Moderate river density with natural meandering",
  "params": {
    "riverDensity": 0.4,
    "riverMeandering": 0.6,
    "valleyDepth": 0.3,
    "erosionSpeed": 0.5
  }
}

// presets/mountain_planet.json
{
  "name": "Mountain Planet",
  "description": "Many steep mountain streams with deep gorges",
  "params": {
    "riverDensity": 0.7,
    "riverMeandering": 0.3,
    "valleyDepth": 0.8,
    "erosionSpeed": 0.4
  }
}
```

**Implementation Phases**:

**Phase 1: Stage-Based Cache** (~2h)
- Implement `WorldGenCache` with all stage outputs
- Add `RegenerateFromStage()` with fallthrough logic
- Unit tests: Cache invalidation triggers correct stages

**Phase 2: Godot UI Panel** (~3h)
- Create `WorldGenDebugPanel.tscn` (collapsible stage sections)
- Semantic parameter sliders (HSlider + SpinBox linked)
- "Regenerate Stage X+" buttons with timing estimates
- Expert mode toggle (show/hide physics params)

**Phase 3: Preset System** (~1-2h)
- JSON serialization (`PresetManager` service)
- Load/save preset buttons
- Ship with 3-5 example presets (Earth, Mountains, Desert, Islands)

**Phase 4: Scale Preview** (~1h)
- "Test Map Size" dropdown (256/512/1024)
- Preview physics param scaling WITHOUT regenerating
- Shows how params adapt to different map sizes

**Key Outputs**:
- Interactive UI panel with stage-based regen
- 25× faster iteration for erosion tuning (0.5s vs 2s)
- Preset system for sharing parameter sets
- Expert mode for physics debugging

**Performance Targets**:
- **Stage 3 (Erosion) regen**: <500ms (instant feedback!)
- **Stage 2 (Climate) regen**: <200ms (smooth iteration)
- **Stage 1 (Tectonics) regen**: ~1.5s (only when changing seed)

**Done When**:
1. All semantic parameters exposed in UI (4 for erosion, future: climate, elevation)
2. Stage-based incremental regeneration working (cache reuse)
3. Erosion parameter changes update world in <500ms (real-time tuning!)
4. Preset save/load working (JSON serialization)
5. Ships with 3-5 example presets (Earth, Mountains, Desert, Islands, Pangaea)
6. UI responsive (no freezing during regeneration)
7. Panel toggleable via debug menu or F3 hotkey
8. Expert mode shows derived physics params (debugging)
9. All existing tests remain GREEN (no regression)

**Depends On**:
- VS_029 ✅ (erosion semantic params are primary use case!)
- Existing climate/elevation stages (VS_024-028)

**Blocks**: Nothing (pure debug tool, worldgen works without it)

**Benefits**:
- 🎯 **ESSENTIAL for VS_029** - Particle erosion has 4 semantic params needing tuning
- 🎯 **25× faster iteration** - 0.5s vs 2s for erosion param changes
- 🎯 **Designer-friendly** - 4 sliders vs 10+ physics constants
- 🎯 **Preset portability** - Presets work on ANY map size (scale-aware normalization!)
- 🎯 **Expert accessible** - Physics params visible in expert mode

---

### Watermap Simulation ⏳ REPLACED BY VS_029

**Status**: ~~Proposed~~ **REPLACED** - VS_029 discharge map serves as watermap!
**Previous Size**: M (~3-4h) → **Saved by using discharge field**

**What** (ORIGINAL PLAN): Droplet flow model simulating water accumulation from precipitation

**ARCHITECTURAL CHANGE**: VS_029's particle simulation **already produces** a discharge map (flow accumulation field). This discharge map IS the watermap - no separate simulation needed!

**Why VS_029 Discharge Map is Better**:
- ✅ **Already computed** - Particles track discharge as they flow (no extra simulation!)
- ✅ **More accurate** - Real erosion physics vs simplified droplet model
- ✅ **Better performance** - One simulation vs two (saves ~3-4h implementation + runtime)
- ✅ **Unified data** - Discharge field serves triple duty (erosion, watermap, irrigation)

**How It Works** (from VS_029):
```csharp
// During particle simulation (VS_029):
for each particle:
    discharge[particle.Position] += particle.Volume;  // Track flow!

// After simulation:
var dischargeMap = ApplyExponentialSmoothing(discharge);  // Persistent channels

// Use as watermap for irrigation:
var irrigationInput = dischargeMap;  // Direct reuse!
```

**Key Outputs** (from VS_029):
- `DischargeMap` - Flow accumulation per cell [0,1+] ← **This IS the watermap!**
- `WaterMap` - Discrete classification (Dry/Creek/Stream/River/Lake) ← Thresholded discharge

**Visual Representation** (Presentation layer):
- Use `WaterMap` enum for rendering (already classified by VS_029!)
- Creek/Stream/River distinction built into extraction algorithm
- No separate watermap thresholds needed (VS_029 handles this)

**Time Savings**: ~3-4h implementation + faster runtime (one simulation instead of two)

**Depends On**: VS_029 ✅ (discharge map output)

---

### Irrigation & Humidity ⏳ PLANNED

**Status**: Proposed (VS_022 Phase 4 - renumbered after watermap removal) | **Size**: M (~3-4h)

**What**: Moisture spreading from waterways + combined precipitation/irrigation for biome classification

**Two-Step Process** (WorldEngine `irrigation.py` + `humidity.py`):

**1. Irrigation** (logarithmic moisture spreading from VS_029 discharge map):
```csharp
// Use VS_029's discharge map as watermap input (no separate watermap needed!)
var watermap = erosionResult.DischargeMap;  // Flow accumulation from particles

// For each cell, spread moisture from nearby water
for (int y = 0; y < height; y++) {
    for (int x = 0; x < width; x++) {
        float irrigationInfluence = 0f;

        // 21×21 neighborhood (radius 10)
        for (int dy = -10; dy <= 10; dy++) {
            for (int dx = -10; dx <= 10; dx++) {
                int nx = x + dx, ny = y + dy;
                if (!InBounds(nx, ny)) continue;

                float distance = MathF.Sqrt(dx * dx + dy * dy);
                if (distance <= 10f) {
                    // Logarithmic decay (nearby water has strong influence)
                    float influence = watermap[ny, nx] / MathF.Log(distance + 1);
                    irrigationInfluence += influence;
                }
            }
        }

        irrigation[y, x] = irrigationInfluence;
    }
}
```

**2. Humidity** (precipitation + irrigation combined):
```csharp
// WorldEngine formula: precipitation × 1 + irrigation × 3 (irrigation 3× stronger!)
for (int y = 0; y < height; y++) {
    for (int x = 0; x < width; x++) {
        float precip = finalPrecipitation[y, x];
        float irrig = irrigation[y, x];
        humidity[y, x] = precip * 1.0f + irrig * 3.0f;
    }
}

// Calculate 8-level moisture classification (superarid → superhumid)
var quantiles = CalculatePercentiles(humidity, [12, 25, 37, 50, 62, 75, 87]);
// Results in 8 moisture levels for biome classification
```

**Key Insight**:
**Humidity ≠ Precipitation!** Cells near rivers get 3× moisture bonus even if precipitation is low.

**Example**:
```
Desert cell (low precipitation):
- Final precipitation: 0.2 (rain shadow desert)
- Irrigation: 0.3 (near river from distant wet mountains)
- Humidity: 0.2 × 1 + 0.3 × 3 = 1.1
- Result: River oasis (wet biome despite low rainfall!) ✓ REALISTIC
```

**Key Outputs**:
- `IrrigationMap` - Moisture from nearby waterways [0,∞)
- `HumidityMap` - Combined moisture (precip + irrigation) [0,∞)
- `HumidityQuantiles` - 8-level classification for biomes

**Why This Matters for Biomes**:
- Biomes use **humidity**, not precipitation (WorldEngine pattern)
- Rivers create green corridors through deserts (realistic!)
- Coastal moisture + river irrigation = very wet biomes (rainforests, marshes)

**Depends On**: Watermap ✅ (irrigation spreads from watermap data)

**Blocks**: Biome Classification (uses humidity as input)

---

## Stage 4: Biome Classification

### Biome Classification System ⏳ PLANNED

**Status**: Proposed (VS_022 Phase 6) | **Size**: M (~6h)

**What**: 48 biome types classified by temperature + humidity + elevation (WorldEngine catalog)

**Classification Algorithm** (Holdridge-style):
```csharp
public BiomeType Classify(float temperature, float humidity, float elevation) {
    // 1. Special cases (overrides)
    if (IsOcean(elevation)) return BiomeType.Ocean;
    if (IsIceCap(temperature, elevation)) return BiomeType.IceCap;  // Arctic mountains

    // 2. Temperature level (6 bands from WorldEngine temperature_thresholds)
    int tempLevel = GetTemperatureLevel(temperature, temperatureQuantiles);
    // 0: Polar (<-5°C)
    // 1: Alpine (-5° to 5°C)
    // 2: Boreal (5° to 13°C)
    // 3: Cool Temperate (13° to 18°C)
    // 4: Warm Temperate (18° to 23°C)
    // 5: Subtropical/Tropical (>23°C)

    // 3. Humidity level (8 bands from humidity quantiles)
    int humidityLevel = GetHumidityLevel(humidity, humidityQuantiles);
    // 0: Superarid (<12th percentile) - Extreme deserts
    // 1: Perarid (12-25th)
    // 2: Arid (25-37th)
    // 3: Semiarid (37-50th)
    // 4: Subhumid (50-62th)
    // 5: Humid (62-75th)
    // 6: Perhumid (75-87th)
    // 7: Superhumid (>87th) - Rainforests

    // 4. Holdridge classification (temperature × humidity lookup table)
    return HoldridgeTable[tempLevel, humidityLevel];
    // Examples:
    // - Tropical (temp 5) + Superhumid (hum 7) = Tropical Rainforest
    // - Boreal (temp 2) + Arid (hum 2) = Boreal Desert
    // - Subtropical (temp 5) + Perarid (hum 1) = Hot Desert (Sahara)
}
```

**48 Biome Types** (WorldEngine catalog):
```
POLAR (temp 0):
- Ice, Tundra, Alpine Tundra

ALPINE (temp 1):
- Alpine Desert, Alpine Shrubland, Alpine Meadow

BOREAL (temp 2):
- Boreal Desert, Taiga, Boreal Forest

COOL TEMPERATE (temp 3):
- Cold Desert, Steppe, Temperate Grassland, Temperate Deciduous Forest

WARM TEMPERATE (temp 4):
- Warm Desert, Mediterranean Shrubland, Temperate Rainforest

SUBTROPICAL/TROPICAL (temp 5):
- Hot Desert, Savanna, Tropical Seasonal Forest, Tropical Rainforest

SPECIAL:
- Ocean, Ice Cap, River, Lake
```

**Biome Transitions** (smooth gradients):
```csharp
// NOT discrete borders - blend nearby biomes for realistic transitions
public BiomeType ClassifyWithTransitions(float temperature, float humidity, float elevation) {
    // Sample 3×3 neighborhood, blend biome influences
    var neighborBiomes = SampleNeighborhood(temperature, humidity, elevation, radius: 1);
    var dominantBiome = neighborBiomes.MostCommon();

    // Apply transition blending (70% dominant, 30% neighbors)
    return dominantBiome;
}
```

**Key Outputs**:
- `BiomeMap` - BiomeType[,] (48 types)
- `BiomeTransitionMap` - Smooth gradients (optional, for visual polish)

**Visual Representation**:
- Each biome type has unique color (Kenney palette)
- Legends show biome distribution percentages
- Probe displays biome name + properties (temperature, humidity, elevation ranges)

**Depends On**:
- Temperature ✅ (VS_025)
- Humidity ✅ (Irrigation phase)
- Elevation ✅ (VS_024)

**Blocks**: Strategic layer settlement placement (biomes determine economy buildings)

---

## Phase 2: Hydrology Extensions (Quick Wins)

**Goal**: Enhanced water features using existing data (fast, high-value additions)

**Timeline**: 1 week after Phase 1 complete

### Swamp Biome Classification

**Size**: S (2-3h)

**What**: Swamp biome subtype based on poor drainage detection

**Algorithm**:
```csharp
public bool IsSwamp(float elevation, float humidity, float slope) {
    // Swamps: Waterlogged lowlands with poor drainage
    return elevation < seaLevel + 200  // Low elevation (< 200m above sea level)
        && humidity > 0.7              // High humidity (wet climate)
        && slope < 5f;                 // Flat terrain (< 5° slope, poor drainage)
}
```

**Key Insight**: Slope calculation needed (also used by thermal erosion in Phase 3!)

**Outputs**:
- SwampMask (bool[,]) - Cells classified as swamps
- Swamp biome subtypes: Bog, Marsh, Mangrove (differentiated by temperature/salinity)

**Dependencies**:
- Humidity ✅ (Phase 1)
- Slope map (NEW - calculate from eroded heightmap)

---

### Creek/River/Main River Visualization

**Size**: S (1-2h)

**What**: Visual distinction between waterway sizes using watermap thresholds

**Implementation** (Presentation layer only):
```csharp
// Watermap thresholds already exist from Phase 1!
public void RenderWaterways(float[,] watermap, WatermapThresholds thresholds) {
    foreach (var cell in grid) {
        float flow = watermap[cell.Y, cell.X];

        if (flow > thresholds.MainRiver) {
            DrawLine(cell, thickness: 3, color: DarkBlue);  // Major rivers
        } else if (flow > thresholds.River) {
            DrawLine(cell, thickness: 2, color: Blue);      // Medium rivers
        } else if (flow > thresholds.Creek) {
            DrawLine(cell, thickness: 1, color: LightBlue);  // Creeks/streams
        }
    }
}
```

**Key Insight**: Data already exists (watermap thresholds from Phase 1), just needs visual rendering!

**Dependencies**: Watermap ✅ (Phase 1)

---

### Slope Map Calculation

**Size**: XS (1h)

**What**: Calculate slope (degrees) per cell from eroded heightmap

**Algorithm**:
```csharp
public float CalculateSlope(int x, int y, float[,] elevation) {
    // Sample 8-directional neighbors
    float maxElevationDifference = 0f;

    foreach (var neighbor in Get8Neighbors(x, y)) {
        float diff = MathF.Abs(elevation[y, x] - elevation[neighbor.Y, neighbor.X]);
        maxElevationDifference = MathF.Max(maxElevationDifference, diff);
    }

    // Convert elevation difference to slope (degrees)
    // Assuming cell size = 50km at 512×512 world
    float cellSize = 50_000f;  // meters
    float slope = MathF.Atan(maxElevationDifference / cellSize) * (180f / MathF.PI);
    return slope;
}
```

**Outputs**:
- `SlopeMap` - Slope in degrees [0°-90°]

**Used By**:
- Swamp detection (flat terrain threshold)
- Thermal erosion (Phase 3 - slope collapse)
- Passability (gameplay - units can't climb steep slopes)

**Dependencies**: Eroded heightmap ✅ (VS_029)

---

## Phase 3: Geology & Resources (DF-Inspired Depth)

**Goal**: Dwarf Fortress-level geological depth for standalone worldgen game

**Timeline**: 3-4 weeks after Phase 2 complete

**Status**: Design phase (requires plate boundary detection from native library)

### Stage 0: Geological Foundation

**Philosophy**: Geology drives mineral resources, volcanoes, and advanced biomes

#### Plate Boundary Detection

**Size**: S-M (2-8h depending on platec API availability)

**What**: Extract plate boundary data from native plate tectonics simulation

**Critical Decision**: Does `platec` library expose plate boundary data?

**Option A** (IF platec exposes boundaries): S (2-3h)
```csharp
// Best case: Native lib already calculates boundaries
var boundaries = PlateTectonicsNative.GetPlateBoundaries(simData);
// boundaries: List<Position> with boundary type (collision, rift, transform)
```

**Option B** (IF platec doesn't expose boundaries): M (6-8h)
```csharp
// Worst case: Calculate boundaries from elevation gradients
public List<(Position, BoundaryType)> DetectBoundaries(float[,] elevation, float[,] plateIds) {
    // 1. Find cells where adjacent plates differ (plate ID mismatch)
    // 2. Calculate elevation gradient (collision = high gradient, rift = low)
    // 3. Classify: Collision (mountains), Rift (valleys), Transform (flat)
}
```

**Outputs**:
- `PlateBoundaries` - List<(Position, BoundaryType)>
- Boundary types: Collision (mountains), Rift (ocean trenches), Transform (faults), Hotspot (isolated volcanoes)

**Dependencies**: Native plate tectonics simulation ✅ (VS_024)

**Blocks**: Volcanic System (volcanoes spawn at boundaries)

---

#### Volcanic System

**Size**: M (6-8h)

**What**: Spawn volcanoes at plate boundaries, generate volcanic cones, add geothermal heat

**Three-Component System**:

**1. Volcano Spawning** (plate boundaries + hotspots):
```csharp
foreach (var boundary in plateBoundaries.Where(b => b.Type == Collision || b.Type == Hotspot)) {
    // Spawn volcano with probability based on boundary type
    float spawnChance = boundary.Type == Collision ? 0.3f : 0.1f;  // Hotspots rarer
    if (random.NextFloat() < spawnChance) {
        volcanoes.Add(new Volcano {
            Position = boundary.Position,
            IsActive = random.NextFloat() < 0.2f,  // 20% active, 80% dormant
            MagmaChamberDepth = random.Next(5, 20)  // km (affects eruption style)
        });
    }
}
```

**2. Volcanic Cone Generation** (modify heightmap):
```csharp
foreach (var volcano in volcanoes) {
    // Create conical mountain (radius 3-5 cells)
    int radius = random.Next(3, 5);
    float peakHeight = random.Next(1500, 3500);  // meters

    for (int dy = -radius; dy <= radius; dy++) {
        for (int dx = -radius; dx <= radius; dx++) {
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance <= radius) {
                // Conical profile (linear slope from peak to base)
                float heightBoost = peakHeight * (1f - distance / radius);
                elevation[volcano.Y + dy, volcano.X + dx] += heightBoost;
            }
        }
    }
}
```

**3. Geothermal Temperature** (local heat boost):
```csharp
foreach (var volcano in volcanoes.Where(v => v.IsActive)) {
    // Active volcanoes create warm microclimates
    for (int dy = -5; dy <= 5; dy++) {
        for (int dx = -5; dx <= 5; dx++) {
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance <= 5) {
                // Geothermal heat decays with distance
                float heatBoost = 0.3f * (1f - distance / 5f);  // Max +0.3 temperature
                temperature[volcano.Y + dy, volcano.X + dx] += heatBoost;
            }
        }
    }
}
```

**Key Outputs**:
- `Volcanoes` - List<Volcano> (position, active status, magma depth)
- Modified heightmap (volcanic cones added)
- Modified temperature map (geothermal heat near active volcanoes)

**Gameplay Impact**:
- Volcanic ash soil (high fertility, unique crops)
- Geothermal heat (tropical plants at high latitudes - Iceland effect)
- Volcanic obsidian (crafting material)
- Lava tubes (dungeon entrances)

**Dependencies**: Plate Boundary Detection ✅

---

#### Geology Layers (Rock Types)

**Size**: L (12-16h) - Most complex Phase 3 feature

**What**: Classify rock types (igneous/sedimentary/metamorphic) per cell, generate ore veins

**Three Rock Types** (Dwarf Fortress pattern):

**1. Rock Type Classification**:
```csharp
public RockType ClassifyRockType(Position pos, float elevation, bool nearVolcano, bool wasOcean) {
    // Igneous: Volcanic origin (near volcanoes, young mountains)
    if (nearVolcano || elevation > peakLevel) {
        return RockType.Igneous;
    }

    // Sedimentary: Old ocean floors (below historical sea level)
    if (wasOcean || elevation < seaLevel + 500) {
        return RockType.Sedimentary;
    }

    // Metamorphic: High pressure zones (mountains, collision boundaries)
    if (elevation > mountainLevel || nearPlateBoundary) {
        return RockType.Metamorphic;
    }

    return RockType.Sedimentary;  // Default (most common)
}
```

**2. Ore Vein Generation** (Perlin worms constrained by geology):
```csharp
// Ore types constrained by rock type (DF pattern)
var oreRules = new Dictionary<RockType, List<OreType>> {
    { RockType.Igneous, [Gold, Silver, Copper, Obsidian] },      // Precious metals + volcanic glass
    { RockType.Sedimentary, [Iron, Coal, Clay, Limestone] },     // Industrial resources
    { RockType.Metamorphic, [Gems, Marble, Platinum, Mithril] } // Rare/luxury resources
};

// Perlin worm algorithm (continuous ore veins, not random scatter)
public List<OreVein> GenerateOreVeins(float[,] geology, RockType[,] rockTypes) {
    var veins = new List<OreVein>();

    foreach (var oreType in AllOreTypes) {
        // Seed veins in appropriate rock types
        var validCells = GetCellsWithRockType(rockTypes, oreRules[oreType]);
        int veinCount = oreType.Rarity;  // Gold: 3 veins, Iron: 20 veins

        for (int i = 0; i < veinCount; i++) {
            Position start = validCells[random.Next(validCells.Count)];

            // Perlin worm: Random walk with coherent noise guidance
            List<Position> veinCells = new() { start };
            Position current = start;

            for (int step = 0; step < 50; step++) {  // Vein length: ~50 cells
                float noiseAngle = GetPerlinNoise(current.X, current.Y, oreType.Seed) * MathF.PI * 2;
                int dx = (int)MathF.Round(MathF.Cos(noiseAngle));
                int dy = (int)MathF.Round(MathF.Sin(noiseAngle));

                Position next = current + (dx, dy);
                if (InBounds(next) && rockTypes[next.Y, next.X] == oreRules[oreType]) {
                    veinCells.Add(next);
                    current = next;
                }
            }

            veins.Add(new OreVein { OreType = oreType, Cells = veinCells });
        }
    }

    return veins;
}
```

**Key Outputs**:
- `GeologyMap` - RockType[,] (igneous, sedimentary, metamorphic)
- `OreVeins` - List<OreVein> (ore type, cell positions, richness)
- `MineralDensity` - float[,] (surface ore exposure for settlement placement)

**Ore Types** (DF-inspired):
- **Common**: Iron, Copper, Coal, Clay, Limestone (20-50 veins per world)
- **Uncommon**: Silver, Gold, Gems, Marble (5-10 veins)
- **Rare**: Platinum, Mithril, Magical Crystals (1-3 veins)

**Gameplay Impact**:
- Settlement placement (iron mines in sedimentary mountains)
- Economy buildings (gold mines in volcanic regions)
- Strategic resources (rare ores create trade/conflict)

**Dependencies**:
- Volcanic System ✅ (igneous rock classification)
- Plate Boundaries ✅ (metamorphic rock zones)
- Historical ocean mask (sedimentary classification)

---

#### Thermal Erosion (Slope Smoothing)

**Size**: S (2-3h) - Simple algorithm, deferred from Phase 1

**What**: Smooth unrealistic slopes (angle of repose), create talus slopes, expose rock layers

**Algorithm**:
```csharp
public float[,] ApplyThermalErosion(float[,] heightmap, int iterations = 5) {
    const float talusAngle = 35f;  // Angle of repose for rock (degrees)

    for (int iter = 0; iter < iterations; iter++) {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float maxSlope = CalculateMaxSlope(x, y, heightmap);

                if (maxSlope > talusAngle) {
                    // Find steepest neighbor
                    Position steepest = FindSteepestNeighbor(x, y, heightmap);

                    // Transfer material from steep neighbor to current cell
                    float excess = (maxSlope - talusAngle) * 0.5f;  // Move 50% of excess
                    heightmap[steepest.Y, steepest.X] -= excess;
                    heightmap[y, x] += excess;
                }
            }
        }
    }

    return heightmap;
}
```

**Key Outputs**:
- Modified `ErodedHeightmap` (smoother slopes, talus formations)
- `SlopeMap` (already calculated in Phase 2)

**Visual Effect**:
- Mountains less jagged (realistic angle of repose)
- Talus slopes at cliff bases (debris fields)
- Exposes different rock layers (geology visible on surface)

**Gameplay Impact**:
- Passability (units can climb 35° slopes, not 80° cliffs)
- Surface mining (talus slopes expose ore veins)

**Dependencies**:
- Eroded heightmap ✅ (VS_029)
- Slope map ✅ (Phase 2)

**Integration Point**: Runs AFTER hydraulic erosion (VS_029), BEFORE biome classification

---

### Stage 5: Extended Biome Types

**Size**: M (4-6h)

**What**: Volcanic, mineral-rich, and swamp biome subtypes using geology data

**Three New Biome Categories**:

**1. Volcanic Biomes**:
```csharp
if (nearActiveVolcano && temperature > 0.6f) {
    return BiomeType.VolcanicRainforest;  // Geothermal heat + ash soil = lush
} else if (nearActiveVolcano) {
    return BiomeType.VolcanicWasteland;   // Recent lava flows, sparse vegetation
}
```

**2. Mineral-Rich Biomes** (surface ore exposure):
```csharp
if (mineralDensity[y, x] > 0.7f) {
    return BiomeType.MineralOutcrop;  // Visible ore veins (strategic importance)
}
```

**3. Swamp Subtypes** (from Phase 2):
```csharp
if (IsSwamp(elevation, humidity, slope)) {
    if (temperature > 0.7f && nearOcean) {
        return BiomeType.MangroveSwamp;  // Tropical coastal
    } else if (temperature < 0.3f) {
        return BiomeType.Bog;  // Cold peat bogs
    } else {
        return BiomeType.Marsh;  // Temperate wetlands
    }
}
```

**Key Outputs**:
- Extended `BiomeMap` (48 base types + 10 geology subtypes = 58 total)

**Dependencies**:
- Volcanic System ✅ (Phase 3)
- Geology Layers ✅ (Phase 3)
- Swamp detection ✅ (Phase 2)

---

### Stage 6: Resource Distribution

**Size**: M (6-8h)

**What**: Place strategic resources (minerals, vegetation, special materials) based on biomes + geology

**Three Resource Categories**:

**1. Mineral Resources** (geology-based):
```csharp
// Already have ore veins from geology stage, now add surface density
foreach (var vein in oreVeins) {
    foreach (var cell in vein.Cells) {
        // Surface exposure based on slope + erosion
        if (slope[cell.Y, cell.X] > 30f) {  // Steep slopes expose veins
            mineralDensity[cell.Y, cell.X] += vein.Richness;
        }
    }
}
```

**2. Vegetation Resources** (biome-based):
```csharp
var vegetationRules = new Dictionary<BiomeType, List<ResourceType>> {
    { BiomeType.TemperateForest, [Timber, Herbs, Mushrooms] },
    { BiomeType.TropicalRainforest, [Hardwood, RareHerbs, Fruits] },
    { BiomeType.Swamp, [Peat, Reeds, Alchemy Ingredients] }
};

foreach (var cell in grid) {
    BiomeType biome = biomeMap[cell.Y, cell.X];
    if (vegetationRules.ContainsKey(biome)) {
        foreach (var resource in vegetationRules[biome]) {
            float density = CalculateDensity(resource, humidity[cell], temperature[cell]);
            resourceMap[cell] = (resource, density);
        }
    }
}
```

**3. Strategic Resources** (rare, high-value):
```csharp
// Magical crystals near plate boundaries + high elevation
if (nearPlateBoundary && elevation > peakLevel && random.NextFloat() < 0.01f) {
    strategicResources.Add((cell, ResourceType.MagicalCrystal));
}

// Rare herbs in specific biome + elevation combinations
if (biome == TropicalRainforest && elevation > 1000 && elevation < 2000) {
    strategicResources.Add((cell, ResourceType.RareHealing Herb));
}
```

**Key Outputs**:
- `ResourceMap` - Dictionary<Position, (ResourceType, float density)>
- `StrategicResources` - List<(Position, ResourceType)> (rare locations)

**Gameplay Impact**:
- Settlement economy (buildings produce resources based on biome + geology)
- Trade routes (rare resources create inter-settlement dependencies)
- Quest hooks ("Recover magical crystal from volcanic peak")

**Dependencies**:
- Geology Layers ✅ (Phase 3)
- Extended Biomes ✅ (Phase 3 Stage 5)

---

## Standalone Worldgen Game Potential

**Vision**: Extract worldgen system as standalone exploration/discovery game (separate from Darklands tactical RPG)

**Core Gameplay Loop**:
```
Generate World (seed-based, 30 seconds)
  ↓
Explore Map (pan/zoom, view modes, probe)
  ↓
Discover Features (volcanoes, ore veins, river sources, biome diversity)
  ↓
Export Data (JSON, PNG heightmap, seed sharing)
  ↓
Generate New World (procedural variety, compare worlds)
```

**Key Features for Standalone**:

**1. Visual Exploration Tools**:
- Multi-mode visualization (15+ view modes: elevation, temperature, precipitation, geology, biomes, resources)
- Interactive probe (click cell → full data: elevation, biome, temperature, humidity, geology, ores, rivers)
- Legends with percentile distributions (e.g., "Mountains: 12% of world", "Volcanic biomes: 3%")
- Comparison mode (split-screen: two worlds side-by-side)

**2. World Metrics Dashboard**:
```
World Stats:
- Continents: 4 (23% land, 77% ocean)
- Highest Peak: Mount Drakenfel (8,234m)
- Longest River: Serpent River (2,145km, 47 tributaries)
- Active Volcanoes: 12
- Biome Diversity: 34/58 types present
- Ore Richness: Gold (3 veins), Iron (24 veins), Mithril (1 vein - rare!)

Climate Summary:
- Average Temperature: 14.2°C
- Precipitation Range: 120mm (Sahara-like) to 2,800mm (rainforest)
- Largest Desert: Ashenfell Wastes (142,000 km²)
- Largest Forest: Greenwood Expanse (89,000 km²)
```

**3. Seed System**:
- Shareable seeds (reproduce exact worlds)
- Seed challenges ("Find a world with 5+ active volcanoes")
- Leaderboards (most diverse biomes, rarest resources)

**4. Export Functionality**:
- PNG heightmap export (use in other tools: Unity, Unreal, Blender)
- JSON world data (elevation, biomes, resources - modding support)
- Settlement suggestions ("Optimal city location: coordinates X, Y - coastal + iron + timber")

**Monetization Potential**:
- Free version: Core worldgen, 5 view modes, seed sharing
- Pro version: Advanced view modes, export, world comparison, metrics dashboard
- DLC: Fantasy biomes (corruption, magical zones), alien planets (low gravity, exotic chemistry)

**Target Audience**:
- Worldbuilding enthusiasts (writers, D&D DMs)
- Procedural generation fans (dwarf fortress community)
- Game developers (prototype tool for game world design)

**Development Effort**: +2-3 weeks after Phase 3 complete (polish UI, add export, metrics dashboard)

---

## Strategic Layer Integration

**Vision**: Darklands strategic layer consumes worldgen data to place settlements, factions, economy

**Integration Architecture**:
```
┌────────────────────────────────────────────────┐
│ Worldgen Layer (Terrain Foundation)           │
│ - Generates: elevation, climate, biomes       │
│ - Generates: geology, resources, rivers       │
│ - Output: WorldGenerationResult DTO           │
└─────────────────┬──────────────────────────────┘
                  │ Provides terrain data
                  ↓
┌────────────────────────────────────────────────┐
│ Strategic Layer (Civilization)                 │
│ - Consumes: biomes → settlement placement     │
│ - Consumes: resources → economy buildings     │
│ - Consumes: rivers → trade routes             │
│ - Generates: factions, history, quests        │
└─────────────────┬──────────────────────────────┘
                  │ Zoom in
                  ↓
┌────────────────────────────────────────────────┐
│ Tactical Layer (Grid Combat)                   │
│ - VS_001-021 systems (combat, inventory, AI)  │
│ - Encounters at strategic locations            │
└────────────────────────────────────────────────┘
```

**Settlement Placement Algorithm** (terrain-driven):
```csharp
public List<Settlement> PlaceSettlements(WorldGenerationResult world, int targetCount = 50) {
    var settlements = new List<Settlement>();

    // Score each cell for settlement suitability
    var suitability = new float[world.Height, world.Width];

    for (int y = 0; y < world.Height; y++) {
        for (int x = 0; x < world.Width; x++) {
            float score = 0f;

            // Positive factors
            if (world.BiomeMap[y, x] == BiomeType.TemperateGrassland) score += 10f;  // Ideal farmland
            if (IsNearRiver(x, y, world.Rivers)) score += 5f;  // Water access
            if (IsCoastal(x, y, world.OceanMask)) score += 3f;  // Trade ports
            if (world.MineralDensity[y, x] > 0.5f) score += 4f;  // Resource access

            // Negative factors
            if (world.BiomeMap[y, x] == BiomeType.HotDesert) score -= 5f;  // Uninhabitable
            if (world.Elevation[y, x] > world.PeakLevel) score -= 8f;  // Too mountainous
            if (IsNearVolcano(x, y, world.Volcanoes, activeOnly: true)) score -= 10f;  // Danger

            suitability[y, x] = score;
        }
    }

    // Place settlements at highest-scoring locations (with min distance spacing)
    var sortedCells = GetCellsSortedByScore(suitability);
    foreach (var cell in sortedCells.Take(targetCount)) {
        if (!HasNearbySettlement(cell, settlements, minDistance: 10)) {
            settlements.Add(CreateSettlement(cell, world));
        }
    }

    return settlements;
}
```

**Economy Building Assignment** (biome + geology → building types):
```csharp
public List<Building> AssignBuildings(Settlement settlement, WorldGenerationResult world) {
    var buildings = new List<Building>();
    Position pos = settlement.Position;
    BiomeType biome = world.BiomeMap[pos.Y, pos.X];

    // Terrain-driven attached buildings (Legends Mod pattern)
    if (IsCoastal(pos, world.OceanMask)) {
        buildings.Add(new Building { Type = BuildingType.FishingHut });
        buildings.Add(new Building { Type = BuildingType.Harbor });
    }

    if (world.GeologyMap[pos] == RockType.Sedimentary && world.MineralDensity[pos] > 0.6f) {
        buildings.Add(new Building { Type = BuildingType.IronMine });
    }

    if (IsNearVolcano(pos, world.Volcanoes) && world.MineralDensity[pos] > 0.8f) {
        buildings.Add(new Building { Type = BuildingType.GoldMine });  // Rare!
    }

    if (biome == BiomeType.TemperateForest) {
        buildings.Add(new Building { Type = BuildingType.LumberCamp });
        buildings.Add(new Building { Type = BuildingType.HunterCabin });
    }

    if (biome == BiomeType.Swamp) {
        buildings.Add(new Building { Type = BuildingType.HerbalistGrove });  // Rare plants
    }

    return buildings;
}
```

**Trade Route Generation** (river networks):
```csharp
// Settlements on same river system can trade easily
foreach (var river in world.Rivers) {
    var settlementsOnRiver = settlements.Where(s => river.Path.Contains(s.Position));
    if (settlementsOnRiver.Count() >= 2) {
        // Create trade network along river
        tradeRoutes.Add(new TradeRoute {
            Settlements = settlementsOnRiver.ToList(),
            TransportType = TransportType.River,
            TravelTimeModifier = 0.5f  // River transport 2× faster than land
        });
    }
}
```

**Dependencies**:
- Worldgen Phase 1 ✅ (biomes, elevation, rivers)
- Worldgen Phase 3 (geology, minerals, volcanoes for advanced economy)

**Implementation Timeline**: After Phase 1 complete (basic settlements), Phase 3 complete (rich economy)

---

## Performance Budget

**Target**: <2s for 512×512 world generation (fast iteration during development)

**Current Performance** (Phase 1 complete):
```
Stage 1: Plate Tectonics (VS_024)
  - Native simulation: 1.0s (83% of total)
  - Elevation post-processing: 0.2s
  - Visualization: 0.3s
  - Total: 1.5s ✅ UNDER BUDGET

Stage 2: Atmospheric Climate (VS_025-027)
  - Temperature: 0.06s
  - Base Precipitation: 0.03s
  - Rain Shadow: 0.02s
  - Total: 0.11s ✅ MINIMAL OVERHEAD

Current Total: 1.61s (Phase 1-2 complete)
Remaining Budget: 0.39s for Phase 3-4
```

**Projected Performance** (Phase 3-4):
```
VS_028: Coastal Moisture
  - BFS distance-to-ocean: 0.03s (O(n))
  - Exponential decay: 0.02s (O(n))
  - Subtotal: 0.05s

VS_029: Erosion & Rivers
  - River source detection: 0.05s (flow accumulation)
  - River path tracing: 0.10s (A* for 50-100 rivers)
  - Valley carving: 0.05s (radius 2, gentle)
  - Subtotal: 0.20s

Watermap Simulation:
  - 20,000 droplets: 0.08s (recursive flow)
  - Subtotal: 0.08s

Irrigation & Humidity:
  - 21×21 convolution: 0.12s (O(n × 441))
  - Humidity combine: 0.01s
  - Subtotal: 0.13s

Biome Classification:
  - Holdridge lookup: 0.02s (O(n))
  - Subtotal: 0.02s

Phase 1-4 Total: 1.61 + 0.05 + 0.20 + 0.08 + 0.13 + 0.02 = 2.09s
⚠️ SLIGHTLY OVER BUDGET (+0.09s)
```

**Optimization Strategies** (if needed):
1. **Parallelize river tracing** (independent rivers → ThreadPool) → -0.07s
2. **Reduce droplet count** (20,000 → 10,000) → -0.04s
3. **Optimize irrigation convolution** (skip low watermap cells) → -0.05s

**With optimizations**: 2.09s - 0.16s = **1.93s ✅ UNDER BUDGET**

**Phase 3 Performance** (deferred, not in critical path):
- Geology calculations: ~0.3s (acceptable for offline preprocessing)
- Volcanic system: ~0.05s (minimal overhead)
- Thermal erosion: ~0.10s (5 iterations)
- **Not counted against 2s budget** (Phase 3 = optional standalone features)

---

## Next Steps

**Immediate Priority** (Complete Phase 1):
1. ✅ Implement **VS_028** (coastal moisture) - Completes atmospheric climate pipeline (~3-4h)
2. ✅ Implement **VS_029** (erosion & rivers) - Begin hydrological processes (~8-10h)
3. ✅ Implement **Watermap → Irrigation → Humidity** - Foundation for biomes (~6-8h)
4. ✅ Implement **Biome Classification** - Complete Phase 1 MVP (~6h)

**Phase 2** (After Phase 1 playable):
1. Add **swamp detection** (slope calculation + classification) (~2-3h)
2. Visualize **creeks** (watermap threshold rendering) (~1-2h)
3. Visual validation of Phase 1 worlds (is thermal erosion needed?)

**Phase 3** (Standalone Worldgen Game):
1. Research platec API for plate boundaries (determine effort: S or M)
2. Implement **volcanic system** (~6-8h)
3. Implement **geology layers + ore veins** (~12-16h)
4. Add **thermal erosion** if slopes unrealistic (~2-3h)
5. Polish UI for standalone release (~2-3 weeks)

**Product Owner Decisions Needed**:
- Approve VS_028 implementation start (ready now)
- Decide Phase 2 vs Phase 3 priority (quick wins vs DF depth)
- Standalone worldgen game roadmap (timeline, monetization)

---

**Last Updated**: 2025-10-09 01:09
**Status**: Phase 1 ~75% complete (VS_028 next, VS_029 after)
**Owner**: Tech Lead (roadmap maintenance), Dev Engineer (implementation)

---

*This roadmap provides comprehensive technical details for Darklands world generation system. See [Roadmap.md](Roadmap.md) for high-level project overview.*
