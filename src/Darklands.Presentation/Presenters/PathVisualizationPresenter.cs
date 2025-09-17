using System;
using System.Threading.Tasks;
using Darklands.Application.Grid.Services;
using Darklands.Application.Grid.Queries;
using Darklands.Application.Common;
using Darklands.Domain.Grid;
using Darklands.Presentation.Views;
using MediatR;
using static LanguageExt.Prelude;

namespace Darklands.Presentation.Presenters
{
    /// <summary>
    /// Presenter for path visualization and movement planning.
    /// Handles pathfinding calculations, visual path display, and movement preview feedback.
    /// Follows MVP pattern - contains presentation logic without view implementation details.
    /// Integrates with CalculatePathQuery for A* pathfinding functionality.
    /// </summary>
    public sealed class PathVisualizationPresenter : PresenterBase<IPathVisualizationView>, IPathVisualizationPresenter
    {
        private readonly IMediator _mediator;
        private readonly IGridStateService _gridStateService;
        private readonly ICategoryLogger _logger;

        /// <summary>
        /// Creates a new PathVisualizationPresenter with the specified dependencies.
        /// The view will be attached later via AttachView method.
        /// </summary>
        /// <param name="mediator">MediatR mediator for sending CalculatePathQuery</param>
        /// <param name="gridStateService">Grid state service for position validation</param>
        /// <param name="logger">Logger for pathfinding messages</param>
        public PathVisualizationPresenter(
            IMediator mediator,
            IGridStateService gridStateService,
            ICategoryLogger logger)
            : base()
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _gridStateService = gridStateService ?? throw new ArgumentNullException(nameof(gridStateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Attaches a view to this presenter.
        /// Overrides base implementation to properly handle the view attachment.
        /// </summary>
        public new void AttachView(IPathVisualizationView view)
        {
            base.AttachView(view);
            // Additional initialization if needed
        }

        /// <summary>
        /// Legacy method for backward compatibility.
        /// </summary>
        void IPathVisualizationPresenter.AttachView(IPathVisualizationView view)
        {
            AttachView(view);
        }

        /// <summary>
        /// Calculates and displays a path between two positions.
        /// Uses A* pathfinding algorithm via CalculatePathQuery to find optimal route.
        /// </summary>
        /// <param name="fromPosition">Starting position</param>
        /// <param name="toPosition">Target position</param>
        public async Task ShowPathAsync(Position fromPosition, Position toPosition)
        {
            try
            {
                _logger.Log(LogLevel.Debug, LogCategory.Pathfinding, "Calculating path from {FromPosition} to {ToPosition}",
                    fromPosition, toPosition);

                // Validate positions before pathfinding
                if (!_gridStateService.IsValidPosition(fromPosition))
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Pathfinding, "Invalid start position: {Position}", fromPosition);
                    await View.ShowNoPathFoundAsync(fromPosition, toPosition, "Invalid start position");
                    return;
                }

                if (!_gridStateService.IsValidPosition(toPosition))
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Pathfinding, "Invalid target position: {Position}", toPosition);
                    await View.ShowNoPathFoundAsync(fromPosition, toPosition, "Invalid target position");
                    return;
                }

                // Use CalculatePathQuery to get A* pathfinding result
                var query = CalculatePathQuery.Create(fromPosition, toPosition);
                var pathResult = await _mediator.Send(query);

                // Handle pathfinding result
                await pathResult.Match(
                    Succ: async path =>
                    {
                        _logger.Log(LogLevel.Debug, LogCategory.Pathfinding, "Path found with {PathLength} positions",
                            path.Count);
                        await View.ShowPathAsync(path, fromPosition, toPosition);
                    },
                    Fail: async error =>
                    {
                        _logger.Log(LogLevel.Warning, LogCategory.Pathfinding, "No path found: {Error}", error.Message);
                        await View.ShowNoPathFoundAsync(fromPosition, toPosition, error.Message);
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Pathfinding, "Error calculating path from {FromPosition} to {ToPosition}: {Error}",
                    fromPosition, toPosition, ex.Message);
                await View.ShowNoPathFoundAsync(fromPosition, toPosition, "Pathfinding error occurred");
            }
        }

        /// <summary>
        /// Clears the current path display.
        /// Removes all path visualization overlays from the grid.
        /// </summary>
        public async Task ClearPathAsync()
        {
            try
            {
                await View.ClearPathAsync();
                _logger.Log(LogLevel.Debug, LogCategory.Pathfinding, "Path display cleared");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Pathfinding, "Error clearing path display: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Highlights valid movement endpoints from a given position.
        /// Shows all positions that can be reached via pathfinding.
        /// </summary>
        /// <param name="fromPosition">Starting position to calculate paths from</param>
        public async Task HighlightValidMovementAsync(Position fromPosition)
        {
            try
            {
                if (!_gridStateService.IsValidPosition(fromPosition))
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Pathfinding, "Cannot highlight movement: Invalid position {Position}", fromPosition);
                    return;
                }

                // Get current grid to determine valid endpoints
                var gridResult = _gridStateService.GetCurrentGrid();
                await gridResult.Match(
                    Succ: async grid =>
                    {
                        // Calculate all walkable positions as potential endpoints
                        var validEndpoints = new System.Collections.Generic.List<Position>();

                        for (int x = 0; x < grid.Width; x++)
                        {
                            for (int y = 0; y < grid.Height; y++)
                            {
                                var position = new Position(x, y);
                                if (_gridStateService.IsWalkable(position) && !position.Equals(fromPosition))
                                {
                                    validEndpoints.Add(position);
                                }
                            }
                        }

                        await View.HighlightValidEndpointsAsync(fromPosition, validEndpoints.ToArray());
                        _logger.Log(LogLevel.Debug, LogCategory.Pathfinding, "Highlighted {EndpointCount} valid movement endpoints from {FromPosition}",
                            validEndpoints.Count, fromPosition);
                    },
                    Fail: error =>
                    {
                        _logger.Log(LogLevel.Error, LogCategory.Pathfinding, "Cannot highlight movement: Grid error - {Error}", error.Message);
                        return Task.CompletedTask;
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Pathfinding, "Error highlighting valid movement from {FromPosition}: {Error}",
                    fromPosition, ex.Message);
            }
        }

        /// <summary>
        /// Clears all path-related highlighting.
        /// Removes both path display and endpoint highlighting.
        /// </summary>
        public async Task ClearAllHighlightingAsync()
        {
            try
            {
                await View.ClearPathAsync();
                await View.ClearEndpointHighlightingAsync();
                _logger.Log(LogLevel.Debug, LogCategory.Pathfinding, "All path highlighting cleared");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Pathfinding, "Error clearing path highlighting: {Error}", ex.Message);
            }
        }
    }
}
