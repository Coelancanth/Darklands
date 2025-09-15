using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Determinism;

namespace Darklands.Core.Domain.Vision
{
    /// <summary>
    /// Implements recursive shadowcasting field-of-view algorithm.
    /// This is a pure, deterministic algorithm that calculates visible tiles
    /// from a given origin point, taking into account vision-blocking terrain.
    /// 
    /// Based on the algorithm described by Björn Bergström and refined by the roguelike community.
    /// Divides the FOV into 8 octants and recursively casts shadows for efficiency.
    /// </summary>
    public static class ShadowcastingFOV
    {
        /// <summary>
        /// Octant transformation matrices based on libtcod's proven implementation.
        /// Each row represents [xx, xy, yx, yy] where:
        /// x = col * xx + row * xy
        /// y = col * yx + row * yy
        /// This transforms from octant-space (row/col) to world-space (x/y).
        /// </summary>
        private static readonly int[,] Octants = new int[,]
        {
            {  1,  0,  0,  1 },  // Octant 0: East-Northeast
            {  0,  1,  1,  0 },  // Octant 1: North-Northeast  
            {  0, -1,  1,  0 },  // Octant 2: North-Northwest
            { -1,  0,  0,  1 },  // Octant 3: West-Northwest
            { -1,  0,  0, -1 },  // Octant 4: West-Southwest
            {  0, -1, -1,  0 },  // Octant 5: South-Southwest
            {  0,  1, -1,  0 },  // Octant 6: South-Southeast
            {  1,  0,  0, -1 },  // Octant 7: East-Southeast
        };

        /// <summary>
        /// Calculates field of view from an origin position using shadowcasting.
        /// </summary>
        /// <param name="origin">The viewing position</param>
        /// <param name="range">Maximum vision range in tiles</param>
        /// <param name="grid">The grid to calculate FOV on</param>
        /// <returns>Set of visible positions or error</returns>
        public static Fin<ImmutableHashSet<Position>> CalculateFOV(Position origin, int range, Darklands.Core.Domain.Grid.Grid grid)
        {
            if (range < 0)
                return FinFail<ImmutableHashSet<Position>>(Error.New("Vision range cannot be negative"));

            if (!grid.IsValidPosition(origin))
                return FinFail<ImmutableHashSet<Position>>(Error.New($"Origin position {origin} is outside grid bounds"));

            var visible = new System.Collections.Generic.HashSet<Position> { origin };

            if (range == 0)
                return FinSucc(visible.ToImmutableHashSet());

            // Cast shadows in all 8 octants
            for (int octant = 0; octant < 8; octant++)
            {
                // Start at distance 1 with full slope range (1.0 to 0.0)
                CastShadow(origin, range, grid, octant, visible, 1, Fixed.One, Fixed.Zero);
            }

            return FinSucc(visible.ToImmutableHashSet());
        }

        /// <summary>
        /// Recursively casts shadows in a single octant.
        /// Based on libtcod's proven recursive shadowcasting implementation.
        /// </summary>
        private static void CastShadow(
            Position origin,
            int range,
            Darklands.Core.Domain.Grid.Grid grid,
            int octant,
            System.Collections.Generic.HashSet<Position> visible,
            int distance,
            Fixed viewSlopeHigh,
            Fixed viewSlopeLow)
        {
            if (viewSlopeHigh < viewSlopeLow)
                return;

            if (distance > range)
                return;

            bool prevTileBlocked = false;

            // Iterate from high angle to low angle (columns in this row)
            // This matches libtcod's approach for proper shadow propagation
            for (int angle = distance; angle >= 0; angle--)
            {
                // Calculate slopes for this tile using Fixed-point arithmetic for determinism
                // Note: We need to be careful with division by zero when distance is 0 or 1
                Fixed tileSlopeHigh = distance == 0 ? Fixed.One :
                    (Fixed.FromInt(angle) + Fixed.Half) / (Fixed.FromInt(distance) - Fixed.Half);
                Fixed tileSlopeLow = (Fixed.FromInt(angle) - Fixed.Half) / (Fixed.FromInt(distance) + Fixed.Half);
                Fixed prevTileSlopeLow = (Fixed.FromInt(angle) + Fixed.Half) / (Fixed.FromInt(distance) + Fixed.Half);

                // Check if tile is within view slopes
                if (tileSlopeLow > viewSlopeHigh)
                    continue;  // Tile is not in view yet
                if (tileSlopeHigh < viewSlopeLow)
                    break;  // Tiles will no longer be in view

                // Transform octant coordinates to world coordinates
                int xx = Octants[octant, 0];
                int xy = Octants[octant, 1];
                int yx = Octants[octant, 2];
                int yy = Octants[octant, 3];

                int worldX = origin.X + angle * xx + distance * xy;
                int worldY = origin.Y + angle * yx + distance * yy;
                var worldPos = new Position(worldX, worldY);

                // Check if position is within grid bounds
                if (!grid.IsValidPosition(worldPos))
                    continue;

                // Check actual distance (squared for efficiency)
                int dx = worldX - origin.X;
                int dy = worldY - origin.Y;
                if (dx * dx + dy * dy > range * range)
                    continue;

                // Add to visible set
                visible.Add(worldPos);

                // Check if this tile blocks vision
                var tileResult = grid.GetTile(worldPos);
                bool blocksVision = tileResult.Match(
                    Succ: tile => tile.BlocksLineOfSight,
                    Fail: _ => true
                );

                if (prevTileBlocked)
                {
                    // Previous tile was blocking
                    if (blocksVision)
                    {
                        // Still blocked - no change needed
                    }
                    else
                    {
                        // Wall -> floor transition: reduce view size and reset
                        viewSlopeHigh = prevTileSlopeLow;
                    }
                }
                else
                {
                    // Previous tile was not blocking
                    if (blocksVision && distance < range)
                    {
                        // Floor -> wall transition: recurse for the visible portion
                        // Use tileSlopeHigh as the new END slope for the recursion
                        CastShadow(origin, range, grid, octant, visible,
                                 distance + 1, viewSlopeHigh, tileSlopeHigh);
                    }
                }

                // Update previous tile state for next iteration
                prevTileBlocked = blocksVision;
            }

            // If row ended without blocking, continue to next distance
            if (!prevTileBlocked)
            {
                CastShadow(origin, range, grid, octant, visible,
                         distance + 1, viewSlopeHigh, viewSlopeLow);
            }
        }

