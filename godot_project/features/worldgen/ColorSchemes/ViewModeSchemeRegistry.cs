using Darklands.Core.Features.WorldGen.Application.Common;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// SINGLE SOURCE OF TRUTH: Maps each MapViewMode to its ColorScheme.
/// This registry ensures Renderer and Legend ALWAYS use the same scheme.
/// Change the mapping here, both components automatically update!
/// </summary>
public static class ViewModeSchemeRegistry
{
    /// <summary>
    /// Gets the color scheme for a given view mode.
    /// Returns null for view modes that don't use schemes (e.g., Plates, RawElevation).
    /// </summary>
    public static IColorScheme? GetScheme(MapViewMode viewMode)
    {
        return viewMode switch
        {
            // Elevation views (2 modes, 1 scheme)
            MapViewMode.ColoredOriginalElevation => ColorSchemes.Elevation,
            MapViewMode.ColoredPostProcessedElevation => ColorSchemes.Elevation,

            // Temperature views (4 modes, 1 scheme)
            MapViewMode.TemperatureLatitudeOnly => ColorSchemes.Temperature,
            MapViewMode.TemperatureWithNoise => ColorSchemes.Temperature,
            MapViewMode.TemperatureWithDistance => ColorSchemes.Temperature,
            MapViewMode.TemperatureFinal => ColorSchemes.Temperature,

            // Precipitation views (5 modes, 1 scheme)
            MapViewMode.PrecipitationNoiseOnly => ColorSchemes.Precipitation,
            MapViewMode.PrecipitationTemperatureShaped => ColorSchemes.Precipitation,
            MapViewMode.PrecipitationBase => ColorSchemes.Precipitation,
            MapViewMode.PrecipitationWithRainShadow => ColorSchemes.Precipitation,
            MapViewMode.PrecipitationFinal => ColorSchemes.Precipitation,

            // D-8 Erosion views (VS_029)
            MapViewMode.SinksPreFilling => ColorSchemes.Sinks,
            MapViewMode.SinksPostFilling => ColorSchemes.Sinks,
            MapViewMode.FlowDirections => ColorSchemes.FlowDirections,
            MapViewMode.FlowAccumulation => ColorSchemes.FlowAccumulation,
            MapViewMode.RiverSources => ColorSchemes.RiverSources,
            MapViewMode.ErosionHotspots => ColorSchemes.Hotspots,

            // Simple views
            MapViewMode.RawElevation => ColorSchemes.Grayscale,

            // Non-scheme views (procedural/custom rendering)
            MapViewMode.Plates => null,  // Procedural random colors per plate

            // Default: No scheme
            _ => null
        };
    }

    /// <summary>
    /// Gets a human-readable title for the view mode's legend.
    /// Used as the legend header subtitle (context for the color scheme).
    /// </summary>
    public static string GetLegendTitle(MapViewMode viewMode)
    {
        return viewMode switch
        {
            // Elevation
            MapViewMode.ColoredOriginalElevation => "Original Elevation (native raw, unmodified)",
            MapViewMode.ColoredPostProcessedElevation => "Post-Processed (noise + smooth ocean)",

            // Temperature
            MapViewMode.TemperatureLatitudeOnly => "Latitude Only (quantile bands)",
            MapViewMode.TemperatureWithNoise => "+ Climate Noise (8% variation)",
            MapViewMode.TemperatureWithDistance => "+ Distance to Sun (hot/cold planets)",
            MapViewMode.TemperatureFinal => "Final Temperature (+ mountain cooling)",

            // Precipitation
            MapViewMode.PrecipitationNoiseOnly => "Noise Only (base coherent noise)",
            MapViewMode.PrecipitationTemperatureShaped => "+ Temp Gamma Curve (physics shaping)",
            MapViewMode.PrecipitationBase => "Base Precipitation (before rain shadow)",
            MapViewMode.PrecipitationWithRainShadow => "+ Rain Shadow (orographic blocking)",
            MapViewMode.PrecipitationFinal => "FINAL (+ Coastal - maritime vs continental)",

            // D-8 Erosion
            MapViewMode.SinksPreFilling => "Sinks (PRE-Filling) - Baseline before pit-filling",
            MapViewMode.SinksPostFilling => "Sinks (POST-Filling) - After pit-filling",
            MapViewMode.FlowDirections => "Flow Directions (D-8) - Steepest descent",
            MapViewMode.FlowAccumulation => "Flow Accumulation - Two-layer naturalistic",
            MapViewMode.RiverSources => "River Sources - Threshold-crossing algorithm",
            MapViewMode.ErosionHotspots => "Erosion Hotspots - High-energy zones",

            // Simple
            MapViewMode.RawElevation => "Raw Elevation (grayscale)",
            MapViewMode.Plates => "Plates (random colors per plate)",

            _ => "Unknown View Mode"
        };
    }

    /// <summary>
    /// Checks if a view mode uses a color scheme.
    /// If false, the view mode requires custom rendering logic.
    /// </summary>
    public static bool HasScheme(MapViewMode viewMode)
    {
        return GetScheme(viewMode) != null;
    }
}
