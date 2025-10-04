using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Movement.Application.Queries;
using Darklands.Core.Features.Movement.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Features.Movement.Application.Queries;

[Trait("Category", "Movement")]
[Trait("Category", "Unit")]
public class FindPathQueryHandlerTests
{
    private readonly IPathfindingService _mockPathfindingService;
    private readonly ILogger<FindPathQueryHandler> _mockLogger;
    private readonly FindPathQueryHandler _handler;

    public FindPathQueryHandlerTests()
    {
        _mockPathfindingService = Substitute.For<IPathfindingService>();
        _mockLogger = Substitute.For<ILogger<FindPathQueryHandler>>();
        _handler = new FindPathQueryHandler(_mockPathfindingService, _mockLogger);
    }

    #region Valid Path Tests

    [Fact]
    public async Task Handle_ValidPath_ShouldReturnPath()
    {
        // Arrange
        var start = new Position(0, 0);
        var goal = new Position(2, 2);
        var expectedPath = new List<Position>
        {
            new Position(0, 0),
            new Position(1, 1),
            new Position(2, 2)
        }.AsReadOnly();

        _mockPathfindingService
            .FindPath(start, goal, Arg.Any<Func<Position, bool>>(), Arg.Any<Func<Position, int>>())
            .Returns(Result.Success<IReadOnlyList<Position>>(expectedPath));

        var query = new FindPathQuery(
            start,
            goal,
            pos => true, // All passable
            pos => 1);   // Uniform cost

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().ContainInOrder(expectedPath);
    }

    [Fact]
    public async Task Handle_StartEqualsGoal_ShouldReturnSinglePositionPath()
    {
        // WHY: Path from position to itself is valid (single-element path)

        // Arrange
        var position = new Position(5, 5);
        var expectedPath = new List<Position> { position }.AsReadOnly();

        _mockPathfindingService
            .FindPath(position, position, Arg.Any<Func<Position, bool>>(), Arg.Any<Func<Position, int>>())
            .Returns(Result.Success<IReadOnlyList<Position>>(expectedPath));

        var query = new FindPathQuery(position, position, pos => true, pos => 1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Should().Be(position);
    }

    #endregion

    #region Failure Tests

    [Fact]
    public async Task Handle_NoPathExists_ShouldReturnFailure()
    {
        // WHY: Blocked by walls - no path possible

        // Arrange
        var start = new Position(0, 0);
        var goal = new Position(10, 10);

        _mockPathfindingService
            .FindPath(start, goal, Arg.Any<Func<Position, bool>>(), Arg.Any<Func<Position, int>>())
            .Returns(Result.Failure<IReadOnlyList<Position>>("No path exists"));

        var query = new FindPathQuery(start, goal, pos => false, pos => 1); // All impassable

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No path exists");
    }

    [Fact]
    public void Handle_NullIsPassableFunction_ShouldThrowArgumentNullException()
    {
        // WHY: Programmer error (contract violation) - fail fast per ADR-003

        // Arrange
        var query = new FindPathQuery(
            new Position(0, 0),
            new Position(5, 5),
            null!, // Programmer error
            pos => 1);

        // Act & Assert
        var act = () => _handler.Handle(query, CancellationToken.None);
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Handle_NullGetCostFunction_ShouldThrowArgumentNullException()
    {
        // WHY: Programmer error (contract violation) - fail fast per ADR-003

        // Arrange
        var query = new FindPathQuery(
            new Position(0, 0),
            new Position(5, 5),
            pos => true,
            null!); // Programmer error

        // Act & Assert
        var act = () => _handler.Handle(query, CancellationToken.None);
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Service Delegation Tests

    [Fact]
    public async Task Handle_ShouldDelegateToPathfindingService()
    {
        // WHY: Handler is thin wrapper - service does the work

        // Arrange
        var start = new Position(1, 1);
        var goal = new Position(9, 9);
        Func<Position, bool> isPassable = pos => true;
        Func<Position, int> getCost = pos => 1;

        _mockPathfindingService
            .FindPath(start, goal, Arg.Any<Func<Position, bool>>(), Arg.Any<Func<Position, int>>())
            .Returns(Result.Success<IReadOnlyList<Position>>(new List<Position>().AsReadOnly()));

        var query = new FindPathQuery(start, goal, isPassable, getCost);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockPathfindingService.Received(1).FindPath(
            start,
            goal,
            Arg.Any<Func<Position, bool>>(),
            Arg.Any<Func<Position, int>>());
    }

    #endregion
}
