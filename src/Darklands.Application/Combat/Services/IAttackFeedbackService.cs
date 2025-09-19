using System.Threading.Tasks;
using Darklands.Domain.Grid;
using Darklands.Domain.Combat;

namespace Darklands.Application.Combat.Services
{
    /// <summary>
    /// Service interface for providing attack feedback and visual effects.
    /// Allows the application layer to request presentation feedback without depending on presentation layer directly.
    /// Implemented by presentation layer components to maintain Clean Architecture boundaries.
    /// </summary>
    public interface IAttackFeedbackService
    {
        /// <summary>
        /// Provides feedback for a successful attack.
        /// </summary>
        /// <param name="attackerId">The actor that performed the attack</param>
        /// <param name="targetId">The target that was attacked</param>
        /// <param name="combatAction">The combat action that was performed</param>
        /// <param name="damage">Amount of damage dealt</param>
        /// <param name="wasLethal">Whether the attack killed the target</param>
        Task ProcessAttackSuccessAsync(ActorId attackerId, ActorId targetId, CombatAction combatAction, int damage, bool wasLethal);

        /// <summary>
        /// Provides feedback for a successful attack (synchronous - TD_011 async→sync transformation).
        /// </summary>
        /// <param name="attackerId">The actor that performed the attack</param>
        /// <param name="targetId">The target that was attacked</param>
        /// <param name="combatAction">The combat action that was performed</param>
        /// <param name="damage">Amount of damage dealt</param>
        /// <param name="wasLethal">Whether the attack killed the target</param>
        void ProcessAttackSuccess(ActorId attackerId, ActorId targetId, CombatAction combatAction, int damage, bool wasLethal);

        /// <summary>
        /// Provides feedback for a failed attack.
        /// </summary>
        /// <param name="attackerId">The actor that attempted the attack</param>
        /// <param name="targetId">The intended target</param>
        /// <param name="combatAction">The combat action that was attempted</param>
        /// <param name="reason">Reason why the attack failed</param>
        Task ProcessAttackFailureAsync(ActorId attackerId, ActorId targetId, CombatAction combatAction, string reason);
    }
}
