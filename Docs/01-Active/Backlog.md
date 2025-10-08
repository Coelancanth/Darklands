# Darklands Development Backlog


**Last Updated**: 2025-10-09 03:27 (Tech Lead: VS_031 created - WorldGen Debug Panel for real-time parameter tuning with stage-based caching!)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 019
- **Next VS**: 032


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

### VS_029: WorldGen Stage 3 - Erosion & Rivers (Hydraulic Erosion)
**Status**: Approved (Ready for Dev Engineer)
**Owner**: Tech Lead → Dev Engineer (implement)
**Size**: M (8-10h estimate)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PIPELINE] [STAGE-3] [HYDROLOGY] [ARCHITECTURE]

**What**: Generate rivers and carve realistic valleys using **hydraulic erosion simulation** - improved algorithm design inspired by WorldEngine but optimized for performance

**Why**: Realistic terrain requires water erosion (valleys, river networks). Rivers spawn in wet mountains, flow to ocean/lakes, carve valleys over geological time. Critical for gameplay (river resources, navigation, terrain tactics).

**How** (Optimized 4-Phase Algorithm):

### Phase 1: Pit Filling + Flow Accumulation + Source Detection (O(n log n) - Hydrologically Correct!)
**Insight**: Preprocess heightmap to eliminate pits (no A* fallback needed!), then model drainage basins

```csharp
// Step 1a: Selective Pit Filling (O(n log n) - priority flood algorithm)
// Eliminates small pits (artifacts), keeps large pits (realistic lakes)
float[,] FillPitsSelectively(
    float[,] heightmap,
    float[,] precipitation,
    bool[,] oceanMask)
{
    // Identify all local minima (pits)
    var pits = FindLocalMinima(heightmap, oceanMask);

    // Classify pits: fill small/shallow, keep large/deep as lakes
    var smallPits = new List<Pit>();
    var lakeCandidates = new List<Pit>();

    foreach (pit in pits)
    {
        float depth = MeasureDepth(pit, heightmap);
        float area = MeasureArea(pit, heightmap);
        float inflow = EstimateInflow(pit, precipitation);

        // Heuristic: Fill noise artifacts, preserve real endorheic basins
        if (depth < 50f || area < 100)
            smallPits.Add(pit);  // Shallow/small → fill (would erode naturally)
        else
            lakeCandidates.Add(pit);  // Large/deep → keep as lake site
    }

    // Priority flood fill algorithm (O(n log n))
    // Raises pit floors to spillway level (ensures downhill path exists)
    return PriorityFloodFill(heightmap, oceanMask, smallPits);
}

// Step 1b: Compute flow directions on FILLED heightmap (O(n))
int[,] ComputeFlowDirections(float[,] filledHeightmap)
{
    for each cell:
        flowDir[cell] = FindSteepestNeighbor(cell, filledHeightmap);
    return flowDir;
}

// Step 1c: Topological sort for upstream→downstream processing (O(n log n))
List<(int x, int y)> TopologicalSort(int[,] flowDir)
{
    // Kahn's algorithm or DFS-based topological sort
    // Ensures all upstream cells processed before downstream cells
    return sortedCells;  // Headwaters → ocean order
}

// Step 1d: Flow accumulation (O(n) - single pass in topo order!)
float[,] AccumulateFlow(float[,] precipitation, int[,] flowDir, topologicalOrder)
{
    var flowAccum = new float[height, width];

    foreach (cell in topologicalOrder)  // ← KEY: upstream → downstream!
    {
        // Start with local precipitation
        flowAccum[cell] = precipitation[cell];

        // Add ALL upstream contributions (already computed!)
        foreach (upstream in GetUpstreamNeighbors(cell, flowDir))
            flowAccum[cell] += flowAccum[upstream];
    }
    return flowAccum;
}

// Step 1e: Find sources (O(n) - threshold check)
List<(int x, int y)> FindRiverSources(
    float[,] filledHeightmap,
    float[,] flowAccumulation,
    ElevationThresholds elevThresholds)
{
    // Only cells with LARGE catchment areas become sources!
    const float accumulationThreshold = 0.5f;  // Tunable (higher = fewer, larger rivers)

    for each cell:
        if (filledHeightmap[cell] >= elevThresholds.Mountain &&
            flowAccumulation[cell] >= accumulationThreshold)
            sources.Add(cell);

    return sources;
}
```

**Why pit filling is SUPERIOR to A* fallback**:
- ✅ **Standard practice** - Used in all GIS hydrology tools (ArcGIS, QGIS, GRASS)
- ✅ **Faster** - O(n log n) once vs O(rivers × radius²) repeatedly
- ✅ **Cleaner code** - No pathfinding complexity (simpler tracing)
- ✅ **Better visuals** - No A* detour artifacts (smooth natural flow)
- ✅ **Geological realism** - Selective filling preserves real lakes (endorheic basins)
- ✅ **Guaranteed success** - All rivers reach ocean OR lake (no stuck rivers!)

**Why this is MORE realistic than greedy alone**:
- ✅ **Models drainage basins** - water collects from upstream areas (actual hydrology!)
- ✅ **Natural river density** - only cells with large catchments become sources
- ✅ **River size ∝ catchment area** - matches real-world behavior
- ✅ **Prevents uniform grid** - no river in every wet mountain (unrealistic!)

