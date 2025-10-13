using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Topological sort for flow network (upstream→downstream ordering).
/// Part of VS_029 Phase 1 (Step 1c): Critical for correct flow accumulation.
/// </summary>
/// <remarks>
/// Algorithm: Kahn's algorithm (BFS-based topological sort)
///
/// Why this matters:
/// Flow accumulation MUST process cells in dependency order:
/// - Upstream cells (headwaters) BEFORE downstream cells
/// - Ensures all upstream contributions counted before processing downstream
///
/// Fixes WorldEngine bug:
/// - WorldEngine used raster-scan order (row-by-row) → WRONG for branching flow!
/// - Our approach: True topological order via Kahn's algorithm
///
/// Complexity: O(n) where n = cell count
///
/// Example:
///   Headwater → Stream → River → Ocean
///      (0)        (1)     (2)    (sink)
/// Topological order: [Headwater, Stream, River]
/// (Ocean not included - it's a sink with no downstream)
/// </remarks>
public static class TopologicalSortCalculator
{
    /// <summary>
    /// Computes topological ordering of cells (upstream→downstream).
    /// Uses Kahn's algorithm: BFS starting from headwaters (in-degree = 0).
    /// </summary>
    /// <param name="flowDirections">Flow direction map (0-7 direction, -1 sink)</param>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>List of cells in topological order (headwaters first, sinks last)</returns>
    public static List<(int x, int y)> Sort(int[,] flowDirections, int width, int height, ILogger? logger = null)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Compute in-degree for each cell
        // ═══════════════════════════════════════════════════════════════════════
        // in-degree = number of cells flowing INTO this cell

        var inDegree = new int[height, width];
        int totalCells = width * height;
        int sinkCount = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int dir = flowDirections[y, x];

                // Skip sinks (they don't flow anywhere)
                if (dir == -1)
                {
                    sinkCount++;
                    continue;
                }

                // Find downstream neighbor
                var (dx, dy) = FlowDirectionCalculator.GetDirectionOffset(dir);
                int nx = x + dx;
                int ny = y + dy;

                // Increment in-degree of downstream cell
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    inDegree[ny, nx]++;
                }
            }
        }

        logger?.LogInformation("[TopoSort] STEP 1: Computed in-degrees for {Total} cells, found {Sinks} sinks ({Percent:F1}%)",
            totalCells, sinkCount, (sinkCount * 100.0f / totalCells));

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Find all headwaters (in-degree = 0, BUT NOT SINKS!)
        // ═══════════════════════════════════════════════════════════════════════
        // Headwaters have no upstream contributors
        // CRITICAL: Exclude sinks (dir=-1) - they're terminals, not sources!

        var queue = new Queue<(int x, int y)>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // BUG FIX: Only enqueue non-sink cells with in-degree 0
                // Sinks (ocean) also have in-degree 0, but they're terminals, not headwaters!
                if (inDegree[y, x] == 0 && flowDirections[y, x] != -1)
                {
                    queue.Enqueue((x, y));
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 3: Process queue (Kahn's algorithm - BFS)
        // ═══════════════════════════════════════════════════════════════════════

        var topologicalOrder = new List<(int x, int y)>();

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            topologicalOrder.Add((x, y));

            // Find downstream neighbor
            int dir = flowDirections[y, x];

            // Skip sinks (no downstream)
            if (dir == -1)
                continue;

            var (dx, dy) = FlowDirectionCalculator.GetDirectionOffset(dir);
            int nx = x + dx;
            int ny = y + dy;

            // Check bounds
            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                continue;

            // Decrement downstream neighbor's in-degree
            inDegree[ny, nx]--;

            // If in-degree reaches 0, all upstream dependencies satisfied
            if (inDegree[ny, nx] == 0)
            {
                queue.Enqueue((nx, ny));
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: Cells in dependency order (upstream → downstream)
        // ═══════════════════════════════════════════════════════════════════════

        return topologicalOrder;
    }
}
