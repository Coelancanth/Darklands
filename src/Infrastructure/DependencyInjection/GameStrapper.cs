using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System.Reflection;
using MediatR;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using Serilog.Core;

namespace Darklands.Core.Infrastructure.DependencyInjection;

/// <summary>
/// Central dependency injection bootstrapper for the Darklands game.
/// Implements the proven GameStrapper pattern with fallback-safe logging,
/// MediatR command/query pipeline, and strict service lifetime management.
/// </summary>
public static class GameStrapper
{
    private static readonly object _initializationLock = new();
    private static volatile ServiceProvider? _serviceProvider;
    private static volatile bool _isInitialized;

    /// <summary>
    /// Gets the configured service provider safely.
    /// </summary>
    public static Fin<ServiceProvider> GetServices()
    {
        var provider = _serviceProvider;
        if (provider == null)
            return FinFail<ServiceProvider>(Error.New("GameStrapper not initialized. Call Initialize() first."));
        return FinSucc(provider);
    }

    /// <summary>
    /// Legacy property for backward compatibility. 
    /// DEPRECATED: Use GetServices() for proper error handling.
    /// </summary>
    [Obsolete("Use GetServices() for proper error handling", false)]
    public static ServiceProvider Services
    {
        get
        {
            var result = GetServices();
            return result.Match(
                Succ: provider => provider,
                Fail: error => throw new InvalidOperationException(error.ToString()));
        }
    }

    /// <summary>
    /// Initializes the DI container with all required services.
    /// Thread-safe with double-checked locking pattern.
    /// Uses fallback-safe logging configuration that never crashes the application.
    /// </summary>
    /// <param name="configuration">Optional configuration overrides</param>
    /// <param name="godotConsoleSink">Optional Godot console sink for rich Editor output</param>
    /// <returns>Success/failure result with detailed error information</returns>
    public static Fin<ServiceProvider> Initialize(GameStrapperConfiguration? configuration = null, ILogEventSink? godotConsoleSink = null)
    {
        // Double-checked locking pattern for thread safety
        if (_isInitialized && _serviceProvider != null)
            return FinSucc(_serviceProvider);

        lock (_initializationLock)
        {
            // Check again inside the lock
            if (_isInitialized && _serviceProvider != null)
                return FinSucc(_serviceProvider);

            try
            {
                var config = configuration ?? GameStrapperConfiguration.Default;
                var services = new ServiceCollection();

                // Phase 1: Core Infrastructure  
                var loggingResult = ConfigureLogging(services, config, godotConsoleSink);
                if (loggingResult.IsFail)
                    return loggingResult.Match(
                        Succ: _ => FinFail<ServiceProvider>(Error.New("Unexpected success in error path")),
                        Fail: err => FinFail<ServiceProvider>(err));

                // Phase 2: MediatR Command/Query Pipeline
                var mediatrResult = ConfigureMediatR(services);
                if (mediatrResult.IsFail)
                    return mediatrResult.Match(
                        Succ: _ => FinFail<ServiceProvider>(Error.New("Unexpected success in error path")),
                        Fail: err => FinFail<ServiceProvider>(err));

                // Phase 3: Application Services
                var appServicesResult = ConfigureApplicationServices(services);
                if (appServicesResult.IsFail)
                    return appServicesResult.Match(
                        Succ: _ => FinFail<ServiceProvider>(Error.New("Unexpected success in error path")),
                        Fail: err => FinFail<ServiceProvider>(err));

                // Phase 4: Validation and Build
                var buildResult = BuildAndValidateContainer(services, config);
                if (buildResult.IsFail)
                    return buildResult;

                var serviceProvider = buildResult.Match(
                    Succ: provider => provider,
                    Fail: _ => throw new InvalidOperationException("Build result was expected to succeed"));

                // Log successful initialization
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("GameStrapper");
                logger.LogInformation("GameStrapper initialized successfully with {ServiceCount} services",
                    services.Count);

                // Atomic assignment - set provider before setting initialized flag
                _serviceProvider = serviceProvider;
                _isInitialized = true;

                return FinSucc(_serviceProvider);
            }
            catch (Exception ex)
            {
                // Fallback logging to console if Serilog fails
                Console.WriteLine($"CRITICAL: GameStrapper initialization failed: {ex.Message}");
                return FinFail<ServiceProvider>(Error.New($"GameStrapper initialization failed: {ex.Message}", ex));
            }
        }
    }

    /// <summary>
    /// Global logging level switch that can be updated at runtime.
    /// Allows dynamic control of log verbosity from the debug configuration.
    /// </summary>
    public static readonly LoggingLevelSwitch GlobalLevelSwitch = new(Serilog.Events.LogEventLevel.Information);