**Complexity**: O(n log n) for pit filling + O(n log n) for topological sort = **O(n log n) total**

### Phase 2: Guaranteed River Tracing (O(rivers × path) - No A* Needed!)
**Insight**: Pit-filled heightmap guarantees all rivers reach ocean or lake!

```csharp
List<(int x, int y)> TraceRiver(
    (int x, int y) source,
    float[,] filledHeightmap,  // ← Pit-filled, guarantees downhill path!
    bool[,] oceanMask,
    Dictionary<(int,int), int> riverCellToId)
{
    var path = new List<(int, int)>();
    var current = source;

    while (true)
    {
        path.Add(current);

        // Termination checks
        if (oceanMask[current])
            return new River(path, reachedOcean: true);  // Success!

        if (riverCellToId.ContainsKey(current))
            return new River(path, reachedOcean: true);  // Merged into existing river!

        // Greedy descent (ALWAYS succeeds on filled heightmap!)
        var next = FindSteepestNeighbor(current, filledHeightmap);

        if (next == null)
        {
            // This is a REAL lake (large pit we chose not to fill)
            return new River(path, reachedOcean: false);  // Lake!
        }

        current = next;
    }
}
```

**Why this is SIMPLER than A* approach**:
- ❌ **Removed**: A* pathfinding (~200 lines of code)
- ❌ **Removed**: FindPathToLowerRegion radius search (~50 lines)
- ❌ **Removed**: Wrapped coordinate handling (~100 lines)
- ✅ **Result**: Trivial greedy descent (pit filling did the hard work!)
- ✅ **Guaranteed**: All rivers reach ocean OR lake (no stuck rivers!)
- ✅ **Deterministic**: No heuristic search, easier testing

### Phase 3: Distance Field Valley Erosion (O(n) - Major Improvement!)
```csharp
// WorldEngine: O(rivers × path × 25) per-cell radius checks
// Ours: O(n) distance field BFS + O(n) erosion application

void ErodeValleys(float[,] heightmap, List<River> rivers)
{
    // Single BFS for all rivers
    var distanceToRiver = BuildDistanceField(rivers, heightmap);

    // Apply erosion based on distance
    for each cell:
        if (distanceToRiver[cell] == 1)
            heightmap[cell] += (riverElev - heightmap[cell]) * 0.2f;
        else if (distanceToRiver[cell] == 2)
            heightmap[cell] += (riverElev - heightmap[cell]) * 0.05f;
}
```
**Benefits**: Single pass for all rivers, cleaner code, same visual quality

### Phase 4: Elevation Monotonicity (O(rivers × path))
Ensure rivers flow downhill monotonically (WorldEngine pattern, unchanged)

**Complexity Comparison**:
- **WorldEngine**: O(n³) worst case (buggy raster-scan accumulation)
- **Ours**: O(n log n) topological sort + O(n) accumulation = **O(n log n) total**
- **Performance**: ~100-200ms for 512×512 (topological sort is cache-friendly!)

**Key Architectural Decisions**:
1. **Selective pit filling** - standard GIS hydrology practice (eliminates pathfinding artifacts!)
2. **Proper flow accumulation** - models drainage basins (hydrologically correct!)
3. **Topological ordering** - fixes WorldEngine's bug (upstream → downstream processing)
4. **Tunable thresholds** - `accumulationThreshold` (river density), `pitDepth/pitArea` (lake classification)
5. **Distance field erosion** - single BFS instead of per-cell radius checks
6. **No A* pathfinding** - pit filling makes it unnecessary (simpler code, better visuals!)

**Integration**:
- **Location**: `Infrastructure/Pipeline/HydraulicErosionProcessor.cs`
- **Input**: `heightmap`, `oceanMask`, `finalPrecipitation` (from VS_028)
- **Output**: `ErosionResult(erodedHeightmap, rivers, lakes)`
- **Pattern**: Functional pipeline (pure functions, Result<T> monad)

**Data Structures**:
```csharp
public record River(
    List<(int x, int y)> Path,
    bool ReachedOcean);

public record ErosionResult(
    float[,] ErodedHeightmap,     // Modified heightmap with valleys
    List<River> Rivers,            // All river paths
    List<(int x, int y)> Lakes);  // Lake positions (dead-ends)
```

**Testing Strategy**:
1. **Unit tests** (15-18 tests):
   - Pit identification (local minima detection)
   - Pit classification (depth/area/inflow heuristics)
   - Priority flood fill (selective filling correctness)
   - Flow direction computation (steepest descent on filled heightmap)
   - Topological sort (upstream → downstream order)
   - Flow accumulation (catchment area calculation)
   - Source detection (large catchment + high elevation)
   - River tracing (greedy descent, merge, lake termination)
   - Valley erosion (distance field, erosion curves)
   - Monotonicity cleanup
2. **Visual validation**:
   - Small pits filled (no pathfinding detours!)
   - Large basins preserved as lakes (Dead Sea, Great Salt Lake analogs)
   - Rivers flow smoothly (no A* artifacts)
   - Fewer, larger rivers (not uniform grid!)
   - Rivers merge into major drainages (tributary systems)
3. **Performance benchmark**: <200ms for 512×512 map

