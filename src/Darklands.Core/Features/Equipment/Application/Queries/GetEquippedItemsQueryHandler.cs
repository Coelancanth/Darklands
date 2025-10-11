using CSharpFunctionalExtensions;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Equipment.Application.Queries;

/// <summary>
/// Handler for GetEquippedItemsQuery.
/// Returns dictionary of equipped items (slot → itemId mapping).
/// </summary>
public sealed class GetEquippedItemsQueryHandler
    : IRequestHandler<GetEquippedItemsQuery, Result<IReadOnlyDictionary<EquipmentSlot, ItemId>>>
{
    private readonly IActorRepository _actors;
    private readonly ILogger<GetEquippedItemsQueryHandler> _logger;

    public GetEquippedItemsQueryHandler(
        IActorRepository actors,
        ILogger<GetEquippedItemsQueryHandler> logger)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlyDictionary<EquipmentSlot, ItemId>>> Handle(
        GetEquippedItemsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Retrieving equipped items for actor {ActorId}",
            query.ActorId);

        // Get actor
        var actorResult = await _actors.GetByIdAsync(query.ActorId);
        if (actorResult.IsFailure)
        {
            return Result.Failure<IReadOnlyDictionary<EquipmentSlot, ItemId>>(
                $"Actor {query.ActorId} not found");
        }

        var actor = actorResult.Value;

        // Check if actor has equipment component
        if (!actor.HasComponent<IEquipmentComponent>())
        {
            // Actor has no equipment component - return empty dictionary
            _logger.LogDebug(
                "Actor {ActorId} has no equipment component, returning empty equipment",
                query.ActorId);
            return Result.Success<IReadOnlyDictionary<EquipmentSlot, ItemId>>(
                new Dictionary<EquipmentSlot, ItemId>());
        }

        var equipmentComp = actor.GetComponent<IEquipmentComponent>().Value;

        // Build dictionary of equipped items (only occupied slots)
        var equippedItems = new Dictionary<EquipmentSlot, ItemId>();

        foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
        {
            if (equipmentComp.IsSlotOccupied(slot))
            {
                var itemResult = equipmentComp.GetEquippedItem(slot);
                if (itemResult.IsSuccess)
                {
                    equippedItems[slot] = itemResult.Value;
                }
            }
        }

        _logger.LogDebug(
            "Retrieved {Count} equipped items for actor {ActorId}",
            equippedItems.Count,
            query.ActorId);

        return Result.Success<IReadOnlyDictionary<EquipmentSlot, ItemId>>(equippedItems);
    }
}
