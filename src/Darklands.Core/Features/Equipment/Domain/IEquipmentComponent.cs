using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Components;

namespace Darklands.Core.Features.Equipment.Domain;

/// <summary>
/// Component that manages an actor's equipped items across multiple equipment slots.
/// Provides equipment operations (equip, unequip) with validation for two-handed weapons.
/// </summary>
/// <remarks>
/// <para><b>Component Responsibilities</b>:</para>
/// <list type="bullet">
/// <item><description>Store equipped items per slot (MainHand, OffHand, Head, Torso, Legs)</description></item>
/// <item><description>Validate equipment rules (two-handed weapons, slot occupation)</description></item>
/// <item><description>Handle atomic operations (two-handed equip/unequip affects multiple slots)</description></item>
/// </list>
///
/// <para><b>Two-Handed Weapon Pattern</b>:</para>
/// <para>
/// Two-handed weapons occupy BOTH MainHand and OffHand simultaneously.
/// The same ItemId is stored in both slots for atomic operations.
/// Unequipping from either hand removes the weapon from both slots.
/// </para>
///
/// <para><b>Usage Example</b>:</para>
/// <code>
/// var equipment = actor.GetComponent&lt;IEquipmentComponent&gt;().Value;
///
/// // Equip sword to main hand
/// var result = equipment.EquipItem(EquipmentSlot.MainHand, swordId, isTwoHanded: false);
///
/// // Equip two-handed greatsword (occupies both hands)
/// var result = equipment.EquipItem(EquipmentSlot.MainHand, greatswordId, isTwoHanded: true);
///
/// // Unequip (automatically clears both slots if two-handed)
/// var unequipped = equipment.UnequipItem(EquipmentSlot.MainHand);
/// </code>
/// </remarks>
public interface IEquipmentComponent : IComponent
{
    /// <summary>
    /// The actor that owns this equipment component.
    /// </summary>
    ActorId OwnerId { get; }

    /// <summary>
    /// Equips an item to the specified equipment slot.
    /// For two-handed weapons, automatically occupies both MainHand and OffHand.
    /// </summary>
    /// <param name="slot">The equipment slot to equip to (MainHand required for two-handed)</param>
    /// <param name="itemId">The item to equip</param>
    /// <param name="isTwoHanded">True if item is two-handed (occupies MainHand + OffHand), false otherwise</param>
    /// <returns>
    /// Success if equipped, Failure with translation key if:
    /// - Slot is already occupied
    /// - Two-handed weapon to non-MainHand slot
    /// - Two-handed weapon when either hand occupied
    /// </returns>
    Result EquipItem(EquipmentSlot slot, ItemId itemId, bool isTwoHanded = false);

    /// <summary>
    /// Unequips an item from the specified equipment slot.
    /// For two-handed weapons, automatically clears both MainHand and OffHand.
    /// </summary>
    /// <param name="slot">The equipment slot to unequip from</param>
    /// <returns>
    /// Success with unequipped ItemId, or Failure with translation key if slot is empty
    /// </returns>
    Result<ItemId> UnequipItem(EquipmentSlot slot);

    /// <summary>
    /// Gets the item currently equipped in the specified slot.
    /// </summary>
    /// <param name="slot">The equipment slot to query</param>
    /// <returns>
    /// Success with equipped ItemId, or Failure with translation key if slot is empty
    /// </returns>
    Result<ItemId> GetEquippedItem(EquipmentSlot slot);

    /// <summary>
    /// Checks if the specified equipment slot is currently occupied.
    /// </summary>
    /// <param name="slot">The equipment slot to check</param>
    /// <returns>True if slot has an item equipped, false if empty</returns>
    bool IsSlotOccupied(EquipmentSlot slot);
}
