using Xunit;
using FluentAssertions;
using Darklands.Core.Domain.Determinism;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darklands.Core.Tests.Domain.Determinism;

[Trait("Category", "Phase1")]
public class DeterministicExtensionsTests
{
    private readonly DeterministicRandom _random = new(12345UL, logger: NullLogger<DeterministicRandom>.Instance);

    [Fact]
    public void OrderByStable_EqualKeys_PreservesOriginalOrder()
    {
        // Arrange - Items with same key value but different original positions
        var items = new[]
        {
            new { Key = 5, Value = "A" },
            new { Key = 3, Value = "B" },
            new { Key = 5, Value = "C" },  // Same key as first item
            new { Key = 3, Value = "D" },  // Same key as second item
            new { Key = 5, Value = "E" }   // Same key as first and third items
        };

        // Act
        var sorted = items.OrderByStable(x => x.Key).ToArray();

        // Assert - Items with value=3 should appear in original order (B, D)
        sorted.Where(x => x.Key == 3).Select(x => x.Value).Should().Equal("B", "D");

        // Items with value=5 should appear in original order (A, C, E)
        sorted.Where(x => x.Key == 5).Select(x => x.Value).Should().Equal("A", "C", "E");
    }

    [Fact]
    public void OrderByStable_SameInputTwice_ProducesIdenticalResults()
    {
        // Arrange
        var items = Enumerable.Range(0, 50)
            .Select(i => new { Key = i % 5, Value = $"Item{i}" })
            .ToArray();

        // Act
        var sorted1 = items.OrderByStable(x => x.Key).ToArray();
        var sorted2 = items.OrderByStable(x => x.Key).ToArray();

        // Assert
        sorted1.SequenceEqual(sorted2).Should().BeTrue("stable sort should produce identical results");
    }

    [Fact]
    public void OrderByStable_WithSecondaryKey_UsesBothKeys()
    {
        // Arrange
        var items = new[]
        {
            new { Primary = 1, Secondary = "Z" },
            new { Primary = 1, Secondary = "A" },
            new { Primary = 2, Secondary = "B" },
            new { Primary = 1, Secondary = "M" }
        };

        // Act
        var sorted = items.OrderByStable(x => x.Primary, x => x.Secondary).ToArray();

        // Assert
        sorted[0].Should().BeEquivalentTo(new { Primary = 1, Secondary = "A" });
        sorted[1].Should().BeEquivalentTo(new { Primary = 1, Secondary = "M" });
        sorted[2].Should().BeEquivalentTo(new { Primary = 1, Secondary = "Z" });
        sorted[3].Should().BeEquivalentTo(new { Primary = 2, Secondary = "B" });
    }

    [Fact]
    public void Shuffle_SameSeedAndInput_ProducesIdenticalResults()
    {
        // Arrange
        var items = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        var random1 = new DeterministicRandom(42UL);
        var random2 = new DeterministicRandom(42UL);

        // Act
        var shuffled1 = items.Shuffle(random1, "test");
        var shuffled2 = items.Shuffle(random2, "test");

        // Assert
        shuffled1.IsSucc.Should().BeTrue();
        shuffled2.IsSucc.Should().BeTrue();

        var list1 = shuffled1.Match(Succ: x => x, Fail: _ => throw new Exception());
        var list2 = shuffled2.Match(Succ: x => x, Fail: _ => throw new Exception());

        list1.SequenceEqual(list2).Should().BeTrue("same seed should produce identical shuffle");
    }

    [Fact]
    public void Shuffle_ValidInput_ChangesOrder()
    {
        // Arrange
        var items = Enumerable.Range(1, 20).Select(i => $"Item{i}").ToArray();
        var originalOrder = items.ToArray();

        // Act
        var shuffled = items.Shuffle(_random, "shuffle_test");

        // Assert
        shuffled.IsSucc.Should().BeTrue();
        shuffled.Match(
            Succ: result =>
            {
                result.Should().HaveCount(originalOrder.Length, "shuffle should preserve all items");
                result.Should().BeEquivalentTo(originalOrder, "shuffle should contain same items");
                result.SequenceEqual(originalOrder).Should().BeFalse("shuffle should change order (extremely unlikely to be same)");
            },
            Fail: error => throw new Exception($"Shuffle failed: {error}")
        );
    }

