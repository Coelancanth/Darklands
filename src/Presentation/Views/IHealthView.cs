using System.Threading.Tasks;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Actor;

namespace Darklands.Core.Presentation.Views
{
    /// <summary>
    /// Interface for the health view that handles actor health bar visualization.
    /// Manages the visual representation of actor health states including damage and healing feedback.
    /// Abstracts Godot-specific implementation details following Clean Architecture principles.
    /// </summary>
    public interface IHealthView
    {
        /// <summary>
        /// Creates and displays a health bar for an actor at the specified position.
        /// </summary>
        /// <param name="actorId">Unique identifier for the actor</param>
        /// <param name="position">Grid position where the health bar should appear</param>
        /// <param name="health">Current health state to display</param>
        Task DisplayHealthBarAsync(ActorId actorId, Position position, Health health);

        /// <summary>
        /// Creates and displays a health bar for an actor at the specified position (synchronous).
        /// Uses CallDeferred for Godot thread safety in turn-based sequential processing.
        /// </summary>
        /// <param name="actorId">Unique identifier for the actor</param>
        /// <param name="position">Grid position where the health bar should appear</param>
        /// <param name="health">Current health state to display</param>
        void DisplayHealthBar(ActorId actorId, Position position, Health health);

        /// <summary>
        /// Updates an actor's health bar with new health values.
        /// Should provide smooth transitions for health changes.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="oldHealth">Previous health state (for animation reference)</param>
        /// <param name="newHealth">New health state to display</param>
        Task UpdateHealthAsync(ActorId actorId, Health oldHealth, Health newHealth);

        /// <summary>
        /// Moves a health bar to a new position when the actor moves.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="fromPosition">Previous position</param>
        /// <param name="toPosition">New position where the health bar should be displayed</param>
        Task MoveHealthBarAsync(ActorId actorId, Position fromPosition, Position toPosition);

        /// <summary>
        /// Removes a health bar from the display.
        /// Called when actors die or leave the battlefield.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="position">Last known position of the actor</param>
        Task RemoveHealthBarAsync(ActorId actorId, Position position);

        /// <summary>
        /// Shows visual feedback for health changes (damage numbers, healing effects).
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="feedbackType">Type of health feedback to display</param>
        /// <param name="amount">Amount of health change (damage/healing value)</param>
        /// <param name="position">Position where feedback should appear</param>
        Task ShowHealthFeedbackAsync(ActorId actorId, HealthFeedbackType feedbackType, int amount, Position position);

        /// <summary>
        /// Highlights a health bar to indicate targeting or special state.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="highlightType">Type of highlight to apply</param>
        Task HighlightHealthBarAsync(ActorId actorId, HealthHighlightType highlightType);

        /// <summary>
        /// Removes highlighting from a health bar.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        Task UnhighlightHealthBarAsync(ActorId actorId);

        /// <summary>
        /// Refreshes all health bar displays with their current states.
        /// Used for initialization or full refresh scenarios.
        /// </summary>
        Task RefreshAllHealthBarsAsync();

        /// <summary>
        /// Sets the visibility of a health bar based on player vision.
        /// Used by the fog of war system to show/hide health bars dynamically.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="isVisible">True to show the health bar, false to hide</param>
        Task SetHealthBarVisibilityAsync(ActorId actorId, bool isVisible);
    }

    /// <summary>
    /// Types of visual feedback that can be shown for health changes.
    /// Used to communicate health state changes to the player.
    /// </summary>
    public enum HealthFeedbackType
    {
        /// <summary>
        /// Actor took damage - show damage number.
        /// </summary>
        Damage,

        /// <summary>
        /// Actor was healed - show healing number.
        /// </summary>
        Healing,

        /// <summary>
        /// Actor died - show death effect.
        /// </summary>
        Death,

        /// <summary>
        /// Actor reached critical health - show warning.
        /// </summary>
        CriticalHealth,

        /// <summary>
        /// Actor fully recovered - show restoration effect.
        /// </summary>
        FullRestore
    }

    /// <summary>
    /// Types of highlighting that can be applied to health bars.
    /// Maps to different visual treatments without exposing rendering details.
    /// </summary>
    public enum HealthHighlightType
    {
        /// <summary>
        /// Health bar is being targeted by a healing spell or ability.
        /// </summary>
        HealTarget,

        /// <summary>
        /// Health bar is being targeted by a damaging attack.
        /// </summary>
        DamageTarget,

        /// <summary>
        /// Health bar is in a critical state (low health warning).
        /// </summary>
        Critical,

        /// <summary>
        /// Health bar belongs to the currently selected actor.
        /// </summary>
        Selected
    }
}
