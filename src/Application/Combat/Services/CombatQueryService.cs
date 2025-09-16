using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using Darklands.Application.Actor.Services;
using Darklands.Application.Grid.Services;
using Darklands.Domain.Grid;

namespace Darklands.Application.Combat.Services
{
    /// <summary>
    /// Implementation of ICombatQueryService that coordinates between ActorStateService and GridStateService.
    /// Maintains Single Source of Truth by querying each service separately and composing results.
    /// Thread-safe through delegation to underlying thread-safe services.
    /// </summary>
    public class CombatQueryService : ICombatQueryService
    {
        private readonly IActorStateService _actorStateService;
        private readonly IGridStateService _gridStateService;

        /// <summary>
        /// Initializes a new instance of CombatQueryService.
        /// </summary>
        /// <param name="actorStateService">Service for actor health and stats</param>
        /// <param name="gridStateService">Service for actor positions</param>
        public CombatQueryService(
            IActorStateService actorStateService,
            IGridStateService gridStateService)
        {
            _actorStateService = actorStateService;
            _gridStateService = gridStateService;
        }

        public Option<ActorWithPosition> GetActorWithPosition(ActorId actorId)
        {
            var actorOption = _actorStateService.GetActor(actorId);
            var positionOption = _gridStateService.GetActorPosition(actorId);

            // Only return combined data if both actor and position exist
            return actorOption.Match(
                Some: actor => positionOption.Match(
                    Some: position => Option<ActorWithPosition>.Some(ActorWithPosition.Create(actor, position)),
                    None: () => Option<ActorWithPosition>.None
                ),
                None: () => Option<ActorWithPosition>.None
            );
        }

        public IReadOnlyList<ActorWithPosition> GetAllActorsWithPositions()
        {
            var allPositions = _gridStateService.GetAllActorPositions();
            var result = new List<ActorWithPosition>();

            foreach (var (actorId, position) in allPositions)
            {
                var actorOption = _actorStateService.GetActor(actorId);
                actorOption.Match(
                    Some: actor => result.Add(ActorWithPosition.Create(actor, position)),
                    None: () => { } // Skip actors not found in actor state service
                );
            }

            return result.AsReadOnly();
        }

        public IReadOnlyList<ActorWithPosition> GetActorsInRadius(Position centerPosition, int radius)
        {
            if (radius < 0) return new List<ActorWithPosition>().AsReadOnly();

            return GetAllActorsWithPositions()
                .Where(actorWithPos => actorWithPos.Position.ManhattanDistanceTo(centerPosition) <= radius)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<ActorWithPosition> GetAdjacentActors(Position position)
        {
            return GetAllActorsWithPositions()
                .Where(actorWithPos => actorWithPos.Position.IsAdjacentTo(position))
                .ToList()
                .AsReadOnly();
        }

        public bool IsPositionOccupiedByLivingActor(Position position)
        {
            // First check if position is empty on grid (fast check)
            if (_gridStateService.IsPositionEmpty(position))
                return false;

            // Get all actors at position and check if any are alive
            return GetAllActorsWithPositions()
                .Where(actorWithPos => actorWithPos.Position.Equals(position))
                .Any(actorWithPos => actorWithPos.IsAlive);
        }
    }
}
