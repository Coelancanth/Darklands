using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Domain.Grid;
using Darklands.Domain.FogOfWar;
using Darklands.Application.Common;
using Darklands.Application.FogOfWar.Events;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace Darklands.Application.FogOfWar.Services
{
    /// <summary>
    /// In-memory implementation of IMovementProgressionService.
    /// Manages timer-based movement progressions with FOV coordination.
    /// Thread-safe implementation supporting concurrent movement of multiple actors.
    /// Implements ADR-022 Two-Position Model with logical position authority.
    /// </summary>
    public class MovementProgressionService : IMovementProgressionService
    {
        private readonly object _stateLock = new();
        private readonly ConcurrentDictionary<ActorId, RevealProgression> _activeProgressions = new();
        private readonly IMediator _mediator;
        private readonly ICategoryLogger _logger;
        private int _currentGameTimeMs;

        public MovementProgressionService(IMediator mediator, ICategoryLogger logger)
        {
            _mediator = mediator;
            _logger = logger;
            _currentGameTimeMs = 0;
        }

        public Fin<Unit> StartMovement(
            ActorId actorId,
            IReadOnlyList<Position> path,
            int millisecondsPerStep = 200,
            int currentTurn = 0)
        {
            if (path == null || path.Count == 0)
            {
                return FinFail<Unit>(Error.New("INVALID_PATH: Movement path cannot be null or empty"));
            }

            if (millisecondsPerStep <= 0)
            {
                return FinFail<Unit>(Error.New("INVALID_TIMING: MillisecondsPerStep must be positive"));
            }

            lock (_stateLock)
            {
                // Check for existing movement
                if (_activeProgressions.ContainsKey(actorId))
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Gameplay,
                        "Cancelling existing movement for {ActorId} to start new progression", actorId);

                    // Cancel existing movement first
                    var cancelResult = CancelMovementInternal(actorId, currentTurn);
                    if (cancelResult.IsFail)
                    {
                        return cancelResult;
                    }
                }

                // Create new progression from domain
                var progressionResult = RevealProgression.Create(
                    actorId,
                    path,
                    millisecondsPerStep,
                    _currentGameTimeMs);

                return progressionResult.Match(
                    Succ: progression =>
                    {
                        // Store the new progression
                        _activeProgressions.AddOrUpdate(actorId, progression, (key, old) => progression);

                        _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                            "Started movement progression for {ActorId}: {PathLength} steps, {Timing}ms per step",
                            actorId, path.Count, millisecondsPerStep);

                        // Publish start notification for FOV coordination (fire and forget)
                        var startNotification = new RevealProgressionStartedNotification(
                            actorId, path.ToList(), currentTurn);
                        _ = Task.Run(async () => await _mediator.Publish(startNotification));

                        return FinSucc(Unit.Default);
                    },
                    Fail: error => FinFail<Unit>(Error.New($"PROGRESSION_CREATE_FAILED: {error.Message}"))
                );
            }
        }

        public Fin<Unit> CancelMovement(ActorId actorId, int currentTurn)
        {
            lock (_stateLock)
            {
                return CancelMovementInternal(actorId, currentTurn);
            }
        }

        private Fin<Unit> CancelMovementInternal(ActorId actorId, int currentTurn)
        {
            if (!_activeProgressions.TryRemove(actorId, out var progression))
            {
                return FinFail<Unit>(Error.New($"NO_ACTIVE_MOVEMENT: No active movement found for actor {actorId}"));
            }

            _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                "Cancelled movement progression for {ActorId} at position {Position}",
                actorId, progression.CurrentRevealPosition);

            // Publish completion notification for cleanup
            var completionNotification = new RevealProgressionCompletedNotification(
                actorId, progression.CurrentRevealPosition, currentTurn);

            // Fire and forget - don't await in lock
            _ = Task.Run(async () => await _mediator.Publish(completionNotification));

            return FinSucc(Unit.Default);
        }

        public Fin<int> AdvanceGameTime(int deltaMilliseconds, int currentTurn)
        {
            if (deltaMilliseconds < 0)
            {
                return FinFail<int>(Error.New("INVALID_DELTA: Time delta cannot be negative"));
            }

            lock (_stateLock)
            {
                _currentGameTimeMs += deltaMilliseconds;
                int advancementCount = 0;
                var advancementEvents = new List<(ActorId ActorId, RevealPositionAdvanced Event)>();
                var completionEvents = new List<(ActorId ActorId, RevealProgressionCompleted Event)>();

                // Process all active progressions
                var progressionsToUpdate = _activeProgressions.ToArray();

                foreach (var (actorId, progression) in progressionsToUpdate)
                {
                    var (newProgression, advancementEvent) = progression.TryAdvance(_currentGameTimeMs, currentTurn);

                    // Update progression if changed
                    if (!ReferenceEquals(progression, newProgression))
                    {
                        _activeProgressions.AddOrUpdate(actorId, newProgression, (key, old) => newProgression);
                    }

                    // Collect advancement event
                    advancementEvent.Match(
                        Some: evt =>
                        {
                            advancementEvents.Add((actorId, evt));
                            advancementCount++;

                            _logger.Log(LogLevel.Debug, LogCategory.Gameplay,
                                "Actor {ActorId} advanced to position {Position}",
                                actorId, evt.NewRevealPosition);
                        },
                        None: () => { }
                    );

                    // Check for completion
                    var completionEvent = newProgression.TryCreateCompletionEvent(currentTurn);
                    completionEvent.Match(
                        Some: evt =>
                        {
                            completionEvents.Add((actorId, evt));
                            _activeProgressions.TryRemove(actorId, out _);

                            _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                                "Movement progression completed for {ActorId} at {Position}",
                                actorId, evt.FinalPosition);
                        },
                        None: () => { }
                    );
                }

                // Publish all events outside of lock (fire and forget)
                if (advancementEvents.Any() || completionEvents.Any())
                {
                    _ = Task.Run(async () => await PublishProgressionEvents(advancementEvents, completionEvents));
                }

                return FinSucc(advancementCount);
            }
        }

        private async Task PublishProgressionEvents(
            List<(ActorId ActorId, RevealPositionAdvanced Event)> advancementEvents,
            List<(ActorId ActorId, RevealProgressionCompleted Event)> completionEvents)
        {
            // Publish advancement events
            foreach (var (actorId, evt) in advancementEvents)
            {
                var notification = new RevealPositionAdvancedNotification(
                    evt.ActorId, evt.NewRevealPosition, evt.PreviousPosition, evt.Turn);
                await _mediator.Publish(notification);
            }

            // Publish completion events
            foreach (var (actorId, evt) in completionEvents)
            {
                var notification = new RevealProgressionCompletedNotification(
                    evt.ActorId, evt.FinalPosition, evt.Turn);
                await _mediator.Publish(notification);
            }
        }

        public Option<Position> GetCurrentPosition(ActorId actorId)
        {
            return _activeProgressions.TryGetValue(actorId, out var progression)
                ? Some(progression.CurrentRevealPosition)
                : None;
        }

        public bool IsMoving(ActorId actorId)
        {
            return _activeProgressions.ContainsKey(actorId);
        }

        public IReadOnlyCollection<ActorId> GetMovingActors()
        {
            return _activeProgressions.Keys.ToArray();
        }

        public Option<RevealProgression> GetProgressionState(ActorId actorId)
        {
            return _activeProgressions.TryGetValue(actorId, out var progression)
                ? Some(progression)
                : None;
        }

        public Fin<int> ClearAllProgressions(int currentTurn)
        {
            lock (_stateLock)
            {
                var clearedCount = _activeProgressions.Count;
                var completionEvents = new List<(ActorId ActorId, RevealProgressionCompleted Event)>();

                foreach (var (actorId, progression) in _activeProgressions)
                {
                    var completionEvent = new RevealProgressionCompleted(
                        actorId, progression.CurrentRevealPosition, currentTurn);
                    completionEvents.Add((actorId, completionEvent));

                    _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                        "Cleared movement progression for {ActorId} at {Position}",
                        actorId, progression.CurrentRevealPosition);
                }

                _activeProgressions.Clear();

                // Publish completion events (fire and forget)
                if (completionEvents.Any())
                {
                    _ = Task.Run(async () =>
                    {
                        foreach (var (actorId, evt) in completionEvents)
                        {
                            var notification = new RevealProgressionCompletedNotification(
                                evt.ActorId, evt.FinalPosition, evt.Turn);
                            await _mediator.Publish(notification);
                        }
                    });
                }

                return FinSucc(clearedCount);
            }
        }
    }
}
