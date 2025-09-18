using LanguageExt;
using LanguageExt.Common;
using Unit = LanguageExt.Unit;

namespace Darklands.Infrastructure.Services
{
    /// <summary>
    /// Specialized timer service for coordinating movement progression with game time.
    /// Bridges the gap between infrastructure timing and application movement logic.
    /// Manages the timer-driven advancement of movement progressions per ADR-022.
    /// </summary>
    public interface IMovementTimer
    {
        /// <summary>
        /// Initializes the movement timer with required dependencies.
        /// Must be called before starting the timer.
        /// </summary>
        /// <returns>Success or error if already initialized</returns>
        Fin<Unit> Initialize();

        /// <summary>
        /// Starts the movement timer coordination.
        /// Subscribes to game time events and begins movement progression.
        /// </summary>
        /// <returns>Success or error if not initialized or already running</returns>
        Fin<Unit> Start();

        /// <summary>
        /// Stops the movement timer coordination.
        /// Unsubscribes from game time events and halts movement progression.
        /// </summary>
        /// <returns>Success or error if not running</returns>
        Fin<Unit> Stop();

        /// <summary>
        /// Indicates if the movement timer is currently active.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the current turn context for movement operations.
        /// Used to provide turn information to movement progression events.
        /// </summary>
        int CurrentTurn { get; set; }

        /// <summary>
        /// Gets statistics about movement timer performance.
        /// Used for debugging and performance monitoring.
        /// </summary>
        MovementTimerStats GetStats();
    }

    /// <summary>
    /// Performance and debugging statistics for movement timer operation.
    /// </summary>
    public record MovementTimerStats(
        int TotalTimeAdvancement,
        int MovementAdvancementCount,
        int ErrorCount,
        double AverageAdvancementTimeMs
    );
}
