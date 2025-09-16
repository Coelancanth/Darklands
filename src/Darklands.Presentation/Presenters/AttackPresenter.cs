using System;
using System.Threading.Tasks;
using Darklands.Application.Grid.Services;
using Darklands.Application.Combat.Services;
using Darklands.Application.Common;
using Darklands.Domain.Grid;
using Darklands.Domain.Combat;
using Darklands.Presentation.Views;
using MediatR;
using static LanguageExt.Prelude;

namespace Darklands.Presentation.Presenters
{
    /// <summary>
    /// Presenter for attack animations and visual feedback.
    /// Handles combat visual effects, damage display, and attack success/failure feedback.
    /// Follows MVP pattern - contains presentation logic without view implementation details.
    /// Implements IAttackFeedbackService to provide feedback to the application layer.
    /// </summary>
    public sealed class AttackPresenter : PresenterBase<IAttackView>, IAttackPresenter, IAttackFeedbackService
    {
        private readonly IGridStateService _gridStateService;
        private IActorView? _actorView;
        private readonly ICategoryLogger _logger;

        /// <summary>
        /// Creates a new AttackPresenter with the specified dependencies.
        /// The view will be attached later via AttachView method.
        /// </summary>
        /// <param name="gridStateService">Grid state service for position lookup</param>
        /// <param name="logger">Logger for combat messages (serves as combat log)</param>
        public AttackPresenter(IGridStateService gridStateService, ICategoryLogger logger)
            : base()
        {
            _gridStateService = gridStateService ?? throw new ArgumentNullException(nameof(gridStateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sets the actor view for coordinated visual feedback.
        /// Called during view initialization after presenters are wired up.
        /// </summary>
        public void SetActorView(IActorView actorView)
        {
            _actorView = actorView ?? throw new ArgumentNullException(nameof(actorView));
        }

        /// <summary>
        /// Attaches a view to this presenter.
        /// Overrides base implementation to properly handle the view attachment.
        /// </summary>
        public new void AttachView(IAttackView view)
        {
            base.AttachView(view);
            // Additional initialization if needed
        }

        /// <summary>
        /// Legacy method for backward compatibility.
        /// </summary>
        void IAttackPresenter.AttachView(IAttackView view)
        {
            AttachView(view);
        }


        /// <summary>
        /// Processes a successful attack with full visual and logging feedback.
        /// Orchestrates attack animation, damage effects, actor feedback, and combat logging.
        /// </summary>
        /// <param name="attackerId">The actor that performed the attack</param>
        /// <param name="targetId">The target that was attacked</param>
        /// <param name="combatAction">The combat action that was performed</param>
        /// <param name="damage">Amount of damage dealt</param>
        /// <param name="wasLethal">Whether the attack killed the target</param>
        public async Task ProcessAttackSuccessAsync(ActorId attackerId, ActorId targetId, CombatAction combatAction, int damage, bool wasLethal)
        {
            try
            {
                // Get positions for animations
                var attackerPos = _gridStateService.GetActorPosition(attackerId);
                var targetPos = _gridStateService.GetActorPosition(targetId);

                if (attackerPos.IsNone || targetPos.IsNone)
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Combat, "Attack success feedback failed: Could not find positions for attacker {AttackerId} or target {TargetId}",
                        attackerId, targetId);
                    return;
                }

                var attackerPosition = attackerPos.IfNone(() => throw new InvalidOperationException("Position should exist"));
                var targetPosition = targetPos.IfNone(() => throw new InvalidOperationException("Position should exist"));

                // Log combat message (this serves as our combat log)
                if (wasLethal)
                {
                    _logger.Log(LogLevel.Information, LogCategory.Combat, "{AttackerName} killed {TargetName} with {AttackName} for {Damage} damage",
                        attackerId, targetId, combatAction.Name, damage);
                }
                else
                {
                    _logger.Log(LogLevel.Information, LogCategory.Combat, "{AttackerName} hit {TargetName} with {AttackName} for {Damage} damage",
                        attackerId, targetId, combatAction.Name, damage);
                }

                // Play attack animation
                await View.PlayAttackAnimationAsync(attackerId, targetId, attackerPosition, targetPosition, combatAction);

                // Show damage effect on target
                await View.ShowDamageEffectAsync(targetId, targetPosition, damage, wasLethal);

                // Show success feedback on attacker
                await View.ShowAttackSuccessAsync(attackerId, attackerPosition, combatAction.Name);

                // Coordinate with actor view for damage feedback
                if (_actorView != null)
                {
                    await _actorView.ShowActorFeedbackAsync(targetId, ActorFeedbackType.Damage, damage.ToString());
                    await _actorView.ShowActorFeedbackAsync(attackerId, ActorFeedbackType.ActionSuccess);
                }

                // Handle death effects if lethal
                if (wasLethal)
                {
                    await View.ShowDeathEffectAsync(targetId, targetPosition);
                    _logger.Log(LogLevel.Information, LogCategory.Combat, "{TargetName} has been defeated", targetId);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Combat, "Error processing attack success feedback for {AttackerId} -> {TargetId}: {Error}",
                    attackerId, targetId, ex.Message);
            }
        }

        /// <summary>
        /// Processes a successful attack (synchronous - TD_011 async→sync transformation).
        /// Uses CallDeferred for Godot operations to maintain thread safety.
        /// </summary>
        public void ProcessAttackSuccess(ActorId attackerId, ActorId targetId, CombatAction combatAction, int damage, bool wasLethal)
        {
            try
            {
                // Get positions for animations
                var attackerPos = _gridStateService.GetActorPosition(attackerId);
                var targetPos = _gridStateService.GetActorPosition(targetId);

                if (attackerPos.IsNone || targetPos.IsNone)
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Combat, "Attack success feedback failed: Could not find positions for attacker {AttackerId} or target {TargetId}",
                        attackerId, targetId);
                    return;
                }

                var attackerPosition = attackerPos.IfNone(() => throw new InvalidOperationException("Position should exist"));
                var targetPosition = targetPos.IfNone(() => throw new InvalidOperationException("Position should exist"));

                // Log combat message (this serves as our combat log)
                if (wasLethal)
                {
                    _logger.Log(LogLevel.Information, LogCategory.Combat, "{AttackerName} killed {TargetName} with {AttackName} for {Damage} damage",
                        attackerId, targetId, combatAction.Name, damage);
                }
                else
                {
                    _logger.Log(LogLevel.Information, LogCategory.Combat, "{AttackerName} hit {TargetName} with {AttackName} for {Damage} damage",
                        attackerId, targetId, combatAction.Name, damage);
                }

                // TODO: Implement synchronous visual feedback using CallDeferred
                // - Play attack animation via CallDeferred
                // - Show damage effect via CallDeferred  
                // - Show success feedback via CallDeferred
                // - Coordinate with actor view for feedback via direct calls
                // - Handle death effects if lethal via CallDeferred

                _logger.Log(LogLevel.Debug, LogCategory.Combat, "Attack feedback processed synchronously for {AttackerId} -> {TargetId}", attackerId, targetId);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Combat, "Error processing attack success feedback for {AttackerId} -> {TargetId}: {Error}",
                    attackerId, targetId, ex.Message);
            }
        }

        /// <summary>
        /// Processes a failed attack with appropriate visual and logging feedback.
        /// </summary>
        /// <param name="attackerId">The actor that attempted the attack</param>
        /// <param name="targetId">The intended target</param>
        /// <param name="combatAction">The combat action that was attempted</param>
        /// <param name="reason">Reason why the attack failed</param>
        public async Task ProcessAttackFailureAsync(ActorId attackerId, ActorId targetId, CombatAction combatAction, string reason)
        {
            try
            {
                // Get positions for feedback
                var attackerPos = _gridStateService.GetActorPosition(attackerId);
                var targetPos = _gridStateService.GetActorPosition(targetId);

                if (attackerPos.IsNone)
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Combat, "Attack failure feedback failed: Could not find attacker {AttackerId} position", attackerId);
                    return;
                }

                var attackerPosition = attackerPos.IfNone(() => throw new InvalidOperationException("Position should exist"));
                var targetPosition = targetPos.IfNone(new Position(0, 0)); // Default if target not on grid

                // Log combat message
                _logger.Log(LogLevel.Warning, LogCategory.Combat, "{AttackerName} failed to attack {TargetName} with {AttackName}: {Reason}",
                    attackerId, targetId, combatAction.Name, reason);

                // Show failure feedback
                await View.ShowAttackFailureAsync(attackerId, targetId, attackerPosition, targetPosition, reason);

                // Coordinate with actor view for failure feedback
                if (_actorView != null)
                {
                    await _actorView.ShowActorFeedbackAsync(attackerId, ActorFeedbackType.ActionFailed, reason);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Combat, "Error processing attack failure feedback for {AttackerId} -> {TargetId}: {Error}",
                    attackerId, targetId, ex.Message);
            }
        }

        /// <summary>
        /// Highlights valid attack targets for a given attacker.
        /// Used for player targeting UI.
        /// </summary>
        /// <param name="attackerId">The actor that can perform attacks</param>
        public async Task HighlightValidTargetsAsync(ActorId attackerId)
        {
            try
            {
                var attackerPos = _gridStateService.GetActorPosition(attackerId);
                if (attackerPos.IsNone)
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Combat, "Cannot highlight targets: Attacker {AttackerId} not found on grid", attackerId);
                    return;
                }

                var attackerPosition = attackerPos.IfNone(() => throw new InvalidOperationException("Position should exist"));

                // Get all adjacent positions (valid melee targets)
                var validTargets = GetAdjacentPositions(attackerPosition);

                await View.HighlightAttackTargetsAsync(attackerId, attackerPosition, validTargets);
                _logger.Log(LogLevel.Debug, LogCategory.Combat, "Highlighted {TargetCount} valid attack targets for {AttackerId}",
                    validTargets.Length, attackerId);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Combat, "Error highlighting attack targets for {AttackerId}: {Error}", attackerId, ex.Message);
            }
        }

        /// <summary>
        /// Clears attack target highlighting.
        /// </summary>
        public async Task ClearTargetHighlightingAsync()
        {
            try
            {
                await View.ClearAttackHighlightingAsync();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Combat, "Error clearing attack target highlighting: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Gets all positions adjacent to the given position (8-directional).
        /// Used for melee attack range calculation.
        /// </summary>
        /// <param name="center">The center position</param>
        /// <returns>Array of adjacent positions</returns>
        private static Position[] GetAdjacentPositions(Position center)
        {
            return new[]
            {
                new Position(center.X - 1, center.Y - 1), // Top-left
                new Position(center.X, center.Y - 1),     // Top
                new Position(center.X + 1, center.Y - 1), // Top-right
                new Position(center.X - 1, center.Y),     // Left
                new Position(center.X + 1, center.Y),     // Right
                new Position(center.X - 1, center.Y + 1), // Bottom-left
                new Position(center.X, center.Y + 1),     // Bottom
                new Position(center.X + 1, center.Y + 1)  // Bottom-right
            };
        }
    }
}
