using System.Linq;
using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for Sinks (POST-Filling) view (VS_029 Step 0B).
/// Shows: filled elevation, whether this cell is still a sink, reduction from pre-filling.
/// </summary>
public class SinksPostFillingProbeProvider : IProbeDataProvider
{
    public string Name => "Sinks Post-Filling";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        var probeText = $"Cell ({x},{y})\nPOST-Filling Sinks\n\n";

        var erosionData = data.Phase1Erosion;
        if (erosionData == null)
        {
            return probeText + "(No erosion data - regenerate world)";
        }

        // Get filled elevation
        float filledElevation = erosionData.FilledHeightmap[y, x];
        bool? isOcean = data.OceanMask?[y, x];

        // Check if this cell is a post-filling sink (flow direction = -1)
        int flowDir = erosionData.FlowDirections[y, x];
        bool isSink = flowDir == -1;

        // Show elevation
        probeText += $"Elevation: {filledElevation:F2}\n";

        // Ocean status
        if (isOcean == true)
            probeText += "Type: Ocean\n";
        else
            probeText += "Type: Land\n";

        probeText += "\n";

        // Show sink status
        if (isSink)
        {
            // TD_023: Check if it's a preserved lake (check if this cell is a basin center)
            bool isLake = erosionData.PreservedBasins.Any(basin => basin.Center == (x, y));
            if (isLake)
                probeText += "PRESERVED LAKE\n(Large endorheic basin)\n";
            else
                probeText += "LOCAL MINIMUM\n(Remaining sink)\n";
        }
        else
            probeText += "Not a sink\n(Drains to lower cell)\n";

        // Show reduction statistics
        int preSinks = data.PreFillingLocalMinima?.Count ?? 0;
        int postSinks = 0;

        // Count post-filling sinks
        for (int j = 0; j < data.Height; j++)
            for (int i = 0; i < data.Width; i++)
                if (erosionData.FlowDirections[j, i] == -1) postSinks++;

        probeText += $"\nPit-Filling Results:\n";
        probeText += $"Before: {preSinks} sinks\n";
        probeText += $"After: {postSinks} sinks\n";

        if (preSinks > 0)
        {
            float reduction = ((preSinks - postSinks) * 100f) / preSinks;
            probeText += $"Reduction: {reduction:F1}%\n";
        }

        return probeText;
    }
}
