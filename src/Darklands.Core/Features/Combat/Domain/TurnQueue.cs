using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Combat.Domain;

/// <summary>
/// Manages turn-based combat scheduling with time-unit priority queue.
/// Aggregate root for combat turn management.
/// </summary>
/// <remarks>
/// DESIGN DECISIONS:
/// - Queue IS the mode detector: Count > 1 means combat, Count == 1 means exploration
/// - Player permanently scheduled at time=0 during exploration (never removed)
/// - Priority queue sorts by (NextActionTime, IsPlayer) - player wins ties
/// - Relative time model: combat resets to 0 when entering combat mode
///
/// TURN LIFECYCLE:
/// 1. Exploration: Queue = [Player@0] (time frozen)
/// 2. Enemy detected → Schedule(enemy, time=0) → Queue = [Player@0, Enemy@0]
/// 3. Combat mode: IsInCombat() = true (Count > 1)
/// 4. PopNext() returns player (tie-breaking), player acts, time advances
/// 5. Last enemy defeated → Queue = [Player] → ResetToExploration() → Player@0
///
/// WHY: No separate combat state machine needed - queue size IS the state.
/// </remarks>
public sealed class TurnQueue
{
    // Priority queue: actors sorted by (NextActionTime, IsPlayer desc)
    // Player-first tie-breaking: at same time, player comes before non-players
    private readonly List<ScheduledActor> _actors;

    private TurnQueue()
    {
        _actors = new List<ScheduledActor>();
    }

    /// <summary>
    /// Creates a new turn queue with player pre-scheduled at time=0.
    /// </summary>
    /// <param name="playerId">The player character's ActorId</param>
    public static TurnQueue CreateWithPlayer(ActorId playerId)
    {
        var queue = new TurnQueue();
        queue._actors.Add(new ScheduledActor(playerId, TimeUnits.Zero, IsPlayer: true));
        return queue;
    }

    /// <summary>
    /// True if in combat mode (multiple actors scheduled).
    /// False if in exploration mode (only player scheduled).
    /// </summary>
    public bool IsInCombat => _actors.Count > 1;

    /// <summary>
    /// Current number of scheduled actors.
    /// </summary>
    public int Count => _actors.Count;

    /// <summary>
    /// Read-only view of all scheduled actors (for debugging/UI).
    /// </summary>
    public IReadOnlyList<ScheduledActor> ScheduledActors => _actors.AsReadOnly();

    /// <summary>
    /// Gets all scheduled actors (method form for query handlers).
    /// </summary>
    public IReadOnlyList<ScheduledActor> GetScheduledActors() => ScheduledActors;

    /// <summary>
    /// Schedules an actor for action at the specified time.
    /// </summary>
    /// <param name="actorId">Actor to schedule</param>
    /// <param name="nextActionTime">When this actor should act</param>
    /// <param name="isPlayer">True if this is the player (for tie-breaking)</param>
    /// <returns>Success, or Failure if actor already scheduled</returns>
    public Result Schedule(ActorId actorId, TimeUnits nextActionTime, bool isPlayer = false)
    {
        if (Contains(actorId))
            return Result.Failure($"Actor {actorId} is already scheduled");

        var scheduledActor = new ScheduledActor(actorId, nextActionTime, isPlayer);
        _actors.Add(scheduledActor);
        SortQueue();

        return Result.Success();
    }

    /// <summary>
    /// Removes and returns the next actor to act (lowest time, player wins ties).
    /// </summary>
    /// <returns>Success with ScheduledActor, or Failure if queue is empty</returns>
    public Result<ScheduledActor> PopNext()
    {
        if (_actors.Count == 0)
            return Result.Failure<ScheduledActor>("Turn queue is empty");

        var next = _actors[0];
        _actors.RemoveAt(0);

        return Result.Success(next);
    }

    /// <summary>
    /// Peeks at the next actor to act without removing them.
    /// </summary>
    /// <returns>Success with ScheduledActor, or Failure if queue is empty</returns>
    public Result<ScheduledActor> PeekNext()
    {
        if (_actors.Count == 0)
            return Result.Failure<ScheduledActor>("Turn queue is empty");

        return Result.Success(_actors[0]);
    }

    /// <summary>
    /// Checks if an actor is currently scheduled.
    /// </summary>
    public bool Contains(ActorId actorId) =>
        _actors.Any(a => a.ActorId == actorId);

    /// <summary>
    /// Removes an actor from the queue (e.g., when defeated).
    /// </summary>
    /// <param name="actorId">Actor to remove</param>
    /// <returns>Success, or Failure if actor not found</returns>
    public Result Remove(ActorId actorId)
    {
        var actor = _actors.FirstOrDefault(a => a.ActorId == actorId);
        if (actor.ActorId == default)
            return Result.Failure($"Actor {actorId} not found in queue");

        _actors.Remove(actor);

        // If only player remains, reset to exploration mode
        if (_actors.Count == 1 && _actors[0].IsPlayer)
            ResetToExploration();

        return Result.Success();
    }

    /// <summary>
    /// Reschedules an actor to a new action time (e.g., after taking action).
    /// </summary>
    /// <param name="actorId">Actor to reschedule</param>
    /// <param name="newActionTime">New time when actor will act</param>
    /// <returns>Success, or Failure if actor not found</returns>
    public Result Reschedule(ActorId actorId, TimeUnits newActionTime)
    {
        var index = _actors.FindIndex(a => a.ActorId == actorId);
        if (index == -1)
            return Result.Failure($"Actor {actorId} not found in queue");

        var actor = _actors[index];
        _actors[index] = actor with { NextActionTime = newActionTime };
        SortQueue();

        return Result.Success();
    }

    /// <summary>
    /// Resets player to time=0 when returning to exploration mode.
    /// Called automatically when last enemy is removed.
    /// </summary>
    private void ResetToExploration()
    {
        var playerIndex = _actors.FindIndex(a => a.IsPlayer);
        if (playerIndex != -1)
        {
            var player = _actors[playerIndex];
            _actors[playerIndex] = player with { NextActionTime = TimeUnits.Zero };
            SortQueue();
        }
    }

    /// <summary>
    /// Sorts queue by (NextActionTime ascending, IsPlayer descending).
    /// Player-first tie-breaking: at same time, player sorts before non-players.
    /// </summary>
    private void SortQueue()
    {
        _actors.Sort((a, b) =>
        {
            // Primary sort: NextActionTime (ascending - lowest time acts first)
            var timeComparison = a.NextActionTime.CompareTo(b.NextActionTime);
            if (timeComparison != 0)
                return timeComparison;

            // Tie-breaker: IsPlayer (descending - player comes before non-players)
            // True > False in descending order, so player (true) sorts before enemy (false)
            return b.IsPlayer.CompareTo(a.IsPlayer);
        });
    }
}
