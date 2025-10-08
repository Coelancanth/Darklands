using System;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Calculates global temperature distribution using 4-component algorithm.
/// Based on WorldEngine's temperature simulation (temperature.py).
/// </summary>
/// <remarks>
/// VS_025: Multi-stage temperature calculation with debug visibility.
///
/// Algorithm components (WorldEngine-validated):
/// 1. Latitude factor (92% weight) - with axial tilt for planetary variety
/// 2. Coherent noise (8% weight) - climate variation (microclimates)
/// 3. Distance to sun - per-world hot/cold multiplier (inverse-square law)
/// 4. Mountain cooling - elevation-based temperature drop (RAW elevation!)
///
/// Output: 4 intermediate maps for visual debugging:
/// - LatitudeOnlyMap: Pure latitude banding (poles cold, equator hot)
/// - WithNoiseMap: + 8% climate variation (subtle fuzz on bands)
/// - WithDistanceMap: + distance-to-sun (hot/cold planets)
/// - FinalMap: + mountain cooling (mountains blue at all latitudes)
///
/// Pattern: Mirrors ElevationPostProcessor multi-stage approach (VS_024).
/// </remarks>
public static class TemperatureCalculator
{
    /// <summary>
    /// Result containing 4 intermediate temperature maps for debugging.
    /// All maps store normalized [0, 1] values (UI converts to °C via TemperatureMapper).
    /// </summary>
    public record CalculationResult
    {
        /// <summary>
        /// Stage 1: Latitude-only temperature (pure latitude banding with axial tilt).
        /// Visual signature: Horizontal bands, hot zone shifts with tilt.
        /// </summary>
        public float[,] LatitudeOnlyMap { get; init; }

        /// <summary>
        /// Stage 2: Latitude + noise (8% climate variation).
        /// Visual signature: Subtle "fuzz" on bands (not dramatic).
        /// </summary>
        public float[,] WithNoiseMap { get; init; }

        /// <summary>
        /// Stage 3: + Distance to sun (per-world hot/cold multiplier).
        /// Visual signature: Same pattern as Stage 2, but overall hotter/colder.
        /// </summary>
        public float[,] WithDistanceMap { get; init; }

        /// <summary>
        /// Stage 4: FINAL - + Mountain cooling (complete algorithm).
        /// Visual signature: Mountains blue at ALL latitudes (even equator!).
        /// </summary>
        public float[,] FinalMap { get; init; }

        /// <summary>
        /// Per-world axial tilt parameter (shifts equator position).
        /// Range: [-0.5, 0.5], Gaussian-distributed (mean=0, hwhm=0.07).
        /// </summary>
        public float AxialTilt { get; init; }

        /// <summary>
        /// Per-world distance-to-sun parameter (hot vs cold planets).
        /// Range: [0.1, ~1.3], Gaussian-distributed (mean=1.0, hwhm=0.12).
        /// Inverse-square law applied (d² for temperature falloff).
        /// </summary>
        public float DistanceToSun { get; init; }

        public CalculationResult(
            float[,] latitudeOnlyMap,
            float[,] withNoiseMap,
            float[,] withDistanceMap,
            float[,] finalMap,
            float axialTilt,
            float distanceToSun)
        {
            LatitudeOnlyMap = latitudeOnlyMap;
            WithNoiseMap = withNoiseMap;
            WithDistanceMap = withDistanceMap;
            FinalMap = finalMap;
            AxialTilt = axialTilt;
            DistanceToSun = distanceToSun;
        }
    }

    /// <summary>
    /// Calculates temperature maps using WorldEngine's 4-component algorithm.
    /// </summary>
    /// <param name="postProcessedHeightmap">RAW elevation [0.1-20] from post-processing (NOT normalized!)</param>
    /// <param name="mountainLevelThreshold">RAW elevation threshold for mountain cooling (from Thresholds.MountainLevel)</param>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    /// <param name="seed">Seed for noise and per-world parameters</param>
    /// <returns>4 temperature maps + per-world parameters</returns>
    public static CalculationResult Calculate(
        float[,] postProcessedHeightmap,
        float mountainLevelThreshold,
        int width,
        int height,
        int seed)
    {
        // Initialize RNG for per-world parameters and noise base
        var rng = new Random(seed);
        int noiseBase = rng.Next(0, 4096);  // Separate noise seed (WorldEngine pattern)

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Generate per-world parameters (Gaussian-distributed)
        // ═══════════════════════════════════════════════════════════════════════
        // Reference: temperature.py lines 58-67

        // Axial tilt: Shifts equator position (-0.5 to +0.5)
        // HWHM = 0.07 → Most values within ±0.15 range
        float axialTilt = MathUtils.SampleGaussian(rng, mean: 0.0f, hwhm: 0.07f);
        axialTilt = Math.Clamp(axialTilt, -0.5f, 0.5f);  // Cut off Gaussian tails

        // Distance to sun: Hot vs cold planets (0.1 to ~1.3)
        // HWHM = 0.12 → Most values between 0.78 and 1.22 (±22%)
        float distanceToSun = MathUtils.SampleGaussian(rng, mean: 1.0f, hwhm: 0.12f);
        distanceToSun = Math.Max(0.1f, distanceToSun);  // No planets inside star!
        distanceToSun *= distanceToSun;  // Inverse-square law (d²)

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Configure noise generator (8 octaves, freq=128)
        // ═══════════════════════════════════════════════════════════════════════
        // Reference: temperature.py lines 70-72

        int octaves = 8;
        float freq = 16.0f * octaves;  // 128.0
        float n_scale = 1024f / height;  // For 512×512: 2.0

        var noise = new FastNoiseLite(noiseBase);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);  // Fractal Brownian Motion (multi-octave layering)
        noise.SetFractalOctaves(octaves);
        noise.SetFrequency(1.0f / freq);  // freq=128 → frequency=1/128 (matches WorldEngine scaling)

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 3: Allocate 4 output maps
        // ═══════════════════════════════════════════════════════════════════════

