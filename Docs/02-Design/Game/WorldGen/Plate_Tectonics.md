# Plate Tectonics Feature

**Status**: Complete ✅ (VS_024 implemented 2025-10-08)

**Purpose**: Generate realistic elevation maps using plate tectonics simulation with dual-heightmap architecture (original + post-processed) and quantile-based normalization.

---

## Overview

The Plate Tectonics feature is the **foundation** of world generation, producing elevation data that all other features (climate, rivers, biomes) depend on. It uses a native C++ simulation (platec library via P/Invoke) for performance, combined with C# post-processing for normalization and smoothing.

`✶ Insight ─────────────────────────────────────`
**Why Native Simulation is Worth It**

Plate tectonics simulation is **83% of total generation time** (~1.0s out of 1.5s). A C# port would be 2-3 months of work with uncertain performance gains and high risk of physics bugs.

**Decision**: Keep native library, invest in API improvements instead (snapshot getter, memory leak fixes, RNG seeding). This pragmatic choice unblocked world generation MVP in weeks instead of months.
`─────────────────────────────────────────────────`

---

## Architecture

### Dual-Heightmap Design (TD_021)

**Key Insight**: Two heightmaps serve different purposes:

```csharp
public record WorldGenerationResult
{
    // ORIGINAL: Raw physics output [0.1-20 RAW units]
    // - Used by: Climate (mountain cooling), Geology (volcanic arcs)
    // - Rationale: Pure physics data without artistic smoothing
    public float[,] OriginalHeightmap { get; init; }

    // POST-PROCESSED: Smoothed + normalized [0.1-20 RAW units]
    // - Used by: Visualization, Biomes, Pathfinding
    // - Rationale: Removes noise, normalizes per-world
    public float[,] PostProcessedHeightmap { get; init; }

    // Quantile thresholds (per-world adaptive)
    public ElevationThresholds Thresholds { get; init; }
}
```

**Why Two Heightmaps?**
- **Original**: Climate algorithms need **raw elevation values** (mountain cooling at 3000m RAW, not normalized 0.8)
- **Post-Processed**: Biomes need **thresholds** that adapt per-world (20% sea level on water world, 70% sea level on pangaea)

### Sea Level as Single Source of Truth (TD_021)

**Constant** (application-wide):
```csharp
public static class WorldGenConstants
{
    // SSOT: Sea level in RAW elevation units
    public const float SEA_LEVEL_RAW = 1.0f;  // [0.1-20 scale]

    // Derived conversions (computed at runtime)
    public static float ToMeters(float raw) => (raw - SEA_LEVEL_RAW) * 1000f;
    public static float ToNormalized(float raw, float seaLevel) => (raw - seaLevel) / (20f - seaLevel);
}
```

**Benefits**:
- ✅ No magic numbers scattered across codebase
- ✅ Easy to tune sea level globally (adjust one constant)
- ✅ Consistent conversions (RAW ↔ Meters ↔ Normalized)

---

## Implementation (VS_024)

### Phase 1: Native Simulation Integration

**P/Invoke Wrapper** (C++ DLL marshalling):
```csharp
[DllImport("platec.dll")]
private static extern IntPtr platec_api_create(
    uint seed,
    uint width, uint height,
    float seaLevel,
    uint erosionPeriod,
    float foldingRatio,
    uint aggr_overlap_abs,
    uint aggr_overlap_rel,
    uint cycle_count,
    uint num_plates
);

[DllImport("platec.dll")]
private static extern void platec_api_step(IntPtr handle);

[DllImport("platec.dll")]
private static extern IntPtr platec_api_get_heightmap(IntPtr handle);

[DllImport("platec.dll")]
private static extern void platec_api_destroy(IntPtr handle);
```

**Simulation Loop** (iterative timesteps):
```csharp
public SimulationResult RunSimulation(PlateSimulationParams parameters)
{
    IntPtr handle = platec_api_create(
        parameters.Seed,
        parameters.MapSize, parameters.MapSize,
        seaLevel: 1.0f,
        erosionPeriod: 60,
        foldingRatio: 0.02f,
        aggr_overlap_abs: 1000000,
        aggr_overlap_rel: 0.33f,
        cycle_count: 2,
        num_plates: parameters.PlateCount
    );

    try
    {
        // Run simulation for N timesteps (~600 steps for convergence)
        for (int step = 0; step < 600; step++)
        {
            platec_api_step(handle);
        }

        // Extract heightmap (unsafe pointer → float[,] marshalling)
        IntPtr heightmapPtr = platec_api_get_heightmap(handle);
        float[,] heightmap = MarshalHeightmap(heightmapPtr, parameters.MapSize);

        return new SimulationResult(heightmap);
    }
    finally
    {
        platec_api_destroy(handle);  // CRITICAL: Prevent memory leak!
    }
}
```

