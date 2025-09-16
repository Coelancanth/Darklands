using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using Darklands.Application.Infrastructure.Debug;
using Darklands.Application.Combat.Common;
using Darklands.Application.Combat.Commands;
using Darklands.Application.Combat.Queries;
using Darklands.Domain.Combat;
using Darklands.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Application.Combat.Coordination
{
    /// <summary>
    /// Coordinates sequential turn-based game processing according to ADR-009.
    /// Eliminates async race conditions by enforcing strictly sequential execution.
    /// 
    /// This replaces the previous concurrent Task.Run() approach with deterministic
    /// turn processing that aligns with traditional roguelike architecture.
    /// </summary>
    public class GameLoopCoordinator
    {
        private readonly IMediator _mediator;
        private readonly ICategoryLogger _logger;

        /// <summary>
        /// Creates a game loop coordinator for sequential turn processing
        /// </summary>
        public GameLoopCoordinator(IMediator mediator, ICategoryLogger logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes a single turn in the game loop according to ADR-009 sequential pattern.
        /// This method coordinates:
        /// 1. Getting next actor from scheduler
        /// 2. Determining action (player input or AI - future)
        /// 3. Executing action synchronously  
        /// 4. Updating presentation (via MediatR notifications)
        /// </summary>
        /// <returns>Success with next actor ID, or None if no actors scheduled</returns>
        public async Task<Fin<Option<ActorId>>> ProcessNextTurnAsync()
        {
            try
            {
                _logger.Log(LogLevel.Debug, LogCategory.System, "Processing next turn in game loop");

                // Step 1: Get next actor from scheduler (synchronously)
                var nextTurnResult = await _mediator.Send(new ProcessNextTurnCommand());

                // Handle result with proper type mapping from Guid to ActorId
                var result = from turnOption in nextTurnResult
                             select turnOption.Map(guid => ActorId.FromGuid(guid));

                // Side effects for logging
                result.Match(
                    Succ: turnOption => turnOption.Match(
                        Some: actorId => _logger.Log(LogCategory.System, "Turn processed for actor {ActorId}", actorId),
                        None: () => _logger.Log(LogLevel.Debug, LogCategory.System, "No actors scheduled for processing")
                    ),
                    Fail: error => _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to process next turn: {Error}", error.Message)
                );

                return result;
            }
            catch (Exception ex)
            {
                var error = Error.New("TURN_PROCESSING_ERROR", ex);
                _logger.Log(LogLevel.Error, LogCategory.System, "Unexpected error during turn processing" + ": " + ex.Message);
                return FinFail<Option<ActorId>>(error);
            }
        }

        /// <summary>
        /// Initializes the game loop by scheduling all initial actors.
        /// This replaces the previous concurrent actor creation with sequential setup.
        /// </summary>
        /// <param name="initialActors">Actors to schedule at game start</param>
        /// <returns>Success or error if scheduling failed</returns>
        public async Task<Fin<LanguageExt.Unit>> InitializeGameLoopAsync(ISchedulable[] initialActors)
        {
            try
            {
                _logger.Log(LogCategory.System, "Initializing game loop with {ActorCount} actors", initialActors.Length);

                var errors = new List<Error>();

                // Schedule each actor sequentially (no concurrency)
                foreach (var actor in initialActors)
                {
                    // For now, we need a position - this will be provided by caller in real implementation
                    var dummyPosition = new Position(5, 5);
                    var scheduleCommand = ScheduleActorCommand.Create(ActorId.FromGuid(actor.Id), dummyPosition, actor.NextTurn);
                    var scheduleResult = await _mediator.Send(scheduleCommand);

                    scheduleResult.Match(
                        Succ: _ => _logger.Log(LogLevel.Debug, LogCategory.System, "Scheduled actor {ActorId} for turn {NextTurn}",
                            actor.Id, actor.NextTurn),
                        Fail: error =>
                        {
                            _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to schedule actor {ActorId}: {Error}",
                                actor.Id, error.Message);
                            errors.Add(error);
                        }
                    );
                }

                // Return success if all actors scheduled, otherwise aggregate errors  
                if (errors.Count == 0)
                {
                    _logger.Log(LogCategory.System, "Game loop initialized successfully");
                    return FinSucc(LanguageExt.Unit.Default);
                }
                else
                {
                    var aggregateError = Error.New($"INITIALIZATION_PARTIAL_FAILURE: {errors.Count}/{initialActors.Length} actors failed to schedule",
                        (Exception)new AggregateException(errors.Select(e => new Exception(e.Message))));
                    return FinFail<LanguageExt.Unit>(aggregateError);
                }
            }
            catch (Exception ex)
            {
                var error = Error.New("INITIALIZATION_ERROR", ex);
                _logger.Log(LogLevel.Error, LogCategory.System, "Unexpected error during game loop initialization" + ": " + ex.Message);
                return FinFail<LanguageExt.Unit>(error);
            }
        }

        /// <summary>
        /// Gets the current turn order for debugging and display purposes.
        /// This provides visibility into the scheduler state without side effects.
        /// </summary>
        /// <returns>Current turn order or error if retrieval failed</returns>
        public async Task<Fin<IReadOnlyList<ISchedulable>>> GetCurrentTurnOrderAsync()
        {
            try
            {
                var queryResult = await _mediator.Send(new GetSchedulerQuery());

                return queryResult.Match(
                    Succ: turnOrder => FinSucc(turnOrder),
                    Fail: error =>
                    {
                        _logger.Log(LogLevel.Warning, LogCategory.System, "Failed to get scheduler for turn order: {Error}", error.Message);
                        return FinFail<IReadOnlyList<ISchedulable>>(error);
                    }
                );
            }
            catch (Exception ex)
            {
                var error = Error.New("TURN_ORDER_QUERY_ERROR", ex);
                _logger.Log(LogLevel.Error, LogCategory.System, "Unexpected error getting turn order" + ": " + ex.Message);
                return FinFail<IReadOnlyList<ISchedulable>>(error);
            }
        }

        /// <summary>
        /// Checks if the game loop has any scheduled actors remaining.
        /// Useful for game over detection and state queries.
        /// </summary>
        /// <returns>True if actors are scheduled, false if empty, error on failure</returns>
        public async Task<Fin<bool>> HasScheduledActorsAsync()
        {
            var turnOrderResult = await GetCurrentTurnOrderAsync();

            return turnOrderResult.Match(
                Succ: actors => FinSucc(actors.Count > 0),
                Fail: error => FinFail<bool>(error)
            );
        }
    }
}
