using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Combat;

/// <summary>
/// Represents a combat action that can be performed by a combatant.
/// Each action has a base time cost, damage potential, and other properties.
/// </summary>
public readonly record struct CombatAction(
    string Name,
    TimeUnit BaseCost,
    int BaseDamage,
    CombatActionType Type = CombatActionType.Attack,
    int AccuracyBonus = 0)
{
    /// <summary>
    /// Validates that the combat action has valid properties
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        BaseCost.IsValid &&
        BaseDamage >= 0 &&
        AccuracyBonus >= -100 && AccuracyBonus <= 100;

    /// <summary>
    /// Creates a combat action with validation
    /// </summary>
    public static Fin<CombatAction> Create(
        string name,
        TimeUnit baseCost,
        int baseDamage,
        CombatActionType type = CombatActionType.Attack,
        int accuracyBonus = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Fin<CombatAction>.Fail(Error.New("Combat action name cannot be empty"));

        if (!baseCost.IsValid)
            return Fin<CombatAction>.Fail(Error.New($"Invalid base cost: {baseCost}"));

        if (baseDamage < 0)
            return Fin<CombatAction>.Fail(Error.New($"Base damage cannot be negative: {baseDamage}"));

        if (accuracyBonus < -100 || accuracyBonus > 100)
            return Fin<CombatAction>.Fail(Error.New($"Accuracy bonus must be between -100 and 100: {accuracyBonus}"));

        var action = new CombatAction(name, baseCost, baseDamage, type, accuracyBonus);
        return Fin<CombatAction>.Succ(action);
    }

    /// <summary>
    /// Common combat actions for testing and initial implementation
    /// </summary>
    public static class Common
    {
        public static readonly CombatAction DaggerStab = new(
            "Dagger Stab",
            new TimeUnit(500),
            8,
            CombatActionType.Attack,
            10);

        public static readonly CombatAction SwordSlash = new(
            "Sword Slash",
            new TimeUnit(800),
            15,
            CombatActionType.Attack,
            0);

        public static readonly CombatAction AxeChop = new(
            "Axe Chop",
            new TimeUnit(1200),
            22,
            CombatActionType.Attack,
            -5);

        public static readonly CombatAction Block = new(
            "Block",
            new TimeUnit(300),
            0,
            CombatActionType.Defensive,
            0);

        public static readonly CombatAction Dodge = new(
            "Dodge",
            new TimeUnit(200),
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
