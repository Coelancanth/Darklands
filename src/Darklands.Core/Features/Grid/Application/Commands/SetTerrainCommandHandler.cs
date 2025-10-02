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
        // BULK OPERATION: Called hundreds of times during grid initialization
        // Only log errors/warnings, skip debug spam

        // Validate position
        if (!_gridMap.IsValidPosition(request.Position))
        {
            var error = $"Position ({request.Position.X}, {request.Position.Y}) is outside grid bounds";
            _logger.LogWarning("SetTerrain failed: {Error}", error);
            return Task.FromResult(Result.Failure(error));
        }

        // Set terrain (errors logged by GridMap if needed)
        var result = _gridMap.SetTerrain(request.Position, request.TerrainType);

        return Task.FromResult(result);
    }
}
