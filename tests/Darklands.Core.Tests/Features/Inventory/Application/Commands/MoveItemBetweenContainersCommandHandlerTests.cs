using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Infrastructure;
using Darklands.Core.Tests.Features.Item.Application.Stubs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Item = Darklands.Core.Features.Item.Domain.Item;

namespace Darklands.Core.Tests.Features.Inventory.Application.Commands;

[Trait("Category", "Inventory")]
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

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
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
        updatedTarget.Value.GetItemPosition(itemId).Value.Should().Be(targetPos);
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

        var item = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "Sword", "weapon", 1, 1, 1, 1, 1).Value;
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
        updatedInventory.Value.GetItemPosition(itemId).Value.Should().Be(targetPos);
    }

    [Fact]
    public async Task Handle_MoveToWeaponSlotWithPotion_ShouldFail()
    {
        // BUSINESS RULE: Type filtering on target container

        // Arrange
        var sourceActorId = ActorId.NewId();
        var targetActorId = ActorId.NewId();
        var itemId = ItemId.NewId();

        var potion = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 1, 0, "Potion", "item", 1, 1, 1, 1, 10).Value;
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

    [Fact]
    public async Task Handle_FailedTypeValidation_ShouldNotRemoveItemFromSource()
    {
        // REGRESSION TEST: Data loss bug fix
        // WHY: Before fix, items were removed from source before type validation,
        // causing item loss when validation failed. This test ensures items remain
        // in source inventory if target container rejects them.
        //
        // Bug scenario (pre-fix):
        // 1. User drags potion to weapon slot
        // 2. Handler removes potion from source inventory
        // 3. Type validation fails (WeaponOnly rejects "item" type)
        // 4. Early return with failure
        // 5. Potion never added to target, already removed from source = DATA LOSS
        //
        // Fix: Type validation moved BEFORE RemoveItem() call

        // Arrange
        var sourceActorId = ActorId.NewId();
        var targetActorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var sourcePos = new GridPosition(2, 1);

        var potion = Darklands.Core.Features.Item.Domain.Item.Create(
            itemId, 1, 0, "Health Potion", "item", 1, 1, 1, 1, 10).Value;
        var itemRepo = new StubItemRepository(potion);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Place potion in source inventory at specific position
        var sourceInventory = await inventoryRepo.GetByActorIdAsync(sourceActorId);
        sourceInventory.Value.PlaceItemAt(itemId, sourcePos);
        await inventoryRepo.SaveAsync(sourceInventory.Value, default);

        // Verify initial state: Potion in source at (2,1)
        sourceInventory = await inventoryRepo.GetByActorIdAsync(sourceActorId);
        sourceInventory.Value.Contains(itemId).Should().BeTrue();
        sourceInventory.Value.GetItemPosition(itemId).Value.Should().Be(sourcePos);

        // Create weapon-only target inventory (will reject potion)
        var targetInventory = await inventoryRepo.GetByActorIdAsync(targetActorId);
        var weaponSlot = Core.Features.Inventory.Domain.Inventory.Create(
            targetInventory.Value.Id,
            gridWidth: 1,
            gridHeight: 1,
            ContainerType.WeaponOnly).Value;
        await inventoryRepo.SaveAsync(weaponSlot, default);

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

        // Assert: Command should fail
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("weapon");

        // CRITICAL: Verify item STILL in source inventory (not lost!)
        var finalSourceState = await inventoryRepo.GetByActorIdAsync(sourceActorId);
        finalSourceState.Value.Contains(itemId).Should().BeTrue("item must remain in source after failed validation");
        finalSourceState.Value.GetItemPosition(itemId).Value.Should().Be(sourcePos, "item position should be unchanged");

        // Verify item NOT in target inventory
        var finalTargetState = await inventoryRepo.GetByActorIdAsync(targetActorId);
        finalTargetState.Value.Contains(itemId).Should().BeFalse("item should not be added to target after validation failure");
    }

    [Fact]
    public async Task Handle_Move2x2ItemBetweenContainers_ShouldPreserveDimensions()
    {
        // INTEGRATION TEST: Cross-container dimension preservation (PR #87 suggestion)
        // WHY: Ensures multi-cell items (2×2) moved from Backpack A → B still occupy 4 cells in target
        // REGRESSION RISK: If dimension-passing logic breaks, items could become 1×1 in target

        // Arrange: Create 2×2 multi-cell item (ray_gun)
        var sourceActorId = ActorId.NewId();
        var targetActorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var sourcePos = new GridPosition(1, 1);
        var targetPos = new GridPosition(3, 3);

        // 2×2 item: SpriteWidth=4, SpriteHeight=4, InventoryWidth=2, InventoryHeight=2
        var multiCellItem = Darklands.Core.Features.Item.Domain.Item.Create(itemId, 0, 0, "ray_gun", "weapon", 4, 4, 2, 2, 1).Value;
        var itemRepo = new StubItemRepository(multiCellItem);
        var inventoryRepo = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Create source backpack (10×10 grid) and place 2×2 item at (1,1)
        var sourceInventory = await inventoryRepo.GetByActorIdAsync(sourceActorId);
        sourceInventory.Value.PlaceItemAt(itemId, sourcePos, 2, 2);
        await inventoryRepo.SaveAsync(sourceInventory.Value, default);

        // Create target backpack (10×10 grid, empty)
        var targetInventory = await inventoryRepo.GetByActorIdAsync(targetActorId);
        var emptyBackpack = Core.Features.Inventory.Domain.Inventory.Create(
            targetInventory.Value.Id, 10, 10, ContainerType.General).Value;
        inventoryRepo.RegisterInventoryForActor(targetActorId, emptyBackpack);

        var handler = new MoveItemBetweenContainersCommandHandler(
            inventoryRepo,
            itemRepo,
            NullLogger<MoveItemBetweenContainersCommandHandler>.Instance);

        var command = new MoveItemBetweenContainersCommand(
            sourceActorId, targetActorId, itemId, targetPos);

        // Act: Move 2×2 item from source → target
        var result = await handler.Handle(command, default);

        // Assert: Command succeeded
        result.IsSuccess.Should().BeTrue();

        // Verify item removed from source
        var updatedSource = await inventoryRepo.GetByActorIdAsync(sourceActorId);
        updatedSource.Value.Contains(itemId).Should().BeFalse();

        // CRITICAL: Verify item STILL occupies 2×2 cells in target (not collapsed to 1×1)
        var updatedTarget = await inventoryRepo.GetByActorIdAsync(targetActorId);
        updatedTarget.Value.Contains(itemId).Should().BeTrue();
        updatedTarget.Value.GetItemPosition(itemId).Value.Should().Be(targetPos);

        // CRITICAL: Verify dimensions preserved in target (not collapsed to 1×1)
        var dimensions = updatedTarget.Value.ItemDimensions;
        dimensions.Should().ContainKey(itemId, "item dimensions should be cached in target inventory");
        dimensions[itemId].width.Should().Be(2, "inventory width should be preserved across containers");
        dimensions[itemId].height.Should().Be(2, "inventory height should be preserved across containers");

        // Verify multi-cell collision still enforced (cannot place new item at occupied origin)
        var sameOriginItem = ItemId.NewId();
        var placeAtSameOriginResult = updatedTarget.Value.PlaceItemAt(sameOriginItem, targetPos, 1, 1);
        placeAtSameOriginResult.IsFailure.Should().BeTrue("origin position should be blocked by existing 2×2 item");
    }
}
