namespace Darklands.Core.Domain.Common;

/// <summary>
/// Represents a 2D coordinate in inventory grid space.
/// Immutable value type with value semantics - two GridPositions with the same X, Y are equal.
/// </summary>
/// <remarks>
/// GridPosition is context-free and does not validate bounds.
/// Inventory and other consumers enforce their own validity rules.
///
/// DESIGN: Separate from Position (map coordinates) for semantic clarity and future divergence.
/// GridPosition may gain inventory-specific operations (e.g., range iteration for multi-cell items).
/// </remarks>
public readonly record struct GridPosition(int X, int Y);
