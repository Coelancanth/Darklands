# ADR-004: Deterministic Simulation

**Status**: Accepted  
**Date**: 2025-09-08  
**Author**: Tech Lead  
**Deciders**: Tech Lead, Dev Engineer  

## Context

Battle Brothers-scale tactical games require deterministic simulation for:
- **Save/Load Reliability**: Game state must reconstruct identically
- **Replay Systems**: Recording inputs must reproduce exact outcomes
- **Debugging**: Bugs must be reproducible from seed + inputs
- **Multiplayer**: All clients must compute identical results
- **Testing**: Combat outcomes must be predictable

Non-deterministic code creates cascading problems that become exponentially harder to fix as the codebase grows. A single source of non-determinism can invalidate saves, break multiplayer synchronization, and make bugs unreproducible.

### The Problem with Non-Deterministic Code

```csharp
// NON-DETERMINISTIC - Different results each run
var damage = Random.Range(10, 20);  // System random
var crit = UnityEngine.Random.value < 0.2f;  // Float comparison  
actors.OrderBy(a => a.Speed);  // Unstable sort
var now = DateTime.Now;  // Wall clock time
```

These patterns make it impossible to:
- Reproduce a bug from a save file
- Implement multiplayer without constant desyncs
- Create unit tests with predictable outcomes
- Record and replay game sessions

## Decision

We will implement **strict deterministic simulation** where:

1. **ALL randomness** flows through a seedable, deterministic random generator
2. **NO floating-point** arithmetic in gameplay logic (use fixed-point)
3. **Stable sorting** for all collections that affect gameplay
4. **Frame-independent** logic (no wall clock time, no frame deltas)
5. **Deterministic IDs** for all entities (no object hash codes)

### Reviewer Addendum (2025-09-08)

> Reviewer: The approach is strong. To harden it and avoid subtle non-determinism, I recommend the following refinements applied below in code and checklists:
> - Use unbiased range generation (rejection sampling) to avoid modulo bias.
> - Do not use `string.GetHashCode()` for RNG stream derivation; replace with a stable 64-bit hash and derive fork seeds/streams from a root seed rather than current state.
> - Validate inputs for `Check` (0..100), `Choose` (non-empty, positive weights, total within Int32).
> - Optionally expose `Stream` and `RootSeed` for diagnostics and save integrity; persist `State` as before; persist streams for diagnostics if desired.
> - Avoid iteration over unordered collections without applying a stable order.
> - Add cross-platform determinism tests and guard against floating-point reintroduction.

### Core Implementation

