WorldEngine implementation review (References/worldengine)
=========================================================

Date: 2025-10-06
Scope: Ultra-careful review of `References/worldengine` to catalog supported functions and what it can simulate.

Overview
--------
WorldEngine is a procedural world generator built around physical simulations. It creates elevation and tectonic plates using a plate-tectonics simulation, derives oceans and sea depth, then runs climate and hydrology simulations to produce temperature, precipitation, erosion, rivers/lakes, humidity, permeability, irrigation, biomes, and icecaps. It includes a CLI, serialization (protobuf, optional HDF5), image renderers (e.g., elevation/biome/temperature/ancient-map/satellite), and export to GDAL-supported raster formats.

Core data model
---------------
- `worldengine.model.world.World` holds:
  - Dimensions, seed, generation parameters (`n_plates`, ocean level, `Step`), temperature/humidity quantiles, gamma curve/offset.
  - `layers` dictionary of typed layers (plain, with thresholds, with quantiles):
    - Elevation (+ thresholds for sea/plain/hill/mountain)
    - Plates (plate ID per cell)
    - Ocean (bool), Sea depth (normalized), Temperature (+ thresholds), Precipitation (+ thresholds), Humidity (+ quantiles), Irrigation, Permeability (+ thresholds), Watermap (+ thresholds for creek/river/main river), River map, Lake map, Biome (string per cell), Icecap (thickness)
  - Rich query helpers: land/ocean tests, mountain/hill detectors, stream membership, temperature/humidity bucket tests, sampling random land, threshold/level accessors, etc.
  - Serialization: protobuf read/write; HDF5 via optional module; version/tag markers.

World generation pipeline (what it simulates)
--------------------------------------------
High-level flow (plates → noise/border shaping → ocean/thresholds → climate/hydrology → biome/ice):
1) Plate tectonics (elevation + plates)
   - Function: `worldengine.plates.generate_plates_simulation` (C extension `platec`).
   - Produces heightmap and plates map; sim parameters include sea level, erosion period, folding ratio, overlap thresholds, cycles, number of plates.
2) Post-processing of elevation
   - `center_land`: shifts elevation/plates to push ocean to map borders.
   - `add_noise_to_elevation`: coherent noise added to elevation.
   - `place_oceans_at_map_borders`: lowers elevation near borders to favor wrap-friendly oceans.
   - `initialize_ocean_and_thresholds`:
     - `fill_ocean`: flood-fill below sea level from borders to classify ocean.
     - `harmonize_ocean`: smooth ocean floor; compute thresholds for elevation classes.
     - `sea_depth`: approximate normalized depth from sea level, modulated by distance to land and anti-aliased.
3) Climate simulations
   - Temperature (`TemperatureSimulation`):
     - Uses latitude banding with axial tilt and distance-to-sun randomization, coherent noise, and altitude lapse-rate effect vs mountain level.
     - Buckets into thresholds: polar, alpine, boreal, cool, warm, subtropical, tropical.
   - Precipitation (`PrecipitationSimulation`):
     - Coherent noise field (wrap-aware), normalized then gamma-shaped by temperature using `gamma_curve` and `curve_offset` to limit cold wetness; normalized to [-1, 1]; thresholds low/med/high.
4) Hydrology and geomorphology
   - Erosion and rivers (`ErosionSimulation`):
     - Computes per-cell flow direction and river sources based on precipitation and mountain tests; traces river paths to sea with A* fallbacks, forms lakes for dead-ends, erodes elevation around rivers (valley carving), builds `river_map` and `lake_map`.
   - Water map (`WatermapSimulation`):
     - Droplet model seeded on random land (weighted by precipitation) accumulates flow; thresholds for creek/river/main river.
   - Irrigation (`IrrigationSimulation`):
     - Spreads influence of watermap via a logarithmic kernel in a 21×21 neighborhood.
   - Humidity (`HumiditySimulation`):
     - Combines precipitation and irrigation (weights 1 and 3 respectively), produces humidity field with quantiles (12/25/37/50/62/75/87) used for buckets: superarid → superhumid.
   - Permeability (`PermeabilitySimulation`):
     - Noise-based substrate permeability; thresholds low/med/high.
