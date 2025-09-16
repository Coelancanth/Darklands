using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Domain.Combat;
using Darklands.Application.Common;

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
            _logger?.Log(LogLevel.Information, LogCategory.System, "[GameManager] Received death notification for actor {ActorId} at {Position}",
                notification.ActorId, notification.Position);

            // Remove actor sprite using the same logic as the old callback
            if (_actorPresenter != null)
            {
                _logger?.Log(LogLevel.Information, LogCategory.System, "[GameManager] Calling deferred removal for {ActorId}", notification.ActorId);
                // Use CallDeferred to ensure this runs on the main thread
                CallDeferred(nameof(RemoveActorDeferred),
                    notification.ActorId.Value.ToString(),
                    notification.Position.X,
                    notification.Position.Y);
            }
            else
            {
                _logger?.Log(LogLevel.Error, LogCategory.System, "[GameManager] ActorPresenter is NULL - cannot remove sprite!");
            }

        }
        catch (System.Exception ex)
        {
            _logger?.Log(LogLevel.Error, LogCategory.System, "[GameManager] Error processing actor death notification for {ActorId}. Exception: {Exception}",
                notification.ActorId, ex);
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
            _logger?.Log(LogLevel.Information, LogCategory.System, "[GameManager] Received damage notification for actor {ActorId}: {OldHealth} → {NewHealth}",
                notification.ActorId, notification.OldHealth, notification.NewHealth);

            // Update health bar via ActorPresenter (consolidated functionality)
            if (_actorPresenter != null)
            {
                _logger?.Log(LogLevel.Information, LogCategory.System, "[GameManager] Updating health bar via ActorPresenter (consolidated from HealthPresenter)");
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
                _logger?.Log(LogLevel.Error, LogCategory.System, "[GameManager] ActorPresenter is NULL - cannot update health bar!");
            }

        }
        catch (System.Exception ex)
        {
            _logger?.Log(LogLevel.Error, LogCategory.System, "[GameManager] Error processing actor damage notification for {ActorId}. Exception: {Exception}",
                notification.ActorId, ex);
        }
    }
}
