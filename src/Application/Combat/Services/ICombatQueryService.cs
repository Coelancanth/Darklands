using System.Collections.Generic;
using LanguageExt;
using Darklands.Domain.Grid;

namespace Darklands.Application.Combat.Services
{
    /// <summary>
    /// Composite query service that coordinates between ActorStateService and GridStateService
    /// to provide unified actor data combining health/stats with position information.
    /// This service maintains Single Source of Truth by querying each service separately.
    /// </summary>
    public interface ICombatQueryService
    {
        /// <summary>
        /// Gets complete actor information including health and current position.
        /// Combines data from ActorStateService (health) and GridStateService (position).
        /// </summary>
        /// <param name="actorId">The actor identifier</param>
        /// <returns>Complete actor data with position, or None if actor not found</returns>
        Option<ActorWithPosition> GetActorWithPosition(ActorId actorId);

        /// <summary>
        /// Gets all actors currently in combat with their positions.
        /// Useful for combat calculations, UI updates, and game state queries.
        /// </summary>
        /// <returns>Collection of all actors with their current positions</returns>
        IReadOnlyList<ActorWithPosition> GetAllActorsWithPositions();

        /// <summary>
        /// Gets actors within a specific radius of a position.
        /// Used for area-of-effect attacks and proximity-based queries.
        /// </summary>
        /// <param name="centerPosition">Center position to search from</param>
        /// <param name="radius">Maximum distance to include actors</param>
        /// <returns>Actors within the specified radius with their positions</returns>
        IReadOnlyList<ActorWithPosition> GetActorsInRadius(Position centerPosition, int radius);

        /// <summary>
        /// Gets actors adjacent to a specific position (including diagonals).
        /// Used for melee attack validation and adjacency checks.
        /// </summary>
        /// <param name="position">Position to check adjacency from</param>
        /// <returns>Adjacent actors with their positions</returns>
        IReadOnlyList<ActorWithPosition> GetAdjacentActors(Position position);

        /// <summary>
        /// Checks if any living actors are at the specified position.
        /// Combines position data with health status for accurate occupancy checks.
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if a living actor occupies the position, False otherwise</returns>
        bool IsPositionOccupiedByLivingActor(Position position);
    }

    /// <summary>
    /// Composite data structure combining Actor domain model with position data.
    /// This maintains separation of concerns while providing unified access.
    /// </summary>
    public sealed record ActorWithPosition
    {
        /// <summary>
        /// The complete actor domain model with health and stats.
        /// </summary>
        public Domain.Actor.Actor Actor { get; init; }

        /// <summary>
        /// Current position on the combat grid.
        /// </summary>
        public Position Position { get; init; }

        /// <summary>
        /// Convenience property for actor ID.
        /// </summary>
        public ActorId Id => Actor.Id;

        /// <summary>
        /// Convenience property for health status.
        /// </summary>
        public bool IsAlive => Actor.IsAlive;

        /// <summary>
        /// Private constructor to enforce factory method usage.
        /// </summary>
        private ActorWithPosition(Domain.Actor.Actor actor, Position position)
        {
            Actor = actor;
            Position = position;
        }

        /// <summary>
        /// Creates a new ActorWithPosition instance.
        /// </summary>
        /// <param name="actor">The actor domain model</param>
        /// <param name="position">The actor's current position</param>
        /// <returns>Combined actor and position data</returns>
        public static ActorWithPosition Create(Domain.Actor.Actor actor, Position position) =>
            new(actor, position);

        public override string ToString() => $"{Actor.Name} at {Position} ({Actor.Health})";
    }
}
