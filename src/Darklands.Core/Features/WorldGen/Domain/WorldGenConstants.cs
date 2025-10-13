namespace Darklands.Core.Features.WorldGen.Domain;

/// <summary>
/// Physics constants for world generation.
/// These are FIXED values derived from plate tectonics physics, not configuration parameters.
/// </summary>
/// <remarks>
/// TD_021: Sea Level SSOT System
///
/// Design Decision: Sea level is a physics constant, not a user-configurable parameter.
/// Similar to how gravity is fixed, the elevation threshold where ocean becomes land is
/// determined by the plate tectonics simulation's CONTINENTAL_BASE constant (1.0f).
///
/// Why 1.0f?
/// - Matches C++ plate-tectonics library's CONTINENTAL_BASE constant
/// - Raw elevation scale [0.1-20]: Ocean ≤ 1.0f, Land > 1.0f
/// - Used consistently across: native simulation, ocean mask generation, rendering
///
/// Contrast with ElevationThresholds (hills/mountains):
/// - HillLevel/MountainLevel/PeakLevel are ADAPTIVE per-world (quantile-based)
/// - Mountainous worlds have different thresholds than flat worlds
/// - SeaLevel is FIXED across all worlds (physics constant)
/// </remarks>
public static class WorldGenConstants
{
    /// <summary>
    /// Sea level elevation in raw scale from plate tectonics simulation.
    /// This is the CONTINENTAL_BASE constant from the C++ library (1.0f).
    /// Cells with elevation ≤ SEA_LEVEL_RAW are ocean, > SEA_LEVEL_RAW are land.
    /// </summary>
    /// <remarks>
    /// This constant is the SINGLE SOURCE OF TRUTH for sea level across:
    /// - Native plate simulation (continental generation)
    /// - Ocean mask generation (BFS flood fill threshold)
    /// - Climate algorithms (rain shadow, coastal moisture)
    /// - Rendering (water vs land classification)
    ///
    /// DO NOT create additional sea level definitions elsewhere!
    /// </remarks>
    public const float SEA_LEVEL_RAW = 1.0f;
}
