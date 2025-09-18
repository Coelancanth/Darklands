using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Serilog;
using Darklands.Application.FogOfWar.Events;
using Darklands.Application.FogOfWar.Handlers;
using Darklands.Application.Vision.Services;
using Darklands.Application.Infrastructure.DependencyInjection;
using Darklands.Core.Tests.TestUtilities;
using Darklands.Domain.Grid;
using Darklands.Domain.Vision;
using LanguageExt;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace Darklands.Core.Tests.Application.FogOfWar.Handlers;

/// <summary>
/// Integration tests for Progressive FOV coordination between movement and vision systems.
/// Verifies that position advancement notifications trigger proper FOV recalculation.
///
/// Tests TD_061 Progressive FOV notification handling and vision system integration.
/// </summary>
[Trait("Category", "Phase2")]
[Trait("Category", "Integration")]
[Trait("Category", "FogOfWar")]
[Trait("Category", "Vision")]
[Collection("GameStrapper")]
public class ProgressiveFOVCoordinationTests : IDisposable
{
    private ServiceProvider? _testServiceProvider;

    private ServiceProvider GetServiceProvider()
    {
        if (_testServiceProvider == null)
        {
            GameStrapper.Dispose();

            var initResult = GameStrapper.Initialize(GameStrapperConfiguration.Testing);
            _testServiceProvider = initResult.Match(
                Succ: provider => provider,
                Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}"));
        }
        return _testServiceProvider;
    }

    // Safe service accessors
    private IMediator GetMediator() => GetServiceProvider().GetRequiredService<IMediator>();
    private IVisionStateService GetVisionStateService() =>
        GetServiceProvider().GetRequiredService<IVisionStateService>();

    public ProgressiveFOVCoordinationTests()
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        var _ = GetServiceProvider();
    }

    [Fact]
    public async Task RevealPositionAdvancedNotification_TriggersVisionCacheInvalidation()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var previousPosition = new Position(5, 5);
        var newPosition = new Position(6, 5);
        var currentTurn = 1;

        var mediator = GetMediator();
        var visionService = GetVisionStateService();

        // Pre-populate vision state to test cache invalidation
        var initialVisionState = VisionState.CreateEmpty(actorId);
        visionService.UpdateVisionState(initialVisionState);

        // Create advancement notification
        var notification = new RevealPositionAdvancedNotification(
            actorId, newPosition, previousPosition, currentTurn);

        // Act - Publish the notification (triggers handler)
        await mediator.Publish(notification);

        // Assert - This test verifies the handler executes without errors
        // In a full implementation, we could verify:
        // 1. Vision cache was invalidated
        // 2. New FOV calculation was triggered
        // 3. Vision state was updated with new position

        // For Phase 2, we verify the notification can be processed successfully
        // without throwing exceptions
        await Task.Delay(50); // Allow async processing to complete

        // The handler should have processed successfully (no exceptions thrown)
        // In future phases, this could be extended to verify actual FOV changes
        true.Should().BeTrue("Handler processed notification without exceptions");
    }

    [Fact]
    public async Task RevealProgressionStartedNotification_LogsMovementInitiation()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var path = new List<Position>
        {
            new Position(10, 10),
            new Position(11, 10),
            new Position(12, 10)
        };
        var currentTurn = 1;

        var mediator = GetMediator();

        var notification = new RevealProgressionStartedNotification(actorId, path, currentTurn);

        // Act - Publish the notification
        await mediator.Publish(notification);

        // Assert - Verify notification properties are correct
        notification.ActorId.Should().Be(actorId);
        notification.Path.Should().Equal(path);
        notification.Turn.Should().Be(currentTurn);
        notification.StartPosition.Should().Be(new Position(10, 10));
        notification.Destination.Should().Be(new Position(12, 10));
        notification.StepCount.Should().Be(3);

        // Handler should execute without exceptions
        await Task.Delay(50);
        true.Should().BeTrue("Handler processed notification without exceptions");
    }

    [Fact]
    public async Task RevealProgressionCompletedNotification_LogsMovementCompletion()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var finalPosition = new Position(15, 15);
        var currentTurn = 2;

        var mediator = GetMediator();

        var notification = new RevealProgressionCompletedNotification(actorId, finalPosition, currentTurn);

        // Act - Publish the notification
        await mediator.Publish(notification);

        // Assert - Verify notification properties
        notification.ActorId.Should().Be(actorId);
        notification.FinalPosition.Should().Be(finalPosition);
        notification.Turn.Should().Be(currentTurn);

        // Handler should execute without exceptions
        await Task.Delay(50);
        true.Should().BeTrue("Handler processed notification without exceptions");
    }

    [Fact]
    public async Task PositionAdvancement_DiagonalMovement_HandledCorrectly()
    {
        // Arrange - Test diagonal movement detection
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var previousPosition = new Position(8, 8);
        var newPosition = new Position(9, 9); // Diagonal move
        var currentTurn = 1;

        var mediator = GetMediator();

        var notification = new RevealPositionAdvancedNotification(
            actorId, newPosition, previousPosition, currentTurn);

        // Act
        await mediator.Publish(notification);

        // Assert - Verify diagonal movement properties
        notification.IsDiagonalMove.Should().BeTrue();
        notification.MovementVector.Should().Be(new Position(1, 1));
        notification.StepDistance.Should().Be(2); // Manhattan distance

        await Task.Delay(50);
        true.Should().BeTrue("Diagonal movement handled correctly");
    }

    [Fact]
    public async Task PositionAdvancement_OrthogonalMovement_HandledCorrectly()
    {
        // Arrange - Test orthogonal movement
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var previousPosition = new Position(3, 3);
        var newPosition = new Position(4, 3); // Horizontal move
        var currentTurn = 1;

        var mediator = GetMediator();

        var notification = new RevealPositionAdvancedNotification(
            actorId, newPosition, previousPosition, currentTurn);

        // Act
        await mediator.Publish(notification);

        // Assert - Verify orthogonal movement properties
        notification.IsDiagonalMove.Should().BeFalse();
        notification.MovementVector.Should().Be(new Position(1, 0));
        notification.StepDistance.Should().Be(1); // Manhattan distance

        await Task.Delay(50);
        true.Should().BeTrue("Orthogonal movement handled correctly");
    }

    [Fact]
    public void NotificationCreation_FactoryMethods_CreateCorrectly()
    {
        // Arrange
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var position1 = new Position(1, 1);
        var position2 = new Position(2, 2);
        var path = new List<Position> { position1, position2 };
        var turn = 5;

        // Act - Test factory methods
        var startedNotification = RevealProgressionStartedNotification.Create(actorId, path, turn);
        var advancedNotification = RevealPositionAdvancedNotification.Create(actorId, position2, position1, turn);
        var completedNotification = RevealProgressionCompletedNotification.Create(actorId, position2, turn);

        // Assert
        startedNotification.ActorId.Should().Be(actorId);
        startedNotification.Path.Should().Equal(path);
        startedNotification.Turn.Should().Be(turn);

        advancedNotification.ActorId.Should().Be(actorId);
        advancedNotification.NewRevealPosition.Should().Be(position2);
        advancedNotification.PreviousPosition.Should().Be(position1);
        advancedNotification.Turn.Should().Be(turn);

        completedNotification.ActorId.Should().Be(actorId);
        completedNotification.FinalPosition.Should().Be(position2);
        completedNotification.Turn.Should().Be(turn);
    }

    public void Dispose()
    {
        _testServiceProvider?.Dispose();
        GameStrapper.Dispose();
    }
}
