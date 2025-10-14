using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Detects local minima (sinks/pits) in a heightmap for pit-filling validation.
/// Part of VS_029 diagnostic tooling: Compare pre/post pit-filling to validate algorithm effectiveness.
/// </summary>
/// <remarks>
/// Local minimum criteria:
/// - Land cell (not ocean)
/// - No 8-connected neighbor is LOWER than this cell
/// - Represents potential pit that pit-filling algorithm should handle
///
/// Expected counts:
/// - BEFORE pit-filling: 5-20% of land cells (noisy raw heightmap)
/// - AFTER pit-filling: <5% of land cells (artifacts filled, real lakes preserved)
/// - Reduction: 70-90% (validates pit-filling effectiveness!)
/// </remarks>
public static class LocalMinimaDetector
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
    /// Detects all local minima in a heightmap (excludes ocean cells).
    /// </summary>
    /// <param name="heightmap">Heightmap to analyze (raw [0-20] scale)</param>
    /// <param name="oceanMask">Ocean mask (true = water, false = land)</param>
    /// <returns>List of (x, y) coordinates of local minima</returns>
    public static List<(int x, int y)> Detect(float[,] heightmap, bool[,] oceanMask)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var localMinima = new List<(int x, int y)>();

        // Check each cell for local minimum criteria
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Skip ocean cells (ocean is terminal sink, not a "pit")
                if (oceanMask[y, x])
                    continue;

                float currentElev = heightmap[y, x];
                bool isLocalMinimum = true;

                // Check all 8 neighbors - if ANY neighbor is LOWER, not a minimum
                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = x + Directions[dir].dx;
                    int ny = y + Directions[dir].dy;

                    // Check bounds
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    float neighborElev = heightmap[ny, nx];

                    // If neighbor is lower, this is NOT a local minimum
                    if (neighborElev < currentElev)
                    {
                        isLocalMinimum = false;
                        break;
                    }
                }

                if (isLocalMinimum)
                {
                    localMinima.Add((x, y));
                }
            }
        }

        return localMinima;
    }
}
