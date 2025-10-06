using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using MediatR;

namespace Darklands.Core.Features.WorldGen.Application.Commands;

/// <summary>
/// Command to generate a complete world using plate tectonics simulation.
/// Returns heightmap, ocean mask, climate data, and biome classification.
/// </summary>
public record GenerateWorldCommand : IRequest<Result<PlateSimulationResult>>
{
    /// <summary>Random seed for reproducible world generation</summary>
    public int Seed { get; init; }

    /// <summary>World map size (square maps only)</summary>
    public int WorldSize { get; init; } = 512;

    /// <summary>Number of tectonic plates (more = smaller continents)</summary>
    public int PlateCount { get; init; } = 10;

    public GenerateWorldCommand(int seed, int worldSize = 512, int plateCount = 10)
    {
        Seed = seed;
        WorldSize = worldSize;
        PlateCount = plateCount;
    }
}
