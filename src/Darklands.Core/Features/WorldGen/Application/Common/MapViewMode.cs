namespace Darklands.Core.Features.WorldGen.Application.Common;

/// <summary>
/// View modes for world map visualization.
/// Each mode renders a different data layer from the generated world.
/// Organized by: Final Views | Debug Views | Pipeline Stages
/// </summary>
public enum MapViewMode
{
    // ═══════════════════════════════════════════════════════════════════════
    // Final Output Views (what users see in game)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Display biome classification (default view).
    /// Uses Ricklefs or Holdridge color schemes based on toggle.
    /// </summary>
    Biomes,

    /// <summary>
    /// Display elevation data as colored gradient.
    /// Ocean depths (blue) → Mountains (white/pink).
    /// </summary>
    Elevation,

    /// <summary>
    /// Display precipitation/humidity data as cyan gradient.
    /// Arid (dark teal) → Humid (bright cyan).
    /// </summary>
    Precipitation,

    /// <summary>
    /// Display temperature data as thermal gradient.
    /// Polar (blue) → Tropical (red).
    /// </summary>
    Temperature,

    // ═══════════════════════════════════════════════════════════════════════
    // Debug Views (diagnostic tools)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Display the raw heightmap directly from the native plate-tectonics simulation
    /// before any post-processing (borders/noise/ocean harmonization/erosion).
    /// Useful to diagnose plate library output.
    /// </summary>
    RawElevation,

    /// <summary>
    /// Raw elevation colored with the same WorldEngine gradient as Elevation view.
    /// Uses the processed ocean mask for coastal coloring.
    /// </summary>
    RawElevationColored,

    /// <summary>
    /// Display the tectonic plates ownership map (plate id per cell) produced by the
    /// native simulation.
    /// </summary>
    Plates,

    // ═══════════════════════════════════════════════════════════════════════
    // Pipeline Stage Views (visual validation for debugging)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Stage 2: Processed Elevation (CRITICAL - suspect for elevation distribution bug).
    /// Shows heightmap AFTER borders lowered, noise added, ocean flood-filled, but BEFORE climate/erosion.
    /// Compare to RawElevation to see impact of ElevationPostProcessor algorithms.
    /// </summary>
    Stage2_ProcessedElevation,

    /// <summary>
    /// Stage 3: Temperature calculation output.
    /// Shows temperature map AFTER latitude/elevation/noise but BEFORE precipitation.
    /// </summary>
    Stage3_Temperature,

    /// <summary>
    /// Stage 4: Precipitation calculation output.
    /// Shows precipitation AFTER gamma curve/orographic lift but BEFORE erosion.
    /// </summary>
    Stage4_Precipitation,

    /// <summary>
    /// Stage 5: Eroded Elevation (after river valley carving).
    /// Shows heightmap AFTER hydraulic erosion carved valleys around rivers.
    /// Compare to Stage2 to see erosion impact.
    /// </summary>
    Stage5_ErodedElevation,

    /// <summary>
    /// Stage 6: Watermap (flow accumulation from 20k droplet simulation).
    /// Shows creek/river/main river flow patterns.
    /// </summary>
    Stage6_Watermap,

    /// <summary>
    /// Stage 7: Irrigation (moisture spreading from ocean).
    /// Shows how moisture spreads inland from coasts via logarithmic kernel.
    /// </summary>
    Stage7_Irrigation,

    /// <summary>
    /// Stage 8: Humidity (precip + irrigation weighted 1:3).
    /// THIS is what biome classification uses (not raw precipitation!).
    /// Compare to Stage4_Precipitation to see irrigation impact.
    /// </summary>
    Stage8_Humidity
}
