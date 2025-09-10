using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Actor
{
    /// <summary>
    /// Represents a static dummy target for combat testing and training.
    /// Immutable value object that can take damage but has no AI or turn-taking behavior.
    /// Position is managed separately by GridStateService to maintain Single Source of Truth.
    /// </summary>
    public sealed record DummyActor
    {
        /// <summary>
        /// Unique identifier for this dummy actor.
        /// </summary>
        public ActorId Id { get; init; }

        /// <summary>
        /// Current health state of the dummy.
        /// </summary>
        public Health Health { get; init; }

        /// <summary>
        /// Human-readable name for the dummy (for UI and logging).
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Indicates whether this dummy is alive (not destroyed).
        /// </summary>
        public bool IsAlive => !Health.IsDead;

        /// <summary>
        /// Indicates this is a static target that never takes turns.
        /// Used by scheduler to exclude from turn processing.
        /// </summary>
        public bool IsStatic => true;

        /// <summary>
        /// Private constructor to enforce factory method usage.
        /// </summary>
        private DummyActor(ActorId id, Health health, string name)
        {
            Id = id;
            Health = health;
            Name = name;
        }

        /// <summary>
        /// Creates a new DummyActor with validation.
        /// Position is managed separately by GridStateService.
        /// </summary>
        /// <param name="id">Unique dummy identifier</param>
        /// <param name="health">Initial health state</param>
        /// <param name="name">Dummy name (cannot be empty)</param>
        /// <returns>Valid DummyActor instance or validation error</returns>
        public static Fin<DummyActor> Create(ActorId id, Health health, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Error.New("INVALID_DUMMY: Dummy name cannot be empty or whitespace");

            if (id.IsEmpty)
                return Error.New("INVALID_DUMMY: Dummy ID cannot be empty");

            return new DummyActor(id, health, name.Trim());
        }

        /// <summary>
        /// Creates a new DummyActor at full health.
        /// Position is managed separately by GridStateService.
        /// </summary>
        /// <param name="id">Unique dummy identifier</param>
        /// <param name="maxHealth">Maximum health points</param>
        /// <param name="name">Dummy name</param>
        /// <returns>DummyActor at full health or validation error</returns>
        public static Fin<DummyActor> CreateAtFullHealth(ActorId id, int maxHealth, string name) =>
            from health in Health.CreateAtFullHealth(maxHealth)
            from dummy in Create(id, health, name)
            select dummy;

        /// <summary>
        /// Applies damage to this dummy target.
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <returns>New DummyActor instance with damage applied or validation error</returns>
        public Fin<DummyActor> TakeDamage(int damage) =>
            from newHealth in Health.TakeDamage(damage)
            select this with { Health = newHealth };

        /// <summary>
        /// Applies healing to this dummy (for testing restoration mechanics).
        /// </summary>
        /// <param name="healAmount">Amount of healing to apply</param>
        /// <returns>New DummyActor instance with healing applied or validation error</returns>
        public Fin<DummyActor> Heal(int healAmount) =>
            from newHealth in Health.Heal(healAmount)
            select this with { Health = newHealth };

        /// <summary>
        /// Restores this dummy to full health.
        /// </summary>
        /// <returns>DummyActor at full health</returns>
        public DummyActor RestoreToFullHealth() =>
            this with { Health = Health.RestoreToFull() };

        /// <summary>
        /// Sets this dummy to dead state.
        /// </summary>
        /// <returns>Dead DummyActor instance</returns>
        public DummyActor SetToDead() =>
            this with { Health = Health.SetToDead() };

        /// <summary>
        /// Common dummy presets for testing and combat scenarios.
        /// Position must be set separately via GridStateService.
        /// </summary>
        public static class Presets
        {
            public static Fin<DummyActor> CreateCombatDummy(string name = "Combat Dummy") =>
#pragma warning disable CS0618 // Type or member is obsolete
                CreateAtFullHealth(ActorId.NewId(), 50, name);
#pragma warning restore CS0618 // Type or member is obsolete

            public static Fin<DummyActor> CreateTrainingDummy(string name = "Training Dummy") =>
#pragma warning disable CS0618 // Type or member is obsolete
                CreateAtFullHealth(ActorId.NewId(), 100, name);
#pragma warning restore CS0618 // Type or member is obsolete

            public static Fin<DummyActor> CreateWeakDummy(string name = "Weak Dummy") =>
#pragma warning disable CS0618 // Type or member is obsolete
                CreateAtFullHealth(ActorId.NewId(), 25, name);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public override string ToString() => $"{Name} ({Health}) [Static]";
    }
}
