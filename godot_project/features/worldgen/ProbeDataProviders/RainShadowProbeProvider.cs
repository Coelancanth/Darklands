using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for PrecipitationWithRainShadow view mode (VS_027 Stage 4).
/// Shows: base precipitation, rain shadow reduction, latitude-based wind direction.
/// </summary>
public class RainShadowProbeProvider : IProbeDataProvider
{
    public string Name => "Rain Shadow";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        float? basePrecip = data.FinalPrecipitationMap?[y, x];
        float? rainShadowPrecip = data.WithRainShadowPrecipitationMap?[y, x];
        var thresholds = data.PrecipitationThresholds;

        // Calculate latitude for wind direction
        float normalizedLatitude = data.Height > 1 ? (float)y / (data.Height - 1) : 0.5f;
        var (windX, windY) = Core.Features.WorldGen.Infrastructure.Algorithms.PrevailingWinds.GetWindDirection(normalizedLatitude);

        // Get wind band name
        string windBand = Core.Features.WorldGen.Infrastructure.Algorithms.PrevailingWinds.GetWindBandName(normalizedLatitude);
        string windDirection = windX < 0 ? "← Westward" : "→ Eastward";

        // Calculate reduction percentage
        float reductionPercent = 0f;
        if (basePrecip.HasValue && rainShadowPrecip.HasValue && basePrecip.Value > 0)
        {
            reductionPercent = ((basePrecip.Value - rainShadowPrecip.Value) / basePrecip.Value) * 100f;
        }

        string header = $"Cell ({x},{y})\nStage 4: + Rain Shadow\n\n";
        string wind = $"Wind: {windDirection} ({windBand})\n\n";
        string precip = $"Base:\n{FormatPrecipitation(basePrecip ?? 0f, thresholds)}\n\n";
        string shadow = $"Rain Shadow:\n{FormatPrecipitation(rainShadowPrecip ?? 0f, thresholds)}\n\n";
        string reduction = reductionPercent > 0.1f
            ? $"Blocking: -{reductionPercent:F1}% (leeward)\n"
            : $"Blocking: None (windward/flat)\n";

        return header + wind + precip + shadow + reduction;
    }

    /// <summary>
    /// Formats precipitation value with classification label and mm/year estimate.
    /// Duplicated from PrecipitationProbeProvider to keep providers self-contained.
    /// </summary>
    private static string FormatPrecipitation(float precipNormalized, PrecipitationThresholds? thresholds)
    {
        string classification;
        string mmPerYear;

        if (thresholds == null)
        {
            return $"{precipNormalized:F3} (no thresholds)";
        }

        if (precipNormalized < thresholds.LowThreshold)
        {
            classification = "Arid";
            mmPerYear = "<200mm/year";
        }
        else if (precipNormalized < thresholds.MediumThreshold)
        {
            classification = "Low";
            mmPerYear = "200-400mm/year";
        }
        else if (precipNormalized < thresholds.HighThreshold)
        {
            classification = "Medium";
            mmPerYear = "400-800mm/year";
        }
        else
        {
            classification = "High";
            mmPerYear = ">800mm/year";
        }

        return $"{precipNormalized:F3}\n{classification}\n{mmPerYear}";
    }
}
