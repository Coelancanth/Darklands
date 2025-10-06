namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Immutable parameters for plate tectonics simulation.
/// Controls world generation characteristics.
/// </summary>
public record PlateSimulationParams
{
    /// <summary>Random seed for reproducible world generation</summary>
    public int Seed { get; init; }

    /// <summary>World map width (and height - square worlds only)</summary>
    public int WorldSize { get; init; } = 512;

    /// <summary>Number of tectonic plates (more = smaller continents)</summary>
    public int PlateCount { get; init; } = 10;

    /// <summary>
    /// Sea level threshold (0.0-1.0).
    /// Lower = more land, higher = more ocean.
    /// </summary>
    public float SeaLevel { get; init; } = 0.65f;

    /// <summary>
    /// Erosion cycles (higher = smoother terrain).
    /// Typical range: 60-100.
    /// </summary>
    public int ErosionPeriod { get; init; } = 60;

    /// <summary>
    /// Mountain folding ratio (higher = taller mountains).
    /// Typical range: 0.02-0.05.
    /// </summary>
    public float FoldingRatio { get; init; } = 0.02f;

    /// <summary>
    /// Absolute aggregation overlap for plate collisions.
    /// Affects mountain building mechanics.
    /// </summary>
    public int AggrOverlapAbs { get; init; } = 1_000_000;

    /// <summary>
    /// Relative aggregation overlap (0.0-1.0).
    /// Affects plate boundary behavior.
    /// </summary>
    public float AggrOverlapRel { get; init; } = 0.33f;

    /// <summary>
    /// Number of simulation cycles (more = more complex terrain).
    /// Typical range: 1-3. Higher values = longer generation time.
    /// </summary>
    public int CycleCount { get; init; } = 2;

    /// <summary>
    /// Creates parameters with default values (good starting point).
    /// </summary>
    public PlateSimulationParams(int seed)
    {
        Seed = seed;
    }

    /// <summary>
    /// Creates parameters with custom configuration.
    /// </summary>
    public PlateSimulationParams(
        int seed,
        int worldSize = 512,
        int plateCount = 10,
        float seaLevel = 0.65f,
        int erosionPeriod = 60,
        float foldingRatio = 0.02f,
        int aggrOverlapAbs = 1_000_000,
        float aggrOverlapRel = 0.33f,
        int cycleCount = 2)
    {
        Seed = seed;
        WorldSize = worldSize;
        PlateCount = plateCount;
        SeaLevel = seaLevel;
        ErosionPeriod = erosionPeriod;
        FoldingRatio = foldingRatio;
        AggrOverlapAbs = aggrOverlapAbs;
        AggrOverlapRel = aggrOverlapRel;
        CycleCount = cycleCount;
    }
}
