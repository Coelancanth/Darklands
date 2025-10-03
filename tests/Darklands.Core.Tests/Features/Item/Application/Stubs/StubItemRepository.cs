using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using ItemEntity = Darklands.Core.Features.Item.Domain.Item;

namespace Darklands.Core.Tests.Features.Item.Application.Stubs;

/// <summary>
/// Stub implementation of IItemRepository for testing.
/// Holds items in memory, supports async API.
/// </summary>
public sealed class StubItemRepository : IItemRepository
{
    private readonly Dictionary<ItemId, ItemEntity> _items = new();

    public StubItemRepository(params ItemEntity[] items)
    {
        foreach (var item in items)
        {
            _items[item.Id] = item;
        }
    }

    public Result<ItemEntity> GetById(ItemId itemId)
    {
        if (_items.TryGetValue(itemId, out var item))
            return Result.Success(item);

        return Result.Failure<ItemEntity>($"Item {itemId} not found");
    }

    // Async wrapper for handler compatibility
    public Task<Result<ItemEntity>> GetByIdAsync(ItemId itemId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetById(itemId));
    }

    public Result<List<ItemEntity>> GetAll()
    {
        return Result.Success(_items.Values.ToList());
    }

    public Result<List<ItemEntity>> GetByType(string type)
    {
        var filtered = _items.Values.Where(i => i.Type == type).ToList();
        return Result.Success(filtered);
    }
}
