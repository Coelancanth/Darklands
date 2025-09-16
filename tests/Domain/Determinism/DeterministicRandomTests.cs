using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Darklands.Domain.Determinism;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Domain.Determinism;

[Trait("Category", "Phase1")]
public class DeterministicRandomTests
{
    private readonly ILogger<DeterministicRandom> _logger = NullLogger<DeterministicRandom>.Instance;

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var random = new DeterministicRandom(12345UL, 1UL, _logger);

        // Assert
        random.Should().NotBeNull();
        random.RootSeed.Should().Be(12345UL);
        random.Stream.Should().Be(1UL);
    }

    [Fact]
    public void Constructor_EvenStream_MakesOdd()
    {
        // Arrange & Act
        var random = new DeterministicRandom(12345UL, 2UL, _logger);

        // Assert
        random.Stream.Should().Be(3UL); // 2 | 1 = 3
    }

    [Fact]
    public void Next_SameSeed_ProducesIdenticalSequence()
    {
        // Arrange
        var random1 = new DeterministicRandom(12345UL, stream: 1UL);
        var random2 = new DeterministicRandom(12345UL, stream: 1UL);

        // Act & Assert
        for (int i = 0; i < 100; i++)
        {
            var result1 = random1.Next(100, $"test_{i}");
            var result2 = random2.Next(100, $"test_{i}");

            result1.IsSucc.Should().BeTrue($"iteration {i} result1 should succeed");
            result2.IsSucc.Should().BeTrue($"iteration {i} result2 should succeed");

            var value1 = result1.Match(Succ: x => x, Fail: _ => -1);
            var value2 = result2.Match(Succ: x => x, Fail: _ => -1);
            value1.Should().Be(value2, $"iteration {i} should produce same result");
        }
    }

    [Fact]
    public void Next_DifferentSeeds_ProducesDifferentSequences()
    {
        // Arrange
        var random1 = new DeterministicRandom(12345UL);
        var random2 = new DeterministicRandom(54321UL);
        var results1 = new List<int>();
        var results2 = new List<int>();

        // Act
        for (int i = 0; i < 50; i++)
        {
            results1.Add(random1.Next(1000, $"test_{i}").Match(Succ: x => x, Fail: _ => -1));
            results2.Add(random2.Next(1000, $"test_{i}").Match(Succ: x => x, Fail: _ => -1));
        }

        // Assert
        results1.SequenceEqual(results2).Should().BeFalse("different seeds should produce different sequences");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Next_InvalidMaxExclusive_ReturnsFail(int maxExclusive)
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act
        var result = random.Next(maxExclusive, "test");

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("Invalid maxExclusive")
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Next_InvalidContext_ReturnsFail(string context)
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act
        var result = random.Next(100, context);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("Context string cannot be null or empty")
        );
    }

    [Fact]
    public void Next_NullContext_ReturnsFail()
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act
        var result = random.Next(100, null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("Context string cannot be null or empty")
        );
    }

    [Fact]
    public void Next_DistributionTest_IsUnbiased()
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);
        const int sampleSize = 10000;
        const int maxValue = 10;
        var counts = new int[maxValue];

        // Act - Generate large sample
        for (int i = 0; i < sampleSize; i++)
        {
            var result = random.Next(maxValue, $"distribution_test_{i}");
            result.Match(
                Succ: value => counts[value]++,
                Fail: _ => throw new InvalidOperationException("Unexpected failure")
            );
        }

        // Assert - Check distribution is reasonably uniform
        var expectedCount = sampleSize / maxValue;
        var tolerance = expectedCount * 0.1; // 10% tolerance

        for (int i = 0; i < maxValue; i++)
        {
            counts[i].Should().BeCloseTo(expectedCount, (uint)tolerance,
                $"value {i} should appear roughly {expectedCount} times");
        }
    }

    [Fact]
    public void Range_ValidInput_ReturnsValueInRange()
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act & Assert
        for (int i = 0; i < 100; i++)
        {
            var result = random.Range(10, 20, $"range_test_{i}");
            result.IsSucc.Should().BeTrue($"iteration {i} should succeed");
            result.Match(
                Succ: value => value.Should().BeInRange(10, 19, $"iteration {i} should be in range [10, 20)"),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(20, 10)]
    public void Range_InvalidRange_ReturnsFail(int min, int maxExclusive)
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act
        var result = random.Range(min, maxExclusive, "test");

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("Invalid range")
        );
    }

    [Theory]
    [InlineData(0, 6)]    // 0d6 = 0 (+ modifier)
    [InlineData(1, 6)]    // 1d6 = 1-6
    [InlineData(3, 6)]    // 3d6 = 3-18
    [InlineData(2, 10)]   // 2d10 = 2-20
    public void Roll_ValidDice_ReturnsExpectedRange(int count, int sides)
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);
        const int modifier = 5;

        // Act
        var result = random.Roll(count, sides, modifier, "dice_test");

        // Assert
        result.IsSucc.Should().BeTrue($"{count}d{sides}+{modifier} should succeed");
        result.Match(
            Succ: value => value.Should().BeInRange(count + modifier, count * sides + modifier,
                $"{count}d{sides}+{modifier} should be in range [{count + modifier}, {count * sides + modifier}]"),
            Fail: _ => throw new InvalidOperationException("Expected success")
        );
    }

    [Theory]
    [InlineData(-1, 6, 0, "Invalid dice count")]
    [InlineData(1, 0, 0, "Invalid dice sides")]
    [InlineData(1, -1, 0, "Invalid dice sides")]
    public void Roll_InvalidParameters_ReturnsFail(int count, int sides, int modifier, string expectedError)
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act
        var result = random.Roll(count, sides, modifier, "test");

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain(expectedError.Split(' ')[0]) // Check first word
        );
    }

    [Theory]
    [InlineData(0, false)]     // 0% never succeeds
    [InlineData(100, true)]    // 100% always succeeds
    [InlineData(50, null)]     // 50% varies, test later
    public void Check_EdgeCases_BehavesCorrectly(int percent, bool? expectedResult)
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act
        var result = random.Check(percent, "check_test");

        // Assert
        result.IsSucc.Should().BeTrue();
        if (expectedResult.HasValue)
        {
            result.Match(
                Succ: value => value.Should().Be(expectedResult.Value),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    [InlineData(200)]
    public void Check_InvalidPercentage_ReturnsFail(int percent)
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act
        var result = random.Check(percent, "test");

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("Invalid percentage")
        );
    }

    [Fact]
    public void Choose_ValidWeightedList_SelectsFromList()
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);
        var choices = new (string item, int weight)[]
        {
            ("A", 10),
            ("B", 20),
            ("C", 70)  // C should be selected most often
        };

        var results = new Dictionary<string, int>
        {
            ["A"] = 0,
            ["B"] = 0,
            ["C"] = 0
        };

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var result = random.Choose(choices, $"choose_test_{i}");
            result.Match(
                Succ: item => results[item]++,
                Fail: _ => throw new InvalidOperationException("Unexpected failure")
            );
        }

        // Assert
        results["C"].Should().BeGreaterThan(results["A"], "C has higher weight");
        results["C"].Should().BeGreaterThan(results["B"], "C has higher weight");
        results["B"].Should().BeGreaterThan(results["A"], "B has higher weight");
        results.Values.Sum().Should().Be(1000, "all selections should be accounted for");
    }

    [Fact]
    public void Choose_EmptyList_ReturnsFail()
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);
        var emptyChoices = new (string, int)[0];

        // Act
        var result = random.Choose(emptyChoices, "test");

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("non-empty")
        );
    }

    [Fact]
    public void Choose_ZeroOrNegativeWeights_ReturnsFail()
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);
        var invalidChoices = new (string item, int weight)[]
        {
            ("A", 10),
            ("B", 0),   // Invalid: zero weight
            ("C", 20)
        };

        // Act
        var result = random.Choose(invalidChoices, "test");

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("positive")
        );
    }

    [Fact]
    public void Fork_ValidStreamName_CreatesIndependentStream()
    {
        // Arrange
        var parent = new DeterministicRandom(12345UL);

        // Act
        var childResult = parent.Fork("combat");

        // Assert
        childResult.IsSucc.Should().BeTrue();

        childResult.Match(
            Succ: child =>
            {
                child.Should().NotBeNull();
                child.RootSeed.Should().Be(12345UL, "child should have same root seed as original");
                child.Stream.Should().NotBe(parent.Stream, "child should have different stream");

                // Verify independence - parent and child produce different sequences
                var parentValue = parent.Next(1000, "parent_test");
                var childValue = child.Next(1000, "child_test");

                var parentVal = parentValue.Match(Succ: x => x, Fail: _ => -1);
                var childVal = childValue.Match(Succ: x => x, Fail: _ => -2);
                parentVal.Should().NotBe(childVal, "parent and child should produce different values");
            },
            Fail: error => throw new InvalidOperationException($"Fork failed: {error}")
        );
    }

    [Fact]
    public void Fork_SameStreamName_ProducesIdenticalForks()
    {
        // Arrange
        var parent1 = new DeterministicRandom(12345UL);
        var parent2 = new DeterministicRandom(12345UL);

        // Act
        var child1Result = parent1.Fork("combat");
        var child2Result = parent2.Fork("combat");

        // Assert
        child1Result.IsSucc.Should().BeTrue();
        child2Result.IsSucc.Should().BeTrue();

        var child1 = child1Result.Match(Succ: x => x, Fail: _ => throw new Exception());
        var child2 = child2Result.Match(Succ: x => x, Fail: _ => throw new Exception());

        // Same fork name from same root seed should produce identical streams
        child1.Stream.Should().Be(child2.Stream);

        // Verify they produce identical sequences
        for (int i = 0; i < 50; i++)
        {
            var value1 = child1.Next(1000, $"test_{i}");
            var value2 = child2.Next(1000, $"test_{i}");
            var val1 = value1.Match(Succ: x => x, Fail: _ => -1);
            var val2 = value2.Match(Succ: x => x, Fail: _ => -1);
            val1.Should().Be(val2, $"iteration {i} should match");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Fork_InvalidStreamName_ReturnsFail(string streamName)
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act
        var result = random.Fork(streamName);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("Stream name cannot be null or empty")
        );
    }

    [Fact]
    public void Fork_NullStreamName_ReturnsFail()
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Act
        var result = random.Fork(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("Stream name cannot be null or empty")
        );
    }

    [Fact]
    public void State_GetSet_MaintainsGeneratorPosition()
    {
        // Arrange
        var random = new DeterministicRandom(12345UL);

        // Generate some values to advance state
        for (int i = 0; i < 10; i++)
        {
            random.Next(100, $"advance_{i}");
        }

        var savedState = random.State;

        // Generate more values
        var nextValue1 = random.Next(100, "after_save");

        // Reset to saved state
        random.State = savedState;

        // Generate value again
        var nextValue2 = random.Next(100, "after_save");

        // Assert
        var val1 = nextValue1.Match(Succ: x => x, Fail: _ => -1);
        var val2 = nextValue2.Match(Succ: x => x, Fail: _ => -1);
        val1.Should().Be(val2, "resetting state should reproduce same sequence");
    }
}
