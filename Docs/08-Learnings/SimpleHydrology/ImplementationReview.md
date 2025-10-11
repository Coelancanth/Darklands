### SimpleHydrology – Implementation Review

This document provides a deep code-level review of `References/SimpleHydrology` (C++ particle-based hydrology/erosion). It covers architecture, data flow, algorithm details, correctness/performance considerations, and concrete recommendations for porting into our C# worldgen pipeline.

---

### Executive Summary

- **Strengths**: Compact, readable implementation; physically motivated droplet model; discharge and momentum fields create plausible river meandering; mass-conservative sediment/evaporation; clean memory layout (interleaved cell pool); reusable map/mesh update path; decoupled parameters.
- **Main Risks**: Removed/unfinished pooling/flooding (lakes/basins); limited boundary handling; potential mass loss at edges; cascade slope equalization is heuristic; global/static parameters; single-resolution map (no quadtree/LOD yet); tight coupling to TinyEngine for rendering.
- **Priority Fixes**: Edge deposition on OOB termination; parameter surface exposure; deterministic RNG; unit tests for mass conservation, bounds, and stability; optional rate limiting on momentum coupling; restore flooding (or adopt priority-queue basin fill) when needed.

---

### Architecture Overview

- **Entrypoint**: `SimpleHydrology.cpp`
  - Window, camera, shaders, SSAO/lighting, framebuffer setup.
  - Seeds RNG; initializes `World::map`, vertex pool; main loop calls `World::erode(quad::tilesize)` and `Vegetation::grow()`, then updates mesh and hydrology debug textures.

- **Domain & Data Structures**
  - `quad::cell` (in `source/cellpool.h`): height, discharge, momentum (x,y), per-step tracks, rootdensity.
  - `quad::node`: tile wrapper over a `mappool::slice<cell>` with spatial helpers `get/oob/height/discharge/normal`.
  - `quad::map`: fixed-size grid of `node`s; initialization via FastNoiseLite (8-octave FBm), normalization; spatial queries; normal computation.
  - `mappool::pool<T>` and `slice<T>`: interleaved contiguous storage, iterable slices, bounds-checked indexed access.
  - `Vertexpool<Vertex>`: persistently-mapped OpenGL vertex/index buffers; exposes `section`, `fill`, `index`, `render`.
  - `World` (in `source/world.h`): globals (SEED, `map`), erosion parameters, `erode(cycles)`, `cascade(pos)`.
  - `Drop` (in `source/water.h`): particle state and `descend()`.
  - `Vegetation`/`Plant` (in `source/vegetation.h`): simple growth/kill and root-density feedback to erosion.

- **Rendering**
  - Not required for algorithm understanding; uses TinyEngine pipelines/shaders to render terrain and debug maps: `dischargeMap`, `momentumMap`.

---

### Hydrology & Erosion Algorithm

#### 1) Droplet particle update (water.h)

Parameters (defaults shown):
- `Drop::maxAge=500`, `minVol=0.01`, `evapRate=0.001`, `depositionRate=0.1`
- `Drop::entrainment=10.0`, `gravity=1.0`, `momentumTransfer=1.0`

State per droplet:
- `pos: vec2`, `speed: vec2`, `volume: float`, `sediment: float`, `age: int`

