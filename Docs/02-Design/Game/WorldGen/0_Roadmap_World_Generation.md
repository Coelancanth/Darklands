# World Generation Roadmap

**Purpose**: High-level index for Darklands world generation system - Dwarf Fortress-inspired procedural worldgen with physics-driven simulation.

**Last Updated**: 2025-10-14 (Tech Lead: Feature-based modular reorganization)

**Parent Document**: [Roadmap.md](Roadmap.md#world-generation) - Main project roadmap

---

## Vision & Philosophy

**Vision**: Dwarf Fortress-inspired procedural worldgen with physics-driven simulation (plate tectonics → geology → climate → hydrology → biomes → resources). Designed as **standalone game** foundation + Darklands strategic layer integration.

**Philosophy**:
- **Incremental pipeline** - One feature at a time, fully tested and visualized before moving to next
- **Physics-first** - Realistic causality (elevation affects temperature, temperature affects precipitation, precipitation creates rivers)
- **Designer empowerment** - Visual debugging (multi-stage view modes, probe, legends) for rapid iteration
- **Modular architecture** - Each feature standalone, no big-bang integration

**Design Principles**:
1. **Prove the problem exists** - Don't add features until we see generated worlds need them
2. **WorldEngine validation** - Use proven algorithms where available, innovate only when necessary
3. **Performance budget** - <2s for 512×512 world generation (fast iteration)
4. **Seed reproducibility** - Same seed = same world (balanced gameplay, bug reproduction)

---

## Current State

**Completed Features**:
- ✅ **Pipeline Architecture** (TD_027/TD_028) - Stage-based with PipelineBuilder, feedback loop support → [Details](WorldGen/Pipeline_Architecture.md)
- ✅ **Plate Tectonics** (VS_024, TD_021) - Native C++ integration, dual-heightmap architecture → [Details](WorldGen/Plate_Tectonics.md)
- ✅ **Climate System** (VS_025-028) - Temperature, precipitation, rain shadow, coastal moisture → [Details](WorldGen/Climate_System.md)

**Test Coverage**: 468/468 non-WorldGen tests GREEN (100% pass rate)

**Performance**: <2s for 512×512 world (native sim 1.0s + post-processing 0.5s + climate 0.2s)

**Pipeline Modes** (TD_027):
- **Fast Preview** (Single-Pass): Climate → Erosion (~2s generation, fast iteration)
- **High Quality** (Iterative): (Erosion → Climate) × 3-5 iterations (~6-10s, convergence quality)

**Next Priority**: Hydrology & Rivers (VS_029-031) → Biomes to complete Phase 1 MVP

---

## Feature Modules (Detailed Documentation)

### Core Features (Phase 1 - MVP)

| Feature | Status | Description | Details |
|---------|--------|-------------|---------|
| **Pipeline Architecture** | ✅ Complete | Stage-based orchestration with Builder pattern, feedback loops, presets | [→ Pipeline_Architecture.md](WorldGen/Pipeline_Architecture.md) |
| **Plate Tectonics** | ✅ Complete | Elevation generation via native simulation, dual-heightmap, quantile thresholds | [→ Plate_Tectonics.md](WorldGen/Plate_Tectonics.md) |
| **Climate System** | ✅ Complete | Temperature + precipitation (latitude, noise, rain shadow, coastal moisture) | [→ Climate_System.md](WorldGen/Climate_System.md) |
| **Hydrology & Rivers** | ⏳ Planned | Particle erosion, river extraction, humidity, debug panel | [→ Hydrology_And_Rivers.md](WorldGen/Hydrology_And_Rivers.md) *(planned)* |
| **Biome Classification** | ⏳ Planned | 48-type Holdridge system (temperature × humidity × elevation) | [→ Biome_Classification.md](WorldGen/Biome_Classification.md) *(planned)* |

### Extensions (Phase 2-3)

| Feature | Status | Description | Details |
|---------|--------|-------------|---------|
| **Visual Extensions** | 💡 Ideas | Swamp detection, creek/river visualization, slope maps | [→ Visual_Extensions.md](WorldGen/Visual_Extensions.md) *(planned)* |
| **Geology & Resources** | 💡 Ideas | Volcanoes, mineral deposits, ore veins, geological biomes | [→ Geology_And_Resources.md](WorldGen/Geology_And_Resources.md) |

---

## Three-Phase Development

### Phase 1: Core Pipeline (MVP - 85% Complete)

**Goal**: Realistic worlds with climate + rivers + biomes

**Timeline**: 2-3 weeks

**Features**:
- ✅ Pipeline Architecture (orchestration, builder pattern, presets)
- ✅ Plate Tectonics (elevation generation)
- ✅ Climate System (temperature + precipitation with all geographic effects)
- 🔄 Hydrology & Rivers (erosion, river extraction, humidity) ← **NEXT**
- ⏳ Biome Classification (48 types)

**Status**: ~85% complete, ~1-2 weeks remaining

### Phase 2: Hydrology Extensions (Quick Wins - Not Started)

**Goal**: Enhanced water features (swamps, creek visualization)

**Timeline**: 1 week

**Features**:
- Swamp biome classification (low elevation + high humidity + flat terrain)
- Creek/river/main river visual distinction (line thickness, colors)
- Slope map calculation (foundation for thermal erosion + swamp detection)

**Status**: Blocked by Phase 1

### Phase 3: Geology & Resources (DF-Inspired Depth - Design Phase)

**Goal**: Standalone worldgen game features

**Timeline**: 3-4 weeks

**Features**:
- **Stage 0**: Plate library evaluation (native fixes vs C# port vs alternatives)
- **Stage 0.5**: Geological foundation (plate boundaries, volcanoes, mineral prospectivity, ore veins, thermal erosion)
- **Stage 5**: Extended biome types (volcanic, mineral-rich, swamp subtypes)
- **Stage 6**: Resource distribution (minerals, vegetation, strategic resources)

**Status**: Architecture defined (post-process approach validated)

**See**: [Geology_And_Resources.md](WorldGen/Geology_And_Resources.md) for full Phase 3 architecture

---

## Quick Reference

### Performance Budget

**Target**: <2s for 512×512 world generation

**Current** (Phase 1, 85% complete):
```
Plate Tectonics:         1.0s (50%)
Elevation Post-Process:  0.2s (10%)
Climate System:          0.2s (10%)
Visualization:           0.3s (15%)
Remaining Budget:        0.3s (15%)
─────────────────────────────────
Total:                   2.0s ✅ ON BUDGET
```

**Projected** (Phase 1 complete):
```
+ Hydrology (VS_029):    0.5s (erosion + rivers)
+ Humidity:              0.1s (irrigation + combine)
+ Biomes:                0.02s (Holdridge lookup)
─────────────────────────────────
Phase 1 Total:           2.62s ⚠️ 0.6s OVER
```

**Mitigation**: VS_031 debug panel enables stage-based regeneration (hydrology-only = 0.5s, not 2.62s!)

### Key Outputs

**Plate Tectonics** → [Plate_Tectonics.md](WorldGen/Plate_Tectonics.md#outputs)
- `OriginalHeightmap` [0.1-20 RAW units]
- `PostProcessedHeightmap` [0.1-20 RAW units]
- `OceanMask` (border-connected ocean)
- `ElevationThresholds` (quantile-based)

**Climate System** → [Climate_System.md](WorldGen/Climate_System.md#outputs)
- `FinalTemperatureMap` [0,1]
- `FinalPrecipitationMap` [0,1]

**Hydrology & Rivers** (planned):
- `ErodedHeightmap` [0.1-20 RAW units]
- `DischargeMap` [0,1+] (serves as watermap!)
- `Rivers` List<River>
- `Lakes` List<Lake>
- `HumidityMap` [0,∞)

**Biomes** (planned):
- `BiomeMap` BiomeType[,] (48 types)

### View Modes (Debug Visualization)

**Elevation**:
- OriginalElevation (raw plate tectonics)
- PostProcessedElevation (smoothed)
- OceanMask (binary)

**Climate**:
- Temperature stages (Latitude → +Noise → +Distance → +Cooling)
- Precipitation stages (Noise → +TempCurve → +RainShadow → +Coastal)

**Hydrology** (planned):
- ErodedElevation
- DischargeMap (flow accumulation)
- RiverMap (creek/stream/river classification)
- HumidityMap

**Biomes** (planned):
- BiomeMap (48-type classification)

---

## Standalone Worldgen Game Potential

**Vision**: Extract worldgen system as standalone exploration/discovery game (separate from Darklands tactical RPG)

**Core Gameplay Loop**:
```
Generate World (seed-based, 2-10s)
  ↓
Explore Map (pan/zoom, view modes, probe)
  ↓
Discover Features (volcanoes, ore veins, river sources, biome diversity)
  ↓
Export Data (JSON, PNG heightmap, seed sharing)
  ↓
Generate New World (procedural variety, compare worlds)
```

**Key Features**:
- Multi-mode visualization (15+ view modes: elevation, temperature, precipitation, geology, biomes, resources)
- Interactive probe (click cell → full data: elevation, biome, temperature, humidity, geology, ores, rivers)
- World metrics dashboard (highest peak, longest river, biome diversity, ore richness)
- Seed system (shareable, challenges, leaderboards)
- Export functionality (PNG heightmap, JSON world data, settlement suggestions)

**Target Audience**:
- Worldbuilding enthusiasts (writers, D&D DMs)
- Procedural generation fans (Dwarf Fortress community)
- Game developers (prototype tool for game world design)

**Development Effort**: +2-3 weeks after Phase 3 complete

---

## Strategic Layer Integration

**Vision**: Darklands strategic layer consumes worldgen data to place settlements, factions, economy

**Architecture**:
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

**Key Integrations**:
- **Settlement Placement**: Biome suitability scores (temperate grassland + river + coastal = optimal)
- **Economy Buildings**: Terrain-driven attachments (coastal → harbor, iron deposits → mine, forest → lumber camp)
- **Trade Routes**: River networks enable fast transport (settlements on same river = trade network)

**Timeline**: After Phase 1 complete (basic settlements), Phase 3 complete (rich economy)

---

## Next Steps

**Immediate Priority** (Complete Phase 1 MVP):
1. 🔄 Design **Hydrology & Rivers** (VS_029-031) - Particle erosion, river extraction, humidity, debug panel (~20-28h)
2. ⏳ Design **Biome Classification** - 48-type Holdridge system (~6h)
3. ⏳ Implement VS_029-031
4. ⏳ Implement Biome Classification

**Phase 2** (After Phase 1 playable):
1. Add swamp detection (slope calculation + classification) (~2-3h)
2. Visualize creeks (watermap threshold rendering) (~1-2h)
3. Visual validation of Phase 1 worlds (is thermal erosion needed?)

**Phase 3** (Standalone Worldgen Game):
1. Research platec API for plate boundaries (determine effort: S or M)
2. Implement volcanic system (~6-8h)
3. Implement geology layers + ore veins (~12-16h)
4. Add thermal erosion if slopes unrealistic (~2-3h)
5. Polish UI for standalone release (~2-3 weeks)

---

## Decision Log

### 2025-10-14: Feature-Based Documentation Structure

**Decision**: Reorganize roadmap from stage-based (Stage 0, Stage 1, Stage 2) to feature-based (Pipeline Architecture, Plate Tectonics, Climate System).

**Rationale**:
- Pipeline order is **configurable** (Single-Pass vs Iterative modes reorder stages)
- Feature-based organization is **implementation-agnostic** (mirrors code structure: `Features/` folder)
- Modular docs reduce cognitive load (main roadmap as index, detailed docs as modules)

**Result**: Main roadmap simplified from 1827 lines → ~400 lines, detailed content extracted to:
- [Pipeline_Architecture.md](WorldGen/Pipeline_Architecture.md) (TD_027/TD_028 details)
- [Plate_Tectonics.md](WorldGen/Plate_Tectonics.md) (VS_024 details)
- [Climate_System.md](WorldGen/Climate_System.md) (VS_025-028 details)
- [Geology_And_Resources.md](WorldGen/Geology_And_Resources.md) (Phase 3 architecture)

### 2025-10-14: Post-Process Geology Pattern

**Decision**: Geology (volcanoes, minerals) implemented as **post-process** on top of plate outputs, NOT integrated into plate simulation.

**Rationale**:
1. **Causality**: Tectonic forces CREATE conditions → Geology INTERPRETS conditions
2. **Testability**: Mock plate outputs → test geology independently
3. **Iteration Speed**: Regenerate geology in 0.5s vs regenerate plates in 30s (60× faster!)
4. **Extensibility**: Add new resource types without touching plate code

**See**: [Geology_And_Resources.md](WorldGen/Geology_And_Resources.md#overview) for full architectural justification

### 2025-10-14: Pipeline Builder Justified

**Decision**: Implement PipelineBuilder (fluent API) despite initial assessment ("simple stage reordering doesn't need builder").

**Rationale**: Feedback loops revealed hidden complexity:
- Two fundamentally different orchestrators (SinglePass vs Iterative)
- Preset system for VS_031 debug panel (semantic presets, not technical knobs)
- A/B testing requires systematic comparison (pipeline modes + plate algorithms)

**Result**: Builder pattern now justified by THREE real requirements (plate rewrite, erosion reordering, feedback loops).

**See**: [Pipeline_Architecture.md](WorldGen/Pipeline_Architecture.md#why-builder-pattern-is-justified)

---

**Status**: Phase 1 ~85% complete (Hydrology & Biomes remaining)
**Owner**: Tech Lead (roadmap maintenance), Dev Engineer (implementation)

---

*This roadmap provides high-level overview. See feature modules in `WorldGen/` folder for detailed technical documentation.*
