using System;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Irrigation simulation using logarithmic kernel convolution for moisture spreading.
/// Ported from WorldEngine's irrigation.py (IrrigationSimulation class).
///
/// Algorithm overview:
/// - For each ocean cell with watermap value
/// - Spread moisture influence to land cells within radius 10
/// - Influence decay logarithmically with distance: watermap[cell] / (log(distance + 1) + 1)
/// - Accumulate irrigation values from all ocean cells
///
/// Result: Irrigation map showing moisture availability from proximity to water bodies.
/// This feeds into humidity calculation (Phase 4) with 3× weight vs direct precipitation.
///
/// Reference: References/worldengine/worldengine/simulations/irrigation.py
/// </summary>
public static class IrrigationCalculator
{
    private const int Radius = 10; // Neighborhood radius for moisture spreading

    /// <summary>
    /// Calculates irrigation map by spreading watermap values from ocean cells.
    /// Uses logarithmic kernel to model natural moisture decay with distance.
    /// </summary>
    /// <param name="watermap">Flow accumulation data from watermap simulation</param>
    /// <param name="oceanMask">Ocean mask (true = water source, false = land)</param>
    /// <returns>Irrigation map showing moisture influence from water bodies</returns>
    public static float[,] Execute(float[,] watermap, bool[,] oceanMask)
    {
        int height = watermap.GetLength(0);
        int width = watermap.GetLength(1);

        var irrigation = new float[height, width];

        // Pre-calculate logarithmic distance kernel (21×21 matrix, radius 10)
        var logKernel = CalculateLogarithmicKernel(Radius);

        // For each ocean cell, spread moisture influence to neighboring land
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Only ocean cells spread moisture
                if (!oceanMask[y, x])
                    continue;

                float watermapValue = watermap[y, x];

                // Skip cells with no water flow
                if (watermapValue <= 0)
                    continue;

                // Spread moisture to neighborhood
                SpreadMoistureInfluence(
                    x, y,
                    watermapValue,
                    logKernel,
                    irrigation,
                    width, height);
            }
        }

        return irrigation;
    }

    /// <summary>
    /// Pre-calculates logarithmic distance kernel for efficiency.
    /// Formula: log(sqrt(dx² + dy²) + 1) + 1
    /// </summary>
    private static float[,] CalculateLogarithmicKernel(int radius)
    {
        int kernelSize = radius * 2 + 1;
        var kernel = new float[kernelSize, kernelSize];

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                // Distance from center
                float distance = MathF.Sqrt(dx * dx + dy * dy);

                // Logarithmic decay formula
                // log1p(x) = log(x + 1) for numerical stability
                kernel[dy + radius, dx + radius] = MathF.Log(distance + 1) + 1;
            }
        }

        return kernel;
    }

    /// <summary>
    /// Spreads moisture influence from a water cell to its neighborhood.
    /// Accumulates irrigation values weighted by logarithmic distance kernel.
    /// </summary>
    private static void SpreadMoistureInfluence(
        int centerX,
        int centerY,
        float watermapValue,
        float[,] logKernel,
        float[,] irrigation,
        int width,
        int height)
    {
        // Calculate bounds for neighborhood (clamped to map edges)
        int minX = Math.Max(centerX - Radius, 0);
        int maxX = Math.Min(centerX + Radius, width - 1);
        int minY = Math.Max(centerY - Radius, 0);
        int maxY = Math.Min(centerY + Radius, height - 1);

        // Calculate corresponding kernel slice (accounts for map edge clipping)
        int kernelOffsetX = Math.Max(Radius - centerX, 0);
        int kernelOffsetY = Math.Max(Radius - centerY, 0);

        // Spread moisture influence to all cells in neighborhood
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                // Calculate kernel indices
                int kernelX = kernelOffsetX + (x - minX);
                int kernelY = kernelOffsetY + (y - minY);

                // Get logarithmic distance factor
                float logDistance = logKernel[kernelY, kernelX];

                // Accumulate moisture influence: watermap / log(distance)
                irrigation[y, x] += watermapValue / logDistance;
            }
        }
    }
}
