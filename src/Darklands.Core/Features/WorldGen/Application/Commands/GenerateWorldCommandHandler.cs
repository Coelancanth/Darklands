using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Application.Commands;

/// <summary>
/// Handles world generation by orchestrating the pipeline:
/// native simulation + post-processing stages.
/// </summary>
public class GenerateWorldCommandHandler : IRequestHandler<GenerateWorldCommand, Result<WorldGenerationResult>>
{
    private readonly IWorldGenerationPipeline _pipeline;
    private readonly ILogger<GenerateWorldCommandHandler> _logger;

    public GenerateWorldCommandHandler(
        IWorldGenerationPipeline pipeline,
        ILogger<GenerateWorldCommandHandler> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<Result<WorldGenerationResult>> Handle(
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

        // Delegate to pipeline (native simulation + post-processing)
        var result = _pipeline.Generate(parameters);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "World generation complete: {Width}x{Height} world with {Stages} stage(s)",
                result.Value.Width, result.Value.Height,
                GetImplementedStageCount(result.Value));
        }
        else
        {
            _logger.LogError("World generation failed: {Error}", result.Error);
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// Counts how many optional pipeline stages are implemented (for logging).
    /// </summary>
    private static int GetImplementedStageCount(WorldGenerationResult result)
    {
        int count = 1; // Stage 0 (native) always present
        if (result.OceanMask != null) count++;
        if (result.TemperatureMap != null) count++;
        if (result.PrecipitationMap != null) count++;
        return count;
    }
}
