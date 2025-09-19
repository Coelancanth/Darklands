using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Darklands.Application.Common;
using Darklands.Application.Events;
using static LanguageExt.Prelude;

namespace Darklands.Application.Movement.Handlers;

/// <summary>
/// Handler for MovementStartedEvent - Coordinates application-level concerns when movement begins.
///
/// Responsibilities:
/// - Log movement initiation for debugging and monitoring
/// - Prepare for game state transitions (will integrate with TD_063 state manager)
/// - Coordinate with future animation timing systems
///
/// Architecture Notes:
/// - Pure coordination handler, no business logic
/// - Domain event → Application coordination → UIEventBus forwarding (via UIEventForwarder)
/// - Scheduler-based movement: only one actor moves at a time
/// </summary>
public sealed class MovementStartedHandler : INotificationHandler<MovementStartedEvent>
{
    private readonly ICategoryLogger _logger;

    /// <summary>
    /// Initializes the movement started handler with required dependencies.
    /// </summary>
    /// <param name="logger">Logger for movement lifecycle tracking</param>
    public MovementStartedHandler(ICategoryLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the MovementStartedEvent by coordinating application-level movement initiation.
    ///
    /// Current Phase 2 Scope:
    /// - Logging and monitoring
    /// - Foundation for future game state coordination
    ///
    /// Future Integration Points:
    /// - Game state transition to "Animating" (TD_063)
    /// - Animation timing coordination
    /// - Input blocking during movement
    /// </summary>
    /// <param name="notification">The movement started event from domain</param>
    /// <param name="cancellationToken">Cancellation token for async coordination</param>
    /// <returns>Completion task for MediatR pipeline</returns>
    public Task Handle(MovementStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information, LogCategory.Gameplay,
            "Movement started: Actor {ActorId} beginning {Steps}-step path from {Start} to {Destination}",
            notification.ActorId,
            notification.TotalSteps,
            notification.StartPosition,
            notification.Destination);

        _logger.Log(LogLevel.Debug, LogCategory.Gameplay,
            "Movement path details: Actor {ActorId} path = [{Path}]",
            notification.ActorId,
            string.Join(" → ", notification.Path));

        // Phase 2: Foundation complete
        // Phase 3: Game loop will advance movement
        // Future: Game state transitions, animation coordination

        return Task.CompletedTask;
    }
}
