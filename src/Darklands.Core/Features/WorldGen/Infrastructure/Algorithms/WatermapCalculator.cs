using System;
using System.Collections.Generic;
using System.Linq;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Watermap simulation using droplet model for flow accumulation.
/// Ported from WorldEngine's hydrology.py (WatermapSimulation class).
///
/// Algorithm overview:
/// 1. Seed 20,000 droplets on random land cells (weighted by precipitation)
/// 2. Each droplet flows downhill recursively, accumulating flow in watermap
/// 3. Flow is distributed proportionally to lower neighbors by elevation difference
/// 4. Calculate thresholds for creek (5%), river (2%), main river (0.7%)
///
/// Result: Watermap showing accumulated flow through each cell, with thresholds
/// for classifying waterways by size.
///
/// Reference: References/worldengine/worldengine/simulations/hydrology.py
/// </summary>
public static class WatermapCalculator
{
    private const int DefaultDropletCount = 20000;
    private const float MinFlowQuantity = 0.05f; // Stop recursion below this threshold

    // WorldEngine threshold percentiles for waterway classification
    private const float CreekPercentile = 0.05f;      // 5th percentile (top 5% cells = creeks)
    private const float RiverPercentile = 0.02f;      // 2nd percentile (top 2% cells = rivers)
    private const float MainRiverPercentile = 0.007f; // 0.7th percentile (top 0.7% cells = main rivers)

    /// <summary>
    /// Calculates watermap using droplet model.
    /// Seeds droplets on random land cells (weighted by precipitation) and simulates
    /// flow accumulation through recursive downhill distribution.
    /// </summary>
    /// <param name="heightmap">Elevation data</param>
    /// <param name="oceanMask">Ocean mask (true = water, false = land)</param>
    /// <param name="precipitationMap">Precipitation (for droplet weighting)</param>
    /// <param name="seed">Random seed for droplet sampling</param>
    /// <param name="dropletCount">Number of droplets to simulate (default: 20000)</param>
    /// <returns>Tuple of (watermap, thresholds)</returns>
    public static (float[,] watermap, WatermapThresholds thresholds) Execute(
        float[,] heightmap,
        bool[,] oceanMask,
        float[,] precipitationMap,
        int seed,
        int dropletCount = DefaultDropletCount)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var watermap = new float[height, width];

        // Sample random land cells for droplet seeding
        var landSamples = SampleRandomLandCells(oceanMask, dropletCount, seed);

        // Simulate each droplet
        foreach (var (x, y) in landSamples)
        {
            float precipitationAtCell = precipitationMap[y, x];

            // Only simulate if cell has precipitation
            if (precipitationAtCell > 0)
            {
                SimulateDroplet((x, y), precipitationAtCell, heightmap, watermap, oceanMask, width, height);
            }
        }

        // Calculate waterway classification thresholds
        var thresholds = CalculateThresholds(watermap, oceanMask);

