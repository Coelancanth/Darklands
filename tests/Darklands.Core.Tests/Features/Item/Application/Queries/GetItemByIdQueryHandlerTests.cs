using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using Darklands.Core.Features.Item.Application.Queries;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using ItemEntity = Darklands.Core.Features.Item.Domain.Item;

namespace Darklands.Core.Tests.Features.Item.Application.Queries;

[Trait("Category", "Phase2")]
[Trait("Category", "Unit")]
public class GetItemByIdQueryHandlerTests
{
    private readonly IItemRepository _mockRepository;
    private readonly ILogger<GetItemByIdQueryHandler> _mockLogger;
    private readonly GetItemByIdQueryHandler _handler;

    public GetItemByIdQueryHandlerTests()
    {
        _mockRepository = Substitute.For<IItemRepository>();
        _mockLogger = Substitute.For<ILogger<GetItemByIdQueryHandler>>();
        _handler = new GetItemByIdQueryHandler(_mockRepository, _mockLogger);
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_ItemExists_ShouldReturnItemDto()
    {
        // Arrange
        var itemId = ItemId.NewId();
        var item = ItemEntity.Create(
            itemId,
            atlasX: 4,
            atlasY: 0,
            name: "baton",
            type: "weapon",
            spriteWidth: 2,
            spriteHeight: 8,
            inventoryWidth: 1,
            inventoryHeight: 1,
            maxStackSize: 1).Value;

        _mockRepository.GetById(itemId).Returns(Result.Success(item));

        var query = new GetItemByIdQuery(itemId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;

        dto.Id.Should().Be(itemId);
        dto.AtlasX.Should().Be(4);
        dto.AtlasY.Should().Be(0);
        dto.Name.Should().Be("baton");
        dto.Type.Should().Be("weapon");
        dto.SpriteWidth.Should().Be(2);
        dto.SpriteHeight.Should().Be(8);
        dto.MaxStackSize.Should().Be(1);
        dto.IsStackable.Should().BeFalse();
    }

    #endregion

    #region Item Not Found Tests

    [Fact]
    public async Task Handle_ItemNotFound_ShouldReturnFailure()
    {
        // BUSINESS RULE: Invalid item IDs return failure

        // Arrange
        var itemId = ItemId.NewId();

        _mockRepository.GetById(itemId)
            .Returns(Result.Failure<ItemEntity>("Item not found"));

        var query = new GetItemByIdQuery(itemId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Item not found");
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public async Task Handle_StackableItem_ShouldPreserveIsStackableProperty()
    {
        // WHY: Verify computed property IsStackable survives DTO mapping

        // Arrange
        var itemId = ItemId.NewId();
        var item = ItemEntity.Create(
            itemId,
            atlasX: 6,
            atlasY: 2,
            name: "cell_background",
            type: "UI",
            spriteWidth: 2,
            spriteHeight: 2,
            inventoryWidth: 1,
            inventoryHeight: 1,
            maxStackSize: 20).Value;

        _mockRepository.GetById(itemId).Returns(Result.Success(item));

        var query = new GetItemByIdQuery(itemId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxStackSize.Should().Be(20);
        result.Value.IsStackable.Should().BeTrue();
    }

    #endregion
}
