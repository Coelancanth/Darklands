using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Item.Application.Queries;

/// <summary>
/// Handler for GetItemByIdQuery.
/// Delegates to IItemRepository for item lookup.
/// </summary>
public class GetItemByIdQueryHandler : IRequestHandler<GetItemByIdQuery, Result<ItemDto>>
{
    private readonly IItemRepository _itemRepository;
    private readonly ILogger<GetItemByIdQueryHandler> _logger;

    public GetItemByIdQueryHandler(
        IItemRepository itemRepository,
        ILogger<GetItemByIdQueryHandler> logger)
    {
        _itemRepository = itemRepository;
        _logger = logger;
    }

    public Task<Result<ItemDto>> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        // POTENTIAL HOT PATH: Called when rendering inventory items
        // Only log warnings/errors, not debug info
        var result = _itemRepository.GetById(request.ItemId)
            .Map(item => new ItemDto(
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
                item.IsStackable));

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get item {ItemId}: {Error}",
                request.ItemId,
                result.Error);
        }

        return Task.FromResult(result);
    }
}