        /// <summary>
        /// Calculates FOV and returns it as a VisionState.
        /// Convenience method for integration with the vision system.
        /// </summary>
        public static Fin<VisionState> CalculateVisionState(
            ActorId viewerId,
            Position origin,
            VisionRange range,
            Darklands.Core.Domain.Grid.Grid grid,
            VisionState? previousState,
            int currentTurn)
        {
            return CalculateFOV(origin, range.Value, grid)
                .Map(visible =>
                {
                    var baseState = previousState ?? VisionState.CreateEmpty(viewerId);
                    return baseState.UpdateVisibility(visible, currentTurn);
                });
        }

        /// <summary>
        /// Checks if there is line of sight between two positions.
        /// Uses a simplified bresenham-like check, faster than full FOV calculation.
        /// </summary>
        public static bool HasLineOfSight(Position from, Position to, Darklands.Core.Domain.Grid.Grid grid)
        {
            var dx = Math.Abs(to.X - from.X);
            var dy = Math.Abs(to.Y - from.Y);
            var x = from.X;
            var y = from.Y;
            var n = 1 + dx + dy;
            var xInc = (to.X > from.X) ? 1 : -1;
            var yInc = (to.Y > from.Y) ? 1 : -1;
            var error = dx - dy;
            dx *= 2;
            dy *= 2;

            for (; n > 0; --n)
            {
                var currentPos = new Position(x, y);

                // Skip checking the start and end positions
                if (currentPos != from && currentPos != to)
                {
                    if (!grid.IsValidPosition(currentPos))
                        return false;

                    var tileResult = grid.GetTile(currentPos);
                    if (tileResult.Match(
                        Succ: tile => tile.BlocksLineOfSight,
                        Fail: _ => true))
                    {
                        return false;
                    }
                }

                if (error > 0)
                {
                    x += xInc;
                    error -= dy;
                }
                else
                {
                    y += yInc;
                    error += dx;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all positions that have line of sight to a given position.
        /// Useful for AI threat detection and tactical analysis.
        /// </summary>
        public static Fin<ImmutableHashSet<Position>> GetPositionsWithLineOfSight(
            Position target,
            int maxRange,
            Darklands.Core.Domain.Grid.Grid grid)
        {
            if (!grid.IsValidPosition(target))
                return FinFail<ImmutableHashSet<Position>>(Error.New($"Target position {target} is outside grid bounds"));

            var positions = new System.Collections.Generic.HashSet<Position>();
            var visionRange = VisionRange.Create(maxRange).IfFail(VisionRange.Blind);
            var bounds = visionRange.GetBounds(target);

            for (int x = Math.Max(0, bounds.min.X); x <= Math.Min(grid.Width - 1, bounds.max.X); x++)
            {
                for (int y = Math.Max(0, bounds.min.Y); y <= Math.Min(grid.Height - 1, bounds.max.Y); y++)
                {
                    var pos = new Position(x, y);
                    if (pos != target && HasLineOfSight(pos, target, grid))
                    {
                        positions.Add(pos);
                    }
                }
            }

            return FinSucc(positions.ToImmutableHashSet());
        }
    }
}