5) Biomes (`BiomeSimulation`)
   - Holdridge-style classification combining temperature bucket and humidity bucket into one of 39 land biomes; ocean treated separately. Produces `biome` layer and counts per class.
6) Icecaps (`IcecapSimulation`)
   - Freezing over ocean tiles based on coldest temperature threshold and RNG, with neighbor influence; produces ice thickness map.

CLI operations and options (how to run it)
-----------------------------------------
- Operations: `world`, `plates`, `ancient_map`, `info`, `export`.
- Key options:
  - General: `--output-dir`, `--worldname`, `--hdf5`, `--seed`, `--step` (`plates|precipitations|full`), `--width`, `--height`, `--number-of-plates`, `--recursion_limit`, `--verbose`, `--version`, `--bw`.
  - Generate-only: `--rivers`, `--grayscale-heightmap`, `--ocean_level`, `--temps`, `--humidity`, `--gamma-value`, `--gamma-offset`, `--not-fade-borders`, `--scatter`, `--sat`, `--ice`.
  - Ancient map: `--worldfile`, `--generatedfile`, `--resize-factor`, `--sea_color [blue|brown]`, `--not-draw-biome`, `--not-draw-mountains`, `--not-draw-rivers`, `--draw-outer-border`.
  - Export: `--export-format`, `--export-datatype`, `--export-dimensions`, `--export-normalize`, `--export-subset`.

Image rendering (what maps it can produce)
-----------------------------------------
- Elevation: simple elevation, grayscale heightmap (scaled), shaded variants.
- Oceans: ocean/land mask, sea depth shading.
- Climate: precipitation (note: renderer actually uses humidity buckets), temperature level map, humidity scatter plot.
- Biomes: categorical color map.
- Rivers/Lakes: river map overlay, lakes.
- Satellite: biome-colored, elevation- and noise-modulated with smoothing and shading, with river/lake overlays and ice paint.
- Ancient map: stylized parchment-like rendering with forests/deserts/mountains patterns and optional outer land border.

Export and serialization
------------------------
- Serialization:
  - Protobuf: write/read `.world` with version tag and embedded generation params and all layers; `World.protobuf_serialize()`, `World.open_protobuf()`.
  - HDF5: optional `save_world_to_hdf5` when HDF5 libs are present.
- Export:
  - `worldengine.imex.export` writes elevation raster via GDAL to formats like GTiff/PNG/etc., with optional resizing, normalization, or subsetting. Uses ENVI as intermediate and `gdal.Translate` for processing.

Library entry points and notable functions (what functions it supports)
----------------------------------------------------------------------
- World creation and steps:
  - `worldengine.plates.world_gen(name, width, height, seed, temps, humids, num_plates, ocean_level, step, gamma_curve, curve_offset, fade_borders, verbose)` → fully simulated `World`.
  - `worldengine.plates.generate_plates_simulation(seed, width, height, ..., num_plates, ...)` → `(heightmap, platesmap)` raw arrays.
  - `worldengine.step.Step.get_by_name("plates|precipitations|full")` and presets `Step.full()/precipitations()/plates()` controlling which simulations run.
- Generation helpers (pre/post plate stage):
  - `center_land(world)`, `place_oceans_at_map_borders(world)`, `add_noise_to_elevation(world, seed)`, `initialize_ocean_and_thresholds(world, ocean_level)`; internal: `fill_ocean`, `harmonize_ocean`, `sea_depth`.
- Simulations (execute pattern):
  - `TemperatureSimulation.execute(world, seed)`
  - `PrecipitationSimulation.execute(world, seed)`
  - `ErosionSimulation.execute(world, seed)` (rivers/lakes + erosion)
  - `WatermapSimulation.execute(world, seed)` (creek/river/main river thresholds)
  - `IrrigationSimulation.execute(world, seed)`
  - `HumiditySimulation.execute(world, seed)`
  - `PermeabilitySimulation.execute(world, seed)`
  - `BiomeSimulation.execute(world, seed)`
  - `IcecapSimulation.execute(world, seed)`
