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
    /// Temperature map in Celsius (VS_025 Stage 2).
    /// Available after temperature simulation (latitude + noise + elevation cooling).
    /// </summary>
    public float[,]? TemperatureMap { get; init; }

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
        float[,]? temperatureMap = null,
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
        TemperatureMap = temperatureMap;
        PrecipitationMap = precipitationMap;
        RawNativeOutput = rawNativeOutput;
    }
}
