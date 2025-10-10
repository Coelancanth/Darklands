using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Handler for AddItemCommand.
/// Orchestrates adding an item to an actor's inventory.
/// No events in MVP - UI queries on-demand.
/// </summary>
public sealed class AddItemCommandHandler : IRequestHandler<AddItemCommand, Result>
{
    private readonly IInventoryRepository _inventories;
    private readonly ILogger<AddItemCommandHandler> _logger;

    public AddItemCommandHandler(
        IInventoryRepository inventories,
        ILogger<AddItemCommandHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        AddItemCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Adding item {ItemId} to inventory {InventoryId}",
            command.ItemId,
            command.InventoryId);

        // Railway-oriented programming: Get inventory, add item, save
        // TD_019: Use GetByIdAsync (not GetByActorIdAsync)
        return await _inventories
            .GetByIdAsync(command.InventoryId, cancellationToken)
            .Bind(inventory => inventory
                .AddItem(command.ItemId)
                .Tap(async () =>
                {
                    _logger.LogInformation(
                        "Item {ItemId} added to inventory {InventoryId} (count: {Count}/{Capacity})",
                        command.ItemId,
                        command.InventoryId,
                        inventory.Count,
                        inventory.Capacity);

                    await _inventories.SaveAsync(inventory, cancellationToken);
                }));
    }
}
