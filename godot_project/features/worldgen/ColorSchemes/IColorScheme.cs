using Godot;
using System.Collections.Generic;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Defines a color scheme for map visualization.
/// Implementations provide both rendering colors and legend metadata.
/// This ensures Single Source of Truth (SSOT): colors defined once, legends auto-generate.
///
/// TD_025: Enhanced to support complete rendering pipeline ownership.
/// Schemes can implement EITHER pattern:
/// 1. NEW: Render(data) - Self-contained rendering (recommended for new schemes)
/// 2. OLD: GetColor(value, context) - Per-pixel color lookup (backward compatibility)
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
    /// [OPTIONAL - OLD PATTERN] Gets the color for a given normalized value [0, 1].
    /// Used by WorldMapRendererNode for pixel coloring.
    /// Schemes implementing Render() may return default color or throw NotImplementedException.
    /// </summary>
    /// <param name="normalizedValue">Value in range [0, 1] to map to color</param>
    /// <param name="context">Optional context data (e.g., quantiles, elevation data)</param>
    /// <returns>Color for the given value</returns>
    Color GetColor(float normalizedValue, params object[] context);

    /// <summary>
    /// [NEW PATTERN - TD_025] Renders complete view from world data.
    /// Schemes own their entire rendering pipeline: data fetching, calculations, pixel coloring.
    ///
    /// Benefits:
    /// - Visualization logic (quantiles, statistical analysis) stays in scheme (not in Core)
    /// - Self-contained rendering strategies (true Strategy Pattern)
    /// - No intermediate DTOs for visualization-only data
    /// - Schemes decide HOW to display data, Core provides WHAT data exists
    ///
    /// Returns null if scheme doesn't implement this pattern (falls back to old GetColor() loop).
    /// </summary>
    /// <param name="data">Complete world generation result with all available data</param>
    /// <param name="viewMode">Current view mode (optional context for schemes that render multiple modes)</param>
    /// <returns>Rendered image, or null if scheme uses old GetColor() pattern</returns>
    Image? Render(WorldGenerationResult data, MapViewMode viewMode);
}
