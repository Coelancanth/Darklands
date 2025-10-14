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
    /// Sea level parameter for native simulation (0.0-1.0 normalized, controls land/ocean ratio).
    /// NOTE (TD_021): This is a GENERATION PARAMETER (how much ocean to create), not the physics threshold!
    /// The actual ocean/land boundary in output is always at WorldGenConstants.SEA_LEVEL_RAW (1.0f).
    /// Lower = more land, higher = more ocean. Default 0.65f = 65% ocean coverage target.
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
    /// Number of feedback loop iterations for iterative pipeline mode (TD_027).
    /// Only used when PipelineMode = Iterative (ignored in SinglePass mode).
    /// Typical range: 3-5. Higher values = better convergence but longer generation time.
    /// Default: 3 iterations (balanced quality vs performance).
    /// </summary>
    /// <remarks>
    /// Feedback Loop: (Erosion → Climate) × FeedbackIterations
    /// - Iteration 1: Initial climate calculation influences erosion
    /// - Iteration 2+: Eroded terrain influences climate (rain shadow accuracy improves)
    /// - Convergence: Iterations 3-5 typically stabilize (diminishing returns after 5)
    ///
    /// Performance Impact:
    /// - 1 iteration: ~2s (equivalent to SinglePass mode)
    /// - 3 iterations: ~6s (default, balanced)
    /// - 5 iterations: ~10s (maximum quality)
    /// </remarks>
    public int FeedbackIterations { get; init; } = 3;

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
        int cycleCount = 2,
        int feedbackIterations = 3)
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
        FeedbackIterations = feedbackIterations;
    }
}
