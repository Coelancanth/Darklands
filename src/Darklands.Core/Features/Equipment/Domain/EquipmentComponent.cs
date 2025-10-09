using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Equipment.Domain;

/// <summary>
/// Standard implementation of IEquipmentComponent.
/// Manages equipped items across multiple slots with two-handed weapon support.
/// </summary>
/// <remarks>
/// <para><b>Storage Design</b>:</para>
/// <para>
/// Uses Dictionary&lt;EquipmentSlot, ItemId&gt; for O(1) lookups and mutations.
/// Two-handed weapons store the SAME ItemId in both MainHand and OffHand slots.
/// </para>
///
/// <para><b>Two-Handed Weapon Handling</b>:</para>
/// <para>
/// When equipping a two-handed weapon:
/// - Validates both MainHand and OffHand are empty
/// - Stores same ItemId in both slots atomically
/// - Unequipping from either slot clears both
/// </para>
///
/// <para><b>Immutable ItemId, Mutable Component</b>:</para>
/// <para>
/// ItemIds are immutable value types, but the component mutates its internal dictionary.
/// This follows the pattern from HealthComponent (mutable wrapper around immutable values).
/// </para>
/// </remarks>
public sealed class EquipmentComponent : IEquipmentComponent
{
    private readonly Dictionary<EquipmentSlot, ItemId> _equippedItems = new();

    /// <inheritdoc />
    public ActorId OwnerId { get; }

    /// <summary>
    /// Creates a new EquipmentComponent for an actor.
    /// All equipment slots start empty.
    /// </summary>
    /// <param name="ownerId">The actor that owns this component</param>
    /// <exception cref="ArgumentException">If ownerId is default/empty</exception>
    public EquipmentComponent(ActorId ownerId)
    {
        if (ownerId.Value == Guid.Empty)
            throw new ArgumentException("OwnerId cannot be empty", nameof(ownerId));

        OwnerId = ownerId;
    }

    /// <inheritdoc />
    public Result EquipItem(EquipmentSlot slot, ItemId itemId, bool isTwoHanded = false)
    {
        // Validation: two-handed weapons must be equipped to MainHand
        if (isTwoHanded && slot != EquipmentSlot.MainHand)
        {
            return Result.Failure("ERROR_EQUIPMENT_TWO_HANDED_MUST_USE_MAIN_HAND");
        }

        // Two-handed weapon equip logic
        if (isTwoHanded)
        {
            // Both hands must be empty
            if (_equippedItems.ContainsKey(EquipmentSlot.MainHand))
            {
                return Result.Failure("ERROR_EQUIPMENT_MAIN_HAND_OCCUPIED");
            }

            if (_equippedItems.ContainsKey(EquipmentSlot.OffHand))
            {
                return Result.Failure("ERROR_EQUIPMENT_OFF_HAND_OCCUPIED");
            }

            // Equip to both hands atomically
            _equippedItems[EquipmentSlot.MainHand] = itemId;
            _equippedItems[EquipmentSlot.OffHand] = itemId;
            return Result.Success();
        }

        // Single-handed equip - check slot not occupied
        if (_equippedItems.ContainsKey(slot))
        {
            return Result.Failure("ERROR_EQUIPMENT_SLOT_OCCUPIED");
        }

        _equippedItems[slot] = itemId;
        return Result.Success();
    }

    /// <inheritdoc />
    public Result<ItemId> UnequipItem(EquipmentSlot slot)
    {
        if (!_equippedItems.TryGetValue(slot, out var itemId))
        {
            return Result.Failure<ItemId>("ERROR_EQUIPMENT_SLOT_EMPTY");
        }

        // Check if this is part of a two-handed weapon pair
        if (IsTwoHandedWeaponEquipped(itemId))
        {
            // Remove from both hands atomically
            _equippedItems.Remove(EquipmentSlot.MainHand);
            _equippedItems.Remove(EquipmentSlot.OffHand);
        }
        else
        {
            // Remove from single slot
            _equippedItems.Remove(slot);
        }

        return Result.Success(itemId);
    }

    /// <inheritdoc />
    public Result<ItemId> GetEquippedItem(EquipmentSlot slot)
    {
        if (!_equippedItems.TryGetValue(slot, out var itemId))
        {
            return Result.Failure<ItemId>("ERROR_EQUIPMENT_SLOT_EMPTY");
        }

        return Result.Success(itemId);
    }

    /// <inheritdoc />
    public bool IsSlotOccupied(EquipmentSlot slot)
    {
        return _equippedItems.ContainsKey(slot);
    }

    /// <summary>
    /// Checks if the specified item is equipped as a two-handed weapon.
    /// A two-handed weapon has the same ItemId in both MainHand and OffHand slots.
    /// </summary>
    /// <param name="itemId">The item to check</param>
    /// <returns>True if item is equipped in both hands, false otherwise</returns>
    private bool IsTwoHandedWeaponEquipped(ItemId itemId)
    {
        return _equippedItems.TryGetValue(EquipmentSlot.MainHand, out var mainHand) &&
               _equippedItems.TryGetValue(EquipmentSlot.OffHand, out var offHand) &&
               mainHand == offHand &&
               mainHand == itemId;
    }
}
