using System;
using System.Diagnostics;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Application.Common;
using Darklands.Application.FogOfWar.Services;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;
using AppLogLevel = Darklands.Application.Common.LogLevel;

namespace Darklands.Infrastructure.Services
{
    /// <summary>
    /// Production implementation of movement timer coordination.
    /// Bridges infrastructure timing (GameTimeService) with application logic (MovementProgressionService).
    /// Thread-safe implementation that handles timing coordination gracefully.
    /// Provides performance monitoring and error resilience.
    /// </summary>
    public class MovementTimer : IMovementTimer, IDisposable
    {
        private readonly ICategoryLogger _logger;
        private readonly IGameTimeService _gameTimeService;
        private readonly IMovementProgressionService _movementProgressionService;
        private readonly object _stateLock = new();

        private bool _isInitialized;
        private bool _isRunning;
        private bool _disposed;
        private int _currentTurn;

        // Performance tracking
        private int _totalTimeAdvancement;
        private int _movementAdvancementCount;
        private int _errorCount;
        private readonly Stopwatch _performanceTimer = new();
        private long _totalAdvancementTimeMs;

        /// <summary>
        /// Initializes a new movement timer with required dependencies.
        /// </summary>
        /// <param name="logger">Category logger for movement timing events</param>
        /// <param name="gameTimeService">Game time service for timing coordination</param>
        /// <param name="movementProgressionService">Movement service for progression advancement</param>
        public MovementTimer(
            ICategoryLogger logger,
            IGameTimeService gameTimeService,
            IMovementProgressionService movementProgressionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gameTimeService = gameTimeService ?? throw new ArgumentNullException(nameof(gameTimeService));
            _movementProgressionService = movementProgressionService ?? throw new ArgumentNullException(nameof(movementProgressionService));
        }

        /// <inheritdoc/>
        public bool IsRunning
        {
            get
            {
                lock (_stateLock)
                {
                    return _isRunning;
                }
            }
        }

        /// <inheritdoc/>
        public int CurrentTurn
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentTurn;
                }
            }
            set
            {
                lock (_stateLock)
                {
                    _currentTurn = value;
                }
            }
        }

        /// <inheritdoc/>
        public Fin<Unit> Initialize()
        {
            lock (_stateLock)
            {
                try
                {
                    if (_disposed)
                        return FinFail<Unit>(Error.New("MovementTimer has been disposed"));

                    if (_isInitialized)
                        return FinFail<Unit>(Error.New("MovementTimer is already initialized"));

                    _isInitialized = true;

                    _logger.Log(AppLogLevel.Debug, LogCategory.System, "MovementTimer initialized successfully");

                    return FinSucc(Unit.Default);
                }
                catch (Exception ex)
                {
                    _logger.Log(AppLogLevel.Error, LogCategory.System,
                        "Failed to initialize MovementTimer: {Error}", ex.Message);
                    return FinFail<Unit>(Error.New($"Failed to initialize movement timer: {ex.Message}"));
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
                        return FinFail<Unit>(Error.New("MovementTimer has been disposed"));

                    if (!_isInitialized)
                        return FinFail<Unit>(Error.New("MovementTimer must be initialized before starting"));

                    if (_isRunning)
                        return FinFail<Unit>(Error.New("MovementTimer is already running"));

                    // Subscribe to game time advancement events
                    _gameTimeService.TimeAdvanced += OnGameTimeAdvanced;
                    _isRunning = true;

                    _logger.Log(AppLogLevel.Information, LogCategory.System,
                        "MovementTimer started - subscribed to GameTimeService events");

                    return FinSucc(Unit.Default);
                }
                catch (Exception ex)
                {
                    _logger.Log(AppLogLevel.Error, LogCategory.System,
                        "Failed to start MovementTimer: {Error}", ex.Message);
                    return FinFail<Unit>(Error.New($"Failed to start movement timer: {ex.Message}"));
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
                        return FinFail<Unit>(Error.New("MovementTimer is not running"));

                    // Unsubscribe from game time events
                    _gameTimeService.TimeAdvanced -= OnGameTimeAdvanced;
                    _isRunning = false;

                    _logger.Log(AppLogLevel.Information, LogCategory.System,
                        "MovementTimer stopped - unsubscribed from GameTimeService events");

                    return FinSucc(Unit.Default);
                }
                catch (Exception ex)
                {
                    _logger.Log(AppLogLevel.Error, LogCategory.System,
                        "Failed to stop MovementTimer: {Error}", ex.Message);
                    return FinFail<Unit>(Error.New($"Failed to stop movement timer: {ex.Message}"));
                }
            }
        }

        /// <inheritdoc/>
        public MovementTimerStats GetStats()
        {
            lock (_stateLock)
            {
                var averageTime = _movementAdvancementCount > 0
                    ? (double)_totalAdvancementTimeMs / _movementAdvancementCount
                    : 0.0;

                return new MovementTimerStats(
                    _totalTimeAdvancement,
                    _movementAdvancementCount,
                    _errorCount,
                    averageTime
                );
            }
        }

        /// <summary>
        /// Handles game time advancement events and coordinates movement progression.
        /// Thread-safe and resilient to movement service errors.
        /// </summary>
        /// <param name="deltaMilliseconds">Amount of time that advanced</param>
        private void OnGameTimeAdvanced(int deltaMilliseconds)
        {
            try
            {
                // Check if we're still running (thread-safe)
                lock (_stateLock)
                {
                    if (!_isRunning || _disposed)
                        return;
                }

                _performanceTimer.Restart();

                // Advance movement progressions
                var result = _movementProgressionService.AdvanceGameTime(deltaMilliseconds, _currentTurn);

                _performanceTimer.Stop();

                // Update performance metrics
                lock (_stateLock)
                {
                    _totalTimeAdvancement += deltaMilliseconds;
                    _movementAdvancementCount++;
                    _totalAdvancementTimeMs += _performanceTimer.ElapsedMilliseconds;
                }

                result.Match(
                    Succ: advancementCount =>
                    {
                        if (advancementCount > 0)
                        {
                            _logger.Log(AppLogLevel.Debug, LogCategory.Gameplay,
                                "Movement timer advanced {Count} positions (+{Delta}ms)",
                                advancementCount, deltaMilliseconds);
                        }
                    },
                    Fail: error =>
                    {
                        lock (_stateLock)
                        {
                            _errorCount++;
                        }

                        _logger.Log(AppLogLevel.Warning, LogCategory.System,
                            "Movement progression failed during time advancement: {Error}", error.Message);
                    }
                );
            }
            catch (Exception ex)
            {
                // ARCHITECTURAL BOUNDARY: try-catch for event handler robustness
                lock (_stateLock)
                {
                    _errorCount++;
                }

                _logger.Log(AppLogLevel.Error, LogCategory.System,
                    "Exception in movement timer event handler: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Disposes the movement timer and releases event subscriptions.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_stateLock)
            {
                if (_isRunning)
                {
                    _gameTimeService.TimeAdvanced -= OnGameTimeAdvanced;
                    _isRunning = false;
                }

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
