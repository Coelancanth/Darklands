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
        // TD_019: GetByActorIdAsync (obsolete) auto-creates, GetByOwnerAsync does not

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Act (using obsolete method that auto-creates)
        #pragma warning disable CS0618 // Type or member is obsolete
        var result = await repository.GetByActorIdAsync(actorId);
        #pragma warning restore CS0618

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Capacity.Should().Be(20); // Default capacity
        result.Value.Count.Should().Be(0);
        result.Value.IsFull.Should().BeFalse();
    }

    [Fact]
    public async Task GetByOwnerAsync_SecondTime_ShouldReturnSameInventory()
    {
        // WHY: Repository must return same instance for idempotency
        // TD_019: Use GetByActorIdAsync (obsolete) for auto-creation test

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Get inventory first time and add item (auto-creates)
        #pragma warning disable CS0618 // Type or member is obsolete
        var inv1Result = await repository.GetByActorIdAsync(actorId);
        #pragma warning restore CS0618
        var inv1 = inv1Result.Value;
        inv1.AddItem(itemId);

        // Act (get again)
        #pragma warning disable CS0618 // Type or member is obsolete
        var inv2Result = await repository.GetByActorIdAsync(actorId);
        #pragma warning restore CS0618
        var inv2 = inv2Result.Value;

        // Assert
        inv2.Contains(itemId).Should().BeTrue(); // Item persisted
        inv2.Id.Should().Be(inv1.Id); // Same instance
    }

    [Fact]
    public async Task SaveAsync_ShouldSucceed()
    {
        // WHY: In-memory implementation is no-op but must return success
        // TD_019: Use GetByActorIdAsync (obsolete) for auto-creation test

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        #pragma warning disable CS0618 // Type or member is obsolete
        var inventoryResult = await repository.GetByActorIdAsync(actorId);
        #pragma warning restore CS0618
        var inventory = inventoryResult.Value;

        // Act
        var result = await repository.SaveAsync(inventory);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveInventory()
    {
        // TD_019: Use GetByActorIdAsync (obsolete) for auto-creation test

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Get inventory (auto-creates)
        #pragma warning disable CS0618 // Type or member is obsolete
        var inventoryResult = await repository.GetByActorIdAsync(actorId);
        #pragma warning restore CS0618
        var inventory = inventoryResult.Value;
        var inventoryId = inventory.Id;

        // Act
        var deleteResult = await repository.DeleteAsync(inventoryId);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();

        // Verify: Getting inventory again auto-creates a NEW one
        #pragma warning disable CS0618 // Type or member is obsolete
        var newInventoryResult = await repository.GetByActorIdAsync(actorId);
        #pragma warning restore CS0618
        var newInventory = newInventoryResult.Value;
        newInventory.Id.Should().NotBe(inventoryId); // Different instance
    }
}
