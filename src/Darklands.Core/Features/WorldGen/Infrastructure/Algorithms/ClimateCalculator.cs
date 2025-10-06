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
    /// Calculates precipitation using multi-factor model:
    /// - Latitude bands (ITCZ, Hadley cells, temperate fronts)
    /// - Noise variation (breaks up horizontal banding)
    /// - Orographic lift (mountains increase precipitation)
    /// - Rain shadow effect (leeward sides are drier)
    /// - Ocean proximity (moisture source)
    ///
    /// Inspired by WorldEngine's noise approach + realistic meteorology.
    /// </summary>
    /// <param name="heightmap">Elevation data (for orographic/rain shadow effects)</param>
    /// <param name="oceanMask">Ocean mask (moisture source)</param>
    /// <param name="seed">Random seed for noise generation</param>
    /// <returns>Precipitation map (0.0 = arid, 1.0 = very wet)</returns>
    public static float[,] CalculatePrecipitation(float[,] heightmap, bool[,] oceanMask, int seed = 42)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var precipitation = new float[height, width];

        // Initialize simple noise generator (Perlin-like)
        var random = new Random(seed);
        var noiseOffsetX = random.Next(0, 10000);
        var noiseOffsetY = random.Next(0, 10000);

        // Sample cells for detailed tracing (avoid spam - pick 3 representative cells)
        var sampleCells = new List<(int x, int y, string desc)>
        {
            (width / 4, height / 2, "west-equator"),      // Western equatorial region
            (width / 2, height / 4, "center-northern"),   // Center northern (temperate)
            (3 * width / 4, height / 2, "east-equator")   // Eastern equatorial (potential rain shadow)
        };

        // Statistics tracking
        float minPrecip = float.MaxValue, maxPrecip = float.MinValue, sumPrecip = 0f;
        int oceanCount = 0, landCount = 0;

        for (int y = 0; y < height; y++)
        {
            // Latitude: 0 = equator (center), ±1 = poles (edges)
            float latitude = Math.Abs((y / (float)(height - 1)) * 2f - 1f);

            // Base precipitation from latitude bands (ITCZ, Hadley cells, etc.)
            float latitudePercip = CalculateLatitudePrecipitation(latitude);

            for (int x = 0; x < width; x++)
            {
                float elevation = heightmap[y, x];

                // Check if this is a sample cell for detailed tracing
                bool isSample = sampleCells.Exists(s => s.x == x && s.y == y);
                string sampleDesc = isSample ? sampleCells.Find(s => s.x == x && s.y == y).desc : "";

                if (isSample)
                    _logger?.LogDebug("Precipitation trace [{Desc}] ({X},{Y}): elevation={Elev:F3}, latitude={Lat:F3}",
                        sampleDesc, x, y, elevation, latitude);

                // 1. Start with latitude-based precipitation
                float precip = latitudePercip;

                if (isSample)
                    _logger?.LogDebug("  Step 1 - Latitude base: {Value:F3}", precip);

                // 2. Add noise variation (WorldEngine approach - breaks up banding)
                // Use multiple octaves for more organic variation
                float coarseNoise = SimplexNoise(
                    (x + noiseOffsetX) * 0.02f,
                    (y + noiseOffsetY) * 0.02f,
                    seed);

                // Add fine-grain noise to break up straight lines (higher frequency)
                float fineNoise = SimplexNoise(
                    (x + noiseOffsetX) * 0.08f,
                    (y + noiseOffsetY) * 0.08f,
                    seed + 1000); // Different seed for variety

                float precipBeforeNoise = precip;
                precip += coarseNoise * 0.25f; // ±0.25 large-scale variation
                precip += fineNoise * 0.15f;   // ±0.15 fine-scale variation (breaks up boundaries)

                if (isSample)
                    _logger?.LogDebug("  Step 2 - After noise (coarse={Coarse:F3}, fine={Fine:F3}): {Value:F3} (delta={Delta:+F3;-F3})",
                        coarseNoise, fineNoise, precip, precip - precipBeforeNoise);

                // 3. Orographic lift (mountains increase precipitation on windward side)
                float precipBeforeOro = precip;
                float orographicEffect = CalculateOrographicEffect(heightmap, x, y, width, height);
                precip += orographicEffect;

                if (isSample && Math.Abs(orographicEffect) > 0.01f)
                    _logger?.LogDebug("  Step 3 - Orographic lift: +{Effect:F3} -> {Value:F3}",
                        orographicEffect, precip);

                // 4. Rain shadow effect (leeward side is drier)
                float precipBeforeRainShadow = precip;
                float rainShadowEffect = CalculateRainShadowEffect(heightmap, x, y, width, height);
                precip *= rainShadowEffect; // Multiplicative reduction (0.6-1.0)

                if (isSample && rainShadowEffect < 1.0f)
                    _logger?.LogDebug("  Step 4 - Rain shadow: x{Effect:F3} -> {Value:F3} (reduced by {Reduction:P0})",
                        rainShadowEffect, precip, 1.0f - rainShadowEffect);

                // 5. Ocean proximity (moisture source)
                float precipBeforeOcean = precip;
                if (oceanMask[y, x])
                {
                    precip += 0.15f; // Oceans have more moisture
                    if (isSample)
                        _logger?.LogDebug("  Step 5 - Ocean cell: +0.15 -> {Value:F3}", precip);
                }
                else
                {
                    // Coastal cells get bonus (adjacent to ocean)
                    bool isCoastal = IsAdjacentToOcean(oceanMask, x, y, width, height);
                    if (isCoastal)
                    {
                        precip += 0.1f;
                        if (isSample)
                            _logger?.LogDebug("  Step 5 - Coastal cell: +0.10 -> {Value:F3}", precip);
                    }
                }

                float finalPrecip = Math.Clamp(precip, 0f, 1f);
                precipitation[y, x] = finalPrecip;

                if (isSample)
                    _logger?.LogDebug("  Final (clamped): {Value:F3}", finalPrecip);

                // Update statistics
                minPrecip = Math.Min(minPrecip, finalPrecip);
                maxPrecip = Math.Max(maxPrecip, finalPrecip);
                sumPrecip += finalPrecip;
                if (oceanMask[y, x])
                    oceanCount++;
                else
                    landCount++;
            }
        }

        // Log summary statistics (one message per generation)
        int totalCells = width * height;
        float avgPrecip = sumPrecip / totalCells;
        _logger?.LogInformation(
            "Precipitation summary: min={Min:F3}, max={Max:F3}, avg={Avg:F3} (land={Land}, ocean={Ocean})",
            minPrecip, maxPrecip, avgPrecip, landCount, oceanCount);

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
