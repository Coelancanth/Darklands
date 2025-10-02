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
public class MoveItemBetweenContainersCommandHandlerTests
{
    [Fact]
    public async Task Handle_MoveItemBetweenActors_ShouldSucceed()
    {
        // Arrange
        var sourceActorId = ActorId.NewId();
        var targetActorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var sourcePos = new GridPosition(0, 0);
        var targetPos = new GridPosition(3, 3);

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Place item in source inventory
        var sourceInventory = await inventoryRepo.GetByActorIdAsync(sourceActorId);
        sourceInventory.Value.PlaceItemAt(itemId, sourcePos);
        await inventoryRepo.SaveAsync(sourceInventory.Value, default);

        var handler = new MoveItemBetweenContainersCommandHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<MoveItemBetweenContainersCommandHandler>.Instance);

        var command = new MoveItemBetweenContainersCommand(
            sourceActorId,
            targetActorId,
            itemId,
            targetPos);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify item removed from source
        var updatedSource = await inventoryRepo.GetByActorIdAsync(sourceActorId);
        updatedSource.Value.Contains(itemId).Should().BeFalse();

        // Verify item added to target
        var updatedTarget = await inventoryRepo.GetByActorIdAsync(targetActorId);
        updatedTarget.Value.Contains(itemId).Should().BeTrue();
        updatedTarget.Value.GetItemPosition(itemId).Should().Be(targetPos);
    }

    [Fact]
    public async Task Handle_RepositionWithinSameInventory_ShouldSucceed()
    {
        // EDGE CASE: Source and target are same inventory

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var sourcePos = new GridPosition(0, 0);
        var targetPos = new GridPosition(4, 3); // Valid position in 5×4 grid (20 capacity)

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1).Value;
        var itemRepo = new StubItemRepository(item);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Place item at source position
        var inventory = await inventoryRepo.GetByActorIdAsync(actorId);
        inventory.Value.PlaceItemAt(itemId, sourcePos);
        await inventoryRepo.SaveAsync(inventory.Value, default);

        var handler = new MoveItemBetweenContainersCommandHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<MoveItemBetweenContainersCommandHandler>.Instance);

        var command = new MoveItemBetweenContainersCommand(
            actorId,
            actorId,
            itemId,
            targetPos);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedInventory = await inventoryRepo.GetByActorIdAsync(actorId);
        updatedInventory.Value.Contains(itemId).Should().BeTrue();
        updatedInventory.Value.GetItemPosition(itemId).Should().Be(targetPos);
    }

    [Fact]
    public async Task Handle_MoveToWeaponSlotWithPotion_ShouldFail()
    {
        // BUSINESS RULE: Type filtering on target container

        // Arrange
        var sourceActorId = ActorId.NewId();
        var targetActorId = ActorId.NewId();
        var itemId = ItemId.NewId();

        var potion = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 1, 0, "Potion", "item", 1, 1, 10).Value;
        var itemRepo = new StubItemRepository(potion);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Place potion in source inventory
        var sourceInventory = await inventoryRepo.GetByActorIdAsync(sourceActorId);
        sourceInventory.Value.PlaceItemAt(itemId, new GridPosition(0, 0));
        await inventoryRepo.SaveAsync(sourceInventory.Value, default);

        // Create weapon-only target inventory
        var targetInventory = await inventoryRepo.GetByActorIdAsync(targetActorId);
        var weaponInventory = Core.Features.Inventory.Domain.Inventory.Create(
            targetInventory.Value.Id,
            gridWidth: 1,
            gridHeight: 4,
            ContainerType.WeaponOnly).Value;
        await inventoryRepo.SaveAsync(weaponInventory, default);

        var handler = new MoveItemBetweenContainersCommandHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<MoveItemBetweenContainersCommandHandler>.Instance);

        var command = new MoveItemBetweenContainersCommand(
            sourceActorId,
            targetActorId,
            itemId,
            new GridPosition(0, 0));

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("weapon");
    }
}