**Done When**:
1. **Selective pit filling working** - small pits eliminated, large basins preserved as lakes
2. **Drainage basins modeled** - rivers have catchment areas (flow accumulation working!)
3. **Natural river density** - 5-15 major rivers per 512×512 map (not 100+ uniform grid!)
4. **Tributary systems** - smaller rivers merge into larger ones
5. Rivers spawn in locations with **large accumulated flow** (not just local wetness)
6. **All rivers reach ocean or lake** - no stuck rivers (pit filling guarantees!)
7. **No pathfinding artifacts** - smooth natural flow (no A* detours)
8. Valleys carved around river paths (subtle, radius 2, curves 0.2/0.05)
9. Eroded heightmap smoother than input (realistic weathering)
10. All 495 existing tests remain GREEN
11. 15-18 new tests pass (pit filling, flow accumulation, river tracing, erosion)
12. Performance <200ms for 512×512

**Depends On**: VS_028 ✅ (FINAL precipitation required)

**Blocks**: VS_022 Phase 4-6 (watermap, irrigation, humidity, biomes need rivers)

**Tech Lead Decision** (2025-10-09 03:23 - FINAL after pit filling analysis):
- **Algorithm**: Selective pit filling + flow accumulation + greedy tracing (best practices!)
- **Key insight 1**: Flow accumulation models drainage basins (hydrologically correct!)
- **Key insight 2**: Pit filling eliminates A* pathfinding (simpler, faster, better visuals!)
- **Realism vs Performance**: Chose realism! O(n log n) is still fast (<200ms), drainage basins critical for gameplay
- **WorldEngine improvements**:
  - Topological sort fixes raster-scan bug (upstream → downstream)
  - Pit filling replaces A* (standard GIS practice, removes pathfinding artifacts)
- **Tunable parameters**:
  - `accumulationThreshold` controls river density (fewer/larger vs many/smaller)
  - `pitDepth/pitArea` controls lake classification (which pits to preserve)
- **Code simplification**: Removed ~350 lines of A* pathfinding (pit filling is cleaner!)
- **MVP approach**: Get functional rivers working first (greedy tracing), defer particle physics to VS_030
- **Next steps**: Dev Engineer implements 4-phase algorithm with pit filling + flow accumulation

---

### VS_030: Particle-Based Erosion Enhancement (Visual Polish)
**Status**: Proposed (Deferred after VS_029)
**Owner**: Product Owner → Tech Lead (breakdown after VS_029 validated)
**Size**: L (20-30h estimate)
**Priority**: Ideas
**Markers**: [WORLDGEN] [ENHANCEMENT] [PARTICLE-PHYSICS] [POLISH]

**What**: Enhance VS_029's greedy river tracing with **particle-based erosion simulation** for realistic meandering rivers, sediment deposition, and physically-accurate valley formation

**Why**: VS_029 produces functional rivers with correct drainage basins, but greedy tracing creates **algorithmically straight** paths. Particle simulation adds:
- **Natural meandering** (momentum + inertia physics)
- **Sediment dynamics** (erosion in fast-flow, deposition in slow-flow)
- **Feedback loops** (micro-perturbations amplify into realistic curves)
- **State-of-the-art visual quality** (indistinguishable from real terrain!)

**When**: AFTER VS_029 complete AND gameplay validated (rivers enhance game experience)

**How** (Hybrid Flow + Particle Algorithm):

### Phase 1: Smart Particle Seeding (Use VS_029's Flow Blueprint!)
**Insight**: VS_029's flow accumulation map identifies WHERE rivers should form - use it as a blueprint!

```csharp
// Reuse VS_029 output: flowAccumulation map + source locations
List<Particle> SeedParticles(
    float[,] flowAccumulation,
    List<(int x, int y)> riverSources,
    float[,] finalPrecipitation)
{
    var particles = new List<Particle>();

    // Smart seeding: Only on high-flow cells (not entire map!)
    for each source in riverSources:
        // Seed ~100-200 particles per source
        int particleCount = (int)(flowAccumulation[source] * 200);

        for (i = 0; i < particleCount; i++):
            // Random position near source with small offset (Gaussian noise)
            var pos = source + GaussianOffset(stddev: 2.0f);
            var velocity = Vector2.Zero;
            var sediment = 0.0f;

            particles.Add(new Particle(pos, velocity, sediment));

    return particles;  // Total: ~1000-5000 particles (not millions!)
}
```

**Key optimization**: 90%+ fewer particles than naive "rain entire map" approach!

