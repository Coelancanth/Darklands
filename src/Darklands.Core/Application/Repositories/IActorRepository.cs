using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Entities;

namespace Darklands.Core.Application.Repositories;

/// <summary>
/// Repository for managing Actor entities (WHO: components, name).
/// Works alongside IActorPositionService (WHERE: grid coordinates).
/// </summary>
/// <remarks>
/// <para><b>Two-System Tracking</b>:</para>
/// <para>
/// Actor data is split between two services for separation of concerns:
/// - IActorRepository: WHO is the actor? (name, health, weapon, equipment)
/// - IActorPositionService: WHERE is the actor? (grid position)
/// Both use ActorId as linking key.
/// </para>
///
/// <para><b>Lifecycle</b>:</para>
/// <code>
/// // 1. Create actor from template
/// var actor = ActorFactory.CreateFromTemplate("goblin");
///
/// // 2. Register in BOTH systems
/// await _actors.AddActorAsync(actor);
/// await _positions.RegisterActorAsync(actor.Id, spawnPos);
///
/// // 3. Use actor
/// var actorResult = await _actors.GetByIdAsync(actor.Id);
/// var health = actorResult.Value.GetComponent&lt;IHealthComponent&gt;();
///
/// // 4. Remove when dead (from BOTH systems)
/// await _actors.RemoveActorAsync(actor.Id);
/// await _positions.RemoveActorAsync(actor.Id);
/// </code>
///
/// <para><b>Why Two Systems</b>:</para>
/// <para>
/// - Position changes frequently (every move)
/// - Actor components change occasionally (damage, equipment)
/// - Separating concerns makes position queries fast (no need to load full actor)
/// </para>
/// </remarks>
public interface IActorRepository
{
    /// <summary>
    /// Retrieves an actor by ID.
    /// </summary>
    /// <param name="actorId">Unique identifier</param>
    /// <returns>Success with Actor if found, Failure if not found</returns>
    Task<Result<Actor>> GetByIdAsync(ActorId actorId);

    /// <summary>
    /// Adds a new actor to the repository.
    /// </summary>
    /// <param name="actor">Actor to add</param>
    /// <returns>Success if added, Failure if actor with same ID already exists</returns>
    Task<Result> AddActorAsync(Actor actor);

    /// <summary>
    /// Removes an actor from the repository.
    /// </summary>
    /// <param name="actorId">ID of actor to remove</param>
    /// <returns>Success if removed, Failure if actor not found</returns>
    Task<Result> RemoveActorAsync(ActorId actorId);

    /// <summary>
    /// Gets all actors in the repository.
    /// Useful for queries like "which actors are alive?", "who needs healing?".
    /// </summary>
    /// <returns>List of all actors (empty if none exist)</returns>
    Task<List<Actor>> GetAllAsync();
}
