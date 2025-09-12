using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Application.Features.Combat.Services;
using Darklands.Tactical.Domain.Aggregates.Actors;
using Darklands.Tactical.Domain.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Darklands.Tactical.Application.Features.Combat.Scheduling;

/// <summary>
/// Handles processing of the next turn in combat.
/// </summary>
public sealed class ProcessNextTurnCommandHandler : IRequestHandler<ProcessNextTurnCommand, Fin<TurnResult>>
{
    private readonly ICombatSchedulerService _scheduler;
    private readonly IActorRepository _actorRepository;
    private readonly ILogger<ProcessNextTurnCommandHandler> _logger;

    public ProcessNextTurnCommandHandler(
        ICombatSchedulerService scheduler,
        IActorRepository actorRepository,
        ILogger<ProcessNextTurnCommandHandler> logger)
    {
        _scheduler = scheduler;
        _actorRepository = actorRepository;
        _logger = logger;
    }

    public async Task<Fin<TurnResult>> Handle(ProcessNextTurnCommand command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing next turn at time {CurrentTime}", command.CurrentTime);

        // Get the next actor to act
        var nextActorResult = await _scheduler.GetNextActorAsync(command.CurrentTime);

        return await nextActorResult.Match(
            Succ: async actorId => await PrepareActorTurn(actorId, command.CurrentTime),
            Fail: error =>
            {
                _logger.LogWarning("Failed to get next actor: {Error}", error.Message);
                return FinFail<TurnResult>(error);
            }
        );
    }

    private async Task<Fin<TurnResult>> PrepareActorTurn(EntityId actorId, TimeUnit currentTime)
    {
        var actorResult = await _actorRepository.GetByIdAsync(actorId);

        return await actorResult.Match(
            Succ: async actor =>
            {
                if (!actor.CanAct)
                {
                    _logger.LogInformation("Actor {ActorName} cannot act, skipping turn", actor.Name);

                    // Schedule next turn for this actor and process next
                    await _scheduler.ScheduleActorAsync(actor.Id, currentTime + TimeUnit.QuickAction);

                    // Recursively get the next actor
                    var nextResult = await _scheduler.GetNextActorAsync(currentTime);
                    return await nextResult.Match(
                        Succ: async nextId => await PrepareActorTurn(nextId, currentTime),
                        Fail: error => FinFail<TurnResult>(error)
                    );
                }

                // Generate available actions for the actor
                var availableActions = GenerateAvailableActions(actor);

                // Calculate round number (every 10 turns = 1 round)
                var roundNumber = currentTime.ToTurns() / 10 + 1;

                _logger.LogInformation(
                    "Turn {Round}: {ActorName} (HP: {Health}/{MaxHealth}) is ready to act",
                    roundNumber, actor.Name, actor.Health, actor.MaxHealth);

                return FinSucc(new TurnResult(
                    actor.Id,
                    actor.Name,
                    currentTime,
                    availableActions,
                    IsPlayerControlled(actor), // TODO: Implement player/AI detection
                    roundNumber
                ));
            },
            Fail: error =>
            {
                _logger.LogError("Actor {ActorId} not found: {Error}", actorId, error.Message);
                return FinFail<TurnResult>(error);
            }
        );
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
    private readonly ILogger<ScheduleActorCommandHandler> _logger;

    public ScheduleActorCommandHandler(
        ICombatSchedulerService scheduler,
        ILogger<ScheduleActorCommandHandler> logger)
    {
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task<Fin<LanguageExt.Unit>> Handle(ScheduleActorCommand command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Scheduling actor {ActorId} for time {NextActionTime} with priority {Priority}",
            command.ActorId, command.NextActionTime, command.Priority);

        return await _scheduler.ScheduleActorAsync(command.ActorId, command.NextActionTime, command.Priority);
    }
}
