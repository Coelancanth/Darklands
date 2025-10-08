using System;
using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Calculates coastal moisture enhancement using distance-to-ocean BFS + exponential decay.
/// Maritime climates receive significantly more precipitation than continental interiors.
/// </summary>
/// <remarks>
/// VS_028: Coastal moisture enhancement completes atmospheric climate pipeline (Stage 5).
///
/// Algorithm (2-stage):
/// 1. Distance-to-Ocean BFS - Calculate shortest path from each land cell to nearest ocean
/// 2. Exponential Moisture Decay - Apply distance-based enhancement with elevation resistance
///
/// Key Physics Insights:
/// - **Exponential Decay**: Atmospheric moisture drops off exponentially with distance from evaporation source
/// - **30-cell Range**: ~1500km penetration (matches real maritime climate influence)
/// - **80% Coastal Bonus**: Maritime climates (Seattle, UK) ~2× wetter than interior (Spokane, central Asia)
/// - **Elevation Resistance**: Mountain plateaus stay dry despite ocean proximity (Tibetan Plateau effect)
///
/// Real-World Validation:
/// - **West Africa Coast** (wet) vs **Sahara Interior** (dry) - Same latitude, different distance
/// - **Pacific Northwest** (wet) vs **Great Basin** (dry) - Coastal vs continental climate
/// - **UK Maritime** (950mm/year) vs **Central Asia** (200mm/year) - Ocean proximity dominates
///
/// Reference: Atmospheric moisture transport + continentality effect
/// https://en.wikipedia.org/wiki/Continentality
/// </remarks>
public static class CoastalMoistureCalculator
{
    /// <summary>
    /// Result containing rain shadow input + coastal moisture enhanced precipitation.
    /// Both maps store normalized [0, 1] values (UI converts to mm/year).
    /// </summary>
    public record CalculationResult
    {
        /// <summary>
        /// Input: Precipitation after rain shadow (VS_027 Stage 4).
        /// Included for visual comparison in debug views.
        /// </summary>
        public float[,] WithRainShadowMap { get; init; }

        /// <summary>
        /// Output: FINAL precipitation after coastal moisture enhancement (VS_028 Stage 5).
        /// Coastal regions significantly wetter than interior (maritime vs continental effect).
        /// THIS IS THE FINAL PRECIPITATION MAP used by erosion/rivers (VS_029).
        /// </summary>
        public float[,] FinalMap { get; init; }

        public CalculationResult(
            float[,] withRainShadowMap,
            float[,] finalMap)
        {
            WithRainShadowMap = withRainShadowMap;
            FinalMap = finalMap;
        }
    }

    /// <summary>
    /// Calculates coastal moisture enhancement using distance-to-ocean BFS + exponential decay.
    /// </summary>
    /// <param name="rainShadowPrecipitation">Normalized precipitation [0,1] from VS_027 (WithRainShadowPrecipitationMap)</param>
    /// <param name="oceanMask">Ocean mask from BFS flood fill (VS_024, true=water, false=land)</param>
    /// <param name="heightmap">Post-processed heightmap in RAW units [0.1-20] (PostProcessedHeightmap)</param>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <returns>Rain shadow input + final precipitation maps</returns>
    public static CalculationResult Calculate(
        float[,] rainShadowPrecipitation,
        bool[,] oceanMask,
        float[,] heightmap,
        int width,
        int height)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: BFS Distance-to-Ocean Calculation
        // ═══════════════════════════════════════════════════════════════════════
        // Pattern copied from ElevationPostProcessor.FillOcean (VS_024 Phase 1)

        var distanceToOcean = new int[height, width];
        var queue = new Queue<(int x, int y)>();

