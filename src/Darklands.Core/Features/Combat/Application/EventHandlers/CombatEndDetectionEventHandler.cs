using Darklands.Core.Application;
using Darklands.Core.Features.Combat.Application.Commands;
using Darklands.Core.Features.Combat.Application.Queries;
using Darklands.Core.Features.Grid.Application.Queries;
using Darklands.Core.Features.Grid.Domain;
using Darklands.Core.Features.Grid.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Combat.Application.EventHandlers;

/// <summary>
/// Event handler that subscribes to FOVCalculatedEvent via MediatR.
/// When player's FOV clears (no visible enemies), removes all enemies from turn queue (exits combat mode).
/// </summary>
/// <remarks>
/// BRIDGE: FOV System (VS_005) → Combat System (VS_007) - Exit Path
///
/// EVENT BUS: Receives FOVCalculatedEvent via MediatR (dual publishing pattern).
/// Mirrors EnemyDetectionEventHandler (which handles combat entry).
///
/// KEY BEHAVIOR:
/// - Only processes PLAYER'S FOV events (VS_007 MVP scope)
/// - Checks if player still sees ANY hostile actors
/// - If zero visible enemies → removes ALL scheduled enemies → combat ends
/// - Creates "escape combat" mechanic (run away until enemies out of view)
///
/// COMBAT EXIT FLOW:
/// 1. Player moves (fleeing enemies)
/// 2. FOVCalculatedEvent fires (recalculated vision)
/// 3. This handler checks visible enemies
/// 4. If count == 0 → RemoveActorFromQueueCommand for each scheduled enemy
/// 5. Turn queue drops to 1 actor (player only) → IsInCombat = false
/// 6. Next movement click → ClickToMove queries IsInCombat → exploration mode resumes
///
/// DESIGN RATIONALE:
/// - Combat ends via LINE OF SIGHT, not defeat (simpler, no health system coupling)
/// - Symmetric with EnemyDetectionEventHandler (enter/exit both FOV-driven)
/// - Supports future: enemies defeated, enemies flee, player escapes, fog/smoke breaks LOS
///
/// Per ADR-004 Event Rules:
/// - Rule 3: Terminal subscriber (sends commands, doesn't emit new events)
/// - Rule 4: Max depth = 1 (FOV event → commands, no cascading)
/// </remarks>
public class CombatEndDetectionEventHandler : INotificationHandler<FOVCalculatedEvent>
{
    private readonly IMediator _mediator;
    private readonly IPlayerContext _playerContext;
    private readonly ILogger<CombatEndDetectionEventHandler> _logger;

    public CombatEndDetectionEventHandler(
        IMediator mediator,
        IPlayerContext playerContext,
        ILogger<CombatEndDetectionEventHandler> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(FOVCalculatedEvent notification, CancellationToken cancellationToken)
    {
        // Only process player's FOV events (MVP scope)
        if (!_playerContext.IsPlayer(notification.ActorId))
            return;

        // First check: Are we even in combat? (optimization - skip query if not needed)
        var isInCombatQuery = new IsInCombatQuery();
        var isInCombatResult = await _mediator.Send(isInCombatQuery, cancellationToken);

        if (isInCombatResult.IsFailure || !isInCombatResult.Value)
        {
            // Not in combat, nothing to exit
            return;
        }

        _logger.LogDebug(
            "Checking for combat end: Player FOV recalculated ({VisibleCount} positions)",
            notification.VisiblePositions.Count);

        // TODO: Replace with per-actor vision (VisionConstants → GetActorVisionRadiusQuery when implementing racial bonuses)
        var visibleActorsQuery = new GetVisibleActorsQuery(
            notification.ActorId,
            VisionConstants.DefaultVisionRadius);
        var visibleActorsResult = await _mediator.Send(visibleActorsQuery, cancellationToken);

        if (visibleActorsResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get visible actors for combat end check: {Error}",
                visibleActorsResult.Error);
            return;
        }

        var visibleEnemies = visibleActorsResult.Value;

        if (visibleEnemies.Any())
        {
            // Still enemies in view, combat continues
            _logger.LogDebug(
                "Combat continues: {EnemyCount} enemy(ies) still visible",
                visibleEnemies.Count);
            return;
        }

        // FOV cleared! No enemies visible → Exit combat
        _logger.LogInformation("🏃 FOV cleared - no enemies visible, initiating combat exit");

        // Get ALL scheduled actors (to remove enemies)
        var turnQueueQuery = new GetTurnQueueStateQuery();
        var turnQueueResult = await _mediator.Send(turnQueueQuery, cancellationToken);

        if (turnQueueResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get turn queue for combat exit: {Error}",
                turnQueueResult.Error);
            return;
        }

        var queueState = turnQueueResult.Value;

        // Remove all non-player actors (enemies that left FOV)
        var playerId = _playerContext.GetPlayerId().Value;
        var enemiesToRemove = queueState.ScheduledActors
            .Where(actor => actor.ActorId != playerId)
            .ToList();

        _logger.LogInformation(
            "⚔️ Combat → 🚶 Exploration transition: Removing {EnemyCount} enemy(ies) from turn queue (all out of view), current queue size: {QueueSize}",
            enemiesToRemove.Count,
            queueState.QueueSize);

        foreach (var enemy in enemiesToRemove)
        {
            var removeCommand = new RemoveActorFromQueueCommand(enemy.ActorId);
            var removeResult = await _mediator.Send(removeCommand, cancellationToken);

            if (removeResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to remove enemy {ActorId} from queue: {Error}",
                    enemy.ActorId,
                    removeResult.Error);
            }
            else
            {
                _logger.LogDebug(
                    "Removed enemy {ActorId} (next action: {NextActionTime}) from turn queue",
                    enemy.ActorId,
                    enemy.NextActionTime);
            }
        }

        _logger.LogInformation(
            "✅ Combat ended - Exploration mode resumed (turn queue reset, all enemies escaped)");
    }
}