        var latitudeOnlyMap = new float[height, width];
        var withNoiseMap = new float[height, width];
        var withDistanceMap = new float[height, width];
        var finalMap = new float[height, width];

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 4: Per-cell temperature calculation (4 stages)
        // ═══════════════════════════════════════════════════════════════════════
        // Reference: temperature.py lines 74-99

        for (int y = 0; y < height; y++)
        {
            // Calculate latitude factor for this row (varies by Y only)
            // y_scaled: [0, height] → [-0.5, 0.5]
            float y_scaled = (float)y / height - 0.5f;

            // Piecewise linear interpolation: cold poles, hot equator (with tilt!)
            // axialTilt shifts the hot zone: tilt=+0.1 moves equator north
            float latitudeFactor = MathUtils.Interp(y_scaled,
                xp: new[] { axialTilt - 0.5f, axialTilt, axialTilt + 0.5f },
                fp: new[] { 0.0f, 1.0f, 0.0f },
                left: 0.0f, right: 0.0f);

            for (int x = 0; x < width; x++)
            {
                // ───────────────────────────────────────────────────────────────
                // Stage 1: Latitude-only (pure banding)
                // ───────────────────────────────────────────────────────────────
                latitudeOnlyMap[y, x] = latitudeFactor;

                // ───────────────────────────────────────────────────────────────
                // Stage 2: + Noise (8% climate variation)
                // ───────────────────────────────────────────────────────────────
                // Get coherent noise [-1, 1]
                // WorldEngine: snoise2((x * n_scale) / freq, (y * n_scale) / freq, octaves)
                // FastNoiseLite: Frequency already set to 1/freq, so we just pass (x * n_scale)
                float n = noise.GetNoise(x * n_scale, y * n_scale);

                // Combine: 92% latitude (12/13), 8% noise (1/13)
                // This creates subtle climate variation (not 50/50 split!)
                float withNoise = (latitudeFactor * 12f + n * 1f) / 13f;
                withNoiseMap[y, x] = withNoise;

                // ───────────────────────────────────────────────────────────────
                // Stage 3: + Distance to sun (hot/cold planets)
                // ───────────────────────────────────────────────────────────────
                // Divide by d² (inverse-square law)
                // distanceToSun > 1.0 → colder planet (farther from star)
                // distanceToSun < 1.0 → hotter planet (closer to star)
                float withDistance = withNoise / distanceToSun;
                withDistanceMap[y, x] = withDistance;

                // ───────────────────────────────────────────────────────────────
                // Stage 4: FINAL - + Mountain cooling
                // ───────────────────────────────────────────────────────────────
                // CRITICAL: Use RAW elevation (not normalized!)
                float rawElevation = postProcessedHeightmap[y, x];
                float altitudeFactor = 1.0f;  // Default: no cooling (lowlands)

                if (rawElevation > mountainLevelThreshold)
                {
                    // Mountain cooling: Linear from base to +29 units above
                    if (rawElevation > mountainLevelThreshold + 29f)
                    {
                        // Extreme peaks: 97% cooling (altitude_factor = 0.033)
                        altitudeFactor = 0.033f;
                    }
                    else
                    {
                        // Linear cooling: base (no cooling) → +29 (max cooling)
                        // altitude_factor: 1.0 → 0.0 over 30 units
                        altitudeFactor = 1.0f - (rawElevation - mountainLevelThreshold) / 30f;
                    }
                }

                // Apply altitude cooling (multiply by factor ≤ 1.0)
                float final = withDistance * altitudeFactor;
                finalMap[y, x] = final;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RETURN: All 4 stages + per-world parameters
        // ═══════════════════════════════════════════════════════════════════════

        return new CalculationResult(
            latitudeOnlyMap: latitudeOnlyMap,
            withNoiseMap: withNoiseMap,
            withDistanceMap: withDistanceMap,
            finalMap: finalMap,
            axialTilt: axialTilt,
            distanceToSun: distanceToSun);
    }
}
