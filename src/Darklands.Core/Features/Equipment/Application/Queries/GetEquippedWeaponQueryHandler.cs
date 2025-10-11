using CSharpFunctionalExtensions;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Equipment.Application.Queries;

/// <summary>
/// Handler for GetEquippedWeaponQuery.
/// Returns ItemId of weapon equipped in MainHand slot.
/// </summary>
public sealed class GetEquippedWeaponQueryHandler
    : IRequestHandler<GetEquippedWeaponQuery, Result<ItemId>>
{
    private readonly IActorRepository _actors;
    private readonly ILogger<GetEquippedWeaponQueryHandler> _logger;

    public GetEquippedWeaponQueryHandler(
        IActorRepository actors,
        ILogger<GetEquippedWeaponQueryHandler> logger)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ItemId>> Handle(
        GetEquippedWeaponQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Retrieving equipped weapon for actor {ActorId}",
            query.ActorId);

        // Get actor
        var actorResult = await _actors.GetByIdAsync(query.ActorId);
        if (actorResult.IsFailure)
        {
            return Result.Failure<ItemId>($"Actor {query.ActorId} not found");
        }

        var actor = actorResult.Value;

        // Check if actor has equipment component
        if (!actor.HasComponent<IEquipmentComponent>())
        {
            return Result.Failure<ItemId>("ERROR_EQUIPMENT_NO_WEAPON_EQUIPPED");
        }

        var equipmentComp = actor.GetComponent<IEquipmentComponent>().Value;

        // Get MainHand item (weapon slot)
        var weaponResult = equipmentComp.GetEquippedItem(EquipmentSlot.MainHand);
        if (weaponResult.IsFailure)
        {
            return Result.Failure<ItemId>("ERROR_EQUIPMENT_NO_WEAPON_EQUIPPED");
        }

        _logger.LogDebug(
            "Actor {ActorId} has weapon {WeaponId} equipped",
            query.ActorId,
            weaponResult.Value);

        return Result.Success(weaponResult.Value);
    }
}
