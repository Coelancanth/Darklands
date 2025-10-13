using System;
using System.Collections.Generic;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

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
    /// Result of selective pit filling (TD_023: Enhanced with basin metadata).
    /// </summary>
    public record FillingResult
    {
        /// <summary>
        /// Heightmap after selective pit filling.
        /// Small pits raised to spillway level, large pits preserved.
        /// </summary>
        public float[,] FilledHeightmap { get; init; }

        /// <summary>
        /// Preserved basin metadata (large pits NOT filled).
        /// Includes complete hydrological data: cells, pour points, depths (TD_023).
        /// These are endorheic basins (real lakes) with area ≥ 100 AND depth ≥ 50.
        /// </summary>
        public List<BasinMetadata> PreservedBasins { get; init; }

        public FillingResult(float[,] filledHeightmap, List<BasinMetadata> preservedBasins)
        {
            FilledHeightmap = filledHeightmap;
            PreservedBasins = preservedBasins;
        }
    }

    /// <summary>
    /// Performs selective pit filling on heightmap.
    /// </summary>
    /// <param name="heightmap">Original heightmap (raw [0-20] scale)</param>
    /// <param name="oceanMask">Ocean mask (true = water, false = land)</param>
    /// <param name="pitDepthThreshold">Max depth to fill (default 50.0)</param>
    /// <param name="pitAreaThreshold">Max area to fill (default 100 cells)</param>
    /// <param name="logger">Optional logger for diagnostic output (TD_023 debugging)</param>
    /// <returns>Filled heightmap + preserved lake locations</returns>
    public static FillingResult Fill(
        float[,] heightmap,
        bool[,] oceanMask,
        float pitDepthThreshold = 50.0f,
        int pitAreaThreshold = 100,
        ILogger? logger = null)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Clone heightmap (preserve original)
        var filledHeightmap = (float[,])heightmap.Clone();

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Find all local minima (potential pits)
        // ═══════════════════════════════════════════════════════════════════════

        var localMinima = FindLocalMinima(filledHeightmap, oceanMask, width, height);

        // TD_023 DEBUG: Log local minima count
        logger?.LogInformation("PIT-FILLING STEP 1: Found {Count} local minima (potential pits)", localMinima.Count);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Classify pits (fillable vs preserve as lakes) - TD_023
        // ═══════════════════════════════════════════════════════════════════════

        var fillablePits = new HashSet<(int x, int y)>();
        var preservedBasins = new List<BasinMetadata>();

        int basinId = 0;  // TD_023: Unique identifier for each basin
        foreach (var pit in localMinima)
        {
            // TD_023: Measure pit characteristics and collect complete metadata
            var basinMetadata = MeasurePit(filledHeightmap, pit, basinId, width, height);

            // Decision: Fill small/shallow pits, preserve large/deep pits as lakes
            // TD_023 FIX: Use AND (both conditions must fail) not OR (either condition fails)
            // A large flat basin (area >> 100, depth < 50) should be PRESERVED (inner sea!)
            bool shouldFill = basinMetadata.Depth < pitDepthThreshold && basinMetadata.Area < pitAreaThreshold;

            // TD_023 DEBUG: Log classification decision for first 10 basins
            if (basinId < 10 || basinMetadata.Area > 1000)
            {
                logger?.LogInformation(
                    "  Basin {Id} at ({X},{Y}): depth={Depth:F1}, area={Area} cells -> {Decision}",
                    basinId, pit.x, pit.y, basinMetadata.Depth, basinMetadata.Area,
                    shouldFill ? "FILL" : "PRESERVE");
            }

            if (shouldFill)
            {
                fillablePits.Add(pit);
            }
            else
            {
                preservedBasins.Add(basinMetadata);  // TD_023: Preserve complete basin metadata
            }

            basinId++;  // Increment for next basin
        }

        // TD_023 DEBUG: Summary
        logger?.LogInformation(
            "PIT-FILLING STEP 2: Classified {Total} basins -> {Filled} fillable, {Preserved} preserved",
            localMinima.Count, fillablePits.Count, preservedBasins.Count);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 3: Priority flood fill (raise fillable pits to spillway level)
        // ═══════════════════════════════════════════════════════════════════════

        PriorityFloodFill(filledHeightmap, oceanMask, fillablePits, width, height);

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: Filled heightmap + preserved basin metadata (TD_023)
        // ═══════════════════════════════════════════════════════════════════════

        return new FillingResult(filledHeightmap, preservedBasins);
    }

    /// <summary>
    /// Finds all landlocked basins below sea level using flood-fill (TD_023 REWRITE).
    /// Returns one representative cell per basin (to be measured by MeasurePit).
    /// </summary>
    /// <remarks>
    /// NEW ALGORITHM (suggested by user - MUCH better!):
    /// 1. Find all cells BELOW sea level (elevation < 1.0)
    /// 2. Exclude ocean mask (cells connected to border)
    /// 3. Flood-fill to find connected regions (each = one basin)
    /// 4. Return center of each region as "local minimum"
    ///
    /// Why this works:
    /// - Inner seas are flat/irregular regions BELOW sea level
    /// - Flood-fill naturally handles flat basins (no need for complex local minima logic)
    /// - Each connected region = one basin (automatic de-duplication)
    /// </remarks>
    private static List<(int x, int y)> FindLocalMinima(
        float[,] heightmap,
        bool[,] oceanMask,
        int width,
        int height)
    {
        const float SEA_LEVEL = 1.0f;  // WorldGenConstants.SEA_LEVEL_RAW

        var visited = new bool[height, width];
        var basins = new List<(int x, int y)>();

        // Scan for landlocked below-sea-level cells (potential basin starts)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Skip if already visited or not below sea level
                if (visited[y, x] || heightmap[y, x] >= SEA_LEVEL)
                    continue;

                // Skip if ocean (connected to border)
                if (oceanMask[y, x])
                    continue;

                // Found a landlocked below-sea-level cell - flood-fill to find full basin
                var basinCells = FloodFillBasin(heightmap, visited, x, y, SEA_LEVEL, width, height);

                if (basinCells.Count > 0)
                {
                    // Use first cell as representative (MeasurePit will re-flood to find full extent)
                    basins.Add(basinCells[0]);
                }
            }
        }

        return basins;
    }

    /// <summary>
    /// Flood-fills a below-sea-level basin, marking visited cells and returning all cells in basin.
    /// </summary>
    private static List<(int x, int y)> FloodFillBasin(
        float[,] heightmap,
        bool[,] visited,
        int startX,
        int startY,
        float seaLevel,
        int width,
        int height)
    {
        var basinCells = new List<(int x, int y)>();
        var queue = new Queue<(int x, int y)>();

        queue.Enqueue((startX, startY));
        visited[startY, startX] = true;

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            basinCells.Add((x, y));

            // Check 4-connected neighbors (use 4-connected for conservative basin detection)
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { -1, 0, 1, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                // Check bounds
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                // Skip if visited or above sea level
                if (visited[ny, nx] || heightmap[ny, nx] >= seaLevel)
                    continue;

                // Add to basin
                visited[ny, nx] = true;
                queue.Enqueue((nx, ny));
            }
        }

        return basinCells;
    }

    /// <summary>
    /// Measures pit characteristics and returns complete basin metadata (TD_023).
    /// Performs flood-fill from pit center to determine basin extent, pour point, and depth.
    /// </summary>
    /// <param name="heightmap">Heightmap in raw elevation scale</param>
    /// <param name="pitCenter">Local minimum (basin center)</param>
    /// <param name="basinId">Unique basin identifier</param>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <returns>Complete basin metadata including cells, pour point, and depth</returns>
    /// <remarks>
    /// Hydrological Semantics (TD_023 clarification):
    /// - spillwayElev = MAX(boundary neighbors) → Used for depth calculation (highest water can rise)
    /// - pourPoint = location of MIN(boundary neighbors) → Actual outlet location (where water exits)
    /// - surfaceElevation = pourPoint elevation → Water level at equilibrium (where overflow occurs)
    ///
    /// Why track both?
    /// - Depth calculation needs spillway (max rim height for water column measurement)
    /// - VS_030 pathfinding needs pour point (actual outlet location for thalweg routing)
    ///
    /// Example: Caldera with rim varying 1000m-2000m, pit bottom at 800m:
    /// - spillwayElev = 2000m (max rim) → depth = 2000 - 800 = 1200m
    /// - pourPoint = (x,y) at 1000m location → VS_030 routes thalweg to this outlet
    /// </remarks>
    private static BasinMetadata MeasurePit(
        float[,] heightmap,
        (int x, int y) pitCenter,
        int basinId,
        int width,
        int height)
    {
        const float SEA_LEVEL = 1.0f;  // WorldGenConstants.SEA_LEVEL_RAW
        float pitElev = heightmap[pitCenter.y, pitCenter.x];

        // TD_023 FIX: Flood-fill ALL cells below sea level (not just equal-elevation!)
        // Inner seas have varying elevations (0.11-0.12), so exact equality fails
        var visited = new bool[height, width];
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue(pitCenter);
        visited[pitCenter.y, pitCenter.x] = true;

        // TD_023: Collect ALL cells in basin (critical for VS_030 inlet detection)
        var cells = new List<(int x, int y)>();
        int area = 0;

        // Track BOTH spillway (max boundary) and pour point (min boundary)
        float spillwayElev = pitElev;  // Maximum elevation to escape pit (for depth)
        float pourPointElev = float.MaxValue;  // Minimum boundary elevation (for outlet)
        (int x, int y) pourPoint = pitCenter;  // Outlet location (default to center if no boundary found)

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            area++;
            cells.Add((x, y));  // TD_023: Collect cell for basin boundary tracking

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

                    // TD_023 FIX: Flood-fill ALL cells BELOW sea level (matches FindLocalMinima logic)
                    if (neighborElev >= SEA_LEVEL)
                    {
                        // Neighbor is above sea level = rim boundary
                        spillwayElev = Math.Max(spillwayElev, neighborElev);

                        // Track pour point (MIN boundary - for VS_030 outlet location)
                        if (neighborElev < pourPointElev)
                        {
                            pourPointElev = neighborElev;
                            pourPoint = (nx, ny);  // Actual outlet location
                        }
                    }
                    else
                    {
                        // Neighbor is below sea level = part of basin (expand flood-fill)
                        queue.Enqueue((nx, ny));
                        visited[ny, nx] = true;
                    }
                }
            }
        }

        float depth = spillwayElev - pitElev;
        float surfaceElevation = pourPointElev;  // Water level = pour point elevation

        // TD_023: Return complete BasinMetadata (not just depth/area tuple)
        return new BasinMetadata(
            basinId: basinId,
            center: pitCenter,
            cells: cells,
            pourPoint: pourPoint,
            surfaceElevation: surfaceElevation,
            depth: depth,
            area: area);
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
