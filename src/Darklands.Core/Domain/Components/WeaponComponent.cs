using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Domain.Components;

/// <summary>
/// Standard implementation of IWeaponComponent.
/// Manages a single equipped weapon slot.
/// </summary>
/// <remarks>
/// <para><b>Single Weapon Design</b>:</para>
/// <para>
/// For VS_020 (Basic Combat), actors have ONE weapon slot.
/// Future work (inventory system) may add primary/secondary weapon switching.
/// </para>
///
/// <para><b>Nullable Weapon</b>:</para>
/// <para>
/// EquippedWeapon is nullable - actors can exist without weapons (NPCs, unarmed enemies).
/// CanAttack() checks if weapon is equipped before allowing attacks.
/// </para>
/// </remarks>
public class WeaponComponent : IWeaponComponent
{
    /// <inheritdoc />
    public Weapon? EquippedWeapon { get; private set; }

    /// <summary>
    /// Creates a new WeaponComponent with optional initial weapon.
    /// </summary>
    /// <param name="weapon">Initial weapon (null if unarmed)</param>
    public WeaponComponent(Weapon? weapon = null)
    {
        EquippedWeapon = weapon;
    }

    /// <inheritdoc />
    public bool CanAttack()
    {
        return EquippedWeapon != null;
    }

    /// <inheritdoc />
    public Result EquipWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            return Result.Failure("Cannot equip null weapon");
        }

        EquippedWeapon = weapon;
        return Result.Success();
    }

    /// <inheritdoc />
    public Result<Weapon> UnequipWeapon()
    {
        if (EquippedWeapon == null)
        {
            return Result.Failure<Weapon>("No weapon equipped to unequip");
        }

        var weapon = EquippedWeapon;
        EquippedWeapon = null;
        return Result.Success(weapon);
    }
}
