using System.Threading.Tasks;
using Darklands.Domain.Grid;
using Darklands.Presentation.Views;

namespace Darklands.Presentation.Presenters
{
    /// <summary>
    /// Interface for the grid presenter in the MVP pattern.
    /// Defines the contract for grid presentation logic.
    /// </summary>
    public interface IGridPresenter
    {
        /// <summary>
        /// Attaches a view to this presenter.
        /// </summary>
        void AttachView(IGridView view);

        /// <summary>
        /// Initializes the presenter and sets up the initial grid state.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Handles a tile click event from the view.
        /// </summary>
        Task HandleTileClickAsync(Position position);

        /// <summary>
        /// Disposes of presenter resources.
        /// </summary>
        void Dispose();
    }
}
