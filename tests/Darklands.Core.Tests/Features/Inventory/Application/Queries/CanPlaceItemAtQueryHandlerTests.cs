using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Infrastructure;
using Darklands.Core.Tests.Features.Item.Application.Stubs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Item = Darklands.Core.Features.Item.Domain.Item;

namespace Darklands.Core.Tests.Features.Inventory.Application.Queries;

[Trait("Category", "Phase2")]
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

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var handler = new CanPlaceItemAtQueryHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<CanPlaceItemAtQueryHandler>.Instance);

        var query = new CanPlaceItemAtQuery(actorId, itemId, position);

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

        var item1 = Darklands.Core.Features.Item.Domain.Item.Create(itemId1, 0, 0, "Sword", "weapon", 1, 1, 1).Value;
        var item2 = Darklands.Core.Features.Item.Domain.Item.Create(itemId2, 1, 0, "Axe", "weapon", 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item1, item2);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Place item1 at position
        var inventory = await inventoryRepo.GetByActorIdAsync(actorId);
        inventory.Value.PlaceItemAt(itemId1, position);
        await inventoryRepo.SaveAsync(inventory.Value, default);

        var handler = new CanPlaceItemAtQueryHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<CanPlaceItemAtQueryHandler>.Instance);

        var query = new CanPlaceItemAtQuery(actorId, itemId2, position);

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

        var weapon = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(weapon);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Create weapon-only inventory
        var inventory = await inventoryRepo.GetByActorIdAsync(actorId);
        var weaponInventory = Core.Features.Inventory.Domain.Inventory.Create(
            inventory.Value.Id,
            gridWidth: 1,
            gridHeight: 4,
            ContainerType.WeaponOnly).Value;
        await inventoryRepo.SaveAsync(weaponInventory, default);

        var handler = new CanPlaceItemAtQueryHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<CanPlaceItemAtQueryHandler>.Instance);

        var query = new CanPlaceItemAtQuery(actorId, itemId, position);

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

        var potion = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 1, 0, "Potion", "item", 1, 1, 10).Value;
        var itemRepo = new StubItemRepository(potion);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Create weapon-only inventory
        var inventory = await inventoryRepo.GetByActorIdAsync(actorId);
        var weaponInventory = Core.Features.Inventory.Domain.Inventory.Create(
            inventory.Value.Id,
            gridWidth: 1,
            gridHeight: 4,
            ContainerType.WeaponOnly).Value;
        await inventoryRepo.SaveAsync(weaponInventory, default);

        var handler = new CanPlaceItemAtQueryHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<CanPlaceItemAtQueryHandler>.Instance);

        var query = new CanPlaceItemAtQuery(actorId, itemId, position);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
