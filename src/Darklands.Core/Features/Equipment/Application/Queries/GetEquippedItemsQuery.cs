using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Domain;
using MediatR;

namespace Darklands.Core.Features.Equipment.Application.Queries;

/// <summary>
/// Query to retrieve all equipped items for an actor.
/// Returns dictionary mapping equipment slots to item IDs.
/// </summary>
/// <remarks>
/// <para><b>Usage in Presentation</b>:</para>
/// <para>
/// EquipmentPanelNode queries this to display all 5 equipment slots:
/// - MainHand: Sword sprite
/// - OffHand: Shield sprite
/// - Head: Helmet sprite
/// - Torso: Armor sprite
/// - Legs: Boots sprite
/// </para>
///
/// <para><b>Empty Slots</b>:</para>
/// <para>
/// Dictionary only contains occupied slots. Check with `result.Value.TryGetValue(slot, out itemId)`.
/// Empty slots are not in dictionary (show placeholder sprite in UI).
/// </para>
///
/// <para><b>Two-Handed Weapons</b>:</para>
/// <para>
/// Two-handed weapons appear in BOTH MainHand and OffHand with same ItemId:
/// <code>
/// {
///   MainHand: greatsword_id,
///   OffHand: greatsword_id,  // Same ID
///   Head: helmet_id
/// }
/// </code>
/// UI can detect two-handed by checking `dict[MainHand] == dict[OffHand]`.
/// </para>
///
/// <para><b>Example Usage</b>:</para>
/// <code>
/// var query = new GetEquippedItemsQuery(playerId);
/// var result = await _mediator.Send(query);
///
/// if (result.IsSuccess)
/// {
///     foreach (var (slot, itemId) in result.Value)
///     {
///         UpdateEquipmentSlotUI(slot, itemId); // Show item sprite
///     }
/// }
/// </code>
/// </remarks>
/// <param name="ActorId">Actor whose equipped items to retrieve</param>
public sealed record GetEquippedItemsQuery(ActorId ActorId)
    : IRequest<Result<IReadOnlyDictionary<EquipmentSlot, ItemId>>>;
