using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Provides probe data for Plates view mode.
/// Shows tectonic plate ID and raw elevation.
/// </summary>
public class PlatesProbeProvider : IProbeDataProvider
{
    public string Name => "Plates";

    public string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null)
    {
        float originalElevation = data.Heightmap[y, x];
        uint plateId = data.PlatesMap[y, x];

        return $"Cell ({x},{y})\nPlate ID: {plateId}\nRaw: {originalElevation:F3}";
    }
}
