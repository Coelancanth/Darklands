using System.Collections.Generic;
using LanguageExt;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Application.Grid.Services
{
    /// <summary>
    /// Service interface for managing grid state and actor positions.
    /// Provides read and write operations for tactical combat positioning.
    /// </summary>
    public interface IGridStateService
    {
        /// <summary>
        /// Gets the current grid state snapshot.
        /// </summary>
        Fin<Domain.Grid.Grid> GetCurrentGrid();

        /// <summary>
        /// Checks if a position is within grid bounds.
        /// </summary>
        bool IsValidPosition(Position position);

        /// <summary>
        /// Checks if a position is empty (no actor occupying it).
        /// </summary>
        bool IsPositionEmpty(Position position);

        /// <summary>
        /// Gets the current position of an actor.
        /// </summary>
        Option<Position> GetActorPosition(ActorId actorId);

        /// <summary>
        /// Moves an actor to a new position.
        /// Validates the move and updates grid state.
        /// </summary>
        Fin<Unit> MoveActor(ActorId actorId, Position toPosition);

        /// <summary>
        /// Validates if a move from one position to another is legal.
        /// </summary>
        Fin<Unit> ValidateMove(Position fromPosition, Position toPosition);

        /// <summary>
        /// Adds a new actor to the grid at the specified position.
        /// This establishes the actor's position in the grid state.
        /// </summary>
        Fin<Unit> AddActorToGrid(ActorId actorId, Position position);

        /// <summary>
        /// Removes an actor from the grid state.
        /// Used when actors are destroyed or leave combat.
        /// </summary>
        Fin<Unit> RemoveActorFromGrid(ActorId actorId);

        /// <summary>
        /// Gets all actors currently positioned on the grid.
        /// Returns a dictionary mapping ActorId to Position for composite queries.
        /// </summary>
        IReadOnlyDictionary<ActorId, Position> GetAllActorPositions();
    }
}
