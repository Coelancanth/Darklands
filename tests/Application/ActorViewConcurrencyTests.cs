using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darklands.Domain.Grid;
using FluentAssertions;
using Xunit;

namespace Darklands.Tests.Presentation.Views
{
    /// <summary>
    /// Regression tests for BR_007: Concurrent collection access error in actor display system.
    /// Tests the actual ConcurrentDictionary behavior that fixes the concurrent collection access issue.
    /// These tests verify that the ConcurrentDictionary operations used in ActorView are thread-safe.
    /// </summary>
    public class ActorViewConcurrencyTests
    {
        [Fact(DisplayName = "BR_007: ConcurrentDictionary should handle concurrent AddOrUpdate operations")]
        public async Task ConcurrentDictionary_AddOrUpdate_ShouldBeThreadSafe()
        {
            // Arrange
            const int numberOfActors = 100;
            const int numberOfIterations = 10;
            var exceptions = new List<Exception>();

            for (int iteration = 0; iteration < numberOfIterations; iteration++)
            {
                var actorNodes = new ConcurrentDictionary<ActorId, string>();
                var healthBars = new ConcurrentDictionary<ActorId, string>();

                // Act - Simulate the exact pattern from ActorView.DisplayActorAsync
                var tasks = new List<Task>();

                for (int i = 0; i < numberOfActors; i++)
                {
                    var actorId = new ActorId(Guid.NewGuid());
                    var actorData = $"ActorNode_{i}";
                    var healthData = $"HealthBar_{i}";

                    var task = Task.Run(() =>
                    {
                        try
                        {
                            // These are the exact operations that were causing BR_007 with regular Dictionary
                            actorNodes.AddOrUpdate(actorId, actorData, (key, oldValue) => actorData);
                            healthBars.AddOrUpdate(actorId, healthData, (key, oldValue) => healthData);
                        }
                        catch (Exception ex)
                        {
                            lock (exceptions)
                            {
                                exceptions.Add(ex);
                            }
                        }
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }

            // Assert
            exceptions.Should().BeEmpty(
                "because ConcurrentDictionary should handle concurrent AddOrUpdate without throwing");
        }

        [Fact(DisplayName = "BR_007: ConcurrentDictionary should handle concurrent TryRemove operations")]
        public async Task ConcurrentDictionary_TryRemove_ShouldBeThreadSafe()
        {
            // Arrange
            const int numberOfActors = 50;
            var exceptions = new List<Exception>();
            var actorNodes = new ConcurrentDictionary<ActorId, string>();
            var healthBars = new ConcurrentDictionary<ActorId, string>();

            var actorIds = Enumerable.Range(0, numberOfActors)
                .Select(_ => new ActorId(Guid.NewGuid()))
                .ToList();

            // Pre-populate collections
            foreach (var actorId in actorIds)
            {
                actorNodes.TryAdd(actorId, $"Node_{actorId}");
                healthBars.TryAdd(actorId, $"Health_{actorId}");
            }

            // Act - Simulate concurrent removal (the pattern from ActorView.RemoveActorAsync)
            var removeTasks = actorIds.Select(actorId => Task.Run(() =>
            {
                try
                {
                    // These are the exact operations from the fix
                    actorNodes.TryRemove(actorId, out _);
                    healthBars.TryRemove(actorId, out _);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            })).ToArray();

            await Task.WhenAll(removeTasks);

            // Assert
            exceptions.Should().BeEmpty(
                "because ConcurrentDictionary should handle concurrent TryRemove without throwing");

            actorNodes.Should().BeEmpty("all actors should be removed");
            healthBars.Should().BeEmpty("all health bars should be removed");
        }

        [Fact(DisplayName = "BR_007: Mixed concurrent operations should not corrupt ConcurrentDictionary state")]
        public async Task ConcurrentDictionary_MixedOperations_ShouldMaintainIntegrity()
        {
            // Arrange
            const int numberOfOperations = 200;
            var random = new Random(42); // Deterministic for reproducibility
            var exceptions = new List<Exception>();
            var actorNodes = new ConcurrentDictionary<ActorId, string>();
            var activeActors = new List<ActorId>();

            // Act - Perform mixed operations concurrently
            var tasks = new List<Task>();

            for (int i = 0; i < numberOfOperations; i++)
            {
                var operationType = random.Next(0, 3); // 0: Add, 1: Update, 2: Remove

                var task = Task.Run(() =>
                {
                    try
                    {
                        switch (operationType)
                        {
                            case 0: // Add
                                var newActorId = new ActorId(Guid.NewGuid());
                                actorNodes.TryAdd(newActorId, $"Actor_{newActorId}");
                                lock (activeActors) activeActors.Add(newActorId);
                                break;

                            case 1: // Update
                                ActorId? updateActorId = null;
                                lock (activeActors)
                                {
                                    if (activeActors.Count > 0)
                                        updateActorId = activeActors[random.Next(activeActors.Count)];
                                }
                                if (updateActorId.HasValue)
                                    actorNodes.AddOrUpdate(updateActorId.Value, "Updated", (k, old) => "Updated");
                                break;

                            case 2: // Remove
                                ActorId? removeActorId = null;
                                lock (activeActors)
                                {
                                    if (activeActors.Count > 0)
                                    {
                                        var index = random.Next(activeActors.Count);
                                        removeActorId = activeActors[index];
                                        activeActors.RemoveAt(index);
                                    }
                                }
                                if (removeActorId.HasValue)
                                    actorNodes.TryRemove(removeActorId.Value, out _);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Assert
            exceptions.Should().BeEmpty(
                "because ConcurrentDictionary should handle all concurrent operations safely");
        }

        [Fact(DisplayName = "BR_007: Stress test ConcurrentDictionary with rapid concurrent modifications")]
        public async Task ConcurrentDictionary_StressTest_ShouldNotCorruptState()
        {
            // Arrange
            const int numberOfThreads = 20;
            const int operationsPerThread = 100;
            var exceptions = new List<Exception>();
            var actorNodes = new ConcurrentDictionary<ActorId, string>();
            var testActorId = new ActorId(Guid.NewGuid());

            // Pre-add the test actor
            actorNodes.TryAdd(testActorId, "InitialValue");

            // Act - Multiple threads rapidly modifying the same entry
            var tasks = Enumerable.Range(0, numberOfThreads).Select(threadId => Task.Run(() =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    try
                    {
                        // Rapid fire updates to the same actor (the problematic scenario)
                        var newValue = $"Thread{threadId}_Op{i}";
                        actorNodes.AddOrUpdate(testActorId, newValue, (k, old) => newValue);

                        // Occasionally try to remove and re-add
                        if (i % 10 == 0)
                        {
                            actorNodes.TryRemove(testActorId, out _);
                            actorNodes.TryAdd(testActorId, newValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            // Assert
            exceptions.Where(e => e.Message.Contains("concurrent collections"))
                .Should().BeEmpty("because ConcurrentDictionary should prevent concurrent collection exceptions");

            // The actor should either exist or not, but the collection should be in a valid state
            actorNodes.Keys.Should().NotContain(ActorId.Empty, "collection should not be corrupted");
        }
    }
}