```csharp
// Domain Layer - Deterministic Random Service
namespace Darklands.Core.Domain.Determinism
{
    /// <summary>
    /// Deterministic random number generator based on PCG algorithm.
    /// Provides reproducible sequences from a seed with context tracking.
    /// </summary>
    public interface IDeterministicRandom
    {
        /// <summary>
        /// Generate next integer in range [0, maxExclusive)
        /// </summary>
        /// <param name="maxExclusive">Exclusive upper bound</param>
        /// <param name="context">Debug context for tracking desyncs</param>
        int Next(int maxExclusive, string context);
        
        /// <summary>
        /// Generate next integer in range [min, maxExclusive)
        /// </summary>
        int Range(int min, int maxExclusive, string context);
        
        /// <summary>
        /// Roll dice (e.g., "3d6+2" = 3 six-sided dice plus 2)
        /// </summary>
        int Roll(int count, int sides, int modifier, string context);
        
        /// <summary>
        /// Success/failure check against percentage
        /// </summary>
        bool Check(int percentChance, string context);
        
        /// <summary>
        /// Choose weighted random element
        /// </summary>
        T Choose<T>(IReadOnlyList<(T item, int weight)> weighted, string context);
        
        /// <summary>
        /// Get/set RNG state for saving
        /// </summary>
        ulong State { get; set; }
        
        /// <summary>
        /// Read-only increment/stream (odd) used by PCG step. Exposed for diagnostics/saving.
        /// </summary>
        ulong Stream { get; }
        
        /// <summary>
        /// Root seed used to derive named forks deterministically.
        /// </summary>
        ulong RootSeed { get; }
        
        /// <summary>
        /// Fork a new independent stream (for parallel systems)
        /// </summary>
        IDeterministicRandom Fork(string streamName);
    }

    /// <summary>
    /// PCG-based implementation (Permuted Congruential Generator)
    /// Fast, space-efficient, statistically excellent
    /// </summary>
    public sealed class DeterministicRandom : IDeterministicRandom
    {
        private ulong _state;
        private readonly ulong _stream;
        private readonly ulong _rootSeed;
        private readonly ILogger? _logger; // Use Microsoft.Extensions.Logging in Core; host provides Serilog bridge
        
        public DeterministicRandom(ulong seed, ulong stream = 1, ILogger? logger = null)
        {
            _state = seed;
            _stream = stream | 1; // Must be odd
            _rootSeed = seed;
            _logger = logger;
        }
        
        public int Next(int maxExclusive, string context)
        {
            if (maxExclusive <= 0)
                throw new ArgumentException($"Invalid max: {maxExclusive}");
            
            // Unbiased range via rejection sampling to avoid modulo bias
            var bound = (uint)maxExclusive;
            var threshold = (uint)(-bound) % bound;
            uint r;
            do
            {
                r = NextUInt32();
            } while (r < threshold);
            var result = (int)(r % bound);
            
            _logger?.Debug("RNG[{Context}]: {Result}/{Max} (State: {State:X16})", 
                context, result, maxExclusive, _state);
            
            return result;
        }
        
        public int Range(int min, int maxExclusive, string context)
        {
            if (min >= maxExclusive)
                throw new ArgumentException($"Invalid range: [{min}, {maxExclusive})");
            
            var bound = (long)maxExclusive - min;
            if (bound > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Range width exceeds int.MaxValue");
            return min + Next((int)bound, context);
        }
        
        public bool Check(int percentChance, string context)
        {
            if ((uint)percentChance > 100)
                throw new ArgumentOutOfRangeException(nameof(percentChance), "percentChance must be 0..100");
            return Next(100, context) < percentChance;
        }
        
        public int Roll(int count, int sides, int modifier, string context)
        {
            var total = modifier;
            for (int i = 0; i < count; i++)
            {
                total += Range(1, sides + 1, $"{context}_d{sides}_{i}");
            }
            return total;
        }
        
        public T Choose<T>(IReadOnlyList<(T item, int weight)> weighted, string context)
        {
            if (weighted == null || weighted.Count == 0)
                throw new ArgumentException("Weighted list must be non-empty", nameof(weighted));
            if (weighted.Any(w => w.weight <= 0))
                throw new ArgumentException("All weights must be positive", nameof(weighted));
            var totalWeightLong = weighted.Sum(w => (long)w.weight);
            if (totalWeightLong <= 0 || totalWeightLong > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(weighted), "Total weight must fit in Int32 and be > 0");
            var totalWeight = (int)totalWeightLong;
            var roll = Next(totalWeight, context);
            
            var accumulated = 0;
            foreach (var (item, weight) in weighted)
            {
                accumulated += weight;
                if (roll < accumulated)
                    return item;
            }
            
            // Should never reach here, but safety fallback
            return weighted[^1].item;
        }
        
        public ulong State 
        { 
            get => _state;
            set => _state = value;
        }
        
        public ulong Stream => _stream;
        public ulong RootSeed => _rootSeed;
        
        public IDeterministicRandom Fork(string streamName)
        {
            // Derive independent stream deterministically from root seed and name (stable across platforms)
            var seed = DeterministicHash64($"{_rootSeed}/seed/{streamName}");
            var streamId = DeterministicHash64($"{_rootSeed}/stream/{streamName}") | 1UL; // ensure odd
            return new DeterministicRandom(seed, streamId, _logger);
        }
        
        /// <summary>
        /// Stable 64-bit FNV-1a hash over UTF-8 bytes. Deterministic across processes/platforms.
        /// </summary>
        private static ulong DeterministicHash64(string text)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= prime;
            }
            return hash;
        }
        
        private uint NextUInt32()
        {
            var oldState = _state;
            _state = oldState * 6364136223846793005UL + _stream;
            var xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            var rot = (int)(oldState >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }
        
        private ulong NextUInt64()
        {
            return ((ulong)NextUInt32() << 32) | NextUInt32();
        }
    }
}

// Fixed-Point Math for Deterministic Calculations
namespace Darklands.Core.Domain.Math
{
    /// <summary>
    /// Fixed-point number for deterministic arithmetic.
    /// Uses 16.16 format (16 bits integer, 16 bits fraction).
    /// </summary>
    public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
    {
        private readonly int _raw;
        private const int FractionBits = 16;
        private const int FractionMask = (1 << FractionBits) - 1;
        private const int One = 1 << FractionBits;
        
        private Fixed(int raw) => _raw = raw;
        
        public static Fixed FromInt(int value) => new(value << FractionBits);
        public static Fixed FromRaw(int raw) => new(raw);
        public static Fixed FromFloat(float value) => new((int)(value * One));
        
        public static readonly Fixed Zero = new(0);
        public static readonly Fixed Unit = new(One);
        public static readonly Fixed Half = new(One >> 1);
        
        public int ToInt() => _raw >> FractionBits;
        public float ToFloat() => _raw / (float)One;
        
        public static Fixed operator +(Fixed a, Fixed b) => new(a._raw + b._raw);
        public static Fixed operator -(Fixed a, Fixed b) => new(a._raw - b._raw);
        public static Fixed operator *(Fixed a, Fixed b) => new((int)((long)a._raw * b._raw >> FractionBits));
        public static Fixed operator /(Fixed a, Fixed b)
        {
            if (b._raw == 0) throw new DivideByZeroException("Fixed division by zero");
            return new((int)((long)a._raw << FractionBits) / b._raw);
        }
        
        public static bool operator >(Fixed a, Fixed b) => a._raw > b._raw;
        public static bool operator <(Fixed a, Fixed b) => a._raw < b._raw;
        public static bool operator >=(Fixed a, Fixed b) => a._raw >= b._raw;
        public static bool operator <=(Fixed a, Fixed b) => a._raw <= b._raw;
        public static bool operator ==(Fixed a, Fixed b) => a._raw == b._raw;
        public static bool operator !=(Fixed a, Fixed b) => a._raw != b._raw;
        
        public bool Equals(Fixed other) => _raw == other._raw;
        public override bool Equals(object? obj) => obj is Fixed f && Equals(f);
        public override int GetHashCode() => _raw;
        public int CompareTo(Fixed other) => _raw.CompareTo(other._raw);
        
        public override string ToString() => ToFloat().ToString("F2");
    }
}

// Application Layer - Combat with Deterministic Random
namespace Darklands.Core.Application.Combat
{
    public sealed class DeterministicCombatService
    {
        private readonly IDeterministicRandom _random;
        private readonly ILogger _logger;
        
        public DeterministicCombatService(IDeterministicRandom random, ILogger logger)
        {
            _random = random;
            _logger = logger;
        }
        
        public CombatResult ExecuteAttack(Actor attacker, Actor target)
        {
            // All combat math uses deterministic random and fixed-point
            var hitChance = CalculateHitChance(attacker, target);
            var isHit = _random.Check(hitChance.ToInt(), $"Attack_{attacker.Id}_vs_{target.Id}");
            
            if (!isHit)
            {
                return CombatResult.Miss(attacker.Id, target.Id);
            }
            
            // Damage calculation with deterministic random
            var baseDamage = attacker.Attack.Value;
            var variance = _random.Range(-2, 3, $"Damage_variance_{attacker.Id}");
            var isCrit = _random.Check(attacker.CritChance, $"Crit_check_{attacker.Id}");
            
            var finalDamage = baseDamage + variance;
            if (isCrit) finalDamage *= 2;
            
            return CombatResult.Hit(attacker.Id, target.Id, finalDamage, isCrit);
        }
        
        private Fixed CalculateHitChance(Actor attacker, Actor target)
        {
            // Use fixed-point math for all calculations
            var baseChance = Fixed.FromInt(75);  // 75% base
            var agilityDiff = Fixed.FromInt(attacker.Agility - target.Agility);
            var modifier = agilityDiff * Fixed.FromInt(5);  // 5% per point
            
            var chance = baseChance + modifier;
            
            // Clamp between 5% and 95%
            if (chance < Fixed.FromInt(5)) chance = Fixed.FromInt(5);
            if (chance > Fixed.FromInt(95)) chance = Fixed.FromInt(95);
            
            return chance;
        }
    }
}

// Infrastructure - Stable Entity Ordering
namespace Darklands.Core.Infrastructure.Determinism
{
    public static class DeterministicExtensions
    {
        /// <summary>
        /// Stable sort that preserves relative order of equal elements
        /// </summary>
        public static IEnumerable<T> OrderByStable<T, TKey>(
            this IEnumerable<T> source, 
            Func<T, TKey> keySelector) 
            where TKey : IComparable<TKey>
        {
            return source
                .Select((item, index) => (item, index))
                .OrderBy(x => keySelector(x.item))
                .ThenBy(x => x.index)  // Stabilize with original index
                .Select(x => x.item);
        }
        
        /// <summary>
        /// Deterministic shuffle using provided random
        /// </summary>
        public static IList<T> Shuffle<T>(this IList<T> list, IDeterministicRandom random, string context)
        {
            var array = list.ToArray();
            for (int i = array.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1, $"{context}_shuffle_{i}");
                (array[i], array[j]) = (array[j], array[i]);
            }
            return array;
        }
    }
}
```

