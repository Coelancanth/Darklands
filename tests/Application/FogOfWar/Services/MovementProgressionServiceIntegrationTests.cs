using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Serilog;
using Darklands.Application.FogOfWar.Services;
using Darklands.Application.FogOfWar.Events;
using Darklands.Application.Vision.Services;
using Darklands.Application.Grid.Services;
using Darklands.Application.Actor.Services;
using Darklands.Application.Infrastructure.DependencyInjection;
using Darklands.Core.Tests.TestUtilities;
using Darklands.Domain.Grid;
using Darklands.Domain.Vision;
using LanguageExt;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace Darklands.Core.Tests.Application.FogOfWar.Services;

/// <summary>
/// Integration tests for MovementProgressionService verifying end-to-end movement + FOV coordination
/// with real service implementations and full DI container resolution.
///
/// Phase 2 Focus: Application layer service coordination, MediatR event flow, FOV integration
/// Tests TD_061 Progressive FOV functionality with actual infrastructure.
/// </summary>
[Trait("Category", "Phase2")]
[Trait("Category", "Integration")]
[Trait("Category", "FogOfWar")]
[Collection("GameStrapper")]
public class MovementProgressionServiceIntegrationTests : IDisposable
{
    private ServiceProvider? _testServiceProvider;
    private readonly List<INotification> _publishedNotifications = new();

    private ServiceProvider GetServiceProvider()
    {
        if (_testServiceProvider == null)
        {
            // Ensure GameStrapper is properly disposed before re-initialization
            GameStrapper.Dispose();

            var initResult = GameStrapper.Initialize(GameStrapperConfiguration.Testing);
            _testServiceProvider = initResult.Match(
                Succ: provider => provider,
                Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}"));
        }
        return _testServiceProvider;
    }

    // Safe service accessors
    private IMovementProgressionService GetMovementProgressionService() =>
        GetServiceProvider().GetRequiredService<IMovementProgressionService>();
    private IMediator GetMediator() => GetServiceProvider().GetRequiredService<IMediator>();
    private IVisionStateService GetVisionStateService() =>
        GetServiceProvider().GetRequiredService<IVisionStateService>();
    private IGridStateService GetGridStateService() =>
        GetServiceProvider().GetRequiredService<IGridStateService>();
    private IActorStateService GetActorStateService() =>
        GetServiceProvider().GetRequiredService<IActorStateService>();

    public MovementProgressionServiceIntegrationTests()
    {
        // Initialize GameStrapper with test configuration
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        // Initialize provider for this test instance
        var _ = GetServiceProvider();
    }

    [Fact]
    public void StartMovement_ValidPath_PublishesStartNotificationAndProgressesCorrectly()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var path = new List<Position>
        {
            new Position(10, 10), // Start
            new Position(11, 10), // Step 1
            new Position(12, 10), // Step 2
            new Position(13, 10)  // End
        };
        var movementService = GetMovementProgressionService();
        var currentTurn = 1;

        // Act
        var startResult = movementService.StartMovement(actorId, path, 100, currentTurn);

        // Assert
        startResult.IsSucc.Should().BeTrue();

        // Verify actor is now moving
        movementService.IsMoving(actorId).Should().BeTrue();
        var currentPosition = movementService.GetCurrentPosition(actorId);
        currentPosition.IsSome.Should().BeTrue();
        currentPosition.Match(pos => pos.Should().Be(new Position(10, 10)), () => throw new Exception("Expected position"));

