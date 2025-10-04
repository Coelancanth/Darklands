using CSharpFunctionalExtensions;
using Darklands.Core.Features.Combat.Domain.Events;
using Darklands.Core.Infrastructure.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Combat.Application.Commands;

/// <summary>
/// Handler for RemoveActorFromQueueCommand.
/// Removes actor from turn queue and publishes TurnQueueChangedEvent.
/// </summary>
/// <remarks>
/// CRITICAL: If removing last enemy, queue auto-resets to exploration mode (player@0).
/// Event listeners (like CombatModeDetectedHandler) react to IsInCombat flag change.
/// </remarks>
public class RemoveActorFromQueueCommandHandler : IRequestHandler<RemoveActorFromQueueCommand, Result>
{
    private readonly ITurnQueueRepository _turnQueue;
    private readonly IGodotEventBus _eventBus;
    private readonly ILogger<RemoveActorFromQueueCommandHandler> _logger;

    public RemoveActorFromQueueCommandHandler(
        ITurnQueueRepository turnQueue,
        IGodotEventBus eventBus,
        ILogger<RemoveActorFromQueueCommandHandler> logger)
    {
        _turnQueue = turnQueue;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveActorFromQueueCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Removing actor {ActorId} from turn queue", request.ActorId);

        // Railway-oriented: Get queue, remove actor, save, publish event
        return await _turnQueue
            .GetAsync(cancellationToken)
            .Bind(queue => queue
                .Remove(request.ActorId)
                .Tap(async () =>
                {
                    await _turnQueue.SaveAsync(queue, cancellationToken);

                    var wasLastEnemy = !queue.IsInCombat; // If combat ended, this was last enemy

                    _logger.LogInformation(
                        "Actor {ActorId} removed from queue (combat: {IsInCombat}, queue size: {QueueSize}, was last enemy: {WasLastEnemy})",
                        request.ActorId,
                        queue.IsInCombat,
                        queue.Count,
                        wasLastEnemy);

                    // Publish event: combat mode may have changed (combat → exploration)
                    _eventBus.Publish(new TurnQueueChangedEvent(
                        ActorId: request.ActorId,
                        ChangeType: TurnQueueChangeType.ActorRemoved,
                        IsInCombat: queue.IsInCombat,
                        QueueSize: queue.Count));
                }));
    }
}
