using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application;
using Darklands.Core.Features.Grid.Application.Queries;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;
using Darklands.Core.Features.Grid.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Features.Grid.Application.Queries;

[Trait("Category", "Grid")]
[Trait("Category", "Unit")]
public class CalculateFOVQueryHandlerTests
{
    private readonly ITerrainRepository _terrainRepo;
    private readonly TerrainDefinition _floorTerrain;
    private readonly GridMap _gridMap;
    private readonly IFOVService _mockFOVService;
    private readonly ILogger<CalculateFOVQueryHandler> _mockLogger;
    private readonly CalculateFOVQueryHandler _handler;

    public CalculateFOVQueryHandlerTests()
    {
        _terrainRepo = new StubTerrainRepository();
        _floorTerrain = _terrainRepo.GetDefault().Value;
        _gridMap = new GridMap(_floorTerrain);
        _mockFOVService = Substitute.For<IFOVService>();
        _mockLogger = Substitute.For<ILogger<CalculateFOVQueryHandler>>();
        _handler = new CalculateFOVQueryHandler(_gridMap, _mockFOVService, _mockLogger);
    }

    #region Valid FOV Calculation Tests

    [Fact]
    public async Task Handle_ValidQuery_ShouldDelegateToFOVService()
    {
        // Arrange
        var observer = new Position(10, 10);
        var radius = 5;
        var query = new CalculateFOVQuery(observer, radius);

        var expectedPositions = new HashSet<Position>
        {
            new(10, 10), new(11, 10), new(10, 11)
        };

        _mockFOVService.CalculateFOV(_gridMap, observer, radius)
            .Returns(Result.Success(expectedPositions));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedPositions);
        _mockFOVService.Received(1).CalculateFOV(_gridMap, observer, radius);
    }

    [Fact]
    public async Task Handle_LargeRadius_ShouldSucceed()
    {
        // Arrange
        var observer = new Position(15, 15);
        var radius = 20; // Larger than grid
        var query = new CalculateFOVQuery(observer, radius);

        var expectedPositions = new HashSet<Position> { observer };
        _mockFOVService.CalculateFOV(_gridMap, observer, radius)
            .Returns(Result.Success(expectedPositions));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Invalid Radius Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task Handle_NonPositiveRadius_ShouldReturnFailure(int invalidRadius)
    {
        // WHY: Vision radius must be positive (business rule)

        // Arrange
        var observer = new Position(10, 10);
        var query = new CalculateFOVQuery(observer, invalidRadius);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("must be positive");
        _mockFOVService.DidNotReceive().CalculateFOV(Arg.Any<GridMap>(), Arg.Any<Position>(), Arg.Any<int>());
    }

    #endregion

    #region Out of Bounds Tests

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(30, 0)]
    [InlineData(0, 30)]
    public async Task Handle_ObserverOutOfBounds_ShouldReturnFailure(int x, int y)
    {
        // DOMAIN ERROR: Observer must be within grid bounds

        // Arrange
        var observer = new Position(x, y);
        var query = new CalculateFOVQuery(observer, 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("outside grid bounds");
        _mockFOVService.DidNotReceive().CalculateFOV(Arg.Any<GridMap>(), Arg.Any<Position>(), Arg.Any<int>());
    }

    #endregion

    #region FOV Service Failure Tests

    [Fact]
    public async Task Handle_FOVServiceFailure_ShouldPropagateFailure()
    {
        // RAILWAY-ORIENTED: Service failures propagate through Result<T>

        // Arrange
        var observer = new Position(10, 10);
        var query = new CalculateFOVQuery(observer, 5);

        _mockFOVService.CalculateFOV(_gridMap, observer, 5)
            .Returns(Result.Failure<HashSet<Position>>("FOV calculation error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("FOV calculation error");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_Radius1_ShouldSucceed()
    {
        // EDGE CASE: Minimum valid radius

        // Arrange
        var observer = new Position(10, 10);
        var query = new CalculateFOVQuery(observer, 1);

        var expectedPositions = new HashSet<Position> { observer };
        _mockFOVService.CalculateFOV(_gridMap, observer, 1)
            .Returns(Result.Success(expectedPositions));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CornerPosition_ShouldSucceed()
    {
        // EDGE CASE: Observer at grid corner

        // Arrange
        var observer = new Position(0, 0);
        var query = new CalculateFOVQuery(observer, 5);

        var expectedPositions = new HashSet<Position> { observer, new(1, 0), new(0, 1) };
        _mockFOVService.CalculateFOV(_gridMap, observer, 5)
            .Returns(Result.Success(expectedPositions));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(observer);
    }

    #endregion
}