**Performance**: ~1.0s for 512×512 map with 15 plates

### Phase 2: Elevation Post-Processing

**Four-Step Algorithm** (quantile thresholds + smoothing):

**Step 1: Calculate Quantile Thresholds** (adaptive per-world):
```csharp
// Sample 10,000 random cells (sufficient for quantile estimation)
var samples = SampleRandomCells(heightmap, count: 10_000);
Array.Sort(samples);

var thresholds = new ElevationThresholds
{
    SeaLevel = samples[(int)(samples.Length * 0.50)],      // 50th percentile
    HillLevel = samples[(int)(samples.Length * 0.75)],     // 75th percentile
    MountainLevel = samples[(int)(samples.Length * 0.90)], // 90th percentile
    PeakLevel = samples[(int)(samples.Length * 0.98)]      // 98th percentile
};
```

**Why Quantiles?**
- Water world: 70% ocean → sea level at 70th percentile (adaptive!)
- Pangaea: 20% ocean → sea level at 20th percentile (correct!)
- Hard-coded thresholds fail: "sea level = 1.0" breaks on outlier worlds

**Step 2: Gaussian Smoothing** (remove noise):
```csharp
// 5×5 Gaussian kernel (σ=1.0)
float[,] kernel = new float[5, 5]
{
    { 1,  4,  7,  4, 1 },
    { 4, 16, 26, 16, 4 },
    { 7, 26, 41, 26, 7 },
    { 4, 16, 26, 16, 4 },
    { 1,  4,  7,  4, 1 }
};
// Normalize kernel (sum = 273)

for (int y = 2; y < height - 2; y++)
{
    for (int x = 2; x < width - 2; x++)
    {
        float smoothed = 0f;

        for (int ky = 0; ky < 5; ky++)
        {
            for (int kx = 0; kx < 5; kx++)
            {
                int nx = x + kx - 2;
                int ny = y + ky - 2;
                smoothed += heightmap[ny, nx] * kernel[ky, kx] / 273f;
            }
        }

        result[y, x] = smoothed;
    }
}
```

**Performance**: ~200ms for 512×512 map

**Step 3: Ocean Flood Fill** (border-connected ocean detection):
```csharp
public bool[,] FillOcean(float[,] heightmap, float seaLevel)
{
    var oceanMask = new bool[height, width];
    var queue = new Queue<Position>();

    // Seed: All border cells below sea level
    for (int x = 0; x < width; x++)
    {
        if (heightmap[0, x] < seaLevel) queue.Enqueue(new Position(x, 0));
        if (heightmap[height - 1, x] < seaLevel) queue.Enqueue(new Position(x, height - 1));
    }
    for (int y = 0; y < height; y++)
    {
        if (heightmap[y, 0] < seaLevel) queue.Enqueue(new Position(0, y));
        if (heightmap[y, width - 1] < seaLevel) queue.Enqueue(new Position(width - 1, y));
    }

    // BFS flood-fill (4-directional)
    while (queue.Count > 0)
    {
        var pos = queue.Dequeue();
        if (oceanMask[pos.Y, pos.X]) continue;

        oceanMask[pos.Y, pos.X] = true;

        foreach (var neighbor in Get4Neighbors(pos))
        {
            if (heightmap[neighbor.Y, neighbor.X] < seaLevel)
            {
                queue.Enqueue(neighbor);
            }
        }
    }

    return oceanMask;
}
```

**Limitation**: Only detects **border-connected** ocean, excludes landlocked seas (intentional - see Known Issues below).

**Step 4: Ocean Floor Harmonization** (smooth underwater terrain):
```csharp
// Re-run Gaussian smoothing ONLY on ocean cells
for (int y = 2; y < height - 2; y++)
{
    for (int x = 2; x < width - 2; x++)
    {
        if (oceanMask[y, x])  // Only ocean cells
        {
            heightmap[y, x] = ApplyGaussianKernel(heightmap, x, y);
        }
    }
}

// Re-run ocean flood-fill (elevations changed after smoothing!)
oceanMask = FillOcean(heightmap, seaLevel);
```

**Why Re-run?** Smoothing can raise cells above sea level → ocean mask becomes stale.

---

## Outputs

```csharp
public record PostProcessingResult
{
    public float[,] OriginalHeightmap { get; init; }      // Raw physics [0.1-20]
    public float[,] PostProcessedHeightmap { get; init; } // Smoothed [0.1-20]
    public bool[,] OceanMask { get; init; }               // Border-connected ocean
    public ElevationThresholds Thresholds { get; init; }  // Quantile thresholds
}

public record ElevationThresholds
{
    public float SeaLevel { get; init; }      // 50th percentile
    public float HillLevel { get; init; }     // 75th percentile
    public float MountainLevel { get; init; } // 90th percentile
    public float PeakLevel { get; init; }     // 98th percentile
}
```

