using CSharpFunctionalExtensions;
using Darklands.Core.Application;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Commands;
using Darklands.Core.Features.Movement.Application.Commands;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Features.Movement.Application.Commands;

[Trait("Category", "Movement")]
[Trait("Category", "Unit")]
public class MoveAlongPathCommandHandlerTests
{
    private readonly IMediator _mockMediator;
    private readonly IPlayerContext _mockPlayerContext;
    private readonly ILogger<MoveAlongPathCommandHandler> _mockLogger;
    private readonly MoveAlongPathCommandHandler _handler;

    public MoveAlongPathCommandHandlerTests()
    {
        _mockMediator = Substitute.For<IMediator>();
        _mockPlayerContext = Substitute.For<IPlayerContext>();
        _mockLogger = Substitute.For<ILogger<MoveAlongPathCommandHandler>>();
        _handler = new MoveAlongPathCommandHandler(_mockMediator, _mockPlayerContext, _mockLogger);
    }

    #region Valid Movement Tests

    [Fact]
    public async Task Handle_ValidPath_ShouldExecuteAllSteps()
    {
        // WHY: Multi-step movement orchestrates MoveActorCommand per tile

        // Arrange
        var actorId = ActorId.NewId();
        var path = new List<Position>
        {
            new Position(0, 0), // Start (skip this)
            new Position(1, 1), // Step 1
            new Position(2, 2), // Step 2
            new Position(3, 3)  // Step 3
        }.AsReadOnly();

        _mockMediator
            .Send(Arg.Any<MoveActorCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new MoveAlongPathCommand(actorId, path);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify 3 MoveActorCommand calls (skip first position)
        await _mockMediator.Received(3).Send(
            Arg.Is<MoveActorCommand>(cmd => cmd.ActorId == actorId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TwoStepPath_ShouldExecuteSingleMoveCommand()
    {
        // WHY: Path with start + goal = 1 actual move

        // Arrange
        var actorId = ActorId.NewId();
        var path = new List<Position>
        {
            new Position(5, 5), // Start
            new Position(6, 6)  // Goal
        }.AsReadOnly();

        _mockMediator
            .Send(Arg.Any<MoveActorCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new MoveAlongPathCommand(actorId, path);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _mockMediator.Received(1).Send(
            Arg.Is<MoveActorCommand>(cmd =>
                cmd.ActorId == actorId &&
                cmd.TargetPosition == new Position(6, 6)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidPath_ShouldCallMoveActorCommandInCorrectOrder()
    {
        // WHY: Path order matters - must move tile-by-tile

        // Arrange
        var actorId = ActorId.NewId();
        var path = new List<Position>
        {
            new Position(0, 0),
            new Position(1, 0),
            new Position(2, 0)
        }.AsReadOnly();

        var receivedPositions = new List<Position>();

        _mockMediator
            .Send(Arg.Any<MoveActorCommand>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var cmd = callInfo.Arg<MoveActorCommand>();
                receivedPositions.Add(cmd.TargetPosition);
                return Result.Success();
            });

        var command = new MoveAlongPathCommand(actorId, path);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        receivedPositions.Should().ContainInOrder(
            new Position(1, 0),
            new Position(2, 0));
    }

    #endregion

    #region Failure Tests

    [Fact]
    public async Task Handle_EmptyPath_ShouldReturnFailure()
    {
        // WHY: Invalid input - path must have at least start position

        // Arrange
        var actorId = ActorId.NewId();
        var emptyPath = new List<Position>().AsReadOnly();
        var command = new MoveAlongPathCommand(actorId, emptyPath);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Path cannot be null or empty");
    }

    [Fact]
    public async Task Handle_MoveActorCommandFails_ShouldStopAndReturnFailure()
    {
        // WHY: If step fails (impassable, etc.), stop immediately

        // Arrange
        var actorId = ActorId.NewId();
        var path = new List<Position>
        {
            new Position(0, 0),
            new Position(1, 1), // Will succeed
            new Position(2, 2), // Will fail
            new Position(3, 3)  // Should not reach
        }.AsReadOnly();

        _mockMediator
            .Send(Arg.Is<MoveActorCommand>(cmd => cmd.TargetPosition == new Position(1, 1)), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _mockMediator
            .Send(Arg.Is<MoveActorCommand>(cmd => cmd.TargetPosition == new Position(2, 2)), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Target position is impassable"));

        var command = new MoveAlongPathCommand(actorId, path);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Movement failed at step 2/4"); // Path has 4 positions (0-indexed iteration)
        result.Error.Should().Contain("Target position is impassable");

        // Verify only 2 calls (step 1 success, step 2 failure, step 3 not attempted)
        await _mockMediator.Received(2).Send(
            Arg.Any<MoveActorCommand>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task Handle_CancellationRequested_ShouldStopGracefully()
    {
        // WHY: Right-click interrupt - stop at current tile (no rollback)

        // Arrange
        var actorId = ActorId.NewId();
        var path = new List<Position>
        {
            new Position(0, 0),
            new Position(1, 1),
            new Position(2, 2),
            new Position(3, 3)
        }.AsReadOnly();

        var cts = new CancellationTokenSource();

        _mockMediator
            .Send(Arg.Any<MoveActorCommand>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                // Cancel after first move
                cts.Cancel();
                return Result.Success();
            });

        var command = new MoveAlongPathCommand(actorId, path);

        // Act
        var result = await _handler.Handle(command, cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Graceful stop = success

        // Verify only 1 move executed before cancellation
        await _mockMediator.Received(1).Send(
            Arg.Any<MoveActorCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancelledBeforeFirstMove_ShouldReturnSuccessWithNoMoves()
    {
        // WHY: Cancellation before any work = graceful exit

        // Arrange
        var actorId = ActorId.NewId();
        var path = new List<Position>
        {
            new Position(0, 0),
            new Position(1, 1)
        }.AsReadOnly();

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var command = new MoveAlongPathCommand(actorId, path);

        // Act
        var result = await _handler.Handle(command, cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Graceful cancellation = success

        // Verify no MoveActorCommand calls
        await _mockMediator.DidNotReceive().Send(
            Arg.Any<MoveActorCommand>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Integration with MoveActorCommand

    [Fact]
    public async Task Handle_ShouldReuseExistingMoveActorCommand()
    {
        // ARCHITECTURE: Validates command composition pattern (ADR-004)
        // WHY: No duplication of validation/FOV/events - reuse existing command

        // Arrange
        var actorId = ActorId.NewId();
        var path = new List<Position>
        {
            new Position(0, 0),
            new Position(1, 1)
        }.AsReadOnly();

        _mockMediator
            .Send(Arg.Any<MoveActorCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new MoveAlongPathCommand(actorId, path);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: MoveActorCommand was called (not some custom movement logic)
        await _mockMediator.Received(1).Send(
            Arg.Is<MoveActorCommand>(cmd =>
                cmd.ActorId == actorId &&
                cmd.TargetPosition == new Position(1, 1)),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
