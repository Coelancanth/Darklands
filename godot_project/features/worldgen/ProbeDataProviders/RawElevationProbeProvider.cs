using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for RawElevation view mode.
/// Shows simple grayscale elevation value without terrain classification.
/// </summary>
public class RawElevationProbeProvider : IProbeDataProvider
{
    public string Name => "Raw Elevation";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        float originalElevation = data.Heightmap[y, x];
        return $"Cell ({x},{y})\nRaw: {originalElevation:F3}";
    }
}
