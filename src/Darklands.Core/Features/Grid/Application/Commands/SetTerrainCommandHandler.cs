using CSharpFunctionalExtensions;
using Darklands.Core.Features.Grid.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Grid.Application.Commands;

/// <summary>
/// Handler for SetTerrainCommand.
/// Validates position and delegates terrain modification to GridMap.
/// </summary>
public class SetTerrainCommandHandler : IRequestHandler<SetTerrainCommand, Result>
{
    private readonly GridMap _gridMap;
    private readonly ILogger<SetTerrainCommandHandler> _logger;

    public SetTerrainCommandHandler(
        GridMap gridMap,
        ILogger<SetTerrainCommandHandler> logger)
    {
        _gridMap = gridMap;
        _logger = logger;
    }

    public Task<Result> Handle(SetTerrainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Setting terrain at ({X}, {Y}) to {TerrainType}",
            request.Position.X,
            request.Position.Y,
            request.TerrainType);

        // Validate position
        if (!_gridMap.IsValidPosition(request.Position))
        {
            var error = $"Position ({request.Position.X}, {request.Position.Y}) is outside grid bounds";
            _logger.LogWarning("SetTerrain failed: {Error}", error);
            return Task.FromResult(Result.Failure(error));
        }

        // Set terrain
        var result = _gridMap.SetTerrain(request.Position, request.TerrainType);

        if (result.IsSuccess)
        {
            _logger.LogDebug("Terrain set successfully");
        }

        return Task.FromResult(result);
    }
}
