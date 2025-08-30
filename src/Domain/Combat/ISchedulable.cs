using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Domain.Combat;

/// <summary>
/// Interface for entities that can be scheduled in the combat timeline.
/// Provides the core properties needed for turn-based combat scheduling.
/// </summary>
public interface ISchedulable
{
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
