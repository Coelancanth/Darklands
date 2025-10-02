using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Handler for GetInventoryQuery.
/// Returns inventory state as DTO (prevents presentation from accessing domain entities).
/// </summary>
public sealed class GetInventoryQueryHandler
    : IRequestHandler<GetInventoryQuery, Result<InventoryDto>>
{
    private readonly IInventoryRepository _inventories;
    private readonly ILogger<GetInventoryQueryHandler> _logger;

    public GetInventoryQueryHandler(
        IInventoryRepository inventories,
        ILogger<GetInventoryQueryHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<InventoryDto>> Handle(
        GetInventoryQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Retrieving inventory for actor {ActorId}",
            query.ActorId);

        return await _inventories
            .GetByActorIdAsync(query.ActorId, cancellationToken)
            .Map(inventory => new InventoryDto(
                inventory.Id,
                query.ActorId,
                inventory.Capacity,
                inventory.Count,
                inventory.IsFull,
                inventory.Items,
                inventory.GridWidth,
                inventory.GridHeight,
                inventory.ContainerType,
                inventory.ItemPlacements));
    }
}
