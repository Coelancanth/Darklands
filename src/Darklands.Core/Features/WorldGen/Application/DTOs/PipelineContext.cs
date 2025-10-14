using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Immutable context passed between pipeline stages (TD_027).
/// Contains all intermediate data from world generation process.
/// </summary>
/// <remarks>
/// Design Principles:
/// - Immutable: Stages return new context via `with` expression (functional data flow)
/// - Type-safe: Compile-time field checking (vs runtime dictionary keys)
/// - Optional fields: Most fields start null, populated as pipeline progresses
/// - Fluent updates: Helper methods for common transformations (WithXxx pattern)
///
/// Why DTO instead of Dictionary&lt;string, object&gt;?
/// 1. Type Safety: Compile-time field names, no casting, IntelliSense support
/// 2. Clear Dependencies: Each stage declares what it needs/produces
/// 3. Performance: Direct field access vs dictionary lookups (O(1) hash vs O(1) field)
/// 4. Refactoring: Rename field = compiler finds all usages (vs runtime key strings)
///
/// Data Flow Example (Temperature Stage):
/// <code>
/// // INPUT: Context with elevation data
/// var input = new PipelineContext(...) { PostProcessedHeightmap = heightmap };
///
/// // STAGE: Temperature calculation
/// var tempResult = TemperatureCalculator.Calculate(...);
///
/// // OUTPUT: Updated context with temperature data (immutable)
/// return Result.Success(input with { TemperatureFinal = tempResult.FinalMap });
/// </code>
///
/// Memory Efficiency:
/// - Array fields store REFERENCES (not copies!) → O(1) memory overhead per stage
/// - Immutable record uses structural equality → safe sharing between stages
/// - No defensive cloning needed (stages never mutate shared data)
///
/// Pipeline Stage Dependencies:
/// - Stage 0: PlateGenerationStage → Produces: RawHeightmap, PlatesMap, RawNativeOutput
/// - Stage 1: ElevationPostProcessStage → Requires: RawHeightmap → Produces: PostProcessedHeightmap, OceanMask, SeaDepth
/// - Stage 2: TemperatureStage → Requires: PostProcessedHeightmap, Thresholds → Produces: TemperatureFinal
/// - Stage 3: PrecipitationStage → Requires: TemperatureFinal → Produces: FinalPrecipitationMap
/// - Stage 4: RainShadowStage → Requires: FinalPrecipitationMap, PostProcessedHeightmap → Produces: WithRainShadowPrecipitationMap
/// - Stage 5: CoastalMoistureStage → Requires: WithRainShadowPrecipitationMap, OceanMask → Produces: PrecipitationFinal
/// - Stage 6: D8FlowStage → Requires: PostProcessedHeightmap, PrecipitationFinal → Produces: Phase1Erosion
/// </remarks>
public record PipelineContext
{
    // ═══════════════════════════════════════════════════════════════════════
    // CONFIGURATION (immutable parameters)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Random seed for reproducible world generation</summary>
    public int Seed { get; init; }

    /// <summary>World map size (square maps only)</summary>
    public int WorldSize { get; init; }

    /// <summary>Number of tectonic plates</summary>
    public int PlateCount { get; init; }