### Usage Examples

```csharp
// Game initialization with seed
public class GameSession
{
    private readonly IDeterministicRandom _masterRandom;
    private readonly IDeterministicRandom _combatRandom;
    private readonly IDeterministicRandom _lootRandom;
    
    public GameSession(ulong seed)
    {
        _masterRandom = new DeterministicRandom(seed);
        _combatRandom = _masterRandom.Fork("combat");
        _lootRandom = _masterRandom.Fork("loot");
    }
    
    public void SaveState(SaveData save)
    {
        save.MasterRandomState = _masterRandom.State;
        save.CombatRandomState = _combatRandom.State;
        save.LootRandomState = _lootRandom.State;
        
        // Optional (diagnostics/integrity): persist streams to detect accidental changes
        save.MasterRandomStream = _masterRandom.Stream;
        save.CombatRandomStream = _combatRandom.Stream;
        save.LootRandomStream = _lootRandom.Stream;
    }
    
    public void LoadState(SaveData save)
    {
        _masterRandom.State = save.MasterRandomState;
        _combatRandom.State = save.CombatRandomState;
        _lootRandom.State = save.LootRandomState;
        // Streams are derived from stable names. If they mismatch saved values, fail fast in validation.
    }
}

// Turn order with stable sorting
public class TurnScheduler
{
    public IReadOnlyList<Actor> GetTurnOrder(IEnumerable<Actor> actors)
    {
        // Stable sort ensures consistent order when speeds are equal
        return actors
            .OrderByStable(a => a.NextTurn)
            .ThenBy(a => a.Id.Value)  // Tie-breaker using deterministic ID
            .ToList();
    }
}
```

