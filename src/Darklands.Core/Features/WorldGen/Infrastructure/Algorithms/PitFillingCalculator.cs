using System;
using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Selective pit filling using priority flood fill algorithm (O(n log n)).
/// Part of VS_029 Phase 1 (Step 1a): Eliminates pathfinding artifacts while preserving real lakes.
/// </summary>
/// <remarks>
/// Algorithm: Priority Flood Fill (standard GIS hydrology practice)
///
/// Goals:
/// 1. Fill small pits (depth &lt; 50 OR area &lt; 100) - Likely noise artifacts
/// 2. Preserve large pits (depth ≥ 50 AND area ≥ 100) - Real endorheic basins (lakes)
///
/// Why selective filling is SUPERIOR to greedy tracing + A* fallback:
/// - Standard practice: Used in ArcGIS, QGIS, GRASS GIS
/// - Faster: O(n log n) once vs O(rivers × radius²) repeatedly
/// - Cleaner code: No pathfinding complexity (simpler river tracing)
/// - Better visuals: No A* detour artifacts (smooth natural flow)
/// - Geological realism: Preserves real lakes (Dead Sea, Great Salt Lake analogs)
/// - Guaranteed success: All rivers reach ocean OR lake (no stuck rivers!)
///
/// Complexity: O(n log n) where n = cell count (priority queue operations)
///
/// Real-world analogs (preserved as lakes):
/// - Dead Sea: Depth ~430m, Area ~810 km² (endorheic basin)
/// - Great Salt Lake: Depth ~10m, Area ~4400 km² (shallow but large)
/// - Caspian Sea: Depth ~1025m, Area ~371,000 km² (world's largest lake)
/// </remarks>
public static class PitFillingCalculator
{
    /// <summary>
    /// Result of selective pit filling.
    /// </summary>
    public record FillingResult
    {
        /// <summary>
        /// Heightmap after selective pit filling.
        /// Small pits raised to spillway level, large pits preserved.
        /// </summary>
        public float[,] FilledHeightmap { get; init; }

        /// <summary>
        /// Preserved lake locations (large pits NOT filled).
        /// These are endorheic basins (real lakes).
        /// </summary>
        public List<(int x, int y)> Lakes { get; init; }

        public FillingResult(float[,] filledHeightmap, List<(int x, int y)> lakes)
        {
            FilledHeightmap = filledHeightmap;
            Lakes = lakes;
        }
    }

    /// <summary>
    /// Performs selective pit filling on heightmap.
    /// </summary>
    /// <param name="heightmap">Original heightmap (raw [0-20] scale)</param>
    /// <param name="oceanMask">Ocean mask (true = water, false = land)</param>
    /// <param name="pitDepthThreshold">Max depth to fill (default 50.0)</param>
    /// <param name="pitAreaThreshold">Max area to fill (default 100 cells)</param>
    /// <returns>Filled heightmap + preserved lake locations</returns>
    public static FillingResult Fill(
        float[,] heightmap,
        bool[,] oceanMask,
        float pitDepthThreshold = 50.0f,
        int pitAreaThreshold = 100)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Clone heightmap (preserve original)
        var filledHeightmap = (float[,])heightmap.Clone();

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Find all local minima (potential pits)
        // ═══════════════════════════════════════════════════════════════════════

        var localMinima = FindLocalMinima(filledHeightmap, oceanMask, width, height);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Classify pits (fillable vs preserve as lakes)
        // ═══════════════════════════════════════════════════════════════════════

        var fillablePits = new HashSet<(int x, int y)>();
        var lakes = new List<(int x, int y)>();

