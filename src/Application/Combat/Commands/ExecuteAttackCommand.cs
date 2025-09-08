using Darklands.Core.Application.Common;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using LanguageExt;
using MediatR;

namespace Darklands.Core.Application.Combat.Commands;

/// <summary>
/// Command to execute a melee attack between two actors in combat.
/// Orchestrates attack validation, damage application, and scheduler updates.
/// Following TDD+VSA Comprehensive Development Workflow.
/// </summary>
public sealed record ExecuteAttackCommand : ICommand, IRequest<Fin<LanguageExt.Unit>>
{
    /// <summary>
    /// The actor performing the attack.
    /// </summary>
    public required ActorId AttackerId { get; init; }

    /// <summary>
    /// The target actor being attacked.
    /// </summary>
    public required ActorId TargetId { get; init; }

    /// <summary>
    /// The combat action being performed (determines damage, time cost, etc.).
    /// </summary>
    public required CombatAction CombatAction { get; init; }

    /// <summary>
    /// Creates a new ExecuteAttackCommand with the specified parameters.
    /// </summary>
    /// <param name="attackerId">The actor performing the attack</param>
    /// <param name="targetId">The target of the attack</param>
    /// <param name="combatAction">The combat action to perform (defaults to SwordSlash)</param>
    /// <returns>A new ExecuteAttackCommand instance</returns>
    public static ExecuteAttackCommand Create(ActorId attackerId, ActorId targetId, CombatAction? combatAction = null) =>
        new()
        {
            AttackerId = attackerId,
            TargetId = targetId,
            CombatAction = combatAction ?? CombatAction.Common.SwordSlash
        };
}