    /// <summary>
    /// Configures Serilog with fallback-safe configuration.
    /// Even if logging configuration fails, the application continues to work.
    /// </summary>
    private static Fin<LanguageExt.Unit> ConfigureLogging(IServiceCollection services, GameStrapperConfiguration config, ILogEventSink? godotConsoleSink)
    {
        try
        {
            // Set initial level
            GlobalLevelSwitch.MinimumLevel = config.LogLevel;

            // Create fallback-safe Serilog configuration with dynamic level control
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(GlobalLevelSwitch)
                .Enrich.FromLogContext()
                // Console output disabled - using GodotCategoryLogger and GodotConsoleSink for consistent formatting
                .WriteTo.File(
                    path: config.LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

            // Add Godot console sink if provided (from Godot project layer)
            if (godotConsoleSink != null)
            {
                loggerConfiguration = loggerConfiguration.WriteTo.Sink(godotConsoleSink);
            }

            // Apply custom configuration if provided
            if (config.CustomLoggerConfiguration != null)
                loggerConfiguration = config.CustomLoggerConfiguration(loggerConfiguration);

            // Build logger with fallback protection
            Log.Logger = loggerConfiguration.CreateLogger();

            // Register with DI container
            services.AddSingleton<Serilog.ILogger>(Log.Logger);
            services.AddSingleton<ILoggerFactory>(provider => new SerilogLoggerFactory(Log.Logger));
            services.AddLogging(builder => builder.AddSerilog(Log.Logger));

            return FinSucc(LanguageExt.Unit.Default);
        }
        catch (Exception ex)
        {
            // Fallback: Use console logging if Serilog fails
            Console.WriteLine($"WARNING: Serilog configuration failed, using console fallback: {ex.Message}");
            services.AddLogging(builder => builder.AddConsole());
            return FinSucc(LanguageExt.Unit.Default);
        }
    }

    /// <summary>
    /// Configures MediatR with assembly scanning for commands, queries, and handlers.
    /// </summary>
    private static Fin<LanguageExt.Unit> ConfigureMediatR(IServiceCollection services)
    {
        try
        {
            var coreAssembly = Assembly.GetExecutingAssembly();

            // Register MediatR with assembly scanning
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(coreAssembly);

                // Add logging pipeline behavior
                config.AddOpenBehavior(typeof(LoggingBehavior<,>));

                // Add error handling pipeline behavior
                config.AddOpenBehavior(typeof(ErrorHandlingBehavior<,>));
            });

            return FinSucc(LanguageExt.Unit.Default);
        }
        catch (Exception ex)
        {
            return FinFail<LanguageExt.Unit>(Error.New($"MediatR configuration failed: {ex.Message}", ex));
        }
    }

    /// <summary>
    /// Registers application services with appropriate lifetimes.
    /// Singleton for state services, Transient for handlers and presenters.
    /// </summary>
    private static Fin<LanguageExt.Unit> ConfigureApplicationServices(IServiceCollection services)
    {
        try
        {
            // State services (Singleton - maintain state across operations)
            // Phase 2: Grid state management
            services.AddSingleton<Application.Grid.Services.IGridStateService, Application.Grid.Services.InMemoryGridStateService>();

            // Phase 2: Combat timeline scheduling
            services.AddSingleton<Application.Combat.Services.ICombatSchedulerService, Application.Combat.Services.InMemoryCombatSchedulerService>();

            // Phase 3: Actor state management (including health)
            services.AddSingleton<Application.Actor.Services.IActorStateService, Application.Actor.Services.InMemoryActorStateService>();

            // Phase 2: Vision state management with fog of war
            // Phase 3: Enhanced persistence and performance monitoring
            services.AddSingleton<Infrastructure.Vision.VisionPerformanceMonitor>();
            services.AddSingleton<Application.Vision.Services.IVisionPerformanceMonitor>(provider =>
                provider.GetRequiredService<Infrastructure.Vision.VisionPerformanceMonitor>());
            services.AddSingleton<Application.Vision.Services.IVisionStateService, Infrastructure.Vision.PersistentVisionStateService>();

            // TD_009: Composite query service (coordinates ActorState + Grid services)
            services.AddSingleton<Application.Combat.Services.ICombatQueryService, Application.Combat.Services.CombatQueryService>();

            // TD_011: Game loop coordinator for sequential turn processing
            services.AddSingleton<Application.Combat.Coordination.GameLoopCoordinator>();

            // Repository interfaces (Singleton - typically wrap persistent state)
            // TODO: Register repositories here as they're implemented

            // Presenters (Transient - short-lived, no state)
            // Phase 4: Presentation layer presenters - registered at Godot application level
            // TODO: Register presenters and views in Godot project, not in Core

            // Domain services (Singleton - stateless business logic)

            // TD_022: Core Abstraction Services (ADR-006 Selective Abstraction)
            // These services are abstracted for testing, platform differences, and architectural boundaries
            // NOTE: Using Mock implementations in Core project - Godot implementations are registered in main project

            // Audio Service - Singleton for consistent audio state management
            services.AddSingleton<Domain.Services.IAudioService, Infrastructure.Services.MockAudioService>();

            // Input Service - Singleton for consistent input state and event streaming
            services.AddSingleton<Domain.Services.IInputService, Infrastructure.Services.MockInputService>();

            // Settings Service - Singleton for consistent configuration management across the application
            services.AddSingleton<Domain.Services.ISettingsService, Infrastructure.Services.MockSettingsService>();

            // Deterministic simulation foundation (ADR-004 + TD_026)
            services.AddSingleton<Domain.Determinism.IDeterministicRandom>(provider =>
            {
                // Get game seed from configuration, save file, or generate new seed
                // For now, use a development seed - TODO: integrate with save system
                const ulong developmentSeed = 12345UL;

                var logger = provider.GetService<ILogger<Domain.Determinism.DeterministicRandom>>();
                return new Domain.Determinism.DeterministicRandom(developmentSeed, logger: logger);
            });

            // ID Generation Services (TD_021 Phase 3)
            // Production ID generator for non-deterministic scenarios (saves, user entities)
            services.AddSingleton<Infrastructure.Identity.GuidIdGenerator>();

            // Deterministic ID generator for testing and replay scenarios
            services.AddSingleton<Infrastructure.Identity.DeterministicIdGenerator>(provider =>
            {
                var deterministicRandom = provider.GetRequiredService<Domain.Determinism.IDeterministicRandom>();
                return new Infrastructure.Identity.DeterministicIdGenerator(deterministicRandom);
            });

            // Register the appropriate IStableIdGenerator based on context
            // For production: use GuidIdGenerator for globally unique, non-deterministic IDs
            // For testing: DeterministicIdGenerator is registered separately for test injection
            services.AddSingleton<Domain.Common.IStableIdGenerator>(provider =>
                provider.GetRequiredService<Infrastructure.Identity.GuidIdGenerator>());

            // UI Event Bus (Singleton - bridges MediatR events to Godot UI components)
            // Replaces the old static GameManagerEventRouter with modern publish/subscribe architecture
            services.AddSingleton<Application.Events.IUIEventBus, Infrastructure.Events.UIEventBus>();

            // Note: UIEventForwarder<T> is auto-discovered by MediatR's RegisterServicesFromAssembly
            // No manual registration needed - MediatR finds all INotificationHandler<T> implementations

            // Actor Factory (TD_013) - Clean separation of test data from presenters
            // Singleton to maintain player ID state across the application
            services.AddSingleton<Application.Common.IActorFactory, Application.Common.ActorFactory>();

            return FinSucc(LanguageExt.Unit.Default);
        }
        catch (Exception ex)
        {
            return FinFail<LanguageExt.Unit>(Error.New($"Application services configuration failed: {ex.Message}", ex));
        }
    }

    /// <summary>
    /// Builds the service container with validation enabled.
    /// ValidateOnBuild ensures all dependencies can be resolved at startup.
    /// </summary>
    private static Fin<ServiceProvider> BuildAndValidateContainer(
        IServiceCollection services,
        GameStrapperConfiguration config)
    {
        try
        {
            var serviceProviderOptions = new ServiceProviderOptions
            {
                ValidateOnBuild = config.ValidateOnBuild,
                ValidateScopes = config.ValidateScopes
            };

            var serviceProvider = services.BuildServiceProvider(serviceProviderOptions);

            // Validate critical services can be resolved
            try
            {
                serviceProvider.GetRequiredService<ILoggerFactory>();
                serviceProvider.GetRequiredService<IMediator>();

                return FinSucc(serviceProvider);
            }
            catch (Exception validationEx)
            {
                serviceProvider.Dispose();
                return FinFail<ServiceProvider>(
                    Error.New($"Service validation failed: {validationEx.Message}", validationEx));
            }
        }
        catch (Exception ex)
        {
            return FinFail<ServiceProvider>(
                Error.New($"Container build failed: {ex.Message}", ex));
        }
    }

    /// <summary>
    /// Disposes the DI container and cleans up resources in a thread-safe manner.
    /// </summary>
    public static void Dispose()
    {
        lock (_initializationLock)
        {
            try
            {
                _serviceProvider?.Dispose();
                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: GameStrapper disposal failed: {ex.Message}");
            }
            finally
            {
                _serviceProvider = null;
                _isInitialized = false;
            }
        }
    }
}

/// <summary>
/// Configuration options for GameStrapper initialization.
/// Provides sensible defaults with override capabilities for testing and development.
/// </summary>
public record GameStrapperConfiguration(
    Serilog.Events.LogEventLevel LogLevel = Serilog.Events.LogEventLevel.Information,
    string LogFilePath = "logs/darklands-.log",
    bool ValidateOnBuild = true,
    bool ValidateScopes = true,
    Func<LoggerConfiguration, LoggerConfiguration>? CustomLoggerConfiguration = null)
{
    public static GameStrapperConfiguration Default => new();

    public static GameStrapperConfiguration Development => new(
        LogLevel: Serilog.Events.LogEventLevel.Debug,
        ValidateScopes: true);

    public static GameStrapperConfiguration Testing => new(
        LogLevel: Serilog.Events.LogEventLevel.Warning,
        LogFilePath: "logs/test-.log",
        ValidateScopes: false);
}
