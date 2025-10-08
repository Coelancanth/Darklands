using System;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Calculates rain shadow effect on precipitation using latitude-based prevailing winds + orographic blocking.
/// Mountains upwind block moisture, creating dry leeward zones (deserts).
/// </summary>
/// <remarks>
/// VS_027: Latitude-dependent rain shadow for realistic desert formation.
///
/// Algorithm (2-stage):
/// 1. Base Precipitation (from VS_026) - raw noise × temperature curve
/// 2. Rain Shadow Effect (THIS) - orographic blocking based on upwind mountains
///
/// Key Insights:
/// - **Latitude-based winds**: Each latitude uses appropriate wind direction (Polar Easterlies / Westerlies / Trade Winds)
/// - **Upwind trace**: Look backwards against wind (max 20 cells ≈ 1000km atmospheric moisture transport)
/// - **Accumulative blocking**: Each upwind mountain adds 5% precipitation reduction (max 80% total)
/// - **Directional effect**: Only UPWIND mountains matter (leeward dry, windward unaffected)
///
/// Real-World Examples:
/// - **Sahara Desert** (20°N): Trade Winds (westward) + Atlas Mountains → dry east side
/// - **Gobi Desert** (45°N): Westerlies (eastward) + Himalayas → dry west side
/// - **Atacama Desert** (23°S): Trade Winds (westward) + Andes → driest place on Earth
///
/// Reference: Earth's atmospheric circulation + orographic precipitation
/// https://en.wikipedia.org/wiki/Rain_shadow
/// </remarks>
public static class RainShadowCalculator
{
    /// <summary>
    /// Result containing base precipitation + rain shadow modified precipitation.
    /// Both maps store normalized [0, 1] values (UI converts to mm/year).
    /// </summary>
    public record CalculationResult
    {
        /// <summary>
        /// Input: Base precipitation from VS_026 (before rain shadow).
        /// Included for visual comparison in debug views.
        /// </summary>
        public float[,] BasePrecipitationMap { get; init; }

        /// <summary>
        /// Output: Precipitation AFTER rain shadow effect (orographic blocking applied).
        /// Leeward side of mountains has reduced precipitation (deserts).
        /// Windward side unchanged (moisture increase deferred to VS_028 coastal enhancement).
        /// </summary>
        public float[,] WithRainShadowMap { get; init; }

        public CalculationResult(
            float[,] basePrecipitationMap,
            float[,] withRainShadowMap)
        {
            BasePrecipitationMap = basePrecipitationMap;
            WithRainShadowMap = withRainShadowMap;
        }
    }

    /// <summary>
    /// Calculates rain shadow effect on precipitation using latitude-based prevailing winds.
    /// </summary>
    /// <param name="basePrecipitation">Normalized precipitation [0,1] from VS_026 (FinalPrecipitationMap)</param>
    /// <param name="elevation">Post-processed heightmap in RAW units [0.1-20] (PostProcessedHeightmap)</param>
    /// <param name="seaLevel">Sea level threshold in RAW units (from ElevationThresholds)</param>
    /// <param name="maxElevation">Maximum elevation in RAW units (from WorldGenerationResult)</param>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <returns>Base + rain shadow modified precipitation maps</returns>
    public static CalculationResult Calculate(
        float[,] basePrecipitation,
        float[,] elevation,
        float seaLevel,
        float maxElevation,
        int width,
        int height)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Calculate dynamic elevation threshold (adapts to world terrain)
        // ═══════════════════════════════════════════════════════════════════════
        // 5% of land elevation range = "significant mountain barrier"
        // Flat world (range 2.0): threshold = 0.1 (small barriers matter)
        // Mountainous world (range 10.0): threshold = 0.5 (only major peaks block)

        float elevationDeltaThreshold = (maxElevation - seaLevel) * 0.05f;

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Configuration constants
        // ═══════════════════════════════════════════════════════════════════════

