using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for Erosion Hotspots view (VS_029 - repurposed old algorithm).
/// Shows: erosion potential (elevation × accumulation), classification.
/// </summary>
public class ErosionHotspotsProbeProvider : IProbeDataProvider
{
    public string Name => "Erosion Hotspots";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        var probeText = $"Cell ({x},{y})\nErosion Hotspots\n\n";

        var erosionData = data.Phase1Erosion;
        if (erosionData == null)
        {
            return probeText + "(No erosion data - regenerate world)";
        }

        // Get data
        float elevation = erosionData.FilledHeightmap[y, x];
        float accumulation = erosionData.FlowAccumulation[y, x];
        bool? isOcean = data.OceanMask?[y, x];

        // Calculate erosion potential (simple product)
        float erosionPotential = elevation * accumulation;

        // Show values
        probeText += $"Elevation: {elevation:F2}\n";
        probeText += $"Accumulation: {accumulation:F3}\n";
        probeText += $"Erosion Potential: {erosionPotential:F3}\n";

        // Ocean status
        if (isOcean == true)
        {
            probeText += "\nType: Ocean (no erosion)\n";
            return probeText;
        }

        probeText += "\n";

        // Classify erosion potential
        var thresholds = data.Thresholds;
        bool isHighElevation = thresholds != null && elevation >= thresholds.MountainLevel;
        bool isHighFlow = accumulation > 0.01f; // Arbitrary threshold for display

        if (isHighElevation && isHighFlow)
        {
            probeText += "EROSION HOTSPOT\n";
            probeText += "(High mountains + major river)\n";
            probeText += "Type: Canyon/gorge formation\n";
        }
        else if (isHighElevation)
        {
            probeText += "High elevation\n";
            probeText += "(But low flow - minimal erosion)\n";
        }
        else if (isHighFlow)
        {
            probeText += "High flow\n";
            probeText += "(But low elevation - deposition zone)\n";
        }
        else
        {
            probeText += "Low erosion potential\n";
        }

        return probeText;
    }
}
