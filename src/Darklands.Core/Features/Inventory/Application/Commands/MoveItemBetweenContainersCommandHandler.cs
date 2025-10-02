using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Handler for MoveItemBetweenContainersCommand.
/// Orchestrates cross-container item movement (remove from source, add to target).
/// </summary>
/// <remarks>
/// TRANSACTIONAL SEMANTICS:
/// - Removes from source first
/// - If target placement fails, item is lost (acceptable for in-memory MVP)
/// - Future: Wrap in transaction for persistent storage
///
/// SUPPORTS:
/// - Inter-container movement (backpack A → backpack B)
/// - Intra-container repositioning (same inventory, new position)
/// </remarks>
public sealed class MoveItemBetweenContainersCommandHandler
    : IRequestHandler<MoveItemBetweenContainersCommand, Result>
{
    private readonly IInventoryRepository _inventories;
    private readonly IItemRepository _items;
    private readonly ILogger<MoveItemBetweenContainersCommandHandler> _logger;

    public MoveItemBetweenContainersCommandHandler(
        IInventoryRepository inventories,
        IItemRepository items,
        ILogger<MoveItemBetweenContainersCommandHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        MoveItemBetweenContainersCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Moving item {ItemId} from actor {SourceActorId} to actor {TargetActorId} at position {TargetPosition}",
            command.ItemId,
            command.SourceActorId,
            command.TargetActorId,
            command.TargetPosition);

        // Get item for type validation
        var itemResult = await _items.GetByIdAsync(command.ItemId, cancellationToken);
        if (itemResult.IsFailure)
            return itemResult;

        var item = itemResult.Value;

        // Get source inventory and remove item
        var sourceResult = await _inventories.GetByActorIdAsync(command.SourceActorId, cancellationToken);
        if (sourceResult.IsFailure)
            return sourceResult;

        var sourceInventory = sourceResult.Value;
        var removeResult = sourceInventory.RemoveItem(command.ItemId);
        if (removeResult.IsFailure)
            return removeResult;

        // Get target inventory and place item
        var targetResult = await _inventories.GetByActorIdAsync(command.TargetActorId, cancellationToken);
        if (targetResult.IsFailure)
            return targetResult;

        var targetInventory = targetResult.Value;

        // BUSINESS RULE: Type filtering for specialized containers
        if (targetInventory.ContainerType == ContainerType.WeaponOnly &&
            item.Type != "weapon")
        {
            return Result.Failure("Target container only accepts weapons");
        }

        var placeResult = targetInventory.PlaceItemAt(command.ItemId, command.TargetPosition);
        if (placeResult.IsFailure)
            return placeResult;

        // Persist both inventories
        await _inventories.SaveAsync(sourceInventory, cancellationToken);
        await _inventories.SaveAsync(targetInventory, cancellationToken);

        _logger.LogInformation(
            "Item {ItemId} moved from actor {SourceActorId} to actor {TargetActorId} at position {TargetPosition}",
            command.ItemId,
            command.SourceActorId,
            command.TargetActorId,
            command.TargetPosition);

        return Result.Success();
    }
}
