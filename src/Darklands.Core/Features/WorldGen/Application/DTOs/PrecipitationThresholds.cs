namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Quantile-based precipitation thresholds calculated from world precipitation distribution.
/// Matches WorldEngine's adaptive approach - thresholds adjust per-world (hot vs cold climates).
/// Values are normalized [0,1] precipitation levels (UI displays as mm/year ranges).
/// </summary>
/// <remarks>
/// VS_026: Adaptive classification for precipitation zones.
///
/// Example for a wet world (tropical-dominant):
/// - LowThreshold: 0.28 (30th percentile - arid regions)
/// - MediumThreshold: 0.65 (70th percentile - moderate rainfall)
/// - HighThreshold: 0.92 (95th percentile - rainforests)
///
/// Example for a dry world (desert-dominant):
/// - LowThreshold: 0.15
/// - MediumThreshold: 0.42
/// - HighThreshold: 0.78
///
/// This ensures classification adapts to each world's climate distribution!
/// Quantiles prevent all worlds from looking identical in visualization.
/// </remarks>
public record PrecipitationThresholds
{
    /// <summary>
    /// Low precipitation threshold (30th percentile).
    /// Cells below this are arid/desert zones.
    /// Typical display: &lt;400mm/year.
    /// </summary>
    public float LowThreshold { get; init; }

    /// <summary>
    /// Medium precipitation threshold (70th percentile).
    /// Cells between Low and Medium have moderate rainfall.
    /// Typical display: 400-800mm/year.
    /// </summary>
    public float MediumThreshold { get; init; }

    /// <summary>
    /// High precipitation threshold (95th percentile).
    /// Cells above Medium are wet tropical zones.
    /// Typical display: &gt;800mm/year.
    /// </summary>
    public float HighThreshold { get; init; }

    public PrecipitationThresholds(
        float lowThreshold,
        float mediumThreshold,
        float highThreshold)
    {
        LowThreshold = lowThreshold;
        MediumThreshold = mediumThreshold;
        HighThreshold = highThreshold;
    }
}
