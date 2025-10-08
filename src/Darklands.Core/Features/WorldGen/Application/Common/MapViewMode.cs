namespace Darklands.Core.Features.WorldGen.Application.Common;

/// <summary>
/// View modes for world generation visualization.
/// Includes raw native output, post-processed elevations, and debug views.
/// </summary>
public enum MapViewMode
{
    /// <summary>
    /// Display raw heightmap as grayscale gradient.
    /// Shows unnormalized elevation values from native plate tectonics simulation (no post-processing).
    /// </summary>
    RawElevation,

    /// <summary>
    /// Display tectonic plates ownership map.
    /// Each plate ID rendered with a unique color.
    /// </summary>
    Plates,

    /// <summary>
    /// Display ORIGINAL elevation with quantile-based terrain color gradient (VS_024).
    /// Shows raw native output [0-20] BEFORE post-processing.
    /// Colors: deep blue (ocean) → green (lowlands) → yellow (hills) → brown (peaks).
    /// </summary>
    ColoredOriginalElevation,

    /// <summary>
    /// Display POST-PROCESSED elevation with quantile-based terrain color gradient (VS_024).
    /// Shows raw [0.1-20] AFTER 4 WorldEngine algorithms (add_noise, fill_ocean, harmonize_ocean, sea_depth).
    /// Should differ from ColoredOriginalElevation (noise added, ocean smoothed).
    /// </summary>
    ColoredPostProcessedElevation
}