---

## Visualization

### View Modes (MapViewMode enum)

1. **OriginalElevation**: Raw plate tectonics output (before smoothing)
2. **PostProcessedElevation**: After Gaussian smoothing + harmonization
3. **OceanMask**: Binary visualization (ocean = blue, land = white)

### Legends

**Elevation Legend** (displayed in UI):
```
Sea Level:    1.00 RAW (0m)
Hills:        2.50 RAW (1500m)
Mountains:    5.00 RAW (4000m)
Peaks:        8.20 RAW (7200m)
```

**Probe Display** (click cell to inspect):
```
Position: (256, 128)
Original Elevation: 3.45 RAW (2450m) - Mountain
Post-Processed: 3.38 RAW (2380m) - Mountain
Ocean: No
Biome: Alpine Tundra
```

---

## Known Issues

### Issue 1: Ocean Mask Missing Inland Water

**Problem**: `FillOcean()` uses BFS from **borders only**, intentionally excluding landlocked seas/lakes.

**Example**:
```
Caspian Sea (landlocked):
- Elevation: 0.92 RAW (-80m below sea level)
- Ocean Mask: FALSE ❌ (not connected to borders)
- Expected: Should be marked as water for river flow algorithms
```

**Impact**: River flow algorithms need ALL water bodies (ocean + lakes), not just border-connected ocean.

**Root Cause**: Line 161 comment states: *"Ensures only cells connected to border oceans are marked as ocean (prevents landlocked seas)"*

**Decision**: Defer fix until VS_029 (erosion/rivers) - will implement **Option 2: Separate Ocean/Water Masks**

**Planned Fix**:
```csharp
public record PostProcessingResult
{
    public bool[,] OceanMask { get; init; }    // Border-connected ocean ONLY (gameplay)
    public bool[,] WaterMask { get; init; }    // Ocean + landlocked lakes (river flow)
}
```

### Issue 2: Ocean Mask Stale After Harmonization ✅ FIXED

**Problem**: `FillOcean()` ran BEFORE `HarmonizeOcean()`, so cells raised above sea level by smoothing remained marked as ocean.

**Example**:
```
Cell at (120, 45):
1. Initial elevation: 0.29 RAW → marked as ocean
2. Gaussian smoothing: 0.29 → 1.6 RAW (160m above sea level!)
3. Ocean mask: TRUE ❌ (incorrect - now land!)
```

**Fix Applied**: Re-run `FillOcean()` AFTER `HarmonizeOcean()`:
```csharp
// Algorithm 2: Flood-fill ocean detection (preliminary pass)
var oceanMask = FillOcean(heightmap, seaLevel);

// Algorithm 3: Smooth ocean floor (modifies elevations!)
HarmonizeOcean(heightmap, oceanMask);

// Algorithm 2B: Re-run flood-fill AFTER harmonization (NEW)
oceanMask = FillOcean(heightmap, seaLevel);  // Now accurate!
```

**Validation Tool**: `MapViewMode.OceanMask` debug view exposes mismatches clearly.

---

## Performance Characteristics

**Total Time**: ~1.5s for 512×512 map

**Breakdown**:
- Native simulation: 1.0s (67%)
- Post-processing: 0.2s (13%)
- Visualization: 0.3s (20%)

**Optimization Opportunities**:
- ❌ **C# Port**: 2-3 months effort, uncertain gains, high risk → NOT WORTH IT
- ✅ **API Improvements**: Snapshot getter (~50ms saved), parallelization (future)
- ✅ **Smaller Maps**: 256×256 = 4× faster (~0.4s) for prototyping

---

## Integration with Other Features

### Climate System Dependencies

**Temperature** (VS_025) uses **OriginalHeightmap**:
```csharp
// Mountain cooling (RAW elevation > MountainLevel threshold)
if (originalElevation > thresholds.MountainLevel)
{
    float altitudeFactor = 1.0f - (originalElevation - thresholds.MountainLevel) * 0.033f;
    temperature *= altitudeFactor;  // Cooling at altitude
}
```

**Why Original?** Physics requires **real elevation values** (3000m RAW), not normalized (0.8).

### Biome Classification Dependencies

**Biomes** use **PostProcessedHeightmap** + **Thresholds**:
```csharp
// Elevation band classification
if (elevation < thresholds.SeaLevel)
    return BiomeType.Ocean;
else if (elevation > thresholds.PeakLevel)
    return BiomeType.IceCap;  // High alpine
else if (elevation > thresholds.MountainLevel)
    return BiomeType.Alpine;
// ... etc
```

