using System.Collections.Generic;
using Darklands.Core.Features.WorldGen.Domain;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Pipeline stage DTOs for visual debugging and validation.
/// Each stage represents a discrete step in world generation matching WorldEngine's 10-step architecture.
/// </summary>

/// <summary>
/// Stage 1: Raw output from plate tectonics simulation (no post-processing).
/// </summary>
public record Stage1_RawHeightmap(
    float[,] Heightmap,     // Raw elevation from native library
    uint[,] PlateIds);      // Plate ownership map

/// <summary>
/// Stage 2: Elevation post-processing (borders lowered, noise added, ocean flood-filled).
/// This is the CRITICAL stage to inspect - if elevation distribution looks wrong here,
/// the bug is in ElevationPostProcessor algorithms.
/// </summary>
public record Stage2_ProcessedElevation(
    float[,] Heightmap,     // Post-processed elevation (borders, noise, harmonization)
    bool[,] OceanMask);     // Ocean vs land classification

/// <summary>
/// Stage 3: Temperature calculation (latitude + elevation cooling + noise variation).
/// Depends on Stage 2 elevation data.
/// </summary>
public record Stage3_Temperature(
    float[,] TemperatureMap); // Temperature values [0.0, 1.0]

/// <summary>
/// Stage 4: Precipitation calculation (latitude bands + gamma curve + orographic lift + rain shadow).
/// Depends on Stage 2 elevation + Stage 3 temperature.
/// </summary>
public record Stage4_Precipitation(
    float[,] PrecipitationMap); // Precipitation values [0.0, 1.0]

/// <summary>
/// Stage 5: Hydraulic erosion (rivers traced from mountains to ocean, valleys carved).
/// MUTATES: Heightmap (valley carving around rivers).
/// </summary>
public record Stage5_Erosion(
    float[,] ErodedHeightmap,        // Heightmap with carved river valleys
    List<River> Rivers,               // River paths from source to ocean/lake
    List<(int x, int y)> Lakes);     // Lake positions (endorheic basins)

/// <summary>
/// Stage 6: Watermap simulation (20k droplet flow accumulation model).
/// Shows creek/river/main river thresholds based on flow quantity.
/// </summary>
public record Stage6_Watermap(
    float[,] WatermapData,           // Flow accumulation per cell
    WatermapThresholds Thresholds);  // Creek/River/MainRiver percentile thresholds

/// <summary>
/// Stage 7: Irrigation simulation (moisture spreading from ocean via logarithmic kernel).
/// </summary>
public record Stage7_Irrigation(
    float[,] IrrigationMap);         // Moisture availability from nearby water

/// <summary>
/// Stage 8: Humidity simulation (precipitation + irrigation with 1:3 weight).
/// THIS is what biome classification should use (not raw precipitation!).
/// </summary>
public record Stage8_Humidity(
    float[,] HumidityMap,            // Combined moisture (precip × 1.0 + irrigation × 3.0) / 4.0
    HumidityQuantiles Quantiles);    // Quantile thresholds for moisture classification

/// <summary>
/// Stage 9: Biome classification (Holdridge life zones using temperature + humidity).
/// Final stage before packaging result.
/// </summary>
public record Stage9_Biomes(
    BiomeType[,] BiomeMap);          // Classified biomes per cell

/// <summary>
/// Stage 10: Final packaged result (all stages combined into PlateSimulationResult).
/// Not a separate DTO - this IS PlateSimulationResult itself.
/// </summary>
