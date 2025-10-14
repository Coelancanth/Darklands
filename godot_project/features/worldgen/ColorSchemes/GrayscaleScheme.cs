using Godot;
using System.Collections.Generic;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Simple grayscale elevation rendering (min=black, max=white).
/// Used by RawElevation view mode and as base layer for marker-based visualizations.
/// </summary>
public class GrayscaleScheme : IColorScheme
{
    public string Name => "Grayscale Elevation";

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Black", new Color(0f, 0f, 0f), "Low elevation"),
            new("Gray", new Color(0.5f, 0.5f, 0.5f), "Mid elevation"),
            new("White", new Color(1f, 1f, 1f), "High elevation")
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // Simple grayscale: normalized value [0,1] maps directly to gray [black, white]
        return new Color(normalizedValue, normalizedValue, normalizedValue);
    }
}
