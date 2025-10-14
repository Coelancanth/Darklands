using Godot;
using System;
using System.Collections.Generic;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Precipitation visualization: Smooth 3-stop moisture gradient.
/// Yellow (dry deserts) → Green (moderate vegetation) → Blue (wet tropics).
/// Used by all precipitation view modes (NoiseOnly, TemperatureShaped, Base, WithRainShadow, Final).
/// </summary>
public class PrecipitationScheme : IColorScheme
{
    public string Name => "Precipitation";

    // SSOT: Color definitions (Yellow → Green → Blue)
    private static readonly Color DryColor = new(1f, 1f, 0f);               // Yellow (RGB: 255, 255, 0)
    private static readonly Color ModerateColor = new(0f, 200f/255f, 0f);   // Green (RGB: 0, 200, 0)
    private static readonly Color WetColor = new(0f, 0f, 1f);               // Blue (RGB: 0, 0, 255)

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Yellow", DryColor, "Dry (<400mm/year)"),
            new("Green", ModerateColor, "Medium (400-800mm)"),
            new("Blue", WetColor, "High (>800mm/year)")
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // 3-stop gradient: Yellow (0.0) → Green (0.5) → Blue (1.0)
        if (normalizedValue < 0.5f)
        {
            // Dry to moderate: Yellow → Green
            return Gradient(normalizedValue, 0.0f, 0.5f, DryColor, ModerateColor);
        }
        else
        {
            // Moderate to wet: Green → Blue
            return Gradient(normalizedValue, 0.5f, 1.0f, ModerateColor, WetColor);
        }
    }

    /// <summary>
    /// Linear interpolation between two colors.
    /// </summary>
    private static Color Gradient(float value, float min, float max, Color colorA, Color colorB)
    {
        float delta = max - min;
        if (delta < 0.00001f) return colorA;

        float t = Mathf.Clamp((value - min) / delta, 0f, 1f);
        return colorA.Lerp(colorB, t);
    }
}