### Phase 2: Particle Physics Simulation (Momentum + Erosion + Deposition)
```csharp
// Simulate particle lifetime (~100-200 steps per particle)
void SimulateParticle(
    Particle p,
    float[,] heightmap,
    float[,] flowBlueprint,  // ← VS_029's flow accumulation (guides particles!)
    ParticleParams parameters)
{
    const int maxSteps = 200;
    const float deltaTime = 0.1f;

    for (step = 0; step < maxSteps; step++)
    {
        // ─────────────────────────────────────────────────────────────
        // Step 2a: Gravity + Momentum Physics
        // ─────────────────────────────────────────────────────────────
        var gradient = ComputeGradient(heightmap, p.position);
        var gravity = -gradient * parameters.gravityStrength;  // Downhill force

        // Apply momentum (prevents instant 90° turns!)
        p.velocity = p.velocity * parameters.inertia + gravity * deltaTime;
        p.position += p.velocity * deltaTime;

        if (OutOfBounds(p.position) || ReachedOcean(p.position))
            break;  // Particle exits simulation

        // ─────────────────────────────────────────────────────────────
        // Step 2b: Erosion Model (velocity-based terrain removal)
        // ─────────────────────────────────────────────────────────────
        float speed = p.velocity.Length();
        float erosionCapacity = speed * parameters.erosionFactor;  // Fast = more erosion

        // Erode terrain (remove height)
        float eroded = Math.Min(erosionCapacity, heightmap[p.position]);
        heightmap[p.position] -= eroded * deltaTime;
        p.sediment += eroded;  // Carry sediment

        // ─────────────────────────────────────────────────────────────
        // Step 2c: Deposition Model (slow flow = sediment drops)
        // ─────────────────────────────────────────────────────────────
        float depositThreshold = parameters.minSpeedForSuspension;
        if (speed < depositThreshold)
        {
            // Slow water drops sediment (creates floodplains, deltas!)
            float deposited = p.sediment * parameters.depositionRate * deltaTime;
            heightmap[p.position] += deposited;
            p.sediment -= deposited;
        }

        // ─────────────────────────────────────────────────────────────
        // Step 2d: Flow Blueprint Guidance (soft constraint)
        // ─────────────────────────────────────────────────────────────
        // Particles should generally follow VS_029's flow blueprint,
        // but momentum/physics can create natural deviations!
        var blueprintDirection = GetFlowDirection(flowBlueprint, p.position);
        p.velocity += blueprintDirection * parameters.blueprintWeight * deltaTime;
    }
}
```

**Physics parameters** (tunable for different terrain styles):
- `gravityStrength`: How strongly particles follow terrain slope (default: 4.0)
- `inertia`: Velocity retention 0-1 (0.7 = smooth curves, 0.3 = sharp bends)
- `erosionFactor`: How fast flowing water erodes (default: 0.5)
- `depositionRate`: How fast sediment settles (default: 0.3)
- `minSpeedForSuspension`: Speed threshold for deposition (default: 0.1)
- `blueprintWeight`: How strongly to follow VS_029's flow map (default: 0.2)

### Phase 3: River Extraction (Identify Carved Channels)
```csharp
// After all particles simulated, extract river paths from eroded heightmap
List<River> ExtractRivers(
    float[,] erodedHeightmap,
    float[,] originalHeightmap,
    float erosionThreshold)
{
    // Compute erosion depth map
    var erosionDepth = new float[height, width];
    for each cell:
        erosionDepth[cell] = originalHeightmap[cell] - erodedHeightmap[cell];

    // Connected component analysis: find channels
    var channels = FindConnectedErodedRegions(erosionDepth, erosionThreshold);

    // Trace centerline of each channel (river path)
    var rivers = new List<River>();
    foreach (channel in channels)
        rivers.Add(TraceCenterline(channel, erodedHeightmap));

    return rivers;
}
```

### Phase 4: Integration with VS_029 Pipeline
```csharp
// VS_029 provides the blueprint, VS_030 refines it!
public static ErosionResult ProcessWithParticles(
    float[,] heightmap,
    bool[,] oceanMask,
    float[,] finalPrecipitation,
    ElevationThresholds elevThresholds)
{
    // ═══════════════════════════════════════════════════════════════
    // PHASE 1: Run VS_029 (Flow Accumulation + Greedy Tracing)
    // ═══════════════════════════════════════════════════════════════
    var flowAccum = AccumulateFlow(finalPrecipitation, ...);
    var sources = FindRiverSources(heightmap, flowAccum, elevThresholds);
    var blueprintRivers = TraceRivers(sources, heightmap, ...);  // Greedy paths

    // ═══════════════════════════════════════════════════════════════
    // PHASE 2: Enhance with Particle Simulation (VS_030)
    // ═══════════════════════════════════════════════════════════════
    var particles = SeedParticles(flowAccum, sources, finalPrecipitation);
    var erodedHeightmap = heightmap.Clone();

    foreach (particle in particles)
        SimulateParticle(particle, erodedHeightmap, flowAccum, params);

    // ═══════════════════════════════════════════════════════════════
    // PHASE 3: Extract Final Rivers (Physically Carved)
    // ═══════════════════════════════════════════════════════════════
    var finalRivers = ExtractRivers(erodedHeightmap, heightmap, erosionThreshold);
    var lakes = IdentifyLakes(finalRivers, oceanMask);

    return new ErosionResult(erodedHeightmap, finalRivers, lakes);
}
```

**Complexity**:
- Flow accumulation: O(n log n) - from VS_029
- Particle simulation: O(k × s × 8) where k=particles (~5000), s=steps (~200) = **O(8M)** constant!
- River extraction: O(n) - connected components
- **Total: O(n log n)** - same asymptotic complexity as VS_029!

**Performance**:
- VS_029 alone: ~100-200ms
- VS_030 particle sim: ~300-500ms additional
- **Total: ~500-700ms for 512×512** (acceptable for world generation!)

**Key Benefits Over VS_029**:
1. ✅ **Natural meandering** - rivers curve realistically (not algorithmic straight lines)
2. ✅ **Sediment features** - floodplains, deltas, alluvial deposits
3. ✅ **Erosion-deposition feedback** - micro-perturbations amplify into realistic patterns
4. ✅ **State-of-the-art visual quality** - professional terrain generation
5. ✅ **Still fast** - smart seeding limits particles to <5000 (not millions!)

