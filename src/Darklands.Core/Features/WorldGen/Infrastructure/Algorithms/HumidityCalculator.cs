using System;
using System.Collections.Generic;
using System.Linq;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Humidity simulation combining precipitation and irrigation for moisture classification.
/// Ported from WorldEngine's humidity.py (HumiditySimulation class).
///
/// Algorithm overview:
/// - Combine precipitation (direct rainfall) + irrigation (moisture from nearby water)
/// - Irrigation weighted 3× stronger than direct precipitation
/// - Calculate quantiles for 8-level moisture classification (superarid → superhumid)
///
/// Result: Humidity map that considers BOTH rainfall AND proximity to water bodies.
/// This is used for biome classification instead of raw precipitation.
///
/// Reference: References/worldengine/worldengine/simulations/humidity.py
/// </summary>
public static class HumidityCalculator
{
    private const float PrecipitationWeight = 1.0f;
    private const float IrrigationWeight = 3.0f;

    // Quantile percentiles for 8-level moisture classification (WorldEngine defaults)
    // IMPORTANT: Keys map as follows (see References/worldengine/simulations/humidity.py):
    // '12' -> 0.002, '25' -> 0.014, '37' -> 0.073, '50' -> 0.236,
    // '62' -> 0.507, '75' -> 0.778, '87' -> 0.941
    private static readonly float[] QuantilePercentilesAscending = { 0.002f, 0.014f, 0.073f, 0.236f, 0.507f, 0.778f, 0.941f };

    /// <summary>
    /// Calculates humidity by combining precipitation and irrigation with weighted average.
    /// Returns humidity map and quantile thresholds for moisture classification.
    /// </summary>
    /// <param name="precipitationMap">Direct rainfall</param>
    /// <param name="irrigationMap">Moisture from nearby water</param>
    /// <param name="oceanMask">Ocean mask (for land-only quantile calculation)</param>
    /// <returns>Tuple of (humidity map, quantile thresholds)</returns>
    public static (float[,] humidity, HumidityQuantiles quantiles) Execute(
        float[,] precipitationMap,
        float[,] irrigationMap,
        bool[,] oceanMask)
    {
        int height = precipitationMap.GetLength(0);
        int width = precipitationMap.GetLength(1);

        var humidity = new float[height, width];

        // Combine precipitation + irrigation with weighted average
        // NOTE: WorldEngine code (humidity.py:23) has a MINUS sign which appears to be a bug
        // Physical sense: irrigation should ADD moisture, not subtract it
        // Formula: humidity = (precip × 1 + irrigation × 3) / (1 + 3)
        float totalWeight = PrecipitationWeight + IrrigationWeight;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float precip = precipitationMap[y, x];
                float irrigation = irrigationMap[y, x];

                // Weighted combination (normalized by total weight)
                humidity[y, x] = (precip * PrecipitationWeight + irrigation * IrrigationWeight) / totalWeight;
            }
        }

        // Calculate quantile thresholds for land-only cells
        var quantiles = CalculateQuantiles(humidity, oceanMask);

        return (humidity, quantiles);
    }

    /// <summary>
    /// Calculates humidity quantile thresholds for 8-level moisture classification.
    /// Uses bell curve percentiles for better biome distribution.
    /// </summary>
    private static HumidityQuantiles CalculateQuantiles(float[,] humidity, bool[,] oceanMask)
    {
        int height = humidity.GetLength(0);
        int width = humidity.GetLength(1);

        // Collect land-only humidity values
        var landHumidity = new List<float>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!oceanMask[y, x])
                    landHumidity.Add(humidity[y, x]);
            }
        }

        // Sort for quantile calculation
        landHumidity.Sort();
        landHumidity.Reverse(); // Highest values first

        // Calculate quantile thresholds mapping exactly like WorldEngine
        // Keys represent humidity buckets and are NOT direct percent values
        return new HumidityQuantiles(
            Quantile12: GetPercentileThreshold(landHumidity, QuantilePercentilesAscending[0]),  // 0.2% (superhumid threshold)
            Quantile25: GetPercentileThreshold(landHumidity, QuantilePercentilesAscending[1]),  // 1.4%
            Quantile37: GetPercentileThreshold(landHumidity, QuantilePercentilesAscending[2]),  // 7.3%
            Quantile50: GetPercentileThreshold(landHumidity, QuantilePercentilesAscending[3]),  // 23.6%
            Quantile62: GetPercentileThreshold(landHumidity, QuantilePercentilesAscending[4]),  // 50.7%
            Quantile75: GetPercentileThreshold(landHumidity, QuantilePercentilesAscending[5]),  // 77.8%
            Quantile87: GetPercentileThreshold(landHumidity, QuantilePercentilesAscending[6])); // 94.1%
    }

    /// <summary>
    /// Gets the threshold value at a given percentile (top X%).
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
/// Humidity quantile thresholds for 8-level moisture classification.
/// Used by biome classifier to determine moisture levels (superarid → superhumid).
/// </summary>
public record HumidityQuantiles(
    float Quantile12,  // 12th percentile (top 94.1%) - Superhumid threshold
    float Quantile25,  // 25th percentile (top 77.8%) - Perhumid threshold
    float Quantile37,  // 37th percentile (top 50.7%) - Humid threshold
    float Quantile50,  // 50th percentile (top 23.6%) - Subhumid threshold
    float Quantile62,  // 62nd percentile (top 7.3%)  - Semiarid threshold
    float Quantile75,  // 75th percentile (top 1.4%)  - Arid threshold
    float Quantile87); // 87th percentile (top 0.2%)  - Superarid threshold
