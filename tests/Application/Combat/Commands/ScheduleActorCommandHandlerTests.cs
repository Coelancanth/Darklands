using Xunit;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Application.Combat.Commands;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Application.Combat.Commands
{
    [Trait("Category", "Phase2")]
    public class ScheduleActorCommandHandlerTests
    {
        // Test stub for ICombatSchedulerService - minimal implementation for testing
        private class TestCombatSchedulerService : ICombatSchedulerService
        {
            private readonly bool _shouldSucceed;
            private readonly string _errorMessage;
            public ActorId? LastScheduledActorId { get; private set; }
            public Position? LastScheduledPosition { get; private set; }
            public TimeUnit? LastScheduledTime { get; private set; }

            public TestCombatSchedulerService(bool shouldSucceed = true, string errorMessage = "Test error")
            {
                _shouldSucceed = shouldSucceed;
                _errorMessage = errorMessage;
            }

            public Fin<Unit> ScheduleActor(ActorId actorId, Position position, TimeUnit nextTurn)
            {
                if (!_shouldSucceed)
                    return FinFail<Unit>(Error.New(_errorMessage));

                LastScheduledActorId = actorId;
                LastScheduledPosition = position;
                LastScheduledTime = nextTurn;
                return FinSucc(Unit.Default);
            }

            public Fin<Option<Guid>> ProcessNextTurn() => throw new NotImplementedException();
            public Fin<IReadOnlyList<ISchedulable>> GetTurnOrder() => throw new NotImplementedException();
            public int GetScheduledCount() => throw new NotImplementedException();
            public void ClearSchedule() => throw new NotImplementedException();
        }

        [Fact]
        public async Task Handle_ValidCommand_SchedulesActorSuccessfully()
        {
            // Arrange
            var actorId = ActorId.FromGuid(Guid.NewGuid());
            var position = new Position(5, 7);
            var nextTurn = TimeUnit.CreateUnsafe(1500);
            var command = ScheduleActorCommand.Create(actorId, position, nextTurn);

            var service = new TestCombatSchedulerService(shouldSucceed: true);
            var handler = new ScheduleActorCommandHandler(service, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
            service.LastScheduledActorId.Should().Be(actorId);
            service.LastScheduledPosition.Should().Be(position);
            service.LastScheduledTime.Should().Be(nextTurn);
        }

        [Fact]
        public async Task Handle_ServiceFails_ReturnsFailure()
        {
            // Arrange
            var command = ScheduleActorCommand.Create(
                ActorId.FromGuid(Guid.NewGuid()),
                new Position(0, 0),
                TimeUnit.CreateUnsafe(1000));

            var service = new TestCombatSchedulerService(shouldSucceed: false, errorMessage: "SCHEDULER_FULL");
            var handler = new ScheduleActorCommandHandler(service, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("SCHEDULER_FULL")
            );
        }

        [Fact]
        public async Task Handle_MultipleCommands_AllScheduledCorrectly()
        {
            // Arrange
            var service = new TestCombatSchedulerService(shouldSucceed: true);
            var handler = new ScheduleActorCommandHandler(service, null!);

            var commands = new[]
            {
                ScheduleActorCommand.Create(ActorId.NewId(), new Position(1, 1), TimeUnit.CreateUnsafe(1000)),
                ScheduleActorCommand.Create(ActorId.NewId(), new Position(2, 2), TimeUnit.CreateUnsafe(1500)),
                ScheduleActorCommand.Create(ActorId.NewId(), new Position(3, 3), TimeUnit.CreateUnsafe(2000))
            };

            // Act & Assert
            foreach (var command in commands)
            {
                var result = await handler.Handle(command, CancellationToken.None);
                result.IsSucc.Should().BeTrue($"Command for actor {command.ActorId} should succeed");
                
                // Verify the service received the correct parameters
                service.LastScheduledActorId.Should().Be(command.ActorId);
                service.LastScheduledPosition.Should().Be(command.Position);
                service.LastScheduledTime.Should().Be(command.NextTurn);
            }
        }

        [Fact]
        public async Task Handle_CreateFactoryMethod_ProducesValidCommand()
        {
            // Arrange
            var actorId = ActorId.FromGuid(Guid.NewGuid());
            var position = new Position(10, 15);
            var nextTurn = TimeUnit.CreateUnsafe(2500);

            // Act
            var command = ScheduleActorCommand.Create(actorId, position, nextTurn);

            // Assert
            command.ActorId.Should().Be(actorId);
            command.Position.Should().Be(position);
            command.NextTurn.Should().Be(nextTurn);

            // Verify it can be handled
            var service = new TestCombatSchedulerService(shouldSucceed: true);
            var handler = new ScheduleActorCommandHandler(service, null!);
            var result = await handler.Handle(command, CancellationToken.None);
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EdgeCaseValues_HandledCorrectly()
        {
            // Arrange - Test with edge case values
            var service = new TestCombatSchedulerService(shouldSucceed: true);
            var handler = new ScheduleActorCommandHandler(service, null!);

            var edgeCases = new[]
            {
                // Minimum time
                ScheduleActorCommand.Create(ActorId.NewId(), new Position(0, 0), TimeUnit.Minimum),
                // Maximum time
                ScheduleActorCommand.Create(ActorId.NewId(), new Position(99, 99), TimeUnit.Maximum),
                // Zero time
                ScheduleActorCommand.Create(ActorId.NewId(), new Position(50, 50), TimeUnit.Zero)
            };

            // Act & Assert
            foreach (var command in edgeCases)
            {
                var result = await handler.Handle(command, CancellationToken.None);
                result.IsSucc.Should().BeTrue($"Edge case command should succeed: {command.NextTurn}");
            }
        }
    }
}