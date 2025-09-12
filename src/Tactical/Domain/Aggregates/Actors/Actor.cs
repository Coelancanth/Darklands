using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Domain.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Tactical.Domain.Aggregates.Actors;

/// <summary>
/// Represents a tactical actor in combat. An aggregate root that manages combat state and actions.
/// </summary>
public sealed class Actor
{
    /// <summary>
    /// Unique identifier for this actor.
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// Display name of the actor.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Current health points. Actor is dead when this reaches 0.
    /// </summary>
    public int Health { get; private set; }

    /// <summary>
    /// Maximum health points.
    /// </summary>
    public int MaxHealth { get; }

    /// <summary>
    /// Base attack damage this actor can deal.
    /// </summary>
    public int AttackPower { get; private set; }

    /// <summary>
    /// Defense rating that reduces incoming damage.
    /// </summary>
    public int Defense { get; private set; }

    /// <summary>
    /// Current initiative or speed value for turn scheduling.
    /// </summary>
    public TimeUnit Initiative { get; private set; }

    /// <summary>
    /// Indicates if the actor is currently alive (Health > 0).
    /// </summary>
    public bool IsAlive => Health > 0;

    /// <summary>
    /// Indicates if the actor can currently act (alive and not stunned/disabled).
    /// </summary>
    public bool CanAct => IsAlive && !IsStunned;

    /// <summary>
    /// Indicates if the actor is currently stunned or otherwise disabled.
    /// </summary>
    public bool IsStunned { get; private set; }

    /// <summary>
    /// Collection of domain events that have occurred for this actor.
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the collection of uncommitted domain events.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Creates a new Actor instance.
    /// </summary>
    private Actor(EntityId id, string name, int maxHealth, int attackPower, int defense, TimeUnit initiative)
    {
        Id = id;
        Name = name;
        Health = maxHealth;
        MaxHealth = maxHealth;
        AttackPower = attackPower;
        Defense = defense;
        Initiative = initiative;
        IsStunned = false;
    }

    /// <summary>
    /// Factory method to create a new Actor with validation.
    /// </summary>
    public static Fin<Actor> Create(
        EntityId id,
        string name,
        int maxHealth,
        int attackPower,
        int defense,
        TimeUnit initiative)
    {
        var validations = Seq(
            ValidateName(name),
            ValidateHealth(maxHealth),
            ValidateAttackPower(attackPower),
            ValidateDefense(defense)
        );

        var errors = validations.Filter(v => v.IsFail).Map(v => v.Match(
            Succ: _ => Error.New(""),
            Fail: e => e
        ));

        if (errors.Count > 0)
        {
            return FinFail<Actor>(Error.Many(errors.ToSeq()));
        }

        return FinSucc(new Actor(id, name, maxHealth, attackPower, defense, initiative));
    }

    /// <summary>
    /// Applies damage to this actor.
    /// </summary>
    public Fin<int> TakeDamage(int rawDamage, TimeUnit occurredAt)
    {
        if (!IsAlive)
            return FinFail<int>(Error.New("Cannot damage a dead actor"));

        if (rawDamage < 0)
            return FinFail<int>(Error.New("Damage cannot be negative"));

        var actualDamage = Math.Max(0, rawDamage - Defense);
        var previousHealth = Health;
        Health = Math.Max(0, Health - actualDamage);

        AddDomainEvent(new ActorDamagedEvent(
            Id,
            actualDamage,
            Health,
            occurredAt
        ));

        if (Health == 0 && previousHealth > 0)
        {
            AddDomainEvent(new ActorDiedEvent(Id, occurredAt));
        }

        return FinSucc(actualDamage);
    }

