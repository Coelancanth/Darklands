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
        /// Disposes of presenter resources.
        /// </summary>
        void Dispose();
    }
}
