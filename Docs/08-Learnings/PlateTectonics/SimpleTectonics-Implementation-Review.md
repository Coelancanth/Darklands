## SimpleTectonics – Implementation Review (Control Flow Focus)

Date: 2025-10-09
Scope: Control-flow and lifecycle review of `References/SimpleTectonics` (clustered convection plate tectonics). Emphasis on per-frame update order, module responsibilities, GPU passes, and practical portability.

---

### Executive Summary

- SimpleTectonics simulates plate tectonics using clustered convection: a Voronoi segmentation of Poisson-disc centroids produces many small lithospheric segments that are grouped into plates. Plate kinematics are driven by a heatmap gradient (convection), while plate–plate interaction moves mass via simplified subduction and redistribution.
- The system is a real-time demo built on TinyEngine with heavy GPU participation (Voronoi, diffusion, subduction, cascading), not a reusable library. It renders both plate and terrain views and updates the world every frame.
- Strengths: clear conceptual model; GPU-accelerated Voronoi and surface effects; visually engaging; explicit per-segment buoyancy (mass/area/thickness → density → height); Poisson seeding and dynamic gap filling maintain coverage.
- Risks/Trade-offs: tight TinyEngine and shader coupling; non-deterministic `rand()` usage; simplified physics (local scans for collision, heuristic transfer); ambiguous “colliding” vs “alive” flags; not straightforward to embed in a C#/Godot pipeline.

---

### How It Runs (Entrypoint and Frame Loop)

- Entrypoint: `SimpleTectonics.cpp`
  - Defines constants: `SIZE=256`, `nplates=12`, `K` (Voronoi sites), time step `DT`.
  - Initializes TinyEngine window, shaders (default, depth, flat, diffusion, cascading, subduction).
  - Constructs `World world(SEED)`, builds initial plate and (optionally) surface models.
  - Per-frame loop when `animate` is true:
    - For each plate: `plate.update(world.cluster, world.heatmap)`
    - Surface effects: `world.subduct(...)` (GPU) → `world.sediment(...)` (GPU if surface view)
    - World maintenance: `world.update()` (segment/plate housekeeping; cluster recompute)
    - Rebuild models for rendering (plates or terrain)

---

### Data Model and Modules

- Segments and Plates (source/tectonics.h)
  - `Segment`: base with `vec2* pos`, `area`.
  - `Litho : Segment`: adds kinematics and material properties: `speed`, `alive`, `colliding`, `mass`, `thickness`, derived `density`, `height`, `growth`, `plateheight`, pointer to parent `Plate`. `buoyancy()` computes `density=mass/(area*thickness)`, `height=thickness*(1-density)`.
  - `Plate`: owns `vector<Litho*> seg`, kinematics (`pos`, `speed`, `rotation`, `angveloc`), totals (`mass`, `area`, `inertia`, `height`), parameters (`convection`, `growth`). Methods: `recenter()` recomputes center, inertia, averaged height; `update(cluster, heatmap)` drives one step.

- Clustering (source/cluster.h)
  - `Cluster<T>` manages Voronoi segmentation on GPU:
    - Fields: `points` (centroids), `segs` (T*), GPU resources: `Billboard target`, `Shader voronoi`, `Instance` for instanced centroid draws, `indexmap` buffer.
    - `init()`: Poisson-disc sampling (K points), allocate GPU resources, build `segs` from `points`, compute initial Voronoi and segment areas.
    - `update()`: render Voronoi texture; read back `indexmap` and per-segment `area` from SSBO.
    - `sample(vec2 p)`: returns segment index at pixel p (or -1 for void) by reading `indexmap` color.
    - `add`, `reassign`, `remove` support dynamic segmentation changes.