- CLI helpers:
  - `generate_world(...)` (CLI wrapper: persists `.world` and writes standard images), `generate_plates(...)`, `generate_grayscale_heightmap`, `generate_rivers_map`, `draw_scatter_plot`, `draw_satellite_map`, `draw_icecaps_map`.
  - `load_world(path)`, `print_world_info(world)`.
- Drawing to disk (`worldengine.draw`):
  - `draw_*_on_file(...)` for: `ancientmap`, `biome`, `ocean`, `precipitation`, `grayscale_heightmap`, `simple_elevation`, `temperature_levels`, `riversmap`, `scatter_plot`, `satellite`, `icecaps`, `world`.
- Basic utilities:
  - `common.set_verbose/get_verbose/anti_alias/count_neighbours`, `basic_map_operations.distance/index_of_nearest`.

Key configuration knobs
-----------------------
- Map size: `--width/--height`.
- Plates and sea level: `--number-of-plates`, `--ocean_level` (affects `fill_ocean`).
- Climate shape: `--temps` (6 quantiles), `--humidity` (7 quantiles), `--gamma-value`, `--gamma-offset`.
- Borders/tiling friendliness: `--not-fade-borders` (by default borders fade to ocean), wrap-aware noise/river logic.
- Output selection: `--rivers`, `--gs`, `--scatter`, `--sat`, `--ice`, image colorization via `--bw`.

Notable behaviors and caveats
-----------------------------
- Noise fields (temperature/precipitation/permeability) are wrap-aware at left/right edges to avoid seams.
- Some routines use a local `numpy.RandomState(seed)` to ensure reproducibility per sub-simulation; others (e.g., adding elevation noise) use the global RNG once during generation.
- The precipitation renderer in `draw_precipitation` colors by humidity buckets (FIXME noted in code), not raw precipitation.
- Ice formation currently applies only on ocean tiles; freezing of rivers/lakes is TODO.
- Performance-sensitive areas: erosion tracing with A*, irrigation convolution, anti-alias/count-neighbours kernels.

What it can simulate (concise list)
-----------------------------------
- Plate tectonics and orogeny (mountain chains).
- Elevation-based land/ocean classification and sea depth approximation.
- Global temperature distribution with latitude tilt, altitude correction, and stochastic variation.
- Precipitation patterns modulated by temperature (gamma curve) and noise.
- River networks, lakes, and erosion of terrain around rivers.
- Surface water accumulation intensity (watermap).
- Irrigation influence from nearby water.
- Humidity (annual average) from precipitation and irrigation.
- Substrate permeability patterns.
- Biomes via Holdridge-like classification across 39 land types.
- Ocean icecaps with thickness distribution.

Outputs (standard artifacts)
----------------------------
- `.world` protobuf (or HDF5) containing all layers and generation params.
- PNG images: ocean, elevation, temperature, precipitation (humidity-colored), biome, grayscale heightmap, rivers, icecaps; optional satellite and ancient map renders; optional scatter plot.
- GDAL raster exports of elevation in chosen format/bit-depth with optional resize/normalize/subset.

References to important files in this repo snapshot
---------------------------------------------------
- Core: `worldengine/worldengine/plates.py`, `worldengine/worldengine/simulations/__init__.py`, `worldengine/worldengine/model/world.py`, `worldengine/worldengine/step.py`
- Simulations: `worldengine/worldengine/simulations/{temperature,precipitation,hydrology,irrigation,humidity,erosion,permeability,biome,icecap}.py`
- CLI: `worldengine/worldengine/cli/main.py`
- Rendering: `worldengine/worldengine/draw.py`, `worldengine/worldengine/drawing_functions.py`
- Export: `worldengine/worldengine/imex/__init__.py`
- Utilities: `worldengine/worldengine/common.py`, `worldengine/worldengine/basic_map_operations.py`

Bottom line
-----------
WorldEngine supports a comprehensive set of functions for procedural planet generation and environmental simulation, exposing both a scriptable library API and a full-featured CLI. It simulates plates/elevation, climate (temperature/precipitation), hydrology and erosion (rivers/lakes/water accumulation), soil permeability, irrigation, humidity, biome classification, and icecaps, and can serialize and render these layers in multiple formats.