**Why Post-Processed?** Smoothing removes noise, quantiles adapt per-world.

### River Flow Dependencies

**VS_029** (rivers) needs **WaterMask** (ocean + lakes):
```csharp
// D-8 flow directions (rivers terminate at water)
if (waterMask[neighbor.Y, neighbor.X])
{
    flowDirection[y, x] = DIRECTION_TO_OCEAN;  // River ends
}
```

**Current Issue**: Only `OceanMask` exists (excludes landlocked lakes) → Will fix in VS_029.

---

## Serialization System (Format v2)

**Purpose**: Cache worlds for fast iteration (avoid 1.5s regeneration per test).

**Format v2** (backward compatible):
```csharp
public record WorldCache
{
    public int Version { get; init; } = 2;  // Format version
    public uint Seed { get; init; }
    public int MapSize { get; init; }
    public float[,] OriginalHeightmap { get; init; }      // NEW in v2
    public float[,] PostProcessedHeightmap { get; init; }
    public bool[,] OceanMask { get; init; }
    public ElevationThresholds Thresholds { get; init; }
}
```

**Backward Compatibility** (reads Format v1):
```csharp
public WorldCache LoadCache(string path)
{
    var data = File.ReadAllBytes(path);
    int version = BitConverter.ToInt32(data, 0);

    if (version == 1)
    {
        // v1: Only had PostProcessedHeightmap
        var cache = DeserializeV1(data);
        cache.OriginalHeightmap = cache.PostProcessedHeightmap;  // Fallback
        return cache;
    }
    else if (version == 2)
    {
        return DeserializeV2(data);
    }

    throw new InvalidDataException($"Unknown cache version: {version}");
}
```

**Performance**: 0ms reload (vs 1.5s regeneration) → 100× faster iteration!

---

## Test Coverage

**433 tests passing** (100% coverage for plate tectonics feature)

**Unit Tests** (TDD):
- Quantile threshold calculation (edge cases: all water, all land)
- Gaussian smoothing (kernel correctness, boundary handling)
- Ocean flood-fill (simple cases, islands, lakes)
- Sea level constant conversions (RAW ↔ Meters ↔ Normalized)

**Integration Tests**:
- Full pipeline (native sim → post-processing → outputs)
- Deterministic seeds (same seed = same world)
- Cache serialization (v1 ↔ v2 compatibility)

**Visual Validation**:
- WorldMapProbeNode (click-to-inspect elevation values)
- Dual view modes (original vs post-processed)
- Ocean mask debug view (binary visualization)

---

## Future Enhancements

### Phase 3: Plate Library Evaluation

**See**: [Geology_And_Resources.md](Geology_And_Resources.md#stage-0-plate-lib-evaluation--rewrite)

**Options**:
1. **Quick Wins**: Fix API bugs (memory leak, snapshot getter, RNG seeding) - 2-3 weeks
2. **C# Port**: Full WorldEngine-style port - 2-3 months
3. **Alternatives**: FastNoise-based, hybrid, custom simple plates - 1-2 months research

**Recommendation**: Start with Quick Wins, then A/B test alternatives using Strategy pattern (already enabled by TD_027 PipelineBuilder).

### Plate Boundary Detection

**Purpose**: Enable volcanic system and mineral prospectivity (Phase 3).

**Two Approaches**:
- **Option A**: Platec exposes boundaries via API (2-3h) ← Preferred
- **Option B**: Derive from elevation gradients + plate IDs (6-8h) ← Fallback

**Outputs Needed**:
```csharp
public record PlateBoundary
{
    public Position Location { get; init; }
    public BoundaryType Type { get; init; }  // Convergent, Divergent, Transform
    public float Velocity { get; init; }     // Relative plate velocity
}
```

---

## Related Documents

- [Pipeline_Architecture.md](1_Pipeline_Architecture.md) - How plate tectonics integrates into pipeline
- [Climate_System.md](Climate_System.md) - Uses OriginalHeightmap for mountain cooling
- [Geology_And_Resources.md](Geology_And_Resources.md) - Phase 3 enhancements (volcanoes, minerals)
- [Main Roadmap](0_Roadmap_World_Generation.md) - High-level overview

---

**Backlog Items**:
- [VS_024: Plate Tectonics & Elevation Post-Processing](../../../01-Active/Backlog.md) ✅ Complete
- [TD_021: Sea Level SSOT](../../../01-Active/Backlog.md) ✅ Complete

**Archive**:
- [Completed_Backlog_2025-10_Part2.md](../../../07-Archive/Completed_Backlog_2025-10_Part2.md) - Full VS_024 details

---

**Last Updated**: 2025-10-14 (Feature-based reorganization)
