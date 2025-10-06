using CSharpFunctionalExtensions;
using Darklands.Core.Application;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Queries;
using Darklands.Core.Features.Grid.Application.Services;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Features.Grid.Application.Queries;

[Trait("Category", "Grid")]
[Trait("Category", "Unit")]
public class GetVisibleActorsQueryHandlerTests
{
    private readonly IActorPositionService _mockPositionService;
    private readonly IMediator _mockMediator;
    private readonly IPlayerContext _mockPlayerContext;
    private readonly ILogger<GetVisibleActorsQueryHandler> _mockLogger;
    private readonly GetVisibleActorsQueryHandler _handler;

    public GetVisibleActorsQueryHandlerTests()
    {
        _mockPositionService = Substitute.For<IActorPositionService>();
        _mockMediator = Substitute.For<IMediator>();
        _mockPlayerContext = Substitute.For<IPlayerContext>();
        _mockLogger = Substitute.For<ILogger<GetVisibleActorsQueryHandler>>();
        _handler = new GetVisibleActorsQueryHandler(_mockPositionService, _mockMediator, _mockPlayerContext, _mockLogger);
    }

    #region Valid Visibility Tests

    [Fact]
    public async Task Handle_ActorsInFOV_ShouldReturnVisibleActors()
    {
        // QUERY COMPOSITION: Uses CalculateFOVQuery internally

        // Arrange
        var observerId = ActorId.NewId();
        var actor1 = ActorId.NewId();
        var actor2 = ActorId.NewId();
        var actor3 = ActorId.NewId();

        var observerPos = new Position(10, 10);
        var actor1Pos = new Position(11, 10); // Visible
        var actor2Pos = new Position(12, 10); // Visible
        var actor3Pos = new Position(20, 20); // Not visible

        var visiblePositions = new HashSet<Position> { observerPos, actor1Pos, actor2Pos };

        var query = new GetVisibleActorsQuery(observerId, 5);

        // Setup: Observer position
        _mockPositionService.GetPosition(observerId).Returns(Result.Success(observerPos));

        // Setup: FOV calculation (via mediator)
        _mockMediator.Send(Arg.Is<CalculateFOVQuery>(q => q.Observer == observerPos && q.Radius == 5), Arg.Any<CancellationToken>())
            .Returns(Result.Success(visiblePositions));

        // Setup: All actors
        _mockPositionService.GetAllActors()
            .Returns(Result.Success(new List<ActorId> { observerId, actor1, actor2, actor3 }));

        // Setup: Actor positions
        _mockPositionService.GetPosition(actor1).Returns(Result.Success(actor1Pos));
        _mockPositionService.GetPosition(actor2).Returns(Result.Success(actor2Pos));
        _mockPositionService.GetPosition(actor3).Returns(Result.Success(actor3Pos));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(actor1);
        result.Value.Should().Contain(actor2);
        result.Value.Should().NotContain(actor3); // Out of FOV
        result.Value.Should().NotContain(observerId); // Observer excluded from results
    }

