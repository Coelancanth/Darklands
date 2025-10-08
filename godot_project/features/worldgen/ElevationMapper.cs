using Godot;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Maps raw elevation values [0.1-20] from plate tectonics library to real-world meters.
/// Pure presentation utility - NOT used by algorithms (they use raw values + thresholds).
/// </summary>
/// <remarks>
/// VS_024: Separation of concerns -
/// - **Algorithms** use raw elevation [0.1-20] with quantile thresholds (simple, WorldEngine-compatible)
/// - **Display/UI** use meters mapping for human-readable output ("4,200m above sea level")
///
/// Mapping approach:
/// - Ocean: [0.1, seaLevel] → [-11,000m, 0m] (Mariana Trench to sea level)
/// - Land: [seaLevel, 20.0] → [0m, 8,849m] (sea level to Mt. Everest)
///
/// This creates realistic elevations for player-facing UI without complicating algorithm logic.
/// </remarks>
public static class ElevationMapper
{
    // Real-world reference points (meters)
    private const float OCEAN_FLOOR_METERS = -11000f;  // Mariana Trench depth
    private const float SEA_LEVEL_METERS = 0f;
    private const float EVEREST_METERS = 8849f;        // Mt. Everest height

    /// <summary>
    /// Converts raw elevation to meters above/below sea level.
    /// Uses piecewise linear mapping: ocean and land have different scales.
    /// </summary>
    /// <param name="rawElevation">Raw elevation from heightmap</param>
    /// <param name="seaLevelThreshold">Sea level threshold from world's Thresholds</param>
    /// <param name="minElevation">Actual minimum elevation in heightmap (ocean floor)</param>
    /// <param name="maxElevation">Actual maximum elevation in heightmap (highest peak)</param>
    /// <returns>Elevation in meters (negative = below sea level, positive = above)</returns>
    public static float ToMeters(
        float rawElevation,
        float seaLevelThreshold,
        float minElevation,
        float maxElevation)
    {
        if (rawElevation <= seaLevelThreshold)
        {
            // Ocean depth: Linear map [actual_min, seaLevel] → [-11000m, 0m]
            float t = (rawElevation - minElevation) / (seaLevelThreshold - minElevation);
            return Mathf.Lerp(OCEAN_FLOOR_METERS, SEA_LEVEL_METERS, t);
        }
        else
        {
            // Land elevation: Linear map [seaLevel, actual_max] → [0m, 8849m]
            float t = (rawElevation - seaLevelThreshold) / (maxElevation - seaLevelThreshold);
            return Mathf.Lerp(SEA_LEVEL_METERS, EVEREST_METERS, t);
        }
    }

    /// <summary>
    /// Formats elevation as human-readable string for display.
    /// Examples: "4,200m above sea level", "350m below sea level"
    /// </summary>
    /// <param name="rawElevation">Raw elevation from heightmap</param>
    /// <param name="seaLevelThreshold">Sea level threshold from world's Thresholds</param>
    /// <param name="minElevation">Actual minimum elevation in heightmap</param>
    /// <param name="maxElevation">Actual maximum elevation in heightmap</param>
    /// <returns>Formatted elevation string</returns>
    public static string FormatElevation(
        float rawElevation,
        float seaLevelThreshold,
        float minElevation,
        float maxElevation)
    {
        float meters = ToMeters(rawElevation, seaLevelThreshold, minElevation, maxElevation);

        if (meters >= 0)
        {
            // Above sea level (land)
            return $"{meters:N0}m above sea level";
        }
        else
        {
            // Below sea level (ocean)
            return $"{-meters:N0}m below sea level";
        }
    }

    /// <summary>
    /// Formats elevation with terrain type hint based on thresholds.
    /// Examples: "1,200m (Hills)", "5,800m (Mountains)", "200m (Lowlands)"
    /// </summary>
    /// <param name="rawElevation">Raw elevation from heightmap</param>
    /// <param name="seaLevelThreshold">Sea level threshold</param>
    /// <param name="minElevation">Actual minimum elevation in heightmap</param>
    /// <param name="maxElevation">Actual maximum elevation in heightmap</param>
    /// <param name="hillThreshold">Hill threshold (optional)</param>
    /// <param name="mountainThreshold">Mountain threshold (optional)</param>
    /// <param name="peakThreshold">Peak threshold (optional)</param>
    /// <returns>Formatted elevation with terrain type</returns>
    public static string FormatElevationWithTerrain(
        float rawElevation,
        float seaLevelThreshold,
        float minElevation,
        float maxElevation,
        float? hillThreshold = null,
        float? mountainThreshold = null,
        float? peakThreshold = null)
    {
        float meters = ToMeters(rawElevation, seaLevelThreshold, minElevation, maxElevation);

        // Determine terrain type from thresholds
        string terrainType;
        if (rawElevation <= seaLevelThreshold)
        {
            terrainType = "Ocean";
        }
        else if (peakThreshold.HasValue && rawElevation >= peakThreshold.Value)
        {
            terrainType = "Peaks";
        }
        else if (mountainThreshold.HasValue && rawElevation >= mountainThreshold.Value)
        {
            terrainType = "Mountains";
        }
        else if (hillThreshold.HasValue && rawElevation >= hillThreshold.Value)
        {
            terrainType = "Hills";
        }
        else
        {
            terrainType = "Lowlands";
        }

        if (meters >= 0)
        {
            return $"{meters:N0}m ({terrainType})";
        }
        else
        {
            return $"{-meters:N0}m below sea level ({terrainType})";
        }
    }
}
