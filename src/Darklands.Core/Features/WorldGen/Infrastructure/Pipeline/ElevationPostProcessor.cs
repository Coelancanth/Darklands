using System;
using System.Collections.Generic;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline;

/// <summary>
/// Elevation post-processing algorithms ported from WorldEngine (basic.py).
/// Applies 4 sequential transformations to raw heightmap from native plate tectonics simulation.
/// </summary>
/// <remarks>
/// Algorithms (VS_024 Stage 1):
/// 1. add_noise_to_elevation() - Add Perlin/Simplex noise variation (~10% amplitude)
/// 2. fill_ocean() - BFS flood fill from borders to detect connected ocean regions
/// 3. harmonize_ocean() - Smooth ocean floor to reduce jaggedness
/// 4. sea_depth() - Calculate normalized depth map for ocean cells
///
/// SKIPPED (not needed per VS_024 decision):
/// - center_land() - Shifts continents away from borders
/// - place_oceans_at_map_borders() - Lowers border elevation
/// </remarks>
public static class ElevationPostProcessor
{
    /// <summary>
    /// Result of elevation post-processing containing processed heightmap and derived data.
    /// </summary>
    public record PostProcessingResult
    {
        /// <summary>
        /// Heightmap after 4 WorldEngine algorithms (still in raw [0-20] range, NOT normalized).
        /// </summary>
        public float[,] ProcessedHeightmap { get; init; }

        /// <summary>
        /// Ocean mask from BFS flood fill (true = water, false = land).
        /// NOT a simple threshold! Only connected regions from borders.
        /// </summary>
        public bool[,] OceanMask { get; init; }

        /// <summary>
        /// Normalized depth map [0, 1] for ocean cells (0 for land).
        /// </summary>
        public float[,] SeaDepth { get; init; }

        public PostProcessingResult(
            float[,] processedHeightmap,
            bool[,] oceanMask,
            float[,] seaDepth)
        {
            ProcessedHeightmap = processedHeightmap;
            OceanMask = oceanMask;
            SeaDepth = seaDepth;
        }
    }

    /// <summary>
    /// Executes all 4 elevation post-processing algorithms on a COPY of the original heightmap.
    /// </summary>
    /// <param name="originalHeightmap">Raw heightmap from native sim (preserved, not modified)</param>
    /// <param name="seaLevel">Sea level threshold (typically 1.0 for plate tectonics library)</param>
    /// <param name="seed">Seed for noise generation (should match world seed)</param>
    /// <param name="addNoise">DIAGNOSTIC: Set false to skip noise addition (isolate noise as sink source)</param>
    /// <returns>Post-processed heightmap, ocean mask, and sea depth map</returns>
    public static PostProcessingResult Process(
        float[,] originalHeightmap,
        float seaLevel,
        int seed,
        bool addNoise = true)
    {
        int height = originalHeightmap.GetLength(0);
        int width = originalHeightmap.GetLength(1);

        // Clone to preserve original (SACRED!)
        var heightmap = (float[,])originalHeightmap.Clone();

        // Algorithm 1: Add coherent noise variation
        if (addNoise)
        {
            AddNoiseToElevation(heightmap, seed);
        }

        // Algorithm 2: Flood-fill ocean detection
        var oceanMask = FillOcean(heightmap, seaLevel);

        // Algorithm 3: Smooth ocean floor
        HarmonizeOcean(heightmap, oceanMask);

        // Algorithm 4: Calculate ocean depth
        var seaDepth = CalculateSeaDepth(heightmap, oceanMask, seaLevel);

        return new PostProcessingResult(heightmap, oceanMask, seaDepth);
    }

