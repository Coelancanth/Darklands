using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Darklands.Core.Application.Combat.Common;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Tests.Application.Combat.Common
{
    [Trait("Category", "Phase2")]
    public class TimeComparerTests
    {
        [Fact]
        public void Compare_DifferentTimes_OrdersByTimeFirst()
        {
            // Arrange
            var earlierTime = TimeUnit.CreateUnsafe(1000);
            var laterTime = TimeUnit.CreateUnsafe(2000);

            var actor1 = new SchedulableActor(Guid.NewGuid(), laterTime, new Position(0, 0));
            var actor2 = new SchedulableActor(Guid.NewGuid(), earlierTime, new Position(1, 1));

            var comparer = TimeComparer.Instance;

            // Act
            var result = comparer.Compare(actor1, actor2);

            // Assert - actor2 should come first (earlier time = higher priority = negative result)
            result.Should().BePositive("actor1 has later time and should come after actor2");
        }

        [Fact]
        public void Compare_SameTime_DifferentIds_OrdersByIdDeterministically()
        {
            // Arrange
            var sameTime = TimeUnit.CreateUnsafe(1000);
            var guid1 = new Guid("11111111-1111-1111-1111-111111111111");
            var guid2 = new Guid("22222222-2222-2222-2222-222222222222");

            var actor1 = new SchedulableActor(guid1, sameTime, new Position(0, 0));
            var actor2 = new SchedulableActor(guid2, sameTime, new Position(1, 1));

            var comparer = TimeComparer.Instance;

            // Act
            var result = comparer.Compare(actor1, actor2);

            // Assert - should be deterministic based on Guid comparison
            result.Should().BeNegative("guid1 < guid2 lexicographically");

            // Verify determinism - should always return same result
            var result2 = comparer.Compare(actor1, actor2);
            result2.Should().Be(result, "comparison should be deterministic");
        }

        [Fact]
        public void Compare_SameTimeAndId_ReturnsZero()
        {
            // Arrange
            var sameTime = TimeUnit.CreateUnsafe(1000);
            var sameId = Guid.NewGuid();
            var position = new Position(0, 0);

            var actor1 = new SchedulableActor(sameId, sameTime, position);
            var actor2 = new SchedulableActor(sameId, sameTime, position);

            var comparer = TimeComparer.Instance;

            // Act
            var result = comparer.Compare(actor1, actor2);

            // Assert
            result.Should().Be(0, "identical actors should be considered equal");
        }

        [Fact]
        public void Compare_NullInputs_HandlesGracefully()
        {
            // Arrange
            var comparer = TimeComparer.Instance;
            var validActor = new SchedulableActor(Guid.NewGuid(), TimeUnit.CreateUnsafe(1000), new Position(0, 0));

            // Act & Assert
            comparer.Compare(null, null).Should().Be(0, "both null should be equal");
            comparer.Compare(null, validActor).Should().BePositive("null should come after valid actor");
            comparer.Compare(validActor, null).Should().BeNegative("valid actor should come before null");
        }

        [Fact]
        public void Compare_LargeScale_MaintainsDeterminism()
        {
            // Arrange - Create many actors with same time but different IDs
            var sameTime = TimeUnit.CreateUnsafe(1500);
            var actors = Enumerable.Range(0, 100)
                .Select(_ => new SchedulableActor(Guid.NewGuid(), sameTime, new Position(0, 0)))
                .ToList();

            var comparer = TimeComparer.Instance;

            // Act - Sort multiple times
            var sorted1 = actors.OrderBy(a => a, comparer).ToList();
            var sorted2 = actors.OrderBy(a => a, comparer).ToList();
            var sorted3 = actors.OrderBy(a => a, comparer).ToList();

            // Assert - All sorts should produce identical order
            for (int i = 0; i < sorted1.Count; i++)
            {
                sorted1[i].Id.Should().Be(sorted2[i].Id, $"sort should be deterministic at index {i}");
                sorted1[i].Id.Should().Be(sorted3[i].Id, $"sort should be deterministic at index {i}");
            }
        }

        [Fact]
        public void Instance_IsSingleton()
        {
            // Arrange & Act
            var instance1 = TimeComparer.Instance;
            var instance2 = TimeComparer.Instance;

            // Assert
            ReferenceEquals(instance1, instance2).Should().BeTrue("should use singleton pattern");
        }

        [Fact]
        public void Compare_TimeAndIdTieBreaking_WorksCorrectlyInSortedCollection()
        {
            // Arrange - Create actors with mixed times and IDs for comprehensive testing
            var earlyTime = TimeUnit.CreateUnsafe(1000);
            var lateTime = TimeUnit.CreateUnsafe(2000);

            var guid1 = new Guid("11111111-1111-1111-1111-111111111111");
            var guid2 = new Guid("22222222-2222-2222-2222-222222222222");
            var guid3 = new Guid("33333333-3333-3333-3333-333333333333");

            var actors = new List<SchedulableActor>
            {
                new(guid2, lateTime, new Position(0, 0)),   // Should be 4th (late time, middle id)
                new(guid3, earlyTime, new Position(1, 1)),  // Should be 2nd (early time, high id)
                new(guid1, lateTime, new Position(2, 2)),   // Should be 3rd (late time, low id)
                new(guid1, earlyTime, new Position(3, 3)),  // Should be 1st (early time, low id)
                new(guid3, lateTime, new Position(4, 4)),   // Should be 5th (late time, high id)
            };

            // Act
            var sorted = actors.OrderBy(a => a, TimeComparer.Instance).ToList();

            // Assert - Verify exact order
            sorted[0].Id.Should().Be(guid1, "earliest time with lowest id should be first");
            sorted[0].NextTurn.Should().Be(earlyTime);

            sorted[1].Id.Should().Be(guid3, "earliest time with higher id should be second");
            sorted[1].NextTurn.Should().Be(earlyTime);

            sorted[2].Id.Should().Be(guid1, "later time with lowest id should be third");
            sorted[2].NextTurn.Should().Be(lateTime);

            sorted[3].Id.Should().Be(guid2, "later time with middle id should be fourth");
            sorted[3].NextTurn.Should().Be(lateTime);

            sorted[4].Id.Should().Be(guid3, "later time with highest id should be last");
            sorted[4].NextTurn.Should().Be(lateTime);
        }
    }
}