**Integration**:
- **Location**: `Infrastructure/Pipeline/ParticleErosionEnhancer.cs` (new)
- **Pattern**: Decorator over VS_029's `HydraulicErosionProcessor`
- **Interface**: `IErosionModel` allows swapping greedy vs particle (strategy pattern)
- **Configuration**: `UseParticleSimulation` flag in world gen params

**Testing Strategy**:
1. **Unit tests** (10-12 tests):
   - Particle physics (gravity, momentum, inertia)
   - Erosion model (velocity-based terrain removal)
   - Deposition model (sediment settling)
   - River extraction (centerline tracing)
2. **Visual validation**:
   - Rivers meander naturally (not straight greedy paths!)
   - Floodplains visible in slow-flow regions
   - Deltas form where rivers meet ocean
   - Valleys show erosion-deposition gradients
3. **Performance benchmark**: <700ms for 512×512 map
4. **A/B comparison**: Side-by-side VS_029 greedy vs VS_030 particle (visual quality delta)

**Done When**:
1. Particles follow physics (momentum prevents instant turns)
2. Rivers meander naturally (no greedy straight paths visible!)
3. Sediment deposition creates realistic features (floodplains, deltas)
4. Performance <700ms for 512×512
5. All 495+ existing tests remain GREEN
6. 10-12 new particle/physics tests pass
7. Visual quality indistinguishable from real satellite terrain
8. Parameters tunable via config (no hardcoded magic numbers!)

**Depends On**:
- VS_029 ✅ (provides flow blueprint + source detection)
- **Gameplay validation** - rivers must be valuable to game loop!

**Blocks**: Nothing (pure enhancement, VS_029 is self-sufficient)

**Tech Lead Decision** (2025-10-09 03:17):
- **Approach**: Hybrid flow + particle (best of both worlds!)
- **Timing**: Deferred until VS_029 complete + gameplay validated
- **Risk mitigation**: MVP principle - ship functional rivers first, polish later
- **Performance**: O(n log n) maintained via smart particle seeding
- **Quality vs Speed tradeoff**: Iteration speed now, visual fidelity after validation
- **Interface design**: Strategy pattern allows swapping greedy ↔ particle easily
- **Next steps**: Product Owner validates VS_029 provides gameplay value, then approves VS_030

---

### VS_031: WorldGen Debug Panel (Real-Time Parameter Tuning)
**Status**: Proposed (Implement after VS_029)
**Owner**: Product Owner → Dev Engineer (implement)
**Size**: M (6-8h estimate)
**Priority**: Ideas
**Markers**: [WORLDGEN] [DEBUG-TOOLS] [UX] [DEVELOPER-EXPERIENCE]

**What**: Real-time parameter tuning UI panel for world generation with **incremental stage-based regeneration** - instantly see the impact of parameter changes without full world regeneration

**Why**:
- **25+ tunable parameters** across worldgen pipeline (plate tectonics, climate, erosion, future stages)
- **Guess-compile-test cycle is slow** - Full regeneration takes 5-10 seconds
- **Visual feedback critical** - Procedural generation parameters interact in non-obvious ways
- **Artist-friendly** - Non-programmers can discover optimal values through experimentation
- **Development velocity** - 25× faster iteration when tuning VS_029/VS_030 parameters!

**When**: Immediately after VS_029 complete (use panel to tune erosion parameters!)

**How** (3-Phase Implementation):

### Phase 1: Stage-Based Cache Architecture (~2h)
**Insight**: Only regenerate stages AFTER the changed parameter (reuse cached upstream results!)

```csharp
// Cache all intermediate results
public class WorldGenCache
{
    // Stage 1: Plate Tectonics (~5s to regenerate)
    public float[,] RawHeightmap { get; set; }
    public uint[,] PlatesMap { get; set; }

    // Stage 2: Elevation Post-Processing (~0.5s)
    public float[,] PostProcessedHeightmap { get; set; }
    public bool[,] OceanMask { get; set; }

    // Stage 3: Climate (~1s)
    public float[,] TemperatureMap { get; set; }
    public float[,] PrecipitationMap { get; set; }

    // Stage 4: Erosion (VS_029 - ~0.2s) ✨
    public float[,] FilledHeightmap { get; set; }
    public float[,] FlowAccumulation { get; set; }
    public List<River> Rivers { get; set; }
    public List<(int,int)> Lakes { get; set; }

    // Future stages...
}

public enum WorldGenStage
{
    PlateTectonics,   // Slowest (~5s)
    Elevation,        // Fast (~0.5s)
    Climate,          // Medium (~1s)
    Erosion,          // Fast (~0.2s) ← VS_029!
    Watermap,         // TBD
    Biomes            // TBD
}

// Only regenerate stages after the changed parameter
public void RegenerateFromStage(WorldGenStage stage, WorldGenParams newParams)
{
    switch (stage)
    {
        case WorldGenStage.PlateTectonics:
            // Full regen (slowest)
            var raw = _plateSim.Generate(newParams);
            _cache.RawHeightmap = raw.Heightmap;
            _cache.PlatesMap = raw.PlatesMap;
            goto case WorldGenStage.Elevation;  // Fallthrough!

        case WorldGenStage.Elevation:
            var elev = _elevProcessor.Process(_cache.RawHeightmap, newParams);
            _cache.PostProcessedHeightmap = elev.Heightmap;
            _cache.OceanMask = elev.OceanMask;
            goto case WorldGenStage.Climate;

        case WorldGenStage.Climate:
            var climate = _climateCalc.Calculate(_cache.PostProcessedHeightmap, newParams);
            _cache.TemperatureMap = climate.Temperature;
            _cache.PrecipitationMap = climate.Precipitation;
            goto case WorldGenStage.Erosion;

        case WorldGenStage.Erosion:
            // ✨ FAST! Reuses cached heightmap + climate (0.2s!)
            var erosion = _erosionProcessor.Process(
                _cache.PostProcessedHeightmap,
                _cache.OceanMask,
                _cache.PrecipitationMap,
                newParams);
            _cache.FilledHeightmap = erosion.ErodedHeightmap;
            _cache.Rivers = erosion.Rivers;
            _cache.Lakes = erosion.Lakes;
            // Continue to watermap...
            break;
    }

    // Update visualization
    _worldView.DisplayWorld(_cache);
}
```

