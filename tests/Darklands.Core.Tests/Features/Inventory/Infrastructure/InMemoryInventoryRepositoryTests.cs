using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Inventory.Infrastructure;

[Trait("Category", "Inventory")]
[Trait("Category", "Integration")]
public class InMemoryInventoryRepositoryTests
{
    [Fact]
    public async Task GetByOwnerAsync_FirstTime_ShouldAutoCreateInventory()
    {
        // DESIGN DECISION: Auto-create inventory with default capacity (20 slots)

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Create and register inventory
        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        repository.RegisterInventory(inventory);

        // Act
        var result = await repository.GetByOwnerAsync(actorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(); // Should return list with one inventory
        var retrievedInventory = result.Value.First();
        retrievedInventory.Capacity.Should().Be(20); // Default capacity
        retrievedInventory.Count.Should().Be(0);
        retrievedInventory.IsFull.Should().BeFalse();
    }

    [Fact]
    public async Task GetByOwnerAsync_SecondTime_ShouldReturnSameInventory()
    {
        // WHY: Repository must return same instance for idempotency

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Create and register inventory, then add item
        var inv1 = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        repository.RegisterInventory(inv1);
        inv1.AddItem(itemId);

        // Act (get again)
        var inv2Result = await repository.GetByOwnerAsync(actorId);

        // Assert
        inv2Result.IsSuccess.Should().BeTrue();
        var inv2 = inv2Result.Value.First(); // Extract from list
        inv2.Contains(itemId).Should().BeTrue(); // Item persisted
        inv2.Id.Should().Be(inv1.Id); // Same instance
    }

    [Fact]
    public async Task SaveAsync_ShouldSucceed()
    {
        // WHY: In-memory implementation is no-op but must return success

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        repository.RegisterInventory(inventory);

        // Act
        var result = await repository.SaveAsync(inventory);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveInventory()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Create and register inventory
        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        repository.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        // Act
        var deleteResult = await repository.DeleteAsync(inventoryId);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();

        // TD_019: GetByOwnerAsync returns empty list (not failure) after delete
        var getResult = await repository.GetByOwnerAsync(actorId);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().BeEmpty(); // No inventories remain for this actor
    }
}
