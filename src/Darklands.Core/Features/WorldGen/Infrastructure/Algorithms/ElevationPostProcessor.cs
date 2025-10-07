using System;
using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Post-processing algorithms for elevation data from plate tectonics simulation.
/// Ported from WorldEngine (Python) with C# optimizations.
/// </summary>
public static class ElevationPostProcessor
{
    /// <summary>
    /// Lowers elevation at map borders to create natural coastlines.
    /// Prevents landlocked oceans and ensures borders are water.
    /// </summary>
    /// <param name="heightmap">Elevation data (modified in-place)</param>
    /// <param name="borderReduction">Multiplier for border cells (0.0-1.0, default 0.8)</param>
    public static void PlaceOceansAtBorders(float[,] heightmap, float borderReduction = 0.8f)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        // Lower elevation at all border cells
        for (int x = 0; x < width; x++)
        {
            heightmap[0, x] *= borderReduction;          // Top border
            heightmap[height - 1, x] *= borderReduction; // Bottom border
        }

        // Left and right borders (excluding corners already processed)
        for (int y = 1; y < height - 1; y++)
        {
            heightmap[y, 0] *= borderReduction;         // Left border
            heightmap[y, width - 1] *= borderReduction; // Right border
        }
    }

    /// <summary>
    /// Redistributes land elevations to correct skewed distributions where most land sits near the maximum.
    /// Applies a gamma transform only on land cells, preserving ocean and overall min/max range.
    /// Use gamma > 1.0 to push values down (more lowlands), gamma < 1.0 to lift values up (more highlands).
    /// </summary>
    /// <param name="heightmap">Elevation data (modified in-place)</param>
    /// <param name="oceanMask">Ocean mask</param>
    /// <param name="gamma">Gamma exponent for redistribution (default 1.8 for more plains/hills)</param>
    public static void NormalizeLandDistribution(float[,] heightmap, bool[,] oceanMask, float gamma = 1.8f)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        float minLand = float.PositiveInfinity;
        float maxLand = float.NegativeInfinity;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (oceanMask[y, x])
                    continue;

                float e = heightmap[y, x];
                if (e < minLand) minLand = e;
                if (e > maxLand) maxLand = e;
            }
        }

        // Nothing to do if no land or degenerate range
        if (float.IsPositiveInfinity(minLand) || maxLand - minLand <= 1e-6f)
            return;

        float range = maxLand - minLand;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (oceanMask[y, x])
                    continue;

                float e = heightmap[y, x];
                float t = (e - minLand) / range; // [0,1]
                // Push high values downwards to create more plains/hills
                float tPrime = (float)Math.Pow(Math.Clamp(t, 0f, 1f), gamma);
                heightmap[y, x] = Math.Clamp(minLand + tPrime * range, 0f, 1f);
            }
        }
    }

    /// <summary>
    /// Marks ocean cells using flood fill from map borders.
    /// Any cell connected to border by cells below sea level is ocean.
    /// </summary>
    /// <param name="heightmap">Elevation data</param>
    /// <param name="seaLevel">Threshold for water (cells below this are water)</param>
    /// <returns>Ocean mask (true = ocean, false = land)</returns>
    public static bool[,] FillOcean(float[,] heightmap, float seaLevel)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var oceanMask = new bool[height, width];
        var visited = new bool[height, width];

        // BFS queue starting from all border cells below sea level
        var queue = new Queue<(int y, int x)>();

        // Seed queue with border cells that are water
        for (int x = 0; x < width; x++)
        {
            // Top border
            if (heightmap[0, x] < seaLevel)
            {
                queue.Enqueue((0, x));
                visited[0, x] = true;
            }

            // Bottom border
            if (heightmap[height - 1, x] < seaLevel)
            {
                queue.Enqueue((height - 1, x));
                visited[height - 1, x] = true;
            }
        }

        for (int y = 0; y < height; y++)
        {
            // Left border
            if (heightmap[y, 0] < seaLevel)
            {
                queue.Enqueue((y, 0));
                visited[y, 0] = true;
            }

            // Right border
            if (heightmap[y, width - 1] < seaLevel)
            {
                queue.Enqueue((y, width - 1));
                visited[y, width - 1] = true;
            }
        }

        // BFS flood fill
        while (queue.Count > 0)
        {
            var (y, x) = queue.Dequeue();
            oceanMask[y, x] = true;

            // Check 4 neighbors (N, S, E, W)
            TryEnqueue(y - 1, x); // North
            TryEnqueue(y + 1, x); // South
            TryEnqueue(y, x + 1); // East
            TryEnqueue(y, x - 1); // West

            void TryEnqueue(int ny, int nx)
            {
                if (ny >= 0 && ny < height && nx >= 0 && nx < width &&
                    !visited[ny, nx] && heightmap[ny, nx] < seaLevel)
                {
                    queue.Enqueue((ny, nx));
                    visited[ny, nx] = true;
                }
            }
        }

        return oceanMask;
    }

    /// <summary>
    /// Adds Perlin noise to elevation for natural terrain variation.
    /// Prevents artificially smooth terrain from plate simulation.
    /// </summary>
    /// <param name="heightmap">Elevation data (modified in-place)</param>
    /// <param name="seed">Random seed for reproducibility</param>
    /// <param name="scale">Noise frequency (lower = larger features, default 0.05)</param>
    /// <param name="amplitude">Noise strength (default 0.1 = ±10% elevation change)</param>
    public static void AddNoise(float[,] heightmap, int seed, float scale = 0.05f, float amplitude = 0.06f)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var noise = new PerlinNoise(seed);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Sample Perlin noise at scaled coordinates
                float noiseValue = noise.Sample(x * scale, y * scale);

                // Add noise to elevation (locally clamped to [0, 1])
                float newElevation = heightmap[y, x] + noiseValue * amplitude;
                heightmap[y, x] = Math.Clamp(newElevation, 0f, 1f);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper: Simple Perlin Noise Implementation
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lightweight Perlin noise for terrain variation.
    /// Sufficient for MVP; can upgrade to FastNoiseLite if needed.
    /// </summary>
    private class PerlinNoise
    {
        private readonly int[] _permutation;

        public PerlinNoise(int seed)
        {
            var random = new Random(seed);

            // Generate permutation table
            _permutation = new int[512];
            var p = new int[256];

            for (int i = 0; i < 256; i++)
                p[i] = i;

            // Fisher-Yates shuffle
            for (int i = 255; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }

            // Duplicate for wrapping
            for (int i = 0; i < 512; i++)
                _permutation[i] = p[i % 256];
        }

        public float Sample(float x, float y)
        {
            // Grid cell coordinates
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;

            // Local coordinates within cell [0, 1]
            float xf = x - (float)Math.Floor(x);
            float yf = y - (float)Math.Floor(y);

            // Fade curves for smooth interpolation
            float u = Fade(xf);
            float v = Fade(yf);

            // Hash coordinates of 4 cube corners
            int aa = _permutation[_permutation[xi] + yi];
            int ab = _permutation[_permutation[xi] + yi + 1];
            int ba = _permutation[_permutation[xi + 1] + yi];
            int bb = _permutation[_permutation[xi + 1] + yi + 1];

            // Gradients at corners
            float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            float x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

            return Lerp(x1, x2, v);
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        private static float Grad(int hash, float x, float y)
        {
            // Convert hash to gradient direction (8 directions)
            int h = hash & 7;
            float u = h < 4 ? x : y;
            float v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