    /// <summary>
    /// Heals this actor.
    /// </summary>
    public Fin<int> Heal(int amount, TimeUnit occurredAt)
    {
        if (!IsAlive)
            return FinFail<int>(Error.New("Cannot heal a dead actor"));

        if (amount < 0)
            return FinFail<int>(Error.New("Heal amount cannot be negative"));

        var previousHealth = Health;
        Health = Math.Min(MaxHealth, Health + amount);
        var actualHealing = Health - previousHealth;

        if (actualHealing > 0)
        {
            AddDomainEvent(new ActorHealedEvent(
                Id,
                actualHealing,
                Health,
                occurredAt
            ));
        }

        return FinSucc(actualHealing);
    }

    /// <summary>
    /// Updates the actor's initiative for turn scheduling.
    /// </summary>
    public Fin<Unit> UpdateInitiative(TimeUnit newInitiative)
    {
        if (newInitiative.Value < 0)
            return FinFail<Unit>(Error.New("Initiative cannot be negative"));

        Initiative = newInitiative;
        return FinSucc(unit);
    }

    /// <summary>
    /// Stuns the actor, preventing them from acting.
    /// </summary>
    public Fin<Unit> ApplyStun(TimeUnit occurredAt)
    {
        if (!IsAlive)
            return FinFail<Unit>(Error.New("Cannot stun a dead actor"));

        if (IsStunned)
            return FinFail<Unit>(Error.New("Actor is already stunned"));

        IsStunned = true;
        AddDomainEvent(new ActorStunnedEvent(Id, occurredAt));

        return FinSucc(unit);
    }

    /// <summary>
    /// Removes stun effect from the actor.
    /// </summary>
    public Fin<Unit> RemoveStun(TimeUnit occurredAt)
    {
        if (!IsStunned)
            return FinFail<Unit>(Error.New("Actor is not stunned"));

        IsStunned = false;
        AddDomainEvent(new ActorStunRemovedEvent(Id, occurredAt));

        return FinSucc(unit);
    }

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    private static Fin<Unit> ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return FinFail<Unit>(Error.New("Actor name cannot be empty"));

        if (name.Length > 50)
            return FinFail<Unit>(Error.New("Actor name cannot exceed 50 characters"));

        return FinSucc(unit);
    }

    private static Fin<Unit> ValidateHealth(int maxHealth)
    {
        if (maxHealth <= 0)
            return FinFail<Unit>(Error.New("Max health must be greater than 0"));

        if (maxHealth > 9999)
            return FinFail<Unit>(Error.New("Max health cannot exceed 9999"));

        return FinSucc(unit);
    }

    private static Fin<Unit> ValidateAttackPower(int attackPower)
    {
        if (attackPower < 0)
            return FinFail<Unit>(Error.New("Attack power cannot be negative"));

        if (attackPower > 999)
            return FinFail<Unit>(Error.New("Attack power cannot exceed 999"));

        return FinSucc(unit);
    }

    private static Fin<Unit> ValidateDefense(int defense)
    {
        if (defense < 0)
            return FinFail<Unit>(Error.New("Defense cannot be negative"));

        if (defense > 999)
            return FinFail<Unit>(Error.New("Defense cannot exceed 999"));

        return FinSucc(unit);
    }
}

// Domain Events
public sealed record ActorDamagedEvent(
    EntityId ActorId,
    int Damage,
    int RemainingHealth,
    TimeUnit OccurredAt
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => DateTime.UtcNow; // Will be replaced with GameTick later
}

public sealed record ActorDiedEvent(
    EntityId ActorId,
    TimeUnit OccurredAt
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => DateTime.UtcNow; // Will be replaced with GameTick later
}

public sealed record ActorHealedEvent(
    EntityId ActorId,
    int Amount,
    int NewHealth,
    TimeUnit OccurredAt
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => DateTime.UtcNow; // Will be replaced with GameTick later
}

public sealed record ActorStunnedEvent(
    EntityId ActorId,
    TimeUnit OccurredAt
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => DateTime.UtcNow; // Will be replaced with GameTick later
}

public sealed record ActorStunRemovedEvent(
    EntityId ActorId,
    TimeUnit OccurredAt
) : IDomainEvent
{
    DateTime IDomainEvent.OccurredAt => DateTime.UtcNow; // Will be replaced with GameTick later
}