    /// <summary>
    /// Algorithm 0: Apply Gaussian blur to remove high-frequency noise from native plate simulation.
    /// Uses 2D Gaussian kernel convolution with configurable sigma (standard deviation).
    /// </summary>
    /// <param name="heightmap">Heightmap to smooth (modified in-place)</param>
    /// <param name="sigma">Standard deviation of Gaussian (controls smoothing strength: 1.0=light, 2.0=moderate, 3.0=aggressive)</param>
    /// <remarks>
    /// Purpose: Native plate simulation produces high-frequency noise (micro-pits causing 3%+ sinks).
    /// Gaussian blur removes these artifacts while preserving large-scale terrain features (mountains, valleys).
    /// 3-sigma rule: Kernel radius = ceil(3*sigma) captures 99.7% of Gaussian distribution.
    /// </remarks>
    public static void ApplyGaussianBlur(float[,] heightmap, float sigma = 1.5f)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Calculate kernel size (3-sigma rule: 99.7% of distribution)
        int radius = (int)Math.Ceiling(3 * sigma);
        int kernelSize = 2 * radius + 1;

        // Generate Gaussian kernel
        float[,] kernel = new float[kernelSize, kernelSize];
        float kernelSum = 0f;

        for (int ky = 0; ky < kernelSize; ky++)
        {
            for (int kx = 0; kx < kernelSize; kx++)
            {
                int dy = ky - radius;
                int dx = kx - radius;
                float value = (float)Math.Exp(-(dx * dx + dy * dy) / (2 * sigma * sigma));
                kernel[ky, kx] = value;
                kernelSum += value;
            }
        }

        // Normalize kernel (ensures weighted average preserves elevation mass)
        for (int ky = 0; ky < kernelSize; ky++)
        {
            for (int kx = 0; kx < kernelSize; kx++)
            {
                kernel[ky, kx] /= kernelSum;
            }
        }

