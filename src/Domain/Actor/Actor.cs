using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Actor
{
    /// <summary>
    /// Represents a combat actor (player, NPC, creature) in the tactical combat system.
    /// Immutable value object that encapsulates actor state including health and combat attributes.
    /// Position is managed separately by GridStateService to maintain Single Source of Truth.
    /// </summary>
    public sealed record Actor
    {
        /// <summary>
        /// Unique identifier for this actor.
        /// </summary>
        public ActorId Id { get; init; }

        /// <summary>
        /// Current health state of the actor.
        /// </summary>
        public Health Health { get; init; }

        /// <summary>
        /// Human-readable name for the actor (for UI and logging).
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Indicates whether this actor is alive (not dead).
        /// </summary>
        public bool IsAlive => !Health.IsDead;

        /// <summary>
        /// Private constructor to enforce factory method usage.
        /// </summary>
        private Actor(ActorId id, Health health, string name)
        {
            Id = id;
            Health = health;
            Name = name;
        }

        /// <summary>
        /// Creates a new Actor with validation.
        /// Position is managed separately by GridStateService.
        /// </summary>
        /// <param name="id">Unique actor identifier</param>
        /// <param name="health">Initial health state</param>
        /// <param name="name">Actor name (cannot be empty)</param>
        /// <returns>Valid Actor instance or validation error</returns>
        public static Fin<Actor> Create(ActorId id, Health health, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Error.New("INVALID_ACTOR: Actor name cannot be empty or whitespace");

            if (id.IsEmpty)
                return Error.New("INVALID_ACTOR: Actor ID cannot be empty");

            return new Actor(id, health, name.Trim());
        }

        /// <summary>
        /// Creates a new Actor at full health.
        /// Position is managed separately by GridStateService.
        /// </summary>
        /// <param name="id">Unique actor identifier</param>
        /// <param name="maxHealth">Maximum health points</param>
        /// <param name="name">Actor name</param>
        /// <returns>Actor at full health or validation error</returns>
        public static Fin<Actor> CreateAtFullHealth(ActorId id, int maxHealth, string name) =>
            from health in Health.CreateAtFullHealth(maxHealth)
            from actor in Create(id, health, name)
            select actor;


        /// <summary>
        /// Applies damage to this actor.
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <returns>New Actor instance with damage applied or validation error</returns>
        public Fin<Actor> TakeDamage(int damage) =>
            from newHealth in Health.TakeDamage(damage)
            select this with { Health = newHealth };

        /// <summary>
        /// Applies healing to this actor.
        /// </summary>
        /// <param name="healAmount">Amount of healing to apply</param>
        /// <returns>New Actor instance with healing applied or validation error</returns>
        public Fin<Actor> Heal(int healAmount) =>
            from newHealth in Health.Heal(healAmount)
            select this with { Health = newHealth };

        /// <summary>
        /// Restores this actor to full health.
        /// </summary>
        /// <returns>Actor at full health</returns>
        public Actor RestoreToFullHealth() =>
            this with { Health = Health.RestoreToFull() };

        /// <summary>
        /// Sets this actor to dead state.
        /// </summary>
        /// <returns>Dead Actor instance</returns>
        public Actor SetToDead() =>
            this with { Health = Health.SetToDead() };

        /// <summary>
        /// Common actor presets for testing and common scenarios.
        /// Position must be set separately via GridStateService.
        /// </summary>
        public static class Presets
        {
            public static Fin<Actor> CreateWarrior(string name = "Warrior") =>
                CreateAtFullHealth(ActorId.NewId(), 100, name);

            public static Fin<Actor> CreateMage(string name = "Mage") =>
                CreateAtFullHealth(ActorId.NewId(), 60, name);

            public static Fin<Actor> CreateRogue(string name = "Rogue") =>
                CreateAtFullHealth(ActorId.NewId(), 80, name);

            public static Fin<Actor> CreatePlayer(string name = "Player") =>
                CreateAtFullHealth(ActorId.NewId(), 100, name);
        }

        public override string ToString() => $"{Name} ({Health})";
    }
}
