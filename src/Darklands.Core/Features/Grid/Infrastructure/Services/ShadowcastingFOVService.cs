using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;

namespace Darklands.Core.Features.Grid.Infrastructure.Services;

/// <summary>
/// Implements Field of View calculation using recursive shadowcasting algorithm.
/// Based on libtcod implementation - divides vision into 8 octants and recursively
/// casts shadows from opaque terrain.
/// </summary>
/// <remarks>
/// References:
/// - Primary: libtcod fov_recursive_shadowcasting.c (lines 58-114)
/// - Secondary: GoRogue RecursiveShadowcastingFOV.cs (lines 122-181)
///
/// Algorithm: For each octant (45° wedge), iterate outward from observer tracking
/// visible "slope wedge". When hitting opaque terrain, cast shadow by recursing
/// with narrowed slopes. Performance: O(8 × radius²), typically 2-5ms for radius=8.
/// </remarks>
public sealed class ShadowcastingFOVService : IFOVService
{
    /// <summary>
    /// Octant transformation matrices for converting local octant coordinates to world coordinates.
    /// Each octant covers 45° of vision (8 octants = 360°).
    /// Format: {xx, xy, yx, yy} where worldX = originX + angle*xx + distance*xy
    /// </summary>
    private static readonly int[,] OctantMatrix = new int[,]
    {
        {  1,  0,  0,  1 },  // Octant 0: East (0° - 45°)
        {  0,  1,  1,  0 },  // Octant 1: Northeast (45° - 90°)
        {  0, -1,  1,  0 },  // Octant 2: North-Northwest (90° - 135°)
        { -1,  0,  0,  1 },  // Octant 3: North (135° - 180°)
        { -1,  0,  0, -1 },  // Octant 4: West (180° - 225°)
        {  0, -1, -1,  0 },  // Octant 5: Southwest (225° - 270°)
        {  0,  1, -1,  0 },  // Octant 6: South-Southeast (270° - 315°)
        {  1,  0,  0, -1 }   // Octant 7: South (315° - 360°)
    };

    /// <inheritdoc />
    public Result<HashSet<Position>> CalculateFOV(GridMap map, Position observer, int radius)
    {
        // Validate radius
        if (radius <= 0)
        {
            return Result.Failure<HashSet<Position>>(
                $"Radius must be positive, got {radius}");
        }

        // Validate observer position
        if (!map.IsValidPosition(observer))
        {
            return Result.Failure<HashSet<Position>>(
                $"Observer position ({observer.X}, {observer.Y}) is outside grid bounds");
        }

        // Initialize visible set with observer position (origin always visible)
        var visible = new HashSet<Position> { observer };

        // Cast vision in all 8 octants (full 360° coverage)
        for (int octant = 0; octant < 8; octant++)
        {
            CastLight(
                map,
                observer,
                distance: 1,           // Start at distance 1 from observer
                slopeHigh: 1.0,        // Initial wedge spans full octant (slope 0 to 1)
                slopeLow: 0.0,
                radius,
                octant,
                visible);
        }

        return Result.Success(visible);
    }

    /// <summary>
    /// Recursively casts light (calculates visibility) for a single octant wedge.
    /// Implements shadowcasting by narrowing the visible slope range when hitting opaque terrain.
    /// </summary>
    /// <param name="map">Grid map for terrain opacity checks</param>
    /// <param name="origin">Observer position (in world coordinates)</param>
    /// <param name="distance">Current distance from observer (polar coordinate)</param>
    /// <param name="slopeHigh">Upper bound of visible slope wedge</param>
    /// <param name="slopeLow">Lower bound of visible slope wedge</param>
    /// <param name="radius">Maximum vision distance</param>
    /// <param name="octant">Which octant (0-7) we're casting in</param>
    /// <param name="visible">Accumulator set of visible positions (mutated)</param>
    private void CastLight(
        GridMap map,
        Position origin,
        int distance,
        double slopeHigh,
        double slopeLow,
        int radius,
        int octant,
        HashSet<Position> visible)
    {
        // Base case: Invalid view wedge (slopes inverted)
        if (slopeHigh < slopeLow)
        {
            return;
        }

        // Base case: Beyond vision radius
        if (distance > radius)
        {
            return;
        }

        // Get octant transformation matrix
        int xx = OctantMatrix[octant, 0];
        int xy = OctantMatrix[octant, 1];
        int yx = OctantMatrix[octant, 2];
        int yy = OctantMatrix[octant, 3];

        // Optimization: Pre-calculate radius squared (avoid sqrt in distance checks)
        int radiusSquared = radius * radius;

        // Track previous tile's opacity for state transitions (shadow casting logic)
        bool prevTileBlocked = false;

        // Iterate through tiles at current distance, from high angle to low
        // (angle is the "perpendicular" coordinate within the octant)
        for (int angle = distance; angle >= 0; angle--)
        {
            // Calculate this tile's slope range (based on tile corners)
            // Slope = angle / distance, adjusted by 0.5 for tile corners
            double tileHigh = (angle + 0.5) / (distance - 0.5);
            double tileLow = (angle - 0.5) / (distance + 0.5);
            double prevTileLow = (angle + 0.5) / (distance + 0.5);

            // Skip tiles not yet in view
            if (tileLow > slopeHigh)
            {
                continue;
            }

            // Stop when tiles are fully past the view wedge
            if (tileHigh < slopeLow)
            {
                break;
            }

            // Transform octant-local coordinates (angle, distance) to world coordinates
            int worldX = origin.X + angle * xx + distance * xy;
            int worldY = origin.Y + angle * yx + distance * yy;
            Position currentPos = new Position(worldX, worldY);

            // Skip if position is out of grid bounds
            var terrainResult = map.GetTerrain(currentPos);
            if (terrainResult.IsFailure)
            {
                continue;
            }

            // Check if within circular radius (Euclidean distance)
            int distanceSquared = angle * angle + distance * distance;
            bool withinRadius = distanceSquared <= radiusSquared;

            // Determine terrain transparency (opaque blocks vision)
            bool isTransparent = !terrainResult.Value.IsOpaque();

            // Mark tile visible if within radius AND transparent
            // (Opaque tiles like walls are visible themselves but block vision beyond)
            if (withinRadius && isTransparent)
            {
                visible.Add(currentPos);
            }

            // Mark opaque tiles visible if within radius (can see the wall itself)
            if (withinRadius && !isTransparent)
            {
                visible.Add(currentPos);
            }

            // Shadowcasting state machine: Handle transitions between transparent/opaque
            if (prevTileBlocked && isTransparent)
            {
                // Transition: Wall → Floor
                // Narrow the view wedge (shadow was cast, continue with reduced vision)
                slopeHigh = prevTileLow;
            }

            if (!prevTileBlocked && !isTransparent)
            {
                // Transition: Floor → Wall
                // Recurse into the "floor sequence" before this wall with narrowed high slope
                // This casts the shadow cone behind the wall
                CastLight(
                    map,
                    origin,
                    distance + 1,
                    slopeHigh,
                    tileHigh,  // Narrow the high slope to cast shadow
                    radius,
                    octant,
                    visible);
            }

            // Update state for next iteration
            prevTileBlocked = !isTransparent;
        }

        // Tail recursion: If the last tile was transparent, continue the current view wedge
        if (!prevTileBlocked)
        {
            CastLight(
                map,
                origin,
                distance + 1,
                slopeHigh,
                slopeLow,
                radius,
                octant,
                visible);
        }
    }
}