- World Container (source/tectonics.h)
  - Fields: `plates`, `cluster`, `heatmap`, `heightmap`, scratch `tmpmap`, billboard textures `heatA/B`, `heightA/B`, and a depthmap.
  - `initialize()`:
    - Perlin heatmap generation and normalization to [0,1].
    - Heightmap baseline set to sea level (~0.31) and wrapped in billboards.
    - Create `nplates` with random positions.
    - Assign each cluster segment to the nearest plate, set `parent`, then `recenter()` plates.
  - `update()`:
    - Cull segments moved off-world; remove dead segments from plates; prune empty plates.
    - `cluster.remove` segments where `!alive` and reassign.
    - Gap fill: for each plate/segment, sample a random scan direction; if the sampled cluster pixel is void, add a new cluster point there, push new segment into plate, `cluster.reassign()`, `recenter()`.
    - `cluster.update()` to recompute Voronoi/areas.
  - Surface effects:
    - `subduct(diffusion, subduction, flat, n)`:
      - Build a `colliding` int buffer by marking segments where `!alive` as 1; bind as SSBO.
      - Alternating passes: diffuse heat (`diffusion.fs`) then apply `subduction.fs` using the cluster texture → update `heatA/B`.
      - Read back `heatA` into `heatmap`.
    - `sediment(cascading, flat, n)`:
      - Build a `height` buffer from segment heights; bind to shader.
      - Alternating passes of `cascading.fs` to build sediment offsets into `heightA/B`.
      - Read back `heightA` decodes RGB into float heightmap values.

---

### Plate::update() – Detailed Control Flow

1) Collision detection and subduction (local neighborhood scans):
   - For each segment `s` in the plate:
     - If out of world bounds → `s->alive=false` and continue.
     - Sample current segment index (unused for exclusion beyond bounds check).
     - Scan `n=12` directions on a circle radius `CR*SIZE` around `s->pos`.
     - For each neighbor position:
       - If inside world and maps to a segment `n` from a different plate `parent`:
         - If `s->density > n->density` and neighbor not already marked `colliding`:
           - Transfer thickness/mass from `s` to `n` (`n->thickness += hdiff`, `n->mass += mdiff`), recompute `buoyancy()`, set `n->colliding=true`, mark `s->alive=false`.

2) Post-collision redistribution (secondary ring):
   - For each cluster segment `s` with `colliding=true`:
     - Scan `n=24` directions radius `R*SIZE` around `s->pos`.
     - For neighbor segment `n` (excluding self):
       - Compute `hdiff = s->height - n->height - 0.01` (bias); if positive, compute `mdiff`.
       - Transfer a fraction (`trate=0.2`) of height/mass from high to low; update buoyancy for both; finally set `s->colliding=false`.

3) Growth (deposition/dissolution by heat):
   - For each alive segment `s`:
     - Sample normalized heat `nd = heatmap[ip]` at integer pos.
     - Linear growth `G = growth * (1-nd) * (1-nd - s->density * s->thickness)`; if negative, damp by 0.05 (dissolution).
     - Equilibrium density modifier `D = langmuir(3, 1-nd)`.
     - Update `s->mass += s->area * G * D`, `s->thickness += G`, then derive `density` and `height`.

4) Convection (force and torque):
   - Define `force(i, heatmap)` via central differences at integer `i = *(s->pos)`; zeroed on borders.
   - For each segment `s`: compute `f = force(*(s->pos))` and direction `dir=*(s->pos)-plate.pos`.
   - Accumulate plate-level linear `acc -= convection*f` and torque `torque -= convection * |dir| * |f| * sin(angle(f)-angle(dir))`.

5) Integrate plate kinematics and advect segments:
   - `speed += DT * acc / mass`, `angveloc += 1e4 * DT * torque / inertia`.
   - `pos += DT*speed`, wrap rotation into [0,2π].
   - For each segment, compute new rotated position around updated plate center, update `s->speed` and `*(s->pos)` accordingly.

6) Note: `plate.recenter()` is not called within `update()`. Re-centering happens in `world.update()` after culls/additions.

---

### Cluster Update and World Maintenance

- After all plates update and surface passes run, `world.update()`:
  - Removes off-world segments from plates and prunes empty plates.
  - Deletes `cluster` segments where `!alive`.
  - Attempts to fill voids by sampling a random scan direction per segment; if `cluster.sample(scan)` is void, adds a new cluster point/segment, reassigns segment → plate mapping, re-centers the plate.
  - Re-renders Voronoi, updates per-segment `area` via SSBO, and refreshes the integer `indexmap` (used for later sampling and void detection).

---

