using CSharpFunctionalExtensions;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Grid.Application.Commands;

/// <summary>
/// Handler for RegisterActorCommand.
/// Validates position and registers actor in the position service.
/// </summary>
public class RegisterActorCommandHandler : IRequestHandler<RegisterActorCommand, Result>
{
    private readonly GridMap _gridMap;
    private readonly IActorPositionService _actorPositionService;
    private readonly ILogger<RegisterActorCommandHandler> _logger;

    public RegisterActorCommandHandler(
        GridMap gridMap,
        IActorPositionService actorPositionService,
        ILogger<RegisterActorCommandHandler> logger)
    {
        _gridMap = gridMap;
        _actorPositionService = actorPositionService;
        _logger = logger;
    }

    public Task<Result> Handle(RegisterActorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Registering actor {ActorId} at position ({X}, {Y})",
            request.ActorId,
            request.InitialPosition.X,
            request.InitialPosition.Y);

        // Validate position is within grid bounds
        if (!_gridMap.IsValidPosition(request.InitialPosition))
        {
            var error = $"Position ({request.InitialPosition.X}, {request.InitialPosition.Y}) is outside grid bounds";
            _logger.LogWarning("RegisterActor failed: {Error}", error);
            return Task.FromResult(Result.Failure(error));
        }

        // Register actor (SetPosition allows both new and updates)
        var result = _actorPositionService.SetPosition(request.ActorId, request.InitialPosition);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Successfully registered actor {ActorId} at ({X}, {Y})",
                request.ActorId,
                request.InitialPosition.X,
                request.InitialPosition.Y);
        }

        return Task.FromResult(result);
    }
}
