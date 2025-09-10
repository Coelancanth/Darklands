using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.GameState;

/// <summary>
/// Complete state of all random number generators in the game.
/// Preserves deterministic gameplay according to ADR-004.
/// 
/// Contains both state (current position) and stream (increment) values
/// for integrity checks and lockstep determinism during replay/save-load.
/// 
/// Uses PCG (Permuted Congruential Generator) concepts:
/// - State: Current internal state of the generator
/// - Stream: Increment value (unique per stream for parallel generators)
/// </summary>
public sealed record RandomState(
    // Primary RNG streams
    ulong MasterState,
    ulong MasterStream,

    // Specialized RNG streams
    ulong CombatState,
    ulong CombatStream,

    ulong LootState,
    ulong LootStream,

    ulong EventState,
    ulong EventStream,

    ulong MovementState,
    ulong MovementStream,

    // Additional streams for extensibility
    ImmutableDictionary<string, RngStreamState> CustomStreams
)
{
    /// <summary>
    /// Creates initial random state from a master seed.
    /// Generates different streams with unique increments for parallel generation.
    /// </summary>
    /// <param name="masterSeed">Master seed for all generators</param>
    /// <returns>New random state with all streams initialized</returns>
    public static RandomState CreateNew(ulong masterSeed)
    {
        // Use different streams (increments) to ensure different sequences
        // Even with the same seed, different streams produce different outputs
        const ulong masterIncrement = 0x14057B7EF767814F;
        const ulong combatIncrement = 0x2BAD01CE9F4D5C21;
        const ulong lootIncrement = 0x5AC635D3EE70CEEF;
        const ulong eventIncrement = 0x9C6526D5FD13B13D;
        const ulong movementIncrement = 0x1ED5AD5FFC42F63B;

        return new RandomState(
            masterSeed,
            masterIncrement,

            masterSeed,
            combatIncrement,

            masterSeed,
            lootIncrement,

            masterSeed,
            eventIncrement,

            masterSeed,
            movementIncrement,

            ImmutableDictionary<string, RngStreamState>.Empty
        );
    }

    /// <summary>
    /// Updates a specific RNG stream state.
    /// Used by the deterministic random service after generating numbers.
    /// </summary>
    /// <param name="streamName">Name of the stream to update</param>
    /// <param name="newState">New state value</param>
    /// <returns>New RandomState with updated stream</returns>
    public RandomState UpdateStream(string streamName, ulong newState)
    {
        return streamName.ToLowerInvariant() switch
        {
            "master" => this with { MasterState = newState },
            "combat" => this with { CombatState = newState },
            "loot" => this with { LootState = newState },
            "event" => this with { EventState = newState },
            "movement" => this with { MovementState = newState },
            _ => UpdateCustomStream(streamName, newState)
        };
    }

    /// <summary>
    /// Gets the current state for a specific stream.
    /// </summary>
    /// <param name="streamName">Name of the stream</param>
    /// <returns>Current state value or None if stream doesn't exist</returns>
    public Option<ulong> GetStreamState(string streamName)
    {
        return streamName.ToLowerInvariant() switch
        {
            "master" => Some(MasterState),
            "combat" => Some(CombatState),
            "loot" => Some(LootState),
            "event" => Some(EventState),
            "movement" => Some(MovementState),
            _ => GetCustomStreamState(streamName)
        };
    }

    /// <summary>
    /// Gets the stream increment for a specific stream.
    /// </summary>
    /// <param name="streamName">Name of the stream</param>
    /// <returns>Stream increment value or None if stream doesn't exist</returns>
    public Option<ulong> GetStreamIncrement(string streamName)
    {
        return streamName.ToLowerInvariant() switch
        {
            "master" => Some(MasterStream),
            "combat" => Some(CombatStream),
            "loot" => Some(LootStream),
            "event" => Some(EventStream),
            "movement" => Some(MovementStream),
            _ => GetCustomStreamIncrement(streamName)
        };
    }

    /// <summary>
    /// Adds a custom RNG stream for mods or new features.
    /// </summary>
    /// <param name="streamName">Name of the new stream</param>
    /// <param name="initialSeed">Initial seed for this stream</param>
    /// <returns>New RandomState with custom stream added</returns>
    public RandomState AddCustomStream(string streamName, ulong initialSeed)
    {
        // Generate a unique increment for this custom stream
        var increment = GenerateStreamIncrement(streamName);
        var streamState = new RngStreamState(initialSeed, increment);

        return this with
        {
            CustomStreams = CustomStreams.SetItem(streamName, streamState)
        };
    }

    /// <summary>
    /// Removes a custom RNG stream.
    /// </summary>
    /// <param name="streamName">Name of the stream to remove</param>
    /// <returns>New RandomState without the specified stream</returns>
    public RandomState RemoveCustomStream(string streamName)
    {
        return this with { CustomStreams = CustomStreams.Remove(streamName) };
    }

    /// <summary>
    /// Gets all available stream names.
    /// </summary>
    /// <returns>Collection of all stream names</returns>
    public IEnumerable<string> GetAllStreamNames()
    {
        yield return "master";
        yield return "combat";
        yield return "loot";
        yield return "event";
        yield return "movement";

        foreach (var customStream in CustomStreams.Keys)
        {
            yield return customStream;
        }
    }

    /// <summary>
    /// Creates a summary for debugging and logging.
    /// </summary>
    /// <returns>Human-readable state summary</returns>
    public string CreateSummary()
    {
        var customCount = CustomStreams.Count;
        return $"RandomState(Master: {MasterState:X8}, Combat: {CombatState:X8}, " +
               $"Loot: {LootState:X8}, Event: {EventState:X8}, Movement: {MovementState:X8}" +
               (customCount > 0 ? $", +{customCount} custom streams" : "") + ")";
    }

    private RandomState UpdateCustomStream(string streamName, ulong newState)
    {
        if (CustomStreams.TryGetValue(streamName, out var existing))
        {
            var updated = existing with { State = newState };
            return this with { CustomStreams = CustomStreams.SetItem(streamName, updated) };
        }

        // Stream doesn't exist - return unchanged
        return this;
    }

    private Option<ulong> GetCustomStreamState(string streamName)
    {
        return CustomStreams.TryGetValue(streamName, out var stream)
            ? Some(stream.State)
            : None;
    }

    private Option<ulong> GetCustomStreamIncrement(string streamName)
    {
        return CustomStreams.TryGetValue(streamName, out var stream)
            ? Some(stream.Stream)
            : None;
    }

    private static ulong GenerateStreamIncrement(string streamName)
    {
        // Generate a deterministic but unique increment from the stream name
        // This ensures different streams don't interfere with each other
        var hash = streamName.GetHashCode();

        // Use a mix of hash and some prime numbers to generate a good increment
        // PCG increments should be odd and have good bit distribution
        ulong increment = (ulong)hash * 0x9E3779B97F4A7C15;
        increment |= 1; // Ensure odd (required for PCG)

        return increment;
    }

    public override string ToString() => CreateSummary();
}

