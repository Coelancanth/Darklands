using System;
using System.Collections.Immutable;
using Darklands.Domain.Grid;
using Darklands.Domain.Common;

namespace Darklands.Domain.Vision
{
    /// <summary>
    /// Represents the visibility state of the grid from an actor's perspective.
    /// Tracks currently visible tiles and previously explored tiles for fog of war.
    /// Save-ready immutable record per ADR-005.
    /// </summary>
    public sealed record VisionState(
        ActorId ViewerId,
        ImmutableHashSet<Position> CurrentlyVisible,
        ImmutableHashSet<Position> PreviouslyExplored,
        int LastCalculatedTurn
    ) : IPersistentEntity
    {
        /// <summary>
        /// IPersistentEntity implementation - uses viewer's ID.
        /// </summary>
        IEntityId IPersistentEntity.Id => ViewerId;

        /// <summary>
        /// Creates an empty vision state for a new actor.
        /// </summary>
        public static VisionState CreateEmpty(ActorId viewerId) =>
            new(viewerId,
                ImmutableHashSet<Position>.Empty,
                ImmutableHashSet<Position>.Empty,
                0);

        /// <summary>
        /// Updates the currently visible tiles and adds them to explored.
        /// </summary>
        public VisionState UpdateVisibility(ImmutableHashSet<Position> newVisible, int currentTurn) =>
            this with
            {
                CurrentlyVisible = newVisible,
                PreviouslyExplored = PreviouslyExplored.Union(newVisible),
                LastCalculatedTurn = currentTurn
            };

        /// <summary>
        /// Clears current visibility (used when actor dies or is blinded).
        /// Preserves explored tiles for respawn or recovery.
        /// </summary>
        public VisionState ClearVisibility(int currentTurn) =>
            this with
            {
                CurrentlyVisible = ImmutableHashSet<Position>.Empty,
                LastCalculatedTurn = currentTurn
            };

        /// <summary>
        /// Gets the visibility level of a specific position.
        /// </summary>
        public VisibilityLevel GetVisibilityLevel(Position position)
        {
            if (CurrentlyVisible.Contains(position))
                return VisibilityLevel.Visible;

            if (PreviouslyExplored.Contains(position))
                return VisibilityLevel.Explored;

            return VisibilityLevel.Unseen;
        }

        /// <summary>
        /// Checks if this vision state needs recalculation.
        /// </summary>
        public bool NeedsRecalculation(int currentTurn) =>
            LastCalculatedTurn < currentTurn;

        /// <summary>
        /// Merges another vision state into this one (for shared vision mechanics).
        /// </summary>
        public VisionState MergeWith(VisionState other, int currentTurn) =>
            this with
            {
                CurrentlyVisible = CurrentlyVisible.Union(other.CurrentlyVisible),
                PreviouslyExplored = PreviouslyExplored.Union(other.PreviouslyExplored),
                LastCalculatedTurn = currentTurn
            };

        /// <summary>
        /// Gets statistics about this vision state.
        /// </summary>
        public (int visible, int explored, int total) GetStatistics(int gridSize) =>
            (CurrentlyVisible.Count, PreviouslyExplored.Count, gridSize);

        public override string ToString() =>
            $"VisionState(Actor: {ViewerId.Value.ToString()[..8]}, Visible: {CurrentlyVisible.Count}, Explored: {PreviouslyExplored.Count})";
    }

    /// <summary>
    /// Three-state visibility system for fog of war.
    /// </summary>
    public enum VisibilityLevel
    {
        /// <summary>
        /// Never seen - rendered as black/opaque overlay.
        /// </summary>
        Unseen = 0,

        /// <summary>
        /// Previously seen but not currently visible - rendered as gray/semi-transparent overlay.
        /// </summary>
        Explored = 1,

        /// <summary>
        /// Currently visible - no overlay.
        /// </summary>
        Visible = 2
    }
}
