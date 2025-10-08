using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Detects river sources (mountain cells with large accumulated flow).
/// Part of VS_029 Phase 1 (Step 1e): Identifies starting points for river tracing.
/// </summary>
/// <remarks>
/// Algorithm:
/// For each cell, check two criteria:
/// 1. High elevation: heightmap[cell] >= MountainLevel
/// 2. Large catchment: flowAccumulation[cell] >= accumulationThreshold
///
/// Why both criteria?
/// - Elevation: Rivers spawn in mountains (gravity-driven flow)
/// - Flow accumulation: Only cells with LARGE drainage basins become sources
///   (prevents uniform "river every 5 cells" grid)
///
/// Tunable parameter: accumulationThreshold
/// - Higher (e.g., 0.8) = Fewer, larger rivers (realistic!)
/// - Lower (e.g., 0.2) = Many, smaller rivers (unrealistic grid)
/// - Default: 0.5 (balanced - 5-15 major rivers per 512×512 map)
///
/// Result: 5-15 river sources for a 512×512 map (vs 100+ with greedy-only approach!)
/// </remarks>
public static class RiverSourceDetector
{
    /// <summary>
    /// Detects river sources based on elevation and flow accumulation thresholds.
    /// </summary>
    /// <param name="filledHeightmap">Heightmap after pit filling (raw [0-20] scale)</param>
    /// <param name="flowAccumulation">Flow accumulation map (drainage basin sizes)</param>
    /// <param name="mountainLevel">Mountain elevation threshold (from quantile-based thresholds)</param>
    /// <param name="accumulationThreshold">Minimum accumulated flow to spawn river (default 0.5)</param>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <returns>List of river source locations (x, y)</returns>
    public static List<(int x, int y)> Detect(
        float[,] filledHeightmap,
        float[,] flowAccumulation,
        float mountainLevel,
        float accumulationThreshold,
        int width,
        int height)
    {
        var sources = new List<(int x, int y)>();

        // ═══════════════════════════════════════════════════════════════════════
        // SCAN: Find cells meeting BOTH elevation AND flow criteria
        // ═══════════════════════════════════════════════════════════════════════

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Criterion 1: High elevation (mountain)
                bool isHighElevation = filledHeightmap[y, x] >= mountainLevel;

                // Criterion 2: Large accumulated flow (large drainage basin)
                bool isLargeFlow = flowAccumulation[y, x] >= accumulationThreshold;

                // Both criteria must be met
                if (isHighElevation && isLargeFlow)
                {
                    sources.Add((x, y));
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: River sources (starting points for Phase 2 tracing)
        // ═══════════════════════════════════════════════════════════════════════

        return sources;
    }
}
