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
    public class ProcessNextTurnCommandHandlerTests
    {
        // Test stub for ICombatSchedulerService
        private class TestCombatSchedulerService : ICombatSchedulerService
        {
            private readonly Fin<Option<Guid>> _processResult;
            public bool ProcessNextTurnCalled { get; private set; }

            public TestCombatSchedulerService(Fin<Option<Guid>> processResult)
            {
                _processResult = processResult;
            }

            public Fin<Option<Guid>> ProcessNextTurn()
            {
                ProcessNextTurnCalled = true;
                return _processResult;
            }

            public Fin<Unit> ScheduleActor(ActorId actorId, Position position, TimeUnit nextTurn) => throw new NotImplementedException();
            public Fin<IReadOnlyList<ISchedulable>> GetTurnOrder() => throw new NotImplementedException();
            public int GetScheduledCount() => throw new NotImplementedException();
            public void ClearSchedule() => throw new NotImplementedException();
        }

        [Fact]
        public async Task Handle_ActorScheduled_ReturnsActorId()
        {
            // Arrange
            var expectedActorId = Guid.NewGuid();
            var service = new TestCombatSchedulerService(FinSucc(Some(expectedActorId)));
            var handler = new ProcessNextTurnCommandHandler(service, null!);
            var command = ProcessNextTurnCommand.Create();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
            service.ProcessNextTurnCalled.Should().BeTrue();
            
            result.Match(
                Succ: option => option.Match(
                    Some: actorId => actorId.Should().Be(expectedActorId),
                    None: () => throw new InvalidOperationException("Expected Some(actorId)")
                ),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public async Task Handle_NoActorsScheduled_ReturnsNone()
        {
            // Arrange
            var service = new TestCombatSchedulerService(FinSucc(Option<Guid>.None));
            var handler = new ProcessNextTurnCommandHandler(service, null!);
            var command = ProcessNextTurnCommand.Create();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
            service.ProcessNextTurnCalled.Should().BeTrue();
            
            result.Match(
                Succ: option => option.IsNone.Should().BeTrue(),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public async Task Handle_ServiceError_ReturnsFailure()
        {
            // Arrange
            var error = Error.New("Test scheduler error");
            var service = new TestCombatSchedulerService(FinFail<Option<Guid>>(error));
            var handler = new ProcessNextTurnCommandHandler(service, null!);
            var command = ProcessNextTurnCommand.Create();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            service.ProcessNextTurnCalled.Should().BeTrue();
            
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: actualError => actualError.Message.Should().Be(error.Message)
            );
        }

        [Fact]
        public async Task Handle_MultipleSequentialCalls_EachCallsService()
        {
            // Arrange
            var actorIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var handler = new ProcessNextTurnCommandHandler(null!, null!);
            var command = ProcessNextTurnCommand.Create();

            // Act & Assert - Simulate processing multiple turns in sequence
            for (int i = 0; i < actorIds.Length; i++)
            {
                var service = new TestCombatSchedulerService(FinSucc(Some(actorIds[i])));
                var testHandler = new ProcessNextTurnCommandHandler(service, null!);
                
                var result = await testHandler.Handle(command, CancellationToken.None);
                
                result.IsSucc.Should().BeTrue();
                service.ProcessNextTurnCalled.Should().BeTrue();
                
                result.Match(
                    Succ: option => option.Match(
                        Some: actorId => actorId.Should().Be(actorIds[i]),
                        None: () => throw new InvalidOperationException($"Expected actor {i}")
                    ),
                    Fail: _ => throw new InvalidOperationException($"Expected success for actor {i}")
                );
            }
        }

        [Fact]
        public async Task Handle_CreateFactoryMethod_ProducesValidCommand()
        {
            // Arrange & Act
            var command = ProcessNextTurnCommand.Create();

            // Assert
            command.Should().NotBeNull();
            
            // Verify it can be handled
            var service = new TestCombatSchedulerService(FinSucc(Option<Guid>.None));
            var handler = new ProcessNextTurnCommandHandler(service, null!);
            var result = await handler.Handle(command, CancellationToken.None);
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_CancellationRequested_StillCompletes()
        {
            // Arrange
            var service = new TestCombatSchedulerService(FinSucc(Some(Guid.NewGuid())));
            var handler = new ProcessNextTurnCommandHandler(service, null!);
            var command = ProcessNextTurnCommand.Create();
            
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancel the token

            // Act
            var result = await handler.Handle(command, cts.Token);

            // Assert - Handler should still complete (doesn't check cancellation)
            result.IsSucc.Should().BeTrue();
            service.ProcessNextTurnCalled.Should().BeTrue();
        }
    }
}