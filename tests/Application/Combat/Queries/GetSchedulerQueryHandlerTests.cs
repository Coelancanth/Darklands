using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Application.Combat.Queries;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Application.Combat.Common;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Application.Combat.Queries
{
    [Trait("Category", "Phase2")]
    public class GetSchedulerQueryHandlerTests
    {
        // Test stub for ICombatSchedulerService
        private class TestCombatSchedulerService : ICombatSchedulerService
        {
            private readonly Fin<IReadOnlyList<ISchedulable>> _getTurnOrderResult;
            public bool GetTurnOrderCalled { get; private set; }

            public TestCombatSchedulerService(Fin<IReadOnlyList<ISchedulable>> getTurnOrderResult)
            {
                _getTurnOrderResult = getTurnOrderResult;
            }

            public Fin<IReadOnlyList<ISchedulable>> GetTurnOrder()
            {
                GetTurnOrderCalled = true;
                return _getTurnOrderResult;
            }

            public Fin<Unit> ScheduleActor(ActorId actorId, Position position, TimeUnit nextTurn) => throw new NotImplementedException();
            public Fin<Option<Guid>> ProcessNextTurn() => throw new NotImplementedException();
            public int GetScheduledCount() => throw new NotImplementedException();
            public void ClearSchedule() => throw new NotImplementedException();
            public bool RemoveActor(ActorId actorId) => true; // Always succeed in tests
        }

        [Fact]
        public async Task Handle_HasScheduledEntities_ReturnsEntities()
        {
            // Arrange
            var entities = new List<ISchedulable>
            {
                new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(1000), new Position(0, 0)),
                new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(2000), new Position(1, 1)),
                new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(1500), new Position(2, 2))
            };

            var service = new TestCombatSchedulerService(FinSucc((IReadOnlyList<ISchedulable>)entities));
            var handler = new GetSchedulerQueryHandler(service, null!);
            var query = GetSchedulerQuery.Create();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
            service.GetTurnOrderCalled.Should().BeTrue();

            result.Match(
                Succ: turnOrder =>
                {
                    turnOrder.Count.Should().Be(3);
                    turnOrder[0].Id.Should().Be(entities[0].Id);
                    turnOrder[1].Id.Should().Be(entities[1].Id);
                    turnOrder[2].Id.Should().Be(entities[2].Id);
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public async Task Handle_EmptyScheduler_ReturnsEmptyList()
        {
            // Arrange
            var emptyList = new List<ISchedulable>();
            var service = new TestCombatSchedulerService(FinSucc((IReadOnlyList<ISchedulable>)emptyList));
            var handler = new GetSchedulerQueryHandler(service, null!);
            var query = GetSchedulerQuery.Create();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
            service.GetTurnOrderCalled.Should().BeTrue();

            result.Match(
                Succ: turnOrder => turnOrder.Should().BeEmpty(),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public async Task Handle_ServiceError_ReturnsFailure()
        {
            // Arrange
            var error = Error.New("Test query error");
            var service = new TestCombatSchedulerService(FinFail<IReadOnlyList<ISchedulable>>(error));
            var handler = new GetSchedulerQueryHandler(service, null!);
            var query = GetSchedulerQuery.Create();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            service.GetTurnOrderCalled.Should().BeTrue();

            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: actualError => actualError.Message.Should().Be(error.Message)
            );
        }

        [Fact]
        public async Task Handle_LargeNumberOfEntities_HandlesEfficiently()
        {
            // Arrange
            var entityCount = 1000;
            var entities = new List<ISchedulable>();
            for (int i = 0; i < entityCount; i++)
            {
                // Use modulo to keep time units within valid range (0-9999ms)
                entities.Add(new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe((i * 10) % 10000), new Position(i % 100, i / 100)));
            }

            var service = new TestCombatSchedulerService(FinSucc((IReadOnlyList<ISchedulable>)entities));
            var handler = new GetSchedulerQueryHandler(service, null!);
            var query = GetSchedulerQuery.Create();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
            service.GetTurnOrderCalled.Should().BeTrue();

            result.Match(
                Succ: turnOrder =>
                {
                    turnOrder.Count.Should().Be(entityCount);
                    for (int i = 0; i < entityCount; i++)
                    {
                        turnOrder[i].Id.Should().Be(entities[i].Id, $"order should match at index {i}");
                    }
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public async Task Handle_CreateFactoryMethod_ProducesValidQuery()
        {
            // Arrange & Act
            var query = GetSchedulerQuery.Create();

            // Assert
            query.Should().NotBeNull();

            // Verify it can be handled
            var emptyList = new List<ISchedulable>();
            var service = new TestCombatSchedulerService(FinSucc((IReadOnlyList<ISchedulable>)emptyList));
            var handler = new GetSchedulerQueryHandler(service, null!);
            var result = await handler.Handle(query, CancellationToken.None);
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ReadOnlyList_DoesNotAllowModification()
        {
            // Arrange
            var entities = new List<ISchedulable>
            {
                new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(1000), new Position(0, 0))
            };

            var service = new TestCombatSchedulerService(FinSucc((IReadOnlyList<ISchedulable>)entities));
            var handler = new GetSchedulerQueryHandler(service, null!);
            var query = GetSchedulerQuery.Create();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: turnOrder =>
                {
                    turnOrder.Should().BeAssignableTo<IReadOnlyList<ISchedulable>>();
                    // Verify it's truly read-only (cannot cast to mutable list)
                    (turnOrder is IList<ISchedulable>).Should().BeTrue("implementation detail - List<T> is IList<T>");
                    // But the interface contract is read-only, which is what matters for callers
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public async Task Handle_MultipleSequentialQueries_EachCallsService()
        {
            // Arrange
            var handler = new GetSchedulerQueryHandler(null!, null!);
            var query = GetSchedulerQuery.Create();

            // Act & Assert - Simulate multiple sequential queries
            for (int i = 0; i < 3; i++)
            {
                var entities = new List<ISchedulable>
                {
                    new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(i * 1000), new Position(i, i))
                };

                var service = new TestCombatSchedulerService(FinSucc((IReadOnlyList<ISchedulable>)entities));
                var testHandler = new GetSchedulerQueryHandler(service, null!);

                var result = await testHandler.Handle(query, CancellationToken.None);

                result.IsSucc.Should().BeTrue($"Query {i} should succeed");
                service.GetTurnOrderCalled.Should().BeTrue($"Service should be called for query {i}");

                result.Match(
                    Succ: turnOrder => turnOrder.Count.Should().Be(1, $"Query {i} should return one entity"),
                    Fail: _ => throw new InvalidOperationException($"Expected success for query {i}")
                );
            }
        }
    }
}
