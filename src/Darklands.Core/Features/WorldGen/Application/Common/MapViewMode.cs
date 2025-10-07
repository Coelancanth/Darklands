namespace Darklands.Core.Features.WorldGen.Application.Common;

/// <summary>
/// View modes for world map visualization.
/// Each mode renders a different data layer from the generated world.
/// </summary>
public enum MapViewMode
{
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
    Temperature

    ,

    /// <summary>
    /// Display the raw heightmap directly from the native plate-tectonics simulation
    /// before any post-processing (borders/noise/ocean harmonization/erosion).
    /// Useful to diagnose plate library output.
    /// </summary>
    RawElevation,

    /// <summary>
    /// Display the tectonic plates ownership map (plate id per cell) produced by the
    /// native simulation.
    /// </summary>
    Plates

    ,

    /// <summary>
    /// Raw elevation colored with the same WorldEngine gradient as Elevation view.
    /// Uses the processed ocean mask for coastal coloring.
    /// </summary>
    RawElevationColored
}
