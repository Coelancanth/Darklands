using Godot;
using System;
using System.Collections.Generic;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Elevation visualization: Quantile-based 7-band terrain gradient.
/// Matches plate-tectonics library reference implementation (map_drawing.cpp).
/// Deep ocean → Ocean → Shallow water → Grass → Hills → Mountains → Peaks.
/// Used by ColoredOriginalElevation, ColoredPostProcessedElevation view modes.
/// </summary>
public class ElevationScheme : IColorScheme
{
    public string Name => "Elevation";

    // SSOT: 7-band terrain color gradient (plate-tectonics reference palette)
    // Deep ocean → Ocean
    private static readonly Color DeepOceanStart = new(0f, 0f, 1f);
    private static readonly Color DeepOceanEnd = new(0f, 0.078f, 0.784f);

    // Ocean → Shallow water
    private static readonly Color OceanStart = new(0f, 0.078f, 0.784f);
    private static readonly Color OceanEnd = new(0.196f, 0.314f, 0.882f);

    // Shallow water → Land transition
    private static readonly Color ShallowStart = new(0.196f, 0.314f, 0.882f);
    private static readonly Color ShallowEnd = new(0.529f, 0.929f, 0.922f);

    // Land/grass → Hills
    private static readonly Color LandStart = new(0.345f, 0.678f, 0.192f);
    private static readonly Color LandEnd = new(0.855f, 0.886f, 0.227f);

    // Hills → Mountains
    private static readonly Color HillsStart = new(0.855f, 0.886f, 0.227f);
    private static readonly Color HillsEnd = new(0.984f, 0.988f, 0.165f);

    // Mountains → High peaks
    private static readonly Color MountainsStart = new(0.984f, 0.988f, 0.165f);
    private static readonly Color MountainsEnd = new(0.357f, 0.110f, 0.051f);

    // High peaks → Extreme peaks
    private static readonly Color PeaksStart = new(0.357f, 0.110f, 0.051f);
    private static readonly Color PeaksEnd = new(0.200f, 0f, 0.016f);

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Deep Blue", DeepOceanStart, "Deep ocean"),
            new("Blue", OceanStart, "Ocean"),
            new("Cyan", ShallowEnd, "Shallow water"),
            new("Green", LandStart, "Grass/Lowlands"),
            new("Yellow-Green", LandEnd, "Hills"),
            new("Yellow", HillsEnd, "Mountains"),
            new("Brown", MountainsEnd, "Peaks")
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // Context must contain quantiles: float[] { q15, q70, q75, q90, q95, q99 }
        if (context.Length == 0 || context[0] is not float[] quantiles || quantiles.Length != 6)
        {
            throw new ArgumentException("ElevationScheme requires 6 quantile thresholds in context[0]");
        }

        float q15 = quantiles[0];
        float q70 = quantiles[1];
        float q75 = quantiles[2];
        float q90 = quantiles[3];
        float q95 = quantiles[4];
        float q99 = quantiles[5];

        // Quantile-based color bands
        if (normalizedValue < q15)
            return Gradient(normalizedValue, 0.0f, q15, DeepOceanStart, DeepOceanEnd);

        if (normalizedValue < q70)
            return Gradient(normalizedValue, q15, q70, OceanStart, OceanEnd);

        if (normalizedValue < q75)
            return Gradient(normalizedValue, q70, q75, ShallowStart, ShallowEnd);

        if (normalizedValue < q90)
            return Gradient(normalizedValue, q75, q90, LandStart, LandEnd);

        if (normalizedValue < q95)
            return Gradient(normalizedValue, q90, q95, HillsStart, HillsEnd);

        if (normalizedValue < q99)
            return Gradient(normalizedValue, q95, q99, MountainsStart, MountainsEnd);

        return Gradient(normalizedValue, q99, 1.0f, PeaksStart, PeaksEnd);
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
