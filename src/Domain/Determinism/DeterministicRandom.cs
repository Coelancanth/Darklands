using System.Text;
using Microsoft.Extensions.Logging;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Determinism;

/// <summary>
/// PCG-based deterministic random number generator implementing ADR-004 + TD_026 hardening.
/// 
/// Features:
/// - PCG algorithm: Fast, space-efficient, statistically excellent
/// - Rejection sampling: Eliminates modulo bias for unbiased distributions
/// - Stable hashing: FNV-1a ensures cross-platform consistency
/// - Input validation: Comprehensive bounds checking with meaningful errors
/// - Context tracking: Debug information for desync investigation
/// - Fork streams: Independent deterministic sub-generators
/// 
/// Thread Safety: Not thread-safe. Use separate instances per thread or external synchronization.
/// </summary>
public sealed class DeterministicRandom : IDeterministicRandom
{
    private ulong _state;
    private readonly ulong _stream;
    private readonly ulong _rootSeed;
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a new deterministic random generator.
    /// </summary>
    /// <param name="seed">Initial seed value for the generator</param>
    /// <param name="stream">Stream identifier (will be made odd automatically)</param>
    /// <param name="logger">Optional logger for debugging random calls</param>
    public DeterministicRandom(ulong seed, ulong stream = 1, ILogger? logger = null)
    {
        _state = seed;
        _stream = stream | 1; // Ensure stream is odd (PCG requirement)
        _rootSeed = seed;
        _logger = logger;
    }

    // Internal constructor for forks that preserves original root seed
    private DeterministicRandom(ulong rootSeed, ulong state, ulong stream, ILogger? logger)
    {
        _rootSeed = rootSeed;
        _state = state;
        _stream = stream | 1; // Ensure stream is odd
        _logger = logger;
    }

    public Fin<int> Next(int maxExclusive, string context)
    {
        // Input validation with meaningful errors
        if (string.IsNullOrWhiteSpace(context))
            return FinFail<int>(Error.New("Context string cannot be null or empty"));

        if (maxExclusive <= 0)
            return FinFail<int>(Error.New($"Invalid maxExclusive: {maxExclusive}. Must be > 0"));

        // Unbiased range generation using rejection sampling (eliminates modulo bias)
        var bound = (uint)maxExclusive;
        var threshold = (uint)(-bound) % bound; // Rejection threshold
        uint r;

        do
        {
            r = NextUInt32();
        } while (r < threshold);

        var result = (int)(r % bound);

        // Optional debug logging with context
        _logger?.LogDebug("RNG[{Context}]: {Result}/{Max} (State: {State:X16})",
            context, result, maxExclusive, _state);

        return FinSucc(result);
    }

