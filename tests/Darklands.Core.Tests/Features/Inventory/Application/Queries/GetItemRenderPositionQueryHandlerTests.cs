using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Infrastructure;
using Darklands.Core.Tests.Features.Item.Application.Stubs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Inventory.Application.Queries;

/// <summary>
/// Tests for GetItemRenderPositionQueryHandler.
/// Validates business logic for determining how items should be positioned for rendering.
/// </summary>
/// <remarks>
/// TD_004 Leak #3: Replaces equipment slot centering logic at SpatialInventoryContainerNode.cs:853-871
/// Core provides GridOffset - Presentation applies pixel math.
/// </remarks>
public class GetItemRenderPositionQueryHandlerTests
{
    [Fact]
    public async Task Handle_RegularInventoryItem_ShouldReturnZeroOffset()
    {
        // WHY: Regular grid items align to top-left of cells (no centering)

        // Arrange: Item in regular General inventory
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Test Item", "item", 2, 3, 2, 3, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventoryId = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId, 5, 5, ContainerType.General).Value;
        var position = new GridPosition(1, 1);
        inventory.PlaceItemAt(itemId, position, item.Shape, Rotation.Degrees0);
        inventoryRepo.RegisterInventoryForActor(actorId, inventory);

        var handler = new GetItemRenderPositionQueryHandler(inventoryRepo, itemRepo, NullLogger<GetItemRenderPositionQueryHandler>.Instance);
        var query = new GetItemRenderPositionQuery(actorId, itemId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShouldCenterInSlot.Should().BeFalse(); // Regular inventory - no centering
        result.Value.Position.Should().Be(new GridPosition(1, 1));
        result.Value.EffectiveWidth.Should().Be(2); // 2×3 item, no rotation
        result.Value.EffectiveHeight.Should().Be(3);
    }

    [Fact]
    public async Task Handle_EquipmentSlotItem_ShouldReturnCenterOffset()
    {
        // WHY: Equipment slots center items (Diablo 2 pattern)
        // BUSINESS RULE: WeaponOnly containers with 1×1 grid display items centered

        // Arrange: Item in equipment slot (WeaponOnly, 1×1 grid)
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();

        // 1×1 weapon (fits in equipment slot)
        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Weapon", "weapon", 1, 1, 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Equipment slot: WeaponOnly + 1×1 grid
        var inventoryId = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId, 1, 1, ContainerType.WeaponOnly).Value;
        var position = new GridPosition(0, 0);
        inventory.PlaceItemAt(itemId, position, item.Shape, Rotation.Degrees0); // Should succeed
        inventoryRepo.RegisterInventoryForActor(actorId, inventory);

        var handler = new GetItemRenderPositionQueryHandler(inventoryRepo, itemRepo, NullLogger<GetItemRenderPositionQueryHandler>.Instance);
        var query = new GetItemRenderPositionQuery(actorId, itemId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShouldCenterInSlot.Should().BeTrue(); // Equipment slot = centered!
        result.Value.Position.Should().Be(new GridPosition(0, 0));
        result.Value.EffectiveWidth.Should().Be(1); // 1×1 weapon, no rotation
        result.Value.EffectiveHeight.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ItemNotInInventory_ShouldReturnFailure()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();

        var itemRepo = new StubItemRepository(); // Empty
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var handler = new GetItemRenderPositionQueryHandler(inventoryRepo, itemRepo, NullLogger<GetItemRenderPositionQueryHandler>.Instance);
        var query = new GetItemRenderPositionQuery(actorId, itemId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
