using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Core.Domain.Debug;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Domain.Actor;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Grid.Commands
{
    /// <summary>
    /// Handler for SpawnDummyCommand - Creates and places dummy actors on the grid.
    /// Implements functional CQRS pattern with Fin&lt;T&gt; monads for error handling.
    /// </summary>
    public class SpawnDummyCommandHandler : IRequestHandler<SpawnDummyCommand, Fin<LanguageExt.Unit>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly IActorStateService _actorStateService;
        private readonly ICategoryLogger _logger;
        private readonly Domain.Common.IStableIdGenerator _idGenerator;

        public SpawnDummyCommandHandler(
            IGridStateService gridStateService,
            IActorStateService actorStateService,
            ICategoryLogger logger,
            Domain.Common.IStableIdGenerator idGenerator)
        {
            _gridStateService = gridStateService;
            _actorStateService = actorStateService;
            _logger = logger;
            _idGenerator = idGenerator ?? throw new System.ArgumentNullException(nameof(idGenerator));
        }

        public Task<Fin<LanguageExt.Unit>> Handle(SpawnDummyCommand request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Command, "Processing SpawnDummyCommand at Position: {Position}, MaxHealth: {MaxHealth}, Name: {Name}",
                request.Position, request.MaxHealth, request.Name);

            // Step 1: Validate target position is available
            var isEmpty = _gridStateService.IsPositionEmpty(request.Position);
            if (!isEmpty)
            {
                var error = Error.New($"POSITION_OCCUPIED: Position {request.Position} is already occupied");
                _logger.Log(LogLevel.Warning, LogCategory.Command, "Cannot spawn dummy at occupied position {Position}: {Error}", request.Position, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            // Step 2: Create DummyActor domain entity using injected ID generator
            var dummyResult = DummyActor.CreateAtFullHealth(ActorId.NewId(_idGenerator), request.MaxHealth, request.Name);
            if (dummyResult.IsFail)
            {
                var error = dummyResult.Match<Error>(Succ: _ => Error.New("UNKNOWN: Unknown error"), Fail: e => e);
                _logger.Log(LogLevel.Error, LogCategory.Command, "Failed to create DummyActor: {Error}", error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            var dummy = dummyResult.Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Should not reach here")
            );

            // Step 3: Register dummy in actor state service
            // Convert DummyActor to Actor for service registration
            var actorForRegistration = Darklands.Core.Domain.Actor.Actor.Create(dummy.Id, dummy.Health, dummy.Name);
            if (actorForRegistration.IsFail)
            {
                var error = actorForRegistration.Match<Error>(Succ: _ => Error.New("UNKNOWN: Unknown error"), Fail: e => e);
                _logger.Log(LogLevel.Error, LogCategory.Command, "Failed to convert dummy {ActorId} to Actor: {Error}", dummy.Id, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            var actor = actorForRegistration.Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Should not reach here")
            );

            var addActorResult = _actorStateService.AddActor(actor);
            if (addActorResult.IsFail)
            {
                var error = addActorResult.Match<Error>(Succ: _ => Error.New("UNKNOWN: Unknown error"), Fail: e => e);
                _logger.Log(LogLevel.Error, LogCategory.Command, "Failed to register dummy {ActorId} in actor state service: {Error}", dummy.Id, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            // Step 4: Add dummy to grid
            var addGridResult = _gridStateService.AddActorToGrid(dummy.Id, request.Position);
            if (addGridResult.IsFail)
            {
                // If grid placement fails, remove from actor state service to maintain consistency
                _ = _actorStateService.RemoveDeadActor(dummy.Id); // Best effort cleanup
                var error = addGridResult.Match<Error>(Succ: _ => Error.New("UNKNOWN: Unknown error"), Fail: e => e);
                _logger.Log(LogLevel.Error, LogCategory.Command, "Failed to add dummy {ActorId} to grid at {Position}: {Error}", dummy.Id, request.Position, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            _logger.Log(LogLevel.Information, LogCategory.Command, "Successfully spawned dummy {Name} ({ActorId}) at position {Position} with {Health} health",
                dummy.Name, dummy.Id, request.Position, dummy.Health.Maximum);

            return Task.FromResult(FinSucc(LanguageExt.Unit.Default));
        }
    }
}
