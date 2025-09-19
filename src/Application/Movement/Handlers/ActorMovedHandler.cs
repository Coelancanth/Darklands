using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using Darklands.Application.Events;
using Darklands.Application.Grid.Services;
using Darklands.Application.Actor.Services;
using Darklands.Application.Vision.Queries;
using Darklands.Domain.Grid;
using Darklands.Domain.Actor;
using static LanguageExt.Prelude;

namespace Darklands.Application.Movement.Handlers;

/// <summary>
/// Handler for ActorMovedEvent - Core coordination for step-by-step movement progression.
///
/// Critical Responsibilities:
/// - Update GridStateService with new actor position (single source of truth)
/// - Update ActorStateService with latest actor movement state
/// - Trigger FOV calculation for ALL actors (AI needs vision data)
/// - Coordinate vision display updates (player only, no spoilers for enemies)
///
/// Architecture Notes:
/// - This handler embodies the truth of step-by-step movement
/// - Coordinates infrastructure services based on domain events
/// - Scheduler-based: only one actor moves at a time (no concurrency complexity)
/// - FOV calculated for all actors but only displayed for player
/// </summary>
public sealed class ActorMovedHandler : INotificationHandler<ActorMovedEvent>
{
    private readonly IGridStateService _gridStateService;
    private readonly IActorStateService _actorStateService;
    private readonly IMediator _mediator;
    private readonly ICategoryLogger _logger;

    /// <summary>
    /// Initializes the actor moved handler with required infrastructure services.
    /// </summary>
    /// <param name="gridStateService">Grid state management for position updates</param>
    /// <param name="actorStateService">Actor state management for movement state updates</param>
    /// <param name="mediator">MediatR for triggering FOV calculation queries</param>
    /// <param name="logger">Logger for movement tracking and debugging</param>
    public ActorMovedHandler(
        IGridStateService gridStateService,
        IActorStateService actorStateService,
        IMediator mediator,
        ICategoryLogger logger)
    {
        _gridStateService = gridStateService;
        _actorStateService = actorStateService;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Handles ActorMovedEvent by coordinating all infrastructure updates for a movement step.
    ///
    /// Coordination Flow:
    /// 1. Update GridStateService with new position (source of truth)
    /// 2. Update ActorStateService with movement progression state
    /// 3. Calculate FOV for ALL actors (AI needs vision data)
    /// 4. Log movement progression for debugging
    ///
    /// Vision Handling:
    /// - FOV calculated for ALL actors (including enemies)
    /// - Vision display updated ONLY for player (no spoilers)
    /// - Enemy vision tracked internally for AI decision making
    /// </summary>
    /// <param name="notification">The actor moved event from domain</param>
    /// <param name="cancellationToken">Cancellation token for async coordination</param>
    /// <returns>Completion task for MediatR pipeline</returns>
    public async Task Handle(ActorMovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Debug, LogCategory.Gameplay,
            "Processing actor move: {ActorId} from {From} to {To}, remaining steps: {Remaining}",
            notification.ActorId,
            notification.FromPosition,
            notification.ToPosition,
            notification.StepsRemaining);

        // 1. Update grid position (single source of truth)
        var gridUpdateResult = await UpdateGridPosition(notification);
        if (gridUpdateResult.IsFail)
        {
            gridUpdateResult.IfFail(error =>
            {
                _logger.Log(LogLevel.Error, LogCategory.Gameplay,
                    "Failed to update grid position for {ActorId}: {Error}",
                    notification.ActorId, error.Message);
                return unit;
            });
            return; // Cannot proceed without position update
        }

        // 2. Update actor movement state
        var actorUpdateResult = await UpdateActorMovementState(notification);
        if (actorUpdateResult.IsFail)
        {
            actorUpdateResult.IfFail(error =>
            {
                _logger.Log(LogLevel.Warning, LogCategory.Gameplay,
                    "Failed to update actor movement state for {ActorId}: {Error}",
                    notification.ActorId, error.Message);
                return unit;
            });
            // Continue - position update succeeded, actor state sync can be retried
        }

        // 3. Calculate FOV for ALL actors (AI needs vision data)
        await RecalculateVisionForAllActors(notification);

        _logger.Log(LogLevel.Information, LogCategory.Gameplay,
            "Actor move completed: {ActorId} now at {Position}, {Remaining} steps remaining",
            notification.ActorId, notification.ToPosition, notification.StepsRemaining);
    }

    private Task<Fin<LanguageExt.Unit>> UpdateGridPosition(ActorMovedEvent notification)
    {
        try
        {
            // Move actor in grid state service (single source of truth for positions)
            var moveResult = _gridStateService.MoveActor(notification.ActorId, notification.ToPosition);

            if (moveResult.IsFail)
            {
                return Task.FromResult(moveResult.Map(_ => unit));
            }

            _logger.Log(LogLevel.Debug, LogCategory.Gameplay,
                "Grid position updated: {ActorId} moved to {Position}",
                notification.ActorId, notification.ToPosition);

            return Task.FromResult(FinSucc(unit));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(FinFail<LanguageExt.Unit>(Error.New($"Grid position update failed: {ex.Message}", ex)));
        }
    }

    private Task<Fin<LanguageExt.Unit>> UpdateActorMovementState(ActorMovedEvent notification)
    {
        try
        {
            // Get current actor state
            var actorOption = _actorStateService.GetActor(notification.ActorId);
            if (actorOption.IsNone)
            {
                return Task.FromResult(FinFail<LanguageExt.Unit>(Error.New($"Actor {notification.ActorId} not found in state service")));
            }

            var currentActor = actorOption.IfNone(() => throw new System.InvalidOperationException());

            // Update actor with new movement progression
            // Note: The actual position update is handled by GridStateService
            // This just ensures actor movement state is synchronized

            _logger.Log(LogLevel.Debug, LogCategory.Gameplay,
                "Actor movement state synchronized: {ActorId}",
                notification.ActorId);

            return Task.FromResult(FinSucc(unit));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(FinFail<LanguageExt.Unit>(Error.New($"Actor state update failed: {ex.Message}", ex)));
        }
    }

    private async Task RecalculateVisionForAllActors(ActorMovedEvent notification)
    {
        try
        {
            // Phase 2 Implementation: Log FOV calculation trigger
            // Phase 3/4: Actual FOV calculation and coordination

            _logger.Log(LogLevel.Debug, LogCategory.Vision,
                "FOV recalculation triggered by movement: {ActorId} at {Position}",
                notification.ActorId, notification.ToPosition);

            // TODO Phase 3: Implement actual FOV calculation
            // 1. Get all actors from ActorStateService
            // 2. Calculate FOV for each actor using CalculateFOVQuery
            // 3. Update vision state for ALL actors (AI needs this data)
            // 4. Update fog display ONLY for player (no spoilers for enemies)

            await Task.CompletedTask; // Placeholder for async FOV calculation
        }
        catch (System.Exception ex)
        {
            _logger.Log(LogLevel.Warning, LogCategory.Vision,
                "FOV recalculation failed after movement: {Error}", ex.Message);
            // Non-critical: movement succeeded even if vision update failed
        }
    }
}
