using Darklands.Core.Domain.Common;
using FluentAssertions;
using Xunit;
using InventoryEntity = Darklands.Core.Features.Inventory.Domain.Inventory;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Tests.Features.Inventory.Domain;

[Trait("Category", "Phase1")]
[Trait("Category", "Unit")]
public class InventoryTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidCapacity_ShouldSucceed()
    {
        // Arrange & Act
        var result = InventoryEntity.Create(InventoryId.NewId(), capacity: 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Capacity.Should().Be(20);
        result.Value.Count.Should().Be(0);
        result.Value.IsFull.Should().BeFalse();
        result.Value.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithNonPositiveCapacity_ShouldFail(int invalidCapacity)
    {
        // Act
        var result = InventoryEntity.Create(InventoryId.NewId(), invalidCapacity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("positive");
    }

    [Fact]
    public void Create_WithCapacityOver100_ShouldFail()
    {
        // BUSINESS RULE: Maximum inventory capacity is 100 slots

        // Act
        var result = InventoryEntity.Create(InventoryId.NewId(), capacity: 101);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceed 100");
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_WhenNotFull_ShouldSucceed()
    {
        // Arrange
        var inventory = InventoryEntity.Create(InventoryId.NewId(), capacity: 3).Value;
        var itemId = ItemId.NewId();

        // Act
        var result = inventory.AddItem(itemId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inventory.Contains(itemId).Should().BeTrue();
        inventory.Count.Should().Be(1);
    }

    [Fact]
    public void AddItem_WhenFull_ShouldFail()
    {
        // BUSINESS RULE: Cannot add items to full inventory

        // Arrange
        var inventory = InventoryEntity.Create(InventoryId.NewId(), capacity: 2).Value;
        inventory.AddItem(ItemId.NewId());
        inventory.AddItem(ItemId.NewId());

        // Act
        var result = inventory.AddItem(ItemId.NewId());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("full");
        inventory.Count.Should().Be(2);
    }

    [Fact]
    public void AddItem_WhenDuplicateItem_ShouldFail()
    {
        // BUSINESS RULE: Items are unique, cannot add same item twice

        // Arrange
        var inventory = InventoryEntity.Create(InventoryId.NewId(), capacity: 5).Value;
        var itemId = ItemId.NewId();
        inventory.AddItem(itemId);

        // Act
        var result = inventory.AddItem(itemId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already in inventory");
        inventory.Count.Should().Be(1);
    }

    #endregion

    #region RemoveItem Tests

    [Fact]
    public void RemoveItem_WhenExists_ShouldSucceed()
    {
        // Arrange
        var inventory = InventoryEntity.Create(InventoryId.NewId(), capacity: 5).Value;
        var itemId = ItemId.NewId();
        inventory.AddItem(itemId);

        // Act
        var result = inventory.RemoveItem(itemId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inventory.Contains(itemId).Should().BeFalse();
        inventory.Count.Should().Be(0);
    }

    [Fact]
    public void RemoveItem_WhenNotExists_ShouldFail()
    {
        // Arrange
        var inventory = InventoryEntity.Create(InventoryId.NewId(), capacity: 5).Value;

        // Act
        var result = inventory.RemoveItem(ItemId.NewId());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region IsFull Tests

    [Fact]
    public void IsFull_WhenAtCapacity_ShouldBeTrue()
    {
        // Arrange
        var inventory = InventoryEntity.Create(InventoryId.NewId(), capacity: 2).Value;
        inventory.AddItem(ItemId.NewId());
        inventory.AddItem(ItemId.NewId());

        // Assert
        inventory.IsFull.Should().BeTrue();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        // Arrange
        var inventory = InventoryEntity.Create(InventoryId.NewId(), capacity: 5).Value;
        inventory.AddItem(ItemId.NewId());
        inventory.AddItem(ItemId.NewId());
        inventory.AddItem(ItemId.NewId());

        // Act
        inventory.Clear();

        // Assert
        inventory.Count.Should().Be(0);
        inventory.Items.Should().BeEmpty();
        inventory.IsFull.Should().BeFalse();
    }

    #endregion
}
