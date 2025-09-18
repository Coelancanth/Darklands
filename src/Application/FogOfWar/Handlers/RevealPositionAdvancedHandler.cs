using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using MediatR;
using Darklands.Application.Common;
using Darklands.Application.FogOfWar.Events;
using Darklands.Application.Vision.Queries;
using Darklands.Application.Vision.Services;
using Darklands.Domain.Vision;
using static LanguageExt.Prelude;

namespace Darklands.Application.FogOfWar.Handlers
{
    /// <summary>
    /// Handler for RevealPositionAdvancedNotification that triggers progressive FOV updates.
    /// Recalculates field of view when an actor's logical position advances during movement.
    /// Implements the core FOV coordination for TD_061 Progressive FOV functionality.
    /// </summary>
    public class RevealPositionAdvancedHandler : INotificationHandler<RevealPositionAdvancedNotification>
    {
        private readonly IMediator _mediator;
        private readonly IVisionStateService _visionStateService;
        private readonly ICategoryLogger _logger;

        // Standard vision range for player actors - could be made configurable later
        private static readonly VisionRange DefaultVisionRange = VisionRange.Create(8).Match(
            Succ: range => range,
            Fail: _ => throw new InvalidOperationException("Failed to create default vision range")
        );

        public RevealPositionAdvancedHandler(
            IMediator mediator,
            IVisionStateService visionStateService,
            ICategoryLogger logger)
        {
            _mediator = mediator;
            _visionStateService = visionStateService;
            _logger = logger;
        }

        public async Task Handle(RevealPositionAdvancedNotification notification, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Vision,
                "Processing progressive FOV update for Actor {ActorId}: {PrevPos} → {NewPos}",
                notification.ActorId, notification.PreviousPosition, notification.NewRevealPosition);

            // Invalidate vision cache since position changed
            var invalidationResult = _visionStateService.InvalidateVisionCache(notification.ActorId);
            invalidationResult.Match(
                Succ: _ => _logger.Log(LogLevel.Debug, LogCategory.Vision,
                    "Invalidated vision cache for Actor {ActorId}", notification.ActorId),
                Fail: error => _logger.Log(LogLevel.Warning, LogCategory.Vision,
                    "Failed to invalidate vision cache for Actor {ActorId}: {Error}",
                    notification.ActorId, error.Message)
            );

            // Calculate new FOV from the advanced position
            var fovQuery = CalculateFOVQuery.Create(
                notification.ActorId,
                notification.NewRevealPosition,
                DefaultVisionRange,
                notification.Turn
            );

            var fovResult = await _mediator.Send(fovQuery, cancellationToken);

            fovResult.Match(
                Succ: visionState =>
                {
                    _logger.Log(LogLevel.Information, LogCategory.Vision,
                        "Progressive FOV updated for Actor {ActorId} at {Position}: {Visible} visible, {Explored} explored",
                        notification.ActorId, notification.NewRevealPosition,
                        visionState.CurrentlyVisible.Count, visionState.PreviouslyExplored.Count);

                    // Additional logging for movement context
                    if (notification.IsDiagonalMove)
                    {
                        _logger.Log(LogLevel.Debug, LogCategory.Vision,
                            "Diagonal movement FOV update completed for Actor {ActorId}",
                            notification.ActorId);
                    }
                },
                Fail: error =>
                {
                    _logger.Log(LogLevel.Error, LogCategory.Vision,
                        "Progressive FOV calculation failed for Actor {ActorId} at {Position}: {Error}",
                        notification.ActorId, notification.NewRevealPosition, error.Message);
                }
            );
        }
    }
}
