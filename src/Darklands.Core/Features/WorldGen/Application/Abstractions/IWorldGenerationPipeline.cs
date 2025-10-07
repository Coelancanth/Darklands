using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Core.Features.WorldGen.Application.Abstractions;

/// <summary>
/// Orchestrates world generation pipeline: native simulation + post-processing stages.
/// This is the high-level abstraction for complete world generation.
/// </summary>
/// <remarks>
/// Architecture:
/// - IWorldGenerationPipeline (this) → orchestrates full pipeline
/// - IPlateSimulator → native library wrapper (low-level)
///
/// Pipeline stages (incremental implementation per VS_022):
/// Stage 0 (current): Native simulation only (pass-through)
/// Stage 1 (Phase 1): Elevation normalization + ocean detection
/// Stage 2 (Phase 2): Temperature calculation
/// Stage 3 (Phase 3): Precipitation calculation
/// Stage 4+: Erosion, hydrology, biomes
/// </remarks>
public interface IWorldGenerationPipeline
{
    /// <summary>
    /// Generates complete world with terrain and optional post-processing.
    /// </summary>
    /// <param name="parameters">Simulation parameters (seed, size, etc.)</param>
    /// <returns>
    /// Success: WorldGenerationResult with all available data.
    /// Failure: ERROR_WORLDGEN_* translation key.
    /// </returns>
    /// <remarks>
    /// Current behavior (Phase 0):
    /// - Calls native plate simulator
    /// - Returns raw heightmap + plates (no post-processing)
    /// - OceanMask, TemperatureMap, PrecipitationMap are null
    ///
    /// Future behavior (as phases implement):
    /// - Will add normalization, climate, biomes
    /// - Optional fields become populated
    /// </remarks>
    Result<WorldGenerationResult> Generate(PlateSimulationParams parameters);
}
