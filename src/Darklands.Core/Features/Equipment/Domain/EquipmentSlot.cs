namespace Darklands.Core.Features.Equipment.Domain;

/// <summary>
/// Represents the available equipment slots for an actor.
/// Each slot can hold one item (or in the case of two-handed weapons, MainHand + OffHand).
/// </summary>
/// <remarks>
/// <para><b>Equipment System Design</b>:</para>
/// <list type="bullet">
/// <item><description>MainHand - Primary weapon or tool (required for two-handed weapons)</description></item>
/// <item><description>OffHand - Secondary weapon, shield, or occupied by two-handed weapon</description></item>
/// <item><description>Head - Helmet, hat, circlet</description></item>
/// <item><description>Torso - Armor, robe, clothing</description></item>
/// <item><description>Legs - Greaves, boots, pants</description></item>
/// </list>
///
/// <para><b>Two-Handed Weapons</b>:</para>
/// <para>
/// Two-handed weapons occupy BOTH MainHand and OffHand slots simultaneously.
/// When equipped, the same ItemId is stored in both slots for atomic operations.
/// </para>
///
/// <para><b>Future Extensions</b>:</para>
/// <para>
/// Additional slots (Ring1, Ring2, Amulet, Cloak) can be added without breaking existing code.
/// </para>
/// </remarks>
public enum EquipmentSlot
{
    /// <summary>
    /// Primary hand slot - weapons, tools, or two-handed weapon primary slot.
    /// </summary>
    MainHand,

    /// <summary>
    /// Secondary hand slot - shields, off-hand weapons, or occupied by two-handed weapon.
    /// </summary>
    OffHand,

    /// <summary>
    /// Head armor slot - helmets, hats, circlets.
    /// </summary>
    Head,

    /// <summary>
    /// Torso armor slot - chest armor, robes, clothing.
    /// </summary>
    Torso,

    /// <summary>
    /// Leg armor slot - greaves, boots, leg armor.
    /// </summary>
    Legs
}
