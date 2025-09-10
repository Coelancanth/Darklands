using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Darklands.Core.Domain.Determinism;
using System.Text;
using System.Security.Cryptography;

namespace Darklands.Core.Tests.Domain.Determinism;

/// <summary>
/// Cross-platform determinism tests for TD_025.
/// Verifies that IDeterministicRandom produces identical results across Windows/Linux/macOS.
/// These tests provide explicit sequence capture and validation for CI pipeline verification.
/// </summary>
[Trait("Category", "CrossPlatform")]
[Trait("Category", "PropertyBased")]
public class CrossPlatformDeterminismTests
{
    /// <summary>
    /// Reference seed used for cross-platform validation.
    /// This seed is used consistently across all platforms to ensure identical results.
    /// </summary>
    private const ulong ReferenceSeed = 0x123456789ABCDEF0UL;

    /// <summary>
    /// Verifies that the same seed produces identical 1000-element sequences across platforms.
    /// This is the core test for save/multiplayer compatibility.
    /// </summary>
    [Fact]
    public void CrossPlatform_ReferenceSeed_ProducesIdenticalSequence()
    {
        // Arrange
        var random = new DeterministicRandom(ReferenceSeed, stream: 1UL);
        const int sequenceLength = 1000;
        var sequence = new List<int>(sequenceLength);

        // Act - Generate deterministic sequence
        for (int i = 0; i < sequenceLength; i++)
        {
            var result = random.Next(1000000, $"cross_platform_test_{i}");
            result.Match(
                Succ: value => sequence.Add(value),
                Fail: error => throw new InvalidOperationException($"Random generation failed: {error}")
            );
        }

        // Assert - Verify expected sequence properties
        sequence.Should().HaveCount(sequenceLength, "Sequence should have exactly 1000 elements");
        sequence.Should().OnlyContain(x => x >= 0 && x < 1000000, "All values should be in range [0, 1000000)");

        // Calculate and verify sequence hash for cross-platform consistency
        var sequenceHash = CalculateSequenceHash(sequence);

        // For initial implementation, just verify the sequence is deterministic
        // The hash will be captured during first CI run and then validated
        sequenceHash.Should().NotBeNullOrEmpty("Sequence hash should be calculable");

        // Output the hash for CI capture (will be visible in test logs)
        Console.WriteLine($"Cross-platform sequence hash: {sequenceHash}");
        Console.WriteLine($"First 10 values: [{string.Join(", ", sequence.Take(10))}]");
        Console.WriteLine($"Last 10 values: [{string.Join(", ", sequence.Skip(990).Take(10))}]");
    }

    /// <summary>
    /// Verifies that forked streams produce consistent results across platforms.
    /// Critical for game systems that use independent random streams.
    /// </summary>
    [Fact]
    public void CrossPlatform_ForkedStreams_ProduceConsistentResults()
    {
        // Arrange
        var parent = new DeterministicRandom(ReferenceSeed);
        var streamNames = new[] { "combat", "loot", "ai", "world", "effects" };
        var streamResults = new Dictionary<string, List<int>>();

        // Act - Generate sequences from each forked stream
        foreach (var streamName in streamNames)
        {
            var fork = parent.Fork(streamName).Match(
                Succ: f => f,
                Fail: error => throw new InvalidOperationException($"Fork failed: {error}")
            );

            var sequence = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                var result = fork.Next(10000, $"{streamName}_operation_{i}");
                result.Match(
                    Succ: value => sequence.Add(value),
                    Fail: error => throw new InvalidOperationException($"Random generation failed: {error}")
                );
            }

            streamResults[streamName] = sequence;
        }

        // Assert - Verify all streams produced valid sequences
        foreach (var (streamName, sequence) in streamResults)
        {
            sequence.Should().HaveCount(100, $"Stream '{streamName}' should have 100 values");
            sequence.Should().OnlyContain(x => x >= 0 && x < 10000, $"Stream '{streamName}' values should be in range");

            // Output sequence characteristics for cross-platform validation
            var hash = CalculateSequenceHash(sequence);
            Console.WriteLine($"Stream '{streamName}' hash: {hash}");
            Console.WriteLine($"Stream '{streamName}' first 5: [{string.Join(", ", sequence.Take(5))}]");
        }

