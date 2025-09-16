using LanguageExt;

namespace Darklands.Domain.Grid
{
    /// <summary>
    /// Represents a single tile on the combat grid.
    /// Immutable value object that contains position, terrain, and occupancy information.
    /// </summary>
    public readonly record struct Tile
    {
        /// <summary>
        /// Position of this tile on the grid.
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Type of terrain on this tile, affects movement and line of sight.
        /// </summary>
        public TerrainType TerrainType { get; }

        /// <summary>
        /// Actor currently occupying this tile, if any.
        /// Uses Option to safely represent presence/absence of an occupant.
        /// </summary>
        public Option<ActorId> Occupant { get; }

        /// <summary>
        /// Creates a new tile with the specified properties.
        /// </summary>
        /// <param name="position">Position on the grid</param>
        /// <param name="terrainType">Type of terrain</param>
        /// <param name="occupant">Optional occupant actor</param>
        public Tile(Position position, TerrainType terrainType, Option<ActorId> occupant = default)
        {
            Position = position;
            TerrainType = terrainType;
            Occupant = occupant;
        }

        /// <summary>
        /// Creates a new empty tile at the specified position.
        /// </summary>
        public static Tile CreateEmpty(Position position, TerrainType terrainType = TerrainType.Open) =>
            new(position, terrainType, Option<ActorId>.None);

        /// <summary>
        /// Creates a new tile occupied by the specified actor.
        /// </summary>
        public static Tile CreateOccupied(Position position, ActorId actorId, TerrainType terrainType = TerrainType.Open) =>
            new(position, terrainType, Option<ActorId>.Some(actorId));

        /// <summary>
        /// Checks if this tile is currently occupied by an actor.
        /// </summary>
        public bool IsOccupied => Occupant.IsSome;

        /// <summary>
        /// Checks if this tile is empty (no occupant).
        /// </summary>
        public bool IsEmpty => Occupant.IsNone;

        /// <summary>
        /// Determines if this tile can be moved through based on terrain type and occupancy.
        /// </summary>
        public bool IsPassable => GetTerrainPassability(TerrainType) && IsEmpty;

        /// <summary>
        /// Determines if this tile blocks line of sight based on terrain type.
        /// </summary>
        public bool BlocksLineOfSight => GetTerrainLineOfSightBlocking(TerrainType);

        /// <summary>
        /// Creates a new tile with an actor placed on it.
        /// </summary>
        public Tile WithOccupant(ActorId actorId) =>
            new(Position, TerrainType, Option<ActorId>.Some(actorId));

        /// <summary>
        /// Creates a new tile with the occupant removed.
        /// </summary>
        public Tile WithoutOccupant() =>
            new(Position, TerrainType, Option<ActorId>.None);

        /// <summary>
        /// Creates a new tile with different terrain type.
        /// </summary>
        public Tile WithTerrain(TerrainType newTerrainType) =>
            new(Position, newTerrainType, Occupant);

        /// <summary>
        /// Determines terrain passability based on terrain type.
        /// </summary>
        private static bool GetTerrainPassability(TerrainType terrainType) => terrainType switch
        {
            TerrainType.Open => true,
            TerrainType.Forest => true,
            TerrainType.Rocky => true,
            TerrainType.Hill => true,
            TerrainType.Swamp => true,
            TerrainType.Water => false,   // Impassable
            TerrainType.Wall => false,    // Impassable
            _ => true // Default to passable for unknown terrain
        };

        /// <summary>
        /// Determines line of sight blocking based on terrain type.
        /// </summary>
        private static bool GetTerrainLineOfSightBlocking(TerrainType terrainType) => terrainType switch
        {
            TerrainType.Open => false,
            TerrainType.Forest => true,   // Blocks line of sight
            TerrainType.Rocky => false,   // Partial - but simplified to not blocking
            TerrainType.Hill => false,    // High ground doesn't block
            TerrainType.Swamp => false,   // Low ground doesn't block
            TerrainType.Water => false,   // Water doesn't block
            TerrainType.Wall => true,     // Completely blocks
            _ => false // Default to not blocking
        };

        public override string ToString()
        {
            var occupantStr = Occupant.Match(
                Some: actor => $" Occupied:{actor.Value.ToString()[..8]}",
                None: () => " Empty"
            );
            return $"Tile({Position}, {TerrainType},{occupantStr})";
        }
    }
}
