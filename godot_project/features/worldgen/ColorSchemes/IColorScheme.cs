using Godot;
using System.Collections.Generic;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Defines a color scheme for map visualization.
/// Implementations provide both rendering colors and legend metadata.
/// This ensures Single Source of Truth (SSOT): colors defined once, legends auto-generate.
/// </summary>
public interface IColorScheme
{
    /// <summary>
    /// Human-readable name for this color scheme (e.g., "Precipitation", "Temperature").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets legend entries for this color scheme.
    /// Used by WorldMapLegendNode to auto-generate legend UI.
    /// </summary>
    List<LegendEntry> GetLegendEntries();

    /// <summary>
    /// Gets the color for a given normalized value [0, 1].
    /// Used by WorldMapRendererNode for pixel coloring.
    /// </summary>
    /// <param name="normalizedValue">Value in range [0, 1] to map to color</param>
    /// <param name="context">Optional context data (e.g., quantiles, elevation data)</param>
    /// <returns>Color for the given value</returns>
    Color GetColor(float normalizedValue, params object[] context);
}
