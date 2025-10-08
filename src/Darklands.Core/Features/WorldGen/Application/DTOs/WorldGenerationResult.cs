namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Complete world generation output including native simulation and post-processing results.
/// This is the pipeline's output type (vs PlateSimulationResult which is native-only).
/// </summary>
/// <remarks>
/// VS_024: Dual-heightmap architecture with quantile thresholds:
/// - Heightmap (original raw [0.1-20]): SACRED native output, never modified
/// - PostProcessedHeightmap (raw [0.1-20]): After 4 WorldEngine algorithms (add_noise, fill_ocean, harmonize_ocean, sea_depth)
/// - Thresholds: Quantile-based elevation bands (adaptive per-world)
///
/// Design decision: Algorithms use RAW elevation + thresholds (WorldEngine approach).
/// Display/UI uses meters mapping (ElevationMapper utility in Presentation layer).
///
/// Optional fields allow incremental implementation of pipeline stages:
/// - Stage 1 (VS_024): PostProcessedHeightmap, OceanMask, SeaDepth, Thresholds
/// - Stage 2 (VS_025): TemperatureMap (uses raw elevation + MountainLevel threshold)
/// - Stage 3+: PrecipitationMap, erosion, hydrology, biomes
/// </remarks>
public record WorldGenerationResult
{
    /// <summary>
    /// Original heightmap from native simulation (SACRED - never modified).
    /// Raw elevation values from plate tectonics library [0.1-20] range.
    /// Preserved for visual comparison with post-processed results.
    /// </summary>
    public float[,] Heightmap { get; init; }

    /// <summary>
    /// Post-processed heightmap after 4 WorldEngine algorithms (VS_024 Stage 1).
    /// Still in raw range [0.1-20] (NOT normalized) for algorithm compatibility.
    /// Algorithms: add_noise, fill_ocean, harmonize_ocean, sea_depth.
    /// </summary>
    public float[,]? PostProcessedHeightmap { get; init; }

    /// <summary>
    /// Quantile-based elevation thresholds calculated from PostProcessedHeightmap distribution (VS_024).
    /// Used by algorithms (temperature, precipitation) instead of fixed normalization.
    /// Adapts to each world's terrain - flat worlds vs mountainous worlds have different thresholds.
    /// </summary>
    public ElevationThresholds? Thresholds { get; init; }

    /// <summary>
    /// Minimum elevation value in PostProcessedHeightmap (ocean floor).
    /// Used for realistic meters mapping in UI (prevents 50km ocean depths bug).
    /// </summary>
    public float MinElevation { get; init; }

    /// <summary>
    /// Maximum elevation value in PostProcessedHeightmap (highest peak).
    /// Used for realistic meters mapping in UI.
    /// </summary>
    public float MaxElevation { get; init; }

    /// <summary>
    /// Plate ownership map (plate ID per cell).
    /// </summary>
    public uint[,] PlatesMap { get; init; }

    /// <summary>
    /// Ocean mask (true = water, false = land) from flood-fill algorithm (VS_024 Stage 1).
    /// NOT a simple threshold! BFS flood fill from borders ensures connected ocean regions.
    /// </summary>
    public bool[,]? OceanMask { get; init; }

    /// <summary>
    /// Normalized ocean depth map [0, 1] for future ocean rendering (VS_024 Stage 1).
    /// Only non-zero for ocean cells. Depth = (seaLevel - elevation) normalized.
    /// </summary>
    public float[,]? SeaDepth { get; init; }

    /// <summary>
    /// Temperature map - Stage 1: Latitude-only (VS_025 Stage 2 debug).
    /// Pure latitude banding with axial tilt. Normalized [0,1].
    /// Visual signature: Horizontal bands, hot zone shifts with tilt.
    /// </summary>
    public float[,]? TemperatureLatitudeOnly { get; init; }

    /// <summary>
    /// Temperature map - Stage 2: + Noise (VS_025 Stage 2 debug).
    /// Latitude (92%) + climate noise (8%). Normalized [0,1].
    /// Visual signature: Subtle fuzz on latitude bands.
    /// </summary>
    public float[,]? TemperatureWithNoise { get; init; }

    /// <summary>
    /// Temperature map - Stage 3: + Distance to sun (VS_025 Stage 2 debug).
    /// Latitude + noise / distance². Normalized [0,1].
    /// Visual signature: Hot/cold planet variation (per-world).
    /// </summary>
    public float[,]? TemperatureWithDistance { get; init; }

    /// <summary>
    /// Temperature map - Stage 4: FINAL (VS_025 Stage 2 - production).
    /// Complete algorithm with mountain cooling. Normalized [0,1].
    /// Visual signature: Mountains blue at all latitudes.
    /// UI converts to °C via TemperatureMapper: [0,1] → [-60°C, +40°C].
    /// </summary>
    public float[,]? TemperatureFinal { get; init; }

    /// <summary>
    /// Per-world axial tilt parameter (VS_025 Stage 2).
    /// Shifts equator position. Range: [-0.5, 0.5], Gaussian-distributed.
    /// Displayed in probe for debugging hot zone shifts.
    /// </summary>
    public float? AxialTilt { get; init; }

    /// <summary>
    /// Per-world distance-to-sun parameter (VS_025 Stage 2).
    /// Hot vs cold planets. Range: [0.1, ~1.3], Gaussian-distributed, squared.
    /// Displayed in probe for debugging planet temperature variation.
    /// </summary>
    public float? DistanceToSun { get; init; }

    /// <summary>
    /// Precipitation map in mm/year (Stage 3 - future).
    /// Available after precipitation simulation (with rain shadow).
    /// </summary>
    public float[,]? PrecipitationMap { get; init; }

    /// <summary>
    /// Raw native output preserved for debugging and visualization.
    /// Always available regardless of pipeline stages.
    /// </summary>
    public PlateSimulationResult RawNativeOutput { get; init; }

    /// <summary>
    /// Map width (all arrays have same dimensions).
    /// </summary>
    public int Width => Heightmap.GetLength(1);

    /// <summary>
    /// Map height (all arrays have same dimensions).
    /// </summary>
    public int Height => Heightmap.GetLength(0);

    public WorldGenerationResult(
        float[,] heightmap,
        uint[,] platesMap,
        PlateSimulationResult rawNativeOutput,
        float[,]? postProcessedHeightmap = null,
        ElevationThresholds? thresholds = null,
        float minElevation = 0.1f,
        float maxElevation = 20.0f,
        bool[,]? oceanMask = null,
        float[,]? seaDepth = null,
        float[,]? temperatureLatitudeOnly = null,
        float[,]? temperatureWithNoise = null,
        float[,]? temperatureWithDistance = null,
        float[,]? temperatureFinal = null,
        float? axialTilt = null,
        float? distanceToSun = null,
        float[,]? precipitationMap = null)
    {
        Heightmap = heightmap;
        PostProcessedHeightmap = postProcessedHeightmap;
        Thresholds = thresholds;
        MinElevation = minElevation;
        MaxElevation = maxElevation;
        PlatesMap = platesMap;
        OceanMask = oceanMask;
        SeaDepth = seaDepth;
        TemperatureLatitudeOnly = temperatureLatitudeOnly;
        TemperatureWithNoise = temperatureWithNoise;
        TemperatureWithDistance = temperatureWithDistance;
        TemperatureFinal = temperatureFinal;
        AxialTilt = axialTilt;
        DistanceToSun = distanceToSun;
        PrecipitationMap = precipitationMap;
        RawNativeOutput = rawNativeOutput;
    }
}
