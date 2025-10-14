namespace Darklands.Core.Features.WorldGen.Application.DTOs;

using System.Numerics;

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
    /// Per-plate kinematics data (velocities and mass centers). Optional based on pipeline stage.
    /// </summary>
    public TectonicKinematicsData[]? Kinematics { get; init; }

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
        uint[,] platesMap,
        TectonicKinematicsData[]? kinematics = null)
    {
        Heightmap = heightmap;
        PlatesMap = platesMap;
        Kinematics = kinematics;
    }
}

/// <summary>
/// Per-plate kinematics data (velocity and centroid) for geology layer.
/// </summary>
public record TectonicKinematicsData(
    uint PlateId,
    Vector2 VelocityUnitVector,
    float VelocityMagnitude,
    Vector2 MassCenter);
