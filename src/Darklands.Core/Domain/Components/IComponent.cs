namespace Darklands.Core.Domain.Components;

/// <summary>
/// Base interface for all actor components.
/// Components encapsulate specific behaviors (health, weapons, equipment, etc.).
/// </summary>
/// <remarks>
/// <para><b>Component Pattern</b>:</para>
/// <para>
/// Actor is a "component container" that composes behaviors via components.
/// This allows flexible entity design - Player has EquipmentComponent, enemies don't.
/// Boss has PhaseComponent, normal enemies don't. Mix and match as needed.
/// </para>
///
/// <para><b>Why Marker Interface</b>:</para>
/// <para>
/// IComponent has no methods - it's a type marker for Actor's component dictionary.
/// Specific components define their own contracts (IHealthComponent.TakeDamage).
/// </para>
///
/// <para><b>Example Components</b>:</para>
/// <list type="bullet">
/// <item><description>IHealthComponent - HP, damage, healing, death detection</description></item>
/// <item><description>IWeaponComponent - Equipped weapon, attack capabilities</description></item>
/// <item><description>IEquipmentComponent - Armor, accessories, stat bonuses (future)</description></item>
/// <item><description>IStatusEffectComponent - Buffs/debuffs (future)</description></item>
/// </list>
///
/// <para><b>Usage</b>:</para>
/// <code>
/// // Add components to actor
/// actor.AddComponent&lt;IHealthComponent&gt;(new HealthComponent(maxHealth: 100));
/// actor.AddComponent&lt;IWeaponComponent&gt;(new WeaponComponent(sword));
///
/// // Retrieve and use components
/// var health = actor.GetComponent&lt;IHealthComponent&gt;();
/// health.TakeDamage(10);
///
/// // Check if actor has component
/// if (actor.HasComponent&lt;IWeaponComponent&gt;())
/// {
///     var weapon = actor.GetComponent&lt;IWeaponComponent&gt;();
///     // Execute attack...
/// }
/// </code>
/// </remarks>
public interface IComponent
{
    // Marker interface - no methods needed
    // Components define behavior via specific interfaces (IHealthComponent, IWeaponComponent, etc.)
}