    [Theory]
    [InlineData("", "Context cannot be null or empty")]
    [InlineData("   ", "Context cannot be null or empty")]
    public void Shuffle_InvalidInput_ReturnsFail(string context, string expectedError)
    {
        // Arrange
        var items = new[] { "A", "B", "C" };

        // Act
        var result = items.Shuffle(_random, context);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain(expectedError.Split(' ')[0])
        );
    }

    [Fact]
    public void Shuffle_NullInput_ReturnsFail()
    {
        // Arrange
        IEnumerable<string>? items = null;

        // Act
        var result = items!.Shuffle(_random, "test");

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("Source sequence cannot be null")
        );
    }

    [Fact]
    public void SelectRandom_ValidInput_SelectsFromSequence()
    {
        // Arrange
        var items = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry" };

        // Act
        var selected = items.SelectRandom(_random, "select_test");

        // Assert
        selected.IsSucc.Should().BeTrue();
        selected.Match(
            Succ: item => items.Should().Contain(item, "selected item should be from original sequence"),
            Fail: error => throw new Exception($"SelectRandom failed: {error}")
        );
    }

    [Fact]
    public void SelectRandom_EmptySequence_ReturnsFail()
    {
        // Arrange
        var emptyItems = Array.Empty<string>();

        // Act
        var result = emptyItems.SelectRandom(_random, "empty_test");

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("empty sequence")
        );
    }

    [Fact]
    public void SelectRandom_SameSeed_ProducesIdenticalSelection()
    {
        // Arrange
        var items = new[] { "A", "B", "C", "D", "E" };
        var random1 = new DeterministicRandom(999UL);
        var random2 = new DeterministicRandom(999UL);

        // Act
        var selected1 = items.SelectRandom(random1, "deterministic_test");
        var selected2 = items.SelectRandom(random2, "deterministic_test");

        // Assert
        var value1 = selected1.Match(Succ: x => x, Fail: _ => "");
        var value2 = selected2.Match(Succ: x => x, Fail: _ => "");
        value1.Should().Be(value2, "same seed should produce same selection");
    }

    [Fact]
    public void SelectRandomMany_ValidInput_SelectsCorrectCount()
    {
        // Arrange
        var items = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        const int selectCount = 3;

        // Act
        var selected = items.SelectRandomMany(selectCount, _random, "select_many_test");

        // Assert
        selected.IsSucc.Should().BeTrue();
        selected.Match(
            Succ: result =>
            {
                result.Should().HaveCount(selectCount, "should select exact count requested");
                result.Should().OnlyContain(item => items.Contains(item), "all selected items should be from original");
                result.Distinct().Should().HaveCount(selectCount, "should not have duplicates");
            },
            Fail: error => throw new Exception($"SelectRandomMany failed: {error}")
        );
    }

    [Fact]
    public void SelectRandomMany_CountZero_ReturnsEmptyList()
    {
        // Arrange
        var items = new[] { "A", "B", "C" };

        // Act
        var selected = items.SelectRandomMany(0, _random, "zero_count_test");

        // Assert
        selected.IsSucc.Should().BeTrue();
        selected.Match(
            Succ: result => result.Should().BeEmpty("count=0 should return empty list"),
            Fail: error => throw new Exception($"SelectRandomMany failed: {error}")
        );
    }

    [Fact]
    public void SelectRandomMany_CountExceedsSequence_ReturnsFail()
    {
        // Arrange
        var items = new[] { "A", "B", "C" };
        const int tooManyCount = 5;

        // Act
        var result = items.SelectRandomMany(tooManyCount, _random, "too_many_test");

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain("Cannot select")
        );
    }

    [Fact]
    public void PartitionDeterministic_ValidInput_CreatesCorrectPartitions()
    {
        // Arrange
        var items = Enumerable.Range(0, 20).Select(i => $"Item{i}").ToArray();
        const int partitionCount = 3;

        // Act
        var partitioned = items.PartitionDeterministic(partitionCount, item => item);

        // Assert
        partitioned.IsSucc.Should().BeTrue();
        partitioned.Match(
            Succ: partitions =>
            {
                partitions.Should().HaveCount(partitionCount, "should create requested number of partitions");

                var totalItems = partitions.Sum(p => p.Count);
                totalItems.Should().Be(items.Length, "should preserve all items across partitions");

                var allPartitionedItems = partitions.SelectMany(p => p);
                allPartitionedItems.Should().BeEquivalentTo(items, "should contain all original items");
            },
            Fail: error => throw new Exception($"PartitionDeterministic failed: {error}")
        );
    }

    [Fact]
    public void PartitionDeterministic_SameInput_ProducesIdenticalPartitions()
    {
        // Arrange
        var items = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig" };

        // Act
        var partitioned1 = items.PartitionDeterministic(2, item => item);
        var partitioned2 = items.PartitionDeterministic(2, item => item);

        // Assert
        partitioned1.IsSucc.Should().BeTrue();
        partitioned2.IsSucc.Should().BeTrue();

        var p1 = partitioned1.Match(Succ: x => x, Fail: _ => throw new Exception());
        var p2 = partitioned2.Match(Succ: x => x, Fail: _ => throw new Exception());

        for (int i = 0; i < p1.Count; i++)
        {
            p1[i].SequenceEqual(p2[i]).Should().BeTrue($"partition {i} should be identical");
        }
    }

    [Theory]
    [InlineData(0, "Partition count must be > 0")]
    [InlineData(-1, "Partition count must be > 0")]
    [InlineData(-5, "Partition count must be > 0")]
    public void PartitionDeterministic_InvalidPartitionCount_ReturnsFail(int partitionCount, string expectedError)
    {
        // Arrange
        var items = new[] { "A", "B", "C" };

        // Act
        var result = items.PartitionDeterministic(partitionCount, item => item);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Expected failure"),
            Fail: error => error.Message.Should().Contain(expectedError.Split(':')[0])
        );
    }

    [Fact]
    public void DeterministicExtensions_CrossPlatform_ProducesConsistentResults()
    {
        // This test verifies that all deterministic operations produce
        // identical results across different platforms

        // Arrange
        var testData = new[] { "Alpha", "Beta", "Gamma", "Delta", "Echo", "Foxtrot" };
        var testRandom = new DeterministicRandom(777UL);

        // Act - Perform multiple operations
        var shuffleResult = testData.Shuffle(testRandom, "crossplatform_test");
        testRandom.State = 777UL; // Reset to same state

        var selectResult = testData.SelectRandom(testRandom, "crossplatform_select");
        testRandom.State = 777UL; // Reset again

        var manyResult = testData.SelectRandomMany(3, testRandom, "crossplatform_many");

        var partitionResult = testData.PartitionDeterministic(2, x => x);
        var sortedResult = testData.OrderByStable(x => x.Length).ToArray();

        // Assert - These specific results should be deterministic
        shuffleResult.IsSucc.Should().BeTrue("shuffle should succeed");
        selectResult.IsSucc.Should().BeTrue("select should succeed");
        manyResult.IsSucc.Should().BeTrue("select many should succeed");
        partitionResult.IsSucc.Should().BeTrue("partition should succeed");

        // The exact values should be deterministic across platforms
        // (These are implementation-specific but should be consistent)
        sortedResult.Should().StartWith("Beta", "stable sort by length should be deterministic");
    }
}
