using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Climate simulation algorithms (precipitation, temperature).
/// Combines WorldEngine's noise-based approach with elevation-aware precipitation modeling.
/// </summary>
public static class ClimateCalculator
{
    private static ILogger? _logger;

    /// <summary>
    /// Sets the logger for algorithm tracing (optional - for debugging climate calculations).
    /// </summary>
    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }
    /// <summary>
    /// Calculates precipitation using WorldEngine-like approach:
    /// - Base multi-octave noise field normalized per world
    /// - Temperature gamma curve modulation: (t^gamma)*(1-curveOffset)+curveOffset
    /// - Optional orographic/rain-shadow and coastal bonuses
    /// </summary>
    /// <param name="heightmap">Elevation data (for orographic/rain shadow effects)</param>
    /// <param name="oceanMask">Ocean mask (moisture source)</param>
    /// <param name="temperatureMap">Computed temperature map (0..1), must be available BEFORE precipitation</param>
    /// <param name="seed">Random seed</param>
    /// <param name="gammaCurve">Temperature-precip coupling (WorldEngine default 1.25)</param>
    /// <param name="curveOffset">Minimum precipitation fraction (WorldEngine default 0.20)</param>
    public static float[,] CalculatePrecipitation(
        float[,] heightmap,
        bool[,] oceanMask,
        float[,] temperatureMap,
        int seed = 42,
        float gammaCurve = 1.25f,
        float curveOffset = 0.20f)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var noiseValues = new float[height, width];

        var rng = new Random(seed);
        int baseX = rng.Next(0, 4096);
        int baseY = rng.Next(0, 4096);

        // WorldEngine uses 6 octaves; our SimplexNoise wrapper already sums gradients per call
        float coarseFreq = 64.0f;       // effective frequency scaling
        float nScale = 1024f / Math.Max(1f, height); // preserve patterns across sizes

        // Generate wrap-aware noise on X edges (approximation)
        int border = Math.Max(1, width / 4);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x * nScale) / coarseFreq;
                float ny = (y * nScale) / coarseFreq;

                float n = SimplexNoise(nx + baseX, ny + baseY, seed);

                if (x <= border)
                {
                    // Blend with wrapped sample from the right edge to avoid seams
                    float nRight = SimplexNoise(((x * nScale) + width) / coarseFreq + baseX, ny + baseY, seed);
                    float t = x / (float)border;
                    n = n * t + nRight * (1f - t);
                }

                noiseValues[y, x] = n;
            }
        }

        // Normalize noise to [0,1]
        float minN = float.MaxValue, maxN = float.MinValue;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float v = noiseValues[y, x];
                if (v < minN) minN = v;
                if (v > maxN) maxN = v;
            }
        float deltaN = Math.Max(1e-6f, maxN - minN);

        // Normalize temperature to [0,1]
        float minT = float.MaxValue, maxT = float.MinValue;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float t = temperatureMap[y, x];
                if (t < minT) minT = t;
                if (t > maxT) maxT = t;
            }
        float deltaT = Math.Max(1e-6f, maxT - minT);

        var precipitation = new float[height, width];

        float minP = float.MaxValue, maxP = float.MinValue, sumP = 0f;
        int oceanCells = 0, landCells = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float p = (noiseValues[y, x] - minN) / deltaN; // [0,1]
                float tNorm = (temperatureMap[y, x] - minT) / deltaT; // [0,1]

                // Temperature gamma curve
                float curve = (float)(Math.Pow(tNorm, gammaCurve) * (1f - curveOffset) + curveOffset);
                p *= curve;

                // Orographic lift and rain shadow
                p += CalculateOrographicEffect(heightmap, x, y, width, height);
                p *= CalculateRainShadowEffect(heightmap, x, y, width, height);

                // Ocean/coastal bonuses
                if (oceanMask[y, x])
                {
                    p += 0.15f;
                    oceanCells++;
                }
                else
                {
                    bool isCoastal = IsAdjacentToOcean(oceanMask, x, y, width, height);
                    if (isCoastal) p += 0.10f;
                    landCells++;
                }

                // Clamp to [0,1]
                float pc = Math.Clamp(p, 0f, 1f);
                precipitation[y, x] = pc;
                if (pc < minP) minP = pc;
                if (pc > maxP) maxP = pc;
                sumP += pc;
            }
        }

        int total = width * height;
        _logger?.LogInformation(
            "Precipitation summary: min={Min:F3}, max={Max:F3}, avg={Avg:F3} (land={Land}, ocean={Ocean})",
            minP, maxP, sumP / Math.Max(1, total), landCells, oceanCells);

        return precipitation;
    }

    /// <summary>
    /// Calculates base precipitation from latitude bands.
    /// Models ITCZ (wet equator), Hadley cells (dry subtropics), temperate fronts, polar regions.
    /// </summary>
    private static float CalculateLatitudePrecipitation(float latitude)
    {
        // Latitude bands (simplified Earth-like pattern):
        // - Equator (0.0-0.15): ITCZ, very wet (0.7-0.9)
        // - Subtropics (0.3-0.5): Hadley cell descent, dry (0.2-0.3)
        // - Temperate (0.5-0.7): Frontal systems, wet (0.5-0.7)
        // - Polar (0.7-1.0): Cold, dry (0.3-0.4)

        if (latitude < 0.15f)
            return 0.8f; // Equatorial (ITCZ)
        else if (latitude < 0.35f)
            return Lerp(0.8f, 0.25f, (latitude - 0.15f) / 0.2f); // Transition to subtropical
        else if (latitude < 0.5f)
            return 0.25f; // Subtropical high (dry belt)
        else if (latitude < 0.7f)
            return Lerp(0.25f, 0.6f, (latitude - 0.5f) / 0.2f); // Temperate (wet)
        else
            return Lerp(0.6f, 0.35f, (latitude - 0.7f) / 0.3f); // Polar (dry)
    }

    /// <summary>
    /// Calculates orographic lift effect (mountains force air upward, causing precipitation).
    /// Higher elevations and steep slopes get more rain on windward (western) side.
    /// </summary>
    private static float CalculateOrographicEffect(float[,] heightmap, int x, int y, int width, int height)
    {
        float elevation = heightmap[y, x];

        // Base orographic effect: higher elevation → more precipitation (to a point)
        // Peaks: 0.0-0.3 (lowlands) → 0 bonus
        // Peaks: 0.3-0.7 (highlands) → +0.1 to +0.2 bonus
        // Peaks: 0.7-1.0 (mountains) → +0.2 bonus (plateaus out - too high is dry)
        float elevationBonus = 0f;
        if (elevation > 0.3f && elevation < 0.8f)
        {
            elevationBonus = (elevation - 0.3f) * 0.4f; // Max +0.2 at elevation 0.8
        }
        else if (elevation >= 0.8f)
        {
            elevationBonus = 0.2f; // Plateau at very high elevations
        }

        // Slope effect (steeper western slopes get more rain - prevailing westerlies)
        float slopeBonus = 0f;
        if (x > 0)
        {
            float westNeighbor = heightmap[y, x - 1];
            float eastSlope = elevation - westNeighbor; // Positive = rising to east (windward slope)

            if (eastSlope > 0.1f) // Significant upward slope
            {
                slopeBonus = Math.Min(eastSlope * 0.5f, 0.15f); // Up to +0.15 for steep slopes
            }
        }

        return elevationBonus + slopeBonus;
    }

    /// <summary>
    /// Calculates rain shadow effect (leeward side of mountains is drier).
    /// Assumes prevailing westerly winds (west → east).
    /// Returns multiplicative factor (0.6 = 40% reduction, 1.0 = no effect).
    /// </summary>
    private static float CalculateRainShadowEffect(float[,] heightmap, int x, int y, int width, int height)
    {
        // Check if there's a mountain range to the west (upwind)
        // If so, this cell is in rain shadow (leeward side)

        const int scanDistance = 5; // Look 5 cells west for blocking mountains
        float currentElevation = heightmap[y, x];

        bool isInRainShadow = false;
        float maxBlockingHeight = 0f;

        for (int dx = 1; dx <= scanDistance && (x - dx) >= 0; dx++)
        {
            float westElevation = heightmap[y, x - dx];

            // If western neighbor is significantly higher, it blocks moisture
            if (westElevation > currentElevation + 0.2f)
            {
                isInRainShadow = true;
                maxBlockingHeight = Math.Max(maxBlockingHeight, westElevation - currentElevation);
            }
        }

        if (isInRainShadow)
        {
            // Stronger rain shadow for higher blocking mountains
            // Reduction: 0.6 (40% less rain) to 0.8 (20% less rain)
            float reduction = Math.Max(0.6f, 1.0f - maxBlockingHeight * 0.5f);
            return reduction;
        }

        return 1.0f; // No rain shadow effect
    }

    /// <summary>
    /// Checks if a land cell is adjacent to ocean (coastal moisture source).
    /// </summary>
    private static bool IsAdjacentToOcean(bool[,] oceanMask, int x, int y, int width, int height)
    {
        // Check 8 neighbors
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue; // Skip self

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (oceanMask[ny, nx])
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Simple 2D Simplex-like noise for precipitation variation.
    /// Breaks up horizontal banding by adding local variation.
    /// Returns value in range [-1, 1].
    /// </summary>
    private static float SimplexNoise(float x, float y, int seed)
    {
        // Simple gradient noise implementation (faster than full Simplex)
        // Based on improved Perlin noise concepts

        int xi = (int)Math.Floor(x);
        int yi = (int)Math.Floor(y);

        float xf = x - xi;
        float yf = y - yi;

        // Hash grid corners
        float n00 = GradientNoise(xi, yi, xf, yf, seed);
        float n10 = GradientNoise(xi + 1, yi, xf - 1, yf, seed);
        float n01 = GradientNoise(xi, yi + 1, xf, yf - 1, seed);
        float n11 = GradientNoise(xi + 1, yi + 1, xf - 1, yf - 1, seed);

        // Smoothstep interpolation
        float u = Smoothstep(xf);
        float v = Smoothstep(yf);

        // Bilinear interpolation
        float nx0 = Lerp(n00, n10, u);
        float nx1 = Lerp(n01, n11, u);

        return Lerp(nx0, nx1, v);
    }

    /// <summary>
    /// Gradient noise helper (dot product with random gradient).
    /// </summary>
    private static float GradientNoise(int ix, int iy, float fx, float fy, int seed)
    {
        // Hash to get gradient index
        int hash = Hash(ix, iy, seed);

        // Use hash to select gradient vector (8 directions)
        float gx = ((hash & 4) != 0) ? -1f : 1f; // Left or right
        float gy = ((hash & 2) != 0) ? -1f : 1f; // Up or down

        // Normalize to unit circle (approximate)
        if ((hash & 1) != 0)
        {
            // Diagonal
            float norm = 1f / (float)Math.Sqrt(2);
            gx *= norm;
            gy *= norm;
        }
        else
        {
            // Axis-aligned
            if ((hash & 8) != 0)
                gx = 0;
            else
                gy = 0;
        }

        // Dot product with distance vector
        return gx * fx + gy * fy;
    }

    /// <summary>
    /// Simple hash function for noise generation.
    /// </summary>
    private static int Hash(int x, int y, int seed)
    {
        // Mix coordinates with seed using prime number multipliers
        int hash = seed;
        hash = hash * 374761393 + x * 668265263; // Large primes
        hash = hash * 1274126177 + y * 1301081;
        hash = hash * 1911520717;

        return hash & 0x7FFFFFFF; // Ensure positive
    }

    /// <summary>
    /// Smoothstep function for smooth interpolation (cubic Hermite).
    /// </summary>
    private static float Smoothstep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Calculates temperature based on latitude and elevation.
    /// Uses WorldEngine-compatible thresholds for 6-band temperature classification.
    /// Adds noise variation to prevent perfectly horizontal temperature bands.
    /// Reference: WorldEngine temps=[0.874, 0.765, 0.594, 0.439, 0.366, 0.124]
    /// </summary>
    /// <param name="heightmap">Elevation data (for elevation cooling)</param>
    /// <param name="oceanMask">Ocean mask (oceans moderate temperature)</param>
    /// <param name="seed">Random seed for temperature noise variation</param>
    /// <returns>Temperature map (0.0 = coldest, 1.0 = hottest)</returns>
    public static float[,] CalculateTemperature(float[,] heightmap, bool[,] oceanMask, int seed = 42)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var temperature = new float[height, width];

        // Initialize noise offsets for temperature variation
        var random = new Random(seed + 5000); // Different seed from precipitation
        var noiseOffsetX = random.Next(0, 10000);
        var noiseOffsetY = random.Next(0, 10000);

        // Sample cells for detailed tracing (same positions as precipitation)
        var sampleCells = new List<(int x, int y, string desc)>
        {
            (width / 4, height / 2, "west-equator"),
            (width / 2, height / 4, "center-northern"),
            (3 * width / 4, height / 2, "east-equator")
        };

        // Statistics tracking
        float minTemp = float.MaxValue, maxTemp = float.MinValue, sumTemp = 0f;

        for (int y = 0; y < height; y++)
        {
            // Latitude: 0 = equator (center), ±1 = poles (edges)
            float latitude = Math.Abs((y / (float)(height - 1)) * 2f - 1f);

            // Base temperature from latitude (cosine curve for realistic gradient)
            float baseTemp = (float)Math.Cos(latitude * Math.PI / 2); // 1.0 at equator, 0.0 at poles

            for (int x = 0; x < width; x++)
            {
                // Check if this is a sample cell for detailed tracing
                bool isSample = sampleCells.Exists(s => s.x == x && s.y == y);
                string sampleDesc = isSample ? sampleCells.Find(s => s.x == x && s.y == y).desc : "";

                if (isSample)
                    _logger?.LogDebug("Temperature trace [{Desc}] ({X},{Y}): elevation={Elev:F3}, latitude={Lat:F3}",
                        sampleDesc, x, y, heightmap[y, x], latitude);

                float temp = baseTemp;

                if (isSample)
                    _logger?.LogDebug("  Step 1 - Latitude base (cosine): {Value:F3}", temp);

                // Add temperature variation noise (breaks up horizontal bands)
                // Use fine-grain noise to create micro-climate variation
                float tempNoise = SimplexNoise(
                    (x + noiseOffsetX) * 0.05f,
                    (y + noiseOffsetY) * 0.05f,
                    seed + 5000);
                float tempBeforeNoise = temp;
                temp += tempNoise * 0.08f; // ±0.08 temperature variation (enough to cross biome boundaries)

                if (isSample)
                    _logger?.LogDebug("  Step 2 - After noise: {Value:F3} (delta={Delta:+F3;-F3})",
                        temp, temp - tempBeforeNoise);

                // Elevation cooling: FIXED from 0.5× to 0.25× (was making highlands too cold)
                // This allows temperate highlands to support forests instead of just ice/tundra
                float tempBeforeCooling = temp;
                float elevationCooling = heightmap[y, x] * 0.25f;
                temp = Math.Max(0f, temp - elevationCooling);

                if (isSample && elevationCooling > 0.01f)
                    _logger?.LogDebug("  Step 3 - Elevation cooling: -{Cooling:F3} -> {Value:F3}",
                        elevationCooling, temp);

                // Oceans moderate temperature (reduce extremes)
                if (oceanMask[y, x])
                {
                    float tempBeforeModeration = temp;
                    temp = Lerp(temp, 0.5f, 0.2f); // Pull towards moderate temp
                    if (isSample)
                        _logger?.LogDebug("  Step 4 - Ocean moderation: {Before:F3} -> {After:F3}",
                            tempBeforeModeration, temp);
                }

                float finalTemp = Math.Clamp(temp, 0f, 1f);
                temperature[y, x] = finalTemp;

                if (isSample)
                    _logger?.LogDebug("  Final (clamped): {Value:F3}", finalTemp);

                // Update statistics
                minTemp = Math.Min(minTemp, finalTemp);
                maxTemp = Math.Max(maxTemp, finalTemp);
                sumTemp += finalTemp;
            }
        }

        // Log summary statistics
        int totalCells = width * height;
        float avgTemp = sumTemp / totalCells;
        _logger?.LogInformation(
            "Temperature summary: min={Min:F3}, max={Max:F3}, avg={Avg:F3}",
            minTemp, maxTemp, avgTemp);

        return temperature;
    }

    private static float Lerp(float a, float b, float t) => a + t * (b - a);
}
