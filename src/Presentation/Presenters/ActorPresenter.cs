using System;
using System.Threading.Tasks;
using Darklands.Core.Application.Common;
using Darklands.Core.Application.Grid.Queries;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Presentation.Views;
using Darklands.Core.Domain.Debug;
using Darklands.Core.Infrastructure.Debug;
using MediatR;
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
        private readonly ICategoryLogger _logger;
        private readonly IActorFactory _actorFactory;
        private readonly IActorStateService _actorStateService;
        private readonly ICombatQueryService _combatQueryService;
        private GridPresenter? _gridPresenter;

        /// <summary>
        /// Creates a new ActorPresenter with the specified dependencies.
        /// </summary>
        /// <param name="view">The actor view interface this presenter controls</param>
        /// <param name="mediator">MediatR instance for sending commands and queries</param>
        /// <param name="logger">Logger for tracking actor operations</param>
        /// <param name="actorFactory">Factory for creating and managing actors</param>
        /// <param name="actorStateService">Service for querying actor state including health</param>
        /// <param name="combatQueryService">Combat query service for composite actor and position data</param>
        public ActorPresenter(IActorView view, IMediator mediator, ICategoryLogger logger, IActorFactory actorFactory, IActorStateService actorStateService, ICombatQueryService combatQueryService)
            : base(view)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));
            _actorStateService = actorStateService ?? throw new ArgumentNullException(nameof(actorStateService));
            _combatQueryService = combatQueryService ?? throw new ArgumentNullException(nameof(combatQueryService));
        }


        /// <summary>
        /// Sets the grid presenter for coordinated vision updates.
        /// Called by GameManager during MVP setup.
        /// </summary>
        /// <param name="gridPresenter">The grid presenter to coordinate with</param>
        public void SetGridPresenter(GridPresenter gridPresenter)
        {
            _gridPresenter = gridPresenter ?? throw new ArgumentNullException(nameof(gridPresenter));
            _logger.Log(LogLevel.Debug, LogCategory.System, "ActorPresenter connected to GridPresenter for vision updates");
        }

        /// <summary>
        /// Initializes the actor presenter and sets up the initial actor display.
        /// Creates test actors using the actor factory for clean separation of concerns.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _logger.Log(LogCategory.System, "ActorPresenter initialized, setting up initial actors");

            try
            {
                // Create initial actors using the factory (clean separation of concerns)
                try
                {
                    // Create test player at strategic center position (15,10)
                    var playerResult = _actorFactory.CreatePlayer(new Domain.Grid.Position(15, 10), "Player");
                    playerResult.Match(
                        Succ: playerId =>
                        {
                            // Display the actor visually (health bar is created as child)
                            _ = Task.Run(async () =>
                            {
                                await View.DisplayActorAsync(playerId, new Domain.Grid.Position(15, 10), ActorType.Player);

                                // Initialize health bar with correct values immediately after display
                                await InitializeActorHealthBar(playerId);
                            });

                            _logger.Log(LogLevel.Debug, LogCategory.System, "Player created at strategic center (15,10) with ID {0}", playerId);

                            // Trigger initial vision update after player creation
                            if (_gridPresenter != null)
                            {
                                _ = Task.Run(async () => await _gridPresenter.UpdatePlayerVisionAsync(1));
                                _logger.Log(LogLevel.Debug, LogCategory.System, "Triggered initial vision update after player creation");
                            }
                            else
                            {
                                _logger.Log(LogLevel.Warning, LogCategory.System, "GridPresenter not connected - initial vision update skipped");
                            }
                        },
                        Fail: error => _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to create test player: {0}", error.Message)
                    );

                    // Create dummy target at position (5,5)
                    var dummyResult = _actorFactory.CreateDummy(new Domain.Grid.Position(5, 5), 50);
                    dummyResult.Match(
                        Succ: dummyId =>
                        {
                            // Display the dummy visually (health bar is created as child)
                            _ = Task.Run(async () =>
                            {
                                await View.DisplayActorAsync(dummyId, new Domain.Grid.Position(5, 5), ActorType.Enemy);

                                // Initialize health bar with correct values immediately after display
                                await InitializeActorHealthBar(dummyId);
                            });

                            _logger.Log(LogLevel.Debug, LogCategory.System, "Dummy target created at position (5,5) with ID {0}", dummyId);
                        },
                        Fail: error => _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to create dummy target: {0}", error.Message)
                    );

                    _logger.Log(LogLevel.Debug, LogCategory.System, "Initial actor display setup completed - player and dummy target");
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, LogCategory.System, "Error during actor initialization: {0}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.System, "Error during ActorPresenter initialization: {0}", ex.Message);
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
            _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Handling actor move for {0} from {1} to {2}",
                actorId, fromPosition, toPosition);

            try
            {
                // Update the actor's visual position
                // Health bar moves automatically as a child node
                await View.MoveActorAsync(actorId, fromPosition, toPosition);

                // Show brief success feedback
                await View.ShowActorFeedbackAsync(actorId, ActorFeedbackType.ActionSuccess, "Moved");

                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Successfully updated actor position for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Gameplay, "Error handling actor move for {0}: {1}", actorId, ex.Message);
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
                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Actor {ActorId} selected and highlighted", actorId);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Error handling actor selection for {0}: {1}", actorId, ex.Message);
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
                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Actor {ActorId} deselected and unhighlighted", actorId);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Error handling actor deselection for {ActorId}. Exception: {Exception}", actorId, ex);
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
                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Refreshing all actor displays");

                // For Phase 4, we only have the test player
                // Future versions would query the application layer for all actors
                await View.RefreshAllActorsAsync();

                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "All actor displays refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Gameplay, "Error refreshing all actor displays. Exception: {Exception}", ex);
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
                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Updated actor {ActorId} at position {Position}", actorId, position);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Error updating actor {ActorId} at position {Position}. Exception: {Exception}", actorId, position, ex);
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
                _logger.Log(LogCategory.System, "Removed actor {ActorId} from position {Position}", actorId, position);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Gameplay, "Error removing actor {ActorId} from position {Position}: {1}", actorId, position, ex.Message);
            }
        }

        /// <summary>
        /// Sets the visibility of an actor based on player vision.
        /// Health bar visibility is handled automatically as it's a child node.
        /// </summary>
        /// <param name="actorId">ID of the actor to show/hide</param>
        /// <param name="isVisible">True to show the actor, false to hide</param>
        public async Task SetActorVisibilityAsync(Domain.Grid.ActorId actorId, bool isVisible)
        {
            try
            {
                // Update actor visibility in the view
                // Health bar will be hidden/shown automatically as a child node
                await View.SetActorVisibilityAsync(actorId, isVisible);

                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Set actor {0} visibility to {1}",
                    actorId.Value.ToString()[..8], isVisible ? "VISIBLE" : "HIDDEN");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Gameplay, "Error setting actor visibility for {ActorId}: {1}", actorId, ex.Message);
            }
        }

        /// <summary>
        /// Updates an actor's health bar when health changes occur.
        /// Called by HealthPresenter to maintain coordination between health and actor systems.
        /// </summary>
        /// <param name="actorId">ID of the actor whose health changed</param>
        /// <param name="currentHealth">New current health value</param>
        /// <param name="maxHealth">Maximum health value</param>
        public async Task UpdateActorHealthAsync(Domain.Grid.ActorId actorId, int currentHealth, int maxHealth)
        {
            try
            {
                // Update the health bar via the actor view interface
                View.UpdateActorHealth(actorId, currentHealth, maxHealth);
                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Updated health bar for actor {0} to {1}/{2}",
                    actorId, currentHealth, maxHealth);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Gameplay, "Error updating health bar for actor {ActorId}: {1}", actorId, ex.Message);
            }
        }

        /// <summary>
        /// Initializes an actor's health bar with correct health values after display.
        /// Called after DisplayActorAsync to set the correct initial health display.
        /// </summary>
        /// <param name="actorId">ID of the actor whose health bar needs initialization</param>
        private async Task InitializeActorHealthBar(Domain.Grid.ActorId actorId)
        {
            try
            {
                // Query for the actor's current health
                var actorOption = _actorStateService.GetActor(actorId);
                actorOption.Match(
                    Some: actor =>
                    {
                        // Update health bar with actual values
                        View.UpdateActorHealth(actorId, actor.Health.Current, actor.Health.Maximum);
                        _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Initialized health bar for actor {0} with {1}/{2} HP",
                            actorId, actor.Health.Current, actor.Health.Maximum);
                    },
                    None: () => _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Could not initialize health bar - actor {ActorId} not found in state service", actorId)
                );

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Gameplay, "Error initializing health bar for actor {ActorId}: {1}", actorId, ex.Message);
            }
        }

        // Health-specific methods consolidated from HealthPresenter

        /// <summary>
        /// Handles health change notifications from the application layer.
        /// Updates the health bar display and shows appropriate feedback.
        /// </summary>
        /// <param name="actorId">ID of the actor whose health changed</param>
        /// <param name="oldHealth">Previous health state</param>
        /// <param name="newHealth">New health state</param>
        public async Task HandleHealthChangedAsync(Domain.Grid.ActorId actorId, Domain.Actor.Health oldHealth, Domain.Actor.Health newHealth)
        {
            _logger.Log(LogCategory.System, "Handling health change for {0} from {1} to {2}",
                actorId, oldHealth, newHealth);

            try
            {
                // Update the health bar display via the consolidated ActorView
                await View.UpdateActorHealthAsync(actorId, oldHealth, newHealth);

                // Show appropriate feedback based on the change
                if (newHealth.Current < oldHealth.Current)
                {
                    // Damage was taken
                    var damage = oldHealth.Current - newHealth.Current;
                    var actorWithPositionOption = _combatQueryService.GetActorWithPosition(actorId);
                    await actorWithPositionOption.Match(
                        Some: async actorWithPosition =>
                        {
                            await View.ShowHealthFeedbackAsync(actorId, HealthFeedbackType.Damage, damage, actorWithPosition.Position);

                            // Check for critical health or death
                            if (newHealth.IsDead)
                            {
                                await View.ShowHealthFeedbackAsync(actorId, HealthFeedbackType.Death, 0, actorWithPosition.Position);
                                _logger.Log(LogCategory.System, "Actor {ActorId} died from health change", actorId);
                            }
                            else if (newHealth.HealthPercentage <= 0.25) // Critical at 25% health
                            {
                                await View.HighlightActorHealthBarAsync(actorId, HealthHighlightType.Critical);
                                await View.ShowHealthFeedbackAsync(actorId, HealthFeedbackType.CriticalHealth, 0, actorWithPosition.Position);
                                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor {ActorId} is at critical health: {Health}", actorId, newHealth);
                            }
                        },
                        None: () =>
                        {
                            _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor {ActorId} not found when handling health change", actorId);
                            return Task.CompletedTask;
                        }
                    );
                }
                else if (newHealth.Current > oldHealth.Current)
                {
                    // Healing was applied
                    var healing = newHealth.Current - oldHealth.Current;
                    var actorWithPositionOption = _combatQueryService.GetActorWithPosition(actorId);
                    await actorWithPositionOption.Match(
                        Some: async actorWithPosition =>
                        {
                            await View.ShowHealthFeedbackAsync(actorId, HealthFeedbackType.Healing, healing, actorWithPosition.Position);

                            // Remove critical highlighting if healed above 25%
                            if (oldHealth.HealthPercentage <= 0.25 && newHealth.HealthPercentage > 0.25)
                            {
                                await View.UnhighlightActorHealthBarAsync(actorId);
                            }

                            // Show full restore effect if fully healed
                            if (newHealth.IsFullHealth && !oldHealth.IsFullHealth)
                            {
                                await View.ShowHealthFeedbackAsync(actorId, HealthFeedbackType.FullRestore, 0, actorWithPosition.Position);
                            }
                        },
                        None: () =>
                        {
                            _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor {ActorId} not found when handling health change", actorId);
                            return Task.CompletedTask;
                        }
                    );
                }

                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Successfully updated health display for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Gameplay, "Error handling health change for {ActorId}: {1}", actorId, ex.Message);
            }
        }

        /// <summary>
        /// Handles actor targeting for healing spells or abilities.
        /// </summary>
        /// <param name="actorId">ID of the actor being targeted for healing</param>
        public async Task HandleHealTargetingAsync(Domain.Grid.ActorId actorId)
        {
            try
            {
                await View.HighlightActorHealthBarAsync(actorId, HealthHighlightType.HealTarget);
                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Highlighted health bar for healing target {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Error highlighting heal target for {ActorId}. Exception: {Exception}", actorId, ex);
            }
        }

        /// <summary>
        /// Handles actor targeting for damage attacks.
        /// </summary>
        /// <param name="actorId">ID of the actor being targeted for damage</param>
        public async Task HandleDamageTargetingAsync(Domain.Grid.ActorId actorId)
        {
            try
            {
                await View.HighlightActorHealthBarAsync(actorId, HealthHighlightType.DamageTarget);
                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Highlighted health bar for damage target {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Error highlighting damage target for {ActorId}. Exception: {Exception}", actorId, ex);
            }
        }

        /// <summary>
        /// Clears all targeting highlights.
        /// </summary>
        /// <param name="actorId">ID of the actor to unhighlight</param>
        public async Task HandleTargetingClearedAsync(Domain.Grid.ActorId actorId)
        {
            try
            {
                await View.UnhighlightActorHealthBarAsync(actorId);
                _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Cleared targeting highlight for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay, "Error clearing targeting highlight for {ActorId}. Exception: {Exception}", actorId, ex);
            }
        }

        /// <summary>
        /// Disposes the presenter and cleans up any resources or subscriptions.
        /// </summary>
        public override void Dispose()
        {
            _logger.Log(LogCategory.System, "ActorPresenter disposing and cleaning up resources");
            base.Dispose();
        }
    }
}
