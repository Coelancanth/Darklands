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
public class GetItemsByTypeQueryHandlerTests
{
    private readonly IItemRepository _mockRepository;
    private readonly ILogger<GetItemsByTypeQueryHandler> _mockLogger;
    private readonly GetItemsByTypeQueryHandler _handler;

    public GetItemsByTypeQueryHandlerTests()
    {
        _mockRepository = Substitute.For<IItemRepository>();
        _mockLogger = Substitute.For<ILogger<GetItemsByTypeQueryHandler>>();
        _handler = new GetItemsByTypeQueryHandler(_mockRepository, _mockLogger);
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_WeaponTypeFilter_ShouldReturnOnlyWeapons()
    {
        // Arrange
        var weapon1 = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 6,
            atlasY: 0,
            name: "ray_gun",
            type: "weapon",
            width: 4,
            height: 4,
            maxStackSize: 1).Value;

        var weapon2 = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 4,
            atlasY: 0,
            name: "baton",
            type: "weapon",
            width: 2,
            height: 8,
            maxStackSize: 1).Value;

        var weapons = new List<ItemEntity> { weapon1, weapon2 };

        _mockRepository.GetByType("weapon").Returns(Result.Success(weapons));

        var query = new GetItemsByTypeQuery("weapon");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(dto => dto.Type.Should().Be("weapon"));
        result.Value.Should().Contain(dto => dto.Name == "ray_gun");
        result.Value.Should().Contain(dto => dto.Name == "baton");
    }

    [Fact]
    public async Task Handle_ItemTypeFilter_ShouldReturnOnlyItems()
    {
        // Arrange
        var item1 = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 2,
            atlasY: 6,
            name: "green_vial",
            type: "item",
            width: 2,
            height: 2,
            maxStackSize: 5).Value;

        var items = new List<ItemEntity> { item1 };

        _mockRepository.GetByType("item").Returns(Result.Success(items));

        var query = new GetItemsByTypeQuery("item");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Single().Type.Should().Be("item");
        result.Value.Single().Name.Should().Be("green_vial");
    }

    [Fact]
    public async Task Handle_NoMatchingType_ShouldReturnEmptyList()
    {
        // EDGE CASE: Filter for type that doesn't exist

        // Arrange
        _mockRepository.GetByType("nonexistent_type")
            .Returns(Result.Success(new List<ItemEntity>()));

        var query = new GetItemsByTypeQuery("nonexistent_type");

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
        _mockRepository.GetByType("weapon")
            .Returns(Result.Failure<List<ItemEntity>>("Catalog error"));

        var query = new GetItemsByTypeQuery("weapon");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Catalog error");
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public async Task Handle_ShouldMapAllPropertiesCorrectly()
    {
        // WHY: Verify type filtering doesn't corrupt DTO mapping

        // Arrange
        var weapon = ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 4,
            name: "dagger",
            type: "weapon",
            width: 4,
            height: 2,
            maxStackSize: 1).Value;

        _mockRepository.GetByType("weapon")
            .Returns(Result.Success(new List<ItemEntity> { weapon }));

        var query = new GetItemsByTypeQuery("weapon");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Single();

        dto.Id.Should().Be(weapon.Id);
        dto.AtlasX.Should().Be(0);
        dto.AtlasY.Should().Be(4);
        dto.Name.Should().Be("dagger");
        dto.Type.Should().Be("weapon");
        dto.Width.Should().Be(4);
        dto.Height.Should().Be(2);
        dto.MaxStackSize.Should().Be(1);
        dto.IsStackable.Should().BeFalse();
    }

    #endregion
}
