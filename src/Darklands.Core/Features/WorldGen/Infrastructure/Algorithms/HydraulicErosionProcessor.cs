using System;
using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Hydraulic erosion simulation for realistic river systems and valley formation.
/// Ported from WorldEngine's erosion.py (ErosionSimulation class).
///
/// Algorithm overview:
/// 1. FindWaterFlow: Compute flow direction for each cell (steepest descent)
/// 2. FindRiverSources: Identify river sources (mountains + precipitation threshold)
/// 3. TraceRiverPath: Trace rivers from source to ocean (with A* fallback)
/// 4. ErodeValleysAroundRivers: Carve valleys around river paths (radius 2, gentle curves)
/// 5. CleanUpFlow: Ensure monotonic elevation along rivers (no uphill flow)
///
/// Result: Rivers flowing realistically from mountains to sea, with eroded valleys
/// and lakes where rivers can't reach the ocean.
///
/// Reference: References/worldengine/worldengine/simulations/erosion.py
/// </summary>
public static class HydraulicErosionProcessor
{
    // WorldEngine constants
    private const float RiverThreshold = 0.02f;       // Min precipitation flow to form river source
    private const int RiverSourceMinSpacing = 9;      // Min radius between river sources (cells)
    private const int ErosionRadius = 2;              // Valley erosion radius around rivers
    private const int SearchRadius = 40;              // Max search radius for lower elevation
    private const float AdjacentErosionCurve = 0.2f;  // Erosion strength for adjacent cells
    private const float DiagonalErosionCurve = 0.05f; // Erosion strength for diagonal cells

    // Direction offsets (4-connected: N, E, S, W)
    private static readonly (int dx, int dy)[] Directions =
    {
        (0, -1),  // North
        (1, 0),   // East
        (0, 1),   // South
        (-1, 0)   // West
    };

    // Direction offsets with center (for flow direction indexing)
    private static readonly (int dx, int dy)[] DirectionsWithCenter =
    {
        (0, 0),   // Center (index 0 = no flow)
        (0, -1),  // North  (index 1)
        (1, 0),   // East   (index 2)
        (0, 1),   // South  (index 3)
        (-1, 0)   // West   (index 4)
    };

    /// <summary>
    /// Executes hydraulic erosion simulation on a heightmap.
    /// </summary>
    /// <param name="heightmap">Elevation data (will be modified in-place for erosion)</param>
    /// <param name="oceanMask">Ocean mask (true = water, false = land)</param>
    /// <param name="precipitationMap">Precipitation data (for river source detection)</param>
    /// <param name="seaLevel">Sea level threshold (for mountain detection)</param>
    /// <returns>Tuple of (eroded heightmap, rivers, lakes)</returns>
    public static (float[,] heightmap, List<River> rivers, List<(int x, int y)> lakes) Execute(
        float[,] heightmap,
        bool[,] oceanMask,
        float[,] precipitationMap,
        float seaLevel = 0.65f)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Make a working copy of heightmap (erosion modifies it)
        var workingHeightmap = (float[,])heightmap.Clone();

        // Step 1: Compute flow direction for each cell
        var waterPath = FindWaterFlow(workingHeightmap, width, height);

        // Step 2: Find river sources (mountains with sufficient precipitation)
        var waterFlow = new float[height, width];
        var riverSources = FindRiverSources(
            workingHeightmap,
            oceanMask,
            precipitationMap,
            waterPath,
            waterFlow,
            seaLevel,
            width,
            height);

        // Step 3: Trace rivers from sources to ocean
        var rivers = new List<River>();
        var lakes = new List<(int x, int y)>();

        foreach (var source in riverSources)
        {
            var riverPath = TraceRiverPath(
                source,
                workingHeightmap,
                oceanMask,
                rivers,
                lakes,
                width,
                height);

            if (riverPath != null && riverPath.Count > 0)
            {
                // Ensure river flows downhill monotonically
                CleanUpFlow(riverPath, workingHeightmap);

                // Check if river reached ocean or formed a lake
                var (lastX, lastY) = riverPath[^1];
                bool reachedOcean = oceanMask[lastY, lastX];

                rivers.Add(new River(riverPath, reachedOcean));

                if (!reachedOcean)
                    lakes.Add((lastX, lastY));
            }
        }

        // Step 4: Erode valleys around rivers
        foreach (var river in rivers)
        {
            ErodeValleysAroundRivers(river.Path, workingHeightmap, width, height);
        }