    /// <summary>Feedback loop iteration count (for Iterative mode)</summary>
    public int FeedbackIterations { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // STAGE 0: PLATE TECTONICS SIMULATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Raw heightmap from native plate simulation (SACRED - never modified)</summary>
    public float[,]? RawHeightmap { get; init; }

    /// <summary>Plate ownership map (plate ID per cell)</summary>
    public uint[,]? PlatesMap { get; init; }

    /// <summary>Raw native simulation result (for debugging/comparison)</summary>
    public PlateSimulationResult? RawNativeOutput { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // STAGE 1: ELEVATION POST-PROCESSING (VS_024)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Post-processed heightmap (after 4 WorldEngine algorithms, still raw [0.1-20])</summary>
    public float[,]? PostProcessedHeightmap { get; init; }

    /// <summary>Quantile-based elevation thresholds (adaptive per-world)</summary>
    public ElevationThresholds? Thresholds { get; init; }

    /// <summary>Minimum elevation in PostProcessedHeightmap (ocean floor)</summary>
    public float MinElevation { get; init; }

    /// <summary>Maximum elevation in PostProcessedHeightmap (highest peak)</summary>
    public float MaxElevation { get; init; }

    /// <summary>Ocean mask from BFS flood fill (true = water, false = land)</summary>
    public bool[,]? OceanMask { get; init; }

    /// <summary>Normalized ocean depth map [0, 1]</summary>
    public float[,]? SeaDepth { get; init; }

    /// <summary>Sea level in normalized [0, 1] scale for rendering</summary>
    public float? SeaLevelNormalized { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // STAGE 2: TEMPERATURE (VS_025)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Temperature map - Stage 1: Latitude-only (debug)</summary>
    public float[,]? TemperatureLatitudeOnly { get; init; }

    /// <summary>Temperature map - Stage 2: + Noise (debug)</summary>
    public float[,]? TemperatureWithNoise { get; init; }

    /// <summary>Temperature map - Stage 3: + Distance to sun (debug)</summary>
    public float[,]? TemperatureWithDistance { get; init; }

    /// <summary>Temperature map - Stage 4: FINAL (with mountain cooling)</summary>
    public float[,]? TemperatureFinal { get; init; }

    /// <summary>Per-world axial tilt parameter</summary>
    public float? AxialTilt { get; init; }

    /// <summary>Per-world distance-to-sun parameter</summary>
    public float? DistanceToSun { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // STAGE 3: BASE PRECIPITATION (VS_026)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Precipitation map - Stage 1: Base Noise (debug)</summary>
    public float[,]? BaseNoisePrecipitationMap { get; init; }

    /// <summary>Precipitation map - Stage 2: + Temperature shaping (debug)</summary>
    public float[,]? TemperatureShapedPrecipitationMap { get; init; }

    /// <summary>Precipitation map - Stage 3: FINAL base precipitation</summary>
    public float[,]? FinalPrecipitationMap { get; init; }

    /// <summary>Quantile-based precipitation thresholds (adaptive per-world)</summary>
    public PrecipitationThresholds? PrecipitationThresholds { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // STAGE 4: RAIN SHADOW (VS_027)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Precipitation map - Stage 4: + Rain shadow effect (orographic blocking)</summary>
    public float[,]? WithRainShadowPrecipitationMap { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // STAGE 5: COASTAL MOISTURE (VS_028)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Precipitation map - Stage 5: FINAL (with coastal enhancement, used by erosion)</summary>
    public float[,]? PrecipitationFinal { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // STAGE 6: D-8 FLOW (VS_029)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Phase 1 erosion data (pit filling, flow directions, flow accumulation, river sources)</summary>
    public Phase1ErosionData? Phase1Erosion { get; init; }

    /// <summary>Local minima detected BEFORE pit-filling (diagnostic baseline)</summary>
    public List<(int x, int y)>? PreFillingLocalMinima { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates new pipeline context with configuration parameters.
    /// All data fields start null, populated by stages as pipeline executes.
    /// </summary>
    public PipelineContext(
        int seed,
        int worldSize,
        int plateCount,
        int feedbackIterations = 1)
    {
        Seed = seed;
        WorldSize = worldSize;
        PlateCount = plateCount;
        FeedbackIterations = feedbackIterations;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FLUENT UPDATE HELPERS (immutable transformations)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns new context with plate simulation results.
    /// Used by PlateGenerationStage (Stage 0).
    /// </summary>
    public PipelineContext WithPlateSimulation(PlateSimulationResult nativeResult) => this with
    {
        RawHeightmap = nativeResult.Heightmap,
        PlatesMap = nativeResult.PlatesMap,
        RawNativeOutput = nativeResult
    };

    /// <summary>
    /// Returns new context with elevation post-processing results.
    /// Used by ElevationPostProcessStage (Stage 1).
    /// </summary>
    public PipelineContext WithElevationProcessing(
        float[,] postProcessedHeightmap,
        ElevationThresholds thresholds,
        float minElevation,
        float maxElevation,
        bool[,] oceanMask,
        float[,] seaDepth,
        float seaLevelNormalized) => this with
        {
            PostProcessedHeightmap = postProcessedHeightmap,
            Thresholds = thresholds,
            MinElevation = minElevation,
            MaxElevation = maxElevation,
            OceanMask = oceanMask,
            SeaDepth = seaDepth,
            SeaLevelNormalized = seaLevelNormalized
        };

    /// <summary>
    /// Returns new context with temperature calculation results.
    /// Used by TemperatureStage (Stage 2).
    /// </summary>
    public PipelineContext WithTemperature(
        float[,] latitudeOnly,
        float[,] withNoise,
        float[,] withDistance,
        float[,] final,
        float axialTilt,
        float distanceToSun) => this with
        {
            TemperatureLatitudeOnly = latitudeOnly,
            TemperatureWithNoise = withNoise,
            TemperatureWithDistance = withDistance,
            TemperatureFinal = final,
            AxialTilt = axialTilt,
            DistanceToSun = distanceToSun
        };

    /// <summary>
    /// Returns new context with base precipitation calculation results.
    /// Used by PrecipitationStage (Stage 3).
    /// </summary>
    public PipelineContext WithBasePrecipitation(
        float[,] baseNoise,
        float[,] temperatureShaped,
        float[,] final,
        PrecipitationThresholds thresholds) => this with
        {
            BaseNoisePrecipitationMap = baseNoise,
            TemperatureShapedPrecipitationMap = temperatureShaped,
            FinalPrecipitationMap = final,
            PrecipitationThresholds = thresholds
        };

    /// <summary>
    /// Returns new context with rain shadow calculation results.
    /// Used by RainShadowStage (Stage 4).
    /// </summary>
    public PipelineContext WithRainShadow(float[,] withRainShadowMap) => this with
    {
        WithRainShadowPrecipitationMap = withRainShadowMap
    };

    /// <summary>
    /// Returns new context with coastal moisture calculation results.
    /// Used by CoastalMoistureStage (Stage 5).
    /// </summary>
    public PipelineContext WithCoastalMoisture(float[,] finalPrecipitation) => this with
    {
        PrecipitationFinal = finalPrecipitation
    };

    /// <summary>
    /// Returns new context with D-8 flow calculation results.
    /// Used by D8FlowStage (Stage 6).
    /// </summary>
    public PipelineContext WithD8Flow(
        Phase1ErosionData phase1Data,
        List<(int x, int y)> preFillingMinima) => this with
        {
            Phase1Erosion = phase1Data,
            PreFillingLocalMinima = preFillingMinima
        };
}
