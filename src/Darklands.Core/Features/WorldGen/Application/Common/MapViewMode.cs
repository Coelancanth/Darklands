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
    ColoredPostProcessedElevation,

    /// <summary>
    /// Display temperature - Stage 1: Latitude-only (VS_025 debug).
    /// Pure latitude banding with axial tilt. Normalized [0,1] → [-60°C, +40°C].
    /// Visual signature: Horizontal bands, hot zone shifts with per-world tilt.
    /// </summary>
    TemperatureLatitudeOnly,

    /// <summary>
    /// Display temperature - Stage 2: + Noise (VS_025 debug).
    /// Latitude (92%) + climate noise (8%). Normalized [0,1] → [-60°C, +40°C].
    /// Visual signature: Subtle fuzz on latitude bands (not dramatic).
    /// </summary>
    TemperatureWithNoise,

    /// <summary>
    /// Display temperature - Stage 3: + Distance to sun (VS_025 debug).
    /// Latitude + noise / distance². Normalized [0,1] → [-60°C, +40°C].
    /// Visual signature: Hot/cold planet variation (per-world multiplier).
    /// </summary>
    TemperatureWithDistance,

    /// <summary>
    /// Display temperature - Stage 4: FINAL (VS_025 production).
    /// Complete algorithm with mountain cooling. Normalized [0,1] → [-60°C, +40°C].
    /// Visual signature: Mountains blue at ALL latitudes (even equator).
    /// </summary>
    TemperatureFinal
}
