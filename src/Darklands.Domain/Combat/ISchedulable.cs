using System;
using Darklands.Domain.Grid;

namespace Darklands.Domain.Combat;

/// <summary>
/// Interface for entities that can be scheduled in the combat timeline.
/// Provides the core properties needed for turn-based combat scheduling.
/// </summary>
public interface ISchedulable
{
    /// <summary>
    /// Unique identifier for this schedulable entity.
    /// Used for deterministic tie-breaking when NextTurn values are equal.
    /// Essential for consistent turn order and replay functionality.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// The absolute time when this entity will next act.
    /// Used by the combat scheduler to determine turn order.
    /// </summary>
    TimeUnit NextTurn { get; }

    /// <summary>
    /// The current position of this entity on the combat grid.
    /// Required for grid-based combat mechanics and positioning logic.
    /// </summary>
    Position Position { get; }
}
