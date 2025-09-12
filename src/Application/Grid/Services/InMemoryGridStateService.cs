using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Common;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Grid.Services
{
    /// <summary>
    /// In-memory implementation of IGridStateService for Phase 2.
    /// Provides thread-safe grid state management for development and testing.
    /// Will be replaced with persistent storage in Phase 3.
    /// </summary>
    public class InMemoryGridStateService : IGridStateService
    {
        private readonly object _stateLock = new();
        private readonly ConcurrentDictionary<ActorId, Position> _actorPositions = new();
        private readonly IStableIdGenerator _idGenerator;
        private Domain.Grid.Grid? _currentGrid;

        public InMemoryGridStateService(IStableIdGenerator idGenerator)
        {
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
            // Initialize with a default 10x10 grid for Phase 2
            InitializeDefaultGrid();
        }

        public Fin<Domain.Grid.Grid> GetCurrentGrid()
        {
            lock (_stateLock)
            {
                return _currentGrid != null
                    ? FinSucc(_currentGrid)
                    : FinFail<Domain.Grid.Grid>(Error.New("GRID_NOT_INITIALIZED: Grid has not been initialized"));
            }
        }

        public bool IsValidPosition(Position position)
        {
            lock (_stateLock)
            {
                if (_currentGrid == null) return false;

                // Use actual grid dimensions for bounds checking
                return position.X >= 0 && position.Y >= 0 &&
                       position.X < _currentGrid.Width && position.Y < _currentGrid.Height;
            }
        }

        public bool IsPositionEmpty(Position position)
        {
            if (!IsValidPosition(position)) return false;

            return !_actorPositions.Values.Any(pos => pos.Equals(position));
        }

        public Option<Position> GetActorPosition(ActorId actorId)
        {
            return _actorPositions.TryGetValue(actorId, out var position)
                ? Some(position)
                : None;
        }

        public Fin<Unit> MoveActor(ActorId actorId, Position toPosition)
        {
            if (!IsValidPosition(toPosition))
                return FinFail<Unit>(Error.New($"INVALID_POSITION: Position {toPosition} is outside grid bounds"));

            if (!IsPositionEmpty(toPosition))
                return FinFail<Unit>(Error.New($"POSITION_OCCUPIED: Position {toPosition} is already occupied"));

            // Check if target tile is passable (prevents moving to walls, water, etc.)
            lock (_stateLock)
            {
                if (_currentGrid == null)
                    return FinFail<Unit>(Error.New("GRID_NOT_INITIALIZED: Grid has not been initialized"));

                var tileResult = _currentGrid.GetTile(toPosition);
                var passabilityCheck = tileResult.Match(
                    Succ: tile => tile.IsPassable
                        ? FinSucc(Unit.Default)
                        : FinFail<Unit>(Error.New($"IMPASSABLE_TERRAIN: Cannot move to {toPosition} - {tile.TerrainType} blocks movement")),
                    Fail: error => FinFail<Unit>(error)
                );

                if (passabilityCheck.IsFail)
                    return passabilityCheck;
            }

            // Update actor position atomically
            _actorPositions.AddOrUpdate(actorId, toPosition, (key, oldPosition) => toPosition);

            return FinSucc(Unit.Default);
        }

        public Fin<Unit> ValidateMove(Position fromPosition, Position toPosition)
        {
            // Check if trying to move to same position
            if (fromPosition.Equals(toPosition))
                return FinFail<Unit>(Error.New("SAME_POSITION: Actor is already at the target position"));

            // Check if target position is valid
            if (!IsValidPosition(toPosition))
                return FinFail<Unit>(Error.New($"INVALID_POSITION: Position {toPosition} is outside grid bounds"));

            // Check if target position is empty
            if (!IsPositionEmpty(toPosition))
                return FinFail<Unit>(Error.New($"POSITION_OCCUPIED: Position {toPosition} is already occupied"));

            // Check if target tile is passable (blocks movement through walls, water, etc.)
            lock (_stateLock)
            {
                if (_currentGrid == null)
                    return FinFail<Unit>(Error.New("GRID_NOT_INITIALIZED: Grid has not been initialized"));

                var tileResult = _currentGrid.GetTile(toPosition);
                return tileResult.Match(
                    Succ: tile => tile.IsPassable
                        ? FinSucc(Unit.Default)
                        : FinFail<Unit>(Error.New($"IMPASSABLE_TERRAIN: Cannot move to {toPosition} - {tile.TerrainType} blocks movement")),
                    Fail: error => FinFail<Unit>(error)
                );
            }
        }

        public Fin<Unit> AddActorToGrid(ActorId actorId, Position position)
        {
            if (!IsValidPosition(position))
                return FinFail<Unit>(Error.New($"INVALID_POSITION: Position {position} is outside grid bounds"));

            if (!IsPositionEmpty(position))
                return FinFail<Unit>(Error.New($"POSITION_OCCUPIED: Position {position} is already occupied"));

            // Check if target tile is passable (prevents spawning on walls, water, etc.)
            lock (_stateLock)
            {
                if (_currentGrid == null)
                    return FinFail<Unit>(Error.New("GRID_NOT_INITIALIZED: Grid has not been initialized"));

                var tileResult = _currentGrid.GetTile(position);
                var passabilityCheck = tileResult.Match(
                    Succ: tile => tile.IsPassable
                        ? FinSucc(Unit.Default)
                        : FinFail<Unit>(Error.New($"IMPASSABLE_TERRAIN: Cannot place actor at {position} - {tile.TerrainType} blocks placement")),
                    Fail: error => FinFail<Unit>(error)
                );

                if (passabilityCheck.IsFail)
                    return passabilityCheck;
            }

            // Add actor to grid atomically
            _actorPositions.AddOrUpdate(actorId, position, (key, oldPosition) => position);

            return FinSucc(Unit.Default);
        }

        public Fin<Unit> RemoveActorFromGrid(ActorId actorId)
        {
            var removed = _actorPositions.TryRemove(actorId, out _);

            return removed
                ? FinSucc(Unit.Default)
                : FinFail<Unit>(Error.New($"ACTOR_NOT_FOUND: Actor {actorId} not found on grid"));
        }

        public IReadOnlyDictionary<ActorId, Position> GetAllActorPositions()
        {
            return new Dictionary<ActorId, Position>(_actorPositions);
        }

        private void InitializeDefaultGrid()
        {
            lock (_stateLock)
            {
                // Create strategic 30x20 test grid for Phase 4 fog of war testing
                // Strategic layout with walls, pillars, and corridors for comprehensive vision testing
                _currentGrid = CreateStrategicTestGrid(_idGenerator);
            }
        }

        /// <summary>
        /// Creates a strategic 30x20 test grid layout for fog of war and vision testing.
        /// Designed for 4K displays (1920x1280 pixels at 64px/tile) with comprehensive tactical scenarios.
        /// Features: Long walls, pillar formations, corridors, and room structures for shadowcasting validation.
        /// </summary>
        /// <returns>Strategic test grid with player at center (15,10) and complex terrain</returns>
        private static Domain.Grid.Grid CreateStrategicTestGrid(IStableIdGenerator idGenerator)
        {
            const int width = 30;
            const int height = 20;

            // Create base empty grid (Open terrain)
            var grid = Domain.Grid.Grid.Create(idGenerator, width, height, TerrainType.Open)
                .IfFail(_ => throw new System.InvalidOperationException("Failed to create strategic test grid"));

            // Strategic Layout Design:
            // - Player at (15, 10) - center position with vision range 8
            // - Complex wall patterns for comprehensive shadowcasting testing
            // - Pillar formations for corner occlusion validation
            // - Room structures with corridors for tactical movement

            // === PERIMETER WALLS (Frame) ===
            // Top and bottom borders
            for (int x = 0; x < width; x++)
            {
                grid = PlaceWall(grid, x, 0);      // Top border
                grid = PlaceWall(grid, x, height - 1); // Bottom border
            }
            // Left and right borders  
            for (int y = 0; y < height; y++)
            {
                grid = PlaceWall(grid, 0, y);         // Left border
                grid = PlaceWall(grid, width - 1, y); // Right border
            }

            // === MAJOR STRUCTURAL WALLS ===
            // Long horizontal wall for shadowcasting validation (with gaps)
            for (int x = 3; x <= 12; x++)
            {
                grid = PlaceWall(grid, x, 6);
            }
            // Gap at (13, 6) for corridor access
            for (int x = 14; x <= 26; x++)
            {
                grid = PlaceWall(grid, x, 6);
            }

            // Vertical dividing wall with strategic gaps
            for (int y = 2; y <= 5; y++)
            {
                grid = PlaceWall(grid, 8, y);
            }
            // Gap at (8, 6) already created by horizontal wall intersection
            for (int y = 7; y <= 10; y++)
            {
                grid = PlaceWall(grid, 8, y);
            }
            // Gap at (8, 11) for access
            for (int y = 12; y <= 17; y++)
            {
                grid = PlaceWall(grid, 8, y);
            }

            // === ROOM STRUCTURES ===
            // Northwest room (closed with single entrance)
            for (int x = 2; x <= 6; x++)
            {
                grid = PlaceWall(grid, x, 2);
                grid = PlaceWall(grid, x, 4);
            }
            for (int y = 2; y <= 4; y++)
            {
                grid = PlaceWall(grid, 2, y);
                grid = PlaceWall(grid, 6, y);
            }
            // Entrance at (4, 4) - remove wall
            grid = PlaceOpen(grid, 4, 4);

            // Northeast room (L-shaped)
            for (int x = 18; x <= 22; x++)
            {
                grid = PlaceWall(grid, x, 2);
            }
            for (int y = 2; y <= 4; y++)
            {
                grid = PlaceWall(grid, 18, y);
                grid = PlaceWall(grid, 22, y);
            }
            grid = PlaceWall(grid, 22, 4);

            // === PILLAR FORMATIONS (Corner occlusion testing) ===
            // Cross formation near player
            grid = PlaceWall(grid, 13, 8);
            grid = PlaceWall(grid, 17, 8);
            grid = PlaceWall(grid, 15, 6);
            grid = PlaceWall(grid, 15, 12);

            // Diagonal pillar line for complex shadows
            grid = PlaceWall(grid, 10, 14);
            grid = PlaceWall(grid, 12, 15);
            grid = PlaceWall(grid, 14, 16);
            grid = PlaceWall(grid, 16, 15);
            grid = PlaceWall(grid, 18, 14);

            // Isolated pillars for corner peeking tests
            grid = PlaceWall(grid, 5, 8);
            grid = PlaceWall(grid, 25, 12);
            grid = PlaceWall(grid, 11, 3);
            grid = PlaceWall(grid, 20, 17);

            // === CORRIDOR SYSTEM ===
            // Main east-west corridor (already created by horizontal wall gap)
            // North-south corridor through center
            for (int y = 8; y <= 12; y++)
            {
                grid = PlaceOpen(grid, 15, y); // Ensure center corridor is open
            }

            // === FOREST AREAS (Additional vision blocking) ===
            // Small forest cluster for varied terrain
            grid = PlaceForest(grid, 24, 8);
            grid = PlaceForest(grid, 25, 8);
            grid = PlaceForest(grid, 24, 9);
            grid = PlaceForest(grid, 25, 9);

            // === SPECIAL TEST POSITIONS ===
            // Ensure player position is open
            grid = PlaceOpen(grid, 15, 10);

            // Monster positions (different vision ranges for testing)
            grid = PlaceOpen(grid, 5, 10);  // Goblin position (range 5)
            grid = PlaceOpen(grid, 20, 15); // Orc position (range 6) 
            grid = PlaceOpen(grid, 25, 5);  // Eagle position (range 12)

            return grid;
        }

        // Helper methods for strategic grid creation
        private static Domain.Grid.Grid PlaceWall(Domain.Grid.Grid grid, int x, int y)
        {
            var position = new Position(x, y);
            return grid.SetTerrain(position, TerrainType.Wall)
                .IfFail(grid);
        }

        private static Domain.Grid.Grid PlaceOpen(Domain.Grid.Grid grid, int x, int y)
        {
            var position = new Position(x, y);
            return grid.SetTerrain(position, TerrainType.Open)
                .IfFail(grid);
        }

        private static Domain.Grid.Grid PlaceForest(Domain.Grid.Grid grid, int x, int y)
        {
            var position = new Position(x, y);
            return grid.SetTerrain(position, TerrainType.Forest)
                .IfFail(grid);
        }
    }
}
