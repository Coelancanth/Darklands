using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Equipment.Application.Queries;

/// <summary>
/// Query to retrieve the equipped weapon from MainHand slot.
/// Convenience query for combat system - avoids querying all equipment slots.
/// </summary>
/// <remarks>
/// <para><b>Why This Query Exists</b>:</para>
/// <para>
/// Combat system only cares about MainHand weapon. This query:
/// - Avoids overhead of GetEquippedItemsQuery (queries all 5 slots)
/// - Provides clear intent: "Get weapon for attack"
/// - Returns ItemId which combat can use to query weapon stats
/// </para>
///
/// <para><b>Two-Handed Weapons</b>:</para>
/// <para>
/// Two-handed weapons are stored in MainHand slot (same as one-handed).
/// Combat doesn't need to know if weapon is two-handed - that's presentation concern.
/// </para>
///
/// <para><b>Empty MainHand</b>:</para>
/// <para>
/// Returns Failure with "ERROR_EQUIPMENT_NO_WEAPON_EQUIPPED" if MainHand is empty.
/// Combat interprets this as "actor cannot attack" or "unarmed attack".
/// </para>
///
/// <para><b>Example Usage (Phase 6 - Combat Integration)</b>:</para>
/// <code>
/// // In ExecuteAttackCommandHandler
/// var weaponQuery = new GetEquippedWeaponQuery(attackerId);
/// var weaponResult = await _mediator.Send(weaponQuery);
///
/// if (weaponResult.IsFailure)
/// {
///     return Result.Failure("Attacker has no weapon equipped");
/// }
///
/// var weaponId = weaponResult.Value;
/// // Query weapon stats from item repository...
/// </code>
/// </remarks>
/// <param name="ActorId">Actor whose equipped weapon to retrieve</param>
public sealed record GetEquippedWeaponQuery(ActorId ActorId)
    : IRequest<Result<ItemId>>;
