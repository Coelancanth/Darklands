using System.Threading.Tasks;
using Darklands.Domain.Grid;
using Darklands.Presentation.Views;

namespace Darklands.Presentation.Presenters
{
    /// <summary>
    /// Interface for the path visualization presenter in the MVP pattern.
    /// Defines the contract for pathfinding visualization and movement planning.
    /// </summary>
    public interface IPathVisualizationPresenter
    {
        /// <summary>
        /// Attaches a view to this presenter.
        /// </summary>
        void AttachView(IPathVisualizationView view);

        /// <summary>
        /// Initializes the presenter.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Disposes of presenter resources.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Calculates and displays a path between two positions.
        /// </summary>
        /// <param name="fromPosition">Starting position</param>
        /// <param name="toPosition">Target position</param>
        Task ShowPathAsync(Position fromPosition, Position toPosition);

        /// <summary>
        /// Clears the current path display.
        /// </summary>
        Task ClearPathAsync();

        /// <summary>
        /// Highlights valid movement endpoints from a given position.
        /// </summary>
        /// <param name="fromPosition">Starting position to calculate paths from</param>
        Task HighlightValidMovementAsync(Position fromPosition);

        /// <summary>
        /// Clears all path-related highlighting.
        /// </summary>
        Task ClearAllHighlightingAsync();
    }
}