**Key optimization**: Changing `pitDepthThreshold` (Stage 4) only reruns erosion → 0.2s feedback instead of 5s!

### Phase 2: Godot UI Panel (~3h)
**Visual Design**: Collapsible stage sections with sliders + spinboxes

```gdscript
# WorldGenDebugPanel.gd (Godot C# node)
public partial class WorldGenDebugPanel : Control
{
    // UI Components per stage
    private VBoxContainer _stageContainer;

    public override void _Ready()
    {
        // Stage 1: Plate Tectonics (8 params)
        var stage1 = CreateStageSection("Stage 1: Plate Tectonics");
        stage1.AddParameter("Seed", 12345, 0, 999999, WorldGenStage.PlateTectonics);
        stage1.AddParameter("Plate Count", 15, 10, 25, WorldGenStage.PlateTectonics);
        stage1.AddParameter("Sea Level", 1.0f, 0.5f, 2.0f, WorldGenStage.PlateTectonics);
        stage1.AddRegenButton("Regenerate Stage 1+", "~5.2s", WorldGenStage.PlateTectonics);

        // Stage 2: Climate (6 params)
        var stage2 = CreateStageSection("Stage 2: Climate");
        stage2.AddParameter("Axial Tilt", 23.5f, 0f, 45f, WorldGenStage.Climate);
        stage2.AddParameter("Distance to Sun", 1.0f, 0.8f, 1.2f, WorldGenStage.Climate);
        stage2.AddRegenButton("Regenerate Stage 2+", "~1.8s", WorldGenStage.Climate);

        // Stage 3: Erosion (VS_029 - 5+ params) ⚙️
        var stage3 = CreateStageSection("Stage 3: Erosion & Rivers ⚙️");
        stage3.AddParameter("Pit Depth Threshold", 50f, 10f, 200f, WorldGenStage.Erosion);
        stage3.AddParameter("Pit Area Threshold", 100, 50, 500, WorldGenStage.Erosion);
        stage3.AddParameter("Accumulation Threshold", 0.5f, 0.1f, 2.0f, WorldGenStage.Erosion);
        stage3.AddParameter("Valley Erosion Radius", 2, 1, 5, WorldGenStage.Erosion);
        stage3.AddParameter("Erosion Curve Factor", 0.2f, 0.05f, 0.5f, WorldGenStage.Erosion);
        stage3.AddRegenButton("Regenerate Stage 3+", "~0.2s ✨", WorldGenStage.Erosion);

        // Future stages (collapsed by default)
        CreateStageSection("Stage 4: Watermap (Future)", collapsed: true);
        CreateStageSection("Stage 5: Biomes (Future)", collapsed: true);

        // Preset system
        AddPresetButtons();
    }

    private void OnParameterChanged(string paramName, float newValue, WorldGenStage stage)
    {
        // Update params
        _worldGenParams.Set(paramName, newValue);

        // Auto-regenerate (instant for erosion stage!)
        RegenerateFromStage(stage, _worldGenParams);
    }
}
```

**UI Features**:
- **HSlider + SpinBox** linked (drag slider OR type exact value)
- **Collapsible sections** (VBoxContainer with toggle button)
- **Timing display** on regen buttons (shows expected duration)
- **Visual diff highlighting** (optional: show changed rivers in green/red overlay)
- **Auto-regeneration** on parameter change (optional: manual button mode)

### Phase 3: Preset System (~1-2h)
**Purpose**: Share interesting parameter combinations, ship with examples

```json
// presets/tropical_archipelago.json
{
  "name": "Tropical Archipelago",
  "description": "Many small islands with wet climate and dense rivers",
  "author": "Tech Lead",
  "created": "2025-10-09",
  "params": {
    "plateCount": 25,
    "seaLevel": 1.5,
    "axialTilt": 10.0,
    "pitDepthThreshold": 30,
    "pitAreaThreshold": 75,
    "accumulationThreshold": 0.8,
    "valleyErosionRadius": 3
  }
}

// presets/pangaea.json
{
  "name": "Pangaea (Supercontinent)",
  "description": "Single large landmass with major river systems",
  "author": "Tech Lead",
  "created": "2025-10-09",
  "params": {
    "plateCount": 8,
    "seaLevel": 0.8,
    "axialTilt": 25.0,
    "pitDepthThreshold": 100,
    "accumulationThreshold": 0.3,
    "valleyErosionRadius": 4
  }
}
```

