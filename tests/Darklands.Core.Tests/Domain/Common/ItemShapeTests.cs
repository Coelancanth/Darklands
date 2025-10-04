using Darklands.Core.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Domain.Common;

[Trait("Category", "Common")]
public class ItemShapeTests
{
    [Fact]
    public void CreateRectangle_ValidDimensions_ShouldCreateAllOccupiedCells()
    {
        // WHY: Rectangle (2×3) should occupy all 6 cells
        // ARCHITECTURE: OccupiedCells is SSOT for collision detection

        var shape = ItemShape.CreateRectangle(width: 2, height: 3);

        shape.IsSuccess.Should().BeTrue();
        shape.Value.Width.Should().Be(2);
        shape.Value.Height.Should().Be(3);
        shape.Value.OccupiedCells.Should().HaveCount(6);
        shape.Value.OccupiedCells.Should().Contain(new GridPosition(0, 0));
        shape.Value.OccupiedCells.Should().Contain(new GridPosition(1, 0));
        shape.Value.OccupiedCells.Should().Contain(new GridPosition(0, 1));
        shape.Value.OccupiedCells.Should().Contain(new GridPosition(1, 1));
        shape.Value.OccupiedCells.Should().Contain(new GridPosition(0, 2));
        shape.Value.OccupiedCells.Should().Contain(new GridPosition(1, 2));
    }

    [Theory]
    [InlineData(0, 1, "Width must be positive")]
    [InlineData(1, 0, "Height must be positive")]
    [InlineData(-1, 2, "Width must be positive")]
    [InlineData(2, -1, "Height must be positive")]
    public void CreateRectangle_InvalidDimensions_ShouldReturnFailure(
        int width, int height, string expectedError)
    {
        // WHY: Prevent invalid bounding boxes

        var shape = ItemShape.CreateRectangle(width, height);

        shape.IsFailure.Should().BeTrue();
        shape.Error.Should().Contain(expectedError);
    }

    [Fact]
    public void CreateFromEncoding_RectangleEncoding_ShouldCreateAllOccupiedCells()
    {
        // WHY: "rect:2x3" is optimized encoding for rectangles
        // Should be equivalent to CreateRectangle(2, 3)

        var shape = ItemShape.CreateFromEncoding("rect:2x3", width: 2, height: 3);

        shape.IsSuccess.Should().BeTrue();
        shape.Value.Width.Should().Be(2);
        shape.Value.Height.Should().Be(3);
        shape.Value.OccupiedCells.Should().HaveCount(6);
    }

    [Fact]
    public void CreateFromEncoding_LShapeEncoding_ShouldCreatePartialCells()
    {
        // WHY: L-shape (2×2 bounding box, 3 occupied cells) from ray_gun test case
        // "custom:0,0;1,0;1,1" means cells at (0,0), (1,0), (1,1) only
        // Cell (0,1) is EMPTY (not occupied)

        var shape = ItemShape.CreateFromEncoding("custom:0,0;1,0;1,1", width: 2, height: 2);

        shape.IsSuccess.Should().BeTrue();
        shape.Value.Width.Should().Be(2);
        shape.Value.Height.Should().Be(2);
        shape.Value.OccupiedCells.Should().HaveCount(3);
        shape.Value.OccupiedCells.Should().Contain(new GridPosition(0, 0));
        shape.Value.OccupiedCells.Should().Contain(new GridPosition(1, 0));
        shape.Value.OccupiedCells.Should().Contain(new GridPosition(1, 1));
        // (0, 1) should NOT be in occupied cells (empty cell in L-shape)
        shape.Value.OccupiedCells.Should().NotContain(new GridPosition(0, 1));
    }

    [Theory]
    [InlineData("", "Encoding cannot be empty")]
    [InlineData("invalid", "Invalid shape encoding format")]
    [InlineData("rect:", "Invalid rectangle encoding")]
    [InlineData("rect:2", "Invalid rectangle encoding")]
    [InlineData("custom:", "Custom encoding requires at least one coordinate")]
    [InlineData("custom:0", "Invalid coordinate format")]
    [InlineData("custom:a,b", "Invalid coordinate format")]
    public void CreateFromEncoding_InvalidEncoding_ShouldReturnFailure(
        string encoding, string expectedError)
    {
        // WHY: Validate encoding format before parsing

        var shape = ItemShape.CreateFromEncoding(encoding, width: 2, height: 2);

        shape.IsFailure.Should().BeTrue();
        shape.Error.Should().Contain(expectedError);
    }

