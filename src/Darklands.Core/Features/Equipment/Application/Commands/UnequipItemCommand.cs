using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Domain;
using MediatR;

namespace Darklands.Core.Features.Equipment.Application.Commands;

/// <summary>
/// Command to unequip an item from an equipment slot back to inventory.
/// Performs atomic transaction: Remove from equipment slot → Add to inventory.
/// </summary>
/// <remarks>
/// <para><b>Atomic Operation</b>:</para>
/// <para>
/// 1. Validate actor exists and has EquipmentComponent
/// 2. Validate slot is occupied
/// 3. Remove item from equipment slot (handles two-handed automatically)
/// 4. Add item to inventory (rolls back equipment on failure)
/// 5. Save both equipment (actor) and inventory state
/// </para>
///
/// <para><b>Two-Handed Weapon Handling</b>:</para>
/// <para>
/// Equipment component automatically detects two-handed weapons (same ItemId in both hands).
/// Unequipping from EITHER hand clears BOTH slots atomically.
/// </para>
///
/// <para><b>Usage Example</b>:</para>
/// <code>
/// // Unequip sword from main hand back to inventory
/// var cmd = new UnequipItemCommand(playerId, EquipmentSlot.MainHand);
/// var result = await _mediator.Send(cmd);
///
/// // Result.Value contains the ItemId that was unequipped
/// if (result.IsSuccess)
/// {
///     GD.Print($"Unequipped item: {result.Value}");
/// }
/// </code>
/// </remarks>
/// <param name="ActorId">Actor unequipping the item</param>
/// <param name="Slot">Equipment slot to unequip from</param>
public sealed record UnequipItemCommand(
    ActorId ActorId,
    EquipmentSlot Slot
) : IRequest<Result<ItemId>>;
