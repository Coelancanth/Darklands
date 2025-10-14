using System.Collections.Generic;
using System.Linq;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Detects river sources using the physically correct "threshold-crossing" approach.
/// Identifies points where water flow FIRST accumulates enough to form a defined river channel.
/// Part of VS_029 correction: Separates true river origins from major mountain arteries.
/// </summary>
/// <remarks>
/// ALGORITHM: Threshold-Crossing Detection
///
/// A cell is a TRUE RIVER SOURCE if:
/// 1. Its flow accumulation >= threshold T (enough water to be "a river")
/// 2. ALL upstream neighbors have accumulation &lt; T (this is the FIRST cell to cross threshold)
///
/// This differs from the previous approach which incorrectly used:
/// - High elevation + high accumulation (p95) → Finds major rivers IN mountains, not origins
///
/// KEY INSIGHT (from critique):
/// "A point cannot simultaneously be a 'starting point' and a 'result'."
/// - Starting point = LOW accumulation (just crossed threshold)
/// - Result = HIGH accumulation (already a major river)
///
/// The corrected algorithm finds WHERE rivers BEGIN, not where big rivers flow.
///
/// EXPECTED OUTPUT:
/// - 500-2000 potential sources for 512x512 world (every stream that becomes "a river")
/// - These are then FILTERED to select 5-15 major rivers for visualization/gameplay
/// </remarks>
public static class RiverSourceDetector
{
    /// <summary>
    /// 8-direction offsets for neighbor checking.
    /// </summary>
    private static readonly (int dx, int dy)[] Directions = new[]
    {
        (0, -1),   // North
        (1, -1),   // North-East
        (1, 0),    // East
        (1, 1),    // South-East
        (0, 1),    // South
        (-1, 1),   // South-West
        (-1, 0),   // West
        (-1, -1)   // North-West
    };

