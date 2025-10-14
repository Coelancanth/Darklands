using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for Sinks (PRE-Filling) view (VS_029 Step 0A).
/// Shows: raw elevation, whether this cell is a local minimum (sink), total pre-filling sink count.
/// </summary>
public class SinksPreFillingProbeProvider : IProbeDataProvider
{
    public string Name => "Sinks Pre-Filling";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        var probeText = $"Cell ({x},{y})\nPRE-Filling Sinks\n\n";

        // Get elevation (use post-processed before pit-filling)
        float? elevation = data.PostProcessedHeightmap?[y, x];
        bool? isOcean = data.OceanMask?[y, x];

        // Check if this cell is a pre-filling sink
        bool isSink = data.PreFillingLocalMinima?.Contains((x, y)) ?? false;

        // Show elevation
        if (elevation.HasValue)
            probeText += $"Elevation: {elevation.Value:F2}\n";

        // Ocean status
        if (isOcean == true)
            probeText += "Type: Ocean\n";
        else
            probeText += "Type: Land\n";

        probeText += "\n";

        // Show sink status
        if (isSink)
            probeText += "LOCAL MINIMUM\n(Sink before pit-filling)\n";
        else
            probeText += "Not a sink\n";

        // Show total count
        int totalSinks = data.PreFillingLocalMinima?.Count ?? 0;
        probeText += $"\nTotal Pre-Filling Sinks: {totalSinks}\n";

        // Calculate percentage of land cells
        if (totalSinks > 0 && data.OceanMask != null)
        {
            int landCells = 0;
            for (int j = 0; j < data.Height; j++)
                for (int i = 0; i < data.Width; i++)
                    if (!data.OceanMask[j, i]) landCells++;

            float percentage = landCells > 0 ? (totalSinks * 100f / landCells) : 0f;
            probeText += $"({percentage:F1}% of land cells)\n";
        }

        return probeText;
    }
}