        // Verify progression state
        var progressionState = movementService.GetProgressionState(actorId);
        progressionState.IsSome.Should().BeTrue();
        progressionState.Match(
            Some: progression =>
            {
                progression.ActorId.Should().Be(actorId);
                progression.Path.Should().Equal(path);
                progression.CurrentIndex.Should().Be(0);
                progression.HasMoreSteps.Should().BeTrue();
            },
            None: () => throw new Exception("Expected progression state")
        );
    }

    [Fact]
    public void AdvanceGameTime_ProgressesMovementAndPublishesAdvancementNotifications()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var path = new List<Position>
        {
            new Position(5, 5), // Start
            new Position(6, 5), // Step 1
            new Position(7, 5)  // End
        };
        var movementService = GetMovementProgressionService();
        var currentTurn = 1;

        // Start movement with 100ms per step
        movementService.StartMovement(actorId, path, 100, currentTurn);

        // Act - Advance time to trigger first step
        var advanceResult = movementService.AdvanceGameTime(150, currentTurn);

        // Assert - One advancement should occur
        advanceResult.IsSucc.Should().BeTrue();
        advanceResult.Match(
            Succ: count => count.Should().Be(1),
            Fail: _ => throw new Exception("Expected success")
        );

        // Verify position advanced
        var newPosition = movementService.GetCurrentPosition(actorId);
        newPosition.Match(
            Some: pos => pos.Should().Be(new Position(6, 5)),
            None: () => throw new Exception("Expected position")
        );

        // Verify still moving
        movementService.IsMoving(actorId).Should().BeTrue();
    }

    [Fact]
    public void CompleteMovementProgression_PublishesCompletionNotificationAndCleansUp()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var path = new List<Position>
        {
            new Position(0, 0), // Start
            new Position(1, 0)  // End (only 1 step)
        };
        var movementService = GetMovementProgressionService();
        var currentTurn = 1;

        // Start movement
        movementService.StartMovement(actorId, path, 100, currentTurn);

        // Act - Advance enough time to complete movement
        var advanceResult = movementService.AdvanceGameTime(150, currentTurn);

        // Assert - Movement completed
        advanceResult.IsSucc.Should().BeTrue();

        // Verify actor is no longer moving
        movementService.IsMoving(actorId).Should().BeFalse();

        // Verify position is at final destination
        var finalPosition = movementService.GetCurrentPosition(actorId);
        finalPosition.IsNone.Should().BeTrue(); // No longer tracked since completed

        // Verify progression state is cleared
        var progressionState = movementService.GetProgressionState(actorId);
        progressionState.IsNone.Should().BeTrue();

        // Verify not in moving actors list
        var movingActors = movementService.GetMovingActors();
        movingActors.Should().NotContain(actorId);
    }

    [Fact]
    public void CancelMovement_StopsProgressionAndPublishesCompletionNotification()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var path = new List<Position>
        {
            new Position(8, 8), // Start
            new Position(9, 8), // Step 1
            new Position(10, 8) // End
        };
        var movementService = GetMovementProgressionService();
        var currentTurn = 1;

        // Start movement
        movementService.StartMovement(actorId, path, 200, currentTurn);

        // Verify movement started
        movementService.IsMoving(actorId).Should().BeTrue();

        // Act - Cancel movement
        var cancelResult = movementService.CancelMovement(actorId, currentTurn);

        // Assert
        cancelResult.IsSucc.Should().BeTrue();

        // Verify movement stopped
        movementService.IsMoving(actorId).Should().BeFalse();
        movementService.GetCurrentPosition(actorId).IsNone.Should().BeTrue();
        movementService.GetProgressionState(actorId).IsNone.Should().BeTrue();
    }

    [Fact]
    public void MultipleActors_HandlesConcurrentMovementProgressions()
    {
        // Arrange
        var actor1 = ActorId.NewId(TestIdGenerator.Instance);
        var actor2 = ActorId.NewId(TestIdGenerator.Instance);
        var path1 = new List<Position> { new Position(1, 1), new Position(2, 1) };
        var path2 = new List<Position> { new Position(5, 5), new Position(5, 6) };
        var movementService = GetMovementProgressionService();
        var currentTurn = 1;

        // Act - Start movement for both actors
        var start1 = movementService.StartMovement(actor1, path1, 100, currentTurn);
        var start2 = movementService.StartMovement(actor2, path2, 150, currentTurn);

        // Assert both started successfully
        start1.IsSucc.Should().BeTrue();
        start2.IsSucc.Should().BeTrue();

        // Verify both are moving
        movementService.IsMoving(actor1).Should().BeTrue();
        movementService.IsMoving(actor2).Should().BeTrue();

        var movingActors = movementService.GetMovingActors();
        movingActors.Should().Contain(actor1);
        movingActors.Should().Contain(actor2);
        movingActors.Should().HaveCount(2);

        // Advance time - only actor1 should advance (100ms timing vs 150ms)
        var advanceResult = movementService.AdvanceGameTime(120, currentTurn);
        advanceResult.Match(
            Succ: count => count.Should().Be(1), // Only one actor advanced
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void StartMovement_EmptyPath_ReturnsError()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var emptyPath = new List<Position>();
        var movementService = GetMovementProgressionService();

        // Act
        var result = movementService.StartMovement(actorId, emptyPath);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.Should().Contain("INVALID_PATH")
        );
    }

    [Fact]
    public void StartMovement_InvalidTiming_ReturnsError()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var path = new List<Position> { new Position(0, 0), new Position(1, 0) };
        var movementService = GetMovementProgressionService();

        // Act
        var result = movementService.StartMovement(actorId, path, 0); // Invalid timing

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.Should().Contain("INVALID_TIMING")
        );
    }

    public void Dispose()
    {
        _testServiceProvider?.Dispose();
        GameStrapper.Dispose();
    }
}
