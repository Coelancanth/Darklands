using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Infrastructure;
using Darklands.Core.Tests.Features.Item.Application.Stubs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Inventory.Application.Commands;

/// <summary>
/// Tests for SwapItemsCommandHandler.
/// Validates atomic swap operation with rollback safety.
/// </summary>
/// <remarks>
/// TD_004 Leak #5: Replaces Presentation swap logic at SpatialInventoryContainerNode.cs:476-491, 1122-1202 (78 lines)
/// Core handles: swap decision, atomic execution, rollback on failure.
/// </remarks>
public class SwapItemsCommandHandlerTests
{
    [Fact]
    public async Task Handle_EquipmentSlotSwap_ShouldSwapItems()
    {
        // WHY: Equipment slots allow swapping items when both positions are occupied
        // BUSINESS RULE: WeaponOnly containers support swap for better UX

        // Arrange: Two items in equipment slots
        var actorId = ActorId.NewId();
        var itemId1 = ItemId.NewId();
        var itemId2 = ItemId.NewId();

        var item1 = Darklands.Core.Features.Item.Domain.Item.Create(itemId1, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
        var item2 = Darklands.Core.Features.Item.Domain.Item.Create(itemId2, 1, 0, "Axe", "weapon", 1, 1, 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item1, item2);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Two equipment slots (WeaponOnly, 1×1)
        var inventoryId1 = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory1 = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId1, 1, 1, ContainerType.WeaponOnly).Value;
        inventory1.PlaceItemAt(itemId1, new GridPosition(0, 0), item1.Shape, Rotation.Degrees0);

        var inventoryId2 = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory2 = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId2, 1, 1, ContainerType.WeaponOnly).Value;
        inventory2.PlaceItemAt(itemId2, new GridPosition(0, 0), item2.Shape, Rotation.Degrees0);

        var actorId2 = ActorId.NewId();
        inventoryRepo.RegisterInventoryForActor(actorId, inventory1);
        inventoryRepo.RegisterInventoryForActor(actorId2, inventory2);

        var handler = new SwapItemsCommandHandler(inventoryRepo, itemRepo, NullLogger<SwapItemsCommandHandler>.Instance);
        var command = new SwapItemsCommand(
            actorId, itemId1, new GridPosition(0, 0),
            actorId2, itemId2, new GridPosition(0, 0),
            Rotation.Degrees0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify swap occurred
        var inv1ItemsResult = inventory1.GetItemPosition(itemId2); // Item2 should be in inventory1 now
        var inv2ItemsResult = inventory2.GetItemPosition(itemId1); // Item1 should be in inventory2 now

        inv1ItemsResult.IsSuccess.Should().BeTrue();
        inv2ItemsResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DifferentContainerMove_ShouldMoveItem()
    {
        // WHY: When target is unoccupied, command executes MOVE not SWAP

        // Arrange: Item in one container, move to empty position in another
        var actorId1 = ActorId.NewId();
        var actorId2 = ActorId.NewId();
        var itemId = ItemId.NewId();

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Potion", "item", 1, 1, 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventoryId1 = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory1 = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId1, 5, 5, ContainerType.General).Value;
        inventory1.PlaceItemAt(itemId, new GridPosition(0, 0), item.Shape, Rotation.Degrees0);

        var inventoryId2 = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory2 = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId2, 5, 5, ContainerType.General).Value;

        inventoryRepo.RegisterInventoryForActor(actorId1, inventory1);
        inventoryRepo.RegisterInventoryForActor(actorId2, inventory2);

        var handler = new SwapItemsCommandHandler(inventoryRepo, itemRepo, NullLogger<SwapItemsCommandHandler>.Instance);
        var command = new SwapItemsCommand(
            actorId1, itemId, new GridPosition(0, 0),
            actorId2, null, new GridPosition(2, 2),  // null targetItemId = move not swap
            Rotation.Degrees0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify move occurred
        inventory1.Contains(itemId).Should().BeFalse(); // Item removed from source
        var newPosResult = inventory2.GetItemPosition(itemId);
        newPosResult.IsSuccess.Should().BeTrue();
        newPosResult.Value.Should().Be(new GridPosition(2, 2)); // Item in target
    }

    [Fact]
    public async Task Handle_SwapFailureDueToInvalidPlacement_ShouldRollback()
    {
        // WHY: If swap fails (type mismatch, bounds, etc.), items must return to original positions
        // TRANSACTION SAFETY: All-or-nothing operation

        // Arrange: Try to swap items where target placement would fail
        var actorId = ActorId.NewId();
        var itemId1 = ItemId.NewId();
        var itemId2 = ItemId.NewId();

        // Item1: 1×1, Item2: 2×2 (won't fit in 1×1 equipment slot)
        var item1 = Darklands.Core.Features.Item.Domain.Item.Create(itemId1, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
        var item2 = Darklands.Core.Features.Item.Domain.Item.Create(itemId2, 1, 0, "Shield", "weapon", 2, 2, 2, 2, 1).Value;
        var itemRepo = new StubItemRepository(item1, item2);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Equipment slot 1: 1×1 (has item1)
        var inventoryId1 = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory1 = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId1, 1, 1, ContainerType.WeaponOnly).Value;
        inventory1.PlaceItemAt(itemId1, new GridPosition(0, 0), item1.Shape, Rotation.Degrees0);

        // Regular inventory: 5×5 (has item2)
        var inventoryId2 = new Darklands.Core.Features.Inventory.Domain.InventoryId(Guid.NewGuid());
        var inventory2 = Darklands.Core.Features.Inventory.Domain.Inventory.Create(inventoryId2, 5, 5, ContainerType.General).Value;
        inventory2.PlaceItemAt(itemId2, new GridPosition(0, 0), item2.Shape, Rotation.Degrees0);

        var actorId2 = ActorId.NewId();
        inventoryRepo.RegisterInventoryForActor(actorId, inventory1);
        inventoryRepo.RegisterInventoryForActor(actorId2, inventory2);

        var handler = new SwapItemsCommandHandler(inventoryRepo, itemRepo, NullLogger<SwapItemsCommandHandler>.Instance);
        var command = new SwapItemsCommand(
            actorId, itemId1, new GridPosition(0, 0),
            actorId2, itemId2, new GridPosition(0, 0),
            Rotation.Degrees0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        // Swap should fail (2×2 item won't fit in 1×1 slot)
        result.IsFailure.Should().BeTrue();

        // Verify rollback: Items should be at original positions
        inventory1.GetItemPosition(itemId1).Value.Should().Be(new GridPosition(0, 0));
        inventory2.GetItemPosition(itemId2).Value.Should().Be(new GridPosition(0, 0));
    }
}
