using System;
using System.Collections.Generic;
using LanguageExt;
using Darklands.Domain.Combat;
using Darklands.Domain.Grid;

namespace Darklands.Application.Combat.Services
{
    /// <summary>
    /// Service interface for managing the combat timeline and turn scheduling.
    /// Provides operations for scheduling actors and processing turns in deterministic order.
    /// </summary>
    public interface ICombatSchedulerService
    {
        /// <summary>
        /// Schedules an actor to act at a specific time in the combat timeline.
        /// </summary>
        /// <param name="actorId">The unique identifier of the actor</param>
        /// <param name="position">The actor's position on the grid</param>
        /// <param name="nextTurn">When the actor should next act</param>
        /// <returns>Success or failure with error details</returns>
        Fin<Unit> ScheduleActor(ActorId actorId, Position position, TimeUnit nextTurn);

        /// <summary>
        /// Processes the next turn by removing and returning the next scheduled actor.
        /// </summary>
        /// <returns>Some(actorId) if an actor was scheduled, None if timeline is empty</returns>
        Fin<Option<Guid>> ProcessNextTurn();

        /// <summary>
        /// Gets the current turn order without modifying the scheduler state.
        /// </summary>
        /// <returns>Read-only list of all scheduled entities in turn order</returns>
        Fin<IReadOnlyList<ISchedulable>> GetTurnOrder();

        /// <summary>
        /// Gets the number of currently scheduled actors.
        /// </summary>
        int GetScheduledCount();

        /// <summary>
        /// Removes all scheduled actors from the timeline.
        /// </summary>
        void ClearSchedule();

        /// <summary>
        /// Removes a specific actor from the combat timeline.
        /// Used when actors die or leave combat.
        /// </summary>
        /// <param name="actorId">The unique identifier of the actor to remove</param>
        /// <returns>True if actor was removed, false if not found</returns>
        bool RemoveActor(ActorId actorId);
    }
}
