using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Commands;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Features.Grid.Application.Commands;

[Trait("Category", "Phase2")]
[Trait("Category", "Unit")]
public class MoveActorCommandHandlerTests
{
    private readonly GridMap _gridMap;
    private readonly IActorPositionService _mockPositionService;
    private readonly ILogger<MoveActorCommandHandler> _mockLogger;
    private readonly MoveActorCommandHandler _handler;

    public MoveActorCommandHandlerTests()
    {
        _gridMap = new GridMap(); // Real GridMap (pure domain, no mocking needed)
        _mockPositionService = Substitute.For<IActorPositionService>();
        _mockLogger = Substitute.For<ILogger<MoveActorCommandHandler>>();
        _handler = new MoveActorCommandHandler(_gridMap, _mockPositionService, _mockLogger);
    }

    #region Valid Movement Tests

    [Fact]
    public async Task Handle_ValidMoveToFloor_ShouldUpdatePositionAndReturnSuccess()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var targetPos = new Position(10, 10); // Default GridMap is all Floor
        var command = new MoveActorCommand(actorId, targetPos);

        _mockPositionService.SetPosition(actorId, targetPos).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockPositionService.Received(1).SetPosition(actorId, targetPos);
    }

    [Fact]
    public async Task Handle_ValidMoveToSmokeTerrain_ShouldSucceed()
    {
        // WHY: Smoke is passable (even though it blocks vision)

        // Arrange
        var actorId = ActorId.NewId();
        var smokePos = new Position(5, 5);
        _gridMap.SetTerrain(smokePos, TerrainType.Smoke);
        var command = new MoveActorCommand(actorId, smokePos);

        _mockPositionService.SetPosition(actorId, smokePos).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockPositionService.Received(1).SetPosition(actorId, smokePos);
    }

    #endregion

    #region Impassable Terrain Tests

    [Fact]
    public async Task Handle_MoveToWall_ShouldReturnFailure()
    {
        // WHY: Walls block movement (impassable terrain)

        // Arrange
        var actorId = ActorId.NewId();
        var wallPos = new Position(5, 5);
        _gridMap.SetTerrain(wallPos, TerrainType.Wall);
        var command = new MoveActorCommand(actorId, wallPos);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("impassable");
        _mockPositionService.DidNotReceive().SetPosition(Arg.Any<ActorId>(), Arg.Any<Position>());
    }

    #endregion

    #region Out of Bounds Tests

    [Fact]
    public async Task Handle_MoveOutOfBounds_ShouldReturnFailure()
    {
        // DOMAIN ERROR: GridMap enforces 30x30 bounds

        // Arrange
        var actorId = ActorId.NewId();
        var outOfBoundsPos = new Position(-1, 5);
        var command = new MoveActorCommand(actorId, outOfBoundsPos);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("outside grid bounds");
        _mockPositionService.DidNotReceive().SetPosition(Arg.Any<ActorId>(), Arg.Any<Position>());
    }

    [Theory]
    [InlineData(30, 0)]   // X beyond bounds
    [InlineData(0, 30)]   // Y beyond bounds
    [InlineData(-1, -1)]  // Both negative
    public async Task Handle_VariousOutOfBoundsPositions_ShouldReturnFailure(int x, int y)
    {
        // Arrange
        var actorId = ActorId.NewId();
        var invalidPos = new Position(x, y);
        var command = new MoveActorCommand(actorId, invalidPos);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        _mockPositionService.DidNotReceive().SetPosition(Arg.Any<ActorId>(), Arg.Any<Position>());
    }

    #endregion

    #region Service Failure Tests

    [Fact]
    public async Task Handle_PositionServiceFailure_ShouldPropagateFailure()
    {
        // RAILWAY-ORIENTED: Service failures propagate through Result<T>

        // Arrange
        var actorId = ActorId.NewId();
        var targetPos = new Position(10, 10);
        var command = new MoveActorCommand(actorId, targetPos);

        _mockPositionService.SetPosition(actorId, targetPos).Returns(Result.Failure("Actor not found"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Actor not found");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_MoveTo00Corner_ShouldSucceed()
    {
        // EDGE CASE: (0, 0) is valid top-left corner

        // Arrange
        var actorId = ActorId.NewId();
        var cornerPos = new Position(0, 0);
        var command = new MoveActorCommand(actorId, cornerPos);

        _mockPositionService.SetPosition(actorId, cornerPos).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MoveTo2929Corner_ShouldSucceed()
    {
        // EDGE CASE: (29, 29) is valid bottom-right corner

        // Arrange
        var actorId = ActorId.NewId();
        var cornerPos = new Position(29, 29);
        var command = new MoveActorCommand(actorId, cornerPos);

        _mockPositionService.SetPosition(actorId, cornerPos).Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
