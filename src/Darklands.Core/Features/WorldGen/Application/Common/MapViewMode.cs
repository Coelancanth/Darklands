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
}
