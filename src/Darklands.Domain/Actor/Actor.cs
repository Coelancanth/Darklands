using System.Collections.Generic;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Domain.Grid;
using Darklands.Domain.Common;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using System.Linq;
using static LanguageExt.Prelude;

namespace Darklands.Domain.Actor
{
    /// <summary>
    /// Represents a combat actor (player, NPC, creature) in the tactical combat system.
    /// Immutable value object that encapsulates actor state including health and combat attributes.
    /// Position is managed separately by GridStateService to maintain Single Source of Truth.
    /// 
    /// Save-ready entity per ADR-005:
    /// - Implements IPersistentEntity for save/load compatibility
    /// - Contains ModData for future modding support
    /// - Separates persistent from transient state
    /// </summary>
    public sealed record Actor(
        ActorId Id,
        Health Health,
        string Name,
        ImmutableDictionary<string, string> ModData,
        ImmutableList<Position>? ActivePath = null,
        int CurrentPathStep = 0
    ) : IPersistentEntity
    {
        /// <summary>
        /// IPersistentEntity implementation - exposes ID for save system.
        /// </summary>
        IEntityId IPersistentEntity.Id => Id;

        /// <summary>
        /// Transient state that doesn't save (animations, cached data, UI state, etc.).
        /// Kept separate from persistent state and reconstructed after loading.
        /// </summary>
        [JsonIgnore]
        public ITransientState? TransientState { get; init; }

        /// <summary>
        /// Indicates whether this actor is alive (not dead).
        /// </summary>
        public bool IsAlive => !Health.IsDead;

        /// <summary>
        /// Indicates whether this actor has an active movement path in progress.
        /// </summary>
        public bool HasActivePath => ActivePath?.Any() == true && CurrentPathStep < ActivePath.Count;

        /// <summary>
        /// Indicates whether this actor is currently moving (has steps remaining).
        /// </summary>
        public bool IsMoving => HasActivePath;

        /// <summary>
        /// Gets the next position in the movement path, if any.
        /// </summary>
        public Position? NextPosition => HasActivePath ? ActivePath![CurrentPathStep] : null;

        /// <summary>
        /// Gets the remaining steps in the current movement path.
        /// </summary>
        public ImmutableList<Position> RemainingPath =>
            HasActivePath ? ActivePath!.Skip(CurrentPathStep).ToImmutableList() : ImmutableList<Position>.Empty;

        /// <summary>
        /// Starts movement along the specified path.
        /// The actor will begin moving step-by-step through the path positions.
        /// </summary>
        /// <param name="path">The path to follow (must not be empty)</param>
        /// <returns>New Actor instance with movement started or validation error</returns>
        public Fin<Actor> StartMovement(ImmutableList<Position> path)
        {
            if (path.IsEmpty)
                return Error.New("INVALID_MOVEMENT: Cannot start movement with empty path");

            if (HasActivePath)
                return Error.New("INVALID_MOVEMENT: Actor is already moving");

            if (!IsAlive)
                return Error.New("INVALID_MOVEMENT: Dead actors cannot move");

            return this with { ActivePath = path, CurrentPathStep = 0 };
        }

        /// <summary>
        /// Advances movement by one step along the active path.
        /// Returns the actor at the new position or an error if movement cannot advance.
        /// </summary>
        /// <returns>New Actor instance at next step or validation error</returns>
        public Fin<Actor> AdvanceMovement()
        {
            if (!HasActivePath)
                return Error.New("INVALID_MOVEMENT: No active movement to advance");

            if (!IsAlive)
                return Error.New("INVALID_MOVEMENT: Dead actors cannot move");

            var nextStep = CurrentPathStep + 1;

            // If we've reached the end of the path, clear movement state
            if (nextStep >= ActivePath!.Count)
            {
                return this with { ActivePath = null, CurrentPathStep = 0 };
            }

            // Continue to next step
            return this with { CurrentPathStep = nextStep };
        }

        /// <summary>
        /// Cancels any active movement and clears the movement path.
        /// </summary>
        /// <returns>Actor with movement cleared</returns>
        public Actor CancelMovement() =>
            this with { ActivePath = null, CurrentPathStep = 0 };

        /// <summary>
        /// Interrupts movement at the current position.
        /// Similar to cancel but may have different semantic meaning for game logic.
        /// </summary>
        /// <returns>Actor with movement interrupted</returns>
        public Actor InterruptMovement() =>
            this with { ActivePath = null, CurrentPathStep = 0 };

        /// <summary>
        /// Creates a new Actor with validation.
        /// Position is managed separately by GridStateService.
        /// </summary>
        /// <param name="id">Unique actor identifier</param>
        /// <param name="health">Initial health state</param>
        /// <param name="name">Actor name (cannot be empty)</param>
        /// <param name="modData">Optional mod data (null creates empty dictionary)</param>
        /// <returns>Valid Actor instance or validation error</returns>
        public static Fin<Actor> Create(ActorId id, Health health, string name, ImmutableDictionary<string, string>? modData = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Error.New("INVALID_ACTOR: Actor name cannot be empty or whitespace");

            if (id.IsEmpty)
                return Error.New("INVALID_ACTOR: Actor ID cannot be empty");

            return new Actor(id, health, name.Trim(), modData ?? ImmutableDictionary<string, string>.Empty);
        }

        /// <summary>
        /// Creates a new Actor at full health.
        /// Position is managed separately by GridStateService.
        /// </summary>
        /// <param name="id">Unique actor identifier</param>
        /// <param name="maxHealth">Maximum health points</param>
        /// <param name="name">Actor name</param>
        /// <param name="modData">Optional mod data</param>
        /// <returns>Actor at full health or validation error</returns>
        public static Fin<Actor> CreateAtFullHealth(ActorId id, int maxHealth, string name, ImmutableDictionary<string, string>? modData = null) =>
            from health in Health.CreateAtFullHealth(maxHealth)
            from actor in Create(id, health, name, modData)
            select actor;

        /// <summary>
        /// Creates a new Actor at full health using an ID generator.
        /// Preferred method for save-ready actors.
        /// </summary>
        /// <param name="ids">ID generator for creating stable identifiers</param>
        /// <param name="maxHealth">Maximum health points</param>
        /// <param name="name">Actor name</param>
        /// <param name="modData">Optional mod data</param>
        /// <returns>Actor at full health or validation error</returns>
        public static Fin<Actor> CreateAtFullHealth(IStableIdGenerator ids, int maxHealth, string name, ImmutableDictionary<string, string>? modData = null) =>
            CreateAtFullHealth(ActorId.NewId(ids), maxHealth, name, modData);


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
        /// 
        /// NOTE: Uses deprecated ActorId.NewId() for backwards compatibility.
        /// Production code should use methods with IStableIdGenerator.
        /// </summary>
        public static class Presets
        {
            public static Fin<Actor> CreateWarrior(string name = "Warrior") =>
#pragma warning disable CS0618 // Type or member is obsolete
                CreateAtFullHealth(ActorId.NewId(), 100, name);
#pragma warning restore CS0618 // Type or member is obsolete

            public static Fin<Actor> CreateMage(string name = "Mage") =>
#pragma warning disable CS0618 // Type or member is obsolete
                CreateAtFullHealth(ActorId.NewId(), 60, name);
#pragma warning restore CS0618 // Type or member is obsolete

            public static Fin<Actor> CreateRogue(string name = "Rogue") =>
#pragma warning disable CS0618 // Type or member is obsolete
                CreateAtFullHealth(ActorId.NewId(), 80, name);
#pragma warning restore CS0618 // Type or member is obsolete

            public static Fin<Actor> CreatePlayer(string name = "Player") =>
#pragma warning disable CS0618 // Type or member is obsolete
                CreateAtFullHealth(ActorId.NewId(), 100, name);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public override string ToString() => $"{Name} ({Health})";
    }
}