/// <summary>
/// State of a custom RNG stream.
/// </summary>
public sealed record RngStreamState(
    ulong State,
    ulong Stream
);

/// <summary>
/// Extensions for RandomState to support different RNG algorithms.
/// </summary>
public static class RandomStateExtensions
{
    /// <summary>
    /// Validates that all stream increments are odd (required for PCG).
    /// </summary>
    /// <param name="state">Random state to validate</param>
    /// <returns>True if all increments are valid</returns>
    public static bool IsValidForPCG(this RandomState state)
    {
        var increments = new[]
        {
            state.MasterStream,
            state.CombatStream,
            state.LootStream,
            state.EventStream,
            state.MovementStream
        };

        // All increments must be odd for PCG to work correctly
        if (increments.Any(inc => inc % 2 == 0))
            return false;

        // Custom streams must also have odd increments
        return state.CustomStreams.Values.All(s => s.Stream % 2 == 1);
    }

    /// <summary>
    /// Reseeds all streams with a new master seed.
    /// Used for starting new campaigns or resetting determinism.
    /// </summary>
    /// <param name="state">Current random state</param>
    /// <param name="newSeed">New master seed</param>
    /// <returns>Reseeded random state</returns>
    public static RandomState ReseedAll(this RandomState state, ulong newSeed)
    {
        return RandomState.CreateNew(newSeed);
    }
}
