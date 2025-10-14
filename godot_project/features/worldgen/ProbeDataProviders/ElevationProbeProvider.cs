using System.Linq;
using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Domain;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for ColoredOriginalElevation and ColoredPostProcessedElevation view modes.
/// Shows comprehensive elevation data with real-world meters mapping, water body classification,
/// basin metadata, and optional color debugging.
///
/// VS_024: Uses ElevationMapper for human-readable display, shows raw values for debugging.
/// Updated: Distinguishes Ocean vs Inner Sea vs Lake based on preserved basins.
/// </summary>
public class ElevationProbeProvider : IProbeDataProvider
{
    public string Name => "Elevation";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        // Get elevation values
        float originalElevation = data.Heightmap[y, x];
        float? postProcessedElevation = data.PostProcessedHeightmap?[y, x];
        float currentElevation = postProcessedElevation ?? originalElevation;

        // Get ocean/basin data
        bool? isOcean = data.OceanMask?[y, x];
        float? seaDepth = data.SeaDepth?[y, x];
        var thresholds = data.Thresholds;

        var probeText = $"Cell ({x},{y})\n";

        // DEBUG: Get actual rendered color from texture for color bug diagnosis
        string colorDebug = "";
        if (debugTexture != null)
        {
            var image = debugTexture.GetImage();
            if (image != null && x >= 0 && x < image.GetWidth() && y >= 0 && y < image.GetHeight())
            {
                var renderedColor = image.GetPixel(x, y);
                int r = (int)(renderedColor.R * 255);
                int g = (int)(renderedColor.G * 255);
                int b = (int)(renderedColor.B * 255);
                colorDebug = $"Color: RGB({r}, {g}, {b}) #{r:X2}{g:X2}{b:X2}\n";
            }
        }

        // Check if this cell belongs to a preserved basin (inner sea or lake)
        var erosionData = data.Phase1Erosion;
        var containingBasin = erosionData?.PreservedBasins.FirstOrDefault(b => b.Cells.Contains((x, y)));

        // Determine water body type (Ocean, Inner Sea, Lake, or Land)
        string waterBodyType = "";
        if (containingBasin != null)
        {
            // Part of preserved basin - inner sea or lake
            const int INNER_SEA_THRESHOLD = 1000;  // Matches ColoredElevation rendering
            waterBodyType = containingBasin.Area >= INNER_SEA_THRESHOLD
                ? "Inner Sea (landlocked)\n"
                : "Lake (landlocked)\n";
        }
        else if (isOcean == true)
        {
            waterBodyType = "Ocean (border-connected)\n";
        }

        // Show rendered color FIRST (critical debug data for color bug diagnosis)
        if (!string.IsNullOrEmpty(colorDebug))
        {
            probeText += colorDebug;
        }

        // Show water body type if this is water
        if (!string.IsNullOrEmpty(waterBodyType))
        {
            probeText += waterBodyType;
        }

        // Show human-readable meters ONLY for non-basin cells
        // (ElevationMapper returns "Ocean" for ALL cells below sea level, including inner seas!)
        if (containingBasin == null)
        {
            // Not a basin cell - show elevation mapping
            if (thresholds != null)
            {
                string metersDisplay = ElevationMapper.FormatElevationWithTerrain(
                    rawElevation: currentElevation,
                    seaLevelThreshold: WorldGenConstants.SEA_LEVEL_RAW,  // TD_021: Use SSOT constant
                    minElevation: data.MinElevation,     // ← FIX: Use actual min from heightmap
                    maxElevation: data.MaxElevation,     // ← FIX: Use actual max from heightmap
                    hillThreshold: thresholds.HillLevel,
                    mountainThreshold: thresholds.MountainLevel,
                    peakThreshold: thresholds.PeakLevel);
                probeText += metersDisplay;
                probeText += $"\n\nRaw: {originalElevation:F2}";
            }
            else
            {
                // Fallback when data unavailable (cached world or old format)
                probeText += $"Elevation: {originalElevation:F2}";
                probeText += $"\n(Regenerate world for meters)";
            }
        }
        else
        {
            // Basin cell - skip elevation mapping (already shown water body type)
            probeText += $"\nRaw: {originalElevation:F2}";
        }

        // Show post-processed comparison if available
        if (postProcessedElevation.HasValue)
            probeText += $"\nPost-Proc: {postProcessedElevation.Value:F2}";

        // Show basin details if this is an inner sea or lake
        if (containingBasin != null)
        {
            probeText += $"\n\nBasin #{containingBasin.BasinId}";
            probeText += $"\nSize: {containingBasin.Area} cells";
            probeText += $"\nDepth: {containingBasin.Depth:F1}";
        }

        return probeText;
    }
}
