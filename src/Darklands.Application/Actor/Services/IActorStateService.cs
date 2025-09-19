using LanguageExt;
using Darklands.Domain.Grid;

namespace Darklands.Application.Actor.Services
{
    /// <summary>
    /// Service interface for managing actor state including health and combat status.
    /// Extends grid-based positioning with health management for tactical combat.
    /// </summary>
    public interface IActorStateService
    {
        /// <summary>
        /// Adds a new actor to the combat state.
        /// </summary>
        /// <param name="actor">The actor to add</param>
        /// <returns>Success unit or error if actor is invalid or already exists</returns>
        Fin<Unit> AddActor(Domain.Actor.Actor actor);

        /// <summary>
        /// Gets the complete actor state by ID.
        /// </summary>
        /// <param name="actorId">The actor identifier</param>
        /// <returns>Actor instance with current health and position, or None if not found</returns>
        Option<Domain.Actor.Actor> GetActor(ActorId actorId);

        /// <summary>
        /// Updates an actor's health state while preserving position and other properties.
        /// </summary>
        /// <param name="actorId">The actor identifier</param>
        /// <param name="newHealth">The new health state to apply</param>
        /// <returns>Success unit or error if actor not found or update failed</returns>
        Fin<Unit> UpdateActorHealth(ActorId actorId, Domain.Actor.Health newHealth);

        /// <summary>
        /// Applies damage to an actor, reducing their current health.
        /// </summary>
        /// <param name="actorId">The actor identifier</param>
        /// <param name="damage">Amount of damage to apply (must be non-negative)</param>
        /// <returns>Updated actor with new health state or error</returns>
        Fin<Domain.Actor.Actor> DamageActor(ActorId actorId, int damage);

        /// <summary>
        /// Applies healing to an actor, restoring their health.
        /// </summary>
        /// <param name="actorId">The actor identifier</param>
        /// <param name="healAmount">Amount of healing to apply (must be non-negative)</param>
        /// <returns>Updated actor with new health state or error</returns>
        Fin<Domain.Actor.Actor> HealActor(ActorId actorId, int healAmount);

        /// <summary>
        /// Checks if an actor is currently alive (health > 0).
        /// </summary>
        /// <param name="actorId">The actor identifier</param>
        /// <returns>True if alive, False if dead, None if actor not found</returns>
        Option<bool> IsActorAlive(ActorId actorId);

        /// <summary>
        /// Removes a dead actor from the combat state.
        /// Used when an actor dies and should be removed from active combat.
        /// </summary>
        /// <param name="actorId">The actor identifier</param>
        /// <returns>Success unit or error if actor not found or removal failed</returns>
        Fin<Unit> RemoveDeadActor(ActorId actorId);
    }
}
