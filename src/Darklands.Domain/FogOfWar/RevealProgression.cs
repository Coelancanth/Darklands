using System;
using System.Collections.Generic;
using System.Linq;
using Darklands.Domain.Grid;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Domain.FogOfWar
{
    /// <summary>
    /// Value object representing an actor's progressive reveal state along a movement path.
    /// Immutable state with methods to advance progression step-by-step.
    /// </summary>
    public sealed record RevealProgression
    {
        public ActorId ActorId { get; init; }
        public IReadOnlyList<Position> Path { get; init; } = new List<Position>();
        public int CurrentIndex { get; init; }
        public int MillisecondsPerStep { get; init; }
        public int NextAdvanceTimeMs { get; init; }

        /// <summary>
        /// Current reveal position where FOV should be calculated.
        /// </summary>
        public Position CurrentRevealPosition =>
            CurrentIndex < Path.Count ? Path[CurrentIndex] : Path[^1];

        /// <summary>
        /// True if there are more steps to advance.
        /// </summary>
        public bool HasMoreSteps => CurrentIndex < Path.Count - 1;

        /// <summary>
        /// Creates a new reveal progression starting at the first position in the path.
        /// </summary>
        public static Fin<RevealProgression> Create(
            ActorId actorId,
            IEnumerable<Position> path,
            int millisecondsPerStep = 200,
            int currentGameTimeMs = 0)
        {
            var pathList = path.ToList();

            if (pathList.Count == 0)
            {
                return FinFail<RevealProgression>(Error.New("Path cannot be empty"));
            }

            if (millisecondsPerStep <= 0)
            {
                return FinFail<RevealProgression>(Error.New("MillisecondsPerStep must be positive"));
            }

            return FinSucc(new RevealProgression
            {
                ActorId = actorId,
                Path = pathList,
                CurrentIndex = 0,
                MillisecondsPerStep = millisecondsPerStep,
                NextAdvanceTimeMs = currentGameTimeMs + millisecondsPerStep
            });
        }

        /// <summary>
        /// Advances to the next position if enough time has passed.
        /// Returns the new progression state and optionally an advancement event.
        /// </summary>
        public (RevealProgression NewProgression, Option<RevealPositionAdvanced> AdvancementEvent)
            TryAdvance(int currentGameTimeMs, int currentTurn)
        {
            // Check if enough time has passed
            if (currentGameTimeMs < NextAdvanceTimeMs)
            {
                return (this, None);
            }

            // Check if we can advance
            if (!HasMoreSteps)
            {
                return (this, None);
            }

            var previousPosition = CurrentRevealPosition;
            var newIndex = CurrentIndex + 1;
            var newPosition = Path[newIndex];

            var newProgression = this with
            {
                CurrentIndex = newIndex,
                NextAdvanceTimeMs = currentGameTimeMs + MillisecondsPerStep
            };

            var advancementEvent = new RevealPositionAdvanced(
                ActorId,
                newPosition,
                previousPosition,
                currentTurn
            );

            return (newProgression, Some(advancementEvent));
        }

        /// <summary>
        /// Creates a completion event if the progression has finished.
        /// </summary>
        public Option<RevealProgressionCompleted> TryCreateCompletionEvent(int currentTurn)
        {
            if (HasMoreSteps)
            {
                return None;
            }

            return Some(new RevealProgressionCompleted(ActorId, CurrentRevealPosition, currentTurn));
        }
    }
}