        const int maxUpwindDistance = 20;        // Max cells to trace upwind (~1000km at 512×512 world)
        const float blockingPerCell = 0.05f;     // 5% reduction per upwind mountain cell
        const float minRainfallFactor = 0.20f;   // Min 20% rainfall even in worst deserts (cap at 80% reduction)

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 3: Allocate output map
        // ═══════════════════════════════════════════════════════════════════════

        var withRainShadow = new float[height, width];

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 4: Per-cell rain shadow calculation (latitude-based upwind trace)
        // ═══════════════════════════════════════════════════════════════════════

        for (int y = 0; y < height; y++)
        {
            // ───────────────────────────────────────────────────────────────────
            // Get prevailing wind direction for this latitude (KEY: per-row!)
            // ───────────────────────────────────────────────────────────────────
            // Handle edge case: height=1 → treat as equator (0.5 normalized latitude)
            float normalizedLatitude = height > 1 ? (float)y / (height - 1) : 0.5f;
            var (windX, windY) = PrevailingWinds.GetWindDirection(normalizedLatitude);

            for (int x = 0; x < width; x++)
            {
                float currentElevation = elevation[y, x];
                float mountainBlocking = 0f;

                // ───────────────────────────────────────────────────────────────
                // Trace UPWIND (direction varies by latitude!)
                // ───────────────────────────────────────────────────────────────
                // Wind direction: +1 = eastward (trace WEST to find upwind), -1 = westward (trace EAST to find upwind)
                // Example: Westerlies (windX = +1) → Trace westward (x - 1, x - 2, ...) to find upwind mountains (moisture source)

                for (int step = 1; step <= maxUpwindDistance; step++)
                {
                    // Calculate upwind position (trace OPPOSITE to wind direction to find moisture source)
                    // windX = +1 (eastward wind) → upwindX = x - step (look WEST for upwind mountains blocking moisture)
                    // windX = -1 (westward wind) → upwindX = x + step (look EAST for upwind mountains blocking moisture)
                    int upwindX = x - (int)(windX * step);
                    int upwindY = y;  // Horizontal-only trace (windY = 0 always)

                    // Boundary check (stop if we hit map edge)
                    if (upwindX < 0 || upwindX >= width)
                        break;

                    float upwindElevation = elevation[upwindY, upwindX];

                    // ───────────────────────────────────────────────────────────
                    // Check for significant elevation barrier (mountain upwind)
                    // ───────────────────────────────────────────────────────────
                    // Only mountains HIGHER than current position block moisture
                    // Threshold ensures hills don't block (only major elevation jumps)

                    if (upwindElevation > currentElevation + elevationDeltaThreshold)
                    {
                        mountainBlocking += blockingPerCell;
                    }
                }

                // ───────────────────────────────────────────────────────────────
                // Apply rain shadow effect (max 80% reduction)
                // ───────────────────────────────────────────────────────────────
                // mountainBlocking ranges [0, 1.0+] (uncapped, can exceed 1.0 if many mountains)
                // rainShadowFactor = 1 - blocking, clamped to [minRainfallFactor, 1.0]
                // Final rainfall = base × rainShadowFactor
                //
                // Examples:
                // - No upwind mountains: blocking = 0 → factor = 1.0 → 100% rainfall (unchanged)
                // - 5 upwind mountains: blocking = 0.25 → factor = 0.75 → 75% rainfall
                // - 16+ upwind mountains: blocking = 0.80+ → factor = 0.2 → 20% rainfall (desert)

                float rainShadowFactor = MathF.Max(minRainfallFactor, 1f - mountainBlocking);
                withRainShadow[y, x] = basePrecipitation[y, x] * rainShadowFactor;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: Base + modified precipitation maps
        // ═══════════════════════════════════════════════════════════════════════

        return new CalculationResult(
            basePrecipitationMap: basePrecipitation,
            withRainShadowMap: withRainShadow);
    }
}
