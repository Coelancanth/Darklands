using System;
using System.Threading.Tasks;
using Darklands.Core.Application.Common;
using Darklands.Core.Application.Grid.Queries;
using Darklands.Core.Presentation.Views;
using MediatR;
using Serilog;
using static LanguageExt.Prelude;

namespace Darklands.Core.Presentation.Presenters
{
    /// <summary>
    /// Presenter for the actor view that manages character/unit display and interactions.
    /// Handles actor positioning, visual state updates, and coordinates with the application layer.
    /// Follows MVP pattern - contains presentation logic without view implementation details.
    /// </summary>
    public sealed class ActorPresenter : PresenterBase<IActorView>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IActorFactory _actorFactory;
        private HealthPresenter? _healthPresenter;

        /// <summary>
        /// Creates a new ActorPresenter with the specified dependencies.
        /// </summary>
        /// <param name="view">The actor view interface this presenter controls</param>
        /// <param name="mediator">MediatR instance for sending commands and queries</param>
        /// <param name="logger">Logger for tracking actor operations</param>
        /// <param name="actorFactory">Factory for creating and managing actors</param>
        public ActorPresenter(IActorView view, IMediator mediator, ILogger logger, IActorFactory actorFactory)
            : base(view)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));
        }

        /// <summary>
        /// Sets the health presenter for coordinated health bar updates.
        /// Called by GameManager during MVP setup.
        /// </summary>
        /// <param name="healthPresenter">The health presenter to coordinate with</param>
        public void SetHealthPresenter(HealthPresenter healthPresenter)
        {
            _healthPresenter = healthPresenter ?? throw new ArgumentNullException(nameof(healthPresenter));
            _logger.Debug("ActorPresenter connected to HealthPresenter for coordinated updates");
        }

        /// <summary>
        /// Initializes the actor presenter and sets up the initial actor display.
        /// Creates test actors using the actor factory for clean separation of concerns.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _logger.Information("ActorPresenter initialized, setting up initial actors");

            try
            {
                // Create initial actors using the factory (clean separation of concerns)
                try
                {
                    // Create test player at position (0,0)
                    var playerResult = _actorFactory.CreatePlayer(new Domain.Grid.Position(0, 0), "Test Player");
                    playerResult.Match(
                        Succ: playerId =>
                        {
                            // Display the actor visually
                            View.DisplayActorAsync(playerId, new Domain.Grid.Position(0, 0), ActorType.Player);

                            // Notify health presenter if connected
                            if (_healthPresenter != null)
                            {
                                var healthResult = Domain.Actor.Health.CreateAtFullHealth(100);
                                healthResult.Match(
                                    Succ: health =>
                                    {
                                        _healthPresenter.HandleActorCreated(playerId, new Domain.Grid.Position(0, 0), health);
                                        _logger.Debug("Test player created with health bar at position (0,0) with ID {ActorId}", playerId);
                                    },
                                    Fail: error => _logger.Warning("Failed to create health for test player: {Error}", error.Message)
                                );
                            }
                            else
                            {
                                _logger.Warning("HealthPresenter not connected - health bar will not be displayed for test player");
                            }
                        },
                        Fail: error => _logger.Warning("Failed to create test player: {Error}", error.Message)
                    );

                    // Create dummy target at position (5,5)
                    var dummyResult = _actorFactory.CreateDummy(new Domain.Grid.Position(5, 5), 50);
                    dummyResult.Match(
                        Succ: dummyId =>
                        {
                            // Display the dummy visually
                            View.DisplayActorAsync(dummyId, new Domain.Grid.Position(5, 5), ActorType.Enemy);

                            // Notify health presenter if connected
                            if (_healthPresenter != null)
                            {
                                var healthResult = Domain.Actor.Health.CreateAtFullHealth(50);
                                healthResult.Match(
                                    Succ: health =>
                                    {
                                        _healthPresenter.HandleActorCreated(dummyId, new Domain.Grid.Position(5, 5), health);
                                        _logger.Debug("Dummy target created with health bar at position (5,5) with ID {ActorId}", dummyId);
                                    },
                                    Fail: error => _logger.Warning("Failed to create health for dummy target: {Error}", error.Message)
                                );
                            }
                            else
                            {
                                _logger.Warning("HealthPresenter not connected - dummy health bar will not be displayed");
                            }
                        },
                        Fail: error => _logger.Warning("Failed to create dummy target: {Error}", error.Message)
                    );

                    _logger.Debug("Initial actor display setup completed - player and dummy target");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during actor initialization");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during ActorPresenter initialization");
            }
        }


        /// <summary>
        /// Handles actor movement notifications from the application layer.
        /// Updates the visual representation when actors move on the grid.
        /// </summary>
        /// <param name="actorId">ID of the actor that moved</param>
        /// <param name="fromPosition">Previous position</param>
        /// <param name="toPosition">New position</param>
        public async Task HandleActorMovedAsync(Domain.Grid.ActorId actorId, Domain.Grid.Position fromPosition, Domain.Grid.Position toPosition)
        {
            _logger.Information("Handling actor move for {ActorId} from {FromPosition} to {ToPosition}",
                actorId, fromPosition, toPosition);

            try
            {
                // Update the actor's visual position
                await View.MoveActorAsync(actorId, fromPosition, toPosition);

                // CRITICAL: Also update the health bar position to follow the actor
                if (_healthPresenter != null)
                {
                    await _healthPresenter.HandleActorMovedAsync(actorId, fromPosition, toPosition);
                    _logger.Debug("Health bar position updated for actor {ActorId}", actorId);
                }

                // Show brief success feedback
                await View.ShowActorFeedbackAsync(actorId, ActorFeedbackType.ActionSuccess, "Moved");

                _logger.Debug("Successfully updated actor position and health bar for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error handling actor move for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Handles actor selection events.
        /// Updates visual highlighting to show which actor is currently selected.
        /// </summary>
        /// <param name="actorId">ID of the actor to select</param>
        public async Task HandleActorSelectedAsync(Domain.Grid.ActorId actorId)
        {
            try
            {
                await View.HighlightActorAsync(actorId, ActorHighlightType.Selected);
                _logger.Debug("Actor {ActorId} selected and highlighted", actorId);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error handling actor selection for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Handles actor deselection events.
        /// Removes selection highlighting from the actor.
        /// </summary>
        /// <param name="actorId">ID of the actor to deselect</param>
        public async Task HandleActorDeselectedAsync(Domain.Grid.ActorId actorId)
        {
            try
            {
                await View.UnhighlightActorAsync(actorId);
                _logger.Debug("Actor {ActorId} deselected and unhighlighted", actorId);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error handling actor deselection for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Refreshes the display of all actors with their current states.
        /// Used for initialization or when actor states have changed significantly.
        /// </summary>
        public async Task RefreshAllActorsAsync()
        {
            try
            {
                _logger.Debug("Refreshing all actor displays");

                // For Phase 4, we only have the test player
                // Future versions would query the application layer for all actors
                await View.RefreshAllActorsAsync();

                _logger.Debug("All actor displays refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error refreshing all actor displays");
            }
        }

        /// <summary>
        /// Updates an actor's visual representation.
        /// Called when actor state changes without requiring movement.
        /// </summary>
        /// <param name="actorId">ID of the actor to update</param>
        /// <param name="position">Current position of the actor</param>
        /// <param name="actorType">Updated actor type</param>
        public async Task UpdateActorAsync(Domain.Grid.ActorId actorId, Domain.Grid.Position position, ActorType actorType)
        {
            try
            {
                await View.UpdateActorAsync(actorId, position, actorType);
                _logger.Debug("Updated actor {ActorId} at position {Position}", actorId, position);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error updating actor {ActorId} at position {Position}", actorId, position);
            }
        }

        /// <summary>
        /// Removes an actor from the visual display.
        /// Called when actors are destroyed or leave the battlefield.
        /// </summary>
        /// <param name="actorId">ID of the actor to remove</param>
        /// <param name="position">Last known position of the actor</param>
        public async Task RemoveActorAsync(Domain.Grid.ActorId actorId, Domain.Grid.Position position)
        {
            try
            {
                await View.RemoveActorAsync(actorId, position);
                _logger.Information("Removed actor {ActorId} from position {Position}", actorId, position);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error removing actor {ActorId} from position {Position}", actorId, position);
            }
        }

        /// <summary>
        /// Disposes the presenter and cleans up any resources or subscriptions.
        /// </summary>
        public override void Dispose()
        {
            _logger.Information("ActorPresenter disposing and cleaning up resources");
            base.Dispose();
        }
    }
}
