using System.Threading.Tasks;
using Darklands.Domain.Grid;
using Darklands.Domain.Actor;

namespace Darklands.Application.Presentation.Views
{
    /// <summary>
    /// Interface for the actor view that handles character/unit visualization and positioning.
    /// Manages the visual representation of actors on the tactical grid.
    /// Abstracts Godot-specific implementation details following Clean Architecture principles.
    /// </summary>
    public interface IActorView
    {
        /// <summary>
        /// Creates and displays a new actor at the specified position.
        /// </summary>
        /// <param name="actorId">Unique identifier for the actor</param>
        /// <param name="position">Grid position where the actor should appear</param>
        /// <param name="actorType">Type of actor to display (affects visual representation)</param>
        Task DisplayActorAsync(ActorId actorId, Position position, ActorType actorType);

        /// <summary>
        /// Updates an existing actor's position on the grid.
        /// Should provide smooth transition between positions.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor to move</param>
        /// <param name="fromPosition">Previous position (for animation reference)</param>
        /// <param name="toPosition">New position where the actor should be displayed</param>
        Task MoveActorAsync(ActorId actorId, Position fromPosition, Position toPosition);

        /// <summary>
        /// Updates an actor's visual state or appearance.
        /// Used for showing status effects, health changes, or other state modifications.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor to update</param>
        /// <param name="position">Current position of the actor</param>
        /// <param name="actorType">Updated actor type (if changed)</param>
        Task UpdateActorAsync(ActorId actorId, Position position, ActorType actorType);

        /// <summary>
        /// Removes an actor from the visual display.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor to remove</param>
        /// <param name="position">Position where the actor was located</param>
        Task RemoveActorAsync(ActorId actorId, Position position);

        /// <summary>
        /// Highlights an actor to indicate selection, targeting, or special state.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor to highlight</param>
        /// <param name="highlightType">Type of highlight to apply</param>
        Task HighlightActorAsync(ActorId actorId, ActorHighlightType highlightType);

        /// <summary>
        /// Removes highlighting from an actor.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor to unhighlight</param>
        Task UnhighlightActorAsync(ActorId actorId);

        /// <summary>
        /// Shows visual feedback related to an actor (damage, healing, status effects).
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="feedbackType">Type of feedback to display</param>
        /// <param name="message">Message or value to show (optional)</param>
        Task ShowActorFeedbackAsync(ActorId actorId, ActorFeedbackType feedbackType, string? message = null);

        /// <summary>
        /// Sets the visibility of an actor based on player vision.
        /// Used by the fog of war system to show/hide actors dynamically.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="isVisible">True to show the actor, false to hide</param>
        Task SetActorVisibilityAsync(ActorId actorId, bool isVisible);

        /// <summary>
        /// Refreshes the display of all actors with their current states.
        /// Used for initialization or full refresh scenarios.
        /// </summary>
        Task RefreshAllActorsAsync();

        /// <summary>
        /// Updates the health bar display for an actor.
        /// Called when actor health changes to reflect new health values.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="currentHealth">Current health value to display</param>
        /// <param name="maxHealth">Maximum health value for the health bar</param>
        void UpdateActorHealth(ActorId actorId, int currentHealth, int maxHealth);

        // Health-specific methods consolidated from IHealthView
        /// <summary>
        /// Updates an actor's health bar with new health values.
        /// Should provide smooth transitions for health changes.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="oldHealth">Previous health state (for animation reference)</param>
        /// <param name="newHealth">New health state to display</param>
        Task UpdateActorHealthAsync(ActorId actorId, Health oldHealth, Health newHealth);

        /// <summary>
        /// Shows visual feedback for health changes (damage numbers, healing effects).
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="feedbackType">Type of health feedback to display</param>
        /// <param name="amount">Amount of health change (damage/healing value)</param>
        /// <param name="position">Position where feedback should appear</param>
        Task ShowHealthFeedbackAsync(ActorId actorId, HealthFeedbackType feedbackType, int amount, Position position);

        /// <summary>
        /// Highlights an actor's health bar to indicate targeting or special state.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        /// <param name="highlightType">Type of highlight to apply</param>
        Task HighlightActorHealthBarAsync(ActorId actorId, HealthHighlightType highlightType);

        /// <summary>
        /// Removes highlighting from an actor's health bar.
        /// </summary>
        /// <param name="actorId">Unique identifier of the actor</param>
        Task UnhighlightActorHealthBarAsync(ActorId actorId);
    }

    /// <summary>
    /// Types of actors that can be displayed on the grid.
    /// Determines the visual representation used for each actor.
    /// </summary>
    public enum ActorType
    {
        /// <summary>
        /// Player-controlled character.
        /// </summary>
        Player,

        /// <summary>
        /// Enemy unit.
        /// </summary>
        Enemy,

        /// <summary>
        /// Neutral or allied NPC.
        /// </summary>
        Neutral,

        /// <summary>
        /// Environmental object that can be interacted with.
        /// </summary>
        Interactive
    }

    /// <summary>
    /// Types of highlighting that can be applied to actors.
    /// Maps to different visual treatments without exposing rendering details.
    /// </summary>
    public enum ActorHighlightType
    {
        /// <summary>
        /// Actor is currently selected by the player.
        /// </summary>
        Selected,

        /// <summary>
        /// Actor is being targeted by an action.
        /// </summary>
        Targeted,

        /// <summary>
        /// Actor is being hovered over by the cursor.
        /// </summary>
        Hover,

        /// <summary>
        /// Actor is in an active or alert state.
        /// </summary>
        Active,

        /// <summary>
        /// Actor is in a defensive or warning state.
        /// </summary>
        Warning
    }

    /// <summary>
    /// Types of visual feedback that can be shown for actors.
    /// Used to communicate game state changes to the player.
    /// </summary>
    public enum ActorFeedbackType
    {
        /// <summary>
        /// Actor took damage.
        /// </summary>
        Damage,

        /// <summary>
        /// Actor was healed.
        /// </summary>
        Healing,

        /// <summary>
        /// Actor gained a positive status effect.
        /// </summary>
        BuffApplied,

        /// <summary>
        /// Actor gained a negative status effect.
        /// </summary>
        DebuffApplied,

        /// <summary>
        /// Actor performed a successful action.
        /// </summary>
        ActionSuccess,

        /// <summary>
        /// Actor's action failed or was blocked.
        /// </summary>
        ActionFailed
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
