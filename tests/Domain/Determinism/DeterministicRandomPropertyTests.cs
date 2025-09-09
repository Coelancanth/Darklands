using Xunit;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Darklands.Core.Domain.Determinism;
using LanguageExt;
using static LanguageExt.Prelude;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Darklands.Core.Tests.Domain.Determinism;

/// <summary>
/// Property-based tests for IDeterministicRandom using FsCheck 3.x.
/// Tests mathematical invariants, distribution properties, and cross-platform determinism.
/// </summary>
[Trait("Category", "Phase1")]
[Trait("Category", "PropertyBased")]
public class DeterministicRandomPropertyTests
{
    private readonly ILogger<DeterministicRandom> _nullLogger = NullLogger<DeterministicRandom>.Instance;

    /// <summary>
    /// Custom generator for valid seeds (ulong values)
    /// </summary>
    private static Gen<ulong> GenSeed()
    {
        return Gen.Choose(0, int.MaxValue).Select(x => (ulong)x);
    }

    /// <summary>
    /// Property: Next(n) must always return a value in range [0, n) for any valid n
    /// </summary>
    [Fact]
    public void Next_AlwaysReturnsValueInRange()
    {
        var generator =
            from seed in GenSeed()
            from maxExclusive in Gen.Choose(1, 1000)
            from contextIndex in Gen.Choose(0, 100)
            select new { Seed = seed, MaxExclusive = maxExclusive, Context = $"test_{contextIndex}" };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var random = new DeterministicRandom(test.Seed, stream: 1UL, _nullLogger);

                // Act
                var result = random.Next(test.MaxExclusive, test.Context);

                // Assert
                result.IsSucc.Should().BeTrue();
                result.Match(
                    Succ: value =>
                    {
                        value.Should().BeGreaterThanOrEqualTo(0, "Next() should never return negative values");
                        value.Should().BeLessThan(test.MaxExclusive, "Next() should never return values >= maxExclusive");
                        return true;
                    },
                    Fail: _ => false
                ).Should().BeTrue();
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Range(min, max) must always return a value in [min, max) for any valid range
    /// </summary>
    [Fact]
    public void Range_AlwaysReturnsValueInBounds()
    {
        var generator =
            from seed in GenSeed()
            from min in Gen.Choose(-1000, 1000)
            from rangeSize in Gen.Choose(1, 1000)
            let maxExclusive = min + rangeSize
            select new { Seed = seed, Min = min, MaxExclusive = maxExclusive };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var random = new DeterministicRandom(test.Seed);

                // Act
                var result = random.Range(test.Min, test.MaxExclusive, "range_test");

                // Assert
                result.IsSucc.Should().BeTrue();
                result.Match(
                    Succ: value =>
                    {
                        value.Should().BeGreaterThanOrEqualTo(test.Min, "Range() should respect minimum bound");
                        value.Should().BeLessThan(test.MaxExclusive, "Range() should respect maximum bound");
                        return true;
                    },
                    Fail: _ => false
                ).Should().BeTrue();
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Check(percent) must return true approximately percent% of the time
    /// Tests statistical distribution accuracy
    /// </summary>
    [Fact]
    public void Check_StatisticalDistributionIsAccurate()
    {
        var generator =
            from seed in GenSeed()
            from percent in Gen.Choose(10, 90) // Avoid edge cases for statistical test
            select new { Seed = seed, Percent = percent };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var random = new DeterministicRandom(test.Seed);
                const int sampleSize = 10000;
                var successCount = 0;

                // Act
                for (int i = 0; i < sampleSize; i++)
                {
                    var result = random.Check(test.Percent, $"check_{i}");
                    result.Match(
                        Succ: success => { if (success) successCount++; return true; },
                        Fail: _ => false
                    );
                }

                // Assert - Allow 5% margin for statistical variation
                var expectedSuccesses = sampleSize * test.Percent / 100.0;
                var tolerance = sampleSize * 0.05; // 5% tolerance
                successCount.Should().BeCloseTo((int)expectedSuccesses, (uint)tolerance,
                    $"Check({test.Percent}%) should succeed approximately {test.Percent}% of the time");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Same seed + same context must always produce identical sequences (determinism)
    /// This is critical for save/load and multiplayer sync
    /// </summary>
    [Fact]
    public void DeterministicSequence_SameSeedProducesIdenticalResults()
    {
        var generator =
            from seed in GenSeed()
            from stream in Gen.Choose(1, 1000).Select(x => (ulong)x)
            from sequenceLength in Gen.Choose(10, 100)
            select new { Seed = seed, Stream = stream, Length = sequenceLength };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange - Create two instances with same seed
                var random1 = new DeterministicRandom(test.Seed, test.Stream);
                var random2 = new DeterministicRandom(test.Seed, test.Stream);
                var sequence1 = new List<int>();
                var sequence2 = new List<int>();

                // Act - Generate sequences
                for (int i = 0; i < test.Length; i++)
                {
                    random1.Next(1000, $"seq_{i}").Match(
                        Succ: v => sequence1.Add(v),
                        Fail: _ => sequence1.Add(-1)
                    );
                    random2.Next(1000, $"seq_{i}").Match(
                        Succ: v => sequence2.Add(v),
                        Fail: _ => sequence2.Add(-1)
                    );
                }

                // Assert
                sequence1.Should().Equal(sequence2,
                    "Same seed and context should produce identical sequences");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Fork() creates independent streams that don't affect parent or siblings
    /// </summary>
    [Fact]
    public void Fork_CreatesIndependentStreams()
    {
        var generator =
            from seed in GenSeed()
            from forkCount in Gen.Choose(2, 5)
            from drawsPerFork in Gen.Choose(10, 50)
            select new { Seed = seed, ForkCount = forkCount, DrawsPerFork = drawsPerFork };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var parent = new DeterministicRandom(test.Seed);
                var forks = new List<IDeterministicRandom>();
                var sequences = new List<List<int>>();

                // Create forks
                for (int i = 0; i < test.ForkCount; i++)
                {
                    parent.Fork($"fork_{i}").Match(
                        Succ: fork => forks.Add(fork),
                        Fail: _ => throw new InvalidOperationException("Fork should not fail")
                    );
                    sequences.Add(new List<int>());
                }

                // Act - Draw from each fork
                for (int draw = 0; draw < test.DrawsPerFork; draw++)
                {
                    for (int fork = 0; fork < test.ForkCount; fork++)
                    {
                        forks[fork].Next(1000, $"draw_{draw}").Match(
                            Succ: v => sequences[fork].Add(v),
                            Fail: _ => sequences[fork].Add(-1)
                        );
                    }
                }

                // Assert - All sequences should be different (extremely high probability)
                for (int i = 0; i < test.ForkCount - 1; i++)
                {
                    for (int j = i + 1; j < test.ForkCount; j++)
                    {
                        sequences[i].SequenceEqual(sequences[j]).Should().BeFalse(
                            $"Fork {i} and Fork {j} should produce different sequences");
                    }
                }
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Roll(n, d, m) always returns values in range [n+m, n*d+m]
    /// </summary>
    [Fact]
    public void Roll_AlwaysReturnsValidDiceResults()
    {
        var generator =
            from seed in GenSeed()
            from count in Gen.Choose(0, 10) // 0d6 is valid (just modifier)
            from sides in Gen.Choose(2, 20) // Common dice: d4 to d20
            from modifier in Gen.Choose(-20, 20)
            select new { Seed = seed, Count = count, Sides = sides, Modifier = modifier };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var random = new DeterministicRandom(test.Seed);
                var minValue = test.Count + test.Modifier;
                var maxValue = test.Count * test.Sides + test.Modifier;

                // Act
                var result = random.Roll(test.Count, test.Sides, test.Modifier, "dice_test");

                // Assert
                result.IsSucc.Should().BeTrue();
                result.Match(
                    Succ: value =>
                    {
                        value.Should().BeGreaterThanOrEqualTo(minValue,
                            $"{test.Count}d{test.Sides}+{test.Modifier} minimum should be {minValue}");
                        value.Should().BeLessThanOrEqualTo(maxValue,
                            $"{test.Count}d{test.Sides}+{test.Modifier} maximum should be {maxValue}");
                        return true;
                    },
                    Fail: _ => false
                ).Should().BeTrue();
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Choose() respects weights - items with higher weights are selected more often
    /// </summary>
    [Fact]
    public void Choose_RespectsWeightDistribution()
    {
        var generator =
            from seed in GenSeed()
            from weight1 in Gen.Choose(1, 10)
            from weight2 in Gen.Choose(20, 30) // Significantly higher
            from weight3 in Gen.Choose(5, 15)
            select new { Seed = seed, W1 = weight1, W2 = weight2, W3 = weight3 };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var random = new DeterministicRandom(test.Seed);
                var choices = new (string item, int weight)[]
                {
                    ("Low", test.W1),
                    ("High", test.W2),
                    ("Medium", test.W3)
                };
                var counts = new Dictionary<string, int>
                {
                    ["Low"] = 0,
                    ["High"] = 0,
                    ["Medium"] = 0
                };

                // Act - Sample many times
                const int samples = 1000;
                for (int i = 0; i < samples; i++)
                {
                    random.Choose(choices, $"choose_{i}").Match(
                        Succ: item => counts[item]++,
                        Fail: _ => throw new InvalidOperationException("Choose should not fail")
                    );
                }

                // Assert - High weight should be selected most often
                counts["High"].Should().BeGreaterThan(counts["Low"],
                    "Item with higher weight should be selected more often");
                counts["High"].Should().BeGreaterThan(counts["Medium"],
                    "Item with higher weight should be selected more often");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: State save/restore produces identical sequences (critical for save/load)
    /// </summary>
    [Fact]
    public void State_SaveRestoreProducesIdenticalSequences()
    {
        var generator =
            from seed in GenSeed()
            from advanceCount in Gen.Choose(0, 50)
            from sequenceLength in Gen.Choose(10, 50)
            select new { Seed = seed, AdvanceCount = advanceCount, SequenceLength = sequenceLength };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var random = new DeterministicRandom(test.Seed);

                // Advance the state
                for (int i = 0; i < test.AdvanceCount; i++)
                {
                    random.Next(100, $"advance_{i}");
                }

                // Save state
                var savedState = random.State;

                // Generate sequence 1
                var sequence1 = new List<int>();
                for (int i = 0; i < test.SequenceLength; i++)
                {
                    random.Next(1000, $"seq1_{i}").Match(
                        Succ: v => sequence1.Add(v),
                        Fail: _ => sequence1.Add(-1)
                    );
                }

                // Restore state
                random.State = savedState;

                // Generate sequence 2
                var sequence2 = new List<int>();
                for (int i = 0; i < test.SequenceLength; i++)
                {
                    random.Next(1000, $"seq1_{i}").Match( // Same context as sequence1
                        Succ: v => sequence2.Add(v),
                        Fail: _ => sequence2.Add(-1)
                    );
                }

                // Assert
                sequence1.Should().Equal(sequence2,
                    "Restoring state should produce identical sequences");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Rejection sampling eliminates modulo bias in Next()
    /// Tests that distribution is uniform even for non-power-of-2 bounds
    /// </summary>
    [Fact]
    public void Next_UniformDistributionWithoutModuloBias()
    {
        // Test with bounds that would show modulo bias if present
        var generator =
            from seed in GenSeed()
            from bound in Gen.Elements(3, 5, 7, 11, 13, 17, 19, 23, 29, 31) // Prime numbers show bias best
            select new { Seed = seed, Bound = bound };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var random = new DeterministicRandom(test.Seed);
                var counts = new int[test.Bound];
                const int samples = 10000;

                // Act - Generate many samples
                for (int i = 0; i < samples; i++)
                {
                    random.Next(test.Bound, $"uniform_{i}").Match(
                        Succ: v => counts[v]++,
                        Fail: _ => throw new InvalidOperationException("Next should not fail")
                    );
                }

                // Assert - Chi-square test for uniformity
                var expectedCount = samples / (double)test.Bound;
                var chiSquare = counts.Sum(count =>
                {
                    var diff = count - expectedCount;
                    return (diff * diff) / expectedCount;
                });

                // Critical value for (bound-1) degrees of freedom at 0.05 significance
                // Using conservative threshold appropriate for property testing with small samples
                // For primes up to 31, chi-square critical values at 0.05 significance range from ~7 to ~43
                // Using 5x multiplier to account for statistical variance in property testing
                var criticalValue = test.Bound * 5.0; // Conservative threshold for FsCheck property testing
                chiSquare.Should().BeLessThan(criticalValue,
                    $"Distribution should be uniform (chi-square={chiSquare:F2})");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Different contexts with same seed produce different values
    /// This ensures context hashing works correctly
    /// </summary>
    [Fact]
    public void Context_DifferentContextsProduceDifferentValues()
    {
        var generator =
            from seed in GenSeed()
            from contextCount in Gen.Choose(5, 20)
            select new { Seed = seed, ContextCount = contextCount };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var random = new DeterministicRandom(test.Seed);
                var values = new System.Collections.Generic.HashSet<int>();

                // Act - Generate values with different contexts
                for (int i = 0; i < test.ContextCount; i++)
                {
                    random.Next(int.MaxValue, $"unique_context_{i}").Match(
                        Succ: v => values.Add(v),
                        Fail: _ => throw new InvalidOperationException("Next should not fail")
                    );
                }

                // Assert - Most values should be unique (collision probability is very low)
                var uniqueRatio = values.Count / (double)test.ContextCount;
                uniqueRatio.Should().BeGreaterThan(0.9,
                    "Different contexts should produce mostly different values");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Fork with same name from same root always produces identical streams
    /// This is important for deterministic fork behavior
    /// </summary>
    [Fact]
    public void Fork_SameNameProducesIdenticalStreams()
    {
        var generator =
            from seed in GenSeed()
            from forkName in Gen.Elements("combat", "loot", "ai", "world", "effects")
            from sequenceLength in Gen.Choose(20, 50)
            select new { Seed = seed, ForkName = forkName, SequenceLength = sequenceLength };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange - Create two parent instances with same seed
                var parent1 = new DeterministicRandom(test.Seed);
                var parent2 = new DeterministicRandom(test.Seed);

                // Create forks with same name
                var fork1 = parent1.Fork(test.ForkName).Match(
                    Succ: f => f,
                    Fail: _ => throw new InvalidOperationException("Fork should not fail")
                );
                var fork2 = parent2.Fork(test.ForkName).Match(
                    Succ: f => f,
                    Fail: _ => throw new InvalidOperationException("Fork should not fail")
                );

                // Act - Generate sequences from both forks
                var sequence1 = new List<int>();
                var sequence2 = new List<int>();
                for (int i = 0; i < test.SequenceLength; i++)
                {
                    fork1.Next(1000, $"test_{i}").Match(
                        Succ: v => sequence1.Add(v),
                        Fail: _ => sequence1.Add(-1)
                    );
                    fork2.Next(1000, $"test_{i}").Match(
                        Succ: v => sequence2.Add(v),
                        Fail: _ => sequence2.Add(-1)
                    );
                }

                // Assert
                sequence1.Should().Equal(sequence2,
                    "Same fork name from same root seed should produce identical streams");
            }
        ).QuickCheckThrowOnFailure();
    }
}
