using System.Collections.Generic;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Domain.Determinism;

/// <summary>
/// Deterministic random number generator interface following ADR-004.
/// Provides reproducible sequences from a seed with context tracking for debugging desyncs.
/// All operations use rejection sampling and stable algorithms to ensure cross-platform consistency.
/// </summary>
public interface IDeterministicRandom
{
    /// <summary>
    /// Generate next integer in range [0, maxExclusive) using unbiased rejection sampling.
    /// </summary>
    /// <param name="maxExclusive">Exclusive upper bound (must be > 0)</param>
    /// <param name="context">Debug context for tracking desyncs (required, non-empty)</param>
    /// <returns>Success with random value, or Fail with validation error</returns>
    Fin<int> Next(int maxExclusive, string context);

    /// <summary>
    /// Generate next integer in range [min, maxExclusive) using unbiased rejection sampling.
    /// </summary>
    /// <param name="min">Inclusive lower bound</param>
    /// <param name="maxExclusive">Exclusive upper bound (must be > min)</param>
    /// <param name="context">Debug context for tracking desyncs (required, non-empty)</param>
    /// <returns>Success with random value, or Fail with validation error</returns>
    Fin<int> Range(int min, int maxExclusive, string context);

    /// <summary>
    /// Roll dice notation (e.g., "3d6+2" = 3 six-sided dice plus 2).
    /// </summary>
    /// <param name="count">Number of dice (must be >= 0)</param>
    /// <param name="sides">Sides per die (must be > 0)</param>
    /// <param name="modifier">Flat modifier to add to total</param>
    /// <param name="context">Debug context for tracking desyncs (required, non-empty)</param>
    /// <returns>Success with dice total, or Fail with validation error</returns>
    Fin<int> Roll(int count, int sides, int modifier, string context);

    /// <summary>
    /// Success/failure check against percentage (0-100).
    /// </summary>
    /// <param name="percentChance">Chance of success from 0 to 100 inclusive</param>
    /// <param name="context">Debug context for tracking desyncs (required, non-empty)</param>
    /// <returns>Success with true/false result, or Fail with validation error</returns>
    Fin<bool> Check(int percentChance, string context);

    /// <summary>
    /// Choose weighted random element from list.
    /// Uses stable iteration order and validates all weights are positive.
    /// </summary>
    /// <param name="weighted">Non-empty list of (item, weight) pairs with positive weights</param>
    /// <param name="context">Debug context for tracking desyncs (required, non-empty)</param>
    /// <returns>Success with chosen item, or Fail with validation error</returns>
    Fin<T> Choose<T>(IReadOnlyList<(T item, int weight)> weighted, string context);

    /// <summary>
    /// Get/set RNG internal state for saving/loading.
    /// State represents the PCG generator's current position in the sequence.
    /// </summary>
    ulong State { get; set; }

    /// <summary>
    /// Read-only stream identifier (odd) used by PCG step. 
    /// Exposed for diagnostics and save integrity verification.
    /// </summary>
    ulong Stream { get; }

    /// <summary>
    /// Root seed used to derive named forks deterministically.
    /// Exposed for diagnostics and reproducibility verification.
    /// </summary>
    ulong RootSeed { get; }

    /// <summary>
    /// Fork a new independent stream using stable FNV-1a hash.
    /// Creates deterministic sub-streams for different game systems.
    /// </summary>
    /// <param name="streamName">Unique name for this stream (required, non-empty)</param>
    /// <returns>Success with new independent random generator, or Fail with validation error</returns>
    Fin<IDeterministicRandom> Fork(string streamName);
}
