using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Domain.Components;

/// <summary>
/// Standard implementation of IHealthComponent.
/// Wraps Health value object and provides mutable component interface.
/// </summary>
/// <remarks>
/// <para><b>Mutable Wrapper Pattern</b>:</para>
/// <para>
/// Health value object is immutable, but HealthComponent is mutable.
/// TakeDamage/Heal update the internal Health reference.
/// This allows component to be modified in-place on Actor.
/// </para>
///
/// <para><b>Why Mutable Component</b>:</para>
/// <para>
/// Components live inside Actor's dictionary. Making them mutable simplifies usage:
/// <code>
/// // With mutable component (simple)
/// var health = actor.GetComponent&lt;IHealthComponent&gt;().Value;
/// health.TakeDamage(10); // Component updates itself
///
/// // With immutable component (complex - must replace component)
/// var oldHealth = actor.GetComponent&lt;IHealthComponent&gt;().Value;
/// var newHealth = oldHealth.TakeDamage(10).Value;
/// actor.RemoveComponent&lt;IHealthComponent&gt;();
/// actor.AddComponent(newHealth); // Lots of boilerplate!
/// </code>
/// </para>
///
/// <para><b>Testability</b>:</para>
/// <para>
/// Unit tests can create HealthComponent directly without Actor:
/// <code>
/// var health = new HealthComponent(Health.Create(100, 100).Value);
/// health.TakeDamage(30);
/// Assert.Equal(70, health.CurrentHealth.Current);
/// </code>
/// </para>
/// </remarks>
public class HealthComponent : IHealthComponent
{
    private Health _health;

    /// <inheritdoc />
    public Health CurrentHealth => _health;

    /// <inheritdoc />
    public bool IsAlive => !_health.IsDepleted;

    /// <summary>
    /// Creates a new HealthComponent with the specified health state.
    /// </summary>
    /// <param name="health">Initial health (current and maximum)</param>
    public HealthComponent(Health health)
    {
        _health = health;
    }

    /// <inheritdoc />
    public Result<Health> TakeDamage(float amount)
    {
        var result = _health.Reduce(amount);

        if (result.IsFailure)
        {
            return result;
        }

        // Update internal state with new health
        _health = result.Value;
        return result;
    }

    /// <inheritdoc />
    public Result<Health> Heal(float amount)
    {
        var result = _health.Increase(amount);

        if (result.IsFailure)
        {
            return result;
        }

        // Update internal state with new health
        _health = result.Value;
        return result;
    }
}
