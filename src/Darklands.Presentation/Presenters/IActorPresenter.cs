using System.Threading.Tasks;
using Darklands.Domain.Grid;
using Darklands.Presentation.Views;

namespace Darklands.Presentation.Presenters
{
    /// <summary>
    /// Interface for the actor presenter in the MVP pattern.
    /// Defines the contract for actor presentation logic.
    /// </summary>
    public interface IActorPresenter
    {
        /// <summary>
        /// Attaches a view to this presenter.
        /// </summary>
        void AttachView(IActorView view);

        /// <summary>
        /// Initializes the presenter.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Updates the visual position of an actor.
        /// </summary>
        Task UpdateActorPositionAsync(ActorId actorId, Domain.Grid.Position position);

        /// <summary>
        /// Handles actor movement with a precalculated A* path.
        /// </summary>
        /// <param name="actorId">ID of the actor that moved</param>
        /// <param name="fromPosition">Previous position</param>
        /// <param name="toPosition">New position</param>
        /// <param name="path">The A* path to animate along</param>
        Task HandleActorMovedWithPathAsync(ActorId actorId, Position fromPosition, Position toPosition, System.Collections.Generic.List<Position>? path);

        /// <summary>
        /// Handles actor movement without a path (fallback).
        /// </summary>
        /// <param name="actorId">ID of the actor that moved</param>
        /// <param name="fromPosition">Previous position</param>
        /// <param name="toPosition">New position</param>
        Task HandleActorMovedAsync(ActorId actorId, Position fromPosition, Position toPosition);

        /// <summary>
        /// Disposes of presenter resources.
        /// </summary>
        void Dispose();
    }
}
