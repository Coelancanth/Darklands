using System;
using System.Threading;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Application.Common;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;
using AppLogLevel = Darklands.Application.Common.LogLevel;

namespace Darklands.Infrastructure.Services
{
    /// <summary>
    /// Production implementation of game time service with configurable timing.
    /// Supports both automatic timer-driven advancement and manual control for testing.
    /// Uses System.Threading.Timer for deterministic, controllable timing behavior.
    /// Thread-safe implementation suitable for game loop integration.
    /// </summary>
    public class GameTimeService : IGameTimeService, IDisposable
    {
        private readonly ICategoryLogger _logger;
        private readonly object _stateLock = new();

        private Timer? _gameTimer;
        private int _currentGameTime;
        private bool _isRunning;
        private bool _isPaused;
        private int _millisecondsPerTick = 200; // ADR-022 default
        private bool _disposed;

        /// <summary>
        /// Initializes a new game time service with logging support.
        /// </summary>
        /// <param name="logger">Category logger for timing events</param>
        public GameTimeService(ICategoryLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public event Action<int>? TimeAdvanced;

        /// <inheritdoc/>
        public int CurrentGameTime
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentGameTime;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsRunning
        {
            get
            {
                lock (_stateLock)
                {
                    return _isRunning && !_isPaused;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsPaused
        {
            get
            {
                lock (_stateLock)
                {
                    return _isPaused;
                }
            }
        }

        /// <inheritdoc/>
        public int MillisecondsPerTick
        {
            get
            {
                lock (_stateLock)
                {
                    return _millisecondsPerTick;
                }
            }
            set
            {
                lock (_stateLock)
                {
                    if (value <= 0)
                        throw new ArgumentException("Tick interval must be positive", nameof(value));

                    _millisecondsPerTick = value;

                    // Update running timer if active
                    if (_isRunning && !_isPaused && _gameTimer != null)
                    {
                        _gameTimer.Change(_millisecondsPerTick, _millisecondsPerTick);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public Fin<Unit> Start()
        {
            lock (_stateLock)
            {
                try
                {
                    if (_disposed)
                        return FinFail<Unit>(Error.New("GameTimeService has been disposed"));

                    if (_isRunning && !_isPaused)
                        return FinFail<Unit>(Error.New("GameTimeService is already running"));

                    _isPaused = false;
                    _isRunning = true;

                    // Create or restart the timer
                    _gameTimer?.Dispose();
                    _gameTimer = new Timer(OnTimerTick, null, _millisecondsPerTick, _millisecondsPerTick);

                    _logger.Log(AppLogLevel.Debug, LogCategory.System,
                        "GameTimeService started with {TickInterval}ms intervals", _millisecondsPerTick);

                    return FinSucc(Unit.Default);
                }
                catch (Exception ex)
                {
                    _logger.Log(AppLogLevel.Error, LogCategory.System,
                        "Failed to start GameTimeService: {Error}", ex.Message);
                    return FinFail<Unit>(Error.New($"Failed to start game time service: {ex.Message}"));
                }
            }
        }

        /// <inheritdoc/>
        public Fin<Unit> Stop()
        {
            lock (_stateLock)
            {
                try
                {
                    if (!_isRunning)
                        return FinFail<Unit>(Error.New("GameTimeService is not running"));

                    _isRunning = false;
                    _isPaused = false;
                    _gameTimer?.Dispose();
                    _gameTimer = null;

                    _logger.Log(AppLogLevel.Debug, LogCategory.System, "GameTimeService stopped");

                    return FinSucc(Unit.Default);
                }
                catch (Exception ex)
                {
                    _logger.Log(AppLogLevel.Error, LogCategory.System,
                        "Failed to stop GameTimeService: {Error}", ex.Message);
                    return FinFail<Unit>(Error.New($"Failed to stop game time service: {ex.Message}"));
                }
            }
        }

        /// <inheritdoc/>
        public Fin<Unit> Pause()
        {
            lock (_stateLock)
            {
                try
                {
                    if (!_isRunning)
                        return FinFail<Unit>(Error.New("GameTimeService is not running"));

                    if (_isPaused)
                        return FinFail<Unit>(Error.New("GameTimeService is already paused"));

                    _isPaused = true;
                    _gameTimer?.Dispose();
                    _gameTimer = null;

                    _logger.Log(AppLogLevel.Debug, LogCategory.System, "GameTimeService paused");

                    return FinSucc(Unit.Default);
                }
                catch (Exception ex)
                {
                    _logger.Log(AppLogLevel.Error, LogCategory.System,
                        "Failed to pause GameTimeService: {Error}", ex.Message);
                    return FinFail<Unit>(Error.New($"Failed to pause game time service: {ex.Message}"));
                }
            }
        }

        /// <inheritdoc/>
        public Fin<Unit> AdvanceTime(int deltaMilliseconds)
        {
            if (deltaMilliseconds <= 0)
                return FinFail<Unit>(Error.New("Delta milliseconds must be positive"));

            lock (_stateLock)
            {
                try
                {
                    _currentGameTime += deltaMilliseconds;

                    _logger.Log(AppLogLevel.Debug, LogCategory.System,
                        "GameTime advanced by {Delta}ms to {Total}ms", deltaMilliseconds, _currentGameTime);

                    // Publish time advancement event
                    TimeAdvanced?.Invoke(deltaMilliseconds);

                    return FinSucc(Unit.Default);
                }
                catch (Exception ex)
                {
                    _logger.Log(AppLogLevel.Error, LogCategory.System,
                        "Failed to advance GameTime: {Error}", ex.Message);
                    return FinFail<Unit>(Error.New($"Failed to advance game time: {ex.Message}"));
                }
            }
        }

        /// <summary>
        /// Timer callback that advances game time automatically.
        /// Thread-safe and handles any exceptions to prevent timer failure.
        /// </summary>
        private void OnTimerTick(object? state)
        {
            try
            {
                // Only advance if we're running and not paused
                lock (_stateLock)
                {
                    if (!_isRunning || _isPaused)
                        return;
                }

                // Advance time by configured tick interval
                var result = AdvanceTime(_millisecondsPerTick);
                if (result.IsFail)
                {
                    result.Match(
                        Succ: _ => { },
                        Fail: error => _logger.Log(AppLogLevel.Warning, LogCategory.System,
                            "Timer tick failed to advance time: {Error}", error.Message)
                    );
                }
            }
            catch (Exception ex)
            {
                // ARCHITECTURAL BOUNDARY: try-catch for timer callback robustness
                _logger.Log(AppLogLevel.Error, LogCategory.System,
                    "Timer tick exception: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Disposes the game time service and releases timer resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_stateLock)
            {
                _gameTimer?.Dispose();
                _gameTimer = null;
                _isRunning = false;
                _isPaused = false;
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
