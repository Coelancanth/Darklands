using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Application.Actor.Services;
using static LanguageExt.Prelude;

namespace Darklands.Application.Actor.Commands
{
    /// <summary>
    /// Handler for DamageActorCommand - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class DamageActorCommandHandler : IRequestHandler<DamageActorCommand, Fin<LanguageExt.Unit>>
    {
        private readonly IActorStateService _actorStateService;
        private readonly ICategoryLogger _logger;

        public DamageActorCommandHandler(
            IActorStateService actorStateService,
            ICategoryLogger logger)
        {
            _actorStateService = actorStateService;
            _logger = logger;
        }

        public Task<Fin<LanguageExt.Unit>> Handle(DamageActorCommand request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Command, "Processing DamageActorCommand for ActorId: {ActorId}, Damage: {Damage}, Source: {Source}",
                request.ActorId, request.Damage, request.Source ?? "Unknown");

            // Step 1: Validate damage amount
            if (request.Damage < 0)
            {
                var error = Error.New("INVALID_DAMAGE: Damage amount cannot be negative");
                _logger.Log(LogLevel.Warning, LogCategory.Command, "Invalid damage amount {Damage} for ActorId {ActorId}: {Error}",
                    request.Damage, request.ActorId, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            // Step 2: Get current actor
            var actorOption = _actorStateService.GetActor(request.ActorId);
            if (actorOption.IsNone)
            {
                var error = Error.New($"ACTOR_NOT_FOUND: Actor {request.ActorId} not found");
                _logger.Log(LogLevel.Warning, LogCategory.Command, "Actor not found for ActorId {ActorId}: {Error}", request.ActorId, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            var currentActor = actorOption.Match(
                Some: actor => actor,
                None: () => throw new InvalidOperationException("Actor should exist at this point")
            );

            // Step 3: Apply damage through service
            var damageResult = _actorStateService.DamageActor(request.ActorId, request.Damage);
            if (damageResult.IsFail)
            {
                var error = damageResult.Match<Error>(
                    Succ: _ => Error.New("UNKNOWN: Unknown error"),
                    Fail: e => e
                );
                _logger.Log(LogLevel.Error, LogCategory.Command, "Failed to damage actor {ActorId}: {Error}", request.ActorId, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            var damagedActor = damageResult.Match(
                Succ: actor => actor,
                Fail: _ => throw new InvalidOperationException("Result should be success at this point")
            );

            // Log the damage result
            if (damagedActor.Health.IsDead)
            {
                _logger.Log(LogLevel.Information, LogCategory.Combat, "Actor {ActorId} died from {Damage} damage from {Source}. Final health: {Health}",
                    request.ActorId, request.Damage, request.Source ?? "Unknown", damagedActor.Health);
            }
            else
            {
                _logger.Log(LogLevel.Debug, LogCategory.Combat, "Actor {ActorId} took {Damage} damage from {Source}. Health: {PreviousHealth} → {NewHealth}",
                    request.ActorId, request.Damage, request.Source ?? "Unknown",
                    currentActor.Health, damagedActor.Health);
            }

            return Task.FromResult(FinSucc(LanguageExt.Unit.Default));
        }
    }
}
