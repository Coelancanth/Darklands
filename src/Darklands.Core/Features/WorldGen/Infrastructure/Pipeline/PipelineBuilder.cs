using System;
using System.Collections.Generic;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Infrastructure.Pipeline.Stages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Pipeline;

/// <summary>
/// Fluent builder for world generation pipelines (TD_027).
/// Provides preset configurations (Fast Preview, High Quality) and custom stage composition.
/// </summary>
/// <remarks>
/// Design Patterns:
/// - Builder Pattern: Fluent API for step-by-step construction
/// - Strategy Pattern: Swappable plate simulator (IPlateSimulator)
/// - Preset Pattern: Pre-configured pipelines for common use cases
///
/// Usage Examples:
///
/// **Fast Preview Preset** (one-liner):
/// <code>
/// var pipeline = new PipelineBuilder()
///     .UseFastPreviewPreset(services)
///     .Build(logger);
/// </code>
///
/// **High Quality Preset** (one-liner):
/// <code>
/// var pipeline = new PipelineBuilder()
///     .UseHighQualityPreset(services, iterations: 5)
///     .Build(logger);
/// </code>
///
/// **Custom Configuration** (researcher/advanced):
/// <code>
/// var pipeline = new PipelineBuilder()
///     .UsePlateSimulator(new WorldEngineSimulator(...))  // Custom plate algorithm
///     .UseSinglePassMode()
///     .AddFoundationStage(new PlateGenerationStage(...))
///     .AddFoundationStage(new ElevationPostProcessStage(...))
///     .AddFeedbackStage(new TemperatureStage(...))
///     // ... custom stage order
///     .Build(logger);
/// </code>
///
/// Why Builder Pattern?
/// - **Solves 3 real requirements** (TD_027 backlog):
///   1. Plate algorithm swapping (Strategy pattern integration)
///   2. Mode selection with presets (VS_031 debug panel needs dropdown)
///   3. Custom pipelines (researchers can experiment with stage orders)
/// - **Hides complexity**: Presets abstract common patterns (90% use case)
/// - **Enables experimentation**: Custom builders for research (10% use case)
/// </remarks>
public class PipelineBuilder
{
    private IPlateSimulator? _plateSimulator;
    private ApplicationPipelineMode _mode = ApplicationPipelineMode.SinglePass;
    private int _feedbackIterations = 3;
    private readonly List<IPipelineStage> _foundationStages = new();
    private readonly List<IPipelineStage> _feedbackStages = new();

    /// <summary>
    /// Internal pipeline mode (different from public PipelineMode enum in Application layer).
    /// Used only for builder configuration logic.
    /// </summary>
    private enum ApplicationPipelineMode
    {
        SinglePass,
        Iterative
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PRESET CONFIGURATIONS (90% use case - one-liner setup)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fast Preview preset: SinglePassPipeline with default stages.
    /// Performance: ~2s for 512×512 map.
    /// Use case: Development, testing, quick world previews.
    /// </summary>
    /// <param name="services">DI container for resolving stages</param>
    /// <returns>Builder with Fast Preview configuration</returns>
    public PipelineBuilder UseFastPreviewPreset(IServiceProvider services)
    {
        UsePlateSimulator(services.GetRequiredService<IPlateSimulator>());
        UseSinglePassMode();
        UseDefaultStages(services);
        return this;
    }

