using Godot;
using System;
using System.Collections.Generic;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Temperature visualization: Discrete 7-band climate zones (WorldEngine approach).
/// Uses quantile-based thresholds to adapt to planet's temperature distribution.
/// Blue (polar) → Purple spectrum → Red (tropical).
/// Used by all 4 temperature view modes (LatitudeOnly, WithNoise, WithDistance, Final).
/// </summary>
public class TemperatureScheme : IColorScheme
{
    public string Name => "Temperature";

    // SSOT: 7 discrete climate zone colors (WorldEngine palette)
    private static readonly Color PolarColor = new(0f, 0f, 1f);                    // Blue
    private static readonly Color AlpineColor = new(42f/255f, 0f, 213f/255f);      // Blue-Purple
    private static readonly Color BorealColor = new(85f/255f, 0f, 170f/255f);      // Purple
    private static readonly Color CoolColor = new(128f/255f, 0f, 128f/255f);       // Magenta
    private static readonly Color WarmColor = new(170f/255f, 0f, 85f/255f);        // Purple-Red
    private static readonly Color SubtropicalColor = new(213f/255f, 0f, 42f/255f); // Red-Purple
    private static readonly Color TropicalColor = new(1f, 0f, 0f);                 // Red

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Blue", PolarColor, "Polar (coldest)"),
            new("Blue-Purple", AlpineColor, "Alpine"),
            new("Purple", BorealColor, "Boreal"),
            new("Magenta", CoolColor, "Cool"),
            new("Purple-Red", WarmColor, "Warm"),
            new("Red-Purple", SubtropicalColor, "Subtropical"),
            new("Red", TropicalColor, "Tropical (hottest)")
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // Context must contain quantiles: float[] { q12.5, q25, q37.5, q50, q62.5, q75, q87.5 }
        if (context.Length == 0 || context[0] is not float[] quantiles || quantiles.Length != 7)
        {
            throw new ArgumentException("TemperatureScheme requires 7 quantile thresholds in context[0]");
        }

        // Discrete bands based on quantile thresholds
        if (normalizedValue < quantiles[0]) return PolarColor;       // < 12.5%
        if (normalizedValue < quantiles[1]) return AlpineColor;      // 12.5-25%
        if (normalizedValue < quantiles[2]) return BorealColor;      // 25-37.5%
        if (normalizedValue < quantiles[3]) return CoolColor;        // 37.5-50%
        if (normalizedValue < quantiles[4]) return WarmColor;        // 50-62.5%
        if (normalizedValue < quantiles[5]) return SubtropicalColor; // 62.5-75%
        return TropicalColor;                                         // 75-100%
    }

    /// <summary>
    /// [TD_025] Complete rendering pipeline - renders temperature map with quantile-based climate zones.
    /// Migrated from WorldMapRendererNode.RenderTemperatureMap() + CalculateTemperatureQuantiles().
    /// Supports all 4 temperature view modes (LatitudeOnly, WithNoise, WithDistance, Final).
    /// </summary>
    public Image? Render(WorldGenerationResult data, MapViewMode viewMode)
    {
        // Select the correct temperature map based on view mode
        float[,]? temperatureMap = viewMode switch
        {
            MapViewMode.TemperatureLatitudeOnly => data.TemperatureLatitudeOnly,
            MapViewMode.TemperatureWithNoise => data.TemperatureWithNoise,
            MapViewMode.TemperatureWithDistance => data.TemperatureWithDistance,
            MapViewMode.TemperatureFinal => data.TemperatureFinal,
            _ => null  // Not a temperature view mode
        };

        if (temperatureMap == null)
        {
            return null;  // Temperature data not available for this mode - fall back to legacy rendering
        }

        int h = temperatureMap.GetLength(0);
        int w = temperatureMap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Calculate quantile thresholds (7 temperature zones, WorldEngine-style)
        var quantiles = CalculateTemperatureQuantiles(temperatureMap);

        // Render with discrete color bands based on quantiles
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = temperatureMap[y, x];  // Normalized [0, 1]
                Color color = GetTemperatureColor(t, quantiles);
                image.SetPixel(x, y, color);
            }
        }

        return image;
    }

    /// <summary>
    /// Calculates temperature quantiles for discrete climate zone bands (WorldEngine approach).
    /// Returns 7 quantile thresholds: 12.5%, 25%, 37.5%, 50%, 62.5%, 75%, 87.5%.
    /// Migrated from WorldMapRendererNode.CalculateTemperatureQuantiles().
    /// </summary>
    private float[] CalculateTemperatureQuantiles(float[,] temperatureMap)
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

        // Calculate 7 quantiles (8 bands: < q0, q0-q1, q1-q2, ... q6+)
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
    /// Gets percentile value from a sorted list (helper for quantile calculation).
    /// Migrated from WorldMapRendererNode.GetPercentileFromSorted().
    /// </summary>
    private float GetPercentileFromSorted(List<float> sortedValues, float percentile)
    {
        if (sortedValues.Count == 0) return 0f;

        int index = (int)(percentile * (sortedValues.Count - 1));
        index = Math.Clamp(index, 0, sortedValues.Count - 1);
        return sortedValues[index];
    }

    /// <summary>
    /// Maps temperature value to climate zone color using quantile thresholds.
    /// Migrated from WorldMapRendererNode.GetTemperatureColorQuantile().
    /// </summary>
    private Color GetTemperatureColor(float t, float[] quantiles)
    {
        // Discrete bands based on quantile thresholds
        if (t < quantiles[0]) return PolarColor;       // < 12.5%
        if (t < quantiles[1]) return AlpineColor;      // 12.5-25%
        if (t < quantiles[2]) return BorealColor;      // 25-37.5%
        if (t < quantiles[3]) return CoolColor;        // 37.5-50%
        if (t < quantiles[4]) return WarmColor;        // 50-62.5%
        if (t < quantiles[5]) return SubtropicalColor; // 62.5-75%
        return TropicalColor;                           // 75-100%
    }
}
