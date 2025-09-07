using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Actor
{
    /// <summary>
    /// Represents a combat actor (player, NPC, creature) in the tactical combat system.
    /// Immutable value object that encapsulates actor state including health and position.
    /// </summary>
    public sealed record Actor
    {
        /// <summary>
        /// Unique identifier for this actor.
        /// </summary>
        public ActorId Id { get; init; }

        /// <summary>
        /// Current position on the combat grid.
        /// </summary>
        public Position Position { get; init; }

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
        private Actor(ActorId id, Position position, Health health, string name)
        {
            Id = id;
            Position = position;
            Health = health;
            Name = name;
        }

        /// <summary>
        /// Creates a new Actor with validation.
        /// </summary>
        /// <param name="id">Unique actor identifier</param>
        /// <param name="position">Initial position on the grid</param>
        /// <param name="health">Initial health state</param>
        /// <param name="name">Actor name (cannot be empty)</param>
        /// <returns>Valid Actor instance or validation error</returns>
        public static Fin<Actor> Create(ActorId id, Position position, Health health, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Error.New("INVALID_ACTOR: Actor name cannot be empty or whitespace");

            if (id.IsEmpty)
                return Error.New("INVALID_ACTOR: Actor ID cannot be empty");

            return new Actor(id, position, health, name.Trim());
        }

        /// <summary>
        /// Creates a new Actor at full health.
        /// </summary>
        /// <param name="id">Unique actor identifier</param>
        /// <param name="position">Initial position on the grid</param>
        /// <param name="maxHealth">Maximum health points</param>
        /// <param name="name">Actor name</param>
        /// <returns>Actor at full health or validation error</returns>
        public static Fin<Actor> CreateAtFullHealth(ActorId id, Position position, int maxHealth, string name) =>
            from health in Health.CreateAtFullHealth(maxHealth)
            from actor in Create(id, position, health, name)
            select actor;

        /// <summary>
        /// Moves this actor to a new position.
        /// </summary>
        /// <param name="newPosition">Target position</param>
        /// <returns>New Actor instance at the target position</returns>
        public Actor MoveTo(Position newPosition) =>
            this with { Position = newPosition };

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
        /// </summary>
        public static class Presets
        {
            public static Fin<Actor> CreateWarrior(Position position, string name = "Warrior") =>
                CreateAtFullHealth(ActorId.NewId(), position, 100, name);

            public static Fin<Actor> CreateMage(Position position, string name = "Mage") =>
                CreateAtFullHealth(ActorId.NewId(), position, 60, name);

            public static Fin<Actor> CreateRogue(Position position, string name = "Rogue") =>
                CreateAtFullHealth(ActorId.NewId(), position, 80, name);

            public static Fin<Actor> CreatePlayer(Position position, string name = "Player") =>
                CreateAtFullHealth(ActorId.NewId(), position, 100, name);
        }

        public override string ToString() => $"{Name} at {Position} ({Health})";
    }
}
