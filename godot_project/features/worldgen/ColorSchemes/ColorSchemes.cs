namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Central registry of all color schemes used for map visualization.
/// Single Source of Truth (SSOT): All colors and legends defined here.
/// Usage:
///   - Renderer: ColorSchemes.Precipitation.GetColor(value)
///   - Legend: ColorSchemes.Precipitation.GetLegendEntries()
/// </summary>
public static class ColorSchemes
{
    // Core terrain/climate schemes
    public static readonly ElevationScheme Elevation = new();
    public static readonly TemperatureScheme Temperature = new();
    public static readonly PrecipitationScheme Precipitation = new();

    // D-8 erosion schemes (VS_029)
    public static readonly FlowDirectionScheme FlowDirections = new();
    public static readonly FlowAccumulationScheme FlowAccumulation = new();
    public static readonly SinksMarkerScheme Sinks = new();
    public static readonly RiverSourcesMarkerScheme RiverSources = new();
    public static readonly HotspotsMarkerScheme Hotspots = new();

    // Simple schemes
    public static readonly GrayscaleScheme Grayscale = new();
}
