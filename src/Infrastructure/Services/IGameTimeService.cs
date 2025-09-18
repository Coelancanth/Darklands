using LanguageExt;
using LanguageExt.Common;
using Unit = LanguageExt.Unit;

namespace Darklands.Infrastructure.Services
{
    /// <summary>
    /// Core game time management service for deterministic, pausable timing.
    /// Manages game time progression independently of wall-clock time for save/load support.
    /// Implements ADR-022 requirement for game-time based (not wall-clock) timing.
    /// </summary>
    public interface IGameTimeService
    {
        /// <summary>
        /// Starts the game time progression.
        /// Time will advance according to configured intervals.
        /// </summary>
        /// <returns>Success or error if already running</returns>
        Fin<Unit> Start();

        /// <summary>
        /// Stops the game time progression.
        /// All timing is halted but state is preserved.
        /// </summary>
        /// <returns>Success or error if not running</returns>
        Fin<Unit> Stop();

        /// <summary>
        /// Pauses the game time progression.
        /// Can be resumed with Start() without losing state.
        /// </summary>
        /// <returns>Success or error if not running</returns>
        Fin<Unit> Pause();

        /// <summary>
        /// Manually advance game time by specified amount.
        /// Used for testing and deterministic timing control.
        /// </summary>
        /// <param name="deltaMilliseconds">Amount of time to advance</param>
        /// <returns>Success or error</returns>
        Fin<Unit> AdvanceTime(int deltaMilliseconds);

        /// <summary>
        /// Gets the current accumulated game time.
        /// Independent of wall-clock time and pausable.
        /// </summary>
        int CurrentGameTime { get; }

        /// <summary>
        /// Indicates if time progression is currently active.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Indicates if time progression is paused but not stopped.
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Gets or sets the time interval between automatic advancement ticks.
        /// Default is 200ms per ADR-022 requirements.
        /// </summary>
        int MillisecondsPerTick { get; set; }

        /// <summary>
        /// Event published when game time advances.
        /// Subscribers can react to time progression for their own timing needs.
        /// </summary>
        event System.Action<int> TimeAdvanced; // deltaMilliseconds
    }
}