    [Fact]
    public void RotateClockwise_Rectangle2x3_ShouldSwapDimensionsAndTransformCells()
    {
        // WHY: Rotating 2×3 rectangle 90° clockwise → 3×2 rectangle
        // Cell (0,0) → (2,0), cell (1,2) → (0,1), etc.
        // ROTATION MATH: (x,y) in 2×3 → (height-1-y, x) in 3×2

        var original = ItemShape.CreateRectangle(width: 2, height: 3).Value;

        var rotated = original.RotateClockwise();

        rotated.IsSuccess.Should().BeTrue();
        rotated.Value.Width.Should().Be(3);  // Swapped from 2
        rotated.Value.Height.Should().Be(2); // Swapped from 3
        rotated.Value.OccupiedCells.Should().HaveCount(6);

        // Verify cell transformations (sample check)
        rotated.Value.OccupiedCells.Should().Contain(new GridPosition(2, 0)); // (0,0) rotated
        rotated.Value.OccupiedCells.Should().Contain(new GridPosition(0, 1)); // (1,2) rotated
    }

    [Fact]
    public void RotateClockwise_LShape_ShouldTransformOccupiedCellsOnly()
    {
        // WHY: L-shape rotation must only transform the 3 occupied cells
        // Original: (0,0), (1,0), (1,1) in 2×2 box
        // Rotated 90° clockwise: (1,0), (1,1), (0,1) in 2×2 box (still 3 cells!)

        var original = ItemShape.CreateFromEncoding(
            "custom:0,0;1,0;1,1", width: 2, height: 2).Value;

        var rotated = original.RotateClockwise();

        rotated.IsSuccess.Should().BeTrue();
        rotated.Value.Width.Should().Be(2);  // Same (square)
        rotated.Value.Height.Should().Be(2); // Same (square)
        rotated.Value.OccupiedCells.Should().HaveCount(3); // Only 3 cells still!

        // Verify specific rotated positions (L-shape rotated 90° CW)
        rotated.Value.OccupiedCells.Should().Contain(new GridPosition(1, 0));
        rotated.Value.OccupiedCells.Should().Contain(new GridPosition(1, 1));
        rotated.Value.OccupiedCells.Should().Contain(new GridPosition(0, 1));
    }

    [Fact]
    public void RotateClockwise_FourRotations_ShouldReturnToOriginalShape()
    {
        // WHY: 4 × 90° = 360° = identity transformation
        // REGRESSION TEST: Prevents cumulative rounding errors

        var original = ItemShape.CreateFromEncoding(
            "custom:0,0;1,0;1,1", width: 2, height: 2).Value;

        var rotated = original
            .RotateClockwise().Value
            .RotateClockwise().Value
            .RotateClockwise().Value
            .RotateClockwise().Value;

        rotated.Width.Should().Be(original.Width);
        rotated.Height.Should().Be(original.Height);
        rotated.OccupiedCells.Should().BeEquivalentTo(original.OccupiedCells);
    }

    [Fact]
    public void CreateFromEncoding_CellOutsideBoundingBox_ShouldReturnFailure()
    {
        // WHY: Cells must fit within declared Width×Height bounding box
        // SAFETY: Prevent invalid coordinates from TileSet metadata

        var shape = ItemShape.CreateFromEncoding(
            "custom:0,0;2,0", width: 2, height: 2); // X=2 outside width=2

        shape.IsFailure.Should().BeTrue();
        shape.Error.Should().Contain("outside bounding box");
    }

    [Fact]
    public void CreateFromEncoding_NoCells_ShouldReturnFailure()
    {
        // WHY: Shape must occupy at least one cell
        // Items with zero cells cannot be placed

        var shape = ItemShape.CreateFromEncoding(
            "custom:", width: 2, height: 2);

        shape.IsFailure.Should().BeTrue();
        shape.Error.Should().Contain("at least one");
    }
}
