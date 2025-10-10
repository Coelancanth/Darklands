using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Infrastructure;
using Darklands.Core.Tests.Features.Item.Application.Stubs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Inventory.Application.Queries;

/// <summary>
/// Tests for CalculateHighlightCellsQueryHandler.
/// Validates business logic for highlight cell calculation (rotation + equipment slot override).
/// </summary>
public class CalculateHighlightCellsQueryHandlerTests
{
    [Fact]
    public async Task Handle_RectangleItem_NoRotation_ShouldReturnAllCells()
    {
        // Arrange: 2×3 rectangle at (1,1) with no rotation
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(1, 1);
        var rotation = Rotation.Degrees0;

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Test Item", "item", 2, 3, 2, 3, 1).Value; // 2×3 item
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        inventoryRepo.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        var handler = new CalculateHighlightCellsQueryHandler(inventoryRepo, itemRepo, NullLogger<CalculateHighlightCellsQueryHandler>.Instance);

        var query = new CalculateHighlightCellsQuery(inventoryId, itemId, position, rotation);

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
    public async Task Handle_RectangleItem_Rotated90_ShouldReturnRotatedCells()
    {
        // WHY: Rotation changes which cells are highlighted (2×3 becomes 3×2)

        // Arrange: 2×3 rectangle at (1,1) rotated 90° clockwise
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(1, 1);
        var rotation = Rotation.Degrees90;

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Test Item", "item", 2, 3, 2, 3, 1).Value; // 2×3 base
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        inventoryRepo.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        var handler = new CalculateHighlightCellsQueryHandler(inventoryRepo, itemRepo, NullLogger<CalculateHighlightCellsQueryHandler>.Instance);

        var query = new CalculateHighlightCellsQuery(inventoryId, itemId, position, rotation);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        // After 90° rotation: 2×3 → 3×2 (3 wide, 2 tall)
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(6); // Still 6 cells, different positions
        result.Value.Should().Contain(new GridPosition(1, 1)); // Top-left
        result.Value.Should().Contain(new GridPosition(2, 1));
        result.Value.Should().Contain(new GridPosition(3, 1)); // Top-right
        result.Value.Should().Contain(new GridPosition(1, 2)); // Bottom-left
        result.Value.Should().Contain(new GridPosition(2, 2));
        result.Value.Should().Contain(new GridPosition(3, 2)); // Bottom-right
    }

    [Fact]
    public async Task Handle_EquipmentSlot_ShouldReturn1x1Override()
    {
        // WHY: Equipment slots display items as 1×1 regardless of actual shape (Diablo 2 pattern)
        // BUSINESS RULE: Visual feedback = single cell highlight, not multi-cell L-shape

        // Arrange: 2×3 item in equipment slot (should override to 1×1)
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(0, 0);
        var rotation = Rotation.Degrees0;

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Weapon", "weapon", 2, 3, 2, 3, 1).Value; // 2×3 actual
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Create WeaponOnly container and register it
        var inventoryId = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(
            inventoryId,
            1, 1, ContainerType.WeaponOnly).Value;
        inventoryRepo.RegisterInventory(inventory);

        var handler = new CalculateHighlightCellsQueryHandler(inventoryRepo, itemRepo, NullLogger<CalculateHighlightCellsQueryHandler>.Instance);

        var query = new CalculateHighlightCellsQuery(inventoryId, itemId, position, rotation);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        // Equipment slot override: Ignore actual shape, force 1×1 highlight
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1); // Single cell only!
        result.Value.Should().Contain(new GridPosition(0, 0));
    }

    [Fact]
    public async Task Handle_ItemNotFound_ShouldReturnFailure()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(0, 0);

        var itemRepo = new StubItemRepository(); // Empty repository
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        inventoryRepo.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        var handler = new CalculateHighlightCellsQueryHandler(inventoryRepo, itemRepo, NullLogger<CalculateHighlightCellsQueryHandler>.Instance);

        var query = new CalculateHighlightCellsQuery(inventoryId, itemId, position, Rotation.Degrees0);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AutoCreatedInventory_ShouldUseGeneralContainer()
    {
        // WHY: InMemoryInventoryRepository auto-creates General containers for unknown actors
        // This test validates that auto-created inventories DON'T apply equipment slot override

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(0, 0);

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Test", "item", 2, 2, 2, 2, 1).Value; // 2×2 item
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        inventoryRepo.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        var handler = new CalculateHighlightCellsQueryHandler(inventoryRepo, itemRepo, NullLogger<CalculateHighlightCellsQueryHandler>.Instance);

        var query = new CalculateHighlightCellsQuery(inventoryId, itemId, position, Rotation.Degrees0);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        // Auto-created inventory is General, so NO equipment slot override (shows full 2×2)
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4); // 2×2 = 4 cells, NOT overridden to 1×1
    }
}
