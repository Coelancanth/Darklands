using CSharpFunctionalExtensions;
using Darklands.Core.Application;
using Darklands.Core.Features.Combat.Domain.Events;
using Darklands.Core.Infrastructure.Events;
using Darklands.Core.Infrastructure.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Combat.Application.Commands;

/// <summary>
/// Handler for ScheduleActorCommand.
/// Adds actor to turn queue and publishes TurnQueueChangedEvent.
/// </summary>
public class ScheduleActorCommandHandler : IRequestHandler<ScheduleActorCommand, Result>
{
    private readonly ITurnQueueRepository _turnQueue;
    private readonly IGodotEventBus _eventBus;
    private readonly IPlayerContext _playerContext;
    private readonly ILogger<ScheduleActorCommandHandler> _logger;

    public ScheduleActorCommandHandler(
        ITurnQueueRepository turnQueue,
        IGodotEventBus eventBus,
        IPlayerContext playerContext,
        ILogger<ScheduleActorCommandHandler> logger)
    {
        _turnQueue = turnQueue;
        _eventBus = eventBus;
        _playerContext = playerContext;
        _logger = logger;
    }

    public async Task<Result> Handle(ScheduleActorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            " Scheduling actor {ActorId} at time {Time} (isPlayer: {IsPlayer})",
            request.ActorId.ToLogString(_playerContext),
            request.NextActionTime,
            request.IsPlayer);

        // Railway-oriented: Get queue, schedule actor, save, publish event
        return await _turnQueue
            .GetAsync(cancellationToken)
            .Bind(queue => queue
                .Schedule(request.ActorId, request.NextActionTime, request.IsPlayer)
                .Tap(async () =>
                {
                    await _turnQueue.SaveAsync(queue, cancellationToken);

                    _logger.LogInformation(
                        "Actor {ActorId} scheduled at time {Time} (combat: {IsInCombat}, queue size: {QueueSize})",
                        request.ActorId.ToLogString(_playerContext),
                        request.NextActionTime,
                        queue.IsInCombat,
                        queue.Count);

                    // Publish event: combat mode may have changed (exploration → combat)
                    await _eventBus.PublishAsync(new TurnQueueChangedEvent(
                        ActorId: request.ActorId,
                        ChangeType: TurnQueueChangeType.ActorScheduled,
                        IsInCombat: queue.IsInCombat,
                        QueueSize: queue.Count));
                }));
    }
}
