namespace Darklands.Core.Domain.Common;

/// <summary>
/// Represents the rotation state of an item in inventory.
/// Used for spatial inventory rotation (Phase 3).
/// </summary>
/// <remarks>
/// WHY: Rotating items 90° swaps their width and height, enabling
/// better space optimization (2×1 sword → 1×2 sword for tight spaces).
///
/// ROTATION EFFECTS:
/// - Degrees0, Degrees180: Use original width×height
/// - Degrees90, Degrees270: Swap to height×width
///
/// ARCHITECTURE: Rotation state belongs to PLACEMENT (Inventory entity),
/// not ITEM (catalog). Same item can have different rotations in different containers.
/// </remarks>
public enum Rotation
{
    /// <summary>
    /// No rotation (0 degrees).
    /// </summary>
    Degrees0 = 0,

    /// <summary>
    /// Rotated 90 degrees clockwise (dimensions swapped).
    /// </summary>
    Degrees90 = 90,

    /// <summary>
    /// Rotated 180 degrees (dimensions unchanged).
    /// </summary>
    Degrees180 = 180,

    /// <summary>
    /// Rotated 270 degrees clockwise / 90 counter-clockwise (dimensions swapped).
    /// </summary>
    Degrees270 = 270
}
