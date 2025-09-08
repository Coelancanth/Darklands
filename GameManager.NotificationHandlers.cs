using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Domain.Combat;

namespace Darklands;

/// <summary>
/// Partial class for GameManager that contains event handlers.
/// These handlers are called by the GameManagerEventBridge which receives MediatR notifications.
/// </summary>
public partial class GameManager
{
    /// <summary>
    /// Handles ActorDiedEvent notifications from the combat system via the event bridge.
    /// Called by GameManagerEventBridge when MediatR publishes ActorDiedEvent.
    /// 
    /// Responsibilities:
    /// - Remove actor sprites from the UI
    /// - Update health bars and visual elements
    /// - Log death events for debugging
    /// </summary>
    private void HandleActorDiedEvent(ActorDiedEvent notification)
    {
        try
        {
            _logger?.Information("🎮 [GameManager] Received death notification for actor {ActorId} at {Position}", 
                notification.ActorId, notification.Position);

            // Remove actor sprite using the same logic as the old callback
            if (_actorPresenter != null)
            {
                _logger?.Information("🎮 [GameManager] Calling deferred removal for {ActorId}", notification.ActorId);
                // Use CallDeferred to ensure this runs on the main thread
                CallDeferred(nameof(RemoveActorDeferred), 
                    notification.ActorId.Value.ToString(), 
                    notification.Position.X, 
                    notification.Position.Y);
            }
            else
            {
                _logger?.Error("❌ [GameManager] ActorPresenter is NULL - cannot remove sprite!");
            }

        }
        catch (System.Exception ex)
        {
            _logger?.Error(ex, "💥 [GameManager] Error processing actor death notification for {ActorId}", 
                notification.ActorId);
        }
    }

    /// <summary>
    /// Handles ActorDamagedEvent notifications from the combat system via the event bridge.
    /// Called by GameManagerEventBridge when MediatR publishes ActorDamagedEvent.
    /// 
    /// Responsibilities:
    /// - Update health bars with new values
    /// - Trigger damage visualization effects
    /// - Log damage events for debugging
    /// </summary>
    private void HandleActorDamagedEvent(ActorDamagedEvent notification)
    {
        try
        {
            _logger?.Information("🩺 [GameManager] Received damage notification for actor {ActorId}: {OldHealth} → {NewHealth}", 
                notification.ActorId, notification.OldHealth, notification.NewHealth);

            // Update health bar via presenter using the same logic as the old callback
            if (_healthPresenter != null)
            {
                _logger?.Information("🩺 [GameManager] Updating health bar via HealthPresenter");
                // Use CallDeferred to ensure this runs on the main thread
                CallDeferred(nameof(UpdateHealthBarDeferred), 
                    notification.ActorId.Value.ToString(), 
                    notification.OldHealth.Current, 
                    notification.OldHealth.Maximum, 
                    notification.NewHealth.Current, 
                    notification.NewHealth.Maximum);
            }
            else
            {
                _logger?.Error("❌ [GameManager] HealthPresenter is NULL - cannot update health bar!");
            }

        }
        catch (System.Exception ex)
        {
            _logger?.Error(ex, "💥 [GameManager] Error processing actor damage notification for {ActorId}", 
                notification.ActorId);
        }
    }
}