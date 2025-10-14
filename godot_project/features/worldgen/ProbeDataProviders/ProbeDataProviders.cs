namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Central registry of all probe data providers used for map cell inspection.
/// Single Source of Truth (SSOT): All probe formatting logic defined here.
///
/// TD_026: Mirrors ColorSchemes.cs pattern from TD_025 for architectural consistency.
/// Usage:
///   - WorldMapProbeNode: ProbeDataProviders.Elevation.GetProbeText(data, x, y, viewMode)
///   - Testing: var provider = ProbeDataProviders.Temperature; provider.GetProbeText(...)
/// </summary>
public static class ProbeDataProviders
{
    // Simple providers (single view mode)
    public static readonly RawElevationProbeProvider RawElevation = new();
    public static readonly PlatesProbeProvider Plates = new();

    // Complex providers (multi-mode or extensive logic)
    public static readonly ElevationProbeProvider Elevation = new();
    public static readonly TemperatureProbeProvider Temperature = new();
    public static readonly PrecipitationProbeProvider Precipitation = new();
    public static readonly RainShadowProbeProvider RainShadow = new();
    public static readonly CoastalMoistureProbeProvider CoastalMoisture = new();

    // Erosion/flow providers (VS_029 D-8 flow visualization)
    public static readonly SinksPreFillingProbeProvider SinksPreFilling = new();
    public static readonly SinksPostFillingProbeProvider SinksPostFilling = new();
    public static readonly BasinMetadataProbeProvider BasinMetadata = new();
    public static readonly FlowDirectionsProbeProvider FlowDirections = new();
    public static readonly FlowAccumulationProbeProvider FlowAccumulation = new();
    public static readonly RiverSourcesProbeProvider RiverSources = new();
    public static readonly ErosionHotspotsProbeProvider ErosionHotspots = new();
}
