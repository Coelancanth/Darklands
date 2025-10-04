namespace Darklands.Core.Domain.Common;

/// <summary>
/// Represents a fractional offset within grid space (supports sub-cell positioning).
/// Used for visual positioning rules like equipment slot centering.
/// </summary>
/// <remarks>
/// DESIGN: Separate from GridPosition to distinguish:
/// - GridPosition: Integer cell coordinates (0,0), (1,2), etc.
/// - GridOffset: Fractional positioning within/between cells (0.5, -0.5), etc.
///
/// Example use cases:
/// - Equipment slot centering: GridOffset(0.5f, 0.5f) shifts item to cell center
/// - Custom alignment: GridOffset(0f, 0f) = top-left, (1f, 1f) = bottom-right
///
/// Presentation converts to pixels: pixelX = (gridPos.X + offset.X) * CellSize
/// </remarks>
public readonly record struct GridOffset(float X, float Y)
{
    /// <summary>
    /// Zero offset (top-left alignment, no shift).
    /// </summary>
    public static readonly GridOffset Zero = new(0f, 0f);

    /// <summary>
    /// Center offset (shifts item to center of cell).
    /// Used for equipment slots displaying items centered.
    /// </summary>
    public static readonly GridOffset Center = new(0.5f, 0.5f);
}
