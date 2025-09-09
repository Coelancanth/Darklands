using System;
using System.Threading.Tasks;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Actor;
using Darklands.Core.Presentation.Views;
using MediatR;
using Serilog;
using static LanguageExt.Prelude;

namespace Darklands.Core.Presentation.Presenters
{
    /// <summary>
    /// Presenter for the health view that manages actor health bar display and interactions.
    /// Handles health state updates, visual feedback for damage/healing, and coordinates with the application layer.
    /// Follows MVP pattern - contains presentation logic without view implementation details.
    /// </summary>
    public sealed class HealthPresenter : PresenterBase<IHealthView>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IActorStateService _actorStateService;
        private readonly ICombatQueryService _combatQueryService;

        /// <summary>
        /// Creates a new HealthPresenter with the specified dependencies.
        /// </summary>
        /// <param name="view">The health view interface this presenter controls</param>
        /// <param name="mediator">MediatR instance for sending commands and queries</param>
        /// <param name="logger">Logger for tracking health operations</param>
        /// <param name="actorStateService">Actor state service for health data access</param>
        /// <param name="combatQueryService">Combat query service for composite actor and position data</param>
        public HealthPresenter(IHealthView view, IMediator mediator, ILogger logger, IActorStateService actorStateService, ICombatQueryService combatQueryService)
            : base(view)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _actorStateService = actorStateService ?? throw new ArgumentNullException(nameof(actorStateService));
            _combatQueryService = combatQueryService ?? throw new ArgumentNullException(nameof(combatQueryService));
        }

        /// <summary>
        /// Initializes the health presenter and sets up the initial health display.
        /// Shows health bars for all active actors.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _logger.Information("HealthPresenter initialized, setting up initial health bars");

            try
            {
                // For Phase 4, display health for the test player if it exists
                // Future versions would load all actors from the application state
                _ = InitializeHealthBarsAsync();
                _logger.Debug("Initial health bar display setup initiated");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during HealthPresenter initialization");
            }
        }

        /// <summary>
        /// Sets up health bars for existing actors.
        /// This is a Phase 4 implementation - future versions will load from application state.
        /// Health bars are now created by ActorPresenter via HandleActorCreatedAsync() to avoid duplication.
        /// </summary>
        private async Task InitializeHealthBarsAsync()
        {
            try
            {
                // Health bars are now created by ActorPresenter during actor creation
                // This avoids the duplication issue where both HealthPresenter and ActorPresenter
                // were trying to create health bars for the same actors
                _logger.Debug("HealthPresenter ready - health bars will be created by ActorPresenter");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize health bars");
            }
        }

        /// <summary>
        /// Handles health change notifications from the application layer.
        /// Updates the visual representation when actor health changes.
        /// </summary>
        /// <param name="actorId">ID of the actor whose health changed</param>
        /// <param name="oldHealth">Previous health state</param>
        /// <param name="newHealth">New health state</param>
        public async Task HandleHealthChangedAsync(ActorId actorId, Health oldHealth, Health newHealth)
        {
            _logger.Information("Handling health change for {ActorId} from {OldHealth} to {NewHealth}",
                actorId, oldHealth, newHealth);

            try
            {
                // Update the health bar display
                await View.UpdateHealthAsync(actorId, oldHealth, newHealth);

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
                                _logger.Information("Actor {ActorId} died from health change", actorId);
                            }
                            else if (newHealth.HealthPercentage <= 0.25) // Critical at 25% health
                            {
                                await View.HighlightHealthBarAsync(actorId, HealthHighlightType.Critical);
                                await View.ShowHealthFeedbackAsync(actorId, HealthFeedbackType.CriticalHealth, 0, actorWithPosition.Position);
                                _logger.Warning("Actor {ActorId} is at critical health: {Health}", actorId, newHealth);
                            }
                        },
                        None: () =>
                        {
                            _logger.Warning("Actor {ActorId} not found when handling health change", actorId);
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
                                await View.UnhighlightHealthBarAsync(actorId);
                            }

                            // Show full restore effect if fully healed
                            if (newHealth.IsFullHealth && !oldHealth.IsFullHealth)
                            {
                                await View.ShowHealthFeedbackAsync(actorId, HealthFeedbackType.FullRestore, 0, actorWithPosition.Position);
                            }
                        },
                        None: () =>
                        {
                            _logger.Warning("Actor {ActorId} not found when handling health change", actorId);
                            return Task.CompletedTask;
                        }
                    );
                }

                _logger.Debug("Successfully updated health display for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error handling health change for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Handles actor movement notifications - moves health bars to follow actors.
        /// </summary>
        /// <param name="actorId">ID of the actor that moved</param>
        /// <param name="fromPosition">Previous position</param>
        /// <param name="toPosition">New position</param>
        public async Task HandleActorMovedAsync(ActorId actorId, Position fromPosition, Position toPosition)
        {
            _logger.Debug("Moving health bar for {ActorId} from {FromPosition} to {ToPosition}",
                actorId, fromPosition, toPosition);

            try
            {
                await View.MoveHealthBarAsync(actorId, fromPosition, toPosition);
                _logger.Debug("Successfully moved health bar for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error moving health bar for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Handles actor creation notifications - adds health bar for new actors.
        /// </summary>
        /// <param name="actorId">ID of the new actor</param>
        /// <param name="position">Position where the actor was created</param>
        /// <param name="health">Initial health state</param>
        public async Task HandleActorCreatedAsync(ActorId actorId, Position position, Health health)
        {
            _logger.Information("Creating health bar for new actor {ActorId} at {Position} with health {Health}",
                actorId, position, health);

            try
            {
                await View.DisplayHealthBarAsync(actorId, position, health);
                _logger.Debug("Successfully created health bar for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating health bar for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Handles actor creation synchronously for sequential turn-based processing.
        /// Part of TD_011 async→sync transformation.
        /// </summary>
        public void HandleActorCreated(ActorId actorId, Position position, Health health)
        {
            _logger.Information("Creating health bar for new actor {ActorId} at {Position} with health {Health}",
                actorId, position, health);

            try
            {
                View.DisplayHealthBar(actorId, position, health);
                _logger.Debug("Successfully created health bar for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating health bar for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Handles actor removal notifications - removes health bar when actors are destroyed.
        /// </summary>
        /// <param name="actorId">ID of the actor being removed</param>
        /// <param name="position">Last known position of the actor</param>
        public async Task HandleActorRemovedAsync(ActorId actorId, Position position)
        {
            _logger.Information("Removing health bar for actor {ActorId} at {Position}", actorId, position);

            try
            {
                await View.RemoveHealthBarAsync(actorId, position);
                _logger.Debug("Successfully removed health bar for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error removing health bar for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Handles actor targeting for healing spells or abilities.
        /// </summary>
        /// <param name="actorId">ID of the actor being targeted for healing</param>
        public async Task HandleHealTargetingAsync(ActorId actorId)
        {
            try
            {
                await View.HighlightHealthBarAsync(actorId, HealthHighlightType.HealTarget);
                _logger.Debug("Highlighted health bar for healing target {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error highlighting heal target for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Handles actor targeting for damage attacks.
        /// </summary>
        /// <param name="actorId">ID of the actor being targeted for damage</param>
        public async Task HandleDamageTargetingAsync(ActorId actorId)
        {
            try
            {
                await View.HighlightHealthBarAsync(actorId, HealthHighlightType.DamageTarget);
                _logger.Debug("Highlighted health bar for damage target {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error highlighting damage target for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Clears all targeting highlights.
        /// </summary>
        /// <param name="actorId">ID of the actor to unhighlight</param>
        public async Task HandleTargetingClearedAsync(ActorId actorId)
        {
            try
            {
                await View.UnhighlightHealthBarAsync(actorId);
                _logger.Debug("Cleared targeting highlight for {ActorId}", actorId);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error clearing targeting highlight for {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Refreshes the display of all health bars with their current states.
        /// Used for initialization or when health states have changed significantly.
        /// </summary>
        public async Task RefreshAllHealthBarsAsync()
        {
            try
            {
                _logger.Debug("Refreshing all health bar displays");

                await View.RefreshAllHealthBarsAsync();

                _logger.Debug("All health bar displays refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error refreshing all health bar displays");
            }
        }

        /// <summary>
        /// Disposes the presenter and cleans up any resources or subscriptions.
        /// </summary>
        public override void Dispose()
        {
            _logger.Information("HealthPresenter disposing and cleaning up resources");
            base.Dispose();
        }
    }
}
