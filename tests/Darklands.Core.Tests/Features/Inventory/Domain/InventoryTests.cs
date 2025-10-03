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

    #region Spatial Inventory Tests (VS_018 Phase 1)

    [Fact]
    public void Create_WithGridDimensions_ShouldSucceed()
    {
        // Arrange & Act
        var result = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 10,
            gridHeight: 5,
            ContainerType.General);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GridWidth.Should().Be(10);
        result.Value.GridHeight.Should().Be(5);
        result.Value.Capacity.Should().Be(50); // Width × Height
        result.Value.ContainerType.Should().Be(ContainerType.General);
        result.Value.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(20, 5, 4)]   // 20 → 5×4 grid
    [InlineData(100, 10, 10)] // 100 → 10×10 grid
    [InlineData(30, 6, 5)]    // 30 → 6×5 grid
    public void Create_WithCapacity_ShouldMapToGridDimensions(
        int capacity,
        int expectedWidth,
        int expectedHeight)
    {
        // BACKWARD COMPATIBILITY: Old Create(capacity) signature maps to grid

        // Act
        var result = InventoryEntity.Create(InventoryId.NewId(), capacity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GridWidth.Should().Be(expectedWidth);
        result.Value.GridHeight.Should().Be(expectedHeight);
        result.Value.Capacity.Should().Be(capacity);
        result.Value.ContainerType.Should().Be(ContainerType.General);
    }

    [Fact]
    public void PlaceItemAt_ValidPosition_ShouldSucceed()
    {
        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();
        var position = new GridPosition(2, 3);

        // Act
        var result = inventory.PlaceItemAt(itemId, position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inventory.Contains(itemId).Should().BeTrue();
        inventory.GetItemPosition(itemId).Value.Should().Be(position);
        inventory.Count.Should().Be(1);
    }

    [Fact]
    public void PlaceItemAt_OutOfBounds_ShouldFail()
    {
        // BUSINESS RULE: Cannot place items outside grid boundaries

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();

        // Act
        var result = inventory.PlaceItemAt(itemId, new GridPosition(10, 3));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceeds grid bounds");
    }

    [Fact]
    public void PlaceItemAt_OccupiedPosition_ShouldFail()
    {
        // BUSINESS RULE: Cannot place item where another item exists

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var position = new GridPosition(2, 2);
        inventory.PlaceItemAt(ItemId.NewId(), position);

        // Act
        var result = inventory.PlaceItemAt(ItemId.NewId(), position);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("overlaps with existing item");
    }

    [Fact]
    public void CanPlaceAt_FreePosition_ShouldReturnTrue()
    {
        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;

        // Act
        var canPlace = inventory.CanPlaceAt(new GridPosition(2, 2));

        // Assert
        canPlace.Should().BeTrue();
    }

    [Fact]
    public void CanPlaceAt_OccupiedPosition_ShouldReturnFalse()
    {
        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var position = new GridPosition(2, 2);
        inventory.PlaceItemAt(ItemId.NewId(), position);

        // Act
        var canPlace = inventory.CanPlaceAt(position);

        // Assert
        canPlace.Should().BeFalse();
    }

    [Fact]
    public void AddItem_GeneralContainer_ShouldAutoPlaceAtFirstFreePosition()
    {
        // BACKWARD COMPATIBILITY: AddItem still works for General containers

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 3,
            gridHeight: 3,
            ContainerType.General).Value;
        var itemId1 = ItemId.NewId();
        var itemId2 = ItemId.NewId();

        // Act
        inventory.AddItem(itemId1);
        inventory.AddItem(itemId2);

        // Assert
        inventory.Contains(itemId1).Should().BeTrue();
        inventory.Contains(itemId2).Should().BeTrue();
        inventory.GetItemPosition(itemId1).Value.Should().Be(new GridPosition(0, 0)); // Top-left
        inventory.GetItemPosition(itemId2).Value.Should().Be(new GridPosition(1, 0)); // Next position
    }

    [Fact]
    public void GetItemAtPosition_WithItem_ShouldReturnItemId()
    {
        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();
        var position = new GridPosition(3, 2);
        inventory.PlaceItemAt(itemId, position);

        // Act
        var result = inventory.GetItemAtPosition(position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(itemId);
    }

    [Fact]
    public void GetItemAtPosition_EmptyPosition_ShouldFail()
    {
        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;

        // Act
        var result = inventory.GetItemAtPosition(new GridPosition(3, 2));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No item");
    }

    [Fact]
    public void RemoveItem_SpatialInventory_ShouldRemoveItemAndPosition()
    {
        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();
        var position = new GridPosition(2, 2);
        inventory.PlaceItemAt(itemId, position);

        // Act
        var result = inventory.RemoveItem(itemId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inventory.Contains(itemId).Should().BeFalse();
        inventory.CanPlaceAt(position).Should().BeTrue(); // Position now free
    }

    #endregion
}
