namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Quantile-based elevation thresholds calculated from world heightmap distribution.
/// Matches WorldEngine's adaptive approach - thresholds adjust per-world (flat vs mountainous).
/// Values are in raw elevation scale [0.1-20] from plate tectonics library.
/// </summary>
/// <remarks>
/// VS_024: Replaces fixed normalization with adaptive thresholds.
/// TD_021: Sea level removed - use WorldGenConstants.SEA_LEVEL_RAW (1.0f physics constant).
///
/// Example for a mountainous world:
/// - HillLevel: 3.2 (70th percentile of land)
/// - MountainLevel: 5.8 (85th percentile of land)
/// - PeakLevel: 12.5 (95th percentile of land)
///
/// Example for a flat world:
/// - HillLevel: 1.5
/// - MountainLevel: 2.3
/// - PeakLevel: 4.1
///
/// This ensures temperature/precipitation algorithms adapt to each world's terrain!
/// Sea level (1.0f) is FIXED across all worlds (physics constant from C++ library).
/// </remarks>
public record ElevationThresholds
{

    /// <summary>
    /// Hill threshold (70th percentile of land cells only).
    /// Marks transition from flat lowlands to rolling hills.
    /// </summary>
    public float HillLevel { get; init; }

    /// <summary>
    /// Mountain threshold (85th percentile of land cells only).
    /// Used by temperature algorithm for elevation cooling effect.
    /// Typical value: ~3.0-6.0 depending on world terrain.
    /// </summary>
    public float MountainLevel { get; init; }

    /// <summary>
    /// Peak threshold (95th percentile of land cells only).
    /// Highest elevations - alpine zones, snow caps.
    /// </summary>
    public float PeakLevel { get; init; }

    public ElevationThresholds(
        float hillLevel,
        float mountainLevel,
        float peakLevel)
    {
        HillLevel = hillLevel;
        MountainLevel = mountainLevel;
        PeakLevel = peakLevel;
    }
}
