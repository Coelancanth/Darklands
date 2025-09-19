using System.Collections.Concurrent;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Application.Actor.Services;
using Darklands.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Application.Actor.Services
{
    /// <summary>
    /// In-memory implementation of IActorStateService for Phase 3.
    /// Provides thread-safe actor state management including health and combat status.
    /// Stores complete Actor objects for tactical combat operations.
    /// </summary>
    public class InMemoryActorStateService : IActorStateService
    {
        private readonly object _stateLock = new();
        private readonly ConcurrentDictionary<ActorId, Domain.Actor.Actor> _actors = new();

        public InMemoryActorStateService()
        {
            // Initialize empty - actors will be added as needed for Phase 3
            // Future phases may require pre-populated test actors
        }

        public Option<Domain.Actor.Actor> GetActor(ActorId actorId)
        {
            return _actors.TryGetValue(actorId, out var actor)
                ? Some(actor)
                : None;
        }

        public Fin<Unit> UpdateActorHealth(ActorId actorId, Domain.Actor.Health newHealth)
        {
            var actorOption = GetActor(actorId);
            if (actorOption.IsNone)
                return FinFail<Unit>(Error.New($"ACTOR_NOT_FOUND: Actor {actorId} not found for health update"));

            var currentActor = actorOption.Match(
                Some: actor => actor,
                None: () => throw new InvalidOperationException("Actor should exist at this point")
            );

            // Create new actor with updated health
            var updatedActor = currentActor with { Health = newHealth };

            // Update atomically
            _actors.AddOrUpdate(actorId, updatedActor, (key, oldActor) => updatedActor);

            return FinSucc(Unit.Default);
        }

        public Fin<Domain.Actor.Actor> DamageActor(ActorId actorId, int damage)
        {
            // Input validation
            if (damage < 0)
                return FinFail<Domain.Actor.Actor>(Error.New("INVALID_DAMAGE: Damage amount cannot be negative"));

            var actorOption = GetActor(actorId);
            if (actorOption.IsNone)
                return FinFail<Domain.Actor.Actor>(Error.New($"ACTOR_NOT_FOUND: Actor {actorId} not found for damage"));

            var currentActor = actorOption.Match(
                Some: actor => actor,
                None: () => throw new InvalidOperationException("Actor should exist at this point")
            );

            // Apply damage through domain logic
            var damageResult = currentActor.TakeDamage(damage);
            if (damageResult.IsFail)
                return damageResult.Match<Fin<Domain.Actor.Actor>>(
                    Succ: _ => throw new InvalidOperationException("Expected failure"),
                    Fail: error => FinFail<Domain.Actor.Actor>(error)
                );

            var damagedActor = damageResult.Match(
                Succ: actor => actor,
                Fail: _ => throw new InvalidOperationException("Result should be success at this point")
            );

            // Update state atomically
            _actors.AddOrUpdate(actorId, damagedActor, (key, oldActor) => damagedActor);

            return FinSucc(damagedActor);
        }

        public Fin<Domain.Actor.Actor> HealActor(ActorId actorId, int healAmount)
        {
            // Input validation
            if (healAmount < 0)
                return FinFail<Domain.Actor.Actor>(Error.New("INVALID_HEAL: Heal amount cannot be negative"));

            var actorOption = GetActor(actorId);
            if (actorOption.IsNone)
                return FinFail<Domain.Actor.Actor>(Error.New($"ACTOR_NOT_FOUND: Actor {actorId} not found for healing"));

            var currentActor = actorOption.Match(
                Some: actor => actor,
                None: () => throw new InvalidOperationException("Actor should exist at this point")
            );

            // Business rule: Cannot heal dead actors
            if (currentActor.Health.IsDead)
                return FinFail<Domain.Actor.Actor>(Error.New("CANNOT_HEAL_DEAD: Cannot heal a dead actor"));

            // Apply healing through domain logic
            var healResult = currentActor.Heal(healAmount);
            if (healResult.IsFail)
                return healResult.Match<Fin<Domain.Actor.Actor>>(
                    Succ: _ => throw new InvalidOperationException("Expected failure"),
                    Fail: error => FinFail<Domain.Actor.Actor>(error)
                );

            var healedActor = healResult.Match(
                Succ: actor => actor,
                Fail: _ => throw new InvalidOperationException("Result should be success at this point")
            );

            // Update state atomically
            _actors.AddOrUpdate(actorId, healedActor, (key, oldActor) => healedActor);

            return FinSucc(healedActor);
        }

        public Option<bool> IsActorAlive(ActorId actorId)
        {
            var actorOption = GetActor(actorId);
            return actorOption.Match(
                Some: actor => Some(actor.IsAlive),
                None: () => None
            );
        }

        public Fin<Unit> RemoveDeadActor(ActorId actorId)
        {
            var actorOption = GetActor(actorId);
            if (actorOption.IsNone)
                return FinFail<Unit>(Error.New($"ACTOR_NOT_FOUND: Actor {actorId} not found for removal"));

            var currentActor = actorOption.Match(
                Some: actor => actor,
                None: () => throw new InvalidOperationException("Actor should exist at this point")
            );

            // Business rule: Only remove if actually dead
            if (!currentActor.Health.IsDead)
                return FinFail<Unit>(Error.New("ACTOR_NOT_DEAD: Cannot remove living actor from combat"));

            // Remove actor atomically
            var removed = _actors.TryRemove(actorId, out _);

            return removed
                ? FinSucc(Unit.Default)
                : FinFail<Unit>(Error.New($"REMOVAL_FAILED: Failed to remove actor {actorId}"));
        }

        /// <summary>
        /// Test helper method - adds actor to state service.
        /// Used by tests and initialization code.
        /// </summary>
        public Fin<Unit> AddActor(Domain.Actor.Actor actor)
        {
            if (actor == null)
                return FinFail<Unit>(Error.New("INVALID_ACTOR: Actor cannot be null"));

            // Add or update actor
            _actors.AddOrUpdate(actor.Id, actor, (key, oldActor) => actor);

            return FinSucc(Unit.Default);
        }

        /// <summary>
        /// Test helper method - clears all actors.
        /// Used by tests for clean state.
        /// </summary>
        public void ClearAllActors()
        {
            _actors.Clear();
        }

        /// <summary>
        /// Gets count of actors for testing and diagnostics.
        /// </summary>
        public int ActorCount => _actors.Count;
    }
}
