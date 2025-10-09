using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Entities;
using Darklands.Core.Features.Equipment.Application.Commands;
using Darklands.Core.Features.Equipment.Domain;
using Darklands.Core.Features.Inventory.Application;
using Darklands.Core.Features.Inventory.Infrastructure;
using Darklands.Core.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Equipment.Application.Commands;

/// <summary>
/// Integration tests for equipment command handlers.
/// Tests atomic transactions between Equipment component and Inventory repository.
/// </summary>
[Trait("Category", "Equipment")]
[Trait("Category", "Integration")]
public class EquipmentCommandHandlerTests
{
    #region Helper Methods

    private static (InMemoryActorRepository, InMemoryInventoryRepository) CreateRepositories()
    {
        var actors = new InMemoryActorRepository();
        var inventories = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        return (actors, inventories);
    }

    private static async Task<(Actor actor, ItemId itemId)> SetupActorWithItemInInventory(
        InMemoryActorRepository actors,
        InMemoryInventoryRepository inventories)
    {
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        await actors.AddActorAsync(actor);

        var itemId = ItemId.NewId();
        var inventory = await inventories.GetByActorIdAsync(actor.Id);
        inventory.Value.AddItem(itemId);
        await inventories.SaveAsync(inventory.Value);

        return (actor, itemId);
    }

    #endregion

    #region EquipItemCommand Tests