## Consequences

### Positive

1. **Reproducible Bugs**: Any bug can be reproduced from seed + input sequence
2. **Save Reliability**: Saves always load to identical state
3. **Multiplayer Ready**: Deterministic simulation enables lockstep networking
4. **Testing**: Combat tests have predictable outcomes
5. **Replay System**: Record inputs, replay entire battles
6. **Debug Tools**: Step forward/backward through combat
7. **Performance**: Fixed-point math often faster than floating-point

### Negative

1. **Development Discipline**: Every developer must follow deterministic patterns
2. **No Unity/Godot Random**: Must use our random service everywhere
3. **Fixed-Point Learning**: Developers need to understand fixed-point math
4. **Debugging Overhead**: Context strings add verbosity
5. **Fork Management**: Must carefully manage random streams

## Testing Strategy

```csharp
[Test]
public void Combat_WithSameSeed_ProducesSameResults()
{
    var random1 = new DeterministicRandom(12345);
    var combat1 = new DeterministicCombatService(random1, NullLogger.Instance);
    var results1 = ExecuteCombatSequence(combat1);
    
    var random2 = new DeterministicRandom(12345);
    var combat2 = new DeterministicCombatService(random2, NullLogger.Instance);
    var results2 = ExecuteCombatSequence(combat2);
    
    results1.Should().BeEquivalentTo(results2);
}

[Test]
public void Sorting_WithEqualValues_IsStable()
{
    var actors = CreateActorsWithSameSpeed();
    var sorted1 = actors.OrderByStable(a => a.Speed).ToList();
    var sorted2 = actors.OrderByStable(a => a.Speed).ToList();
    
    sorted1.Select(a => a.Id).Should().Equal(sorted2.Select(a => a.Id));
}
```

