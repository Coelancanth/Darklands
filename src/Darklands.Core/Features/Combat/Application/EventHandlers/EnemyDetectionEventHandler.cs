using Darklands.Core.Application;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Combat.Application.Commands;
using Darklands.Core.Features.Combat.Application.Queries;
using Darklands.Core.Features.Combat.Domain;
using Darklands.Core.Features.Grid.Application.Queries;
using Darklands.Core.Features.Grid.Domain;
using Darklands.Core.Features.Grid.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Combat.Application.EventHandlers;

/// <summary>
/// Event handler that subscribes to FOVCalculatedEvent via MediatR.
/// When player sees new enemies, schedules them in the turn queue (triggers combat mode).
/// </summary>
/// <remarks>
/// BRIDGE: FOV System (VS_005) → Combat System (VS_007)
///
/// EVENT BUS: Receives FOVCalculatedEvent via MediatR (dual publishing pattern).
/// MoveActorCommandHandler publishes to BOTH GodotEventBus (UI) and MediatR (Application).
/// This handler auto-registers via MediatR assembly scan.
///
/// KEY BEHAVIOR:
/// - Only processes PLAYER'S FOV events (VS_007 MVP scope)
/// - Checks each visible position for hostile actors
/// - Schedules new enemies at time=0 (immediate action)
/// - Skips already-scheduled actors (prevents duplicate scheduling during combat)
///
/// REINFORCEMENT HANDLING:
/// - During combat, player moves → FOV recalculates → new enemies detected → auto-scheduled
/// - IsActorScheduledQuery prevents re-scheduling existing combatants
///
/// Per ADR-004 Event Rules:
/// - Rule 3: Terminal subscriber (sends command, doesn't emit new events)
/// - Rule 4: Max depth = 1 (FOV event → command, no cascading)
/// </remarks>
public class EnemyDetectionEventHandler : INotificationHandler<FOVCalculatedEvent>
{
    private readonly IMediator _mediator;
    private readonly IPlayerContext _playerContext;
    private readonly ILogger<EnemyDetectionEventHandler> _logger;

    // TODO: Replace with proper faction/hostility system in future VS
    // For MVP, assume all non-player actors at visible positions are hostile

    public EnemyDetectionEventHandler(
        IMediator mediator,
        IPlayerContext playerContext,
        ILogger<EnemyDetectionEventHandler> logger)
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

        _logger.LogDebug(
            "Processing FOV event for player: {VisibleCount} positions visible",
            notification.VisiblePositions.Count);

        // TODO: Replace with per-actor vision (VisionConstants → GetActorVisionRadiusQuery when implementing racial bonuses)
        var visibleActorsQuery = new GetVisibleActorsQuery(
            notification.ActorId,
            VisionConstants.DefaultVisionRadius);
        var visibleActorsResult = await _mediator.Send(visibleActorsQuery, cancellationToken);

        if (visibleActorsResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get visible actors for FOV event: {Error}",
                visibleActorsResult.Error);
            return;
        }

        var hostileActors = visibleActorsResult.Value; // Already excludes player

        if (!hostileActors.Any())
        {
            _logger.LogDebug("No hostile actors detected in FOV");
            return;
        }

        _logger.LogInformation(
            "👁️ FOV Detection: {HostileCount} hostile actor(s) visible → Initiating combat mode",
            hostileActors.Count);

        _logger.LogDebug(
            "Enemy IDs detected: {ActorIds}",
            string.Join(", ", hostileActors));

        // Schedule each hostile actor (if not already scheduled)
        foreach (var enemyId in hostileActors)
        {
            // Check if already scheduled (prevents duplicate scheduling)
            var isScheduledQuery = new IsActorScheduledQuery(enemyId);
            var isScheduledResult = await _mediator.Send(isScheduledQuery, cancellationToken);

            if (isScheduledResult.IsSuccess && isScheduledResult.Value)
            {
                _logger.LogDebug(
                    "Actor {ActorId} already in turn queue (reinforcement/already engaged), skipping",
                    enemyId);
                continue;
            }

            // Schedule enemy at time=0 (immediate action when first detected)
            const int initialActionTime = 0; // Enemies enter combat ready to act
            var scheduleCommand = new ScheduleActorCommand(
                ActorId: enemyId,
                NextActionTime: TimeUnits.Zero,
                IsPlayer: false);

            var scheduleResult = await _mediator.Send(scheduleCommand, cancellationToken);

            if (scheduleResult.IsFailure)
            {
                _logger.LogError(
                    "❌ Failed to schedule enemy {ActorId}: {Error}",
                    enemyId,
                    scheduleResult.Error);
            }
            else
            {
                _logger.LogInformation(
                    "🚶 Exploration → ⚔️ Combat: Enemy {ActorId} scheduled at time={Time} (immediate action)",
                    enemyId,
                    initialActionTime);
            }
        }
    }
}
