using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Combat;

/// <summary>
/// Represents a combat action that can be performed by an Actor.
/// Each action has a base time cost, damage potential, and other properties.
/// 
/// SAFETY: Constructor is private to prevent invalid instances. Use Create().
/// </summary>
public readonly record struct CombatAction
{
    /// <summary>
    /// Name of the combat action
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Base time cost of the action
    /// </summary>
    public TimeUnit BaseCost { get; }

    /// <summary>
    /// Base damage potential
    /// </summary>
    public int BaseDamage { get; }

    /// <summary>
    /// Type of combat action
    /// </summary>
    public CombatActionType Type { get; }

    /// <summary>
    /// Accuracy modifier for the action
    /// </summary>
    public int AccuracyBonus { get; }

    /// <summary>
    /// Private constructor ensures all instances are valid.
    /// </summary>
    private CombatAction(
        string name,
        TimeUnit baseCost,
        int baseDamage,
        CombatActionType type = CombatActionType.Attack,
        int accuracyBonus = 0)
    {
        Name = name;
        BaseCost = baseCost;
        BaseDamage = baseDamage;
        Type = type;
        AccuracyBonus = accuracyBonus;
    }
    /// <summary>
    /// All CombatAction instances are always valid due to private constructor.
    /// This property exists for interface compatibility but always returns true.
    /// </summary>
    public bool IsValid => true;

    /// <summary>
    /// Creates a combat action with validation. Primary factory method.
    /// </summary>
    public static Fin<CombatAction> Create(
        string name,
        TimeUnit baseCost,
        int baseDamage,
        CombatActionType type = CombatActionType.Attack,
        int accuracyBonus = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            return FinFail<CombatAction>(Error.New("Combat action name cannot be empty"));

        // BaseCost is already validated by TimeUnit factory methods
        if (baseDamage < 0)
            return FinFail<CombatAction>(Error.New($"Base damage cannot be negative: {baseDamage}"));

        if (accuracyBonus < -100 || accuracyBonus > 100)
            return FinFail<CombatAction>(Error.New($"Accuracy bonus must be between -100 and 100: {accuracyBonus}"));

        return FinSucc(new CombatAction(name, baseCost, baseDamage, type, accuracyBonus));
    }

    /// <summary>
    /// Factory for creating CombatActions with known-valid values.
    /// Used by tests and Common actions.
    /// THROWS on invalid values - only use with compile-time known valid values.
    /// </summary>
    public static CombatAction CreateUnsafe(
        string name,
        TimeUnit baseCost,
        int baseDamage,
        CombatActionType type = CombatActionType.Attack,
        int accuracyBonus = 0)
    {
        var result = Create(name, baseCost, baseDamage, type, accuracyBonus);
        return result.Match(
            Succ: action => action,
            Fail: error => throw new InvalidOperationException($"CreateUnsafe called with invalid values: {error}"));
    }

    /// <summary>
    /// Common combat actions for testing and initial implementation
    /// </summary>
    public static class Common
    {
        public static readonly CombatAction DaggerStab = CreateUnsafe(
            "Dagger Stab",
            TimeUnit.CreateUnsafe(500),
            8,
            CombatActionType.Attack,
            10);

        public static readonly CombatAction SwordSlash = CreateUnsafe(
            "Sword Slash",
            TimeUnit.CreateUnsafe(800),
            15,
            CombatActionType.Attack,
            0);

        public static readonly CombatAction AxeChop = CreateUnsafe(
            "Axe Chop",
            TimeUnit.CreateUnsafe(1200),
            22,
            CombatActionType.Attack,
            -5);

        public static readonly CombatAction Block = CreateUnsafe(
            "Block",
            TimeUnit.CreateUnsafe(300),
            0,
            CombatActionType.Defensive,
            0);

        public static readonly CombatAction Dodge = CreateUnsafe(
            "Dodge",
            TimeUnit.CreateUnsafe(200),
            0,
            CombatActionType.Defensive,
            0);
    }
}

/// <summary>
/// Categories of combat actions for different tactical purposes
/// </summary>
public enum CombatActionType
{
    /// <summary>
    /// Offensive action intended to deal damage
    /// </summary>
    Attack,

    /// <summary>
    /// Defensive action to avoid or mitigate damage
    /// </summary>
    Defensive,

    /// <summary>
    /// Utility action like moving or using items
    /// </summary>
    Utility,

    /// <summary>
    /// Special abilities or magic
    /// </summary>
    Special
}
