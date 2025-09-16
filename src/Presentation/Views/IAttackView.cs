using System.Threading.Tasks;
using Darklands.Domain.Grid;
using Darklands.Domain.Combat;

namespace Darklands.Application.Presentation.Views
{
    /// <summary>
    /// Interface for the attack view that handles combat animations and visual feedback.
    /// Manages attack animations, damage effects, and combat-related visual feedback.
    /// Abstracts Godot-specific implementation details following Clean Architecture principles.
    /// </summary>
    public interface IAttackView
    {
        /// <summary>
        /// Plays an attack animation between attacker and target.
        /// </summary>
        /// <param name="attackerId">The actor performing the attack</param>
        /// <param name="targetId">The target of the attack</param>
        /// <param name="attackerPosition">Position of the attacker</param>
        /// <param name="targetPosition">Position of the target</param>
        /// <param name="combatAction">The combat action being performed</param>
        Task PlayAttackAnimationAsync(ActorId attackerId, ActorId targetId, Position attackerPosition, Position targetPosition, CombatAction combatAction);

        /// <summary>
        /// Shows damage feedback on the target.
        /// </summary>
        /// <param name="targetId">The actor that took damage</param>
        /// <param name="targetPosition">Position of the target</param>
        /// <param name="damage">Amount of damage dealt</param>
        /// <param name="wasLethal">Whether this damage killed the target</param>
        Task ShowDamageEffectAsync(ActorId targetId, Position targetPosition, int damage, bool wasLethal);

        /// <summary>
        /// Shows attack success feedback on the attacker.
        /// </summary>
        /// <param name="attackerId">The actor that performed the attack</param>
        /// <param name="attackerPosition">Position of the attacker</param>
        /// <param name="attackName">Name of the successful attack</param>
        Task ShowAttackSuccessAsync(ActorId attackerId, Position attackerPosition, string attackName);

        /// <summary>
        /// Shows attack failure feedback.
        /// </summary>
        /// <param name="attackerId">The actor that attempted the attack</param>
        /// <param name="targetId">The intended target</param>
        /// <param name="attackerPosition">Position of the attacker</param>
        /// <param name="targetPosition">Position of the target</param>
        /// <param name="reason">Reason the attack failed</param>
        Task ShowAttackFailureAsync(ActorId attackerId, ActorId targetId, Position attackerPosition, Position targetPosition, string reason);

        /// <summary>
        /// Shows death animation and effects for an actor.
        /// </summary>
        /// <param name="actorId">The actor that died</param>
        /// <param name="position">Position where the actor died</param>
        Task ShowDeathEffectAsync(ActorId actorId, Position position);

        /// <summary>
        /// Highlights valid attack targets for the current attacker.
        /// </summary>
        /// <param name="attackerId">The actor that can attack</param>
        /// <param name="attackerPosition">Position of the potential attacker</param>
        /// <param name="validTargets">Positions that can be targeted</param>
        Task HighlightAttackTargetsAsync(ActorId attackerId, Position attackerPosition, Position[] validTargets);

        /// <summary>
        /// Clears attack target highlighting.
        /// </summary>
        Task ClearAttackHighlightingAsync();
    }
}
