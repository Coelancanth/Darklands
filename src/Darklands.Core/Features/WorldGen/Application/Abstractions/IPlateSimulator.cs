using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Core.Features.WorldGen.Application.Abstractions;

/// <summary>
/// Abstraction for plate tectonics simulation and world generation.
/// Hides native library complexity behind clean interface.
/// </summary>
public interface IPlateSimulator
{
    /// <summary>
    /// Generates complete world with terrain, climate, and biomes.
    /// </summary>
    /// <param name="parameters">Simulation parameters (seed, size, etc.)</param>
    /// <returns>
    /// Success: PlateSimulationResult with all maps.
    /// Failure: ERROR_WORLDGEN_* translation key.
    /// </returns>
    /// <remarks>
    /// Process:
    /// 1. Run native plate tectonics simulation -> raw heightmap
    /// 2. Post-process elevation (center land, add noise, flood fill oceans)
    /// 3. Calculate precipitation (latitude-based)
    /// 4. Calculate temperature (latitude + elevation)
    /// 5. Classify biomes (Holdridge model)
    /// </remarks>
    Result<PlateSimulationResult> Generate(PlateSimulationParams parameters);
}
