using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Infrastructure;
using Darklands.Core.Tests.Features.Item.Application.Stubs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Item = Darklands.Core.Features.Item.Domain.Item;

namespace Darklands.Core.Tests.Features.Inventory.Application.Commands;

[Trait("Category", "Phase2")]
[Trait("Category", "Unit")]
public class PlaceItemAtPositionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidPlacement_ShouldSucceed()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(2, 3);

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var handler = new PlaceItemAtPositionCommandHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<PlaceItemAtPositionCommandHandler>.Instance);

        var command = new PlaceItemAtPositionCommand(actorId, itemId, position);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var inventory = await inventoryRepo.GetByActorIdAsync(actorId);
        inventory.Value.Contains(itemId).Should().BeTrue();
        inventory.Value.GetItemPosition(itemId).Should().Be(position);
    }

    [Fact]
    public async Task Handle_WeaponToWeaponSlot_ShouldSucceed()
    {
        // BUSINESS RULE: Weapon slots accept weapons

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(0, 0);

        var weapon = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
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

        var handler = new PlaceItemAtPositionCommandHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<PlaceItemAtPositionCommandHandler>.Instance);

        var command = new PlaceItemAtPositionCommand(actorId, itemId, position);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonWeaponToWeaponSlot_ShouldFail()
    {
        // BUSINESS RULE: Weapon slots reject non-weapons

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(0, 0);

        var potion = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 1, 0, "Potion", "item", 1, 1, 1, 1, 10).Value;
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

        var handler = new PlaceItemAtPositionCommandHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<PlaceItemAtPositionCommandHandler>.Instance);

        var command = new PlaceItemAtPositionCommand(actorId, itemId, position);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("weapon");
    }

    [Fact]
    public async Task Handle_OutOfBounds_ShouldFail()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var position = new GridPosition(100, 100);

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var handler = new PlaceItemAtPositionCommandHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<PlaceItemAtPositionCommandHandler>.Instance);

        var command = new PlaceItemAtPositionCommand(actorId, itemId, position);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceeds grid bounds");
    }
}
