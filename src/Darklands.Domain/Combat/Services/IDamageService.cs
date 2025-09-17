using LanguageExt;
using LanguageExt.Common;
using Darklands.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Domain.Combat.Services
{
    /// <summary>
    /// Domain service for applying damage to actors with validation and error handling.
    /// Eliminates MediatR anti-pattern by providing shared damage logic for command handlers.
    ///
    /// This service encapsulates the core damage business logic that was previously
    /// duplicated between ExecuteAttackCommandHandler and DamageActorCommandHandler,
    /// removing the need for nested MediatR calls.
    /// </summary>
    public interface IDamageService
    {
        /// <summary>
        /// Applies damage to an actor with comprehensive validation and state management.
        /// </summary>
        /// <param name="actorId">The actor to damage</param>
        /// <param name="damage">Amount of damage to apply (must be >= 0)</param>
        /// <param name="source">Source of the damage for logging and auditing purposes</param>
        /// <returns>
        /// Success: The updated actor with damage applied
        /// Failure: Error describing validation failure or actor not found
        /// </returns>
        Fin<Darklands.Domain.Actor.Actor> ApplyDamage(ActorId actorId, int damage, string source);
    }
}
