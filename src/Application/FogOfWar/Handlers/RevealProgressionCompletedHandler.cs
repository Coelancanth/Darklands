using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Darklands.Application.Common;
using Darklands.Application.FogOfWar.Events;

namespace Darklands.Application.FogOfWar.Handlers
{
    /// <summary>
    /// Handler for RevealProgressionCompletedNotification that coordinates movement completion.
    /// Logs movement completion and performs cleanup for progressive FOV systems.
    /// Can be extended for game state transitions and resource cleanup.
    /// </summary>
    public class RevealProgressionCompletedHandler : INotificationHandler<RevealProgressionCompletedNotification>
    {
        private readonly ICategoryLogger _logger;

        public RevealProgressionCompletedHandler(ICategoryLogger logger)
        {
            _logger = logger;
        }

        public Task Handle(RevealProgressionCompletedNotification notification, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                "Progressive movement completed for Actor {ActorId} at final position {FinalPosition}",
                notification.ActorId, notification.FinalPosition);

            // Future Phase 3/4 extensions could include:
            // - Game state transitions (return to normal input handling)
            // - UI cleanup (hide movement progress indicators)
            // - Resource cleanup (free movement progression memory)
            // - Animation system coordination (ensure visual position matches logical)
            // - Audio feedback (movement completion sound)
            // - Turn system coordination (actor ready for next action)

            _logger.Log(LogLevel.Debug, LogCategory.Gameplay,
                "Movement progression cleanup completed for Actor {ActorId} on Turn {Turn}",
                notification.ActorId, notification.Turn);

            return Task.CompletedTask;
        }
    }
}
