using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Infrastructure.Events;
using Darklands.Core.Presentation.Presenters;
using Darklands.Presentation.UI;
using Darklands.Presentation.Infrastructure;
using Darklands.Views;
using Darklands.Core.Infrastructure.Logging;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Debug;
using Darklands.Core.Infrastructure.Debug;
using Godot;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darklands
{
    /// <summary>
    /// Main game manager that bootstraps the Darklands application.
    /// Initializes the dependency injection container, sets up the MVP architecture,
    /// and manages the lifecycle of presenters and views.
    /// Entry point for the Godot application that bridges to the Clean Architecture core.
    /// 
    /// Now inherits from EventAwareNode to subscribe to domain events via the UI Event Bus.
    /// </summary>
    public partial class GameManager : EventAwareNode
    {
        private GridView? _gridView;
        private ActorView? _actorView;
        private GridPresenter? _gridPresenter;
        private ActorPresenter? _actorPresenter;
        private ServiceProvider? _serviceProvider;
        private ICategoryLogger? _logger;

        /// <summary>
        /// Called when the node is added to the scene tree.
        /// Initializes the dependency injection container and sets up the MVP connections.
        /// </summary>
        public override void _Ready()
        {
            // Use minimal console output until logging is initialized
            GD.Print("GameManager starting initialization...");

            try
            {
                // Initialize the DI container FIRST (synchronously for initial setup)
                InitializeDIContainer();

                // NOW call base implementation to initialize EventBus from EventAwareNode
                // This will work because GameStrapper is now initialized
                base._Ready();

                // Continue with initialization sequentially per ADR-009
                try
                {
                    CompleteInitializationAsync().GetAwaiter().GetResult();
                    _logger?.Log(LogLevel.Information, LogCategory.System, "GameManager initialization completed successfully");
                }
                catch (Exception ex)
                {
                    // Use GD.PrintErr as fallback since structured logging may not be available yet
                    GD.PrintErr($"GameManager initialization failed: {ex.Message}");
                    _logger?.Log(LogLevel.Error, LogCategory.System, "GameManager initialization failed: {0}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                // Use GD.PrintErr as fallback since structured logging may not be available yet
                GD.PrintErr($"GameManager._Ready error: {ex.Message}");
                _logger?.Log(LogLevel.Error, LogCategory.System, "GameManager._Ready error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Called when the node is being removed from the scene tree.
        /// Cleans up resources and disposes of presenters.
        /// </summary>
        public override void _ExitTree()
        {
            try
            {
                _logger?.Log(LogLevel.Information, LogCategory.System, "GameManager cleaning up resources...");

                // Dispose presenters
                _gridPresenter?.Dispose();
                _actorPresenter?.Dispose();

                _logger?.Log(LogLevel.Information, LogCategory.System, "Presenters disposed successfully");

                // Dispose DI container
                GameStrapper.Dispose();

                GD.Print("GameManager cleanup completed");
                // Note: Don't log after GameStrapper.Dispose() as logger may be disposed
            }
            catch (Exception ex)
            {
                // Use GD.PrintErr as fallback since logger may be disposed
                GD.PrintErr($"GameManager cleanup error: {ex.Message}");
                _logger?.Log(LogLevel.Error, LogCategory.System, "GameManager cleanup error: {0}", ex.Message);
            }
            finally
            {
                // CRITICAL: Call base implementation to unsubscribe from events
                base._ExitTree();
            }
        }

        /// <summary>
        /// Initializes the DI container synchronously.
        /// Must be called before base._Ready() so EventBus can be retrieved.
        /// </summary>
        private void InitializeDIContainer()
        {
            try
            {
                GD.Print("Initializing DI container...");

                // Godot console sink disabled - using GodotCategoryLogger for all Godot output
                // This prevents duplicate messages and ensures consistent formatting
                // var godotConsoleSink = new GodotConsoleSink(
                //     GodotSinkExtensions.DefaultGodotOutputTemplate,
                //     null);

                // Initialize the dependency injection container
                // Load initial log level from debug configuration to respect user preferences
                var initialLogLevel = DebugConfig.LoadInitialLogLevel();
                
                // Detect editor mode and use appropriate configuration
                GameStrapperConfiguration config;
                if (OS.HasFeature("editor"))
                {
                    // Editor mode: use fixed log file that overwrites on each run
                    config = GameStrapperConfiguration.DevelopmentEditor;
                }
                else
                {
                    // Normal mode: use default configuration with rolling logs
                    config = new GameStrapperConfiguration(LogLevel: initialLogLevel);
                }
                
                var initResult = GameStrapper.Initialize(config, null); // No GodotConsoleSink to avoid duplicates
                if (initResult.IsFail)
                {
                    var error = initResult.Match(
                        Succ: _ => "Unknown error",
                        Fail: err => err.ToString()
                    );
                    throw new InvalidOperationException($"Failed to initialize GameStrapper: {error}");
                }

                _serviceProvider = initResult.Match(
                    Succ: services => services,
                    Fail: _ => throw new InvalidOperationException("GameStrapper initialization returned failure")
                );

                // Configure composite outputs (Godot console + file) at the Godot layer
                var logOutput = _serviceProvider.GetRequiredService<Darklands.Core.Infrastructure.Logging.ILogOutput>();
                if (logOutput is Darklands.Core.Infrastructure.Logging.CompositeLogOutput composite)
                {
                    composite.AddOutput(new GodotConsoleOutput());
                    composite.AddOutput(new Darklands.Core.Infrastructure.Logging.FileLogOutput("logs"));
                }

                // Initialize logger from DI
                _logger = _serviceProvider.GetRequiredService<ICategoryLogger>();

                // Update DebugSystem to use the same UnifiedLogger instance
                if (DebugSystem.Instance != null)
                {
                    DebugSystem.Instance.SetLogger(_logger);
                }

                // TD_052: Initialize the real GodotScopeManager and ServiceLocator
                InitializeScopeManager();

                _logger.Log(LogLevel.Information, LogCategory.System, "DI container initialized successfully - unified logging active");
            }
            catch (Exception ex)
            {
                // Use GD.PrintErr as fallback since structured logging may not be available yet
                GD.PrintErr($"DI container initialization error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// TD_052: Initializes the real GodotScopeManager and configures the ServiceLocator autoload.
        /// This replaces the stub implementation with the actual scope management infrastructure.
        /// </summary>
        private void InitializeScopeManager()
        {
            try
            {
                // Create the real GodotScopeManager
                var logger = _serviceProvider?.GetService<Microsoft.Extensions.Logging.ILogger<Darklands.Presentation.Infrastructure.GodotScopeManager>>();
                var godotScopeManager = new Darklands.Presentation.Infrastructure.GodotScopeManager(_serviceProvider!, logger);

                // Get the ServiceLocator autoload
                var serviceLocator = GetNode<ServiceLocator>("/root/ServiceLocator");
                if (serviceLocator == null)
                {
                    _logger?.Log(LogLevel.Error, LogCategory.System, "ServiceLocator autoload not found - scope management will fall back to GameStrapper");
                    return;
                }

                // Initialize ServiceLocator with the real scope manager
                var serviceLocatorLogger = _serviceProvider?.GetService<Microsoft.Extensions.Logging.ILogger<ServiceLocator>>();
                var initialized = serviceLocator.Initialize(godotScopeManager, serviceLocatorLogger);
                if (initialized)
                {
                    _logger?.Log(LogLevel.Information, LogCategory.System, "ServiceLocator initialized with GodotScopeManager - scope-aware services active");

                    // Create a scope for the GameManager (scene root)
                    var scopeCreated = this.CreateScope();
                    if (scopeCreated)
                    {
                        _logger?.Log(LogLevel.Information, LogCategory.System, "Created DI scope for GameManager scene root");
                    }
                    else
                    {
                        _logger?.Log(LogLevel.Warning, LogCategory.System, "Failed to create DI scope for GameManager - services will use fallback");
                    }
                }
                else
                {
                    _logger?.Log(LogLevel.Error, LogCategory.System, "ServiceLocator initialization failed - scope management unavailable");
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.System, "Error initializing scope manager: {0}", ex.Message);
                GD.PrintErr($"Scope manager initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Completes the initialization by setting up MVP architecture.
        /// </summary>
        private async Task CompleteInitializationAsync()
        {
            try
            {
                // Set up views and presenters on the main thread
                CallDeferred(MethodName.SetupMvpArchitecture);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Use GD.PrintErr as fallback since structured logging may not be available yet
                GD.PrintErr($"Application initialization error: {ex.Message}");
                _logger?.Log(LogLevel.Error, LogCategory.System, "Application initialization error: {0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Sets up the MVP architecture by connecting views and presenters.
        /// Called on the main thread to ensure proper Godot node access.
        /// </summary>
        private void SetupMvpArchitecture()
        {
            try
            {
                _logger?.Log(LogLevel.Information, LogCategory.System, "Setting up MVP architecture...");

                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("ServiceProvider not initialized");
                }

                // Find view nodes in the scene tree
                _gridView = GetNode<GridView>("Grid");
                _actorView = GetNode<ActorView>("Actors");

                if (_gridView == null)
                {
                    throw new InvalidOperationException("GridView node not found. Expected child node named 'Grid'");
                }

                if (_actorView == null)
                {
                    throw new InvalidOperationException("ActorView node not found. Expected child node named 'Actors'");
                }

                _logger?.Log(LogLevel.Information, LogCategory.System, "Views found successfully - Grid: \"{0}\", Actor: \"{1}\" (health consolidated into ActorView)",
                    _gridView?.Name ?? "null", _actorView?.Name ?? "null");

                // Inject logger into views for proper architectural logging
                if (_logger != null)
                {
                    _gridView?.SetLogger(_logger);
                    _actorView?.SetLogger(_logger);
                }

                // TD_052: Use scope-aware service resolution instead of root provider
                // GameManager inherits from EventAwareNode, so we can use the scoped service pattern
                var mediator = this.GetService<IMediator>();
                var gridStateService = this.GetService<Darklands.Core.Application.Grid.Services.IGridStateService>();
                var actorStateService = this.GetService<Darklands.Core.Application.Actor.Services.IActorStateService>();
                var combatQueryService = this.GetService<Darklands.Core.Application.Combat.Services.ICombatQueryService>();
                var actorFactory = this.GetService<Darklands.Core.Application.Common.IActorFactory>();

                _gridPresenter = new GridPresenter(_gridView!, mediator, _logger!, gridStateService, combatQueryService, actorFactory);
                _actorPresenter = new ActorPresenter(_actorView!, mediator, _logger!, actorFactory, actorStateService, combatQueryService);

                // Connect views to presenters
                _gridView!.SetPresenter(_gridPresenter);
                _actorView!.SetPresenter(_actorPresenter);

                // CRITICAL: Connect presenters to each other for coordinated updates
                // This was missing and caused the visual movement bug!
                _gridPresenter.SetActorPresenter(_actorPresenter);

                // CRITICAL: Connect ActorPresenter to GridPresenter for initial vision update
                // This ensures player vision is applied after player creation
                _actorPresenter.SetGridPresenter(_gridPresenter);

                // Event subscription is now handled automatically by EventAwareNode base class
                // SubscribeToEvents() will be called after EventBus is initialized
                _logger?.Log(LogLevel.Information, LogCategory.System, "GameManager will subscribe to domain events via UI Event Bus - modern architecture replaces static router");

                _logger?.Log(LogLevel.Information, LogCategory.System, "Presenters created and connected - GridPresenter and ActorPresenter (with consolidated health functionality) initialized with cross-presenter coordination");

                // Initialize presenters (this will set up initial state)
                _gridPresenter.Initialize();
                _actorPresenter.Initialize();

                _logger?.Log(LogLevel.Information, LogCategory.System, "MVP architecture setup completed - application ready for interaction");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.System, "MVP setup error - failed to initialize presenters and views: {0}", ex.Message);
                // Fallback to GD.PrintErr for critical startup errors
                GD.PrintErr($"MVP setup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles unhandled exceptions in background tasks.
        /// Ensures game doesn't crash silently from async operations.
        /// </summary>
        private void HandleUnhandledException(Exception ex)
        {
            _logger?.Log(LogLevel.Error, LogCategory.System, "Unhandled exception in GameManager - application may be unstable: {0}", ex.Message);
            // Fallback to GD.PrintErr for critical errors
            GD.PrintErr($"Unhandled exception in GameManager: {ex.Message}");

            // For development, we might want to crash to surface the issue
            // For production, we'd log and attempt graceful recovery
        }

        /// <summary>
        /// Subscribes to domain events via the UI Event Bus.
        /// Called automatically by EventAwareNode base class after EventBus is initialized.
        /// 
        /// Replaces the static GameManagerEventRouter registration with proper event subscriptions.
        /// Handler methods are implemented in GameManager.NotificationHandlers.cs partial class.
        /// </summary>
        protected override void SubscribeToEvents()
        {
            if (EventBus == null)
            {
                _logger?.Log(LogLevel.Error, LogCategory.System, "Cannot subscribe to events - EventBus is null");
                return;
            }

            try
            {
                // Subscribe to combat events for UI updates
                EventBus.Subscribe<ActorDiedEvent>(this, HandleActorDiedEvent);
                EventBus.Subscribe<ActorDamagedEvent>(this, HandleActorDamagedEvent);

                _logger?.Log(LogLevel.Information, LogCategory.System, "Successfully subscribed to domain events via UI Event Bus");
                _logger?.Log(LogLevel.Information, LogCategory.System, "Modern event architecture active - static router fully replaced");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.System, "Failed to subscribe to domain events: {0}", ex.Message);
                GD.PrintErr($"GameManager event subscription failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Deferred method to update health bar on main thread.
        /// </summary>
        private async void UpdateHealthBarDeferred(string actorIdStr, int oldCurrent, int oldMaximum, int newCurrent, int newMaximum)
        {
            try
            {
                _logger?.Log(LogLevel.Debug, LogCategory.System, "UpdateHealthBarDeferred called for {0}: {1}/{2} → {3}/{4}",
                    actorIdStr, oldCurrent, oldMaximum, newCurrent, newMaximum);

                var actorId = Darklands.Core.Domain.Grid.ActorId.FromGuid(Guid.Parse(actorIdStr));

                // Recreate Health objects from the data
                var oldHealthResult = Darklands.Core.Domain.Actor.Health.Create(oldCurrent, oldMaximum);
                var newHealthResult = Darklands.Core.Domain.Actor.Health.Create(newCurrent, newMaximum);

                await oldHealthResult.Match(
                    Succ: async oldHealth => await newHealthResult.Match(
                        Succ: async newHealth =>
                        {
                            if (_actorPresenter != null)
                            {
                                _logger?.Log(LogLevel.Debug, LogCategory.System, "Calling ActorPresenter.HandleHealthChangedAsync (consolidated functionality)");
                                await _actorPresenter.HandleHealthChangedAsync(actorId, oldHealth, newHealth);
                                _logger?.Log(LogLevel.Debug, LogCategory.System, "Updated health bar for {0}", actorId);
                            }
                            else
                            {
                                _logger?.Log(LogLevel.Error, LogCategory.System, "ActorPresenter is NULL in deferred health update!");
                            }
                        },
                        Fail: error =>
                        {
                            _logger?.Log(LogLevel.Error, LogCategory.System, "Failed to create new health object: {0}", error.Message);
                            return Task.CompletedTask;
                        }
                    ),
                    Fail: error =>
                    {
                        _logger?.Log(LogLevel.Error, LogCategory.System, "Failed to create old health object: {0}", error.Message);
                        return Task.CompletedTask;
                    }
                );
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.System, "Error in deferred health bar update for {0}: {1}", actorIdStr, ex.Message);
            }
        }

        /// <summary>
        /// Deferred method to remove actor and health bar on main thread.
        /// </summary>
        private async void RemoveActorDeferred(string actorIdStr, int x, int y)
        {
            try
            {
                _logger?.Log(LogLevel.Debug, LogCategory.System, "RemoveActorDeferred called for {0} at ({1},{2})", actorIdStr, x, y);

                var actorId = Darklands.Core.Domain.Grid.ActorId.FromGuid(Guid.Parse(actorIdStr));
                var position = new Darklands.Core.Domain.Grid.Position(x, y);

                // Remove actor sprite
                if (_actorPresenter != null)
                {
                    _logger?.Log(LogLevel.Debug, LogCategory.System, "Removing actor sprite via presenter");
                    await _actorPresenter.RemoveActorAsync(actorId, position);
                    _logger?.Log(LogLevel.Debug, LogCategory.System, "Removed dead actor {0} sprite", actorId);
                }
                else
                {
                    _logger?.Log(LogLevel.Error, LogCategory.System, "ActorPresenter is NULL in deferred removal!");
                }

                // Health bar removal is now handled automatically by ActorView when actor is removed
                _logger?.Log(LogLevel.Debug, LogCategory.System, "Health bar removal handled automatically via parent-child node relationship");

                _logger?.Log(LogLevel.Debug, LogCategory.System, "Visual cleanup complete for dead actor {0} at {1}", actorId, position);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.System, "Error in deferred actor removal for {0}: {1}", actorIdStr, ex.Message);
            }
        }
    }
}
