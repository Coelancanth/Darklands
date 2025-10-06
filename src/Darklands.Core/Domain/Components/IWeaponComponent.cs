using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Domain.Components;

/// <summary>
/// Component that manages an actor's equipped weapon.
/// Provides weapon data and attack validation.
/// </summary>
/// <remarks>
/// <para><b>Component Responsibilities</b>:</para>
/// <list type="bullet">
/// <item><description>Store currently equipped weapon</description></item>
/// <item><description>Validate if actor can attack (has weapon)</description></item>
/// <item><description>Provide weapon stats for attack calculations</description></item>
/// </list>
///
/// <para><b>Future Extensions</b>:</para>
/// <list type="bullet">
/// <item><description>Multiple weapons (primary/secondary)</description></item>
/// <item><description>Weapon durability</description></item>
/// <item><description>Weapon switching (unequip/equip)</description></item>
/// </list>
///
/// <para><b>Usage</b>:</para>
/// <code>
/// var weaponComp = actor.GetComponent&lt;IWeaponComponent&gt;().Value;
///
/// if (weaponComp.CanAttack())
/// {
///     var weapon = weaponComp.EquippedWeapon;
///     GD.Print($"Attacking with {weapon.NameKey} for {weapon.Damage} damage");
/// }
/// </code>
/// </remarks>
public interface IWeaponComponent : IComponent
{
    /// <summary>
    /// Currently equipped weapon (if any).
    /// None if actor has no weapon equipped.
    /// </summary>
    Weapon? EquippedWeapon { get; }

    /// <summary>
    /// Checks if actor can perform attacks.
    /// </summary>
    /// <returns>True if weapon equipped, false otherwise</returns>
    bool CanAttack();

    /// <summary>
    /// Equips a weapon for this actor.
    /// </summary>
    /// <param name="weapon">Weapon to equip</param>
    /// <returns>Success if equipped, Failure if weapon is null</returns>
    Result EquipWeapon(Weapon weapon);

    /// <summary>
    /// Unequips the current weapon.
    /// </summary>
    /// <returns>Success with unequipped weapon, or Failure if no weapon equipped</returns>
    Result<Weapon> UnequipWeapon();
}