    [Fact]
    public async Task EquipItem_ToEmptySlot_ShouldSucceedAndRemoveFromInventory()
    {
        // WHY: Core flow - equip item from inventory to empty equipment slot

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var (actor, itemId) = await SetupActorWithItemInInventory(actors, inventories);
        var handler = new EquipItemCommandHandler(actors, inventories, NullLogger<EquipItemCommandHandler>.Instance);

        // Act
        var result = await handler.Handle(
            new EquipItemCommand(actor.Id, itemId, EquipmentSlot.MainHand, IsTwoHanded: false),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify item equipped
        var actorResult = await actors.GetByIdAsync(actor.Id);
        var equipment = actorResult.Value.GetComponent<IEquipmentComponent>().Value;
        equipment.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeTrue();
        equipment.GetEquippedItem(EquipmentSlot.MainHand).Value.Should().Be(itemId);

        // Verify item removed from inventory
        var inventory = await inventories.GetByActorIdAsync(actor.Id);
        inventory.Value.Contains(itemId).Should().BeFalse();
    }

    [Fact]
    public async Task EquipItem_TwoHandedWeapon_ShouldOccupyBothHands()
    {
        // WHY: Two-handed weapons must occupy MainHand + OffHand simultaneously

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var (actor, itemId) = await SetupActorWithItemInInventory(actors, inventories);
        var handler = new EquipItemCommandHandler(actors, inventories, NullLogger<EquipItemCommandHandler>.Instance);

        // Act
        var result = await handler.Handle(
            new EquipItemCommand(actor.Id, itemId, EquipmentSlot.MainHand, IsTwoHanded: true),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var actorResult = await actors.GetByIdAsync(actor.Id);
        var equipment = actorResult.Value.GetComponent<IEquipmentComponent>().Value;
        equipment.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeTrue();
        equipment.IsSlotOccupied(EquipmentSlot.OffHand).Should().BeTrue();
        equipment.GetEquippedItem(EquipmentSlot.MainHand).Value.Should().Be(itemId);
        equipment.GetEquippedItem(EquipmentSlot.OffHand).Value.Should().Be(itemId); // Same item
    }

    [Fact]
    public async Task EquipItem_ToOccupiedSlot_ShouldFail()
    {
        // WHY: Cannot equip to occupied slot - must unequip or swap first

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var (actor, itemId1) = await SetupActorWithItemInInventory(actors, inventories);
        var itemId2 = ItemId.NewId();
        var inventory = await inventories.GetByActorIdAsync(actor.Id);
        inventory.Value.AddItem(itemId2);
        await inventories.SaveAsync(inventory.Value);

        var handler = new EquipItemCommandHandler(actors, inventories, NullLogger<EquipItemCommandHandler>.Instance);

        // Equip first item
        await handler.Handle(new EquipItemCommand(actor.Id, itemId1, EquipmentSlot.MainHand, false), default);

        // Act - Try to equip second item to same slot
        var result = await handler.Handle(new EquipItemCommand(actor.Id, itemId2, EquipmentSlot.MainHand, false), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("SLOT_OCCUPIED");
    }

    [Fact]
    public async Task EquipItem_ItemNotInInventory_ShouldFail()
    {
        // WHY: Cannot equip item that doesn't exist in inventory

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        await actors.AddActorAsync(actor);
        var handler = new EquipItemCommandHandler(actors, inventories, NullLogger<EquipItemCommandHandler>.Instance);

        // Act
        var result = await handler.Handle(
            new EquipItemCommand(actor.Id, ItemId.NewId(), EquipmentSlot.MainHand, false),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("NOT_IN_INVENTORY");
    }

    #endregion

    #region UnequipItemCommand Tests

    [Fact]
    public async Task UnequipItem_FromOccupiedSlot_ShouldSucceedAndAddToInventory()
    {
        // WHY: Core flow - unequip item from equipment slot back to inventory

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var (actor, itemId) = await SetupActorWithItemInInventory(actors, inventories);
        var equipHandler = new EquipItemCommandHandler(actors, inventories, NullLogger<EquipItemCommandHandler>.Instance);
        var unequipHandler = new UnequipItemCommandHandler(actors, inventories, NullLogger<UnequipItemCommandHandler>.Instance);

        // Equip first
        await equipHandler.Handle(new EquipItemCommand(actor.Id, itemId, EquipmentSlot.MainHand, false), default);

        // Act
        var result = await unequipHandler.Handle(new UnequipItemCommand(actor.Id, EquipmentSlot.MainHand), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(itemId);

        // Verify slot is empty
        var actorResult = await actors.GetByIdAsync(actor.Id);
        var equipment = actorResult.Value.GetComponent<IEquipmentComponent>().Value;
        equipment.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeFalse();

        // Verify item back in inventory
        var inventory = await inventories.GetByActorIdAsync(actor.Id);
        inventory.Value.Contains(itemId).Should().BeTrue();
    }

    [Fact]
    public async Task UnequipItem_TwoHandedWeapon_ShouldClearBothHands()
    {
        // WHY: Unequipping two-handed weapon must clear both MainHand and OffHand atomically

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var (actor, itemId) = await SetupActorWithItemInInventory(actors, inventories);
        var equipHandler = new EquipItemCommandHandler(actors, inventories, NullLogger<EquipItemCommandHandler>.Instance);
        var unequipHandler = new UnequipItemCommandHandler(actors, inventories, NullLogger<UnequipItemCommandHandler>.Instance);

        // Equip two-handed weapon
        await equipHandler.Handle(new EquipItemCommand(actor.Id, itemId, EquipmentSlot.MainHand, true), default);

        // Act
        var result = await unequipHandler.Handle(new UnequipItemCommand(actor.Id, EquipmentSlot.MainHand), default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var actorResult = await actors.GetByIdAsync(actor.Id);
        var equipment = actorResult.Value.GetComponent<IEquipmentComponent>().Value;
        equipment.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeFalse();
        equipment.IsSlotOccupied(EquipmentSlot.OffHand).Should().BeFalse(); // Both hands cleared
    }

    [Fact]
    public async Task UnequipItem_FromEmptySlot_ShouldFail()
    {
        // WHY: Cannot unequip from empty slot

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        await actors.AddActorAsync(actor);
        actor.AddComponent<IEquipmentComponent>(new EquipmentComponent(actor.Id));
        var handler = new UnequipItemCommandHandler(actors, inventories, NullLogger<UnequipItemCommandHandler>.Instance);

        // Act
        var result = await handler.Handle(new UnequipItemCommand(actor.Id, EquipmentSlot.MainHand), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("EMPTY");
    }

    #endregion

    #region SwapEquipmentCommand Tests

    [Fact]
    public async Task SwapEquipment_ValidSwap_ShouldExchangeItems()
    {
        // WHY: Atomic swap - old item to inventory, new item from inventory

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var (actor, oldItemId) = await SetupActorWithItemInInventory(actors, inventories);
        var newItemId = ItemId.NewId();
        var inventory = await inventories.GetByActorIdAsync(actor.Id);
        inventory.Value.AddItem(newItemId);
        await inventories.SaveAsync(inventory.Value);

        var equipHandler = new EquipItemCommandHandler(actors, inventories, NullLogger<EquipItemCommandHandler>.Instance);
        var swapHandler = new SwapEquipmentCommandHandler(actors, inventories, NullLogger<SwapEquipmentCommandHandler>.Instance);

        // Equip old item first
        await equipHandler.Handle(new EquipItemCommand(actor.Id, oldItemId, EquipmentSlot.MainHand, false), default);

        // Act
        var result = await swapHandler.Handle(
            new SwapEquipmentCommand(actor.Id, newItemId, EquipmentSlot.MainHand, false),
            default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(oldItemId); // Returns old item ID

        // Verify new item equipped
        var actorResult = await actors.GetByIdAsync(actor.Id);
        var equipment = actorResult.Value.GetComponent<IEquipmentComponent>().Value;
        equipment.GetEquippedItem(EquipmentSlot.MainHand).Value.Should().Be(newItemId);

        // Verify old item in inventory, new item removed
        var updatedInventory = await inventories.GetByActorIdAsync(actor.Id);
        updatedInventory.Value.Contains(oldItemId).Should().BeTrue();
        updatedInventory.Value.Contains(newItemId).Should().BeFalse();
    }

    [Fact]
    public async Task SwapEquipment_EmptySlot_ShouldFail()
    {
        // WHY: Swap requires existing equipped item

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var (actor, itemId) = await SetupActorWithItemInInventory(actors, inventories);

        // Add equipment component so validation passes that step
        actor.AddComponent<IEquipmentComponent>(new EquipmentComponent(actor.Id));

        var handler = new SwapEquipmentCommandHandler(actors, inventories, NullLogger<SwapEquipmentCommandHandler>.Instance);

        // Act
        var result = await handler.Handle(
            new SwapEquipmentCommand(actor.Id, itemId, EquipmentSlot.MainHand, false),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("EMPTY");
    }

    [Fact]
    public async Task SwapEquipment_NewItemNotInInventory_ShouldFail()
    {
        // WHY: Cannot swap with item that doesn't exist in inventory

        // Arrange
        var (actors, inventories) = CreateRepositories();
        var (actor, oldItemId) = await SetupActorWithItemInInventory(actors, inventories);
        var equipHandler = new EquipItemCommandHandler(actors, inventories, NullLogger<EquipItemCommandHandler>.Instance);
        var swapHandler = new SwapEquipmentCommandHandler(actors, inventories, NullLogger<SwapEquipmentCommandHandler>.Instance);

        // Equip old item first
        await equipHandler.Handle(new EquipItemCommand(actor.Id, oldItemId, EquipmentSlot.MainHand, false), default);

        // Act - Try to swap with non-existent item
        var result = await swapHandler.Handle(
            new SwapEquipmentCommand(actor.Id, ItemId.NewId(), EquipmentSlot.MainHand, false),
            default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("NOT_IN_INVENTORY");
    }

    #endregion
}
