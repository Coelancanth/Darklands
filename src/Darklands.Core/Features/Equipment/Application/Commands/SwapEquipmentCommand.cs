using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Domain;
using MediatR;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Equipment.Application.Commands;

/// <summary>
/// Command to swap an equipped item with a new item from inventory.
/// Performs atomic three-way transaction: Unequip old → Add old to inventory → Remove new from inventory → Equip new.
/// </summary>
/// <remarks>
/// <para><b>Atomic Operation</b>:</para>
/// <para>
/// 1. Validate actor exists and has EquipmentComponent
/// 2. Validate slot is occupied (has old item to swap)
/// 3. Validate new item exists in inventory
/// 4. Unequip old item from slot
/// 5. Add old item to inventory
/// 6. Remove new item from inventory
/// 7. Equip new item to slot
/// 8. Rollback on ANY failure (complex multi-step rollback)
/// 9. Save both equipment (actor) and inventory
/// </para>
///
/// <para><b>Two-Handed Weapon Handling</b>:</para>
/// <para>
/// - If old item is two-handed: Clears both MainHand + OffHand
/// - If new item is two-handed: Occupies both MainHand + OffHand
/// - Validation ensures both hands available when swapping TO two-handed weapon
/// </para>
///
/// <para><b>Use Case</b>:</para>
/// <para>
/// Primarily for UI drag-and-drop: Drag new sword from inventory onto occupied MainHand slot.
/// Alternative to manual unequip → equip sequence (more user-friendly, atomic).
/// </para>
///
/// <para><b>Usage Example</b>:</para>
/// <code>
/// // Swap iron sword for steel sword in main hand
/// var cmd = new SwapEquipmentCommand(playerId, steelSwordId, EquipmentSlot.MainHand, isTwoHanded: false);
/// var result = await _mediator.Send(cmd);
///
/// // Result.Value contains the ItemId of the old (unequipped) item
/// if (result.IsSuccess)
/// {
///     GD.Print($"Swapped out old item: {result.Value}");
/// }
/// </code>
/// </remarks>
/// <param name="ActorId">Actor performing the swap</param>
/// <param name="InventoryId">Inventory containing the new item</param>
/// <param name="NewItemId">New item from inventory to equip</param>
/// <param name="Slot">Equipment slot to swap (must be occupied)</param>
/// <param name="IsTwoHanded">True if new item is two-handed, false otherwise</param>
/// <remarks>
/// TD_019: Added InventoryId parameter (breaking change).
/// Enables swapping items from any inventory (e.g., squad member's inventory).
/// </remarks>
public sealed record SwapEquipmentCommand(
    ActorId ActorId,
    InventoryId InventoryId,
    ItemId NewItemId,
    EquipmentSlot Slot,
    bool IsTwoHanded = false
) : IRequest<Result<ItemId>>; // Returns old item ID
