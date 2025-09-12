using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Moq;
using Darklands.Core.Domain.Debug;
using Xunit;
using Darklands.Core.Application.Combat.Coordination;
using Darklands.Core.Application.Combat.Commands;
using Darklands.Core.Application.Combat.Queries;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Tests.TestUtilities;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Application.Combat.Coordination
{
    /// <summary>
    /// Tests for GameLoopCoordinator - sequential turn processing coordinator.
    /// Validates the ADR-009 implementation for eliminating async race conditions.
    /// </summary>
    public class GameLoopCoordinatorTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ICategoryLogger> _mockLogger;
        private readonly GameLoopCoordinator _coordinator;

        public GameLoopCoordinatorTests()
        {
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ICategoryLogger>();
            _coordinator = new GameLoopCoordinator(_mockMediator.Object, _mockLogger.Object);
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public void Constructor_WithValidParameters_CreatesCoordinator()
        {
            // Arrange & Act
            var coordinator = new GameLoopCoordinator(_mockMediator.Object, _mockLogger.Object);

            // Assert
            coordinator.Should().NotBeNull("GameLoopCoordinator should be created with valid dependencies");
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public void Constructor_WithNullMediator_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action createCoordinator = () => new GameLoopCoordinator(null!, _mockLogger.Object);

            // Assert
            createCoordinator.Should().Throw<ArgumentNullException>("Mediator is required dependency");
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action createCoordinator = () => new GameLoopCoordinator(_mockMediator.Object, null!);

            // Assert
            createCoordinator.Should().Throw<ArgumentNullException>("Logger is required dependency");
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public async Task ProcessNextTurnAsync_WithScheduledActor_ReturnsActor()
        {
            // Arrange
            var expectedActorId = ActorId.NewId(TestIdGenerator.Instance);
            var expectedResult = FinSucc(Some(expectedActorId.Value)); // Returns Guid, not ISchedulable

            _mockMediator.Setup(m => m.Send(It.IsAny<ProcessNextTurnCommand>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _coordinator.ProcessNextTurnAsync();

            // Assert
            result.Match(
                Succ: actorOption => actorOption.Match(
                    Some: actorId => actorId.Should().Be(expectedActorId, "Should return the expected actor ID"),
                    None: () => Assert.Fail("Expected actor but got None")
                ),
                Fail: error => Assert.Fail($"Expected success but got error: {error}")
            );

            _mockMediator.Verify(m => m.Send(It.IsAny<ProcessNextTurnCommand>(), default), Times.Once);
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public async Task ProcessNextTurnAsync_WithNoScheduledActors_ReturnsNone()
        {
            // Arrange
            var expectedResult = FinSucc(Option<Guid>.None);

            _mockMediator.Setup(m => m.Send(It.IsAny<ProcessNextTurnCommand>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _coordinator.ProcessNextTurnAsync();

            // Assert
            result.Match(
                Succ: actorOption => actorOption.Match(
                    Some: actor => Assert.Fail($"Expected None but got actor: {actor.Value}"),
                    None: () => { /* Expected - test passes */ }
                ),
                Fail: error => Assert.Fail($"Expected success but got error: {error}")
            );
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public async Task ProcessNextTurnAsync_WithMediatorError_ReturnsError()
        {
            // Arrange
            var expectedError = Error.New("MEDIATOR_ERROR: Test error");
            _mockMediator.Setup(m => m.Send(It.IsAny<ProcessNextTurnCommand>(), default))
                .ReturnsAsync(FinFail<Option<Guid>>(expectedError));

            // Act
            var result = await _coordinator.ProcessNextTurnAsync();

            // Assert
            result.Match(
                Succ: _ => Assert.Fail("Expected error but got success"),
                Fail: error => error.Message.Should().Contain("MEDIATOR_ERROR", "Should propagate mediator error")
            );
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public async Task InitializeGameLoopAsync_WithValidActors_ReturnsSuccess()
        {
            // Arrange
            var actors = new[]
            {
                CreateMockSchedulable(ActorId.NewId(TestIdGenerator.Instance), TimeUnit.FromTU(100).IfFail(TimeUnit.Zero)),
                CreateMockSchedulable(ActorId.NewId(TestIdGenerator.Instance), TimeUnit.FromTU(200).IfFail(TimeUnit.Zero))
            };

            _mockMediator.Setup(m => m.Send(It.IsAny<ScheduleActorCommand>(), default))
                .ReturnsAsync(FinSucc(LanguageExt.Unit.Default));

            // Act
            var result = await _coordinator.InitializeGameLoopAsync(actors);

            // Assert
            result.Match(
                Succ: _ => true.Should().BeTrue("Initialization should succeed with valid actors"),
                Fail: error => Assert.Fail($"Expected success but got error: {error}")
            );
            _mockMediator.Verify(m => m.Send(It.IsAny<ScheduleActorCommand>(), default),
                Times.Exactly(2), "Should schedule each actor once");
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public async Task InitializeGameLoopAsync_WithSomeSchedulingFailures_ReturnsPartialFailure()
        {
            // Arrange
            var actors = new[]
            {
                CreateMockSchedulable(ActorId.NewId(TestIdGenerator.Instance), TimeUnit.FromTU(100).IfFail(TimeUnit.Zero)),
                CreateMockSchedulable(ActorId.NewId(TestIdGenerator.Instance), TimeUnit.FromTU(200).IfFail(TimeUnit.Zero))
            };

            _mockMediator.SetupSequence(m => m.Send(It.IsAny<ScheduleActorCommand>(), default))
                .ReturnsAsync(FinSucc(LanguageExt.Unit.Default))  // First succeeds
                .ReturnsAsync(FinFail<LanguageExt.Unit>(Error.New("SCHEDULE_ERROR: Test failure")));  // Second fails

            // Act  
            var result = await _coordinator.InitializeGameLoopAsync(actors);

            // Assert
            result.Match(
                Succ: _ => Assert.Fail("Expected partial failure"),
                Fail: error => error.Message.Should().Contain("INITIALIZATION_PARTIAL_FAILURE", "Should fail if any actor scheduling fails")
            );
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public async Task GetCurrentTurnOrderAsync_WithValidScheduler_ReturnsActors()
        {
            // Arrange
            var expectedActors = new List<ISchedulable>
            {
                CreateMockSchedulable(ActorId.NewId(TestIdGenerator.Instance), TimeUnit.FromTU(100).IfFail(TimeUnit.Zero)),
                CreateMockSchedulable(ActorId.NewId(TestIdGenerator.Instance), TimeUnit.FromTU(200).IfFail(TimeUnit.Zero))
            };

            _mockMediator.Setup(m => m.Send(It.IsAny<GetSchedulerQuery>(), default))
                .ReturnsAsync(FinSucc((IReadOnlyList<ISchedulable>)expectedActors));

            // Act
            var result = await _coordinator.GetCurrentTurnOrderAsync();

            // Assert
            result.Match(
                Succ: actors => actors.Count.Should().Be(2, "Should return all scheduled actors"),
                Fail: error => Assert.Fail($"Expected success but got error: {error}")
            );
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public async Task HasScheduledActorsAsync_WithActors_ReturnsTrue()
        {
            // Arrange
            var actors = new List<ISchedulable>
            {
                CreateMockSchedulable(ActorId.NewId(TestIdGenerator.Instance), TimeUnit.FromTU(100).IfFail(TimeUnit.Zero))
            };

            _mockMediator.Setup(m => m.Send(It.IsAny<GetSchedulerQuery>(), default))
                .ReturnsAsync(FinSucc((IReadOnlyList<ISchedulable>)actors));

            // Act
            var result = await _coordinator.HasScheduledActorsAsync();

            // Assert
            result.Match(
                Succ: hasActors => hasActors.Should().BeTrue("Should return true when actors are scheduled"),
                Fail: error => Assert.Fail($"Expected success but got error: {error}")
            );
        }

        [Fact]
        [Trait("Category", "GameLoop")]
        [Trait("Category", "Phase2")]
        public async Task HasScheduledActorsAsync_WithNoActors_ReturnsFalse()
        {
            // Arrange
            var emptyActorList = new List<ISchedulable>();

            _mockMediator.Setup(m => m.Send(It.IsAny<GetSchedulerQuery>(), default))
                .ReturnsAsync(FinSucc((IReadOnlyList<ISchedulable>)emptyActorList));

            // Act
            var result = await _coordinator.HasScheduledActorsAsync();

            // Assert
            result.Match(
                Succ: hasActors => hasActors.Should().BeFalse("Should return false when no actors are scheduled"),
                Fail: error => Assert.Fail($"Expected success but got error: {error}")
            );
        }

        /// <summary>
        /// Helper method to create mock schedulable entities for testing
        /// </summary>
        private static ISchedulable CreateMockSchedulable(ActorId actorId, TimeUnit nextTurn)
        {
            var mock = new Mock<ISchedulable>();
            mock.SetupGet(s => s.Id).Returns(actorId.Value);
            mock.SetupGet(s => s.NextTurn).Returns(nextTurn);
            return mock.Object;
        }
    }
}
