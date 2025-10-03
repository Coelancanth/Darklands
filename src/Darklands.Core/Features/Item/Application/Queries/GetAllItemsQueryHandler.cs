using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Item.Application.Queries;

/// <summary>
/// Handler for GetAllItemsQuery.
/// Delegates to IItemRepository for catalog access.
/// </summary>
public class GetAllItemsQueryHandler : IRequestHandler<GetAllItemsQuery, Result<List<ItemDto>>>
{
    private readonly IItemRepository _itemRepository;
    private readonly ILogger<GetAllItemsQueryHandler> _logger;

    public GetAllItemsQueryHandler(
        IItemRepository itemRepository,
        ILogger<GetAllItemsQueryHandler> logger)
    {
        _itemRepository = itemRepository;
        _logger = logger;
    }

    public Task<Result<List<ItemDto>>> Handle(GetAllItemsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving all items from catalog");

        var result = _itemRepository.GetAll()
            .Map(items => items
                .Select(item => new ItemDto(
                    item.Id,
                    item.AtlasX,
                    item.AtlasY,
                    item.Name,
                    item.Type,
                    item.SpriteWidth,
                    item.SpriteHeight,
                    item.InventoryWidth,
                    item.InventoryHeight,
                    item.MaxStackSize,
                    item.IsStackable,
                    item.Shape)) // PHASE 4: Include shape
                .ToList());

        if (result.IsSuccess)
        {
            _logger.LogDebug("Retrieved {Count} items from catalog", result.Value.Count);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve items: {Error}", result.Error);
        }

        return Task.FromResult(result);
    }
}
