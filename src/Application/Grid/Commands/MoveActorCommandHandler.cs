using LanguageExt;
using LanguageExt.Common;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Darklands.Application.Common;
using Darklands.Application.Grid.Services;
using Darklands.Application.Actor.Services;
using Darklands.Application.Grid.Queries;
using Darklands.Application.Events;
using static LanguageExt.Prelude;

namespace Darklands.Application.Grid.Commands
{
    /// <summary>
    /// Handler for MoveActorCommand - Initiates step-by-step movement via domain events.
    /// Replaces instant teleportation with truthful domain-driven movement progression.
    ///
    /// New Architecture (TD_065):
    /// 1. Calculate path using A* pathfinding
    /// 2. Start movement in Actor domain (creates movement state)
    /// 3. Publish MovementStartedEvent for application coordination
    /// 4. GameLoop will advance movement step-by-step via ActorMovedEvents
    ///
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class MoveActorCommandHandler : IRequestHandler<MoveActorCommand, Fin<LanguageExt.Unit>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly IActorStateService _actorStateService;
        private readonly IMediator _mediator;
        private readonly ICategoryLogger _logger;

        public MoveActorCommandHandler(
            IGridStateService gridStateService,
            IActorStateService actorStateService,
            IMediator mediator,
            ICategoryLogger logger)
        {
            _gridStateService = gridStateService;
            _actorStateService = actorStateService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Fin<LanguageExt.Unit>> Handle(MoveActorCommand request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Gameplay,
                "Processing step-by-step MoveActorCommand for ActorId: {ActorId} to Position: {ToPosition}",
                request.ActorId, request.ToPosition);

            // Step 1: Get current actor and position
            var actorOption = _actorStateService.GetActor(request.ActorId);
            if (actorOption.IsNone)
            {
                var error = Error.New($"ACTOR_NOT_FOUND: Actor {request.ActorId} not found");
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor not found: {Error}", error.Message);
                return FinFail<LanguageExt.Unit>(error);
            }

            var actor = actorOption.IfNone(() => throw new System.InvalidOperationException());
            var currentPositionOption = _gridStateService.GetActorPosition(request.ActorId);
            if (currentPositionOption.IsNone)
            {
                var error = Error.New($"POSITION_NOT_FOUND: Actor {request.ActorId} position not found on grid");
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor position not found: {Error}", error.Message);
                return FinFail<LanguageExt.Unit>(error);
            }

            var currentPosition = currentPositionOption.IfNone(() => throw new System.InvalidOperationException());

            // Step 2: Check if actor is already moving
            if (actor.HasActivePath)
            {
                var error = Error.New($"ALREADY_MOVING: Actor {request.ActorId} is already moving");
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Movement blocked: {Error}", error.Message);
                return FinFail<LanguageExt.Unit>(error);
            }

            // Step 3: Calculate path using A* pathfinding
            var pathQuery = new CalculatePathQuery
            {
                FromPosition = currentPosition,
                ToPosition = request.ToPosition
            };

            var pathResult = await _mediator.Send(pathQuery, cancellationToken);
            if (pathResult.IsFail)
            {
                var error = pathResult.Match<Error>(Succ: _ => Error.New("UNKNOWN"), Fail: e => e);
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Pathfinding failed: {Error}", error.Message);
                return FinFail<LanguageExt.Unit>(error);
            }

            var pathSequence = pathResult.IfFail(error => throw new System.InvalidOperationException($"Pathfinding failed: {error.Message}"));
            if (pathSequence.IsEmpty)
            {
                var error = Error.New($"NO_PATH: No valid path from {currentPosition} to {request.ToPosition}");
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "No path found: {Error}", error.Message);
                return FinFail<LanguageExt.Unit>(error);
            }

            // Step 4: Start movement in domain (creates movement state)
            var path = pathSequence.ToImmutableList();
            var movementResult = actor.StartMovement(path);
            if (movementResult.IsFail)
            {
                var error = movementResult.Match<Error>(Succ: _ => Error.New("UNKNOWN"), Fail: e => e);
                _logger.Log(LogLevel.Error, LogCategory.Gameplay, "Failed to start movement: {Error}", error.Message);
                return FinFail<LanguageExt.Unit>(error);
            }

            var movingActor = movementResult.IfFail(error => throw new System.InvalidOperationException($"Movement start failed: {error.Message}"));

            // Step 5: Update actor state with movement
            var updateResult = _actorStateService.AddActor(movingActor); // This will overwrite existing
            if (updateResult.IsFail)
            {
                var error = updateResult.Match<Error>(Succ: _ => Error.New("UNKNOWN"), Fail: e => e);
                _logger.Log(LogLevel.Error, LogCategory.Gameplay, "Failed to update actor state: {Error}", error.Message);
                return FinFail<LanguageExt.Unit>(error);
            }

            // Step 6: Publish MovementStartedEvent for application coordination
            var movementStartedEvent = MovementStartedEvent.Create(request.ActorId, currentPosition, path);
            await _mediator.Publish(movementStartedEvent, cancellationToken);

            _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                "Movement initiated: Actor {ActorId} starting {Steps}-step path from {From} to {To}",
                request.ActorId, path.Count, currentPosition, request.ToPosition);

            return FinSucc(LanguageExt.Unit.Default);
        }
    }
}
