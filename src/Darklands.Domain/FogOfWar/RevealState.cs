using Darklands.Domain.Grid;

namespace Darklands.Domain.FogOfWar
{
    /// <summary>
    /// Value object representing the current reveal state for an actor.
    /// Provides a snapshot of where FOV should be calculated from.
    /// </summary>
    public sealed record RevealState(
        Position CurrentRevealPosition,
        bool IsProgressing,
        int NextAdvanceTimeMs
    )
    {
        /// <summary>
        /// Creates a reveal state for an actor that is not currently progressing.
        /// Used when actor is at rest and FOV should be calculated from their current game position.
        /// </summary>
        public static RevealState AtRest(Position position) =>
            new(position, false, 0);

        /// <summary>
        /// Creates a reveal state for an actor that is currently progressing along a path.
        /// </summary>
        public static RevealState Progressing(Position currentRevealPosition, int nextAdvanceTimeMs) =>
            new(currentRevealPosition, true, nextAdvanceTimeMs);
    }
}
