namespace Darklands.Core.Features.WorldGen.Application.Rendering;

/// <summary>
/// Converts elevation data to colors using WorldEngine's proven gradient algorithm.
/// Port of worldengine/draw.py::_elevation_color() (lines 151-199).
/// Provides smooth visual transitions: ocean depths (blue) → land (green/yellow/orange) → mountains (white/pink).
/// </summary>
public static class ElevationMapColorizer
{
    /// <summary>
    /// Calculates RGB color for elevation value using WorldEngine's gradient.
    /// Ocean and land use different normalization for smooth coastal transitions.
    /// </summary>
    /// <param name="elevation">Raw elevation value (0.0-1.0 normalized from heightmap)</param>
    /// <param name="seaLevel">Sea level threshold (typically 0.0-1.0, default 1.0 matches WorldEngine)</param>
    /// <returns>RGB color tuple (0.0-1.0 range)</returns>
    public static (float R, float G, float B) GetElevationColor(float elevation, float seaLevel = 1.0f)
    {
        const float ColorStep = 1.5f; // WorldEngine's magic number for land elevation zones

        // Zone 1: Deep ocean (< seaLevel/2) - Dark blue → Blue
        if (elevation < seaLevel / 2f)
        {
            var normalizedDepth = elevation / seaLevel; // 0.0 → 0.5
            return (0.0f, 0.0f, 0.75f + 0.5f * normalizedDepth); // (0,0,0.75) → (0,0,1.0)
        }

        // Zone 2: Shallow ocean (< seaLevel) - Blue → Cyan
        if (elevation < seaLevel)
        {
            var normalizedDepth = elevation / seaLevel; // 0.5 → 1.0
            return (0.0f, 2f * (normalizedDepth - 0.5f), 1.0f); // (0,0,1) → (0,1,1)
        }

        // Land elevations: Normalize relative to sea level
        var landElevation = elevation - seaLevel;

        // Zone 3: Low land (0 → 1.5 step) - Dark green → Yellow-green
        if (landElevation < 1.0f * ColorStep)
        {
            var t = landElevation / ColorStep; // 0.0 → 1.0
            return (0.0f, 0.5f + 0.5f * t, 0.0f); // (0,0.5,0) → (0,1,0)
        }

        // Zone 4a: Mid-low elevation (1.5 → 2.0 step) - Yellow-green → Yellow
        if (landElevation < 1.5f * ColorStep)
        {
            var t = (landElevation - 1.0f * ColorStep) / ColorStep; // 0.0 → 0.5
            return (2f * t, 1.0f, 0.0f); // (0,1,0) → (1,1,0)
        }

        // Zone 4b: Mid elevation (2.0 → 2.5 step) - Yellow → Orange
        if (landElevation < 2.0f * ColorStep)
        {
            var t = (landElevation - 1.5f * ColorStep) / ColorStep; // 0.0 → 0.5
            return (1.0f, 1.0f - t, 0.0f); // (1,1,0) → (1,0,0)
        }

        // Zone 5: Mid-high elevation (2.5 → 4.5 step) - Orange → Brown
        if (landElevation < 3.0f * ColorStep)
        {
            var t = (landElevation - 2.0f * ColorStep) / ColorStep; // 0.0 → 1.0
            var r = 1.0f - 0.5f * t; // 1.0 → 0.5
            var g = 0.5f - 0.25f * t; // 0.5 → 0.25
            return (r, g, 0.0f);
        }

        // Zone 6: High elevation (4.5 → 7.5 step) - Brown → Gray → White
        if (landElevation < 5.0f * ColorStep)
        {
            var t = (landElevation - 3.0f * ColorStep) / (2f * ColorStep); // 0.0 → 1.0
            var r = 0.5f - 0.125f * t; // 0.5 → 0.375
            var g = 0.25f + 0.125f * t; // 0.25 → 0.375
            var b = 0.375f * t; // 0.0 → 0.375
            return (r, g, b);
        }

        // Zone 7: Very high elevation (7.5 → 12 step) - Gray → White
        if (landElevation < 8.0f * ColorStep)
        {
            var t = (landElevation - 5.0f * ColorStep) / (3f * ColorStep); // 0.0 → 1.0
            var gray = 0.375f + 0.625f * t; // 0.375 → 1.0 (smooth gray to white)
            return (gray, gray, gray);
        }

        // Zone 8: Extreme peaks (> 12 step) - White → Pink (magenta highlight)
        // Cycling pattern for very tall mountains (visual interest)
        var peakElevation = landElevation - 8.0f * ColorStep;
        while (peakElevation > 2.0f * ColorStep)
        {
            peakElevation -= 2.0f * ColorStep; // Cycle every 2 steps
        }

        var pinkIntensity = 1.0f - peakElevation / 4.0f; // 1.0 → 0.5
        return (1.0f, pinkIntensity, 1.0f); // (1,1,1) → (1,0.5,1)
    }
}
