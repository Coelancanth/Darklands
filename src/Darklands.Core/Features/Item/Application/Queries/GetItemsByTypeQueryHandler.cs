using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Item.Application.Queries;

/// <summary>
/// Handler for GetItemsByTypeQuery.
/// Delegates to IItemRepository for type-filtered catalog access.
/// </summary>
public class GetItemsByTypeQueryHandler : IRequestHandler<GetItemsByTypeQuery, Result<List<ItemDto>>>
{
    private readonly IItemRepository _itemRepository;
    private readonly ILogger<GetItemsByTypeQueryHandler> _logger;

    public GetItemsByTypeQueryHandler(
        IItemRepository itemRepository,
        ILogger<GetItemsByTypeQueryHandler> logger)
    {
        _itemRepository = itemRepository;
        _logger = logger;
    }

    public Task<Result<List<ItemDto>>> Handle(GetItemsByTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving items of type '{Type}'", request.Type);

        var result = _itemRepository.GetByType(request.Type)
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
            _logger.LogDebug(
                "Retrieved {Count} items of type '{Type}'",
                result.Value.Count,
                request.Type);
        }
        else
        {
            _logger.LogWarning(
                "Failed to retrieve items of type '{Type}': {Error}",
                request.Type,
                result.Error);
        }

        return Task.FromResult(result);
    }
}
