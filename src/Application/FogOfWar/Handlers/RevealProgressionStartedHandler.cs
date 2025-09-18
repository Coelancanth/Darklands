using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Darklands.Application.Common;
using Darklands.Application.FogOfWar.Events;

namespace Darklands.Application.FogOfWar.Handlers
{
    /// <summary>
    /// Handler for RevealProgressionStartedNotification that coordinates movement initiation.
    /// Logs movement start and prepares systems for progressive FOV updates.
    /// Can be extended for UI state transitions and input locking.
    /// </summary>
    public class RevealProgressionStartedHandler : INotificationHandler<RevealProgressionStartedNotification>
    {
        private readonly ICategoryLogger _logger;

        public RevealProgressionStartedHandler(ICategoryLogger logger)
        {
            _logger = logger;
        }

        public Task Handle(RevealProgressionStartedNotification notification, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                "Progressive movement started for Actor {ActorId}: {StepCount} steps from {Start} to {Destination}",
                notification.ActorId, notification.StepCount,
                notification.StartPosition, notification.Destination);

            // Future Phase 3/4 extensions could include:
            // - UI state transitions (show movement indicators)
            // - Input state management (lock player input)
            // - Animation system coordination
            // - Audio feedback (movement start sound)

            _logger.Log(LogLevel.Debug, LogCategory.Gameplay,
                "Movement progression tracking initialized for Actor {ActorId} on Turn {Turn}",
                notification.ActorId, notification.Turn);

            return Task.CompletedTask;
        }
    }
}
