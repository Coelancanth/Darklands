using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Domain.Combat;
using Darklands.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Application.Combat.Common
{
    /// <summary>
    /// Priority queue-based timeline scheduler for turn-based combat.
    /// Maintains sorted order of actors by NextTurn time with deterministic tie-breaking.
    /// 
    /// Core functionality:
    /// - Schedule entities with specific turn times
    /// - Process turns in chronological order
    /// - Handle deterministic tie-breaking via Guid comparison
    /// - Support for querying current turn order
    /// </summary>
    public class CombatScheduler
    {
        private readonly List<ISchedulable> _scheduledEntities;

        /// <summary>
        /// Creates a new combat scheduler with deterministic ordering
        /// </summary>
        public CombatScheduler()
        {
            _scheduledEntities = new List<ISchedulable>();
        }

        /// <summary>
        /// Schedules an entity to act at the specified time
        /// </summary>
        public Fin<Unit> ScheduleEntity(ISchedulable entity)
        {
            if (entity == null)
                return FinFail<Unit>(Error.New("INVALID_ENTITY: Cannot schedule null entity"));

            // Insert in sorted position to maintain order
            var insertIndex = _scheduledEntities.BinarySearch(entity, TimeComparer.Instance);
            if (insertIndex < 0)
                insertIndex = ~insertIndex; // BinarySearch returns bitwise complement of index when not found

            _scheduledEntities.Insert(insertIndex, entity);
            return FinSucc(Unit.Default);
        }

        /// <summary>
        /// Removes the next entity to act and returns it
        /// </summary>
        public Fin<Option<ISchedulable>> ProcessNextTurn()
        {
            if (_scheduledEntities.Count == 0)
                return FinSucc(Option<ISchedulable>.None);

            var nextEntity = _scheduledEntities[0];
            _scheduledEntities.RemoveAt(0);
            return FinSucc(Some(nextEntity));
        }

        /// <summary>
        /// Gets the current turn order without modifying the scheduler
        /// </summary>
        public Fin<IReadOnlyList<ISchedulable>> GetTurnOrder()
        {
            return FinSucc((IReadOnlyList<ISchedulable>)_scheduledEntities.ToList());
        }

        /// <summary>
        /// Gets the number of scheduled entities
        /// </summary>
        public int Count => _scheduledEntities.Count;

        /// <summary>
        /// Removes all scheduled entities
        /// </summary>
        public void Clear() => _scheduledEntities.Clear();

        /// <summary>
        /// Removes a specific entity from the scheduler by ActorId
        /// </summary>
        /// <param name="actorId">The ActorId to remove</param>
        /// <returns>True if entity was removed, false if not found</returns>
        public bool RemoveEntity(ActorId actorId)
        {
            var targetGuid = actorId.Value;
            var index = _scheduledEntities.FindIndex(e => e.Id.Equals(targetGuid));

            if (index >= 0)
            {
                _scheduledEntities.RemoveAt(index);
                return true;
            }

            return false;
        }
    }
}
