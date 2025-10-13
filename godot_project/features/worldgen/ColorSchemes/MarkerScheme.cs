using Godot;
using System.Collections.Generic;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Base class for marker-based visualizations (grayscale + colored markers).
/// Subclasses define marker color and meaning.
/// Used by Sinks (red), RiverSources (cyan), and Hotspots (magenta) view modes.
/// </summary>
public abstract class MarkerScheme : IColorScheme
{
    public abstract string Name { get; }
    protected abstract Color MarkerColor { get; }
    protected abstract string MarkerLabel { get; }
    protected abstract string MarkerDescription { get; }

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Grayscale", new Color(0.5f, 0.5f, 0.5f), "Elevation (low→high)"),
            new(MarkerLabel, MarkerColor, MarkerDescription)
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // Context[0] = bool isMarker (if true, return marker color; else grayscale)
        if (context.Length > 0 && context[0] is bool isMarker && isMarker)
        {
            return MarkerColor;
        }

        // Default: Grayscale elevation
        return new Color(normalizedValue, normalizedValue, normalizedValue);
    }
}

/// <summary>
/// Sinks visualization: Grayscale elevation + Red markers at local minima.
/// Used by SinksPreFilling and SinksPostFilling view modes (VS_029).
/// </summary>
public class SinksMarkerScheme : MarkerScheme
{
    public override string Name => "Sinks";
    protected override Color MarkerColor => new(1f, 0f, 0f);  // Bright red
    protected override string MarkerLabel => "Red Dots";
    protected override string MarkerDescription => "Local minima (sinks)";
}

/// <summary>
/// River sources visualization: Grayscale elevation + Cyan markers at spawn points.
/// Used by RiverSources view mode (VS_029).
/// </summary>
public class RiverSourcesMarkerScheme : MarkerScheme
{
    public override string Name => "River Sources";
    protected override Color MarkerColor => new(1f, 0f, 0f);  // Bright red (consistent with sinks)
    protected override string MarkerLabel => "Red Dots";
    protected override string MarkerDescription => "River origins";
}

/// <summary>
/// Erosion hotspots visualization: Colored elevation + Magenta markers at high-energy zones.
/// Used by ErosionHotspots view mode (VS_029).
/// </summary>
public class HotspotsMarkerScheme : MarkerScheme
{
    public override string Name => "Erosion Hotspots";
    protected override Color MarkerColor => new(1f, 0f, 1f);  // Magenta
    protected override string MarkerLabel => "Magenta Dots";
    protected override string MarkerDescription => "High elev + high flow";
}
