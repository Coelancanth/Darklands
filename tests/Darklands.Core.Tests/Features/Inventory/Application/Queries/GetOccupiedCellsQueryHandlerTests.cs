using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Infrastructure;
using Darklands.Core.Tests.Features.Item.Application.Stubs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Inventory.Application.Queries;

/// <summary>
/// Tests for GetOccupiedCellsQueryHandler.
/// Validates business logic for calculating which cells an item occupies in inventory.
/// </summary>
/// <remarks>
/// TD_004 Leak #2: Replaces Presentation logic at SpatialInventoryContainerNode.cs:640-683
/// This query provides absolute cell positions so Presentation doesn't recalculate.
/// </remarks>
public class GetOccupiedCellsQueryHandlerTests
{
    [Fact]
    public async Task Handle_RectangleItem_NoRotation_ShouldReturnAllOccupiedCells()
    {
        // Arrange: 2×3 item at (1,1) with no rotation
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Test Item", "item", 2, 3, 2, 3, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Create inventory and place item
        var inventoryId = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId, 5, 5, ContainerType.General).Value;
        var position = new GridPosition(1, 1);
        inventory.PlaceItemAt(itemId, position, item.Shape, Rotation.Degrees0);
        inventoryRepo.RegisterInventoryForActor(actorId, inventory);

        var handler = new GetOccupiedCellsQueryHandler(inventoryRepo, NullLogger<GetOccupiedCellsQueryHandler>.Instance);
        var query = new GetOccupiedCellsQuery(actorId, itemId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(6); // 2×3 = 6 cells
        result.Value.Should().Contain(new GridPosition(1, 1)); // Top-left
        result.Value.Should().Contain(new GridPosition(2, 1)); // Top-right
        result.Value.Should().Contain(new GridPosition(1, 2));
        result.Value.Should().Contain(new GridPosition(2, 2));
        result.Value.Should().Contain(new GridPosition(1, 3));
        result.Value.Should().Contain(new GridPosition(2, 3)); // Bottom-right
    }

    [Fact]
    public async Task Handle_RectangleItem_Rotated90_ShouldReturnRotatedOccupiedCells()
    {
        // WHY: Rotation changes occupied cells (2×3 becomes 3×2)

        // Arrange: 2×3 item rotated 90° at (1,1)
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Test Item", "item", 2, 3, 2, 3, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventoryId = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId, 5, 5, ContainerType.General).Value;
        var position = new GridPosition(1, 1);
        inventory.PlaceItemAt(itemId, position, item.Shape, Rotation.Degrees90);
        inventoryRepo.RegisterInventoryForActor(actorId, inventory);

        var handler = new GetOccupiedCellsQueryHandler(inventoryRepo, NullLogger<GetOccupiedCellsQueryHandler>.Instance);
        var query = new GetOccupiedCellsQuery(actorId, itemId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        // Inventory stores the ALREADY-ROTATED shape when placed
        // So the occupied cells reflect the rotated footprint at the origin
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(6); // Still 6 cells total

        // WHY: PlaceItemAt() rotates the shape BEFORE storing, so retrieval gets rotated cells
        // These are the absolute positions of the rotated 2×3 → 3×2 footprint at (1,1)
        result.Value.Should().Contain(new GridPosition(1, 1));
        result.Value.Should().Contain(new GridPosition(2, 1));
        result.Value.Should().Contain(new GridPosition(3, 1));
        result.Value.Should().Contain(new GridPosition(1, 2));
        result.Value.Should().Contain(new GridPosition(2, 2));
        result.Value.Should().Contain(new GridPosition(3, 2));
    }

    [Fact]
    public async Task Handle_ItemNotInInventory_ShouldReturnFailure()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();

        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Auto-created inventory won't have this item

        var handler = new GetOccupiedCellsQueryHandler(inventoryRepo, NullLogger<GetOccupiedCellsQueryHandler>.Instance);
        var query = new GetOccupiedCellsQuery(actorId, itemId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
