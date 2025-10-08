using System;
using System.Linq;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Calculates global precipitation distribution using 3-stage algorithm.
/// Based on WorldEngine's precipitation simulation (precipitation.py).
/// </summary>
/// <remarks>
/// VS_026: Multi-stage precipitation calculation with debug visibility.
///
/// Algorithm stages (WorldEngine-validated):
/// 1. Base noise field (6 octaves) - random rainfall patterns
/// 2. Temperature gamma curve - physics-based shaping (cold = less evaporation)
/// 3. Renormalization - stretch to full [0,1] range after shaping
///
/// Output: 3 intermediate maps for visual debugging:
/// - NoiseOnlyMap: Pure coherent noise (no temperature correlation)
/// - TemperatureShapedMap: × gamma curve (tropical wet, polar dry)
/// - FinalMap: Renormalized to [0,1] (full dynamic range restored)
///
/// Pattern: Mirrors TemperatureCalculator multi-stage approach (VS_025).
/// </remarks>
public static class PrecipitationCalculator
{
    /// <summary>
    /// Result containing 3 intermediate precipitation maps for debugging.
    /// All maps store normalized [0, 1] values (UI converts to mm/year via PrecipitationMapper).
    /// </summary>
    public record CalculationResult
    {
        /// <summary>
        /// Stage 1: Base noise field (pure coherent noise, 6 octaves).
        /// Visual signature: Random wet/dry patterns (no temperature correlation).
        /// </summary>
        public float[,] NoiseOnlyMap { get; init; }

        /// <summary>
        /// Stage 2: Temperature-shaped (noise × gamma curve).
        /// Visual signature: Tropical regions wetter, polar regions drier.
        /// Physics: Cold air holds less moisture (quadratic relationship).
        /// </summary>
        public float[,] TemperatureShapedMap { get; init; }

        /// <summary>
        /// Stage 3: FINAL - Renormalized precipitation ([0,1] full range).
        /// Visual signature: Full dynamic range restored after temperature shaping.
        /// </summary>
        public float[,] FinalMap { get; init; }

        /// <summary>
        /// Quantile-based precipitation thresholds for classification.
        /// Adaptive per-world: wet worlds vs dry worlds have different thresholds.
        /// </summary>
        public Application.DTOs.PrecipitationThresholds Thresholds { get; init; }

        public CalculationResult(
            float[,] noiseOnlyMap,
            float[,] temperatureShapedMap,
            float[,] finalMap,
            Application.DTOs.PrecipitationThresholds thresholds)
        {
            NoiseOnlyMap = noiseOnlyMap;
            TemperatureShapedMap = temperatureShapedMap;
            FinalMap = finalMap;
            Thresholds = thresholds;
        }
    }

    /// <summary>
    /// Calculates precipitation maps using WorldEngine's 3-stage algorithm.
    /// </summary>
    /// <param name="temperatureMap">Normalized temperature [0,1] from VS_025 (0=cold, 1=hot)</param>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <param name="seed">Seed for noise generation</param>
    /// <returns>3 precipitation maps + quantile thresholds</returns>
    public static CalculationResult Calculate(
        float[,] temperatureMap,
        int width,
        int height,
        int seed)
    {
        // Initialize RNG for noise
        var rng = new Random(seed);
        int noiseBase = rng.Next(0, 4096);  // Separate noise seed (WorldEngine pattern)

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Configure noise generator (6 octaves, freq=384)
        // ═══════════════════════════════════════════════════════════════════════
        // Reference: WorldEngine precipitation.py lines 45-52

        int octaves = 6;
        float freq = 64.0f * octaves;  // 384.0 (larger-scale patterns than temperature)
        float n_scale = 1024f / height;  // For 512×512: 2.0

        var noise = new FastNoiseLite(noiseBase);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);  // Fractal Brownian Motion
        noise.SetFractalOctaves(octaves);
        noise.SetFrequency(1.0f / freq);  // freq=384 → frequency=1/384

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Allocate 3 output maps
        // ═══════════════════════════════════════════════════════════════════════

