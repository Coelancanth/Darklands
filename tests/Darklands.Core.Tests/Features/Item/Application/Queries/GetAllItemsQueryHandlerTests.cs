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
public class GetAllItemsQueryHandlerTests
{
    private readonly IItemRepository _mockRepository;
    private readonly ILogger<GetAllItemsQueryHandler> _mockLogger;
    private readonly GetAllItemsQueryHandler _handler;

    public GetAllItemsQueryHandlerTests()
    {
        _mockRepository = Substitute.For<IItemRepository>();
        _mockLogger = Substitute.For<ILogger<GetAllItemsQueryHandler>>();
        _handler = new GetAllItemsQueryHandler(_mockRepository, _mockLogger);
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_WithItemsInCatalog_ShouldReturnAllItemsAsDtos()
    {
        // Arrange
        var item1 = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 6,
            atlasY: 0,
            name: "ray_gun",
            type: "weapon",
            spriteWidth: 4,
            spriteHeight: 4,
            inventoryWidth: 1,
            inventoryHeight: 1,
            maxStackSize: 1).Value;

        var item2 = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 2,
            atlasY: 6,
            name: "green_vial",
            type: "item",
            spriteWidth: 2,
            spriteHeight: 2,
            inventoryWidth: 1,
            inventoryHeight: 1,
            maxStackSize: 5).Value;

        var items = new List<ItemEntity> { item1, item2 };

        _mockRepository.GetAll().Returns(Result.Success(items));

        var query = new GetAllItemsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        // Verify DTO mapping for item1
        var dto1 = result.Value.First(dto => dto.Name == "ray_gun");
        dto1.Id.Should().Be(item1.Id);
        dto1.AtlasX.Should().Be(6);
        dto1.AtlasY.Should().Be(0);
        dto1.Name.Should().Be("ray_gun");
        dto1.Type.Should().Be("weapon");
        dto1.SpriteWidth.Should().Be(4);
        dto1.SpriteHeight.Should().Be(4);
        dto1.MaxStackSize.Should().Be(1);
        dto1.IsStackable.Should().BeFalse();

        // Verify DTO mapping for item2
        var dto2 = result.Value.First(dto => dto.Name == "green_vial");
        dto2.Id.Should().Be(item2.Id);
        dto2.MaxStackSize.Should().Be(5);
        dto2.IsStackable.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptyCatalog_ShouldReturnEmptyList()
    {
        // EDGE CASE: Catalog not yet loaded or no items defined

        // Arrange
        _mockRepository.GetAll().Returns(Result.Success(new List<ItemEntity>()));

        var query = new GetAllItemsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Repository Failure Tests

    [Fact]
    public async Task Handle_RepositoryFailure_ShouldReturnFailure()
    {
        // RAILWAY-ORIENTED: Repository failure propagates

        // Arrange
        _mockRepository.GetAll()
            .Returns(Result.Failure<List<ItemEntity>>("TileSet not loaded"));

        var query = new GetAllItemsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("TileSet not loaded");
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public async Task Handle_ShouldMapAllEntityPropertiesToDto()
    {
        // WHY: Verify DTO contract matches domain entity

        // Arrange
        var itemId = ItemId.NewId();
        var item = ItemEntity.Create(
            itemId,
            atlasX: 0,
            atlasY: 4,
            name: "dagger",
            type: "weapon",
            spriteWidth: 4,
            spriteHeight: 2,
            inventoryWidth: 1,
            inventoryHeight: 1,
            maxStackSize: 1).Value;

        _mockRepository.GetAll().Returns(Result.Success(new List<ItemEntity> { item }));

        var query = new GetAllItemsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Single();

        // Verify ALL properties mapped correctly
        dto.Id.Should().Be(itemId);
        dto.AtlasX.Should().Be(0);
        dto.AtlasY.Should().Be(4);
        dto.Name.Should().Be("dagger");
        dto.Type.Should().Be("weapon");
        dto.SpriteWidth.Should().Be(4);
        dto.SpriteHeight.Should().Be(2);
        dto.MaxStackSize.Should().Be(1);
        dto.IsStackable.Should().BeFalse(); // Computed property preserved
    }

    [Fact]
    public async Task Handle_StackableItem_ShouldMapIsStackableTrue()
    {
        // Arrange
        var item = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 6,
            atlasY: 0,
            name: "red_vial",
            type: "item",
            spriteWidth: 2,
            spriteHeight: 2,
            inventoryWidth: 1,
            inventoryHeight: 1,
            maxStackSize: 10).Value;

        _mockRepository.GetAll().Returns(Result.Success(new List<ItemEntity> { item }));

        var query = new GetAllItemsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Single();
        dto.MaxStackSize.Should().Be(10);
        dto.IsStackable.Should().BeTrue(); // Computed from MaxStackSize > 1
    }

    #endregion
}
