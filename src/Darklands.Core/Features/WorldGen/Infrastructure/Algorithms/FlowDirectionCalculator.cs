namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Computes flow direction for each cell (8-connected steepest descent).
/// Part of VS_029 Phase 1 (Step 1b): Foundation for flow accumulation.
/// </summary>
/// <remarks>
/// Algorithm:
/// For each cell, find the steepest downhill neighbor among 8 directions.
/// Direction encoding: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW, -1=sink
///
/// Sinks (flowDir = -1) occur at:
/// - Ocean cells
/// - Pits/lakes (local minima after pit filling)
/// - Flat regions (no lower neighbor)
///
/// Computed on FILLED heightmap (after selective pit filling) to minimize sinks.
/// </remarks>
public static class FlowDirectionCalculator
{
    /// <summary>
    /// 8-direction offsets: N, NE, E, SE, S, SW, W, NW.
    /// Index corresponds to direction code (0-7).
    /// </summary>
    private static readonly (int dx, int dy)[] Directions = new[]
    {
        (0, -1),   // 0: North
        (1, -1),   // 1: North-East
        (1, 0),    // 2: East
        (1, 1),    // 3: South-East
        (0, 1),    // 4: South
        (-1, 1),   // 5: South-West
        (-1, 0),   // 6: West
        (-1, -1)   // 7: North-West
    };

    /// <summary>
    /// Computes flow direction map from filled heightmap.
    /// </summary>
    /// <param name="filledHeightmap">Heightmap after selective pit filling (raw [0-20] scale)</param>
    /// <param name="oceanMask">Ocean mask (true = water, false = land)</param>
    /// <returns>Flow direction map: 0-7 (direction index) or -1 (sink)</returns>
    public static int[,] Calculate(float[,] filledHeightmap, bool[,] oceanMask)
    {
        int height = filledHeightmap.GetLength(0);
        int width = filledHeightmap.GetLength(1);

        var flowDirections = new int[height, width];

        // Compute flow direction for each cell
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Ocean cells are terminal sinks
                if (oceanMask[y, x])
                {
                    flowDirections[y, x] = -1;  // Sink
                    continue;
                }

                // Find steepest downhill neighbor
                float currentElev = filledHeightmap[y, x];
                int steepestDir = -1;  // Default: sink (no lower neighbor)
                float steepestDrop = 0f;

                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = x + Directions[dir].dx;
                    int ny = y + Directions[dir].dy;

                    // Check bounds
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    float neighborElev = filledHeightmap[ny, nx];
                    float drop = currentElev - neighborElev;

                    // Track steepest drop
                    if (drop > steepestDrop)
                    {
                        steepestDrop = drop;
                        steepestDir = dir;
                    }
                }

                flowDirections[y, x] = steepestDir;  // -1 if no lower neighbor (sink/pit)
            }
        }

        return flowDirections;
    }

    /// <summary>
    /// Gets the (dx, dy) offset for a given direction index.
    /// </summary>
    /// <param name="directionIndex">Direction index (0-7) or -1 (sink)</param>
    /// <returns>Offset (dx, dy) or (0, 0) for sinks</returns>
    public static (int dx, int dy) GetDirectionOffset(int directionIndex)
    {
        if (directionIndex < 0 || directionIndex >= 8)
            return (0, 0);  // Sink has no offset

        return Directions[directionIndex];
    }
}