### Orchestration Summary (One Animated Frame)

```cpp
// plate motion & interactions
for (Plate& p : world.plates) {
  p.update(world.cluster, world.heatmap); // collisions, growth, convection, advection
}
// surface responses (GPU)
world.subduct(&diffusion, &subduction, &flat, 25); // heat diffusion + subduction stencil
if (viewsurface) {
  world.sediment(&cascading, &flat, 25); // cascading to height
}
// world housekeeping
world.update(); // cull/add segments, prune empty plates, recompute Voronoi/areas
// rebuild render models (plates or terrain)
```

---

### Notable Details, Assumptions, and Risks

- GPU dependence and engine coupling: Voronoi/area tracking, subduction heat diffusion, and cascading are shader-driven within TinyEngine abstractions. Porting requires rewriting compute passes.
- Collision semantics:
  - Within `Plate::update`, `n->colliding=true` flags receivers; `s->alive=false` flags donors (subducted). Later, `world.subduct` marks `colliding[i]=1` when `!alive`. This mismatch suggests the heat pass uses “dead segments” rather than those flagged `colliding`, which may or may not match intent.
- Boundary handling: force gradient zeroed at borders; out-of-bounds segments are culled; no toroidal wrapping (unlike platec/worldengine).
- Determinism: frequent `rand()` calls (seeded once in `World` constructor) and order-dependent floating math → minor changes can alter behavior.
- Stability/termination: There’s no explicit cycle/restart policy; simulation runs until user stops. Plates can lose all segments and be pruned.
- Units and calibration: Many magic constants (`trate=0.2`, `-0.01` bias, `1e4` torque scale) govern dynamics; tuning is empirical.

---

### Recommendations

- Treat as inspiration rather than a direct dependency for our C# pipeline.
  - The clustered-convection concept is compelling, but porting the shader pipeline and engine coupling would be high effort with uncertain gains vs. established `platec` outputs.
- If adopting ideas:
  - Use Poisson-disc centroids and a CPU Voronoi/Delaunay to seed plate centers over a heightmap; let an existing tectonics solver (e.g., `platec`) evolve elevation and plates.
  - Consider a heat-like scalar field to bias plate motions or a post-process to place convergent/divergent boundaries for visuals, but keep the physical solver authoritative.
- Improve clarity if extending this codebase:
  - Align the “colliding” vs. “alive” logic across `Plate::update` and `World::subduct`.
  - Parameterize magic numbers; expose a config; swap `rand()` for a passed-in RNG for reproducibility.
  - Add wrap support if seamless maps are needed.

---

### Portability to C# (If Insisted)

- Minimal viable port (CPU-first):
  - Replace TinyEngine with pure arrays; implement Poisson-disc sampling, CPU Voronoi (or approximate raster growth), and per-segment bookkeeping.
  - Re-create `Plate::update` logic; drop GPU surface passes initially and compute a simple heightmap from segment heights.
  - Expect multiple weeks of engineering for a robust, testable result; performance will be lower without GPU.
- Preferred hybrid: keep `platec` for tectonics; borrow clustered visualization or seeding if needed.

---

### File Map (Reviewed)

- Entrypoint and loop: `SimpleTectonics.cpp`
- Core logic: `source/tectonics.h` (Plate/Litho/World, update/subduct/sediment)
- Clustering & Voronoi (GPU): `source/cluster.h` (+ `source/shader/voronoi.*`)
- Utilities/infra: `source/poisson.h`, `source/scene.h`, various render shaders (cascading, diffusion, subduction, flat, depth)

---

### Recommendation for Darklands

- For tectonics: use `platec` (the C++ plate-tectonics library we already integrate) as the foundation. It yields physically grounded elevation and a reliable control flow suitable for offline or background generation.
- For hydrology/erosion: continue with our planned SimpleHydrology/WorldEngine-derived pipeline (erosion/watermap/irrigation/humidity) layered on top of `platec` elevation.
- Use SimpleTectonics ideas selectively (e.g., centroid seeding or stylized boundary visuals) if we need faster mockups or artistic control.

In short: prefer `platec` for tectonics in production; treat SimpleTectonics as a reference/demo rather than a core dependency.
