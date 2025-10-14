using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for River Sources view (VS_029 Step 4).
/// Shows: whether cell is a river source, elevation, accumulation threshold.
/// </summary>
public class RiverSourcesProbeProvider : IProbeDataProvider
{
    public string Name => "River Sources";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        var probeText = $"Cell ({x},{y})\nRiver Sources\n\n";

        var erosionData = data.Phase1Erosion;
        if (erosionData == null)
        {
            return probeText + "(No erosion data - regenerate world)";
        }

        // Get data
        float elevation = erosionData.FilledHeightmap[y, x];
        float accumulation = erosionData.FlowAccumulation[y, x];
        bool isSource = erosionData.RiverSources.Contains((x, y));
        bool? isOcean = data.OceanMask?[y, x];

        // Show elevation
        probeText += $"Elevation: {elevation:F2}\n";
        probeText += $"Accumulation: {accumulation:F3}\n";

        // Ocean status
        if (isOcean == true)
            probeText += "Type: Ocean\n\n";
        else
            probeText += "Type: Land\n\n";

        // Show source status
        if (isSource)
        {
            probeText += "RIVER SOURCE\n";
            probeText += "(Major river origin)\n";
        }
        else
        {
            probeText += "Not a river source\n";

            // Explain why not (elevation or accumulation)
            var thresholds = data.Thresholds;
            if (thresholds != null && elevation < thresholds.MountainLevel)
            {
                probeText += "(Elevation too low)\n";
            }
            else
            {
                probeText += "(Accumulation below threshold)\n";
            }
        }

        // Show total source count
        int totalSources = erosionData.RiverSources.Count;
        probeText += $"\nTotal River Sources: {totalSources}\n";

        return probeText;
    }
}
