using Darklands.Core.Domain.Common;
using FluentAssertions;
using Xunit;
using InventoryEntity = Darklands.Core.Features.Inventory.Domain.Inventory;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Tests.Features.Inventory.Domain;

[Trait("Category", "Inventory")]
[Trait("Category", "Integration")]
public class InventoryLShapeTests
{
    [Fact]
    public void PlaceItemAt_LShapeItem_ShouldOnlyOccupyThreeCells()
    {
        // WHY: L-shape (2×2 bounding box, 3 occupied cells) should NOT block cell (0,1)
        // REGRESSION: Old AABB collision would block all 4 cells in bounding box

        // Arrange: 5×5 inventory
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;

        var lShapeItem = ItemId.NewId();
        var otherItem = ItemId.NewId();

        // L-shape: 2×2 bounding box, occupies cells (0,0), (1,0), (1,1)
        // Cell (0,1) is EMPTY (the "notch" of the L)
        var lShape = ItemShape.CreateFromEncoding("custom:0,0;1,0;1,1", width: 2, height: 2).Value;

        // Act: Place L-shape at (0,0) using new shape-based API
        var placeLShapeResult = inventory.PlaceItemAt(
            lShapeItem,
            new GridPosition(0, 0),
            lShape,
            Rotation.Degrees0);

        // Assert: L-shape placed successfully
        placeLShapeResult.IsSuccess.Should().BeTrue();

        // Act: Try to place another 1×1 item at cell (0,1) - the EMPTY cell in L-shape
        var placeAtEmptyCellResult = inventory.PlaceItemAt(
            otherItem,
            new GridPosition(0, 1),
            width: 1,
            height: 1,
            rotation: Rotation.Degrees0);

        // Assert: Should SUCCEED because cell (0,1) is NOT occupied by L-shape
        placeAtEmptyCellResult.IsSuccess.Should().BeTrue(
            "Cell (0,1) is the empty notch of the L-shape and should be free for placement");
    }

    [Fact]
    public void PlaceItemAt_LShapeItem_ShouldBlockOccupiedCells()
    {
        // WHY: Verify L-shape DOES block its 3 occupied cells (not testing the empty cell)

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;

        var lShapeItem = ItemId.NewId();
        var item1 = ItemId.NewId();
        var item2 = ItemId.NewId();
        var item3 = ItemId.NewId();

        var lShape = ItemShape.CreateFromEncoding("custom:0,0;1,0;1,1", width: 2, height: 2).Value;

        // Act: Place L-shape at (0,0)
        inventory.PlaceItemAt(
            lShapeItem,
            new GridPosition(0, 0),
            lShape,
            Rotation.Degrees0);

        // Try to place items at the 3 occupied cells
        var placeAt00 = inventory.PlaceItemAt(item1, new GridPosition(0, 0), 1, 1, Rotation.Degrees0);
        var placeAt10 = inventory.PlaceItemAt(item2, new GridPosition(1, 0), 1, 1, Rotation.Degrees0);
        var placeAt11 = inventory.PlaceItemAt(item3, new GridPosition(1, 1), 1, 1, Rotation.Degrees0);

        // Assert: All 3 occupied cells should be blocked
        placeAt00.IsFailure.Should().BeTrue("Cell (0,0) is occupied by L-shape");
        placeAt10.IsFailure.Should().BeTrue("Cell (1,0) is occupied by L-shape");
        placeAt11.IsFailure.Should().BeTrue("Cell (1,1) is occupied by L-shape");
    }

    [Fact]
    public void PlaceItemAt_RotatedLShape_ShouldTransformOccupiedCells()
    {
        // WHY: Rotating L-shape 90° clockwise should transform occupied cells correctly
        // Original L: (0,0), (1,0), (1,1)
        // Rotated 90° CW: (1,0), (1,1), (0,1)

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 5,
            gridHeight: 5,
            ContainerType.General).Value;

        var lShapeItem = ItemId.NewId();
        var testItem = ItemId.NewId();

        var lShape = ItemShape.CreateFromEncoding("custom:0,0;1,0;1,1", width: 2, height: 2).Value;

        // Act: Place L-shape at (0,0) with 90° rotation
        var placeResult = inventory.PlaceItemAt(
            lShapeItem,
            new GridPosition(0, 0),
            lShape,
            Rotation.Degrees90);

        placeResult.IsSuccess.Should().BeTrue();

        // Rotated L-shape now occupies: (1,0), (1,1), (0,1)
        // Cell (0,0) should now be EMPTY (was occupied in unrotated L)
        var placeAtRotatedEmpty = inventory.PlaceItemAt(
            testItem,
            new GridPosition(0, 0),
            width: 1,
            height: 1,
            rotation: Rotation.Degrees0);

        // Assert: Cell (0,0) should be free after rotation
        placeAtRotatedEmpty.IsSuccess.Should().BeTrue(
            "Cell (0,0) is empty after L-shape rotated 90° clockwise");
    }

    [Fact]
    public void PlaceItemAt_MultipleItemsFillingLShapeNotch_ShouldSucceed()
    {
        // WHY: Real-world scenario - tetris-style packing around L-shape
        // Place L-shape, then fill its empty notch with multiple small items

        // Arrange
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 3,
            gridHeight: 3,
            ContainerType.General).Value;

        var lShapeId = ItemId.NewId();
        var filler = ItemId.NewId();

        var lShape = ItemShape.CreateFromEncoding("custom:0,0;1,0;1,1", width: 2, height: 2).Value;

        // Act: Place L-shape at (0,0)
        inventory.PlaceItemAt(lShapeId, new GridPosition(0, 0), lShape, Rotation.Degrees0);

        // Place 1×1 item in the notch
        var fillNotch = inventory.PlaceItemAt(filler, new GridPosition(0, 1), width: 1, height: 1, Rotation.Degrees0);

        // Assert: Efficient packing achieved
        fillNotch.IsSuccess.Should().BeTrue(
            "Player should be able to tetris-pack items into L-shape notches");
    }

    [Fact]
    public void PlaceItemAt_LShapeOutOfBounds_ShouldFail()
    {
        // WHY: Bounds checking must consider ALL occupied cells, not just bounding box

        // Arrange: 3×3 inventory (small)
        var inventory = InventoryEntity.Create(
            InventoryId.NewId(),
            gridWidth: 3,
            gridHeight: 3,
            ContainerType.General).Value;

        var lShapeId = ItemId.NewId();

        var lShape = ItemShape.CreateFromEncoding("custom:0,0;1,0;1,1", width: 2, height: 2).Value;

        // Act: Try to place L-shape at (2,2) - would put cell (3,2) or (3,3) out of bounds
        var placeResult = inventory.PlaceItemAt(
            lShapeId,
            new GridPosition(2, 2),
            lShape,
            Rotation.Degrees0);

        // Assert: Should fail due to out-of-bounds occupied cells
        placeResult.IsFailure.Should().BeTrue();
        placeResult.Error.Should().Contain("exceeds grid bounds");
    }
}
