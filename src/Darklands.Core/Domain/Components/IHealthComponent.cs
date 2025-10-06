using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Domain.Components;

/// <summary>
/// Component that provides health management for actors.
/// Handles damage, healing, and death detection.
/// </summary>
/// <remarks>
/// <para><b>Component Responsibilities</b>:</para>
/// <list type="bullet">
/// <item><description>Track current and maximum health</description></item>
/// <item><description>Apply damage (reduce health)</description></item>
/// <item><description>Apply healing (increase health)</description></item>
/// <item><description>Detect death (health depleted)</description></item>
/// </list>
///
/// <para><b>Immutability Pattern</b>:</para>
/// <para>
/// TakeDamage/Heal return NEW Health value - caller must update component state.
/// This follows functional programming principles from ADR-003.
/// </para>
///
/// <para><b>Usage</b>:</para>
/// <code>
/// var healthComp = actor.GetComponent&lt;IHealthComponent&gt;().Value;
///
/// // Apply damage
/// var result = healthComp.TakeDamage(15);
/// if (result.IsSuccess)
/// {
///     if (healthComp.IsAlive)
///         GD.Print("Actor survived!");
///     else
///         GD.Print("Actor died!");
/// }
/// </code>
/// </remarks>
public interface IHealthComponent : IComponent
{
    /// <summary>
    /// Current health state (current and maximum HP).
    /// </summary>
    Health CurrentHealth { get; }

    /// <summary>
    /// True if actor is alive (health > 0), false if dead.
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Applies damage to the actor, reducing health.
    /// </summary>
    /// <param name="amount">Damage amount (must be >= 0)</param>
    /// <returns>Success with new health state, or Failure if amount invalid</returns>
    Result<Health> TakeDamage(float amount);

    /// <summary>
    /// Applies healing to the actor, increasing health up to maximum.
    /// </summary>
    /// <param name="amount">Heal amount (must be >= 0)</param>
    /// <returns>Success with new health state, or Failure if amount invalid</returns>
    Result<Health> Heal(float amount);
}
