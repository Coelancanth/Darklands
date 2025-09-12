using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Domain.Debug;
using Darklands.Core.Infrastructure.Debug;
using System;
using System.Threading.Tasks;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Common
{
    /// <summary>
    /// Factory implementation for creating and managing game actors.
    /// Handles all actor creation logic that was previously embedded in presenters.
    /// Maintains clean separation between initialization logic and presentation layer.
    /// </summary>
    public sealed class ActorFactory : IActorFactory
    {
        private readonly IGridStateService _gridStateService;
        private readonly IActorStateService _actorStateService;
        private readonly IStableIdGenerator _idGenerator;
        private readonly ICategoryLogger _logger;

        private ActorId? _playerId;

        /// <summary>
        /// Gets the ID of the current player actor.
        /// Used by other presenters to reference the player.
        /// </summary>
        public ActorId? PlayerId => _playerId;

        /// <summary>
        /// Creates a new ActorFactory with the required dependencies.
        /// </summary>
        /// <param name="gridStateService">Grid state service for actor positioning</param>
        /// <param name="actorStateService">Actor state service for health and combat data</param>
        /// <param name="idGenerator">ID generator for creating stable entity IDs</param>
        /// <param name="logger">Logger for tracking actor creation operations</param>
        public ActorFactory(
            IGridStateService gridStateService,
            IActorStateService actorStateService,
            IStableIdGenerator idGenerator,
            ICategoryLogger logger)
        {
            _gridStateService = gridStateService ?? throw new ArgumentNullException(nameof(gridStateService));
            _actorStateService = actorStateService ?? throw new ArgumentNullException(nameof(actorStateService));
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a test player actor at the specified position.
        /// Registers the actor in both the actor state service and grid state service.
        /// </summary>
        /// <param name="position">Grid position where the player should be created</param>
        /// <param name="name">Name for the player character</param>
        /// <returns>Success with ActorId or failure with error details</returns>
        public Fin<ActorId> CreatePlayer(Position position, string name = "Test Player")
        {
            try
            {
                _logger.Log(LogLevel.Debug, LogCategory.System, "Creating test player at position {Position} with name {Name}", position, name);

                // Create a test player using injected ID generator
                var playerId = ActorId.NewId(_idGenerator);

                // Create a full Actor with health data using domain factory
                var actorResult = Domain.Actor.Actor.CreateAtFullHealth(playerId, 100, name);

                return actorResult.Match(
                    Succ: actor =>
                    {
                        // First, add the actor to the actor state service (for health data)
                        var addResult = _actorStateService.AddActor(actor);
                        if (addResult.IsFail)
                        {
                            return addResult.Match<Fin<ActorId>>(
                                Succ: _ => FinFail<ActorId>(Error.New("Unexpected success in error path")),
                                Fail: error =>
                                {
                                    _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to add test player to actor state service: {Error}", error.Message);
                                    return FinFail<ActorId>(error);
                                });
                        }

                        // Then, place the actor on the grid at the specified position
                        var placeResult = _gridStateService.AddActorToGrid(actor.Id, position);
                        if (placeResult.IsFail)
                        {
                            // Remove from actor state service to maintain consistency
                            _ = _actorStateService.RemoveDeadActor(actor.Id);
                            return placeResult.Match<Fin<ActorId>>(
                                Succ: _ => FinFail<ActorId>(Error.New("Unexpected success in error path")),
                                Fail: error =>
                                {
                                    _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to place test player on grid: {Error}", error.Message);
                                    return FinFail<ActorId>(error);
                                });
                        }

                        // Store the player ID for other presenters to access
                        _playerId = actor.Id;

                        _logger.Log(LogCategory.System, "Successfully created test player {Name} ({ActorId}) at position {Position} with {Health} health",
                            actor.Name, actor.Id, position, actor.Health.Maximum);

                        return FinSucc(actor.Id);
                    },
                    Fail: error =>
                    {
                        _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to create test player actor: {Error}", error.Message);
                        return FinFail<ActorId>(error);
                    });
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.System, "Failed to create test player actor at position {0}: {1}", position, ex.Message);
                return FinFail<ActorId>(Error.New("Failed to create test player actor", ex));
            }
        }

        /// <summary>
        /// Creates a dummy combat target at the specified position.
        /// Useful for testing combat mechanics and providing practice targets.
        /// </summary>
        /// <param name="position">Grid position where the dummy should be created</param>
        /// <param name="health">Maximum health for the dummy actor</param>
        /// <returns>Success with ActorId or failure with error details</returns>
        public Fin<ActorId> CreateDummy(Position position, int health = 50)
        {
            try
            {
                _logger.Log(LogCategory.System, "Creating dummy combat target at position {Position} with {Health} health", position, health);

                // Create the dummy using domain factory pattern (same as existing code)
                var dummyResult = Domain.Actor.DummyActor.Presets.CreateCombatDummy("Combat Dummy");

                return dummyResult.Match(
                    Succ: dummyActor =>
                    {
                        // Convert DummyActor to Actor for service registration (same as handler does)
                        var actorForRegistration = Domain.Actor.Actor.Create(dummyActor.Id, dummyActor.Health, dummyActor.Name);

                        return actorForRegistration.Match(
                            Succ: actor =>
                            {
                                // First, add the actor to the actor state service (for health data)
                                var addResult = _actorStateService.AddActor(actor);
                                if (addResult.IsFail)
                                {
                                    return addResult.Match<Fin<ActorId>>(
                                        Succ: _ => FinFail<ActorId>(Error.New("Unexpected success in error path")),
                                        Fail: error =>
                                        {
                                            _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to add dummy target to actor state service: {Error}", error.Message);
                                            return FinFail<ActorId>(error);
                                        });
                                }

                                // Then, place the actor on the grid at the dummy position
                                var placeResult = _gridStateService.AddActorToGrid(actor.Id, position);
                                if (placeResult.IsFail)
                                {
                                    // Remove from actor state service to maintain consistency
                                    _ = _actorStateService.RemoveDeadActor(actor.Id);
                                    return placeResult.Match<Fin<ActorId>>(
                                        Succ: _ => FinFail<ActorId>(Error.New("Unexpected success in error path")),
                                        Fail: error =>
                                        {
                                            _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to place dummy target on grid: {Error}", error.Message);
                                            return FinFail<ActorId>(error);
                                        });
                                }

                                _logger.Log(LogCategory.System, "Successfully created dummy target {Name} ({ActorId}) at position {Position} with {Health} health",
                                    dummyActor.Name, dummyActor.Id, position, dummyActor.Health.Maximum);

                                return FinSucc(actor.Id);
                            },
                            Fail: error =>
                            {
                                _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to convert dummy to actor: {Error}", error.Message);
                                return FinFail<ActorId>(error);
                            });
                    },
                    Fail: error =>
                    {
                        _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to create dummy actor: {Error}", error.Message);
                        return FinFail<ActorId>(error);
                    });
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.System, "Failed to create dummy combat target at position {0}: {1}", position, ex.Message);
                return FinFail<ActorId>(Error.New("Failed to create dummy combat target", ex));
            }
        }
    }
}
