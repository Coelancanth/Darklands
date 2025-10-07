namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Raw output from native plate tectonics simulation.
/// Contains unnormalized heightmap and plate ownership data.
/// </summary>
public record PlateSimulationResult
{
    /// <summary>
    /// Raw heightmap from native library (unnormalized, typically 0-20 range).
    /// </summary>
    public float[,] Heightmap { get; init; }

    /// <summary>
    /// Plate ownership map (plate ID per cell).
    /// </summary>
    public uint[,] PlatesMap { get; init; }

    /// <summary>
    /// Map width (all arrays have same dimensions).
    /// </summary>
    public int Width => Heightmap.GetLength(1);

    /// <summary>
    /// Map height (all arrays have same dimensions).
    /// </summary>
    public int Height => Heightmap.GetLength(0);

    public PlateSimulationResult(
        float[,] heightmap,
        uint[,] platesMap)
    {
        Heightmap = heightmap;
        PlatesMap = platesMap;
    }
}
