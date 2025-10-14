using Godot;
using System;
using System.Collections.Generic;

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
}
