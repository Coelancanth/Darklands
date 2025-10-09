using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Domain;
using MediatR;

namespace Darklands.Core.Features.Equipment.Application.Commands;

/// <summary>
/// Command to equip an item from inventory to an equipment slot.
/// Performs atomic transaction: Remove from inventory → Add to equipment slot.
/// </summary>
/// <remarks>
/// <para><b>Atomic Operation</b>:</para>
/// <para>
/// 1. Validate actor exists and has EquipmentComponent
/// 2. Validate slot is empty (or two-handed weapons validate both hands)
/// 3. Remove item from inventory
/// 4. Equip item to slot (rolls back inventory on failure)
/// 5. Save both inventory and actor state
/// </para>
///
/// <para><b>Two-Handed Weapon Handling</b>:</para>
/// <para>
/// When IsTwoHanded = true:
/// - Validates BOTH MainHand and OffHand are empty
/// - Stores same ItemId in both slots
/// - If equip fails after inventory removal, item is restored to inventory (rollback)
/// </para>
///
/// <para><b>Usage Example</b>:</para>
/// <code>
/// // Equip sword from inventory to main hand
/// var cmd = new EquipItemCommand(playerId, inventoryId, swordId, EquipmentSlot.MainHand, isTwoHanded: false);
/// var result = await _mediator.Send(cmd);
///
/// // Equip two-handed greatsword
/// var cmd = new EquipItemCommand(playerId, inventoryId, greatswordId, EquipmentSlot.MainHand, isTwoHanded: true);
/// var result = await _mediator.Send(cmd);
/// </code>
/// </remarks>
/// <param name="ActorId">Actor equipping the item</param>
/// <param name="ItemId">Item to equip</param>
/// <param name="Slot">Equipment slot to equip to (MainHand required for two-handed)</param>
/// <param name="IsTwoHanded">True if item occupies both MainHand + OffHand, false for single slot</param>
public sealed record EquipItemCommand(
    ActorId ActorId,
    ItemId ItemId,
    EquipmentSlot Slot,
    bool IsTwoHanded = false
) : IRequest<Result>;
