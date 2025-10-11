using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Handler for RemoveItemCommand.
/// Orchestrates removing an item from an actor's inventory.
/// No events in MVP - UI queries on-demand.
/// </summary>
public sealed class RemoveItemCommandHandler : IRequestHandler<RemoveItemCommand, Result>
{
    private readonly IInventoryRepository _inventories;
    private readonly ILogger<RemoveItemCommandHandler> _logger;

    public RemoveItemCommandHandler(
        IInventoryRepository inventories,
        ILogger<RemoveItemCommandHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        RemoveItemCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Removing item {ItemId} from inventory {InventoryId}",
            command.ItemId,
            command.InventoryId);

        // Railway-oriented programming: Get inventory, remove item, save
        // TD_019: Use GetByIdAsync (not GetByActorIdAsync)
        return await _inventories
            .GetByIdAsync(command.InventoryId, cancellationToken)
            .Bind(inventory => inventory
                .RemoveItem(command.ItemId)
                .Tap(async () =>
                {
                    _logger.LogInformation(
                        "Item {ItemId} removed from inventory {InventoryId} (count: {Count}/{Capacity})",
                        command.ItemId,
                        command.InventoryId,
                        inventory.Count,
                        inventory.Capacity);

                    await _inventories.SaveAsync(inventory, cancellationToken);
                }));
    }
}
