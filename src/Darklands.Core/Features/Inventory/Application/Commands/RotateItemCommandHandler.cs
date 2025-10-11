using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Handler for RotateItemCommand (Phase 3).
/// Orchestrates rotating an item in inventory with collision validation.
/// </summary>
public sealed class RotateItemCommandHandler : IRequestHandler<RotateItemCommand, Result>
{
    private readonly IInventoryRepository _inventories;
    private readonly ILogger<RotateItemCommandHandler> _logger;

    public RotateItemCommandHandler(
        IInventoryRepository inventories,
        ILogger<RotateItemCommandHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        RotateItemCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Rotating item {ItemId} to {Rotation} in inventory {InventoryId}",
            command.ItemId,
            command.NewRotation,
            command.InventoryId);

        // Railway-oriented programming: Get inventory, rotate item, save
        // TD_019: Use GetByIdAsync (not GetByActorIdAsync)
        return await _inventories
            .GetByIdAsync(command.InventoryId, cancellationToken)
            .Bind(inventory => inventory
                .RotateItem(command.ItemId, command.NewRotation)
                .Tap(async () =>
                {
                    _logger.LogInformation(
                        "Item {ItemId} rotated to {Rotation} in inventory {InventoryId}",
                        command.ItemId,
                        command.NewRotation,
                        command.InventoryId);

                    await _inventories.SaveAsync(inventory, cancellationToken);
                }));
    }
}
