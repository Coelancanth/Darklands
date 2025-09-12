using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Domain.ValueObjects;
using LanguageExt;

namespace Darklands.Tactical.Application.Features.Combat.Services;

/// <summary>
/// Service for managing turn order and scheduling in tactical combat.
/// </summary>
public interface ICombatSchedulerService
{
    /// <summary>
    /// Gets the next actor to act based on the current time.
    /// </summary>
    /// <param name="currentTime">The current game time.</param>
    /// <returns>The ID of the next actor to act, or an error if no actors are scheduled.</returns>
    Task<Fin<EntityId>> GetNextActorAsync(TimeUnit currentTime);

    /// <summary>
    /// Schedules an actor's next turn.
    /// </summary>
    /// <param name="actorId">The actor to schedule.</param>
    /// <param name="actionTime">When the actor should next act.</param>
    /// <param name="priority">Priority for tie-breaking (lower values act first).</param>
    /// <returns>Success or error.</returns>
    Task<Fin<Unit>> ScheduleActorAsync(EntityId actorId, TimeUnit actionTime, int priority = 0);

    /// <summary>
    /// Removes an actor from the schedule (e.g., when they die).
    /// </summary>
    /// <param name="actorId">The actor to remove.</param>
    /// <returns>Success or error.</returns>
    Task<Fin<Unit>> RemoveActorAsync(EntityId actorId);

    /// <summary>
    /// Clears all scheduled turns.
    /// </summary>
    /// <returns>Success or error.</returns>
    Task<Fin<Unit>> ClearScheduleAsync();

    /// <summary>
    /// Initializes the schedule with a set of actors.
    /// </summary>
    /// <param name="actorIds">The actors to schedule.</param>
    /// <param name="startTime">The starting time for combat.</param>
    /// <returns>Success or error.</returns>
    Task<Fin<Unit>> InitializeScheduleAsync(Seq<EntityId> actorIds, TimeUnit startTime);

    /// <summary>
    /// Gets all currently scheduled actors in order.
    /// </summary>
    /// <returns>Ordered sequence of scheduled actor turns.</returns>
    Task<Fin<Seq<ScheduledTurn>>> GetScheduleAsync();
}

/// <summary>
/// Represents a scheduled turn for an actor.
/// </summary>
public sealed record ScheduledTurn(
    EntityId ActorId,
    TimeUnit ActionTime,
    int Priority
)
{
    /// <summary>
    /// Compares scheduled turns for ordering.
    /// </summary>
    public static IComparer<ScheduledTurn> TimeComparer { get; } =
        Comparer<ScheduledTurn>.Create((a, b) =>
        {
            var timeComparison = a.ActionTime.CompareTo(b.ActionTime);
            if (timeComparison != 0) return timeComparison;

            // If times are equal, use priority (lower priority value acts first)
            var priorityComparison = a.Priority.CompareTo(b.Priority);
            if (priorityComparison != 0) return priorityComparison;

            // If still tied, use actor ID for consistent ordering
            return a.ActorId.Value.CompareTo(b.ActorId.Value);
        });
}