- Add cross-platform deterministic streams test running the same seeds on CI matrix (Windows/Linux/macOS) to assert byte-for-byte identical sequences for a few thousand draws per named stream.
- Add property-based tests (FsCheck) that `Next(n)` never returns `n` or negatives, `Range(min,max)` stays within bounds, and `Choose` selects only from provided items with frequencies ~proportional to weights.
- Add guardrails test that any use of `System.Random`, `UnityEngine.Random`/`Godot.GD.Rand*`, `DateTime.Now`, or floating-point arithmetic in gameplay assemblies fails a Roslyn analyzer.

## Implementation Checklist

- [ ] Replace all `Random.Range` with `IDeterministicRandom`
- [ ] Replace all `float` calculations with `Fixed`
- [ ] Replace all `OrderBy` with `OrderByStable`
- [ ] Replace all `DateTime.Now` with game time
- [ ] Replace all `GetHashCode` with deterministic IDs
- [ ] Add context strings to all random calls
- [ ] Create random forks for independent systems
- [ ] Add save/load for random states
// Determinism hardening
- [ ] Replace any `string.GetHashCode()` usages in RNG derivation with stable 64-bit hash
- [ ] Ensure `Next(int)` uses rejection sampling (no modulo bias)
- [ ] Validate weights and bounds in `Choose`, `Check`, `Range`
- [ ] Avoid iterating unordered collections without stable ordering (e.g., sort dictionary keys)
- [ ] Add cross-platform determinism CI test suite

### Reviewer Addendum (2025-09-08)
- Use `Microsoft.Extensions.Logging` in Core. Bind Serilog via provider in host/Godot project.
- Define a stable, versioned map of named RNG streams (e.g., "combat", "loot", "events"). Persist states and, optionally, streams; validate on load.

## Common Pitfalls to Avoid

```csharp
// ❌ NEVER: Non-deterministic random
var damage = UnityEngine.Random.Range(10, 20);
var damage = System.Random.Next(10, 20);

// ✅ ALWAYS: Deterministic random with context
var damage = _random.Range(10, 20, "attack_damage");

// ❌ NEVER: Floating-point in gameplay
var chance = 0.75f;
if (Random.value < chance) { }

// ✅ ALWAYS: Fixed-point or integers
var chance = 75;  // Percent as integer
if (_random.Check(chance, "hit_check")) { }

// ❌ NEVER: Unstable sorting
actors.OrderBy(a => a.Speed);

// ✅ ALWAYS: Stable sorting with tie-breaker
actors.OrderByStable(a => a.Speed).ThenBy(a => a.Id);

// ❌ NEVER: Wall clock time
var now = DateTime.Now;

// ✅ ALWAYS: Game time
var now = _gameTime.CurrentTurn;
```

- Do not iterate `Dictionary<TKey, TValue>` or `HashSet<T>` when order matters; apply `.OrderByStable` on a deterministic key or use `SortedDictionary`/`SortedSet`.
- Do not use `string.GetHashCode()` for any persisted or deterministic behavior; its value can differ across runtimes and processes.
- Avoid concurrency inside the simulation step. If parallelizing, partition deterministically and avoid data races/interleavings.
- Do not rely on reflection-based member enumeration without a fixed ordering.

## References

- [PCG Random](https://www.pcg-random.org/) - Statistical quality of algorithm
- [Fixed-Point Arithmetic](https://en.wikipedia.org/wiki/Fixed-point_arithmetic)
- [Deterministic Lockstep](https://gafferongames.com/post/deterministic_lockstep/) - Networking implications
- [Battle Brothers GDC Talk](https://www.youtube.com/watch?v=example) - Their approach to determinism