using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Domain.Aggregates.Actors;
using LanguageExt;

namespace Darklands.Tactical.Application.Features.Combat.Services;

/// <summary>
/// Repository interface for managing Actor aggregates.
/// </summary>
public interface IActorRepository
{
    /// <summary>
    /// Retrieves an actor by their unique identifier.
    /// </summary>
    /// <param name="id">The actor's unique identifier.</param>
    /// <returns>The actor if found, or an error.</returns>
    Task<Fin<Actor>> GetByIdAsync(EntityId id);

    /// <summary>
    /// Retrieves all actors in the current combat session.
    /// </summary>
    /// <returns>A sequence of all actors.</returns>
    Task<Fin<Seq<Actor>>> GetAllAsync();

    /// <summary>
    /// Adds a new actor to the repository.
    /// </summary>
    /// <param name="actor">The actor to add.</param>
    /// <returns>Success or error.</returns>
    Task<Fin<Unit>> AddAsync(Actor actor);

    /// <summary>
    /// Updates an existing actor's state.
    /// </summary>
    /// <param name="actor">The actor with updated state.</param>
    /// <returns>Success or error.</returns>
    Task<Fin<Unit>> UpdateAsync(Actor actor);

    /// <summary>
    /// Removes an actor from the repository.
    /// </summary>
    /// <param name="id">The actor's unique identifier.</param>
    /// <returns>Success or error.</returns>
    Task<Fin<Unit>> RemoveAsync(EntityId id);

    /// <summary>
    /// Checks if an actor exists in the repository.
    /// </summary>
    /// <param name="id">The actor's unique identifier.</param>
    /// <returns>True if the actor exists.</returns>
    Task<bool> ExistsAsync(EntityId id);
}
