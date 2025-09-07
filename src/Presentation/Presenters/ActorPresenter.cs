using System;
using System.Threading.Tasks;
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
        private readonly Application.Grid.Services.IGridStateService _gridStateService;
        private readonly Application.Actor.Services.IActorStateService _actorStateService;
        private HealthPresenter? _healthPresenter;

        // For Phase 4 MVP - shared test player state
        public static Domain.Grid.ActorId? TestPlayerId { get; private set; }

        /// <summary>
        /// Creates a new ActorPresenter with the specified dependencies.
        /// </summary>
        /// <param name="view">The actor view interface this presenter controls</param>
        /// <param name="mediator">MediatR instance for sending commands and queries</param>
        /// <param name="logger">Logger for tracking actor operations</param>
        /// <param name="gridStateService">Grid state service for actor positioning</param>
        /// <param name="actorStateService">Actor state service for health and combat data</param>
        public ActorPresenter(IActorView view, IMediator mediator, ILogger logger,
            Application.Grid.Services.IGridStateService gridStateService,
            Application.Actor.Services.IActorStateService actorStateService)
            : base(view)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gridStateService = gridStateService ?? throw new ArgumentNullException(nameof(gridStateService));
            _actorStateService = actorStateService ?? throw new ArgumentNullException(nameof(actorStateService));
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
        /// Creates a test player actor for Phase 4 MVP demonstration.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _logger.Information("ActorPresenter initialized, setting up initial actors");

            try
            {
                // For Phase 4, create a test player at position (0,0)
                // In the future, this would load actors from the application state
                // Note: This will execute and use deferred calls for UI operations
                InitializeTestPlayer();

                // Create a dummy target for combat testing (VS_010c Phase 4)
                InitializeDummyTarget();

                _logger.Information("Initial actor display setup initiated - player and dummy target");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during ActorPresenter initialization");
            }
        }

        /// <summary>
        /// Creates and displays a test player actor for Phase 4 demonstration.
        /// This is a temporary implementation - future versions would load from application state.
        /// </summary>
        private void InitializeTestPlayer()
        {
            try
            {
                // Create a test player at the grid origin
                TestPlayerId = Domain.Grid.ActorId.NewId();
                var startPosition = new Domain.Grid.Position(0, 0);

                // Create a full Actor with health data using domain factory (position handled separately)
                var actorResult = Domain.Actor.Actor.CreateAtFullHealth(TestPlayerId.Value, 100, "Test Player");

                if (actorResult.IsSucc)
                {
                    var actor = actorResult.Match(succ => succ, fail => throw new InvalidOperationException());

                    // First, add the actor to the actor state service (for health data)
                    var addResult = _actorStateService.AddActor(actor);
                    if (addResult.IsSucc)
                    {
                        // Then, place the actor on the grid at the start position
                        var placeResult = _gridStateService.AddActorToGrid(actor.Id, startPosition);
                        if (placeResult.IsSucc)
                        {
                            // Display the actor visually
                            _ = Task.Run(async () => await View.DisplayActorAsync(actor.Id, startPosition, ActorType.Player));

                            // CRITICAL: Notify the health presenter to create a health bar for this actor
                            if (_healthPresenter != null)
                            {
                                _ = Task.Run(async () => await _healthPresenter.HandleActorCreatedAsync(actor.Id, startPosition, actor.Health));
                                _logger.Information("Test player actor created with health bar at position {Position} with ID {ActorId} and health {Health}",
                                    startPosition, actor.Id, actor.Health);
                            }
                            else
                            {
                                _logger.Warning("HealthPresenter not connected - health bar will not be displayed for test player");
                                _logger.Information("Test player actor created (no health bar) at position {Position} with ID {ActorId} and health {Health}",
                                    startPosition, actor.Id, actor.Health);
                            }
                        }
                        else
                        {
                            placeResult.IfFail(error => _logger.Warning("Failed to place test player on grid: {Error}", error.Message));
                        }
                    }
                    else
                    {
                        addResult.IfFail(error => _logger.Warning("Failed to add test player to actor state service: {Error}", error.Message));
                    }
                }
                else
                {
                    actorResult.IfFail(error => _logger.Warning("Failed to create test player actor: {Error}", error.Message));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize test player actor");
            }
        }

        /// <summary>
        /// Creates and displays a dummy combat target for testing and combat mechanics.
        /// This implements VS_010c Phase 4 by spawning a visual dummy using SpawnDummyCommand.
        /// </summary>
        private void InitializeDummyTarget()
        {
            try
            {
                // Create dummy at position (5,5) as specified in VS_010c requirements  
                var dummyPosition = new Domain.Grid.Position(5, 5);

                _logger.Information("Creating dummy combat target at position {Position}", dummyPosition);

                // Create the dummy directly using the same pattern as InitializeTestPlayer
                // This ensures we have access to the ActorId for visual display coordination
                var dummyActorId = Domain.Grid.ActorId.NewId();

                var dummyResult = Domain.Actor.DummyActor.Presets.CreateCombatDummy("Combat Dummy");
                if (dummyResult.IsSucc)
                {
                    var dummyActor = dummyResult.Match(succ => succ, fail => throw new InvalidOperationException());

                    // Convert DummyActor to Actor for service registration (same as handler does)
                    var actorForRegistration = Domain.Actor.Actor.Create(dummyActor.Id, dummyActor.Health, dummyActor.Name);
                    if (actorForRegistration.IsSucc)
                    {
                        var actor = actorForRegistration.Match(succ => succ, fail => throw new InvalidOperationException());

                        // First, add the actor to the actor state service (for health data)
                        var addResult = _actorStateService.AddActor(actor);
                        if (addResult.IsSucc)
                        {
                            // Then, place the actor on the grid at the dummy position
                            var placeResult = _gridStateService.AddActorToGrid(actor.Id, dummyPosition);
                            if (placeResult.IsSucc)
                            {
                                // Display the dummy visually with brown/enemy coloring
                                _ = Task.Run(async () => await View.DisplayActorAsync(actor.Id, dummyPosition, ActorType.Enemy));

                                // CRITICAL: Notify the health presenter to create a health bar for the dummy
                                if (_healthPresenter != null)
                                {
                                    _ = Task.Run(async () => await _healthPresenter.HandleActorCreatedAsync(actor.Id, dummyPosition, actor.Health));
                                    _logger.Information("Dummy target created with health bar at {Position} - ID: {ActorId}, Health: {Health}",
                                        dummyPosition, actor.Id, actor.Health);
                                }
                                else
                                {
                                    _logger.Warning("HealthPresenter not connected - dummy health bar will not be displayed");
                                }
                            }
                            else
                            {
                                placeResult.IfFail(error => _logger.Warning("Failed to place dummy target on grid: {Error}", error.Message));
                            }
                        }
                        else
                        {
                            addResult.IfFail(error => _logger.Warning("Failed to add dummy target to actor state service: {Error}", error.Message));
                        }
                    }
                    else
                    {
                        actorForRegistration.IfFail(error => _logger.Warning("Failed to convert dummy to actor: {Error}", error.Message));
                    }
                }
                else
                {
                    dummyResult.IfFail(error => _logger.Warning("Failed to create dummy actor: {Error}", error.Message));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize dummy combat target");
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

                _logger.Information("All actor displays refreshed successfully");
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
