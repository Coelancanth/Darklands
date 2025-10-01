using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Grid.Application.Services;

/// <summary>
/// Abstraction for tracking actor positions on the grid.
/// Separates actor location data from terrain data (GridMap handles terrain only).
/// </summary>
public interface IActorPositionService
{
    /// <summary>
    /// Gets the current grid position of an actor.
    /// </summary>
    /// <param name="actorId">Unique identifier of the actor</param>
    /// <returns>
    /// Success with Position if actor exists,
    /// or Failure if actor ID is not found in the registry
    /// </returns>
    Result<Position> GetPosition(ActorId actorId);

    /// <summary>
    /// Updates an actor's position on the grid.
    /// Does NOT validate terrain passability - that's the caller's responsibility.
    /// </summary>
    /// <param name="actorId">Unique identifier of the actor</param>
    /// <param name="position">New grid position</param>
    /// <returns>
    /// Success if position updated,
    /// or Failure if actor ID is not found
    /// </returns>
    Result SetPosition(ActorId actorId, Position position);

    /// <summary>
    /// Retrieves all registered actor IDs.
    /// Used for filtering operations (e.g., "which actors are visible?").
    /// </summary>
    /// <returns>Success with list of all actor IDs (empty list if no actors registered)</returns>
    Result<List<ActorId>> GetAllActors();
}
