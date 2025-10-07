namespace Darklands.Core.Features.WorldGen.Application.Common;

/// <summary>
/// View modes for raw native simulation visualization.
/// Shows heightmap and plate ownership data directly from the native library.
/// </summary>
public enum MapViewMode
{
    /// <summary>
    /// Display raw heightmap as grayscale gradient.
    /// Shows unnormalized elevation values from native plate tectonics simulation.
    /// </summary>
    RawElevation,

    /// <summary>
    /// Display tectonic plates ownership map.
    /// Each plate ID rendered with a unique color.
    /// </summary>
    Plates,

    /// <summary>
    /// Display elevation with terrain-like color gradient.
    /// Colors range from deep blue (low) through green to white (high),
    /// similar to WorldEngine visualization style.
    /// </summary>
    ColoredElevation
}
