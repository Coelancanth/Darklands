using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Domain.ValueObjects;
using LanguageExt;
using MediatR;

namespace Darklands.Tactical.Application.Features.Combat.Scheduling;

/// <summary>
/// Command to process the next turn in combat, determining which actor should act.
/// </summary>
public sealed record ProcessNextTurnCommand : IRequest<Fin<TurnResult>>
{
    /// <summary>
    /// The current game time.
    /// </summary>
    public TimeUnit CurrentTime { get; }

    /// <summary>
    /// Whether to automatically execute the turn for AI actors.
    /// </summary>
    public bool AutoExecuteAI { get; }

    public ProcessNextTurnCommand(TimeUnit currentTime, bool autoExecuteAI = false)
    {
        CurrentTime = currentTime;
        AutoExecuteAI = autoExecuteAI;
    }
}

/// <summary>
/// Result of processing the next turn.
/// </summary>
public sealed record TurnResult
{
    /// <summary>
    /// The ID of the actor whose turn it is.
    /// </summary>
    public EntityId ActiveActorId { get; }

    /// <summary>
    /// The name of the active actor.
    /// </summary>
    public string ActiveActorName { get; }

    /// <summary>
    /// The time when this turn occurs.
    /// </summary>
    public TimeUnit TurnTime { get; }

    /// <summary>
    /// Available actions for the active actor.
    /// </summary>
    public IReadOnlyList<CombatAction> AvailableActions { get; }

    /// <summary>
    /// Whether this is a player-controlled actor.
    /// </summary>
    public bool IsPlayerControlled { get; }

    /// <summary>
    /// Current round number.
    /// </summary>
    public int RoundNumber { get; }

    public TurnResult(
        EntityId activeActorId,
        string activeActorName,
        TimeUnit turnTime,
        IReadOnlyList<CombatAction> availableActions,
        bool isPlayerControlled,
        int roundNumber)
    {
        ActiveActorId = activeActorId;
        ActiveActorName = activeActorName;
        TurnTime = turnTime;
        AvailableActions = availableActions;
        IsPlayerControlled = isPlayerControlled;
        RoundNumber = roundNumber;
    }
}

/// <summary>
/// Command to schedule an actor's next turn.
/// </summary>
public sealed record ScheduleActorCommand : IRequest<Fin<LanguageExt.Unit>>
{
    /// <summary>
    /// The actor to schedule.
    /// </summary>
    public EntityId ActorId { get; }

    /// <summary>
    /// The time when the actor should next act.
    /// </summary>
    public TimeUnit NextActionTime { get; }

    /// <summary>
    /// Priority for tie-breaking (lower is higher priority).
    /// </summary>
    public int Priority { get; }

    public ScheduleActorCommand(EntityId actorId, TimeUnit nextActionTime, int priority = 0)
    {
        ActorId = actorId;
        NextActionTime = nextActionTime;
        Priority = priority;
    }
}