Core step (pseudocode):
```c++
bool Drop::descend() {
  // Convert position to int grid for sampling
  ivec2 ipos = pos;
  node = World::map.get(ipos); cell = node->get(ipos);
  n = World::map.normal(ipos); // surface normal

  // termination: age or dry – deposit remaining sediment locally
  if (age > maxAge || volume < minVol) { cell->height += sediment; return false; }

  // effective deposition rate reduced by vegetation roots
  float effD = max(0, depositionRate * (1.0f - cell->rootdensity));

  // forces
  // gravity downslope (use x/z components of normal)
  speed += quad::lodsize * gravity * vec2(n.x, n.z) / volume;
  // momentum coupling with existing field (meander), projected along droplet dir
  vec2 fspeed = vec2(cell->momentumx, cell->momentumy);
  if (|fspeed|>0 && |speed|>0) {
    float align = dot(normalize(fspeed), normalize(speed));
    speed += quad::lodsize * momentumTransfer * align / (volume + cell->discharge) * fspeed;
  }

  // timestep normalization → constant step length (≈ 1 cell)
  if (|speed|>0) speed = (quad::lodsize * sqrt(2.0f)) * normalize(speed);
  pos += speed;

  // track discharge and momentum (for low-pass filtering later)
  cell->discharge_track += volume;
  cell->momentumx_track += volume * speed.x;
  cell->momentumy_track += volume * speed.y;

  // slope to next sample, special handling at borders
  float h2 = World::map.oob(pos) ? (cell->height - 0.002f) : World::map.height(pos);

  // sediment capacity: proportional to local drop (cell.height - h2),
  // boosted by local discharge (entrainment factor)
  float c_eq = (1.0f + entrainment * node->discharge(ipos)) * (cell->height - h2);
  c_eq = max(0.0f, c_eq);
  float cdiff = c_eq - sediment; // positive → entrain, negative → deposit

  sediment += effD * cdiff;
  cell->height -= effD * cdiff;

  // evaporate (mass-conservative: increase concentration while lowering volume)
  sediment /= (1.0f - evapRate);
  volume   *= (1.0f - evapRate);

  if (World::map.oob(pos)) { volume = 0.0f; return false; }

  World::cascade(pos); // small-scale sediment exchange with neighbors
  age++;
  return true;
}
```

Notes:
- The droplet path length is normalized per step, decoupling speed magnitude from timestep size and stabilizing movement.
- Momentum coupling biases flow along established channels (meandering behavior emerges).
- Evaporation preserves total mass (water+sediment) by concentrating sediment.
- Borders apply a small artificial drop (−0.002) to encourage exit; however, exiting droplets do not deposit remaining sediment (potential mass leak; see recommendations).

#### 2) Global erosion step (world.h)

Erosion loop:
```c++
void World::erode(int cycles) {
  // reset accumulators
  for (node : map.nodes) for (cell,pos : node.s) {
    cell.discharge_track = 0; cell.momentumx_track = 0; cell.momentumy_track = 0;
  }

  // spawn droplets per node
  for (node : map.nodes) for (i=0; i<cycles; ++i) {
    vec2 newpos = node.pos + ivec2(rand()%tileres.x, rand()%tileres.y);
    if (node.height(newpos) < 0.1) continue; // skip deep water
    Drop drop(newpos);
    while (drop.descend());
  }

  // low-pass filter fields
  for (node : map.nodes) for (cell,pos : node.s) {
    cell.discharge = (1-lrate)*cell.discharge + lrate*cell.discharge_track;
    cell.momentumx = (1-lrate)*cell.momentumx + lrate*cell.momentumx_track;
    cell.momentumy = (1-lrate)*cell.momentumy + lrate*cell.momentumy_track;
  }
}
```

Parameters:
- `World::lrate=0.1` (exponential smoothing of discharge/momentum)
- `World::maxdiff=0.01`, `World::settling=0.8` (used by cascade)

#### 3) Sediment cascade (local slope equalization)

Heuristic redistribution among 8 neighbors, sorted by neighbor height; transfers a fraction of the excess height beyond a distance-scaled threshold.
```c++
void World::cascade(vec2 pos) {
  // gather in-bounds neighbors, with their heights and distances
  // sort ascending by neighbor height
  for (neighbor in ascending_height) {
    float diff = h(pos) - h(neighbor);
    if (diff == 0) continue;
    float threshold = (neighbor.h > 0.1) ? neighbor.distance * maxdiff * lodsize : 0.0f;
    float excess = max(0.0f, abs(diff) - threshold);
    float transfer = settling * excess / 2.0f;
    // move height toward neighbor by transfer (sign based on diff)
  }
}
```

