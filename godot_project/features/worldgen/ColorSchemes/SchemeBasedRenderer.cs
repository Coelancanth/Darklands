using Godot;
using System;
using System.Collections.Generic;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Helper utilities for scheme-based rendering.
/// Shared logic to avoid duplication in WorldMapRendererNode.
/// </summary>
public static class SchemeBasedRenderer
{
    /// <summary>
    /// Renders a 2D array using a color scheme.
    /// Generic rendering for normalized [0,1] value maps (temperature, precipitation, etc.).
    /// </summary>
    public static Image RenderNormalizedMap(
        float[,] data,
        IColorScheme scheme,
        object[]? schemeContext = null)
    {
        int h = data.GetLength(0);
        int w = data.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float value = data[y, x];  // Already normalized [0, 1]
                Color color = scheme.GetColor(value, schemeContext ?? Array.Empty<object>());
                image.SetPixel(x, y, color);
            }
        }

        return image;
    }

    /// <summary>
    /// Renders grayscale elevation with colored markers overlay.
    /// Used by Sinks, RiverSources, and other marker-based visualizations.
    /// </summary>
    public static Image RenderGrayscaleWithMarkers(
        float[,] heightmap,
        List<(int x, int y)> markerPositions,
        Color markerColor)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Step 1: Normalize heightmap and render grayscale
        float min = float.MaxValue, max = float.MinValue;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float v = heightmap[y, x];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        float delta = Math.Max(1e-6f, max - min);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float normalized = (heightmap[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(normalized, normalized, normalized));
            }
        }

        // Step 2: Overlay markers
        foreach (var (x, y) in markerPositions)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
            {
                image.SetPixel(x, y, markerColor);
            }
        }

        return image;
    }

    /// <summary>
    /// Calculates 7 temperature quantile thresholds for discrete climate zones.
    /// Returns: { q12.5%, q25%, q37.5%, q50%, q62.5%, q75%, q87.5% }
    /// </summary>
    public static float[] CalculateTemperatureQuantiles(float[,] temperatureMap)
    {
        int h = temperatureMap.GetLength(0);
        int w = temperatureMap.GetLength(1);

        // Collect all temperature values
        var temps = new List<float>(h * w);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                temps.Add(temperatureMap[y, x]);
            }
        }

        // Sort for quantile calculation
        temps.Sort();

        // Calculate 7 quantiles
        return new float[]
        {
            GetPercentileFromSorted(temps, 0.125f),  // 12.5% - polar
            GetPercentileFromSorted(temps, 0.25f),   // 25% - alpine
            GetPercentileFromSorted(temps, 0.375f),  // 37.5% - boreal
            GetPercentileFromSorted(temps, 0.50f),   // 50% - cool
            GetPercentileFromSorted(temps, 0.625f),  // 62.5% - warm
            GetPercentileFromSorted(temps, 0.75f),   // 75% - subtropical
            GetPercentileFromSorted(temps, 0.875f)   // 87.5% - tropical
        };
    }

    /// <summary>
    /// Calculates 6 elevation quantile thresholds for terrain banding.
    /// Returns: { q15%, q70%, q75%, q90%, q95%, q99% }
    /// </summary>
    public static float[] CalculateElevationQuantiles(float[,] normalizedHeightmap)
    {
        return new float[]
        {
            FindQuantile(normalizedHeightmap, 0.15f),
            FindQuantile(normalizedHeightmap, 0.70f),
            FindQuantile(normalizedHeightmap, 0.75f),
            FindQuantile(normalizedHeightmap, 0.90f),
            FindQuantile(normalizedHeightmap, 0.95f),
            FindQuantile(normalizedHeightmap, 0.99f)
        };
    }

    /// <summary>
    /// Binary search approximation for quantile (matches plate-tectonics reference).
    /// </summary>
    private static float FindQuantile(float[,] heightmap, float quantile)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);
        int totalCells = height * width;

        float value = 0.5f;
        float step = 0.5f;

        while (step > 0.00001f)
        {
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (heightmap[y, x] < value) count++;
                }
            }

            step *= 0.5f;
            if (count / (float)totalCells < quantile)
                value += step;
            else
                value -= step;
        }

        return value;
    }

    /// <summary>
    /// Gets percentile value from a sorted list.
    /// </summary>
    private static float GetPercentileFromSorted(List<float> sortedValues, float percentile)
    {
        if (sortedValues.Count == 0) return 0f;

        int index = (int)Mathf.Floor(percentile * (sortedValues.Count - 1));
        index = Mathf.Clamp(index, 0, sortedValues.Count - 1);

        return sortedValues[index];
    }
}
