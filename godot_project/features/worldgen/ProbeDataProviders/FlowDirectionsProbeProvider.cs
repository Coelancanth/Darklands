using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for Flow Directions view (VS_029 Step 2).
/// Shows: flow direction code, compass direction, elevation, whether cell drains.
/// </summary>
public class FlowDirectionsProbeProvider : IProbeDataProvider
{
    public string Name => "Flow Directions";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        var probeText = $"Cell ({x},{y})\nFlow Directions\n\n";

        var erosionData = data.Phase1Erosion;
        if (erosionData == null)
        {
            return probeText + "(No erosion data - regenerate world)";
        }

        // Get flow direction
        int flowDir = erosionData.FlowDirections[y, x];
        float elevation = erosionData.FilledHeightmap[y, x];
        bool? isOcean = data.OceanMask?[y, x];

        // Show elevation
        probeText += $"Elevation: {elevation:F2}\n";

        // Ocean status
        if (isOcean == true)
            probeText += "Type: Ocean (sink)\n\n";
        else
            probeText += "Type: Land\n\n";

        // Show flow direction
        if (flowDir == -1)
        {
            probeText += "Direction: SINK\n";
            probeText += "(Local minimum - no flow)\n";
        }
        else
        {
            string[] dirNames = { "N ↑", "NE ↗", "E →", "SE ↘", "S ↓", "SW ↙", "W ←", "NW ↖" };
            probeText += $"Direction: {dirNames[flowDir]}\n";
            probeText += $"Code: {flowDir}\n";

            // Calculate neighbor position
            int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
            int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };

            int nx = x + dx[flowDir];
            int ny = y + dy[flowDir];

            // Show downstream elevation if in bounds
            if (nx >= 0 && nx < data.Width && ny >= 0 && ny < data.Height)
            {
                float downstreamElev = erosionData.FilledHeightmap[ny, nx];
                float drop = elevation - downstreamElev;
                probeText += $"\nDownstream: {downstreamElev:F2}\n";
                probeText += $"Drop: {drop:F2}\n";
            }
        }

        return probeText;
    }
}
