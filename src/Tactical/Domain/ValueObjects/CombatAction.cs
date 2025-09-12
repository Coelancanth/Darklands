using Darklands.SharedKernel.Domain;

namespace Darklands.Tactical.Domain.ValueObjects;

/// <summary>
/// Represents a combat action that can be performed by an actor.
/// </summary>
public sealed record CombatAction
{
    /// <summary>
    /// The type of combat action.
    /// </summary>
    public CombatActionType Type { get; }

    /// <summary>
    /// The actor performing the action.
    /// </summary>
    public EntityId ActorId { get; }

    /// <summary>
    /// The target of the action (if applicable).
    /// </summary>
    public EntityId? TargetId { get; }

    /// <summary>
    /// The time cost of performing this action.
    /// </summary>
    public TimeUnit TimeCost { get; }

    /// <summary>
    /// Additional parameters for the action (e.g., damage multiplier, healing amount).
    /// </summary>
    public int Power { get; }

    /// <summary>
    /// Description or name of the specific ability being used.
    /// </summary>
    public string ActionName { get; }

    private CombatAction(
        CombatActionType type,
        EntityId actorId,
        EntityId? targetId,
        TimeUnit timeCost,
        int power,
        string actionName)
    {
        Type = type;
        ActorId = actorId;
        TargetId = targetId;
        TimeCost = timeCost;
        Power = power;
        ActionName = actionName;
    }

    /// <summary>
    /// Creates a basic attack action.
    /// </summary>
    public static CombatAction Attack(
        EntityId actorId,
        EntityId targetId,
        int power = 100,
        TimeUnit? timeCost = null)
    {
        return new CombatAction(
            CombatActionType.Attack,
            actorId,
            targetId,
            timeCost ?? TimeUnit.OneTurn,
            power,
            "Basic Attack"
        );
    }

    /// <summary>
    /// Creates a defend action.
    /// </summary>
    public static CombatAction Defend(
        EntityId actorId,
        TimeUnit? timeCost = null)
    {
        return new CombatAction(
            CombatActionType.Defend,
            actorId,
            null,
            timeCost ?? TimeUnit.HalfTurn,
            50, // Defense bonus percentage
            "Defend"
        );
    }

    /// <summary>
    /// Creates a special ability action.
    /// </summary>
    public static CombatAction SpecialAbility(
        EntityId actorId,
        EntityId? targetId,
        string abilityName,
        int power,
        TimeUnit timeCost)
    {
        return new CombatAction(
            CombatActionType.SpecialAbility,
            actorId,
            targetId,
            timeCost,
            power,
            abilityName
        );
    }

    /// <summary>
    /// Creates a wait/skip turn action.
    /// </summary>
    public static CombatAction Wait(
        EntityId actorId,
        TimeUnit? timeCost = null)
    {
        return new CombatAction(
            CombatActionType.Wait,
            actorId,
            null,
            timeCost ?? TimeUnit.QuickAction,
            0,
            "Wait"
        );
    }

    /// <summary>
    /// Creates a heal action.
    /// </summary>
    public static CombatAction Heal(
        EntityId actorId,
        EntityId targetId,
        int healAmount,
        TimeUnit? timeCost = null)
    {
        return new CombatAction(
            CombatActionType.Heal,
            actorId,
            targetId,
            timeCost ?? TimeUnit.OneTurn,
            healAmount,
            "Heal"
        );
    }

    /// <summary>
    /// Checks if this action requires a target.
    /// </summary>
    public bool RequiresTarget => Type switch
    {
        CombatActionType.Attack => true,
        CombatActionType.Heal => true,
        CombatActionType.SpecialAbility => TargetId.HasValue,
        _ => false
    };

    /// <summary>
    /// Checks if this action is offensive (deals damage).
    /// </summary>
    public bool IsOffensive => Type switch
    {
        CombatActionType.Attack => true,
        CombatActionType.SpecialAbility => Power > 0 && TargetId.HasValue,
        _ => false
    };

    /// <summary>
    /// Checks if this action is defensive or supportive.
    /// </summary>
    public bool IsDefensive => Type switch
    {
        CombatActionType.Defend => true,
        CombatActionType.Heal => true,
        CombatActionType.Wait => true,
        _ => false
    };
}

/// <summary>
/// Defines the types of combat actions available.
/// </summary>
public enum CombatActionType
{
    /// <summary>
    /// A standard attack action.
    /// </summary>
    Attack,

    /// <summary>
    /// A defensive stance that reduces incoming damage.
    /// </summary>
    Defend,

    /// <summary>
    /// A special ability with custom effects.
    /// </summary>
    SpecialAbility,

    /// <summary>
    /// Skip the turn with minimal time cost.
    /// </summary>
    Wait,

    /// <summary>
    /// Restore health to a target.
    /// </summary>
    Heal,

    /// <summary>
    /// Move to a different position (for future tactical positioning).
    /// </summary>
    Move,

    /// <summary>
    /// Use an item from inventory.
    /// </summary>
    UseItem
}