    /// <summary>
    /// Detects all potential river sources using threshold-crossing algorithm.
    /// Returns ALL points where flow first exceeds threshold (may be 500-2000 sources).
    /// </summary>
    /// <param name="flowAccumulation">Flow accumulation map (normalized precipitation accumulation)</param>
    /// <param name="flowDirections">Flow direction map (for upstream neighbor detection)</param>
    /// <param name="oceanMask">Ocean mask (true = water, false = land)</param>
    /// <param name="threshold">Minimum accumulation to be considered "a river" (default: adaptive)</param>
    /// <returns>List of all threshold-crossing source coordinates</returns>
    public static List<(int x, int y)> DetectAllSources(
        float[,] flowAccumulation,
        int[,] flowDirections,
        bool[,] oceanMask,
        float? threshold = null)
    {
        int height = flowAccumulation.GetLength(0);
        int width = flowAccumulation.GetLength(1);

        // Calculate adaptive threshold if not provided
        // Use a LOW threshold (10-20th percentile) to catch small streams
        float thresholdT = threshold ?? CalculateAdaptiveThreshold(flowAccumulation, oceanMask);

        var sources = new List<(int x, int y)>();

        // Scan all land cells
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Skip ocean cells
                if (oceanMask[y, x])
                    continue;

                float cellAccumulation = flowAccumulation[y, x];

                // Check if cell has enough accumulation to be "a river"
                if (cellAccumulation < thresholdT)
                    continue;

                // Check if ALL upstream neighbors are below threshold
                // (This is the FIRST cell to cross threshold → TRUE SOURCE)
                if (IsThresholdCrossingPoint(x, y, flowAccumulation, flowDirections, oceanMask, thresholdT, width, height))
                {
                    sources.Add((x, y));
                }
            }
        }

        return sources;
    }

    /// <summary>
    /// Filters all potential sources to select only major rivers.
    /// Uses downstream tracing to rank rivers by importance.
    /// </summary>
    /// <param name="allSources">All threshold-crossing sources from DetectAllSources</param>
    /// <param name="flowAccumulation">Flow accumulation map (for final flow calculation)</param>
    /// <param name="flowDirections">Flow direction map (for downstream tracing)</param>
    /// <param name="oceanMask">Ocean mask</param>
    /// <param name="maxMajorRivers">Maximum number of major rivers to return (default: 15)</param>
    /// <returns>Filtered list of major river sources (typically 5-15)</returns>
    public static List<(int x, int y)> FilterMajorRivers(
        List<(int x, int y)> allSources,
        float[,] flowAccumulation,
        int[,] flowDirections,
        bool[,] oceanMask,
        int maxMajorRivers = 15)
    {
        int height = flowAccumulation.GetLength(0);
        int width = flowAccumulation.GetLength(1);

        // Calculate importance metric for each source
        var rankedSources = allSources
            .Select(source => new
            {
                Coord = source,
                Importance = CalculateRiverImportance(source, flowAccumulation, flowDirections, oceanMask, width, height)
            })
            .OrderByDescending(s => s.Importance)
            .Take(maxMajorRivers)
            .Select(s => s.Coord)
            .ToList();

        return rankedSources;
    }

    /// <summary>
    /// Detects erosion hotspots (major mountain arteries) using the OLD algorithm.
    /// These are high-elevation cells with LARGE accumulated flow.
    /// Useful for VS_030+ particle erosion masking, NOT for river source detection.
    /// </summary>
    /// <param name="filledHeightmap">Heightmap after pit filling</param>
    /// <param name="flowAccumulation">Flow accumulation map</param>
    /// <param name="mountainLevel">Mountain elevation threshold</param>
    /// <param name="accumulationThreshold">High accumulation threshold (p95 or similar)</param>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <returns>List of erosion hotspot locations (for canyon/gorge formation)</returns>
    public static List<(int x, int y)> DetectErosionHotspots(
        float[,] filledHeightmap,
        float[,] flowAccumulation,
        float mountainLevel,
        float accumulationThreshold,
        int width,
        int height)
    {
        var hotspots = new List<(int x, int y)>();

        // SCAN: Find cells meeting BOTH elevation AND flow criteria
        // This identifies where BIG rivers flow through MOUNTAINS (high erosive potential)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Criterion 1: High elevation (mountain)
                bool isHighElevation = filledHeightmap[y, x] >= mountainLevel;

                // Criterion 2: Large accumulated flow (major river already formed)
                bool isLargeFlow = flowAccumulation[y, x] >= accumulationThreshold;

                // Both criteria = erosion hotspot (canyon potential)
                if (isHighElevation && isLargeFlow)
                {
                    hotspots.Add((x, y));
                }
            }
        }

        return hotspots;
    }

    /// <summary>
    /// Checks if a cell is a threshold-crossing point (true river source).
    /// </summary>
    private static bool IsThresholdCrossingPoint(
        int x, int y,
        float[,] flowAccumulation,
        int[,] flowDirections,
        bool[,] oceanMask,
        float threshold,
        int width,
        int height)
    {
        // Get all UPSTREAM neighbors (cells that flow INTO this cell)
        var upstreamNeighbors = GetUpstreamNeighbors(x, y, flowDirections, width, height);

        // If NO upstream neighbors exist, this is a hilltop source (valid!)
        if (upstreamNeighbors.Count == 0)
            return true;

        // Check if ALL upstream neighbors are below threshold
        foreach (var (nx, ny) in upstreamNeighbors)
        {
            // Skip ocean upstream neighbors
            if (oceanMask[ny, nx])
                continue;

            float upstreamAccumulation = flowAccumulation[ny, nx];

            // If ANY upstream neighbor is >= threshold, this is NOT a source
            // (The upstream neighbor is the earlier crossing point)
            if (upstreamAccumulation >= threshold)
                return false;
        }

        // All upstream neighbors below threshold → This is the FIRST crossing point
        return true;
    }

    /// <summary>
    /// Gets all upstream neighbors (cells that flow INTO the target cell).
    /// </summary>
    private static List<(int x, int y)> GetUpstreamNeighbors(
        int targetX, int targetY,
        int[,] flowDirections,
        int width, int height)
    {
        var upstreamNeighbors = new List<(int x, int y)>();

        // Check all 8 neighbors
        for (int dir = 0; dir < 8; dir++)
        {
            int nx = targetX + Directions[dir].dx;
            int ny = targetY + Directions[dir].dy;

            // Check bounds
            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                continue;

            int neighborFlowDir = flowDirections[ny, nx];

            // If neighbor is a sink (-1), it doesn't flow anywhere
            if (neighborFlowDir == -1)
                continue;

            // Check if neighbor flows INTO our target cell
            // (neighbor's flow direction points back to target)
            var (fdx, fdy) = FlowDirectionCalculator.GetDirectionOffset(neighborFlowDir);
            int flowsToX = nx + fdx;
            int flowsToY = ny + fdy;

            if (flowsToX == targetX && flowsToY == targetY)
            {
                upstreamNeighbors.Add((nx, ny));
            }
        }

        return upstreamNeighbors;
    }

    /// <summary>
    /// Calculates adaptive threshold based on flow accumulation distribution.
    /// Uses low percentile (15th) to catch small streams that grow into rivers.
    /// </summary>
    private static float CalculateAdaptiveThreshold(float[,] flowAccumulation, bool[,] oceanMask)
    {
        int height = flowAccumulation.GetLength(0);
        int width = flowAccumulation.GetLength(1);

        // Collect land-only accumulation values
        var landAccumulation = new List<float>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!oceanMask[y, x] && flowAccumulation[y, x] > 0)
                {
                    landAccumulation.Add(flowAccumulation[y, x]);
                }
            }
        }

        if (landAccumulation.Count == 0)
            return 0.1f;  // Fallback

        landAccumulation.Sort();

        // Use 15th percentile as threshold
        // This catches streams with moderate watershed (not tiny rivulets, not major rivers)
        int index = (int)(landAccumulation.Count * 0.15f);
        index = System.Math.Max(0, System.Math.Min(landAccumulation.Count - 1, index));

        return landAccumulation[index];
    }

    /// <summary>
    /// Calculates river importance metric for ranking/filtering.
    /// Traces downstream to estimate final river size.
    /// </summary>
    private static float CalculateRiverImportance(
        (int x, int y) source,
        float[,] flowAccumulation,
        int[,] flowDirections,
        bool[,] oceanMask,
        int width,
        int height)
    {
        // Importance = Final accumulation at river mouth (or maximum along path)
        // This represents the total watershed size

        int x = source.x;
        int y = source.y;
        float maxAccumulation = flowAccumulation[y, x];

        // Trace downstream up to 1000 steps (prevent infinite loops)
        const int maxSteps = 1000;
        int steps = 0;

        while (steps < maxSteps)
        {
            // Get flow direction
            int flowDir = flowDirections[y, x];

            // Reached sink (ocean or pit)
            if (flowDir == -1)
                break;

            // Follow flow direction
            var (dx, dy) = FlowDirectionCalculator.GetDirectionOffset(flowDir);
            x += dx;
            y += dy;

            // Check bounds
            if (x < 0 || x >= width || y < 0 || y >= height)
                break;

            // Update maximum accumulation encountered
            float accumulation = flowAccumulation[y, x];
            if (accumulation > maxAccumulation)
            {
                maxAccumulation = accumulation;
            }

            // Stop at ocean
            if (oceanMask[y, x])
                break;

            steps++;
        }

        return maxAccumulation;
    }
}
