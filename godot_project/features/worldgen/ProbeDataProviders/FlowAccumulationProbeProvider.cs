using System.Collections.Generic;
using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for Flow Accumulation view (VS_029 Step 3).
/// Shows: accumulation value, percentile rank, drainage area estimate.
/// </summary>
public class FlowAccumulationProbeProvider : IProbeDataProvider
{
    public string Name => "Flow Accumulation";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        var probeText = $"Cell ({x},{y})\nFlow Accumulation\n\n";

        var erosionData = data.Phase1Erosion;
        if (erosionData == null)
        {
            return probeText + "(No erosion data - regenerate world)";
        }

        // Get accumulation
        float accumulation = erosionData.FlowAccumulation[y, x];
        bool? isOcean = data.OceanMask?[y, x];

        // Show value
        probeText += $"Accumulation: {accumulation:F3}\n";

        // Ocean cells show as "no flow"
        if (isOcean == true)
        {
            probeText += "Type: Ocean\n";
            probeText += "(Terminal sink - no flow data)\n";
            return probeText;
        }

        // Calculate percentile rank among land cells
        var landAccumulations = new List<float>();
        for (int j = 0; j < data.Height; j++)
        {
            for (int i = 0; i < data.Width; i++)
            {
                if (data.OceanMask?[j, i] == false)
                {
                    landAccumulations.Add(erosionData.FlowAccumulation[j, i]);
                }
            }
        }

        if (landAccumulations.Count > 0)
        {
            landAccumulations.Sort();
            int rank = landAccumulations.BinarySearch(accumulation);
            if (rank < 0) rank = ~rank; // Handle insertion point
            float percentile = (rank * 100f) / landAccumulations.Count;

            probeText += $"Percentile: {percentile:F1}%\n";

            // Classification
            if (percentile > 95f)
                probeText += "Class: Major River\n";
            else if (percentile > 80f)
                probeText += "Class: River\n";
            else if (percentile > 50f)
                probeText += "Class: Stream\n";
            else
                probeText += "Class: Low flow\n";
        }

        return probeText;
    }
}