        // Initialize all cells to "unreached" (max distance)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                distanceToOcean[y, x] = int.MaxValue;
            }
        }

        // Seed queue with all ocean cells (distance = 0)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (oceanMask[y, x])
                {
                    distanceToOcean[y, x] = 0;
                    queue.Enqueue((x, y));
                }
            }
        }

        // BFS propagation: 4-directional neighbors
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            int currentDist = distanceToOcean[y, x];

            // Check all 4 neighbors (N, S, E, W)
            var neighbors = new[]
            {
                (x - 1, y),     // West
                (x + 1, y),     // East
                (x, y - 1),     // North
                (x, y + 1)      // South
            };

            foreach (var (nx, ny) in neighbors)
            {
                // Bounds check
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                // If we found a shorter path, update distance and enqueue
                if (distanceToOcean[ny, nx] > currentDist + 1)
                {
                    distanceToOcean[ny, nx] = currentDist + 1;
                    queue.Enqueue((nx, ny));
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Configuration Constants (Physics-Based)
        // ═══════════════════════════════════════════════════════════════════════

        const float maxCoastalBonus = 0.8f;       // 80% increase at coast (maritime climates ~2× interior)
        const float decayRange = 30f;             // 30 cells ≈ 1500km penetration (realistic atmospheric moisture range)
        const float elevationResistance = 0.02f;  // Mountains resist coastal penetration (linear factor)

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 3: Allocate Output Map
        // ═══════════════════════════════════════════════════════════════════════

        var finalPrecipitation = new float[height, width];

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 4: Per-Cell Coastal Moisture Enhancement
        // ═══════════════════════════════════════════════════════════════════════

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float basePrecip = rainShadowPrecipitation[y, x];

                // Ocean cells: No enhancement (they ARE the moisture source)
                if (oceanMask[y, x])
                {
                    finalPrecipitation[y, x] = basePrecip;
                    continue;
                }

                // Land cells: Apply coastal moisture enhancement
                int dist = distanceToOcean[y, x];

                // Handle landlocked maps (no ocean cells → all distances = int.MaxValue)
                if (dist == int.MaxValue)
                {
                    finalPrecipitation[y, x] = basePrecip;  // No enhancement
                    continue;
                }

                // ───────────────────────────────────────────────────────────────
                // Exponential Moisture Decay: e^(-dist/range)
                // ───────────────────────────────────────────────────────────────
                // Physics: Atmospheric moisture drops off exponentially with distance
                // dist=0 (coast) → bonus=0.8 (80% increase)
                // dist=30 (1500km) → bonus≈0.29 (37% of max, realistic continental drop-off)
                // dist=60 (3000km) → bonus≈0.11 (14% of max, deep interior)

                float coastalBonus = maxCoastalBonus * MathF.Exp(-dist / decayRange);

                // ───────────────────────────────────────────────────────────────
                // Elevation Resistance: High Mountains Block Coastal Penetration
                // ───────────────────────────────────────────────────────────────
                // Physics: Tibetan Plateau (4000m) stays dry despite ~1000km from ocean
                // elevation=1.0 (sea level) → factor=0.98 (nearly full coastal effect)
                // elevation=10.0 (peak) → factor=0.80 (20% reduction, plateau effect)
                // elevation=50.0+ (extreme) → factor=0.0 (capped, no coastal effect)

                float elevation = heightmap[y, x];  // Raw [0.1-20]
                float elevationFactor = 1f - MathF.Min(1f, elevation * elevationResistance);

                // ───────────────────────────────────────────────────────────────
                // Apply Coastal Moisture Enhancement (Additive Bonus)
                // ───────────────────────────────────────────────────────────────
                // ADDITIVE (not replacement): Preserves rain shadow deserts while adding coastal effect
                // Formula: finalPrecip = basePrecip × (1 + coastalBonus × elevationFactor)
                // Example: base=0.5, bonus=0.8, factor=0.98 → final=0.5×(1+0.784)=0.892 (78% increase)

                finalPrecipitation[y, x] = basePrecip * (1f + coastalBonus * elevationFactor);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: Rain shadow input + final precipitation maps
        // ═══════════════════════════════════════════════════════════════════════

        return new CalculationResult(
            withRainShadowMap: rainShadowPrecipitation,
            finalMap: finalPrecipitation);
    }
}