        return (workingHeightmap, rivers, lakes);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Algorithm 1: Find Water Flow Direction
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Computes flow direction for each cell (steepest descent to neighbors).
    /// Direction encoded as index: 0=none, 1=N, 2=E, 3=S, 4=W
    /// </summary>
    private static int[,] FindWaterFlow(float[,] heightmap, int width, int height)
    {
        var waterPath = new int[height, width];

        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                var lowestNeighbor = FindLowestNeighbor(x, y, heightmap, width, height);

                if (lowestNeighbor.HasValue)
                {
                    var (tx, ty) = lowestNeighbor.Value;
                    var flowDir = (tx - x, ty - y);

                    // Encode flow direction as index
                    for (int i = 0; i < DirectionsWithCenter.Length; i++)
                    {
                        if (DirectionsWithCenter[i] == flowDir)
                        {
                            waterPath[y, x] = i;
                            break;
                        }
                    }
                }
            }
        }

        return waterPath;
    }

    /// <summary>
    /// Finds the lowest neighboring cell (4-connected).
    /// Returns null if no lower neighbor exists.
    /// </summary>
    private static (int x, int y)? FindLowestNeighbor(
        int x,
        int y,
        float[,] heightmap,
        int width,
        int height)
    {
        float currentElevation = heightmap[y, x];
        float lowestElevation = currentElevation;
        (int x, int y)? lowestNeighbor = null;

        foreach (var (dx, dy) in Directions)
        {
            int nx = x + dx;
            int ny = y + dy;

            // Check bounds
            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                continue;

            float neighborElevation = heightmap[ny, nx];

            if (neighborElevation < lowestElevation)
            {
                lowestElevation = neighborElevation;
                lowestNeighbor = (nx, ny);
            }
        }

        return lowestNeighbor;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Algorithm 2: Find River Sources
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Identifies river sources: mountain peaks with accumulated water flow above threshold.
    /// Prevents clustering by enforcing minimum spacing between sources.
    /// </summary>
    private static List<(int x, int y)> FindRiverSources(
        float[,] heightmap,
        bool[,] oceanMask,
        float[,] precipitationMap,
        int[,] waterPath,
        float[,] waterFlow,
        float seaLevel,
        int width,
        int height)
    {
        var riverSources = new List<(int x, int y)>();

        // Accumulate water flow by following paths
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                float rainfall = precipitationMap[y, x];
                waterFlow[y, x] = rainfall;

                // No flow direction? Skip
                if (waterPath[y, x] == 0)
                    continue;

                // Follow flow path and accumulate rainfall
                int cx = x, cy = y;
                bool neighborSeedFound = false;

                while (!neighborSeedFound)
                {
                    // Is this a valid river source?
                    if (IsMountain(heightmap[cy, cx], seaLevel) &&
                        waterFlow[cy, cx] >= RiverThreshold)
                    {
                        // Check if too close to existing sources
                        bool tooClose = false;
                        foreach (var (sx, sy) in riverSources)
                        {
                            if (IsInCircle(RiverSourceMinSpacing, cx, cy, sx, sy))
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (!tooClose)
                        {
                            riverSources.Add((cx, cy));
                        }

                        break;
                    }

                    // No flow path? Dead end
                    if (waterPath[cy, cx] == 0)
                        break;

                    // Follow flow direction
                    var (dx, dy) = DirectionsWithCenter[waterPath[cy, cx]];
                    int nx = cx + dx;
                    int ny = cy + dy;

                    // Accumulate flow
                    waterFlow[ny, nx] += rainfall;

                    cx = nx;
                    cy = ny;
                }
            }
        }

        return riverSources;
    }

    /// <summary>
    /// Checks if elevation qualifies as mountain (above sea level + threshold).
    /// </summary>
    private static bool IsMountain(float elevation, float seaLevel)
    {
        // Mountains are significantly above sea level
        return elevation > seaLevel + 0.1f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Algorithm 3: Trace River Path
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces a river path from source to ocean (or lake if no path found).
    /// Uses steepest descent with A* pathfinding fallback for complex terrain.
    /// </summary>
    private static List<(int x, int y)>? TraceRiverPath(
        (int x, int y) source,
        float[,] heightmap,
        bool[,] oceanMask,
        List<River> existingRivers,
        List<(int x, int y)> lakes,
        int width,
        int height)
    {
        var path = new List<(int x, int y)> { source };
        var current = source;

        while (true)
        {
            var (x, y) = current;

            // Check if we can merge into an existing river
            foreach (var (dx, dy) in Directions)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                // Is this cell part of an existing river?
                foreach (var river in existingRivers)
                {
                    int mergeIndex = river.Path.IndexOf((nx, ny));
                    if (mergeIndex >= 0)
                    {
                        // Merge into existing river from this point onward
                        for (int i = mergeIndex; i < river.Path.Count; i++)
                        {
                            path.Add(river.Path[i]);
                        }
                        return path;
                    }
                }
            }

            // Reached ocean?
            if (oceanMask[y, x])
                return path;

            // Try steepest descent
            var lowestNeighbor = FindLowestNeighbor(x, y, heightmap, width, height);

            if (lowestNeighbor.HasValue)
            {
                current = lowestNeighbor.Value;
                path.Add(current);
                continue;
            }

            // Steepest descent failed - use A* to find lower elevation
            var lowerElevation = FindLowerElevationInRadius(current, heightmap, width, height);

            if (lowerElevation.HasValue)
            {
                // Use A* pathfinding to reach lower elevation
                var astarPath = AStarPathfinder.FindPath(heightmap, current, lowerElevation.Value);

                if (astarPath.Count > 0)
                {
                    path.AddRange(astarPath);
                    current = astarPath[^1];
                    continue;
                }
            }

            // Can't find path to ocean - this becomes a lake
            lakes.Add(current);
            return path;
        }
    }

    /// <summary>
    /// Searches for lower elevation within expanding radius (1 to SearchRadius).
    /// Returns first lower cell found, or null if none exists.
    /// </summary>
    private static (int x, int y)? FindLowerElevationInRadius(
        (int x, int y) source,
        float[,] heightmap,
        int width,
        int height)
    {
        var (sx, sy) = source;
        float sourceElevation = heightmap[sy, sx];

        for (int radius = 1; radius <= SearchRadius; radius++)
        {
            for (int cx = -radius; cx <= radius; cx++)
            {
                for (int cy = -radius; cy <= radius; cy++)
                {
                    int nx = sx + cx;
                    int ny = sy + cy;

                    // Within bounds?
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    // Within circle?
                    if (!IsInCircle(radius, sx, sy, nx, ny))
                        continue;

                    // Lower elevation?
                    if (heightmap[ny, nx] < sourceElevation)
                        return (nx, ny);
                }
            }
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Algorithm 4: Erode Valleys Around Rivers
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Carves valleys around river paths by eroding surrounding terrain.
    /// Uses gentle curves for natural-looking valleys (stronger erosion near river).
    /// </summary>
    private static void ErodeValleysAroundRivers(
        List<(int x, int y)> riverPath,
        float[,] heightmap,
        int width,
        int height)
    {
        foreach (var (rx, ry) in riverPath)
        {
            float riverElevation = heightmap[ry, rx];

            // Erode cells within radius
            for (int x = rx - ErosionRadius; x <= rx + ErosionRadius; x++)
            {
                for (int y = ry - ErosionRadius; y <= ry + ErosionRadius; y++)
                {
                    // Bounds check
                    if (x < 0 || x >= width || y < 0 || y >= height)
                        continue;

                    // Skip river cell itself
                    if (x == rx && y == ry)
                        continue;

                    // Skip if cell in river path
                    if (riverPath.Contains((x, y)))
                        continue;

                    // Skip if cell already lower than river
                    if (heightmap[y, x] <= riverElevation)
                        continue;

                    // Within circular radius?
                    if (!IsInCircle(ErosionRadius, rx, ry, x, y))
                        continue;

                    // Calculate erosion curve (distance-based)
                    float curve = CalculateErosionCurve(rx, ry, x, y);

                    // Erode towards river level
                    float diff = riverElevation - heightmap[y, x];
                    float newElevation = heightmap[y, x] + (diff * curve);

                    // Safety: Never erode below river level
                    if (newElevation > riverElevation)
                    {
                        heightmap[y, x] = newElevation;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calculates erosion curve factor based on distance from river.
    /// Adjacent cells: 0.2 (strong erosion), Diagonal: 0.05 (gentle erosion)
    /// </summary>
    private static float CalculateErosionCurve(int rx, int ry, int x, int y)
    {
        int dx = Math.Abs(rx - x);
        int dy = Math.Abs(ry - y);

        // Adjacent (1 cell away)
        if (dx == 1 || dy == 1)
            return AdjacentErosionCurve;

        // Diagonal (2 cells away)
        if (dx == 2 || dy == 2)
            return DiagonalErosionCurve;

        // Default (shouldn't reach here with radius 2)
        return 0.01f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Algorithm 5: Clean Up Flow (Monotonic Elevation)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ensures river flows downhill monotonically (no uphill segments).
    /// If a cell is higher than the previous cell, lower it to match.
    /// </summary>
    private static void CleanUpFlow(List<(int x, int y)> riverPath, float[,] heightmap)
    {
        float previousElevation = 1.0f; // Start high (will be replaced by first cell)

        foreach (var (x, y) in riverPath)
        {
            float currentElevation = heightmap[y, x];

            if (currentElevation <= previousElevation)
            {
                // Flowing downhill - OK
                previousElevation = currentElevation;
            }
            else
            {
                // Flowing uphill - fix it!
                heightmap[y, x] = previousElevation;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Functions
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks if a point is within a circular radius.
    /// </summary>
    private static bool IsInCircle(int radius, int centerX, int centerY, int x, int y)
    {
        int dx = centerX - x;
        int dy = centerY - y;
        int squareDist = dx * dx + dy * dy;
        return squareDist <= radius * radius;
    }
}

// ═══════════════════════════════════════════════════════════════════════
// Domain Types
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Represents a river with its path and destination.
/// </summary>
public record River(
    List<(int x, int y)> Path,
    bool ReachedOcean);
