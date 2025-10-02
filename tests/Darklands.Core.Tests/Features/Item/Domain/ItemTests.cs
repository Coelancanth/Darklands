using Darklands.Core.Domain.Common;
using FluentAssertions;
using Xunit;
using ItemEntity = Darklands.Core.Features.Item.Domain.Item;

namespace Darklands.Core.Tests.Features.Item.Domain;

[Trait("Category", "Phase1")]
[Trait("Category", "Unit")]
public class ItemTests
{
    #region Create Tests - Happy Path

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var id = ItemId.NewId();

        // Act
        var result = ItemEntity.Create(
            id,
            atlasX: 6,
            atlasY: 0,
            name: "ray_gun",
            type: "weapon",
            width: 4,
            height: 4,
            maxStackSize: 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
        result.Value.AtlasX.Should().Be(6);
        result.Value.AtlasY.Should().Be(0);
        result.Value.Name.Should().Be("ray_gun");
        result.Value.Type.Should().Be("weapon");
        result.Value.Width.Should().Be(4);
        result.Value.Height.Should().Be(4);
        result.Value.MaxStackSize.Should().Be(1);
    }

    [Fact]
    public void Create_WithStackableItem_ShouldSucceed()
    {
        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 2,
            atlasY: 6,
            name: "green_vial",
            type: "item",
            width: 2,
            height: 2,
            maxStackSize: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxStackSize.Should().Be(2);
    }

    #endregion

    #region Create Tests - Atlas Coordinate Validation

    [Fact]
    public void Create_WithNegativeAtlasX_ShouldFail()
    {
        // BUSINESS RULE: Atlas coordinates must be non-negative

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: -1,
            atlasY: 0,
            name: "test_item",
            type: "item",
            width: 1,
            height: 1,
            maxStackSize: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Atlas X").And.Contain("non-negative");
    }

    [Fact]
    public void Create_WithNegativeAtlasY_ShouldFail()
    {
        // BUSINESS RULE: Atlas coordinates must be non-negative

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: -1,
            name: "test_item",
            type: "item",
            width: 1,
            height: 1,
            maxStackSize: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Atlas Y").And.Contain("non-negative");
    }

    [Fact]
    public void Create_WithZeroAtlasCoordinates_ShouldSucceed()
    {
        // EDGE CASE: Zero is valid for atlas coordinates (0,0) is top-left

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: "test_item",
            type: "item",
            width: 1,
            height: 1,
            maxStackSize: 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Create Tests - Name Validation

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        // BUSINESS RULE: Name is required

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: "",
            type: "item",
            width: 1,
            height: 1,
            maxStackSize: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name").And.Contain("empty");
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldFail()
    {
        // BUSINESS RULE: Name cannot be whitespace only

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: "   ",
            type: "item",
            width: 1,
            height: 1,
            maxStackSize: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name").And.Contain("empty");
    }

    #endregion

    #region Create Tests - Type Validation

    [Fact]
    public void Create_WithEmptyType_ShouldFail()
    {
        // BUSINESS RULE: Type is required

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: "test_item",
            type: "",
            width: 1,
            height: 1,
            maxStackSize: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("type").And.Contain("empty");
    }

    #endregion

    #region Create Tests - Dimension Validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WithNonPositiveWidth_ShouldFail(int invalidWidth)
    {
        // BUSINESS RULE: Width must be positive (items occupy space)

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: "test_item",
            type: "item",
            width: invalidWidth,
            height: 1,
            maxStackSize: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Width").And.Contain("positive");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WithNonPositiveHeight_ShouldFail(int invalidHeight)
    {
        // BUSINESS RULE: Height must be positive (items occupy space)

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: "test_item",
            type: "item",
            width: 1,
            height: invalidHeight,
            maxStackSize: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Height").And.Contain("positive");
    }

    [Fact]
    public void Create_WithLargeDimensions_ShouldSucceed()
    {
        // EDGE CASE: Large items like 2x8 baton

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 4,
            atlasY: 0,
            name: "baton",
            type: "weapon",
            width: 2,
            height: 8,
            maxStackSize: 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Width.Should().Be(2);
        result.Value.Height.Should().Be(8);
    }

    #endregion

    #region Create Tests - Stack Size Validation

    [Fact]
    public void Create_WithNegativeMaxStackSize_ShouldFail()
    {
        // BUSINESS RULE: Max stack size must be non-negative

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: "test_item",
            type: "item",
            width: 1,
            height: 1,
            maxStackSize: -1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Max stack size").And.Contain("non-negative");
    }

    [Fact]
    public void Create_WithZeroMaxStackSize_ShouldSucceed()
    {
        // EDGE CASE: Zero max stack size is valid (non-stackable)

        // Act
        var result = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: "unique_item",
            type: "item",
            width: 1,
            height: 1,
            maxStackSize: 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxStackSize.Should().Be(0);
    }

    #endregion

    #region IsStackable Tests

    [Fact]
    public void IsStackable_WhenMaxStackSizeIsZero_ShouldBeFalse()
    {
        // Arrange
        var item = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: "unique_item",
            type: "item",
            width: 1,
            height: 1,
            maxStackSize: 0).Value;

        // Assert
        item.IsStackable.Should().BeFalse();
    }

    [Fact]
    public void IsStackable_WhenMaxStackSizeIsOne_ShouldBeFalse()
    {
        // Arrange
        var item = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 6,
            atlasY: 0,
            name: "ray_gun",
            type: "weapon",
            width: 4,
            height: 4,
            maxStackSize: 1).Value;

        // Assert
        item.IsStackable.Should().BeFalse();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(20)]
    [InlineData(99)]
    public void IsStackable_WhenMaxStackSizeGreaterThanOne_ShouldBeTrue(int maxStackSize)
    {
        // Arrange
        var item = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 2,
            atlasY: 6,
            name: "vial",
            type: "item",
            width: 2,
            height: 2,
            maxStackSize: maxStackSize).Value;

        // Assert
        item.IsStackable.Should().BeTrue();
    }

    #endregion
}