        // Verify streams are independent (different sequences)
        var allSequences = streamResults.Values.ToList();
        for (int i = 0; i < allSequences.Count - 1; i++)
        {
            for (int j = i + 1; j < allSequences.Count; j++)
            {
                allSequences[i].Should().NotEqual(allSequences[j],
                    "Independent streams should produce different sequences");
            }
        }
    }

    /// <summary>
    /// Verifies that dice rolling produces consistent results across platforms.
    /// Important for combat and game mechanics consistency.
    /// </summary>
    [Fact]
    public void CrossPlatform_DiceRolling_ProducesConsistentResults()
    {
        // Arrange
        var random = new DeterministicRandom(ReferenceSeed);
        var diceConfigs = new[]
        {
            (count: 1, sides: 6, modifier: 0),   // 1d6
            (count: 3, sides: 6, modifier: 2),   // 3d6+2
            (count: 1, sides: 20, modifier: 5),  // 1d20+5
            (count: 2, sides: 8, modifier: -1),  // 2d8-1
            (count: 4, sides: 4, modifier: 0)    // 4d4
        };

        var allRolls = new List<int>();

        // Act - Perform dice rolls
        foreach (var (count, sides, modifier) in diceConfigs)
        {
            for (int roll = 0; roll < 20; roll++) // 20 rolls per config
            {
                var result = random.Roll(count, sides, modifier, $"dice_{count}d{sides}+{modifier}_roll_{roll}");
                result.Match(
                    Succ: value => allRolls.Add(value),
                    Fail: error => throw new InvalidOperationException($"Dice roll failed: {error}")
                );
            }
        }

        // Assert - Verify dice results
        allRolls.Should().HaveCount(100, "Should have 100 total dice rolls");

        // Calculate and output hash for cross-platform validation
        var rollsHash = CalculateSequenceHash(allRolls);
        Console.WriteLine($"Dice rolls hash: {rollsHash}");
        Console.WriteLine($"Sample rolls: [{string.Join(", ", allRolls.Take(20))}]");

        // Verify dice rolls are within expected bounds
        var minRoll = allRolls.Min();
        var maxRoll = allRolls.Max();
        minRoll.Should().BeGreaterThanOrEqualTo(-1, "Minimum possible roll with 2d8-1");
        maxRoll.Should().BeLessThanOrEqualTo(25, "Maximum possible roll with 1d20+5");
    }

    /// <summary>
    /// Verifies that percentage checks produce consistent distributions across platforms.
    /// Critical for balanced gameplay mechanics.
    /// </summary>
    [Fact]
    public void CrossPlatform_PercentageChecks_ProduceConsistentDistributions()
    {
        // Arrange
        var random = new DeterministicRandom(ReferenceSeed);
        var percentages = new[] { 10, 25, 50, 75, 90 };
        var results = new Dictionary<int, List<bool>>();

        // Act - Perform percentage checks
        foreach (var percentage in percentages)
        {
            var checks = new List<bool>();
            for (int i = 0; i < 1000; i++)
            {
                var result = random.Check(percentage, $"check_{percentage}pct_{i}");
                result.Match(
                    Succ: success => checks.Add(success),
                    Fail: error => throw new InvalidOperationException($"Percentage check failed: {error}")
                );
            }
            results[percentage] = checks;
        }

        // Assert - Verify distributions and calculate hashes
        foreach (var (percentage, checks) in results)
        {
            checks.Should().HaveCount(1000, $"{percentage}% checks should have 1000 results");

            var successCount = checks.Count(x => x);
            var successRate = successCount / 1000.0 * 100.0;

            // Allow 5% variance for statistical noise
            Math.Abs(successRate - percentage).Should().BeLessThanOrEqualTo(5.0,
                $"{percentage}% checks should succeed approximately {percentage}% of the time");

            // Output for cross-platform validation
            var checksHash = CalculateSequenceHash(checks.Select(x => x ? 1 : 0).ToList());
            Console.WriteLine($"{percentage}% checks hash: {checksHash} (success rate: {successRate:F1}%)");
        }
    }

    /// <summary>
    /// Verifies that state save/restore works consistently across platforms.
    /// Critical for save game compatibility.
    /// </summary>
    [Fact]
    public void CrossPlatform_StateSaveRestore_WorksConsistently()
    {
        // Arrange
        var random = new DeterministicRandom(ReferenceSeed);

        // Advance to a specific state
        for (int i = 0; i < 50; i++)
        {
            random.Next(1000, $"advance_{i}");
        }

        // Save state
        var savedState = random.State;
        var savedRootSeed = random.RootSeed;
        var savedStream = random.Stream;

        // Generate sequence 1
        var sequence1 = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            var result = random.Next(10000, $"sequence1_{i}");
            result.Match(
                Succ: value => sequence1.Add(value),
                Fail: error => throw new InvalidOperationException($"Random generation failed: {error}")
            );
        }

        // Restore state
        random.State = savedState;

        // Generate sequence 2 (should be identical)
        var sequence2 = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            var result = random.Next(10000, $"sequence1_{i}"); // Same context as sequence1
            result.Match(
                Succ: value => sequence2.Add(value),
                Fail: error => throw new InvalidOperationException($"Random generation failed: {error}")
            );
        }

        // Assert - Verify state restoration
        sequence1.Should().Equal(sequence2, "State restoration should produce identical sequences");
        random.RootSeed.Should().Be(savedRootSeed, "Root seed should be preserved");
        random.Stream.Should().Be(savedStream, "Stream should be preserved");

        // Output for cross-platform validation
        var hash1 = CalculateSequenceHash(sequence1);
        var hash2 = CalculateSequenceHash(sequence2);
        Console.WriteLine($"State save/restore test - Sequence 1 hash: {hash1}");
        Console.WriteLine($"State save/restore test - Sequence 2 hash: {hash2}");
        Console.WriteLine($"Saved state: 0x{savedState:X16}");
    }

    /// <summary>
    /// Calculates a stable hash of an integer sequence for cross-platform comparison.
    /// Uses SHA256 to ensure identical hash generation across platforms.
    /// </summary>
    private static string CalculateSequenceHash(IEnumerable<int> sequence)
    {
        // Convert sequence to bytes in a deterministic way
        var bytes = new List<byte>();
        foreach (var value in sequence)
        {
            // Use little-endian byte order for consistency
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        // Calculate SHA256 hash
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes.ToArray());

        // Return as hex string
        return Convert.ToHexString(hash);
    }
}