    public Fin<int> Range(int min, int maxExclusive, string context)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(context))
            return FinFail<int>(Error.New("Context string cannot be null or empty"));

        if (min >= maxExclusive)
            return FinFail<int>(Error.New($"Invalid range: [{min}, {maxExclusive}). Min must be < maxExclusive"));

        var width = (long)maxExclusive - min;
        if (width > int.MaxValue)
            return FinFail<int>(Error.New("Range width exceeds int.MaxValue"));

        return Next((int)width, context).Map(value => min + value);
    }

    public Fin<int> Roll(int count, int sides, int modifier, string context)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(context))
            return FinFail<int>(Error.New("Context string cannot be null or empty"));

        if (count < 0)
            return FinFail<int>(Error.New($"Invalid dice count: {count}. Must be >= 0"));

        if (sides <= 0)
            return FinFail<int>(Error.New($"Invalid dice sides: {sides}. Must be > 0"));

        var total = modifier;

        for (int i = 0; i < count; i++)
        {
            var rollResult = Range(1, sides + 1, $"{context}_d{sides}_{i}");
            if (rollResult.IsFail)
                return rollResult.Map(value => total + value);

            total += rollResult.Match(
                Succ: value => value,
                Fail: _ => 0 // This shouldn't happen due to the check above
            );
        }

        return FinSucc(total);
    }

    public Fin<bool> Check(int percentChance, string context)
    {
        // Input validation with strict bounds
        if (string.IsNullOrWhiteSpace(context))
            return FinFail<bool>(Error.New("Context string cannot be null or empty"));

        if (percentChance < 0 || percentChance > 100)
            return FinFail<bool>(Error.New($"Invalid percentage: {percentChance}. Must be 0-100 inclusive"));

        return Next(100, context).Map(roll => roll < percentChance);
    }

    public Fin<T> Choose<T>(IReadOnlyList<(T item, int weight)> weighted, string context)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(context))
            return FinFail<T>(Error.New("Context string cannot be null or empty"));

        if (weighted == null || weighted.Count == 0)
            return FinFail<T>(Error.New("Weighted list must be non-empty"));

        // Validate all weights are positive
        for (int i = 0; i < weighted.Count; i++)
        {
            if (weighted[i].weight <= 0)
                return FinFail<T>(Error.New($"Invalid weight at index {i}: {weighted[i].weight}. All weights must be positive"));
        }

        // Calculate total weight with overflow protection
        var totalWeightLong = 0L;
        for (int i = 0; i < weighted.Count; i++)
        {
            totalWeightLong += weighted[i].weight;
            if (totalWeightLong > int.MaxValue)
                return FinFail<T>(Error.New("Total weight exceeds int.MaxValue"));
        }

        var totalWeight = (int)totalWeightLong;

        return Next(totalWeight, context).Map(roll =>
        {
            var accumulated = 0;
            foreach (var (item, weight) in weighted)
            {
                accumulated += weight;
                if (roll < accumulated)
                    return item;
            }

            // Safety fallback (should never be reached due to validation)
            return weighted[^1].item;
        });
    }

    public ulong State
    {
        get => _state;
        set => _state = value;
    }

    public ulong Stream => _stream;

    public ulong RootSeed => _rootSeed;

    public Fin<IDeterministicRandom> Fork(string streamName)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(streamName))
            return FinFail<IDeterministicRandom>(Error.New("Stream name cannot be null or empty"));

        // Derive independent stream deterministically using stable FNV-1a hash
        var seed = DeterministicHash64($"{_rootSeed}/seed/{streamName}");
        var streamId = DeterministicHash64($"{_rootSeed}/stream/{streamName}") | 1UL; // Ensure odd

        var forked = new DeterministicRandom(_rootSeed, seed, streamId, _logger);

        _logger?.LogDebug("Forked RNG stream '{StreamName}': seed={Seed:X16}, stream={Stream:X16}",
            streamName, seed, streamId);

        return FinSucc<IDeterministicRandom>(forked);
    }

    /// <summary>
    /// Stable 64-bit FNV-1a hash over UTF-8 bytes.
    /// Deterministic across processes, platforms, and .NET versions.
    /// Replaces string.GetHashCode() which is unstable.
    /// </summary>
    private static ulong DeterministicHash64(string text)
    {
        const ulong offsetBasis = 14695981039346656037UL; // FNV offset basis
        const ulong prime = 1099511628211UL; // FNV prime

        var hash = offsetBasis;
        var bytes = Encoding.UTF8.GetBytes(text);

        for (int i = 0; i < bytes.Length; i++)
        {
            hash ^= bytes[i];
            hash *= prime;
        }

        return hash;
    }

    /// <summary>
    /// Generate next 32-bit value using PCG algorithm.
    /// PCG (Permuted Congruential Generator) provides excellent statistical properties
    /// with minimal state and high performance.
    /// </summary>
    private uint NextUInt32()
    {
        var oldState = _state;

        // PCG advance function: state = state * multiplier + increment
        _state = oldState * 6364136223846793005UL + _stream;

        // PCG output function: permute the state for better distribution
        var xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        var rot = (int)(oldState >> 59);

        // Rotate right for final permutation
        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }
}
