using CSharpFunctionalExtensions;
using Darklands.Core.Features.Grid.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Grid.Application.Commands;

/// <summary>
/// Handler for SetTerrainCommand.
/// Resolves terrain name to definition, validates position, and delegates to GridMap.
/// </summary>
/// <remarks>
/// ARCHITECTURE CHANGE (VS_019 Phase 1):
/// - NEW dependency: ITerrainRepository (resolves names to definitions)
/// - Command takes string name, handler resolves to TerrainDefinition
/// - GridMap receives TerrainDefinition (stores definitions directly in cells)
/// </remarks>
public class SetTerrainCommandHandler : IRequestHandler<SetTerrainCommand, Result>
{
    private readonly GridMap _gridMap;
    private readonly ITerrainRepository _terrainRepo;
    private readonly ILogger<SetTerrainCommandHandler> _logger;

    public SetTerrainCommandHandler(
        GridMap gridMap,
        ITerrainRepository terrainRepo,
        ILogger<SetTerrainCommandHandler> logger)
    {
        _gridMap = gridMap;
        _terrainRepo = terrainRepo;
        _logger = logger;
    }

    public Task<Result> Handle(SetTerrainCommand request, CancellationToken cancellationToken)
    {
        // BULK OPERATION: Called hundreds of times during grid initialization
        // Only log errors/warnings, skip debug spam

        // Resolve terrain name to definition
        var terrainResult = _terrainRepo.GetByName(request.TerrainName);
        if (terrainResult.IsFailure)
        {
            _logger.LogWarning("SetTerrain failed: Unknown terrain '{TerrainName}'", request.TerrainName);
            return Task.FromResult(Result.Failure(terrainResult.Error));
        }

        // Validate position
        if (!_gridMap.IsValidPosition(request.Position))
        {
            var error = $"Position ({request.Position.X}, {request.Position.Y}) is outside grid bounds";
            _logger.LogWarning("SetTerrain failed: {Error}", error);
            return Task.FromResult(Result.Failure(error));
        }

        // Set terrain (errors logged by GridMap if needed)
        var result = _gridMap.SetTerrain(request.Position, terrainResult.Value);

        return Task.FromResult(result);
    }
}
