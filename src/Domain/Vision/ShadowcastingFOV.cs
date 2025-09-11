using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using Darklands.Core.Domain.Grid;

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
        /// Multipliers for transforming coordinates into each octant.
        /// Each row represents [xx, xy, yx, yy] multipliers for an octant.
        /// </summary>
        private static readonly int[,] Octants = new int[,]
        {
            { 0, -1, -1,  0 },  // Octant 0: North-Northwest
            { -1,  0,  0, -1 },  // Octant 1: West-Northwest
            { -1,  0,  0,  1 },  // Octant 2: West-Southwest
            { 0,  1, -1,  0 },  // Octant 3: South-Southwest
            { 0,  1,  1,  0 },  // Octant 4: South-Southeast
            { 1,  0,  0,  1 },  // Octant 5: East-Southeast
            { 1,  0,  0, -1 },  // Octant 6: East-Northeast
            { 0, -1,  1,  0 },  // Octant 7: North-Northeast
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
                CastShadow(origin, range, grid, octant, visible, 1, 1.0, 0.0);
            }

            return FinSucc(visible.ToImmutableHashSet());
        }

        /// <summary>
        /// Recursively casts shadows in a single octant.
        /// </summary>
        private static void CastShadow(
            Position origin,
            int range,
            Darklands.Core.Domain.Grid.Grid grid,
            int octant,
            System.Collections.Generic.HashSet<Position> visible,
            int row,
            double startSlope,
            double endSlope)
        {
            if (startSlope < endSlope)
                return;

            double nextStartSlope = startSlope;

            for (int currentRow = row; currentRow <= range; currentRow++)
            {
                bool blocked = false;

                // Calculate the range of columns to check in this row
                int minCol = (int)Math.Floor(currentRow * endSlope + 0.5);
                int maxCol = (int)Math.Floor(currentRow * startSlope + 0.5);

                for (int currentCol = minCol; currentCol <= maxCol; currentCol++)
                {
                    // Transform to world coordinates
                    int worldX = origin.X + currentCol * Octants[octant, 0] + currentRow * Octants[octant, 1];
                    int worldY = origin.Y + currentCol * Octants[octant, 2] + currentRow * Octants[octant, 3];
                    var worldPos = new Position(worldX, worldY);

                    // Check if position is within range (using squared distance to avoid sqrt)
                    int distX = worldX - origin.X;
                    int distY = worldY - origin.Y;
                    if (distX * distX + distY * distY > range * range)
                        continue;

                    // Check if position is within grid bounds
                    if (!grid.IsValidPosition(worldPos))
                        continue;

                    // Calculate slopes for this cell
                    double leftSlope = ((double)currentCol - 0.5) / ((double)currentRow + 0.5);
                    double rightSlope = ((double)currentCol + 0.5) / ((double)currentRow - 0.5);

                    // Check if we can see this tile
                    if (rightSlope > startSlope)
                        continue;
                    if (leftSlope < endSlope)
                        continue;

                    // Add to visible set
                    visible.Add(worldPos);

                    // Check if this tile blocks vision
                    var tileResult = grid.GetTile(worldPos);
                    bool blocksVision = tileResult.Match(
                        Succ: tile => tile.BlocksLineOfSight,
                        Fail: _ => true // Out of bounds blocks vision
                    );

                    if (blocked)
                    {
                        // We were blocked and still are
                        if (blocksVision)
                        {
                            nextStartSlope = rightSlope;
                        }
                        else
                        {
                            // We were blocked but now we're not - the blocking has ended
                            blocked = false;
                            startSlope = nextStartSlope;
                        }
                    }
                    else
                    {
                        // We weren't blocked
                        if (blocksVision && currentRow < range)
                        {
                            // Now we are blocked - start a new shadow
                            blocked = true;
                            CastShadow(origin, range, grid, octant, visible,
                                     currentRow + 1, startSlope, leftSlope);
                            nextStartSlope = rightSlope;
                        }
                    }
                }

                // If the row ended in a block, stop processing this octant
                if (blocked)
                    break;
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