**Implementation**:
```csharp
public class PresetManager
{
    public Result<WorldGenParams> LoadPreset(string presetPath)
    {
        // JSON deserialization
        var json = File.ReadAllText(presetPath);
        var preset = JsonSerializer.Deserialize<PresetData>(json);

        // Validate params (ensure ranges correct)
        return ValidateAndApply(preset.Params);
    }

    public Result SavePreset(string name, string description, WorldGenParams currentParams)
    {
        var preset = new PresetData
        {
            Name = name,
            Description = description,
            Author = "User",
            Created = DateTime.Now.ToString("yyyy-MM-dd"),
            Params = currentParams.ToDictionary()
        };

        var json = JsonSerializer.Serialize(preset, options: new { WriteIndented = true });
        File.WriteAllText($"presets/{SanitizeFilename(name)}.json", json);
        return Result.Success();
    }
}
```

**Shipped Presets** (3-5 examples):
1. **Default** - Balanced earth-like world
2. **Tropical Archipelago** - Many islands, dense rivers
3. **Pangaea** - Supercontinent with major drainages
4. **Arid Desert World** - Low precipitation, rare rivers
5. **Ice Age** - High axial tilt, minimal erosion

**Integration**:
- **Location**: `godot_project/features/worldgen/WorldGenDebugPanel.tscn` (Godot scene)
- **Code**: `godot_project/features/worldgen/WorldGenDebugPanel.cs` (Godot C# script)
- **Access**: Debug menu or press `F3` during world view
- **Configuration**: Toggle in world gen settings (show/hide panel)

**Performance Targets**:
- **Stage 4 (Erosion) regen**: <200ms (instant feedback!)
- **Stage 3 (Climate) regen**: <1s (smooth iteration)
- **Stage 2 (Elevation) regen**: <1s (acceptable)
- **Stage 1 (Tectonics) regen**: ~5s (rare, only when changing seed)

**Testing Strategy**:
1. **Unit tests** (6-8 tests):
   - Cache invalidation (changing param triggers correct stage)
   - Parameter validation (ranges enforced)
   - Preset serialization (save/load roundtrip)
   - Stage dependency graph (erosion depends on climate, etc.)
2. **Manual testing**:
   - All 25+ parameters exposed and functional
   - Sliders update spinboxes (and vice versa)
   - Presets load correctly (all params applied)
   - Regeneration time matches estimates (±20%)
3. **UX testing**:
   - Panel doesn't freeze during regeneration
   - Visual feedback clear (which stage is running)
   - Collapsible sections work (save screen space)

**Done When**:
1. All 25+ worldgen parameters exposed in collapsible UI
2. Stage-based incremental regeneration working (cache architecture)
3. Erosion parameter changes update world in <200ms (instant feedback!)
4. Climate parameter changes update world in <1s (smooth iteration)
5. Preset save/load working (JSON serialization)
6. Ships with 3-5 example presets (archipelago, pangaea, desert, ice age, default)
7. UI responsive (no freezing during regeneration)
8. Panel toggleable via debug menu or F3 hotkey
9. All existing tests remain GREEN (no regression)

**Depends On**:
- VS_029 ✅ (erosion parameters are primary use case!)
- Existing climate/elevation stages (VS_024-028)

**Blocks**: Nothing (pure debug tool, worldgen works without it)

**Benefits Over Manual Tuning**:
- ✅ **25× faster iteration** - 0.2s vs 5s for erosion param changes
- ✅ **Visual discovery** - See parameter impact immediately
- ✅ **Non-programmer friendly** - Artists/designers can tune worlds
- ✅ **Preset sharing** - Distribute interesting worlds as JSON
- ✅ **VS_030 preparation** - Will be invaluable for tuning 6+ particle physics params!

**Tech Lead Decision** (2025-10-09 03:27):
- **Timing**: Implement immediately after VS_029 (use to tune erosion params!)
- **Value**: HUGE ROI - saves hours during VS_029/VS_030 tuning
- **Pattern**: Industry standard (Houdini, Gaea, Unity all use this approach)
- **Architecture**: Stage-based cache with fallthrough regeneration
- **Risk**: Low complexity (mostly UI plumbing + JSON serialization)
- **Next steps**: Dev Engineer implements 3-phase plan after VS_029 merged

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

**STAGE 1: TECTONIC FOUNDATION**
1. **Phase 1: Elevation Post-Processing** ✅ COMPLETE (VS_024, M, ~8h actual)
   - ✅ Ported 4 WorldEngine algorithms (~150 lines): add_noise, fill_ocean, harmonize_ocean, sea_depth
   - ✅ Dual-heightmap architecture: Original raw + Post-processed raw (both [0.1-20] range)
   - ✅ Quantile-based thresholds: SeaLevel, HillLevel, MountainLevel, PeakLevel (adaptive per-world)
   - ✅ Real-world meters mapping: ElevationMapper for UI display (Presentation layer utility)
   - ✅ BFS flood-fill ocean detection (OceanMask, not simple threshold)
   - ✅ FastNoiseLite integration: 8-octave OpenSimplex2 noise for terrain variation
   - ✅ Three colored elevation views: Original, Post-Processed, Normalized (visual validation)
   - ✅ Format v2 serialization: Saves post-processed data with backward compatibility (TD_018)
   - **Outcome**: Foundation complete for climate stages, all 433 tests GREEN

**STAGE 2: ATMOSPHERIC CLIMATE (Instantaneous processes, no terrain modification)**
2. **Phase 2: Climate - Complete Precipitation Pipeline** (PARTIALLY COMPLETE)

   **2a. Temperature** ✅ COMPLETE (VS_025, S, ~5h actual)
   - ✅ 4-component temperature algorithm: Latitude (92%, with axial tilt) + Noise (8%, FBm fractal) + Distance-to-sun (inverse-square) + Mountain-cooling (RAW elevation thresholds)
   - ✅ Per-world climate variation: AxialTilt and DistanceToSun (Gaussian-distributed) create hot/cold planets with shifted equators
   - ✅ 4-stage debug visualization: LatitudeOnly → WithNoise → WithDistance → Final (isolates each component for visual validation)
   - ✅ Normalized [0,1] output: Internal format for biome classification (Stage 6), UI converts to °C via TemperatureMapper
   - ✅ MathUtils library: Interp() for latitude interpolation, SampleGaussian() for per-world parameters
   - ✅ Multi-stage testing: 14 unit tests (Interp edge cases, Gaussian distribution validation, temperature ranges)
   - ✅ Visual validation passed: Smooth latitude bands, subtle noise variation, hot/cold planets, mountains blue at all latitudes
   - ✅ Performance: ~60-80ms for temperature calculation (no threading needed, native sim dominates at 83%)
   - **Outcome**: Temperature maps ready, all 447 tests GREEN

   **2b. Base Precipitation** ✅ COMPLETE (VS_026, S, ~3.5h actual)
   - ✅ 3-stage algorithm: Noise (6 octaves) → Temperature gamma curve → Renormalization
   - ✅ Multi-stage debug visualization: NoiseOnly → TemperatureShaped → Final
   - ✅ Quantile-based thresholds (30th/70th/95th percentiles for classification)
   - ✅ WorldEngine algorithm exact match (gamma=2.0, curveBonus=0.2)
   - **Outcome**: Base precipitation ready for geographic modifiers, all 457 tests GREEN

   **2c. Rain Shadow Effect** ✅ COMPLETE (VS_027, S, ~3h actual)
   - ✅ Latitude-based prevailing winds (Polar Easterlies / Westerlies / Trade Winds)
   - ✅ Orographic blocking: Upwind mountain trace (max 20 cells ≈ 1000km)
   - ✅ Accumulative reduction (5% per mountain, max 80% total blocking)
   - ✅ Real-world desert patterns (Sahara, Gobi, Atacama validation)
   - **Outcome**: Rain shadow precipitation ready, 481/482 tests GREEN (99.8%)

   **2d. Coastal Moisture Enhancement** (VS_028, S, 3h) ✅ **COMPLETE** (2025-10-09)
   - ✅ Distance-to-ocean BFS (O(n) flood fill, copied from VS_024 ocean fill pattern)
   - ✅ Exponential decay: `bonus = 0.8 × e^(-dist/30)` - matches real atmospheric moisture transport
   - ✅ Coastal bonus: 80% at coast (dist=0), 29% at 1500km (dist=30), <10% deep interior (dist=60+)
   - ✅ Elevation resistance: `factor = 1 - min(1, elev × 0.02)` - mountain plateaus resist coastal penetration
   - ✅ Additive enhancement: Preserves rain shadow deserts while adding maritime climate effect
   - ✅ Real-world validation: Maritime (Seattle, UK) wetter than continental (Spokane, central Asia)
   - **Outcome**: FINAL PRECIPITATION MAP ready (Stage 5 complete), 495/495 tests GREEN (100%)

**STAGE 3: HYDROLOGICAL PROCESSES (Slow geological processes, terrain modification)**
3. **Phase 3: Erosion & Rivers** (VS_029, M, ~8-10h) ← AFTER VS_028
   - River source detection (uses FINAL PRECIPITATION from VS_028)
   - River path tracing (downhill flow to ocean/lakes)
   - Valley carving (erosion around river paths, radius 2, gentle curves)
   - **Output**: Eroded heightmap, rivers[], lakes[]
   - **Critical**: Uses final precipitation (rivers spawn in realistically wet locations)

4. **Phase 4: Watermap Simulation** (M, ~3-4h)
   - Droplet flow model (20,000 droplets weighted by final precipitation)
   - Flow accumulation (recursive downhill distribution)
   - Quantile thresholds (creek 5%, river 2%, main river 0.7%)
   - **Output**: Watermap (flow intensity per cell)

5. **Phase 5: Irrigation & Humidity** (M, ~3-4h)
   - Irrigation: Logarithmic kernel (21×21 neighborhood, moisture spreading from watermap)
   - Humidity: Combine precipitation × 1 + irrigation × 3 (hydrologic moisture boost)
   - Quantile-based classification (8-level moisture: superarid → superhumid)
   - **Output**: Humidity map (final moisture for biome classification)

6. **Phase 6: Biome Classification** (M, ~6h)
   - 48 biome types (WorldEngine catalog)
   - Classification: temperature + humidity + elevation
   - Biome transitions (smooth gradients, not hard borders)
   - Biome visualization + legends
   - **Output**: Biome map

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