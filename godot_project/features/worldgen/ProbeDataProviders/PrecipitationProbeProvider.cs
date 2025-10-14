using System;
using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for base precipitation view modes (stages 1-3).
/// Shows progression: NoiseOnly → TemperatureShaped → Final + physics debug.
///
/// VS_026: Shows all 3 stages + gamma curve calculation for debugging.
/// Multi-mode provider: Determines which stage to highlight based on viewMode parameter.
/// </summary>
public class PrecipitationProbeProvider : IProbeDataProvider
{
    public string Name => "Precipitation";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        var probeText = $"Cell ({x},{y})\n";

        // Get all 3 precipitation values at this cell
        float? noiseOnly = data.BaseNoisePrecipitationMap?[y, x];
        float? tempShaped = data.TemperatureShapedPrecipitationMap?[y, x];
        float? final = data.FinalPrecipitationMap?[y, x];

        // Get temperature at this cell for gamma curve calculation
        float? temperature = data.TemperatureFinal?[y, x];

        // Get quantile thresholds for classification
        var thresholds = data.PrecipitationThresholds;

        // Determine debug stage from view mode
        int debugStage = viewMode switch
        {
            MapViewMode.PrecipitationNoiseOnly => 1,
            MapViewMode.PrecipitationTemperatureShaped => 2,
            MapViewMode.PrecipitationBase => 3,
            _ => 3 // Default to base
        };

        // Show current stage prominently
        probeText += debugStage switch
        {
            1 => $"Stage 1: Base Noise\n{noiseOnly ?? 0f:F3}\n",
            2 => $"Stage 2: + Temp Curve\n{tempShaped ?? 0f:F3}\n",
            3 => $"Stage 3: Final\n{FormatPrecipitation(final ?? 0f, thresholds)}\n",
            _ => "Unknown Stage\n"
        };

        probeText += "\n--- Debug: All Stages ---\n";

        // Show all 3 stages for comparison (normalized [0,1] values)
        if (noiseOnly.HasValue)
            probeText += $"1. Noise: {noiseOnly.Value:F3}\n";

        if (tempShaped.HasValue)
            probeText += $"2. Temp Shaped: {tempShaped.Value:F3}\n";

        if (final.HasValue)
            probeText += $"3. Final: {final.Value:F3}\n";

        // Show physics debug info (gamma curve calculation)
        if (temperature.HasValue && noiseOnly.HasValue)
        {
            probeText += "\n--- Physics Debug ---\n";
            probeText += $"Temperature: {temperature.Value:F3}\n";

            // Calculate gamma curve value (same formula as PrecipitationCalculator)
            const float gamma = 2.0f;
            const float curveBonus = 0.2f;
            float curve = MathF.Pow(temperature.Value, gamma) * (1.0f - curveBonus) + curveBonus;

            probeText += $"Gamma Curve: {curve:F3}\n";
            probeText += $"(cold=0.2, hot=1.0)\n";
        }

        // Show classification based on thresholds
        if (final.HasValue && thresholds != null)
        {
            string classification;
            if (final.Value < thresholds.LowThreshold)
                classification = "Arid";
            else if (final.Value < thresholds.MediumThreshold)
                classification = "Low";
            else if (final.Value < thresholds.HighThreshold)
                classification = "Medium";
            else
                classification = "High";

            probeText += $"\nClassification: {classification}\n";
        }

        return probeText;
    }

    /// <summary>
    /// Formats precipitation value with classification label and mm/year estimate.
    /// </summary>
    private static string FormatPrecipitation(float precipNormalized, PrecipitationThresholds? thresholds)
    {
        // Classification based on quantile thresholds
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
