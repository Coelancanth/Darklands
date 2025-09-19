using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Darklands.Application.Common;
using Darklands.Application.Events;
using static LanguageExt.Prelude;

namespace Darklands.Application.Movement.Handlers;

/// <summary>
/// Handler for MovementCompletedEvent - Finalizes movement and coordinates turn progression.
///
/// Responsibilities:
/// - Log movement completion for debugging and analytics
/// - Coordinate with scheduler for turn progression (actor action complete)
/// - Trigger game state transitions back to ready state
/// - Finalize any movement-related cleanup
///
/// Architecture Notes:
/// - Scheduler-based movement: completion signals scheduler to advance to next actor
/// - Game state coordination: transition from "Animating" back to "Ready" (TD_063)
/// - Clean completion point for movement analytics and debugging
/// </summary>
public sealed class MovementCompletedHandler : INotificationHandler<MovementCompletedEvent>
{
    private readonly ICategoryLogger _logger;

    /// <summary>
    /// Initializes the movement completed handler with required dependencies.
    /// </summary>
    /// <param name="logger">Logger for movement completion tracking</param>
    public MovementCompletedHandler(ICategoryLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles MovementCompletedEvent by finalizing movement and coordinating turn progression.
    ///
    /// Current Phase 2 Scope:
    /// - Movement completion logging and monitoring
    /// - Foundation for scheduler coordination
    ///
    /// Future Integration Points:
    /// - Scheduler notification (actor action complete)
    /// - Game state transition back to "Ready" (TD_063)
    /// - Movement analytics and performance tracking
    /// - Animation cleanup coordination
    /// </summary>
    /// <param name="notification">The movement completed event from domain</param>
    /// <param name="cancellationToken">Cancellation token for async coordination</param>
    /// <returns>Completion task for MediatR pipeline</returns>
    public Task Handle(MovementCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information, LogCategory.Gameplay,
            "Movement completed: Actor {ActorId} arrived at final position {Position}",
            notification.ActorId,
            notification.FinalPosition);

        _logger.Log(LogLevel.Debug, LogCategory.Gameplay,
            "Actor action complete: {ActorId} finished movement, scheduler can advance to next actor",
            notification.ActorId);

        // Phase 2: Foundation complete
        // Phase 3: Scheduler coordination for turn progression
        // Future: Game state transitions, animation cleanup, analytics

        return Task.CompletedTask;
    }
}