    [Fact]
    public async Task Handle_NoActorsInFOV_ShouldReturnEmptyList()
    {
        // Arrange
        var observerId = ActorId.NewId();
        var actor1 = ActorId.NewId();

        var observerPos = new Position(10, 10);
        var actor1Pos = new Position(20, 20); // Out of FOV

        var visiblePositions = new HashSet<Position> { observerPos };

        var query = new GetVisibleActorsQuery(observerId, 5);

        _mockPositionService.GetPosition(observerId).Returns(Result.Success(observerPos));
        _mockMediator.Send(Arg.Any<CalculateFOVQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(visiblePositions));
        _mockPositionService.GetAllActors()
            .Returns(Result.Success(new List<ActorId> { observerId, actor1 }));
        _mockPositionService.GetPosition(actor1).Returns(Result.Success(actor1Pos));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OnlyObserverExists_ShouldReturnEmptyList()
    {
        // EDGE CASE: Observer is the only actor in the world

        // Arrange
        var observerId = ActorId.NewId();
        var observerPos = new Position(10, 10);
        var visiblePositions = new HashSet<Position> { observerPos };

        var query = new GetVisibleActorsQuery(observerId, 5);

        _mockPositionService.GetPosition(observerId).Returns(Result.Success(observerPos));
        _mockMediator.Send(Arg.Any<CalculateFOVQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(visiblePositions));
        _mockPositionService.GetAllActors()
            .Returns(Result.Success(new List<ActorId> { observerId })); // Only observer

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty(); // No other actors to see
    }

    #endregion

    #region Observer Not Found Tests

    [Fact]
    public async Task Handle_ObserverNotFound_ShouldReturnFailure()
    {
        // Arrange
        var observerId = ActorId.NewId();
        var query = new GetVisibleActorsQuery(observerId, 5);

        _mockPositionService.GetPosition(observerId)
            .Returns(Result.Failure<Position>("Actor not found"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Actor not found");
        await _mockMediator.DidNotReceive().Send(Arg.Any<CalculateFOVQuery>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region FOV Calculation Failure Tests

    [Fact]
    public async Task Handle_FOVCalculationFails_ShouldReturnFailure()
    {
        // RAILWAY-ORIENTED: FOV failure propagates

        // Arrange
        var observerId = ActorId.NewId();
        var observerPos = new Position(10, 10);
        var query = new GetVisibleActorsQuery(observerId, 5);

        _mockPositionService.GetPosition(observerId).Returns(Result.Success(observerPos));
        _mockMediator.Send(Arg.Any<CalculateFOVQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<HashSet<Position>>("FOV error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("FOV error");
        _mockPositionService.DidNotReceive().GetAllActors();
    }

    #endregion

    #region Actor Position Errors

    [Fact]
    public async Task Handle_ActorWithInvalidPosition_ShouldSkipThatActor()
    {
        // WHY: Resilient to partial failures - skip broken actors, continue processing

        // Arrange
        var observerId = ActorId.NewId();
        var actor1 = ActorId.NewId();
        var actor2 = ActorId.NewId(); // This one has invalid position

        var observerPos = new Position(10, 10);
        var actor1Pos = new Position(11, 10);

        var visiblePositions = new HashSet<Position> { observerPos, actor1Pos };

        var query = new GetVisibleActorsQuery(observerId, 5);

        _mockPositionService.GetPosition(observerId).Returns(Result.Success(observerPos));
        _mockMediator.Send(Arg.Any<CalculateFOVQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(visiblePositions));
        _mockPositionService.GetAllActors()
            .Returns(Result.Success(new List<ActorId> { observerId, actor1, actor2 }));

        _mockPositionService.GetPosition(actor1).Returns(Result.Success(actor1Pos));
        _mockPositionService.GetPosition(actor2).Returns(Result.Failure<Position>("Position error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().Contain(actor1); // Valid actor included
        result.Value.Should().NotContain(actor2); // Invalid actor skipped
    }

    #endregion

    #region Query Composition Tests

    [Fact]
    public async Task Handle_ShouldDelegateToCalculateFOVQuery()
    {
        // ARCHITECTURE: Demonstrates query composition pattern

        // Arrange
        var observerId = ActorId.NewId();
        var observerPos = new Position(15, 15);
        var radius = 8;
        var query = new GetVisibleActorsQuery(observerId, radius);

        _mockPositionService.GetPosition(observerId).Returns(Result.Success(observerPos));
        _mockMediator.Send(Arg.Any<CalculateFOVQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new HashSet<Position> { observerPos }));
        _mockPositionService.GetAllActors()
            .Returns(Result.Success(new List<ActorId> { observerId }));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify CalculateFOVQuery was dispatched with correct parameters
        await _mockMediator.Received(1).Send(
            Arg.Is<CalculateFOVQuery>(q => q.Observer == observerPos && q.Radius == radius),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
