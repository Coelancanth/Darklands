using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Application.Commands;

/// <summary>
/// Handles world generation by orchestrating plate tectonics simulation
/// and post-processing through IPlateSimulator.
/// </summary>
public class GenerateWorldCommandHandler : IRequestHandler<GenerateWorldCommand, Result<PlateSimulationResult>>
{
    private readonly IPlateSimulator _simulator;
    private readonly ILogger<GenerateWorldCommandHandler> _logger;

    public GenerateWorldCommandHandler(
        IPlateSimulator simulator,
        ILogger<GenerateWorldCommandHandler> logger)
    {
        _simulator = simulator;
        _logger = logger;
    }

    public async Task<Result<PlateSimulationResult>> Handle(
        GenerateWorldCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating world: seed={Seed}, size={Size}x{Size}, plates={Plates}",
            command.Seed, command.WorldSize, command.WorldSize, command.PlateCount);

        // Create simulation parameters
        var parameters = new PlateSimulationParams(
            seed: command.Seed,
            worldSize: command.WorldSize,
            plateCount: command.PlateCount);

        // Delegate to simulator (handles all plate tectonics, post-processing, climate, biomes)
        var result = _simulator.Generate(parameters);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "World generation complete: {Width}x{Height}, {BiomeCount} biome types classified",
                result.Value.Width, result.Value.Height, CountUniqueBiomes(result.Value));
        }
        else
        {
            _logger.LogError("World generation failed: {Error}", result.Error);
        }

        return await Task.FromResult(result);
    }

    private static int CountUniqueBiomes(PlateSimulationResult world)
    {
        var uniqueBiomes = new HashSet<Domain.BiomeType>();

        for (int y = 0; y < world.Height; y++)
            for (int x = 0; x < world.Width; x++)
                uniqueBiomes.Add(world.BiomeMap[y, x]);

        return uniqueBiomes.Count;
    }
}
