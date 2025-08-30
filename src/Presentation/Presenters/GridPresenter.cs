using System;
using System.Threading.Tasks;
using Darklands.Core.Application.Grid.Queries;
using Darklands.Core.Presentation.Views;
using MediatR;
using Serilog;

namespace Darklands.Core.Presentation.Presenters
{
    /// <summary>
    /// Presenter for the tactical combat grid view.
    /// Orchestrates grid display, user interactions, and coordinates with the application layer via MediatR.
    /// Follows MVP pattern - handles all grid-related presentation logic without containing view implementation details.
    /// </summary>
    public sealed class GridPresenter : PresenterBase<IGridView>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new GridPresenter with the specified dependencies.
        /// </summary>
        /// <param name="view">The grid view interface this presenter controls</param>
        /// <param name="mediator">MediatR instance for sending commands and queries</param>
        /// <param name="logger">Logger for tracking grid operations</param>
        public GridPresenter(IGridView view, IMediator mediator, ILogger logger)
            : base(view)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes the grid presenter and sets up the initial grid display.
        /// Loads the current grid state from the application layer and displays it.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _logger.Information("GridPresenter initialized, setting up grid display");

            try
            {
                // Load and display the initial grid state asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RefreshGridDisplayAsync();
                        _logger.Information("Initial grid display setup completed");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to setup initial grid display");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during GridPresenter initialization");
            }
        }

        /// <summary>
        /// Handles user clicks on grid tiles to initiate movement or other interactions.
        /// Converts tile clicks into appropriate commands sent through MediatR.
        /// </summary>
        /// <param name="position">The grid position that was clicked</param>
        public async Task HandleTileClickAsync(Domain.Grid.Position position)
        {
            _logger.Information("User clicked on tile at position {Position}", position);

            try
            {
                // For Phase 4, we'll implement simple click-to-move for a test player
                // In the future, this would check for selected units, valid actions, etc.

                // For Phase 4 MVP - get the test player ID from ActorPresenter
                // This is a temporary implementation for Phase 4 MVP
                if (ActorPresenter.TestPlayerId == null)
                {
                    _logger.Warning("No test player available for movement");
                    await View.ShowErrorFeedbackAsync(position, "No player available");
                    return;
                }

                var moveCommand = Application.Grid.Commands.MoveActorCommand.Create(ActorPresenter.TestPlayerId.Value, position);

                var result = await _mediator.Send(moveCommand);

                await result.Match(
                    Succ: async _ =>
                    {
                        _logger.Information("Successfully processed move to position {Position}", position);
                        await View.ShowSuccessFeedbackAsync(position, "Moved");
                    },
                    Fail: async error =>
                    {
                        _logger.Warning("Move to position {Position} failed: {Error}", position, error.Message);
                        await View.ShowErrorFeedbackAsync(position, error.Message);
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error handling tile click at position {Position}", position);
                await View.ShowErrorFeedbackAsync(position, "An unexpected error occurred");
            }
        }

        /// <summary>
        /// Handles mouse hover over grid tiles to show interaction previews.
        /// </summary>
        /// <param name="position">The grid position being hovered over</param>
        public async Task HandleTileHoverAsync(Domain.Grid.Position position)
        {
            try
            {
                // For Phase 4, show simple hover highlighting
                // Future: Could show movement range, attack range, etc.
                await View.HighlightTileAsync(position, HighlightType.Hover);

                _logger.Debug("Highlighting tile at position {Position} for hover", position);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error handling tile hover at position {Position}", position);
            }
        }

        /// <summary>
        /// Handles mouse leaving a grid tile to remove interaction previews.
        /// </summary>
        /// <param name="position">The grid position no longer being hovered over</param>
        public async Task HandleTileUnhoverAsync(Domain.Grid.Position position)
        {
            try
            {
                await View.UnhighlightTileAsync(position);
                _logger.Debug("Removed hover highlighting from tile at position {Position}", position);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error handling tile unhover at position {Position}", position);
            }
        }

        /// <summary>
        /// Refreshes the entire grid display with the current state from the application layer.
        /// Used for initialization and when the grid state has changed significantly.
        /// </summary>
        public async Task RefreshGridDisplayAsync()
        {
            try
            {
                _logger.Debug("Refreshing grid display from application state");

                var query = new GetGridStateQuery();
                var result = await _mediator.Send(query);

                await result.Match(
                    Succ: async grid =>
                    {
                        // Display grid boundaries
                        await View.DisplayGridBoundariesAsync(grid.Width, grid.Height);

                        // Refresh the complete grid state
                        await View.RefreshGridAsync(grid);

                        _logger.Information("Grid display refreshed successfully with {Width}x{Height} grid",
                            grid.Width, grid.Height);
                    },
                    Fail: error =>
                    {
                        _logger.Error("Failed to refresh grid display: {Error}", error.Message);
                        return Task.CompletedTask;
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error refreshing grid display");
            }
        }

        /// <summary>
        /// Updates a specific tile in the grid display.
        /// Called when individual tiles change state without requiring a full refresh.
        /// </summary>
        /// <param name="position">Position of the tile to update</param>
        /// <param name="tile">New tile state</param>
        public async Task UpdateTileAsync(Domain.Grid.Position position, Domain.Grid.Tile tile)
        {
            try
            {
                await View.UpdateTileAsync(position, tile);
                _logger.Debug("Updated tile at position {Position}", position);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error updating tile at position {Position}", position);
            }
        }

        /// <summary>
        /// Disposes the presenter and cleans up any resources or subscriptions.
        /// </summary>
        public override void Dispose()
        {
            _logger.Information("GridPresenter disposing and cleaning up resources");
            base.Dispose();
        }
    }
}
