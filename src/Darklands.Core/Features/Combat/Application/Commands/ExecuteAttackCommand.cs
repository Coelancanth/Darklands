using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Combat.Application.Commands;

/// <summary>
/// Command to execute an attack from one actor to another.
/// Validates range, applies damage, consumes time units, handles death.
/// </summary>
/// <remarks>
/// <para><b>Attack Flow</b>:</para>
/// <list type="number">
/// <item><description>Validate attacker has weapon (IWeaponComponent)</description></item>
/// <item><description>Validate target is alive (IHealthComponent.IsAlive)</description></item>
/// <item><description>Validate range (melee = adjacent, ranged = FOV visibility)</description></item>
/// <item><description>Apply damage to target's HealthComponent</description></item>
/// <item><description>Consume time units from attacker's turn queue slot</description></item>
/// <item><description>Handle death: Remove target from queue if HP reaches 0</description></item>
/// </list>
///
/// <para><b>Range Validation</b>:</para>
/// <list type="bullet">
/// <item><description>Melee: Attacker and target must be adjacent (8-directional, distance = 1 tile)</description></item>
/// <item><description>Ranged: Target must be visible in attacker's FOV + within weapon range</description></item>
/// </list>
///
/// <para><b>Time Cost Integration</b>:</para>
/// <para>
/// Attack consumes weapon's time cost (e.g., 100 time units for sword).
/// TurnQueue advances attacker's time by weapon.TimeCost (uses AdvanceActorTimeCommand from VS_007).
/// </para>
///
/// <para><b>Example Usage</b>:</para>
/// <code>
/// // Player attacks goblin with equipped weapon
/// var cmd = new ExecuteAttackCommand(playerId, goblinId);
/// var result = await _mediator.Send(cmd);
///
/// if (result.IsSuccess)
/// {
///     GD.Print("Attack hit! Target took damage.");
///     if (result.Value.TargetDied)
///         GD.Print("Target defeated!");
/// }
/// else
/// {
///     GD.Print($"Attack failed: {result.Error}");
/// }
/// </code>
/// </remarks>
/// <param name="AttackerId">ActorId of the attacking actor</param>
/// <param name="TargetId">ActorId of the target actor</param>
public record ExecuteAttackCommand(
    ActorId AttackerId,
    ActorId TargetId
) : IRequest<CSharpFunctionalExtensions.Result<AttackResult>>;

/// <summary>
/// Result of an attack execution.
/// </summary>
/// <param name="DamageDealt">Amount of damage dealt to target</param>
/// <param name="TargetDied">True if target's health reached 0 and was removed from combat</param>
/// <param name="TargetRemainingHealth">Target's health after damage (0 if dead)</param>
public record AttackResult(
    float DamageDealt,
    bool TargetDied,
    float TargetRemainingHealth
);
