namespace Darklands.Core.Domain.Common;

/// <summary>
/// Helper utilities for item rotation calculations.
/// </summary>
/// <remarks>
/// PHASE 3: Provides dimension swapping logic for rotated items.
/// Rotation affects BOTH collision detection AND sprite rendering.
/// </remarks>
public static class RotationHelper
{
    /// <summary>
    /// Calculates the effective dimensions of an item after rotation.
    /// </summary>
    /// <param name="baseWidth">Original item width</param>
    /// <param name="baseHeight">Original item height</param>
    /// <param name="rotation">Current rotation state</param>
    /// <returns>Tuple of (effectiveWidth, effectiveHeight) after rotation</returns>
    /// <remarks>
    /// ROTATION LOGIC:
    /// - 0° or 180°: Dimensions unchanged (width, height)
    /// - 90° or 270°: Dimensions swapped (height, width)
    ///
    /// EXAMPLES:
    /// - 2×1 sword at 0°: Returns (2, 1)
    /// - 2×1 sword at 90°: Returns (1, 2) - swapped!
    /// - 2×1 sword at 180°: Returns (2, 1)
    /// - 2×1 sword at 270°: Returns (1, 2) - swapped!
    /// </remarks>
    public static (int width, int height) GetRotatedDimensions(int baseWidth, int baseHeight, Rotation rotation)
    {
        return rotation switch
        {
            Rotation.Degrees0 => (baseWidth, baseHeight),
            Rotation.Degrees90 => (baseHeight, baseWidth),  // Swap!
            Rotation.Degrees180 => (baseWidth, baseHeight),
            Rotation.Degrees270 => (baseHeight, baseWidth), // Swap!
            _ => (baseWidth, baseHeight) // Default fallback
        };
    }

    /// <summary>
    /// Rotates clockwise to the next 90-degree increment.
    /// </summary>
    /// <param name="current">Current rotation state</param>
    /// <returns>Next rotation state (0→90→180→270→0)</returns>
    public static Rotation RotateClockwise(Rotation current)
    {
        return current switch
        {
            Rotation.Degrees0 => Rotation.Degrees90,
            Rotation.Degrees90 => Rotation.Degrees180,
            Rotation.Degrees180 => Rotation.Degrees270,
            Rotation.Degrees270 => Rotation.Degrees0,
            _ => Rotation.Degrees0
        };
    }

    /// <summary>
    /// Rotates counter-clockwise to the previous 90-degree increment.
    /// </summary>
    /// <param name="current">Current rotation state</param>
    /// <returns>Previous rotation state (0→270→180→90→0)</returns>
    public static Rotation RotateCounterClockwise(Rotation current)
    {
        return current switch
        {
            Rotation.Degrees0 => Rotation.Degrees270,
            Rotation.Degrees270 => Rotation.Degrees180,
            Rotation.Degrees180 => Rotation.Degrees90,
            Rotation.Degrees90 => Rotation.Degrees0,
            _ => Rotation.Degrees0
        };
    }

    /// <summary>
    /// Converts rotation enum to radians for Godot rendering.
    /// </summary>
    /// <param name="rotation">Rotation enum value</param>
    /// <returns>Rotation in radians (0, π/2, π, 3π/2)</returns>
    public static float ToRadians(Rotation rotation)
    {
        return rotation switch
        {
            Rotation.Degrees0 => 0f,
            Rotation.Degrees90 => MathF.PI / 2f,
            Rotation.Degrees180 => MathF.PI,
            Rotation.Degrees270 => 3f * MathF.PI / 2f,
            _ => 0f
        };
    }
}