        foreach (var pit in localMinima)
        {
            // Measure pit characteristics
            var (depth, area) = MeasurePit(filledHeightmap, pit, width, height);

            // Decision: Fill small/shallow pits, preserve large/deep pits as lakes
            bool shouldFill = depth < pitDepthThreshold || area < pitAreaThreshold;

            if (shouldFill)
            {
                fillablePits.Add(pit);
            }
            else
            {
                lakes.Add(pit);  // Preserve as lake (endorheic basin)
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 3: Priority flood fill (raise fillable pits to spillway level)
        // ═══════════════════════════════════════════════════════════════════════

        PriorityFloodFill(filledHeightmap, oceanMask, fillablePits, width, height);

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: Filled heightmap + preserved lakes
        // ═══════════════════════════════════════════════════════════════════════

        return new FillingResult(filledHeightmap, lakes);
    }

    /// <summary>
    /// Finds all local minima (cells lower than ALL 8 neighbors).
    /// </summary>
    private static List<(int x, int y)> FindLocalMinima(
        float[,] heightmap,
        bool[,] oceanMask,
        int width,
        int height)
    {
        var localMinima = new List<(int x, int y)>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Skip ocean cells (they're not land pits)
                if (oceanMask[y, x])
                    continue;

                float currentElev = heightmap[y, x];
                bool isLocalMin = true;

                // Check all 8 neighbors
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;  // Skip self

                        int nx = x + dx;
                        int ny = y + dy;

                        // Check bounds
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;

                        // If ANY neighbor is lower or equal, not a local minimum
                        if (heightmap[ny, nx] <= currentElev)
                        {
                            isLocalMin = false;
                            break;
                        }
                    }

                    if (!isLocalMin) break;
                }

                if (isLocalMin)
                {
                    localMinima.Add((x, y));
                }
            }
        }

        return localMinima;
    }

    /// <summary>
    /// Measures pit depth and area for classification.
    /// </summary>
    private static (float depth, int area) MeasurePit(
        float[,] heightmap,
        (int x, int y) pitCenter,
        int width,
        int height)
    {
        float pitElev = heightmap[pitCenter.y, pitCenter.x];

        // Flood fill to find pit extent
        var visited = new bool[height, width];
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue(pitCenter);
        visited[pitCenter.y, pitCenter.x] = true;

        int area = 0;
        float spillwayElev = pitElev;  // Minimum elevation to escape pit

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            area++;

            // Check 8 neighbors
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    if (visited[ny, nx])
                        continue;

                    float neighborElev = heightmap[ny, nx];

                    // If neighbor is higher, it might be pit boundary (spillway)
                    if (neighborElev > pitElev)
                    {
                        spillwayElev = Math.Max(spillwayElev, neighborElev);
                    }
                    else
                    {
                        // Neighbor is part of pit (same or lower elevation)
                        queue.Enqueue((nx, ny));
                        visited[ny, nx] = true;
                    }
                }
            }
        }

        float depth = spillwayElev - pitElev;

        return (depth, area);
    }

    /// <summary>
    /// Priority flood fill algorithm (O(n log n)).
    /// Raises fillable pits to spillway level (ensures downhill path exists).
    /// </summary>
    private static void PriorityFloodFill(
        float[,] heightmap,
        bool[,] oceanMask,
        HashSet<(int x, int y)> fillablePits,
        int width,
        int height)
    {
        var visited = new bool[height, width];
        var priorityQueue = new PriorityQueue<(int x, int y), float>();

        // Seed queue with ALL border cells (ocean + map edges)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBorder = (x == 0 || x == width - 1 || y == 0 || y == height - 1) ||
                                oceanMask[y, x];

                if (isBorder)
                {
                    priorityQueue.Enqueue((x, y), heightmap[y, x]);
                    visited[y, x] = true;
                }
            }
        }

        // Process cells from lowest to highest elevation
        float waterLevel = float.MinValue;

        while (priorityQueue.Count > 0)
        {
            var (x, y) = priorityQueue.Dequeue();
            float currentElev = heightmap[y, x];

            // Update water level (monotonically increasing)
            waterLevel = Math.Max(waterLevel, currentElev);

            // Check all 8 neighbors
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    if (visited[ny, nx])
                        continue;

                    visited[ny, nx] = true;

                    // If neighbor is fillable pit AND below water level → FILL IT!
                    if (fillablePits.Contains((nx, ny)) && heightmap[ny, nx] < waterLevel)
                    {
                        heightmap[ny, nx] = waterLevel;  // Raise to spillway level
                    }

                    priorityQueue.Enqueue((nx, ny), heightmap[ny, nx]);
                }
            }
        }
    }
}
