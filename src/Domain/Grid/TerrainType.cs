namespace Darklands.Core.Domain.Grid
{
    /// <summary>
    /// Defines the different types of terrain that can exist on combat tiles.
    /// Each terrain type affects movement, line of sight, and tactical options.
    /// </summary>
    public enum TerrainType
    {
        /// <summary>
        /// Open ground - no movement penalties, clear line of sight.
        /// </summary>
        Open = 0,

        /// <summary>
        /// Dense forest - reduced movement speed, blocks line of sight.
        /// </summary>
        Forest = 1,

        /// <summary>
        /// Rocky terrain - slows movement, partial line of sight blocking.
        /// </summary>
        Rocky = 2,

        /// <summary>
        /// Water - impassable for most units, blocks movement.
        /// </summary>
        Water = 3,

        /// <summary>
        /// High ground - movement penalty to reach, but provides tactical advantage.
        /// </summary>
        Hill = 4,

        /// <summary>
        /// Mud or swamp - significant movement penalty, difficult terrain.
        /// </summary>
        Swamp = 5,

        /// <summary>
        /// Stone wall or building - completely impassable, blocks line of sight.
        /// </summary>
        Wall = 6
    }
}