Role: smooths sharp steps left by discrete droplet updates; damped by terrain “wetness” (proxy via height>0.1) and neighbor distance.

---

### Terrain Initialization (noise)

- `quad::map::init` creates one tile (`mapsize=1`, `tilesize=512`) with a `mappool::slice<cell>` backed by an interleaved buffer.
- Height generation uses FastNoiseLite OpenSimplex2 FBm (8 octaves), accumulates noise, then normalizes to [0,1].
- Additional noise variables (`scale`, `d`) are prepared for shaping but currently not applied (leftover from experiments).

---

### Vegetation Feedback

- Plants spawn stochastically on sufficiently flat, undisturbed land below a height cap and low discharge.
- Each plant increases `rootdensity` around itself (cross + diagonals with falloff), which reduces `effD` (effective deposition rate), indirectly affecting erosion/deposition.
- Plants can die with chance or if local discharge/height exceed thresholds; on death they remove their root influence.

Observation: Higher root density reduces both entrainment and deposition via `effD` → may warrant reviewing desired ecological effect (is the goal to reduce erosion or to increase cohesion while still allowing deposition?).

---

### Performance & Complexity

- Time per erosion step scales with `nodes * cycles * average_drop_path_length`.
- Each droplet performs O(1) work per step; path length bounded by age and map size. Cascade is O(8 log 8) = O(1) per droplet step.
- Memory is compact: one interleaved `cell` buffer per tile; mesh vertices generated via persistent-mapped VBO through `Vertexpool`.

Optimization opportunities:
- Spawn-position sampling can be weighted by current precipitation/discharge fields (if available) to focus work.
- Parallelize droplet loops per node (no shared mutable cell state during step if using per-step accumulators only; careful with `cascade` modifications → may require double-buffering heights or tiling with halos).
- SIMD-friendly refactors for inner arithmetic; consider compute shaders if staying in C++/GL.

---

### Correctness & Robustness Notes

1) Edge mass leak on OOB termination
- Exiting droplets set `volume=0` and stop without depositing remaining `sediment`.
- Suggestion: deposit residual sediment to the last in-bounds `cell` before returning.

2) Border slope hack
- `h2 = cell.height - 0.002` at borders creates a small artificial gradient; acceptable to encourage outflow but can bias edge carving. Consider making it a parameter and/or a function of local slope.

3) Static globals & determinism
- Parameters are static; RNG uses `rand()` seeded once. For repeatability and testing, encapsulate RNG and parameters in a context object passed to updates.

4) Cascade heuristic
- Uses neighbor height thresholding by distance and a global `maxdiff`; may over-flatten or under-smooth depending on scale. Consider slope-based adaptive thresholds or diffusing only a bounded fraction per step.

5) Unused/undeclared members
- `World::dischargeThresh` declared but not defined/used. Safe (no ODR use) but should be removed or implemented.
- Height init has variables `scale`, `d` prepared but unused.

6) Saturation of discharge
- `node::discharge()` applies `erf(0.4*discharge)`. This compresses dynamic range; ensure downstream logic expects this non-linear scale.

---

### Portability to C# and Our Pipeline

Target: integrate as hydrology/erosion step between climate and humidity in `Darklands.Core` worldgen (cf. TD_009). Options:

- Data structures
  - `cell` → C# struct with: `float Height, Discharge, MomentumX, MomentumY, RootDensity; float DischargeTrack, MomentumXTrack, MomentumYTrack`.
  - Map storage: 2D arrays, row-major; consider a single interleaved array for cache locality.

- Algorithm
  - Implement `Drop` update as a method operating on arrays; inject PRNG and parameters.
  - Replace GLM with `System.Numerics` or plain math; normals via cross products of height differences.
  - Expose parameters via config; pass deterministic seed.
  - Optionally incorporate precipitation map to bias source spawning and entrainment.

