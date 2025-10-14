using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for PrecipitationFinal view mode (VS_028 Stage 5).
/// Shows: rain shadow input, final precipitation, distance-to-ocean, coastal bonus %, elevation resistance.
/// </summary>
public class CoastalMoistureProbeProvider : IProbeDataProvider
{
    public string Name => "Coastal Moisture";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        float? rainShadowPrecip = data.WithRainShadowPrecipitationMap?[y, x];
        float? finalPrecip = data.PrecipitationFinal?[y, x];
        var thresholds = data.PrecipitationThresholds;
        bool? isOcean = data.OceanMask?[y, x];
        float? elevation = data.PostProcessedHeightmap?[y, x];

        string header = $"Cell ({x},{y})\nStage 5: FINAL (+ Coastal)\n\n";

        // Ocean cells have no coastal enhancement
        if (isOcean == true)
        {
            string oceanNote = "Ocean Cell:\n(No coastal enhancement)\n\n";
            string precip = $"Precipitation:\n{FormatPrecipitation(finalPrecip ?? 0f, thresholds)}\n";
            return header + oceanNote + precip;
        }

        // Calculate distance-to-ocean (estimate based on BFS - we don't store it in WorldGenerationResult)
        // For probe display, show relative enhancement instead
        float enhancement = 0f;
        if (rainShadowPrecip.HasValue && finalPrecip.HasValue && rainShadowPrecip.Value > 0)
        {
            enhancement = ((finalPrecip.Value - rainShadowPrecip.Value) / rainShadowPrecip.Value) * 100f;
        }

        // Build probe display
        string rainShadow = $"Rain Shadow:\n{FormatPrecipitation(rainShadowPrecip ?? 0f, thresholds)}\n\n";
        string final = $"Final (+ Coastal):\n{FormatPrecipitation(finalPrecip ?? 0f, thresholds)}\n\n";
        string bonus = enhancement > 0.1f
            ? $"Coastal Bonus: +{enhancement:F1}%\n(Maritime climate effect)\n"
            : $"Coastal Bonus: None\n(Deep interior)\n";

        // Show elevation if high (resistance effect)
        string elevInfo = "";
        if (elevation.HasValue && elevation.Value > 5.0f)
        {
            elevInfo = $"\nElevation: {elevation.Value:F1}\n(High altitude resists coastal moisture)\n";
        }

        return header + rainShadow + final + bonus + elevInfo;
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
