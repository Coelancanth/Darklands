using System.Collections.Generic;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Domain.Grid;
using Darklands.Domain.FogOfWar;
using Unit = LanguageExt.Unit;

namespace Darklands.Application.FogOfWar.Services
{
    /// <summary>
    /// Service for managing progressive movement and FOV revelation during actor movement.
    /// Orchestrates timer-based position advancement with FOV system coordination.
    /// Implements ADR-022 Two-Position Model where logical position is authoritative.
    /// </summary>
    public interface IMovementProgressionService
    {
        /// <summary>
        /// Starts a new movement progression for an actor along the specified path.
        /// Creates timer-based advancement that updates logical position cell-by-cell.
        /// Publishes RevealProgressionStarted notification for FOV coordination.
        /// </summary>
        /// <param name="actorId">Actor to start movement for</param>
        /// <param name="path">Complete path from current to destination position</param>
        /// <param name="millisecondsPerStep">Time between each position advance (default: 200ms)</param>
        /// <param name="currentTurn">Current game turn for event context</param>
        /// <returns>Success or error if movement cannot be started</returns>
        Fin<Unit> StartMovement(
            ActorId actorId,
            IReadOnlyList<Position> path,
            int millisecondsPerStep = 200,
            int currentTurn = 0);

        /// <summary>
        /// Cancels active movement progression for an actor.
        /// Actor remains at their current logical position (per ADR-022).
        /// Publishes RevealProgressionCompleted notification for cleanup.
        /// Used for ESC cancellation or movement redirection.
        /// </summary>
        /// <param name="actorId">Actor to cancel movement for</param>
        /// <param name="currentTurn">Current game turn for event context</param>
        /// <returns>Success or error if no active movement found</returns>
        Fin<Unit> CancelMovement(ActorId actorId, int currentTurn);

        /// <summary>
        /// Advances game time for all active movement progressions.
        /// Triggers logical position updates when enough time has elapsed.
        /// Publishes RevealPositionAdvanced notifications for FOV updates.
        /// Should be called from game timer/update loop.
        /// </summary>
        /// <param name="deltaMilliseconds">Time elapsed since last advance</param>
        /// <param name="currentTurn">Current game turn for event context</param>
        /// <returns>Number of position advancements that occurred</returns>
        Fin<int> AdvanceGameTime(int deltaMilliseconds, int currentTurn);

        /// <summary>
        /// Gets the current authoritative logical position for an actor.
        /// Returns current position in active progression or None if not moving.
        /// This is THE position used for FOV calculations (per ADR-022).
        /// </summary>
        /// <param name="actorId">Actor to get position for</param>
        /// <returns>Current logical position or None if not in progression</returns>
        Option<Position> GetCurrentPosition(ActorId actorId);

        /// <summary>
        /// Checks if an actor has an active movement progression.
        /// Used for state validation and movement conflict detection.
        /// </summary>
        /// <param name="actorId">Actor to check</param>
        /// <returns>True if actor is currently progressing through movement</returns>
        bool IsMoving(ActorId actorId);

        /// <summary>
        /// Gets all actors currently in movement progression.
        /// Used for debugging and state validation.
        /// </summary>
        /// <returns>Collection of actor IDs with active progressions</returns>
        IReadOnlyCollection<ActorId> GetMovingActors();

        /// <summary>
        /// Gets detailed progression state for an actor.
        /// Used for debugging and UI progress indicators.
        /// </summary>
        /// <param name="actorId">Actor to get progression for</param>
        /// <returns>Current progression state or None if not moving</returns>
        Option<RevealProgression> GetProgressionState(ActorId actorId);

        /// <summary>
        /// Clears all active movement progressions.
        /// Used for game state reset or error recovery.
        /// Publishes completion events for all cleared progressions.
        /// </summary>
        /// <param name="currentTurn">Current game turn for event context</param>
        /// <returns>Number of progressions that were cleared</returns>
        Fin<int> ClearAllProgressions(int currentTurn);
    }
}
