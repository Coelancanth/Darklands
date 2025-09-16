using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Domain.Determinism;

/// <summary>
/// Extension methods providing deterministic operations for collections and sequences.
/// Implements stable sorting, deterministic shuffling, and other operations required by ADR-004.
/// 
/// All methods ensure:
/// - Identical results across platforms and .NET versions
/// - Stable ordering for equal elements
/// - No dependence on object hash codes or memory addresses
/// - Reproducible behavior from deterministic random sources
/// </summary>
public static class DeterministicExtensions
{
    /// <summary>
    /// Stable sort that preserves relative order of equal elements.
    /// Required by ADR-004 to replace unstable OrderBy() operations.
    /// 
    /// Unlike LINQ's OrderBy(), this guarantees consistent ordering when key values are equal,
    /// preventing non-deterministic behavior from internal sort algorithm differences.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <typeparam name="TKey">Key type for sorting</typeparam>
    /// <param name="source">Elements to sort</param>
    /// <param name="keySelector">Function to extract sort key from element</param>
    /// <returns>Stably sorted sequence</returns>
    public static IEnumerable<T> OrderByStable<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
        where TKey : IComparable<TKey>
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        return source
            .Select((item, index) => (item, index))
            .OrderBy(x => keySelector(x.item))
            .ThenBy(x => x.index)  // Stabilize with original index
            .Select(x => x.item);
    }

    /// <summary>
    /// Stable sort with secondary key for tie-breaking.
    /// Provides deterministic ordering when primary keys are equal.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <typeparam name="TKey1">Primary key type</typeparam>
    /// <typeparam name="TKey2">Secondary key type</typeparam>
    /// <param name="source">Elements to sort</param>
    /// <param name="keySelector">Primary sort key selector</param>
    /// <param name="thenByKeySelector">Secondary sort key selector for ties</param>
    /// <returns>Stably sorted sequence with deterministic tie-breaking</returns>
    public static IEnumerable<T> OrderByStable<T, TKey1, TKey2>(
        this IEnumerable<T> source,
        Func<T, TKey1> keySelector,
        Func<T, TKey2> thenByKeySelector)
        where TKey1 : IComparable<TKey1>
        where TKey2 : IComparable<TKey2>
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (thenByKeySelector == null) throw new ArgumentNullException(nameof(thenByKeySelector));

        return source
            .Select((item, index) => (item, index))
            .OrderBy(x => keySelector(x.item))
            .ThenBy(x => thenByKeySelector(x.item))
            .ThenBy(x => x.index)  // Final stabilization
            .Select(x => x.item);
    }

    /// <summary>
    /// Deterministic shuffle using Fisher-Yates algorithm with provided random source.
    /// Produces identical shuffle results given same random state and input sequence.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">Elements to shuffle</param>
    /// <param name="random">Deterministic random source</param>
    /// <param name="context">Debug context for random calls</param>
    /// <returns>Success with shuffled list, or Fail with error</returns>
    public static Fin<IList<T>> Shuffle<T>(
        this IEnumerable<T> source,
        IDeterministicRandom random,
        string context)
    {
        if (source == null)
            return FinFail<IList<T>>(Error.New("Source sequence cannot be null"));
        if (random == null)
            return FinFail<IList<T>>(Error.New("Random generator cannot be null"));
        if (string.IsNullOrWhiteSpace(context))
            return FinFail<IList<T>>(Error.New("Context cannot be null or empty"));

        var array = source.ToArray();

        // Fisher-Yates shuffle with deterministic random
        for (int i = array.Length - 1; i > 0; i--)
        {
            var jResult = random.Next(i + 1, $"{context}_shuffle_{i}");
            if (jResult.IsFail)
                return jResult.Map<IList<T>>(j => array); // Convert error type

            var j = jResult.Match(
                Succ: value => value,
                Fail: _ => 0 // Won't reach due to check above
            );

            // Swap elements
            (array[i], array[j]) = (array[j], array[i]);
        }

        return FinSucc<IList<T>>(array);
    }

    /// <summary>
    /// Select random element from sequence using deterministic random source.
    /// Useful for randomly selecting from collections in a reproducible way.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">Non-empty sequence to select from</param>
    /// <param name="random">Deterministic random source</param>
    /// <param name="context">Debug context for random calls</param>
    /// <returns>Success with selected element, or Fail with error</returns>
    public static Fin<T> SelectRandom<T>(
        this IEnumerable<T> source,
        IDeterministicRandom random,
        string context)
    {
        if (source == null)
            return FinFail<T>(Error.New("Source sequence cannot be null"));
        if (random == null)
            return FinFail<T>(Error.New("Random generator cannot be null"));
        if (string.IsNullOrWhiteSpace(context))
            return FinFail<T>(Error.New("Context cannot be null or empty"));

        var array = source.ToArray();
        if (array.Length == 0)
            return FinFail<T>(Error.New("Cannot select from empty sequence"));

        return random.Next(array.Length, context)
            .Map(index => array[index]);
    }

    /// <summary>
    /// Select multiple random elements without replacement.
    /// Ensures no duplicates in the result while maintaining deterministic behavior.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">Non-empty sequence to select from</param>
    /// <param name="count">Number of elements to select (must be <= source length)</param>
    /// <param name="random">Deterministic random source</param>
    /// <param name="context">Debug context for random calls</param>
    /// <returns>Success with selected elements, or Fail with error</returns>
    public static Fin<IList<T>> SelectRandomMany<T>(
        this IEnumerable<T> source,
        int count,
        IDeterministicRandom random,
        string context)
    {
        if (source == null)
            return FinFail<IList<T>>(Error.New("Source sequence cannot be null"));
        if (random == null)
            return FinFail<IList<T>>(Error.New("Random generator cannot be null"));
        if (string.IsNullOrWhiteSpace(context))
            return FinFail<IList<T>>(Error.New("Context cannot be null or empty"));
        if (count < 0)
            return FinFail<IList<T>>(Error.New($"Count cannot be negative: {count}"));

        var array = source.ToArray();
        if (count > array.Length)
            return FinFail<IList<T>>(Error.New($"Cannot select {count} elements from sequence of length {array.Length}"));

        if (count == 0)
            return FinSucc<IList<T>>(new T[0]);

        // Use shuffle and take first N elements for unbiased selection
        var shuffleResult = array.Shuffle(random, context);
        return shuffleResult.Map(shuffled => (IList<T>)shuffled.Take(count).ToArray());
    }

    /// <summary>
    /// Deterministically partition collection into groups using stable ordering.
    /// Useful for distributing entities across game systems in a reproducible way.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">Elements to partition</param>
    /// <param name="partitionCount">Number of partitions (must be > 0)</param>
    /// <param name="keySelector">Function to generate stable partition key</param>
    /// <returns>Success with partitioned groups, or Fail with error</returns>
    public static Fin<IList<IList<T>>> PartitionDeterministic<T>(
        this IEnumerable<T> source,
        int partitionCount,
        Func<T, string> keySelector)
    {
        if (source == null)
            return FinFail<IList<IList<T>>>(Error.New("Source sequence cannot be null"));
        if (partitionCount <= 0)
            return FinFail<IList<IList<T>>>(Error.New($"Partition count must be > 0: {partitionCount}"));
        if (keySelector == null)
            return FinFail<IList<IList<T>>>(Error.New("Key selector cannot be null"));

        var array = source.ToArray();
        var partitions = new List<T>[partitionCount];

        for (int i = 0; i < partitionCount; i++)
        {
            partitions[i] = new List<T>();
        }

        // Use stable hash for deterministic partitioning
        foreach (var item in array)
        {
            var partitionValue = keySelector(item);
            var hash = DeterministicHash(partitionValue);
            var partition = (int)(hash % (uint)partitionCount);
            partitions[partition].Add(item);
        }

        return FinSucc<IList<IList<T>>>(partitions.Cast<IList<T>>().ToArray());
    }

    /// <summary>
    /// Simple deterministic hash for string keys.
    /// Used for stable partitioning operations.
    /// </summary>
    private static uint DeterministicHash(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        uint hash = 2166136261u; // FNV offset basis (32-bit)
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 16777619u; // FNV prime (32-bit)
        }
        return hash;
    }
}
