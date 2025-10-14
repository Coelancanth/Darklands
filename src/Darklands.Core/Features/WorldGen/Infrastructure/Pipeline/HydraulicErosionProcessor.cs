using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline;

/// <summary>
/// Hydraulic erosion simulation - orchestrates VS_029 pipeline phases.
/// Phase 1 (CURRENT): Pit filling + flow accumulation + source detection
/// Phase 2-4 (FUTURE): River tracing, valley erosion, monotonicity cleanup
/// </summary>
/// <remarks>
/// VS_029 Phase 1 Pipeline (O(n log n) - Hydrologically Correct):
/// 1a. Selective pit filling → FilledHeightmap, Lakes
/// 1b. Flow direction computation → FlowDirections
/// 1c. Topological sort → (internal)
/// 1d. Flow accumulation → FlowAccumulation
/// 1e. River source detection → RiverSources
///
/// Output: Phase1ErosionData (foundation for river tracing in Phase 2)
///
/// Key design decisions:
/// - Selective pit filling (not full filling!) - Preserves real lakes
/// - Topological sort (not raster scan!) - Fixes WorldEngine's bug
/// - Flow accumulation models drainage basins (hydrologically correct!)
/// - Tunable thresholds (accumulationThreshold, pitDepth, pitArea)
///
/// Performance: ~100-200ms for 512×512 map (pit filling dominates at O(n log n))
/// </remarks>
public static class HydraulicErosionProcessor
{
    /// <summary>
    /// Default accumulation threshold for river source detection.
    /// Controls river density: Higher = fewer/larger rivers, Lower = many/smaller rivers.
    /// Default 0.5 yields ~5-15 major rivers per 512×512 map (realistic!).
    /// </summary>
    public const float DefaultAccumulationThreshold = 0.5f;

    /// <summary>
    /// Default pit depth threshold for selective filling (meters equivalent).
    /// Pits deeper than this are preserved as lakes.
    /// </summary>
    public const float DefaultPitDepthThreshold = 50.0f;

    /// <summary>
    /// Default pit area threshold for selective filling (cell count).
    /// Pits larger than this are preserved as lakes.
    /// </summary>
    public const int DefaultPitAreaThreshold = 100;

    /// <summary>
    /// Executes Phase 1 of hydraulic erosion simulation.
    /// </summary>
    /// <param name="heightmap">Post-processed heightmap from VS_024 (raw [0-20] scale)</param>
    /// <param name="oceanMask">Ocean mask from VS_024 (true = water, false = land)</param>
    /// <param name="precipitation">FINAL precipitation from VS_028 (normalized [0,1])</param>
    /// <param name="thresholds">Elevation thresholds from VS_024 (for MountainLevel)</param>
    /// <param name="accumulationThreshold">Minimum flow to spawn river (default 0.5)</param>
    /// <param name="pitDepthThreshold">Max pit depth to fill (default 50.0)</param>
    /// <param name="pitAreaThreshold">Max pit area to fill (default 100)</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>Phase 1 erosion data (filled heightmap, flow data, sources, lakes)</returns>
    public static Phase1ErosionData ProcessPhase1(
        float[,] heightmap,
        bool[,] oceanMask,
        float[,] precipitation,
        ElevationThresholds thresholds,
        float accumulationThreshold = DefaultAccumulationThreshold,
        float pitDepthThreshold = DefaultPitDepthThreshold,
        int pitAreaThreshold = DefaultPitAreaThreshold,
        ILogger? logger = null)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1a: Selective Pit Filling (O(n log n) - Priority Flood Fill)
        // ═══════════════════════════════════════════════════════════════════════
        // Fills small pits (artifacts), preserves large pits (real lakes)

        var fillingResult = PitFillingCalculator.Fill(
            heightmap,
            oceanMask,
            pitDepthThreshold,
            pitAreaThreshold,
            logger);

        var filledHeightmap = fillingResult.FilledHeightmap;
        var preservedBasins = fillingResult.PreservedBasins;  // TD_023: Now contains complete basin metadata

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1b: Flow Direction Computation (O(n) - Steepest Descent)
        // ═══════════════════════════════════════════════════════════════════════
        // For each cell, find steepest downhill neighbor (8-connected)

        var flowDirections = FlowDirectionCalculator.Calculate(filledHeightmap, oceanMask);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1c: Topological Sort (O(n) - Kahn's Algorithm)
        // ═══════════════════════════════════════════════════════════════════════
        // Order cells upstream→downstream (critical for correct accumulation!)

        var topologicalOrder = TopologicalSortCalculator.Sort(flowDirections, width, height);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1d: Flow Accumulation (O(n) - Single Pass in Topo Order)
        // ═══════════════════════════════════════════════════════════════════════
        // Compute drainage basin sizes (accumulated precipitation from upstream)

        var flowAccumulation = FlowAccumulationCalculator.Calculate(
            precipitation,
            flowDirections,
            topologicalOrder,
            width,
            height,
            logger);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1e: River Source Detection (O(n) - TWO-STEP: Threshold-Crossing + Filtering)
        // ═══════════════════════════════════════════════════════════════════════
        // NEW ALGORITHM (CORRECTED):
        // Step 1: Find ALL threshold-crossing points (where flow FIRST becomes "a river")
        // Step 2: Filter to select only MAJOR rivers (ranked by downstream importance)

        // Step 1: Detect all potential sources (physically correct threshold-crossing)
        var allPotentialSources = RiverSourceDetector.DetectAllSources(
            flowAccumulation,
            flowDirections,
            oceanMask,
            threshold: null);  // Uses adaptive 15th percentile threshold

        // Step 2: Filter to select major rivers only (artistic control)
        var riverSources = RiverSourceDetector.FilterMajorRivers(
            allPotentialSources,
            flowAccumulation,
            flowDirections,
            oceanMask,
            maxMajorRivers: 15);  // Target 5-15 major rivers

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: Phase 1 Erosion Data (foundation for river tracing)
        // ═══════════════════════════════════════════════════════════════════════

        return new Phase1ErosionData(
            filledHeightmap: filledHeightmap,
            flowDirections: flowDirections,
            flowAccumulation: flowAccumulation,
            riverSources: riverSources,
            preservedBasins: preservedBasins);  // TD_023: Pass basin metadata
    }
}
