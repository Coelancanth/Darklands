using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Grid.Services;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Grid.Commands
{
    /// <summary>
    /// Handler for MoveActorCommand - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class MoveActorCommandHandler : IRequestHandler<MoveActorCommand, Fin<LanguageExt.Unit>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly ILogger _logger;

        public MoveActorCommandHandler(
            IGridStateService gridStateService,
            ILogger logger)
        {
            _gridStateService = gridStateService;
            _logger = logger;
        }

        public Task<Fin<LanguageExt.Unit>> Handle(MoveActorCommand request, CancellationToken cancellationToken)
        {
            _logger?.Debug("Processing MoveActorCommand for ActorId: {ActorId} to Position: {ToPosition}",
                request.ActorId, request.ToPosition);

            // Step 1: Get current actor position
            var currentPositionOption = _gridStateService.GetActorPosition(request.ActorId);
            if (currentPositionOption.IsNone)
            {
                var error = Error.New($"ACTOR_NOT_FOUND: Actor {request.ActorId} not found on grid");
                _logger?.Warning("Actor not found for ActorId {ActorId}: {Error}", request.ActorId, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            var currentPosition = currentPositionOption.Match(pos => pos, () => throw new InvalidOperationException());

            // Step 2: Validate move
            var validationResult = _gridStateService.ValidateMove(currentPosition, request.ToPosition);
            if (validationResult.IsFail)
            {
                var error = validationResult.Match<Error>(Succ: _ => Error.New("UNKNOWN: Unknown error"), Fail: e => e);
                _logger?.Warning("Validation failed for MoveActorCommand: {Error}", error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            // Step 3: Execute move
            var moveResult = _gridStateService.MoveActor(request.ActorId, request.ToPosition);
            if (moveResult.IsFail)
            {
                var error = moveResult.Match<Error>(Succ: _ => Error.New("UNKNOWN: Unknown error"), Fail: e => e);
                _logger?.Error("Failed to move actor {ActorId}: {Error}", request.ActorId, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            _logger?.Debug("Successfully moved actor {ActorId} from {FromPosition} to {ToPosition}",
                request.ActorId, currentPosition, request.ToPosition);

            return Task.FromResult(FinSucc(LanguageExt.Unit.Default));
        }
    }
}