        // Apply convolution (create smoothed copy to avoid reading modified values)
        var smoothed = new float[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float result = 0f;

                // Convolve with Gaussian kernel
                for (int ky = 0; ky < kernelSize; ky++)
                {
                    for (int kx = 0; kx < kernelSize; kx++)
                    {
                        // Calculate source coordinates with border clamping
                        int sy = y + (ky - radius);
                        int sx = x + (kx - radius);

                        // Clamp to valid range (mirror/repeat borders)
                        sy = Math.Max(0, Math.Min(height - 1, sy));
                        sx = Math.Max(0, Math.Min(width - 1, sx));

                        result += heightmap[sy, sx] * kernel[ky, kx];
                    }
                }

                smoothed[y, x] = result;
            }
        }

        // Copy smoothed result back to original array
        Array.Copy(smoothed, heightmap, heightmap.Length);
    }

    /// <summary>
    /// Algorithm 1: Add coherent Perlin/Simplex noise to elevation (~10% amplitude).
    /// Prevents monotone terrain, adds natural variation.
    /// </summary>
    private static void AddNoiseToElevation(float[,] heightmap, int seed)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Find elevation range for amplitude calculation
        float min = float.MaxValue;
        float max = float.MinValue;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float v = heightmap[y, x];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        float range = max - min;
        // NOTE: WorldEngine doesn't scale by amplitude - adds raw noise value directly!
        // float noiseAmplitude = range * 0.1f;  // Our original approach (wrong!)

        // Configure FastNoiseLite to match WorldEngine (generation.py:74-80)
        // octaves = 8, freq = 16.0 * octaves = 128.0
        var noise = new FastNoiseLite(seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);  // Fractal Brownian Motion (multi-octave)
        noise.SetFractalOctaves(8);  // Match WorldEngine
        noise.SetFrequency(1.0f / 128.0f);  // freq = 128.0 → frequency = 1/128

        // Add noise to elevation (WorldEngine adds raw noise value, not scaled!)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // WorldEngine: n = snoise2(x / freq * 2, y / freq * 2, octaves, base=seed)
                // FastNoiseLite handles frequency internally, we just need to scale coords by 2
                float noiseValue = noise.GetNoise(x * 2.0f, y * 2.0f);  // Returns [-1, 1]
                heightmap[y, x] += noiseValue;  // Add directly (WorldEngine style)
            }
        }
    }

    /// <summary>
    /// Algorithm 2: BFS flood fill from borders to detect connected ocean regions.
    /// Ensures only cells connected to border oceans are marked as ocean (prevents landlocked seas).
    /// </summary>
    private static bool[,] FillOcean(float[,] heightmap, float seaLevel)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var oceanMask = new bool[height, width];
        var visited = new bool[height, width];
        var queue = new Queue<(int x, int y)>();

        // Seed queue with border cells below sea level
        // Top and bottom borders
        for (int x = 0; x < width; x++)
        {
            if (heightmap[0, x] <= seaLevel)
                queue.Enqueue((x, 0));

            if (heightmap[height - 1, x] <= seaLevel)
                queue.Enqueue((x, height - 1));
        }

        // Left and right borders
        for (int y = 0; y < height; y++)
        {
            if (heightmap[y, 0] <= seaLevel)
                queue.Enqueue((0, y));

            if (heightmap[y, width - 1] <= seaLevel)
                queue.Enqueue((width - 1, y));
        }

        // BFS to flood-fill ocean
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            if (visited[y, x]) continue;
            visited[y, x] = true;

            if (heightmap[y, x] <= seaLevel)
            {
                oceanMask[y, x] = true;

                // Add 4-connected neighbors
                if (x > 0 && !visited[y, x - 1])
                    queue.Enqueue((x - 1, y));

                if (x < width - 1 && !visited[y, x + 1])
                    queue.Enqueue((x + 1, y));

                if (y > 0 && !visited[y - 1, x])
                    queue.Enqueue((x, y - 1));

                if (y < height - 1 && !visited[y + 1, x])
                    queue.Enqueue((x, y + 1));
            }
        }

        return oceanMask;
    }

    /// <summary>
    /// Algorithm 3: Smooth ocean floor to reduce jaggedness.
    /// Averages ocean cells with their ocean neighbors for realistic bathymetry.
    /// </summary>
    private static void HarmonizeOcean(float[,] heightmap, bool[,] oceanMask)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Create smoothed copy (don't modify while iterating)
        var smoothed = (float[,])heightmap.Clone();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!oceanMask[y, x]) continue;  // Only smooth ocean cells

                // Calculate average of ocean neighbors (8-connected)
                float sum = 0f;
                int count = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                            oceanMask[ny, nx])
                        {
                            sum += heightmap[ny, nx];
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    float avg = sum / count;
                    float smoothingFactor = 0.3f;  // Blend 30% towards average
                    smoothed[y, x] = heightmap[y, x] * (1 - smoothingFactor) + avg * smoothingFactor;
                }
            }
        }

        // Copy smoothed values back to heightmap
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (oceanMask[y, x])
                {
                    heightmap[y, x] = smoothed[y, x];
                }
            }
        }
    }

    /// <summary>
    /// Algorithm 4: Calculate normalized ocean depth map [0, 1].
    /// Depth = (seaLevel - elevation) normalized by ocean elevation range.
    /// </summary>
    private static float[,] CalculateSeaDepth(float[,] heightmap, bool[,] oceanMask, float seaLevel)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var seaDepth = new float[height, width];

        // Find min/max ocean elevation for normalization
        float minOcean = float.MaxValue;
        float maxOcean = float.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (oceanMask[y, x])
                {
                    float elevation = heightmap[y, x];
                    if (elevation < minOcean) minOcean = elevation;
                    if (elevation > maxOcean) maxOcean = elevation;
                }
            }
        }

        // Avoid division by zero
        float oceanRange = Math.Max(1e-6f, seaLevel - minOcean);

        // Calculate normalized depth for ocean cells
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (oceanMask[y, x])
                {
                    // Depth below sea level, normalized [0, 1]
                    float depth = (seaLevel - heightmap[y, x]) / oceanRange;
                    seaDepth[y, x] = Math.Max(0f, Math.Min(1f, depth));
                }
                // else: seaDepth[y, x] = 0 (default for land)
            }
        }

        return seaDepth;
    }
}
