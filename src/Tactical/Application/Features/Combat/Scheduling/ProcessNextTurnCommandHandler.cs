using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Application.Features.Combat.Services;
using Darklands.Tactical.Domain.Aggregates.Actors;
using Darklands.Tactical.Domain.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
// using Microsoft.Extensions.Logging; // TODO: Re-enable after logging package issues resolved
using static LanguageExt.Prelude;

namespace Darklands.Tactical.Application.Features.Combat.Scheduling;

/// <summary>
/// Handles processing of the next turn in combat.
/// 
/// TD_043: No longer implements IRequestHandler to avoid MediatR auto-discovery.
/// Called directly by CombatSwitchAdapter when using tactical implementation.
/// </summary>
public sealed class ProcessNextTurnCommandHandler
{
    private readonly ICombatSchedulerService _scheduler;
    private readonly IActorRepository _actorRepository;
    // private readonly ILogger<ProcessNextTurnCommandHandler> _logger; // TODO: Re-enable after logging package issues resolved

    public ProcessNextTurnCommandHandler(
        ICombatSchedulerService scheduler,
        IActorRepository actorRepository)
        // ILogger<ProcessNextTurnCommandHandler> logger) // TODO: Re-enable after logging package issues resolved
    {
        _scheduler = scheduler;
        _actorRepository = actorRepository;
        // _logger = logger; // TODO: Re-enable after logging package issues resolved
    }

    public async Task<Fin<TurnResult>> Handle(ProcessNextTurnCommand command, CancellationToken cancellationToken)
    {
        // _logger.LogDebug("Processing next turn at time {CurrentTime}", command.CurrentTime); // TODO: Re-enable after logging package issues resolved

        // Get the next actor to act
        var nextActorResult = await _scheduler.GetNextActorAsync(command.CurrentTime);

        if (nextActorResult.IsFail)
        {
            // _logger.LogWarning("Failed to get next actor from scheduler"); // TODO: Re-enable after logging package issues resolved
            return nextActorResult.Match<Fin<TurnResult>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: err => FinFail<TurnResult>(err)
            );
        }

        var actorId = nextActorResult.Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException()
        );

        return await PrepareActorTurn(actorId, command.CurrentTime);
    }

    private async Task<Fin<TurnResult>> PrepareActorTurn(EntityId actorId, TimeUnit currentTime)
    {
        // _logger.LogDebug("Preparing turn for actor {ActorId}", actorId); // TODO: Re-enable after logging package issues resolved
        var actorResult = await _actorRepository.GetByIdAsync(actorId);

        if (actorResult.IsFail)
        {
            // _logger.LogWarning("Failed to retrieve actor {ActorId}", actorId); // TODO: Re-enable after logging package issues resolved
            return actorResult.Match<Fin<TurnResult>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: err => FinFail<TurnResult>(err)
            );
        }

        var actor = actorResult.Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException()
        );

        if (!actor.CanAct)
        {
            // _logger.LogDebug("Actor {ActorName} cannot act, skipping turn", actor.Name); // TODO: Re-enable after logging package issues resolved
            // Schedule next turn for this actor and process next
            await _scheduler.ScheduleActorAsync(actor.Id, currentTime + TimeUnit.QuickAction);

            // Recursively get the next actor
            var nextResult = await _scheduler.GetNextActorAsync(currentTime);
            if (nextResult.IsFail)
                return nextResult.Match<Fin<TurnResult>>(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: err => FinFail<TurnResult>(err)
                );

            var nextId = nextResult.Match(
                Succ: x => x,
                Fail: _ => throw new InvalidOperationException()
            );

            return await PrepareActorTurn(nextId, currentTime);
        }

        // Generate available actions for the actor
        var availableActions = GenerateAvailableActions(actor);
        // _logger.LogInformation("Turn prepared for {ActorName} with {ActionCount} available actions",
        //     actor.Name, availableActions.Count); // TODO: Re-enable after logging package issues resolved

        // Calculate round number (every 10 turns = 1 round)
        var roundNumber = currentTime.ToTurns() / 10 + 1;

        return FinSucc(new TurnResult(
            actor.Id,
            actor.Name,
            currentTime,
            availableActions,
            IsPlayerControlled(actor), // TODO: Implement player/AI detection
            roundNumber
        ));
    }

    private List<CombatAction> GenerateAvailableActions(Actor actor)
    {
        var actions = new List<CombatAction>();

        // Basic actions always available
        actions.Add(CombatAction.Wait(actor.Id));

        if (actor.CanAct)
        {
            // Add attack action (requires target selection)
            // Note: Actual targets would be determined by the UI/presenter
            actions.Add(CombatAction.Attack(actor.Id, EntityId.New(), actor.AttackPower));

            // Add defend action
            actions.Add(CombatAction.Defend(actor.Id));
        }

        return actions;
    }

    private bool IsPlayerControlled(Actor actor)
    {
        // TODO: Implement proper player/AI detection
        // For now, assume actors with certain name patterns are player-controlled
        return actor.Name.StartsWith("Player") || actor.Name.StartsWith("Hero");
    }
}

/// <summary>
/// Handles scheduling an actor's next turn.
/// </summary>
public sealed class ScheduleActorCommandHandler : IRequestHandler<ScheduleActorCommand, Fin<LanguageExt.Unit>>
{
    private readonly ICombatSchedulerService _scheduler;

    public ScheduleActorCommandHandler(
        ICombatSchedulerService scheduler)
    {
        _scheduler = scheduler;
    }

    public async Task<Fin<LanguageExt.Unit>> Handle(ScheduleActorCommand command, CancellationToken cancellationToken)
    {
        // Schedule actor

        return await _scheduler.ScheduleActorAsync(command.ActorId, command.NextActionTime, command.Priority);
    }
}
