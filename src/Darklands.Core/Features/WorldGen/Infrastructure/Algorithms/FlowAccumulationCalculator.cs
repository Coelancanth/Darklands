using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Computes flow accumulation (drainage basin sizes) using topological ordering.
/// Part of VS_029 Phase 1 (Step 1d): Models catchment areas for realistic river density.
/// </summary>
/// <remarks>
/// Algorithm:
/// 1. Initialize: flowAccum[cell] = precipitation[cell] (local contribution)
/// 2. Process cells in topological order (headwaters → ocean):
///    - For each cell, add its accumulated flow to downstream neighbor
///    - flowAccum[downstream] += flowAccum[current]
/// 3. Result: Each cell contains sum of ALL upstream precipitation
///
/// Why this is CRITICAL:
/// - Flow accumulation models drainage basins (hydrologically correct!)
/// - Rivers spawn where LARGE catchment areas exist (not uniform grid!)
/// - Prevents "river every 5 cells" artifact from greedy-only approaches
///
/// Complexity: O(n) - single pass thanks to topological ordering
///
/// Example:
///   Cell A (precip=1.0, no upstream) → flowAccum = 1.0
///   Cell B (precip=1.0, upstream=A) → flowAccum = 1.0 + 1.0 = 2.0
///   Cell C (precip=1.0, upstream=B) → flowAccum = 1.0 + 2.0 = 3.0
///
/// Drainage basin for C = 3.0 (collects from A + B + C)
/// </remarks>
public static class FlowAccumulationCalculator
{
    /// <summary>
    /// Computes flow accumulation map using topological ordering.
    /// </summary>
    /// <param name="precipitation">Precipitation map (normalized [0,1] from VS_028)</param>
    /// <param name="flowDirections">Flow direction map (0-7 direction, -1 sink)</param>
    /// <param name="topologicalOrder">Cells in dependency order (upstream → downstream)</param>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <returns>Flow accumulation map (drainage basin sizes, same units as precipitation)</returns>
    public static float[,] Calculate(
        float[,] precipitation,
        int[,] flowDirections,
        List<(int x, int y)> topologicalOrder,
        int width,
        int height,
        ILogger? logger = null)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Initialize flow accumulation with local precipitation
        // ═══════════════════════════════════════════════════════════════════════

        var flowAccumulation = new float[height, width];

        // Collect precipitation statistics for diagnostics
        var precipValues = new List<float>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float precip = precipitation[y, x];
                flowAccumulation[y, x] = precip;
                precipValues.Add(precip);
            }
        }

        // Log precipitation input statistics
        logger?.LogInformation("[FlowAccum] STEP 1: Initialized with precipitation values");
        logger?.LogInformation("[FlowAccum]   Precip stats: min={Min:F3}, max={Max:F3}, mean={Mean:F3}, nonzero={NonZero}/{Total}",
            precipValues.Min(), precipValues.Max(), precipValues.Average(), precipValues.Count(v => v > 0.001f), precipValues.Count);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Accumulate flow in topological order (headwaters → ocean)
        // ═══════════════════════════════════════════════════════════════════════
        // Key insight: Topological order GUARANTEES all upstream cells processed
        // BEFORE downstream cells, so accumulation is correct in single pass!

        logger?.LogInformation("[FlowAccum] STEP 2: Processing {Count} cells in topological order", topologicalOrder.Count);

        int sinksSkipped = 0;
        int outOfBounds = 0;
        int flowPropagated = 0;

        foreach (var (x, y) in topologicalOrder)
        {
            // Get this cell's accumulated flow
            float currentFlow = flowAccumulation[y, x];

            // Find downstream neighbor
            int dir = flowDirections[y, x];

            // Skip sinks (ocean, pits) - they don't contribute downstream
            if (dir == -1)
            {
                sinksSkipped++;
                continue;
            }

            var (dx, dy) = FlowDirectionCalculator.GetDirectionOffset(dir);
            int nx = x + dx;
            int ny = y + dy;

            // Check bounds
            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
            {
                outOfBounds++;
                continue;
            }

            // Add current cell's flow to downstream neighbor
            flowAccumulation[ny, nx] += currentFlow;
            flowPropagated++;
        }

        logger?.LogInformation("[FlowAccum] STEP 2 Complete: Propagated={Propagated}, Sinks={Sinks}, OutOfBounds={OutOfBounds}",
            flowPropagated, sinksSkipped, outOfBounds);

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: Flow accumulation map (drainage basin sizes)
        // ═══════════════════════════════════════════════════════════════════════

        // Collect final accumulation statistics
        var accumValues = new List<float>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                accumValues.Add(flowAccumulation[y, x]);
            }
        }

        logger?.LogInformation("[FlowAccum] RESULT: Final accumulation statistics");
        logger?.LogInformation("[FlowAccum]   Accum stats: min={Min:F3}, max={Max:F3}, mean={Mean:F3}, p95={P95:F3}",
            accumValues.Min(), accumValues.Max(), accumValues.Average(), accumValues.OrderBy(v => v).Skip((int)(accumValues.Count * 0.95)).First());
        logger?.LogInformation("[FlowAccum]   High accum cells (>5.0): {HighCount}/{Total} ({Percent:F1}%)",
            accumValues.Count(v => v > 5.0f), accumValues.Count, (accumValues.Count(v => v > 5.0f) * 100.0f / accumValues.Count));

        return flowAccumulation;
    }
}
