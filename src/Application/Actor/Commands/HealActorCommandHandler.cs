using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Actor.Services;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Actor.Commands
{
    /// <summary>
    /// Handler for HealActorCommand - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class HealActorCommandHandler : IRequestHandler<HealActorCommand, Fin<LanguageExt.Unit>>
    {
        private readonly IActorStateService _actorStateService;
        private readonly ILogger _logger;

        public HealActorCommandHandler(
            IActorStateService actorStateService,
            ILogger logger)
        {
            _actorStateService = actorStateService;
            _logger = logger;
        }

        public Task<Fin<LanguageExt.Unit>> Handle(HealActorCommand request, CancellationToken cancellationToken)
        {
            _logger?.Debug("Processing HealActorCommand for ActorId: {ActorId}, HealAmount: {HealAmount}, Source: {Source}",
                request.ActorId, request.HealAmount, request.Source ?? "Unknown");

            // Step 1: Validate heal amount
            if (request.HealAmount < 0)
            {
                var error = Error.New("INVALID_HEAL: Heal amount cannot be negative");
                _logger?.Warning("Invalid heal amount {HealAmount} for ActorId {ActorId}: {Error}", 
                    request.HealAmount, request.ActorId, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            // Step 2: Get current actor
            var actorOption = _actorStateService.GetActor(request.ActorId);
            if (actorOption.IsNone)
            {
                var error = Error.New($"ACTOR_NOT_FOUND: Actor {request.ActorId} not found");
                _logger?.Warning("Actor not found for ActorId {ActorId}: {Error}", request.ActorId, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            var currentActor = actorOption.Match(
                Some: actor => actor,
                None: () => throw new InvalidOperationException("Actor should exist at this point")
            );

            // Step 3: Check if actor is alive (cannot heal dead actors)
            if (currentActor.Health.IsDead)
            {
                var error = Error.New($"CANNOT_HEAL_DEAD: Cannot heal dead actor {request.ActorId}");
                _logger?.Warning("Attempted to heal dead actor {ActorId}: {Error}", request.ActorId, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            // Step 4: Apply healing through service
            var healResult = _actorStateService.HealActor(request.ActorId, request.HealAmount);
            if (healResult.IsFail)
            {
                var error = healResult.Match<Error>(
                    Succ: _ => Error.New("UNKNOWN: Unknown error"), 
                    Fail: e => e
                );
                _logger?.Error("Failed to heal actor {ActorId}: {Error}", request.ActorId, error.Message);
                return Task.FromResult(FinFail<LanguageExt.Unit>(error));
            }

            var healedActor = healResult.Match(
                Succ: actor => actor,
                Fail: _ => throw new InvalidOperationException("Result should be success at this point")
            );

            // Log the healing result
            var actualHealing = healedActor.Health.Current - currentActor.Health.Current;
            if (actualHealing > 0)
            {
                _logger?.Debug("Actor {ActorId} healed {ActualHealing} points from {Source}. Health: {PreviousHealth} → {NewHealth}",
                    request.ActorId, actualHealing, request.Source ?? "Unknown", 
                    currentActor.Health, healedActor.Health);
            }
            else
            {
                _logger?.Debug("Actor {ActorId} was already at full health, no healing applied. Health: {Health}",
                    request.ActorId, healedActor.Health);
            }

            return Task.FromResult(FinSucc(LanguageExt.Unit.Default));
        }
    }
}