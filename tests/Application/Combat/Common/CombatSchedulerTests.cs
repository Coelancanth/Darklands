using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Darklands.Core.Application.Combat.Common;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Determinism;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darklands.Core.Tests.Application.Combat.Common
{
    [Trait("Category", "Phase2")]
    public class CombatSchedulerTests
    {
        [Fact]
        public void ScheduleEntity_ValidEntity_AddsSuccessfully()
        {
            // Arrange
            var scheduler = new CombatScheduler();
            var actor = new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(1000), new Position(0, 0));

            // Act
            var result = scheduler.ScheduleEntity(actor);

            // Assert
            result.IsSucc.Should().BeTrue();
            scheduler.Count.Should().Be(1);
        }

        [Fact]
        public void ScheduleEntity_NullEntity_ReturnsFailure()
        {
            // Arrange
            var scheduler = new CombatScheduler();

            // Act
            var result = scheduler.ScheduleEntity(null!);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("INVALID_ENTITY")
            );
        }

        [Fact]
        public void ProcessNextTurn_EmptyScheduler_ReturnsNone()
        {
            // Arrange
            var scheduler = new CombatScheduler();

            // Act
            var result = scheduler.ProcessNextTurn();

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: option => option.IsNone.Should().BeTrue(),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void ProcessNextTurn_WithEntities_ReturnsEarliestAndRemoves()
        {
            // Arrange
            var scheduler = new CombatScheduler();
            var earlyActor = new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(1000), new Position(0, 0));
            var lateActor = new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(2000), new Position(1, 1));

            scheduler.ScheduleEntity(lateActor);
            scheduler.ScheduleEntity(earlyActor); // Add in wrong order

            // Act
            var result = scheduler.ProcessNextTurn();

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: option =>
                {
                    option.IsSome.Should().BeTrue();
                    option.Match(
                        Some: actor => actor.Id.Should().Be(earlyActor.Id),
                        None: () => throw new InvalidOperationException("Expected Some")
                    );
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );

            scheduler.Count.Should().Be(1, "early actor should be removed");
        }

        [Fact]
        public void GetTurnOrder_MultipleEntities_ReturnsCorrectOrder()
        {
            // Arrange
            var scheduler = new CombatScheduler();
            var guid1 = new Guid("11111111-1111-1111-1111-111111111111");
            var guid2 = new Guid("22222222-2222-2222-2222-222222222222");

            var actor1 = new SchedulableActor(guid2, TimeUnit.CreateUnsafe(1000), new Position(0, 0));
            var actor2 = new SchedulableActor(guid1, TimeUnit.CreateUnsafe(1000), new Position(1, 1)); // Same time, lower ID
            var actor3 = new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(500), new Position(2, 2)); // Earlier time

            scheduler.ScheduleEntity(actor1);
            scheduler.ScheduleEntity(actor2);
            scheduler.ScheduleEntity(actor3);

            // Act
            var result = scheduler.GetTurnOrder();

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: turnOrder =>
                {
                    turnOrder.Count.Should().Be(3);
                    turnOrder[0].Id.Should().Be(actor3.Id, "earliest time should be first");
                    turnOrder[1].Id.Should().Be(guid1, "same time, lower ID should be second");
                    turnOrder[2].Id.Should().Be(guid2, "same time, higher ID should be third");
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void GetTurnOrder_DoesNotModifyScheduler()
        {
            // Arrange
            var scheduler = new CombatScheduler();
            var actor = new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(1000), new Position(0, 0));
            scheduler.ScheduleEntity(actor);

            var initialCount = scheduler.Count;

            // Act
            var result = scheduler.GetTurnOrder();

            // Assert
            result.IsSucc.Should().BeTrue();
            scheduler.Count.Should().Be(initialCount, "GetTurnOrder should not modify scheduler");
        }

        [Fact]
        public void Clear_RemovesAllEntities()
        {
            // Arrange
            var scheduler = new CombatScheduler();
            for (int i = 0; i < 5; i++)
            {
                var actor = new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(i * 100), new Position(i, i));
                scheduler.ScheduleEntity(actor);
            }

            scheduler.Count.Should().Be(5, "precondition: scheduler should have entities");

            // Act
            scheduler.Clear();

            // Assert
            scheduler.Count.Should().Be(0);
            var turnOrder = scheduler.GetTurnOrder();
            turnOrder.Match(
                Succ: list => list.Should().BeEmpty(),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void DeterministicOrdering_SameTimeDifferentIds_IsConsistent()
        {
            // Arrange
            var scheduler1 = new CombatScheduler();
            var scheduler2 = new CombatScheduler();
            var sameTime = TimeUnit.CreateUnsafe(1000);

            var actors = Enumerable.Range(0, 50)
                .Select(_ => new SchedulableActor(Guid.NewGuid(), sameTime, new Position(0, 0)))
                .ToList();

            // Add to both schedulers in different orders
            foreach (var actor in actors) scheduler1.ScheduleEntity(actor);
            foreach (var actor in actors.AsEnumerable().Reverse()) scheduler2.ScheduleEntity(actor);

            // Act
            var order1 = scheduler1.GetTurnOrder().Match(Succ: list => list, Fail: _ => throw new Exception());
            var order2 = scheduler2.GetTurnOrder().Match(Succ: list => list, Fail: _ => throw new Exception());

            // Assert - Both should have identical order regardless of insertion order
            order1.Count.Should().Be(order2.Count);
            for (int i = 0; i < order1.Count; i++)
            {
                order1[i].Id.Should().Be(order2[i].Id, $"order should be identical at position {i}");
            }
        }

        [Fact]
        public void PerformanceTest_1000PlusActors_CompletesEfficiently()
        {
            // Arrange
            var scheduler = new CombatScheduler();
            var actorCount = 1500; // Exceeds required 1000+ actors
            var random = new DeterministicRandom(42UL, logger: NullLogger<DeterministicRandom>.Instance); // Fixed seed for reproducible tests

            var actors = new List<SchedulableActor>();
            for (int i = 0; i < actorCount; i++)
            {
                var randomTimeResult = random.Next(9999, $"perf_test_time_{i}");
                var randomTime = randomTimeResult.Match(
                    Succ: value => value + 1, // +1 to shift from [0,9999) to [1,10000)
                    Fail: _ => 5000 // Fallback value
                );

                actors.Add(new SchedulableActor(
                    Guid.NewGuid(),
                    TimeUnit.CreateUnsafe(randomTime),
                    new Position(i % 100, i / 100)
                ));
            }

            var stopwatch = Stopwatch.StartNew();

            // Act - Schedule all actors
            foreach (var actor in actors)
            {
                var result = scheduler.ScheduleEntity(actor);
                result.IsSucc.Should().BeTrue($"Failed to schedule actor {actor.Id}");
            }

            var scheduleTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            // Act - Get turn order (O(n) operation)
            var turnOrderResult = scheduler.GetTurnOrder();
            var queryTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            // Act - Process all turns (O(n log n) total for n operations)
            var processedActors = new List<Guid>();
            for (int i = 0; i < actorCount; i++)
            {
                var result = scheduler.ProcessNextTurn();
                result.IsSucc.Should().BeTrue();
                result.Match(
                    Succ: option => option.Match(
                        Some: actor => processedActors.Add(actor.Id),
                        None: () => throw new InvalidOperationException($"Expected actor at iteration {i}")
                    ),
                    Fail: _ => throw new InvalidOperationException("Process turn failed")
                );
            }

            var processTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            // Assert - Performance requirements
            scheduleTime.Should().BeLessThan(1000, $"Scheduling {actorCount} actors should be fast (<1s), took {scheduleTime}ms");
            queryTime.Should().BeLessThan(100, $"Querying turn order should be very fast (<100ms), took {queryTime}ms");
            processTime.Should().BeLessThan(2000, $"Processing {actorCount} turns should be fast (<2s), took {processTime}ms");

            // Assert - Correctness
            scheduler.Count.Should().Be(0, "All actors should be processed");
            processedActors.Count.Should().Be(actorCount, "All actors should be processed exactly once");
            processedActors.Distinct().Count().Should().Be(actorCount, "No actor should be processed twice");

            // Verify turn order was correct
            turnOrderResult.IsSucc.Should().BeTrue();
            turnOrderResult.Match(
                Succ: turnOrder => turnOrder.Count.Should().Be(actorCount),
                Fail: _ => throw new InvalidOperationException("Turn order query failed")
            );
        }

        [Fact]
        public void ScheduleEntity_DuplicateEntity_AllowsMultipleEntries()
        {
            // Arrange - Some scenarios might legitimately schedule the same entity multiple times
            var scheduler = new CombatScheduler();
            var actor = new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(1000), new Position(0, 0));

            // Act
            var result1 = scheduler.ScheduleEntity(actor);
            var result2 = scheduler.ScheduleEntity(actor);

            // Assert
            result1.IsSucc.Should().BeTrue();
            result2.IsSucc.Should().BeTrue();
            scheduler.Count.Should().Be(2, "duplicate entities should be allowed (business requirement)");
        }
    }
}
