using System.Collections.Concurrent;
using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Application.Features.Combat.Services;
using Darklands.Tactical.Domain.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Tactical.Infrastructure.Services;

/// <summary>
/// Service for managing turn order and scheduling in tactical combat.
/// Uses a priority queue for efficient turn scheduling.
/// Thread-safe implementation using locks for critical sections.
/// </summary>
public sealed class CombatSchedulerService : ICombatSchedulerService
{
    private readonly SortedSet<ScheduledTurn> _schedule;
    private readonly ConcurrentDictionary<EntityId, ScheduledTurn> _actorSchedule;
    private readonly object _scheduleLock = new();

    public CombatSchedulerService()
    {
        _schedule = new SortedSet<ScheduledTurn>(ScheduledTurn.TimeComparer);
        _actorSchedule = new ConcurrentDictionary<EntityId, ScheduledTurn>();
    }

    /// <inheritdoc />
    public Task<Fin<EntityId>> GetNextActorAsync(TimeUnit currentTime)
    {
        lock (_scheduleLock)
        {
            // Find the first actor with action time <= current time
            var nextTurn = _schedule.FirstOrDefault(turn => turn.ActionTime.Value <= currentTime.Value);

            if (nextTurn != null)
            {
                // Remove from schedule since they're taking their turn
                _schedule.Remove(nextTurn);
                _actorSchedule.TryRemove(nextTurn.ActorId, out _);
                
                return Task.FromResult(FinSucc(nextTurn.ActorId));
            }

            // If no actors are ready, check if any are scheduled at all
            if (_schedule.Count == 0)
            {
                return Task.FromResult(
                    FinFail<EntityId>(Error.New(404, "No actors scheduled"))
                );
            }

            // Get the next scheduled actor even if their time hasn't come yet
            var nextScheduled = _schedule.First();
            _schedule.Remove(nextScheduled);
            _actorSchedule.TryRemove(nextScheduled.ActorId, out _);
            
            return Task.FromResult(FinSucc(nextScheduled.ActorId));
        }
    }

    /// <inheritdoc />
    public Task<Fin<Unit>> ScheduleActorAsync(EntityId actorId, TimeUnit actionTime, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(actorId);

        lock (_scheduleLock)
        {
            // Remove existing schedule for this actor if present
            if (_actorSchedule.TryGetValue(actorId, out var existingTurn))
            {
                _schedule.Remove(existingTurn);
            }

            var scheduledTurn = new ScheduledTurn(actorId, actionTime, priority);
            
            // Add to both collections
            _schedule.Add(scheduledTurn);
            _actorSchedule[actorId] = scheduledTurn;

            return Task.FromResult(FinSucc(unit));
        }
    }

    /// <inheritdoc />
    public Task<Fin<Unit>> RemoveActorAsync(EntityId actorId)
    {
        ArgumentNullException.ThrowIfNull(actorId);

        lock (_scheduleLock)
        {
            if (_actorSchedule.TryRemove(actorId, out var turn))
            {
                _schedule.Remove(turn);
                return Task.FromResult(FinSucc(unit));
            }

            return Task.FromResult(
                FinFail<Unit>(Error.New(404, $"Actor {actorId.Value} not found in schedule"))
            );
        }
    }

    /// <inheritdoc />
    public Task<Fin<Unit>> ClearScheduleAsync()
    {
        lock (_scheduleLock)
        {
            _schedule.Clear();
            _actorSchedule.Clear();
            return Task.FromResult(FinSucc(unit));
        }
    }

    /// <inheritdoc />
    public Task<Fin<Unit>> InitializeScheduleAsync(Seq<EntityId> actorIds, TimeUnit startTime)
    {
        if (actorIds.IsEmpty)
        {
            return Task.FromResult(
                FinFail<Unit>(Error.New(400, "Cannot initialize with empty actor list"))
            );
        }

        lock (_scheduleLock)
        {
            // Clear existing schedule
            _schedule.Clear();
            _actorSchedule.Clear();

            // Schedule each actor at the start time with their index as priority
            var priority = 0;
            foreach (var actorId in actorIds)
            {
                var turn = new ScheduledTurn(actorId, startTime, priority++);
                _schedule.Add(turn);
                _actorSchedule[actorId] = turn;
            }

            return Task.FromResult(FinSucc(unit));
        }
    }

    /// <inheritdoc />
    public Task<Fin<Seq<ScheduledTurn>>> GetScheduleAsync()
    {
        lock (_scheduleLock)
        {
            var schedule = toSeq(_schedule);
            return Task.FromResult(FinSucc(schedule));
        }
    }

    /// <summary>
    /// Gets the count of scheduled actors.
    /// Useful for testing and debugging.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_scheduleLock)
            {
                return _schedule.Count;
            }
        }
    }

    /// <summary>
    /// Checks if an actor is currently scheduled.
    /// </summary>
    public bool IsScheduled(EntityId actorId)
    {
        return _actorSchedule.ContainsKey(actorId);
    }
}