    /// <summary>
    /// High Quality preset: IterativePipeline with feedback loops.
    /// Performance: ~6-10s for 512×512 map (3-5 iterations).
    /// Use case: Final production worlds, maximum fidelity.
    /// </summary>
    /// <param name="services">DI container for resolving stages</param>
    /// <param name="iterations">Feedback loop iterations (default: 3, recommended: 3-5)</param>
    /// <returns>Builder with High Quality configuration</returns>
    public PipelineBuilder UseHighQualityPreset(IServiceProvider services, int iterations = 3)
    {
        UsePlateSimulator(services.GetRequiredService<IPlateSimulator>());
        UseIterativeMode(iterations);
        UseDefaultStages(services);
        return this;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONFIGURATION API (custom pipelines for research/experimentation)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Configures plate simulator (Strategy pattern).
    /// Enables A/B testing of different plate algorithms.
    /// </summary>
    /// <param name="simulator">Plate simulator implementation (NativePlateSimulator, WorldEngineSimulator, etc.)</param>
    public PipelineBuilder UsePlateSimulator(IPlateSimulator simulator)
    {
        _plateSimulator = simulator;
        return this;
    }

    /// <summary>
    /// Configures single-pass mode (Climate → Erosion, one iteration).
    /// Fast preview mode (~2s for 512×512).
    /// </summary>
    public PipelineBuilder UseSinglePassMode()
    {
        _mode = ApplicationPipelineMode.SinglePass;
        _feedbackIterations = 1;
        return this;
    }

    /// <summary>
    /// Configures iterative mode ((Erosion → Climate) × N iterations).
    /// High-quality mode (~6-10s for 512×512).
    /// </summary>
    /// <param name="iterations">Number of feedback loop iterations (recommended: 3-5)</param>
    public PipelineBuilder UseIterativeMode(int iterations = 3)
    {
        if (iterations < 1)
            throw new ArgumentException("Iterations must be >= 1", nameof(iterations));

        _mode = ApplicationPipelineMode.Iterative;
        _feedbackIterations = iterations;
        return this;
    }

    /// <summary>
    /// Adds a foundation stage (runs once, never repeats).
    /// Foundation stages: Plate Generation, Elevation Post-Processing.
    /// </summary>
    public PipelineBuilder AddFoundationStage(IPipelineStage stage)
    {
        _foundationStages.Add(stage);
        return this;
    }

    /// <summary>
    /// Adds a feedback stage (repeats in iterative mode).
    /// Feedback stages: Temperature, Precipitation, Rain Shadow, Coastal Moisture, D-8 Flow.
    /// </summary>
    public PipelineBuilder AddFeedbackStage(IPipelineStage stage)
    {
        _feedbackStages.Add(stage);
        return this;
    }

    /// <summary>
    /// Configures default stages for standard pipeline (used by presets).
    /// Foundation stages: PlateGeneration, ElevationPostProcess
    /// Feedback stages: Temperature, Precipitation, RainShadow, CoastalMoisture, D8Flow
    /// </summary>
    /// <param name="services">DI container for resolving stage dependencies (loggers)</param>
    public PipelineBuilder UseDefaultStages(IServiceProvider services)
    {
        // Foundation stages (run once)
        _foundationStages.Clear();
        _foundationStages.Add(new PlateGenerationStage(
            _plateSimulator ?? services.GetRequiredService<IPlateSimulator>(),
            services.GetRequiredService<ILogger<PlateGenerationStage>>()));

        _foundationStages.Add(new ElevationPostProcessStage(
            services.GetRequiredService<ILogger<ElevationPostProcessStage>>()));

        // Feedback stages (repeat in iterative mode)
        _feedbackStages.Clear();
        _feedbackStages.Add(new TemperatureStage(
            services.GetRequiredService<ILogger<TemperatureStage>>()));

        _feedbackStages.Add(new PrecipitationStage(
            services.GetRequiredService<ILogger<PrecipitationStage>>()));

        _feedbackStages.Add(new RainShadowStage(
            services.GetRequiredService<ILogger<RainShadowStage>>()));

        _feedbackStages.Add(new CoastalMoistureStage(
            services.GetRequiredService<ILogger<CoastalMoistureStage>>()));

        _feedbackStages.Add(new D8FlowStage(
            services.GetRequiredService<ILogger<D8FlowStage>>()));

        return this;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BUILD (constructs final pipeline based on configuration)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the configured pipeline (SinglePassPipeline or IterativePipeline).
    /// </summary>
    /// <param name="logger">Logger for pipeline orchestration</param>
    /// <returns>Configured pipeline ready for world generation</returns>
    /// <exception cref="InvalidOperationException">If configuration is invalid</exception>
    public IWorldGenerationPipeline Build(ILogger logger)
    {
        // Validation
        if (_plateSimulator == null && _foundationStages.Count == 0)
        {
            throw new InvalidOperationException(
                "PipelineBuilder requires either UsePlateSimulator() or AddFoundationStage() to be called");
        }

        if (_foundationStages.Count == 0 && _feedbackStages.Count == 0)
        {
            throw new InvalidOperationException(
                "PipelineBuilder requires at least one stage (call UseDefaultStages() or add custom stages)");
        }

        // Build pipeline based on mode
        return _mode switch
        {
            ApplicationPipelineMode.SinglePass => BuildSinglePassPipeline(logger),
            ApplicationPipelineMode.Iterative => BuildIterativePipeline(logger),
            _ => throw new InvalidOperationException($"Unknown pipeline mode: {_mode}")
        };
    }

    /// <summary>
    /// Builds SinglePassPipeline (all stages in sequence, no iteration).
    /// </summary>
    private IWorldGenerationPipeline BuildSinglePassPipeline(ILogger logger)
    {
        // Combine foundation + feedback stages (single pass execution)
        var allStages = new List<IPipelineStage>();
        allStages.AddRange(_foundationStages);
        allStages.AddRange(_feedbackStages);

        return new SinglePassPipeline(
            allStages,
            logger as ILogger<SinglePassPipeline> ?? throw new ArgumentException(
                "Logger must be ILogger<SinglePassPipeline>", nameof(logger)));
    }

    /// <summary>
    /// Builds IterativePipeline (foundation runs once, feedback loops N times).
    /// </summary>
    private IWorldGenerationPipeline BuildIterativePipeline(ILogger logger)
    {
        // Validation: Need exactly 2 foundation stages and 5 feedback stages for default pipeline
        if (_foundationStages.Count < 2)
        {
            throw new InvalidOperationException(
                "IterativePipeline requires at least 2 foundation stages (PlateGeneration, ElevationPostProcess)");
        }

        if (_feedbackStages.Count == 0)
        {
            throw new InvalidOperationException(
                "IterativePipeline requires feedback stages (Temperature, Precipitation, RainShadow, CoastalMoisture, D8Flow)");
        }

        return new IterativePipeline(
            plateGenerationStage: _foundationStages[0],      // Stage 0
            elevationPostProcessStage: _foundationStages[1], // Stage 1
            feedbackStages: _feedbackStages,                 // Stages 2-6 (loop)
            logger: logger as ILogger<IterativePipeline> ?? throw new ArgumentException(
                "Logger must be ILogger<IterativePipeline>", nameof(logger)));
    }
}
