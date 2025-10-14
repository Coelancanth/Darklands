using System.Linq;
using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for Basin Metadata view (TD_023, PreservedLakes mode).
/// Shows: basin ID, elevation, basin size/depth, pour point distance, basin role (center/boundary/outlet).
/// </summary>
public class BasinMetadataProbeProvider : IProbeDataProvider
{
    public string Name => "Basin Metadata";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        var probeText = $"Cell ({x},{y})\nPreserved Lakes (TD_023)\n\n";

        var erosionData = data.Phase1Erosion;
        if (erosionData == null)
        {
            return probeText + "(No erosion data - regenerate world)";
        }

        // Get filled elevation
        float filledElevation = erosionData.FilledHeightmap[y, x];
        bool? isOcean = data.OceanMask?[y, x];

        probeText += $"Elevation: {filledElevation:F2}\n";

        // Check if this cell belongs to any preserved basin (check first for type display)
        var containingBasin = erosionData.PreservedBasins.FirstOrDefault(b => b.Cells.Contains((x, y)));

        // Type display: Inner Sea/Lake takes precedence over Ocean
        if (containingBasin != null)
        {
            // Part of preserved lake - show as water body (more meaningful than "land")
            probeText += containingBasin.Area > 1000
                ? "Type: Inner Sea (endorheic basin)\n\n"
                : "Type: Lake (landlocked)\n\n";
        }
        else if (isOcean == true)
        {
            probeText += "Type: Ocean (border-connected)\n\n";
        }
        else
        {
            probeText += "Type: Land\n\n";
        }

        if (containingBasin != null)
        {
            // Cell is part of a preserved basin - show details
            probeText += $"LAKE #{containingBasin.BasinId}\n";
            probeText += $"Size: {containingBasin.Area} cells\n";
            probeText += $"Depth: {containingBasin.Depth:F1}\n";
            probeText += $"Surface Elev: {containingBasin.SurfaceElevation:F2}\n\n";

            // Determine role in basin
            if (containingBasin.Center == (x, y))
            {
                probeText += "Role: BASIN CENTER\n";
                probeText += "(Local minimum - pit bottom)\n";
            }
            else if (containingBasin.PourPoint == (x, y))
            {
                probeText += "Role: POUR POINT\n";
                probeText += "(Outlet - where water exits)\n";
            }
            else
            {
                probeText += "Role: Basin boundary\n";
                probeText += "(Part of endorheic basin)\n";
            }
        }
        else
        {
            // Cell is not part of any preserved basin
            probeText += "Not part of preserved basin\n";

            int totalBasins = erosionData.PreservedBasins.Count;
            probeText += $"\nTotal Preserved Basins: {totalBasins}\n";

            if (totalBasins == 0)
            {
                probeText += "(All pits filled - no large basins)\n";
            }
        }

        return probeText;
    }
}