        var noiseOnlyMap = new float[height, width];
        var temperatureShapedMap = new float[height, width];
        var finalMap = new float[height, width];

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 3: Per-cell precipitation calculation (3 stages)
        // ═══════════════════════════════════════════════════════════════════════
        // Reference: WorldEngine precipitation.py lines 54-85

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // ───────────────────────────────────────────────────────────────
                // Stage 1: Base noise field (6 octaves coherent noise)
                // ───────────────────────────────────────────────────────────────
                // Get noise [-1, 1] and normalize to [0, 1]
                float n = noise.GetNoise(x * n_scale, y * n_scale);
                float baseNoise = (n + 1.0f) * 0.5f;  // [-1,1] → [0,1]
                noiseOnlyMap[y, x] = baseNoise;

                // ───────────────────────────────────────────────────────────────
                // Stage 2: Temperature gamma curve (physics-based shaping)
                // ───────────────────────────────────────────────────────────────
                // Physics: Cold air holds less moisture (evaporation ∝ temp²)
                // Reference: WorldEngine precipitation.py lines 73-77

                float t = temperatureMap[y, x];  // Normalized [0,1] temperature

                // Gamma curve with minimum bonus
                const float gamma = 2.0f;         // Quadratic relationship (WorldEngine default)
                const float curveBonus = 0.2f;    // Minimum 20% precip at polar regions

                // curve: [0.2, 1.0] for t: [0.0, 1.0]
                float curve = MathF.Pow(t, gamma) * (1.0f - curveBonus) + curveBonus;

                // Apply gamma curve to base noise
                // Arctic (t=0) → curve=0.2 → 20% of base noise
                // Tropical (t=1) → curve=1.0 → 100% of base noise
                float tempShaped = baseNoise * curve;
                temperatureShapedMap[y, x] = tempShaped;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 4: Renormalization (stretch to [0,1] after temperature shaping)
        // ═══════════════════════════════════════════════════════════════════════
        // Temperature shaping compresses dynamic range (most values cluster 0.4-0.6)
        // Renormalization restores full [0,1] range for visualization

        float min = float.MaxValue;
        float max = float.MinValue;

        // Find min/max of temperature-shaped map
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = temperatureShapedMap[y, x];
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        float delta = Math.Max(1e-6f, max - min);  // Prevent division by zero

        // Renormalize to [0, 1]
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = temperatureShapedMap[y, x];
                finalMap[y, x] = (value - min) / delta;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 5: Calculate quantile-based thresholds (adaptive classification)
        // ═══════════════════════════════════════════════════════════════════════

        var thresholds = CalculateThresholds(finalMap);

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: All 3 stages + thresholds
        // ═══════════════════════════════════════════════════════════════════════

        return new CalculationResult(
            noiseOnlyMap: noiseOnlyMap,
            temperatureShapedMap: temperatureShapedMap,
            finalMap: finalMap,
            thresholds: thresholds);
    }

    /// <summary>
    /// Calculates quantile-based precipitation thresholds from final map distribution.
    /// Adapts to each world's climate - wet worlds vs dry worlds have different thresholds.
    /// </summary>
    private static Application.DTOs.PrecipitationThresholds CalculateThresholds(float[,] finalMap)
    {
        int height = finalMap.GetLength(0);
        int width = finalMap.GetLength(1);

        // Collect all precipitation values
        var values = new float[height * width];
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                values[index++] = finalMap[y, x];
            }
        }

        // Sort for quantile calculation
        Array.Sort(values);

        // Calculate 3 quantile thresholds
        float lowThreshold = GetPercentile(values, 0.30f);     // 30th percentile - arid
        float mediumThreshold = GetPercentile(values, 0.70f);  // 70th percentile - moderate
        float highThreshold = GetPercentile(values, 0.95f);    // 95th percentile - wet tropics

        return new Application.DTOs.PrecipitationThresholds(
            lowThreshold: lowThreshold,
            mediumThreshold: mediumThreshold,
            highThreshold: highThreshold);
    }

    /// <summary>
    /// Gets the value at a given percentile from a sorted array.
    /// </summary>
    private static float GetPercentile(float[] sortedValues, float percentile)
    {
        if (sortedValues.Length == 0)
            return 0f;

        int index = (int)MathF.Floor(percentile * (sortedValues.Length - 1));
        index = Math.Clamp(index, 0, sortedValues.Length - 1);

        return sortedValues[index];
    }
}
