using System;
using System.Collections.Generic;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Application.Combat.Common;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Combat.Services
{
    /// <summary>
    /// In-memory implementation of ICombatSchedulerService for Phase 2.
    /// Provides thread-safe combat timeline management for development and testing.
    /// Will be replaced with persistent storage in Phase 3.
    /// 
    /// Uses CombatScheduler internally to maintain deterministic turn order
    /// based on TimeUnit and Guid tie-breaking.
    /// </summary>
    public class InMemoryCombatSchedulerService : ICombatSchedulerService
    {
        private readonly object _schedulerLock = new();
        private readonly CombatScheduler _scheduler = new();

        /// <summary>
        /// Schedules an actor to act at a specific time in the combat timeline.
        /// </summary>
        public Fin<Unit> ScheduleActor(ActorId actorId, Position position, TimeUnit nextTurn)
        {
            if (actorId.IsEmpty)
                return FinFail<Unit>(Error.New("INVALID_ACTOR: ActorId cannot be empty"));

            lock (_schedulerLock)
            {
                var schedulableActor = SchedulableActor.Create(actorId, nextTurn, position);
                return _scheduler.ScheduleEntity(schedulableActor);
            }
        }

        /// <summary>
        /// Processes the next turn by removing and returning the next scheduled actor.
        /// </summary>
        public Fin<Option<Guid>> ProcessNextTurn()
        {
            lock (_schedulerLock)
            {
                return _scheduler.ProcessNextTurn()
                    .Map(optionEntity => optionEntity.Map(entity => entity.Id));
            }
        }

        /// <summary>
        /// Gets the current turn order without modifying the scheduler state.
        /// </summary>
        public Fin<IReadOnlyList<ISchedulable>> GetTurnOrder()
        {
            lock (_schedulerLock)
            {
                return _scheduler.GetTurnOrder();
            }
        }

        /// <summary>
        /// Gets the number of currently scheduled actors.
        /// </summary>
        public int GetScheduledCount()
        {
            lock (_schedulerLock)
            {
                return _scheduler.Count;
            }
        }

        /// <summary>
        /// Removes all scheduled actors from the timeline.
        /// </summary>
        public void ClearSchedule()
        {
            lock (_schedulerLock)
            {
                _scheduler.Clear();
            }
        }
    }
}
