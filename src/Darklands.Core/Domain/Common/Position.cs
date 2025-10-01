namespace Darklands.Core.Domain.Common;

/// <summary>
/// Represents a 2D coordinate in grid space.
/// Immutable value type with value semantics - two Positions with the same X, Y are equal.
/// </summary>
/// <remarks>
/// Position is context-free and does not validate bounds.
/// GridMap and other consumers enforce their own validity rules.
/// </remarks>
public readonly record struct Position(int X, int Y);
