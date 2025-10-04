using Darklands.Core.Domain.Common;
using FluentAssertions;
using Xunit;
using InventoryEntity = Darklands.Core.Features.Inventory.Domain.Inventory;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Tests.Features.Inventory.Domain;

[Trait("Category", "Inventory")]
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

    #region RotateItem Tests (Phase 3)

    [Fact]
    public void RotateItem_FromDegrees0ToDegrees90_ShouldSucceedWhenSpaceAvailable()
    {
        // WHY: Rotating 2×1 sword to 1×2 requires vertical space

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();
        var position = new GridPosition(0, 0);
        inventory.PlaceItemAt(itemId, position, width: 2, height: 1, rotation: Rotation.Degrees0);

        // Act - Rotate 90° (2×1 becomes 1×2)
        var result = inventory.RotateItem(itemId, Rotation.Degrees90);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inventory.GetItemRotation(itemId).Value.Should().Be(Rotation.Degrees90);
    }

    [Fact]
    public void RotateItem_WhenWouldExceedBounds_ShouldFail()
    {
        // WHY: 2×1 item at right edge cannot rotate to 1×2 (would exceed grid width)

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 3,
            gridHeight: 5,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();
        var position = new GridPosition(2, 0); // Right edge (X=2, width=3)
        inventory.PlaceItemAt(itemId, position, width: 1, height: 2, rotation: Rotation.Degrees0);

        // Act - Rotating 90° would make it 2×1, exceeding X=2+2=4 > gridWidth=3
        var result = inventory.RotateItem(itemId, Rotation.Degrees90);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceed grid bounds");
        inventory.GetItemRotation(itemId).Value.Should().Be(Rotation.Degrees0); // Unchanged
    }

    [Fact]
    public void RotateItem_WhenWouldCollideWithOtherItem_ShouldFail()
    {
        // WHY: Cannot rotate if new orientation overlaps with existing item

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var sword = ItemId.NewId();
        var potion = ItemId.NewId();

        // Place sword (2×1) at (0,0)
        inventory.PlaceItemAt(sword, new GridPosition(0, 0), width: 2, height: 1);

        // Place potion (1×1) at (0,1) - directly below sword
        inventory.PlaceItemAt(potion, new GridPosition(0, 1), width: 1, height: 1);

        // Act - Rotating sword 90° would make it 1×2, colliding with potion at (0,1)
        var result = inventory.RotateItem(sword, Rotation.Degrees90);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("overlap");
    }

    [Fact]
    public void RotateItem_FullCircle_ShouldReturnToOriginalOrientation()
    {
        // WHY: Rotating 0→90→180→270→0 should work seamlessly

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();
        inventory.PlaceItemAt(itemId, new GridPosition(1, 1), width: 2, height: 2); // Square item

        // Act & Assert - Rotate through all orientations
        inventory.RotateItem(itemId, Rotation.Degrees90).IsSuccess.Should().BeTrue();
        inventory.GetItemRotation(itemId).Value.Should().Be(Rotation.Degrees90);

        inventory.RotateItem(itemId, Rotation.Degrees180).IsSuccess.Should().BeTrue();
        inventory.GetItemRotation(itemId).Value.Should().Be(Rotation.Degrees180);

        inventory.RotateItem(itemId, Rotation.Degrees270).IsSuccess.Should().BeTrue();
        inventory.GetItemRotation(itemId).Value.Should().Be(Rotation.Degrees270);

        inventory.RotateItem(itemId, Rotation.Degrees0).IsSuccess.Should().BeTrue();
        inventory.GetItemRotation(itemId).Value.Should().Be(Rotation.Degrees0);
    }

    [Fact]
    public void RotateItem_NonExistentItem_ShouldFail()
    {
        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var nonExistentItem = ItemId.NewId();

        // Act
        var result = inventory.RotateItem(nonExistentItem, Rotation.Degrees90);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void GetItemRotation_DefaultsToDegrees0_ForItemPlacedWithoutRotation()
    {
        // WHY: Backward compatibility - items placed before Phase 3 should default to 0°

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();
        inventory.PlaceItemAt(itemId, new GridPosition(0, 0), width: 2, height: 1); // No rotation param

        // Act
        var rotationResult = inventory.GetItemRotation(itemId);

        // Assert
        rotationResult.IsSuccess.Should().BeTrue();
        rotationResult.Value.Should().Be(Rotation.Degrees0); // Default
    }

    [Fact]
    public void PlaceItemAt_WithRotation_StoresRotationState()
    {
        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();

        // Act - Place item with 90° rotation
        var placeResult = inventory.PlaceItemAt(
            itemId,
            new GridPosition(0, 0),
            width: 2,
            height: 1,
            rotation: Rotation.Degrees90);

        // Assert
        placeResult.IsSuccess.Should().BeTrue();
        inventory.GetItemRotation(itemId).Value.Should().Be(Rotation.Degrees90);
    }

    [Fact]
    public void PlaceItemAt_WithRotation_UsesRotatedDimensionsForCollision()
    {
        // WHY: Placing 2×1 item rotated 90° should occupy 1×2 space

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;
        var sword = ItemId.NewId();
        var potion = ItemId.NewId();

        // Place sword (2×1 rotated 90° = 1×2 effective) at (0,0)
        inventory.PlaceItemAt(sword, new GridPosition(0, 0), width: 2, height: 1, rotation: Rotation.Degrees90);

        // Act - Try to place potion at (0,1) - should collide with rotated sword
        var result = inventory.PlaceItemAt(potion, new GridPosition(0, 1), width: 1, height: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("overlap");
    }

    [Fact]
    public void RotateItem_Degrees180_ShouldNotChangeDimensions()
    {
        // WHY: 180° rotation keeps same effective dimensions (2×1 stays 2×1)

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 3,
            gridHeight: 2,
            ContainerType.General).Value;
        var itemId = ItemId.NewId();
        inventory.PlaceItemAt(itemId, new GridPosition(0, 0), width: 2, height: 1);

        // Act - Rotate 180° (dimensions unchanged, no collision)
        var result = inventory.RotateItem(itemId, Rotation.Degrees180);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inventory.GetItemRotation(itemId).Value.Should().Be(Rotation.Degrees180);
    }

    #endregion

    #region RotationHelper Tests (Phase 3)

    [Theory]
    [InlineData(2, 1, Rotation.Degrees0, 2, 1)]   // No rotation
    [InlineData(2, 1, Rotation.Degrees90, 1, 2)]  // Swapped
    [InlineData(2, 1, Rotation.Degrees180, 2, 1)] // No swap
    [InlineData(2, 1, Rotation.Degrees270, 1, 2)] // Swapped
    public void RotationHelper_GetRotatedDimensions_ReturnsCorrectEffectiveDimensions(
        int baseWidth,
        int baseHeight,
        Rotation rotation,
        int expectedWidth,
        int expectedHeight)
    {
        // WHY: Dimension swapping is core to rotation logic

        // Act
        var (actualWidth, actualHeight) = RotationHelper.GetRotatedDimensions(baseWidth, baseHeight, rotation);

        // Assert
        actualWidth.Should().Be(expectedWidth);
        actualHeight.Should().Be(expectedHeight);
    }

    [Fact]
    public void RotationHelper_RotateClockwise_CyclesThroughAllOrientations()
    {
        // Act & Assert
        RotationHelper.RotateClockwise(Rotation.Degrees0).Should().Be(Rotation.Degrees90);
        RotationHelper.RotateClockwise(Rotation.Degrees90).Should().Be(Rotation.Degrees180);
        RotationHelper.RotateClockwise(Rotation.Degrees180).Should().Be(Rotation.Degrees270);
        RotationHelper.RotateClockwise(Rotation.Degrees270).Should().Be(Rotation.Degrees0); // Full circle
    }

    [Fact]
    public void RotationHelper_RotateCounterClockwise_CyclesThroughAllOrientations()
    {
        // Act & Assert
        RotationHelper.RotateCounterClockwise(Rotation.Degrees0).Should().Be(Rotation.Degrees270);
        RotationHelper.RotateCounterClockwise(Rotation.Degrees270).Should().Be(Rotation.Degrees180);
        RotationHelper.RotateCounterClockwise(Rotation.Degrees180).Should().Be(Rotation.Degrees90);
        RotationHelper.RotateCounterClockwise(Rotation.Degrees90).Should().Be(Rotation.Degrees0); // Full circle
    }

    [Theory]
    [InlineData(Rotation.Degrees0, 0f)]
    [InlineData(Rotation.Degrees90, 1.5707964f)] // π/2
    [InlineData(Rotation.Degrees180, 3.1415927f)] // π
    [InlineData(Rotation.Degrees270, 4.712389f)] // 3π/2
    public void RotationHelper_ToRadians_ConvertsCorrectly(Rotation rotation, float expectedRadians)
    {
        // WHY: Godot uses radians for rotation transforms

        // Act
        var radians = RotationHelper.ToRadians(rotation);

        // Assert
        radians.Should().BeApproximately(expectedRadians, precision: 0.0001f);
    }

    #endregion
}