- Parallelization
  - Use `Parallel.For` on droplet batches; serialize only the final accumulator writes (use thread-local accumulators then reduce), or partition map into tiles with halos.

- Outputs (for pipeline)
  - Eroded `Heightmap`
  - `DischargeMap` and `Momentum` for diagnostics
  - Rivers can be inferred by thresholding `DischargeMap` (percentiles), or by tracking high-traffic droplet paths.

Integration sketch:
```csharp
public sealed record HydrologyParams(
  int DropletsPerTile,
  int MaxAge,
  float MinVolume,
  float EvapRate,
  float DepositionRate,
  float Entrainment,
  float Gravity,
  float MomentumTransfer,
  float LowPassRate,
  float CascadeMaxDiff,
  float CascadeSettling);

public static HydrologyData SimulateHydrology(HeightData height, ClimateData climate, HydrologyParams p, int seed) {
  // allocate discharge/momentum + accumulators
  // for (tile) for (n = DropletsPerTile) spawn and walk droplet
  // low-pass filter fields
  // return eroded height + fields
}
```

---

### Recommendations (Concrete)

1) Deposit on OOB exit
```c++
if (World::map.oob(pos)) {
  // deposit remaining sediment at last valid cell
  cell->height += sediment;
  volume = 0.0f;
  return false;
}
```

2) Parameterization & determinism
- Wrap all static parameters in a `ErosionConfig` passed to `erode`/`Drop`.
- Use a per-simulation RNG (PCG/XorShift) for reproducibility; avoid global `rand()`.

3) Cascade calibration
- Make `maxdiff` scale-aware (e.g., proportional to local slope magnitude) or clamp transfers to a fraction of `diff`.

4) Use precipitation to bias sources (optional)
- Prefer spawning droplets in higher-precipitation or higher-slope areas to reduce wasted work on flats.

5) Restore/replace flooding (lakes)
- Adopt a priority-queue flood/basin fill: grow a basin boundary by repeatedly raising the minimum boundary until a spill path exists; fill pits and record lake extents. This integrates well after erosion to identify stable lakes.

6) Expose discharge thresholds (rivers)
- Compute percentiles of `DischargeMap` to derive creek/river/main river thresholds (aligns with our Watermap step).

7) Tests
- Unit: mass conservation within tolerance; edge behavior; cascade monotonicity (no negative heights); reproducibility for fixed seed.
- Property: droplet path length bounded; no NaNs; heights remain within [min,max] bounds.

---

### Comparison to WorldEngine Hydrology

- SimpleHydrology focuses on particle-based erosion with emergent channels; WorldEngine splits concerns: erosion (rivers/lakes with A*), watermap (droplet flow density), irrigation, humidity.
- We can combine: use this erosion to carve valleys and produce discharge; then compute watermap thresholds and irrigation; finally humidity → biome classification.

---

### “Done When” for Port

- C# implementation produces:
  - Eroded `Heightmap` consistent with droplet-carved valleys
  - `DischargeMap` and `Momentum`
  - Optional river masks via percentile thresholds
- Deterministic per-seed outputs
- Edge-case tests pass; no mass leak at borders
- Parameters configurable; no global static state
- Performance: ≤ 2s for 512×512 with 1–2M droplet steps on desktop CPU (parallelized)

---

### File Pointers (Reference)

- `References/SimpleHydrology/SimpleHydrology.cpp`
- `References/SimpleHydrology/source/world.h`
- `References/SimpleHydrology/source/water.h`
- `References/SimpleHydrology/source/vegetation.h`
- `References/SimpleHydrology/source/cellpool.h`
- `References/SimpleHydrology/source/include/math.h`

---

### Conclusion

SimpleHydrology provides a clear, compact particle-based erosion/hydrology model with discharge/momentum feedback that yields convincing channels and meanders. With minor fixes (edge deposition, parameterization, deterministic RNG) and a thin C# port that fits our functional pipeline, it can serve as the erosion/hydrology backbone feeding our humidity and biome classification steps.


