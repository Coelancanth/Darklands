using System;
using System.Linq;
using System.Threading.Tasks;
using Darklands.Core.Application.Combat.Commands;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Application.Common;
using Darklands.Core.Application.Grid.Queries;
using Darklands.Core.Application.Vision.Queries;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Vision;
using Darklands.Core.Presentation.Views;
using LanguageExt;
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
        private readonly Application.Grid.Services.IGridStateService _gridStateService;
        private readonly ICombatQueryService _combatQueryService;
        private readonly IActorFactory _actorFactory;
        private ActorPresenter? _actorPresenter;
        private int _currentTurn = 1; // Track turn for vision system

        /// <summary>
        /// Creates a new GridPresenter with the specified dependencies.
        /// </summary>
        /// <param name="view">The grid view interface this presenter controls</param>
        /// <param name="mediator">MediatR instance for sending commands and queries</param>
        /// <param name="logger">Logger for tracking grid operations</param>
        /// <param name="gridStateService">Service for accessing grid state directly</param>
        /// <param name="combatQueryService">Service for querying actor positions and combat data</param>
        /// <param name="actorFactory">Factory for accessing actor information</param>
        public GridPresenter(IGridView view, IMediator mediator, ILogger logger, Application.Grid.Services.IGridStateService gridStateService, ICombatQueryService combatQueryService, IActorFactory actorFactory)
            : base(view)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gridStateService = gridStateService ?? throw new ArgumentNullException(nameof(gridStateService));
            _combatQueryService = combatQueryService ?? throw new ArgumentNullException(nameof(combatQueryService));
            _actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));
        }

        /// <summary>
        /// Sets the ActorPresenter reference for coordinated visual updates.
        /// This enables the GridPresenter to notify ActorPresenter when moves succeed.
        /// </summary>
        /// <param name="actorPresenter">The ActorPresenter instance to coordinate with</param>
        public void SetActorPresenter(ActorPresenter actorPresenter)
        {
            _actorPresenter = actorPresenter ?? throw new ArgumentNullException(nameof(actorPresenter));
        }

        /// <summary>
        /// Initializes the grid presenter and sets up the initial grid display.
        /// Loads the current grid state from the application layer and displays it.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();


            try
            {
                // Load and display the initial grid state
                // Note: This will execute synchronously but use deferred calls for UI operations
                _ = RefreshGridDisplayAsync();
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
                // Get the player ID from the actor factory
                if (_actorFactory.PlayerId == null)
                {
                    _logger.Warning("No player available for action");
                    await View.ShowErrorFeedbackAsync(position, "No player available");
                    return;
                }

                var playerId = _actorFactory.PlayerId.Value;

                // Check if there's a living enemy at the clicked position
                var targetActors = _combatQueryService.GetActorsInRadius(position, 0); // Exact position only
                var targetActor = targetActors.FirstOrDefault(a => a.Position == position && a.IsAlive);

                if (targetActor != null)
                {
                    // There's a living enemy at this position - attempt attack
                    _logger.Information("Attempting to attack {TargetName} at position {Position}", targetActor.Actor.Name, position);
                    await HandleAttackActionAsync(playerId, targetActor.Id, position);
                }
                else
                {
                    // No enemy at position - attempt move
                    _logger.Information("Attempting to move to empty position {Position}", position);
                    await HandleMoveActionAsync(playerId, position);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error handling tile click at position {Position}", position);
                await View.ShowErrorFeedbackAsync(position, "An unexpected error occurred");
            }
        }

        /// <summary>
        /// Handles attack action when clicking on an enemy.
        /// </summary>
        private async Task HandleAttackActionAsync(Domain.Grid.ActorId attackerId, Domain.Grid.ActorId targetId, Domain.Grid.Position targetPosition)
        {
            var attackCommand = ExecuteAttackCommand.Create(attackerId, targetId);
            var result = await _mediator.Send(attackCommand);

            await result.Match(
                Succ: async _ =>
                {
                    // Combat details are now logged in ExecuteAttackCommandHandler with damage/HP info
                    await View.ShowSuccessFeedbackAsync(targetPosition, "Hit!");
                },
                Fail: async error =>
                {
                    _logger.Warning("Attack on target at {Position} failed: {Error}", targetPosition, error.Message);
                    await View.ShowErrorFeedbackAsync(targetPosition, error.Message);
                }
            );
        }

        /// <summary>
        /// Handles move action when clicking on an empty position.
        /// </summary>
        private async Task HandleMoveActionAsync(Domain.Grid.ActorId playerId, Domain.Grid.Position targetPosition)
        {
            // Get the player's current position BEFORE the move for visual updates
            var fromPositionOption = _gridStateService.GetActorPosition(playerId);

            var moveCommand = Application.Grid.Commands.MoveActorCommand.Create(playerId, targetPosition);
            var result = await _mediator.Send(moveCommand);

            await result.Match(
                Succ: async _ =>
                {
                    _logger.Debug("Successfully processed move to position {Position}", targetPosition);

                    // Verify the move was actually applied by checking the new position
                    var newPositionOption = _gridStateService.GetActorPosition(playerId);
                    _logger.Debug("After move command - Player position in GridStateService: {Position}",
                        newPositionOption.Match(p => p.ToString(), () => "NOT_FOUND"));

                    // Notify ActorPresenter about the successful move
                    if (_actorPresenter != null && fromPositionOption.IsSome)
                    {
                        var from = fromPositionOption.Match(p => p, () => new Domain.Grid.Position(0, 0));
                        await _actorPresenter.HandleActorMovedAsync(playerId, from, targetPosition);
                    }
                    else
                    {
                        _logger.Warning("ActorPresenter not available or from position unknown - visual update skipped");
                    }

                    // Update player vision after movement (fog of war)
                    // Force recalculation by incrementing turn number
                    _currentTurn++;
                    _logger.Debug("Updating player vision after move to {Position} (turn {Turn})", targetPosition, _currentTurn);
                    await UpdatePlayerVisionAsync(_currentTurn);

                    await View.ShowSuccessFeedbackAsync(targetPosition, "Moved");
                },
                Fail: async error =>
                {
                    _logger.Warning("Move to position {Position} failed: {Error}", targetPosition, error.Message);
                    await View.ShowErrorFeedbackAsync(targetPosition, error.Message);
                }
            );
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
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error handling tile unhover at position {Position}", position);
            }
        }

        /// <summary>
        /// Updates the fog of war display for the current player.
        /// Calculates new FOV from player's current position and applies to the view.
        /// </summary>
        /// <param name="currentTurn">Current game turn for vision state tracking</param>
        public async Task UpdatePlayerVisionAsync(int currentTurn = 1)
        {
            try
            {
                // Get the player actor
                if (_actorFactory.PlayerId == null)
                {
                    _logger.Debug("No player available for vision update");
                    return;
                }

                var playerId = _actorFactory.PlayerId.Value;

                // Get player's current position
                var playerPositionOption = _gridStateService.GetActorPosition(playerId);
                if (playerPositionOption.IsNone)
                {
                    _logger.Warning("Could not get player position for vision update");
                    return;
                }

                var playerPosition = playerPositionOption.Match(p => p, () => new Domain.Grid.Position(0, 0));
                _logger.Debug("Vision update - Using player position: {Position}", playerPosition);

                // Create FOV calculation query
                var playerVisionRange = VisionRange.Create(8).IfFail(VisionRange.Blind); // Player vision range from backlog
                var fovQuery = CalculateFOVQuery.Create(playerId, playerPosition, playerVisionRange, currentTurn);

                // Calculate new vision state
                var visionResult = await _mediator.Send(fovQuery);

                await visionResult.Match(
                    Succ: async visionState =>
                    {
                        _logger.Debug("Calculated vision for player at {Position}: {Visible} visible, {Explored} explored",
                            playerPosition, visionState.CurrentlyVisible.Count, visionState.PreviouslyExplored.Count);

                        // Update the view with new fog of war
                        await View.UpdateFogOfWarAsync(visionState);

                        // Update actor visibility based on vision
                        await UpdateActorVisibilityAsync(visionState);
                    },
                    Fail: error =>
                    {
                        _logger.Warning("Failed to calculate player vision: {Error}", error.Message);
                        // Continue without fog of war update
                        return Task.CompletedTask;
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error updating player vision");
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

                var query = new GetGridStateQuery();
                var result = await _mediator.Send(query);

                await result.Match(
                    Succ: async grid =>
                    {
                        // Display grid boundaries
                        await View.DisplayGridBoundariesAsync(grid.Width, grid.Height);

                        // Refresh the complete grid state
                        await View.RefreshGridAsync(grid);

                        // Initialize player vision (fog of war)
                        await UpdatePlayerVisionAsync(_currentTurn);

                        _logger.Debug("Grid display refreshed successfully with {Width}x{Height} grid and initial fog of war",
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
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error updating tile at position {Position}", position);
            }
        }

        /// <summary>
        /// Updates the visibility of actors and health bars based on the current vision state.
        /// Actors are only visible when they're within the player's current vision range.
        /// </summary>
        /// <param name="visionState">Current vision state with visible tiles</param>
        private async Task UpdateActorVisibilityAsync(Domain.Vision.VisionState visionState)
        {
            try
            {
                // Get all actors in the game (except the player)
                var playerId = _actorFactory.PlayerId;
                if (playerId == null) return;

                // Get all actor positions from combat query service
                var allActors = _combatQueryService.GetActorsInRadius(new Domain.Grid.Position(0, 0), 1000); // Large radius to get all

                foreach (var actorData in allActors)
                {
                    // Skip the player - player is always visible
                    if (actorData.Id.Equals(playerId.Value)) continue;

                    var actorPosition = actorData.Position;
                    var isVisible = visionState.GetVisibilityLevel(actorPosition) == Domain.Vision.VisibilityLevel.Visible;

                    _logger.Debug("Actor {ActorId} at {Position}: {Visibility}",
                        actorData.Id.Value.ToString()[..8], actorPosition,
                        isVisible ? "VISIBLE" : "HIDDEN");

                    // Update actor visibility through ActorPresenter
                    if (_actorPresenter != null)
                    {
                        await _actorPresenter.SetActorVisibilityAsync(actorData.Id, isVisible);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating actor visibility based on vision");
            }
        }

        /// <summary>
        /// Disposes the presenter and cleans up any resources or subscriptions.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