        return (watermap, thresholds);
    }

    /// <summary>
    /// Simulates a single water droplet flowing downhill recursively.
    /// Droplet quantity is distributed proportionally to lower neighbors based on elevation difference.
    /// </summary>
    private static void SimulateDroplet(
        (int x, int y) position,
        float quantity,
        float[,] heightmap,
        float[,] watermap,
        bool[,] oceanMask,
        int width,
        int height)
    {
        // Stop if quantity too small
        if (quantity < 0)
            return;

        var (x, y) = position;

        // Current elevation (elevation + accumulated water)
        float currentElevation = heightmap[y, x] + watermap[y, x];

        // Find lower neighbors and calculate flow distribution
        var lowerNeighbors = new List<(int weight, int x, int y)>();
        int totalWeight = 0;

        // Check all 4-connected neighbors
        foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) })
        {
            int nx = x + dx;
            int ny = y + dy;

            // Check bounds
            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                continue;

            float neighborElevation = heightmap[ny, nx] + watermap[ny, nx];

            // Is neighbor lower?
            if (neighborElevation < currentElevation)
            {
                // Weight by elevation difference (higher diff = more flow)
                // Bitshift left by 2 (multiply by 4) to amplify differences
                int weight = (int)((currentElevation - neighborElevation) * 4.0f);

                // Ensure minimum weight of 1
                if (weight == 0)
                    weight = 1;

                lowerNeighbors.Add((weight, nx, ny));
                totalWeight += weight;
            }
        }

        if (lowerNeighbors.Count > 0)
        {
            // Distribute quantity proportionally to lower neighbors
            float flowFactor = quantity / totalWeight;

            foreach (var (weight, nx, ny) in lowerNeighbors)
            {
                // Skip if neighbor is ocean
                if (oceanMask[ny, nx])
                    continue;

                // Calculate flow to this neighbor
                float flowQuantity = flowFactor * weight;

                // Accumulate water in neighbor
                watermap[ny, nx] += flowQuantity;

                // Continue recursion if flow significant
                if (flowQuantity > MinFlowQuantity)
                {
                    SimulateDroplet((nx, ny), flowQuantity, heightmap, watermap, oceanMask, width, height);
                }
            }
        }
        else
        {
            // No lower neighbors - accumulate water here (local depression/lake)
            watermap[y, x] += quantity;
        }
    }

    /// <summary>
    /// Samples random land cells for droplet seeding.
    /// Uses uniform random distribution across all land cells.
    /// </summary>
    private static List<(int x, int y)> SampleRandomLandCells(
        bool[,] oceanMask,
        int count,
        int seed)
    {
        int height = oceanMask.GetLength(0);
        int width = oceanMask.GetLength(1);

        // Collect all land cells
        var landCells = new List<(int x, int y)>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!oceanMask[y, x])
                    landCells.Add((x, y));
            }
        }

        // Sample random cells (with replacement - same cell can be selected multiple times)
        var random = new Random(seed);
        var samples = new List<(int x, int y)>(count);

        for (int i = 0; i < count; i++)
        {
            int index = random.Next(landCells.Count);
            samples.Add(landCells[index]);
        }

        return samples;
    }

    /// <summary>
    /// Calculates waterway classification thresholds using percentile-based approach.
    /// Creek (5%), River (2%), Main River (0.7%) thresholds ensure automatic scaling
    /// regardless of map size or precipitation distribution.
    /// </summary>
    private static WatermapThresholds CalculateThresholds(
        float[,] watermap,
        bool[,] oceanMask)
    {
        int height = watermap.GetLength(0);
        int width = watermap.GetLength(1);

        // Collect land-only watermap values
        var landWaterValues = new List<float>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!oceanMask[y, x])
                {
                    float value = watermap[y, x];
                    if (value > 0) // Only consider cells with water flow
                        landWaterValues.Add(value);
                }
            }
        }

        // Sort for percentile calculation
        landWaterValues.Sort();
        landWaterValues.Reverse(); // Highest values first

        // Calculate thresholds as percentiles (top X% of cells)
        float creekThreshold = GetPercentileThreshold(landWaterValues, CreekPercentile);
        float riverThreshold = GetPercentileThreshold(landWaterValues, RiverPercentile);
        float mainRiverThreshold = GetPercentileThreshold(landWaterValues, MainRiverPercentile);

        return new WatermapThresholds(creekThreshold, riverThreshold, mainRiverThreshold);
    }

    /// <summary>
    /// Gets the threshold value at a given percentile (top X%).
    /// For example, percentile 0.05 returns the value where 5% of cells are above it.
    /// </summary>
    private static float GetPercentileThreshold(List<float> sortedValues, float percentile)
    {
        if (sortedValues.Count == 0)
            return 0f;

        int index = (int)(sortedValues.Count * percentile);

        // Clamp to valid range
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));

        return sortedValues[index];
    }
}

/// <summary>
/// Waterway classification thresholds.
/// Cells with watermap values above these thresholds are classified as creeks, rivers, or main rivers.
/// </summary>
public record WatermapThresholds(
    float Creek,      // Top 5% of flow (smallest waterways)
    float River,      // Top 2% of flow (medium waterways)
    float MainRiver); // Top 0.7% of flow (largest waterways)
