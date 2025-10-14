using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Domain;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for all 4 temperature view modes.
/// Shows progression through algorithm stages: LatitudeOnly → WithNoise → WithDistance → Final.
///
/// VS_025: Shows all 4 stages + per-world parameters for debugging.
/// Multi-mode provider: Determines which stage to highlight based on viewMode parameter.
/// </summary>
public class TemperatureProbeProvider : IProbeDataProvider
{
    public string Name => "Temperature";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        var probeText = $"Cell ({x},{y})\n";

        // Get all 4 temperature values at this cell
        float? latitudeOnly = data.TemperatureLatitudeOnly?[y, x];
        float? withNoise = data.TemperatureWithNoise?[y, x];
        float? withDistance = data.TemperatureWithDistance?[y, x];
        float? final = data.TemperatureFinal?[y, x];

        // Per-world parameters
        float? axialTilt = data.AxialTilt;
        float? distanceToSun = data.DistanceToSun;

        // Determine debug stage from view mode
        int debugStage = viewMode switch
        {
            MapViewMode.TemperatureLatitudeOnly => 1,
            MapViewMode.TemperatureWithNoise => 2,
            MapViewMode.TemperatureWithDistance => 3,
            MapViewMode.TemperatureFinal => 4,
            _ => 4 // Default to final
        };

        // Show current stage prominently with °C conversion
        probeText += debugStage switch
        {
            1 => $"Stage 1: Latitude Only\n{TemperatureMapper.FormatTemperature(latitudeOnly ?? 0f)}\n",
            2 => $"Stage 2: + Noise\n{TemperatureMapper.FormatTemperature(withNoise ?? 0f)}\n",
            3 => $"Stage 3: + Distance\n{TemperatureMapper.FormatTemperature(withDistance ?? 0f)}\n",
            4 => $"Stage 4: Final\n{TemperatureMapper.FormatTemperature(final ?? 0f)}\n",
            _ => "Unknown Stage\n"
        };

        probeText += "\n--- Debug: All Stages ---\n";

        // Show all 4 stages for comparison (normalized [0,1] values)
        if (latitudeOnly.HasValue)
            probeText += $"1. Latitude: {latitudeOnly.Value:F3}\n";

        if (withNoise.HasValue)
            probeText += $"2. + Noise: {withNoise.Value:F3}\n";

        if (withDistance.HasValue)
            probeText += $"3. + Distance: {withDistance.Value:F3}\n";

        if (final.HasValue)
            probeText += $"4. Final: {final.Value:F3}\n";

        // Show per-world parameters (explain hot/cold planets, tilt shifts)
        probeText += "\n--- World Parameters ---\n";

        if (axialTilt.HasValue)
            probeText += $"Axial Tilt: {axialTilt.Value:F3}\n";

        if (distanceToSun.HasValue)
            probeText += $"Distance to Sun: {distanceToSun.Value:F3}×\n";

        return probeText;
    }
}
