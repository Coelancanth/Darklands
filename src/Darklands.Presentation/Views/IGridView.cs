using System.Collections.Generic;
using System.Threading.Tasks;
using Darklands.Domain.Grid;
using Darklands.Domain.Vision;

namespace Darklands.Presentation.Views
{
    /// <summary>
    /// Interface for the grid view that handles tactical combat grid visualization and user interactions.
    /// Abstracts Godot-specific implementation details while providing all necessary grid display capabilities.
    /// Follows Darklands Clean Architecture - no Godot types exposed at this interface level.
    /// </summary>
    public interface IGridView
    {
        /// <summary>
        /// Displays the grid boundaries with the specified dimensions.
        /// Shows the playable area and any visual indicators for grid limits.
        /// </summary>
        /// <param name="width">Width of the grid in tiles</param>
        /// <param name="height">Height of the grid in tiles</param>
        Task DisplayGridBoundariesAsync(int width, int height);

        /// <summary>
        /// Updates the entire grid display with the current tile states.
        /// Used for initialization or full refresh scenarios.
        /// </summary>
        /// <param name="grid">The complete grid state to display</param>
        Task RefreshGridAsync(Domain.Grid.Grid grid);

        /// <summary>
        /// Updates a single tile's visual representation.
        /// </summary>
        /// <param name="position">Position of the tile to update</param>
        /// <param name="tile">New tile state to display</param>
        Task UpdateTileAsync(Position position, Tile tile);

        /// <summary>
        /// Highlights a tile to indicate selection, hover, or special state.
        /// </summary>
        /// <param name="position">Position of the tile to highlight</param>
        /// <param name="highlightType">Type of highlight to apply</param>
        Task HighlightTileAsync(Position position, HighlightType highlightType);

        /// <summary>
        /// Removes highlighting from a tile.
        /// </summary>
        /// <param name="position">Position of the tile to unhighlight</param>
        Task UnhighlightTileAsync(Position position);

        /// <summary>
        /// Shows visual feedback for successful operations.
        /// </summary>
        /// <param name="position">Position where the operation occurred</param>
        /// <param name="message">Success message to display briefly</param>
        Task ShowSuccessFeedbackAsync(Position position, string message);

        /// <summary>
        /// Shows visual feedback for invalid operations or errors.
        /// </summary>
        /// <param name="position">Position where the error occurred</param>
        /// <param name="errorMessage">Error message to display briefly</param>
        Task ShowErrorFeedbackAsync(Position position, string errorMessage);

        /// <summary>
        /// Clears all visual overlays and resets the grid to clean state.
        /// </summary>
        Task ClearOverlaysAsync();

        /// <summary>
        /// Updates the fog of war display based on a vision state.
        /// Applies modulation to all tiles to show visibility levels while preserving terrain colors.
        /// </summary>
        /// <param name="visionState">The vision state containing visibility information</param>
        Task UpdateFogOfWarAsync(VisionState visionState);

        /// <summary>
        /// Updates the fog state for a single tile.
        /// Useful for incremental updates when vision changes.
        /// </summary>
        /// <param name="position">Grid position of the tile</param>
        /// <param name="visibilityLevel">New visibility level for the tile</param>
        Task UpdateTileFogAsync(Position position, VisibilityLevel visibilityLevel);

        /// <summary>
        /// Clears all fog of war effects, making all tiles fully visible.
        /// </summary>
        Task ClearFogOfWarAsync();
    }

    /// <summary>
    /// Types of highlighting that can be applied to grid tiles.
    /// Maps to different visual treatments without exposing rendering details.
    /// </summary>
    public enum HighlightType
    {
        /// <summary>
        /// Tile is currently selected by the user.
        /// </summary>
        Selected,

        /// <summary>
        /// Tile is being hovered over by the cursor.
        /// </summary>
        Hover,

        /// <summary>
        /// Tile is a valid target for the current operation.
        /// </summary>
        ValidTarget,

        /// <summary>
        /// Tile is an invalid target for the current operation.
        /// </summary>
        InvalidTarget,

        /// <summary>
        /// Tile is showing an error state.
        /// </summary>
        Error,

        /// <summary>
        /// Tile is showing a success state.
        /// </summary>
        Success
    }
}
