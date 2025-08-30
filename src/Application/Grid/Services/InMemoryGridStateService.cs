using System.Collections.Concurrent;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Domain.Grid;
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
        private Domain.Grid.Grid? _currentGrid;

        public InMemoryGridStateService()
        {
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

                // Simple bounds checking for Phase 2
                return position.X >= 0 && position.Y >= 0 &&
                       position.X < 10 && position.Y < 10;  // Default 10x10 grid
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

            return FinSucc(Unit.Default);
        }

        private void InitializeDefaultGrid()
        {
            lock (_stateLock)
            {
                // Create a simple 10x10 grid with grass terrain for Phase 2
                // TODO: Replace with proper grid creation in Phase 3
                _currentGrid = Domain.Grid.Grid.Create(10, 10, TerrainType.Open).Match(
                    Succ: grid => grid,
                    Fail: error => throw new InvalidOperationException($"Failed to create default grid: {error}")
                );
            }
        }
    }
}
