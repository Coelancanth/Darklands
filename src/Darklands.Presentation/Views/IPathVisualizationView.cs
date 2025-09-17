using System.Threading.Tasks;
using LanguageExt;
using Darklands.Domain.Grid;

namespace Darklands.Presentation.Views
{
    /// <summary>
    /// Interface for the path visualization view that handles pathfinding display.
    /// Manages path overlay graphics, highlighting, and visual feedback for movement planning.
    /// Abstracts Godot-specific implementation details following Clean Architecture principles.
    /// </summary>
    public interface IPathVisualizationView
    {
        /// <summary>
        /// Displays a calculated path on the grid.
        /// Shows a visual overlay indicating the optimal route from start to end.
        /// </summary>
        /// <param name="path">The sequence of positions representing the path</param>
        /// <param name="startPosition">The starting position of the path</param>
        /// <param name="endPosition">The ending position of the path</param>
        Task ShowPathAsync(Seq<Position> path, Position startPosition, Position endPosition);

        /// <summary>
        /// Clears the current path display.
        /// Removes all path visualization overlays from the grid.
        /// </summary>
        Task ClearPathAsync();

        /// <summary>
        /// Shows a "no path found" indicator between two positions.
        /// Displays visual feedback when pathfinding fails.
        /// </summary>
        /// <param name="startPosition">The attempted start position</param>
        /// <param name="endPosition">The attempted end position</param>
        /// <param name="reason">Reason why no path was found</param>
        Task ShowNoPathFoundAsync(Position startPosition, Position endPosition, string reason);

        /// <summary>
        /// Highlights potential path endpoints for preview.
        /// Shows valid positions where a path can be calculated to.
        /// </summary>
        /// <param name="fromPosition">The starting position for path calculation</param>
        /// <param name="validEndpoints">Positions that can be reached</param>
        Task HighlightValidEndpointsAsync(Position fromPosition, Position[] validEndpoints);

        /// <summary>
        /// Clears endpoint highlighting.
        /// Removes all path endpoint visual indicators.
        /// </summary>
        Task ClearEndpointHighlightingAsync();
    }
}
