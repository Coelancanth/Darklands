using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Infrastructure;
using Darklands.Core.Tests.Features.Item.Application.Stubs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Item = Darklands.Core.Features.Item.Domain.Item;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Tests.Features.Inventory.Application.Queries;

[Trait("Category", "Inventory")]
[Trait("Category", "Unit")]
public class CanPlaceItemAtQueryHandlerTests
{
    [Fact]
    public async Task Handle_FreePosition_ShouldReturnTrue()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(2, 2);

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        inventoryRepo.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        var handler = new CanPlaceItemAtQueryHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<CanPlaceItemAtQueryHandler>.Instance);

        var query = new CanPlaceItemAtQuery(inventoryId, itemId, position);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OccupiedPosition_ShouldReturnFalse()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId1 = ItemId.NewId();
        var itemId2 = ItemId.NewId();
        var position = new GridPosition(2, 2);

        var item1 = Darklands.Core.Features.Item.Domain.Item.Create(itemId1, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
        var item2 = Darklands.Core.Features.Item.Domain.Item.Create(itemId2, 1, 0, "Axe", "weapon", 1, 1, 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item1, item2);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        inventoryRepo.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        // Place item1 at position
        var inventoryResult = await inventoryRepo.GetByIdAsync(inventoryId);
        inventoryResult.Value.PlaceItemAt(itemId1, position);
        await inventoryRepo.SaveAsync(inventoryResult.Value, default);

        var handler = new CanPlaceItemAtQueryHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<CanPlaceItemAtQueryHandler>.Instance);

        var query = new CanPlaceItemAtQuery(inventoryId, itemId2, position);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WeaponToWeaponSlot_ShouldReturnTrue()
    {
        // BUSINESS RULE: Type filtering validation

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(0, 0);

        var weapon = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(weapon);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        inventoryRepo.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        // Create weapon-only inventory
        var weaponInventory = Core.Features.Inventory.Domain.Inventory.Create(
            inventoryId,
            gridWidth: 1,
            gridHeight: 4,
            ContainerType.WeaponOnly).Value;
        await inventoryRepo.SaveAsync(weaponInventory, default);

        var handler = new CanPlaceItemAtQueryHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<CanPlaceItemAtQueryHandler>.Instance);

        var query = new CanPlaceItemAtQuery(inventoryId, itemId, position);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonWeaponToWeaponSlot_ShouldReturnFalse()
    {
        // BUSINESS RULE: Type filtering rejection

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(0, 0);

        var potion = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 1, 0, "Potion", "item", 1, 1, 1, 1, 10).Value;
        var itemRepo = new StubItemRepository(potion);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        inventoryRepo.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        // Create weapon-only inventory
        var weaponInventory = Core.Features.Inventory.Domain.Inventory.Create(
            inventoryId,
            gridWidth: 1,
            gridHeight: 4,
            ContainerType.WeaponOnly).Value;
        await inventoryRepo.SaveAsync(weaponInventory, default);

        var handler = new CanPlaceItemAtQueryHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<CanPlaceItemAtQueryHandler>.Instance);

        var query = new CanPlaceItemAtQuery(inventoryId, itemId, position);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
