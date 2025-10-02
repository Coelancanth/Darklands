using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Inventory.Infrastructure;

[Trait("Category", "Phase3")]
[Trait("Category", "Integration")]
public class InMemoryInventoryRepositoryTests
{
    [Fact]
    public async Task GetByActorIdAsync_FirstTime_ShouldAutoCreateInventory()
    {
        // DESIGN DECISION: Auto-create inventory with default capacity (20 slots)

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Act
        var result = await repository.GetByActorIdAsync(actorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Capacity.Should().Be(20); // Default capacity
        result.Value.Count.Should().Be(0);
        result.Value.IsFull.Should().BeFalse();
    }

    [Fact]
    public async Task GetByActorIdAsync_SecondTime_ShouldReturnSameInventory()
    {
        // WHY: Repository must return same instance for idempotency

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Get inventory first time and add item
        var inv1 = await repository.GetByActorIdAsync(actorId);
        inv1.Value.AddItem(itemId);

        // Act
        var inv2 = await repository.GetByActorIdAsync(actorId);

        // Assert
        inv2.Value.Contains(itemId).Should().BeTrue(); // Item persisted
        inv2.Value.Id.Should().Be(inv1.Value.Id); // Same instance
    }

    [Fact]
    public async Task SaveAsync_ShouldSucceed()
    {
        // WHY: In-memory implementation is no-op but must return success

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var inventory = await repository.GetByActorIdAsync(actorId);

        // Act
        var result = await repository.SaveAsync(inventory.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveInventory()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Get inventory (auto-creates)
        var inventory = await repository.GetByActorIdAsync(actorId);
        var inventoryId = inventory.Value.Id;

        // Act
        var deleteResult = await repository.DeleteAsync(inventoryId);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();

        // Verify: Getting inventory again auto-creates a NEW one
        var newInventory = await repository.GetByActorIdAsync(actorId);
        newInventory.Value.Id.Should().NotBe(inventoryId); // Different instance
    }
}
