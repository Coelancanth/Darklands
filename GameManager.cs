using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Infrastructure.Events;
using Darklands.Core.Presentation.Presenters;
using Darklands.Presentation.UI;
using Darklands.Views;
using Darklands.Infrastructure.Logging;
using Darklands.Core.Domain.Combat;
using Godot;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
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
        private HealthView? _healthView;
        private GridPresenter? _gridPresenter;
        private ActorPresenter? _actorPresenter;
        private HealthPresenter? _healthPresenter;
        private ServiceProvider? _serviceProvider;
        private Serilog.ILogger? _logger;

        /// <summary>
        /// Called when the node is added to the scene tree.
        /// Initializes the dependency injection container and sets up the MVP connections.
        /// </summary>
        public override void _Ready()
        {
            // Use GD.Print here since logger isn't initialized yet
            GD.Print("GameManager starting initialization...");

            try
            {
                // Initialize the DI container FIRST (synchronously for initial setup)
                InitializeDIContainer();

                // NOW call base implementation to initialize EventBus from EventAwareNode
                // This will work because GameStrapper is now initialized
                base._Ready();

                // Continue with async initialization for the rest
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await CompleteInitializationAsync();
                        _logger?.Information("GameManager initialization completed successfully");
                    }
                    catch (Exception ex)
                    {
                        // Use GD.PrintErr as fallback since structured logging may not be available yet
                        GD.PrintErr($"GameManager initialization failed: {ex.Message}");
                        _logger?.Error(ex, "GameManager initialization failed");
                    }
                });
            }
            catch (Exception ex)
            {
                // Use GD.PrintErr as fallback since structured logging may not be available yet
                GD.PrintErr($"GameManager._Ready error: {ex.Message}");
                _logger?.Error(ex, "GameManager._Ready error");
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
                _logger?.Information("GameManager cleaning up resources...");

                // Dispose presenters
                _gridPresenter?.Dispose();
                _actorPresenter?.Dispose();
                _healthPresenter?.Dispose();

                _logger?.Information("Presenters disposed successfully");

                // Dispose DI container
                GameStrapper.Dispose();

                GD.Print("GameManager cleanup completed");
                // Note: Don't log after GameStrapper.Dispose() as logger may be disposed
            }
            catch (Exception ex)
            {
                // Use GD.PrintErr as fallback since logger may be disposed
                GD.PrintErr($"GameManager cleanup error: {ex.Message}");
                _logger?.Error(ex, "GameManager cleanup error");
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

                // Create Godot console sink for rich Editor output
                var godotConsoleSink = new GodotConsoleSink(
                    GodotSinkExtensions.DefaultGodotOutputTemplate,
                    null);

                // Initialize the dependency injection container with Godot console support
                // Use Development configuration to enable Debug level logging
                var initResult = GameStrapper.Initialize(GameStrapperConfiguration.Development, godotConsoleSink);
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

                // Initialize logger after DI container is ready
                _logger = _serviceProvider.GetRequiredService<Serilog.ILogger>();

                _logger.Information("DI container initialized successfully - switching to structured logging");
            }
            catch (Exception ex)
            {
                // Use GD.PrintErr as fallback since structured logging may not be available yet
                GD.PrintErr($"DI container initialization error: {ex.Message}");
                throw;
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
                _logger?.Error(ex, "Application initialization error");
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
                _logger?.Information("Setting up MVP architecture...");

                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("ServiceProvider not initialized");
                }

                // Find view nodes in the scene tree
                _gridView = GetNode<GridView>("Grid");
                _actorView = GetNode<ActorView>("Actors");
                _healthView = GetNode<HealthView>("HealthBars");

                if (_gridView == null)
                {
                    throw new InvalidOperationException("GridView node not found. Expected child node named 'Grid'");
                }

                if (_actorView == null)
                {
                    throw new InvalidOperationException("ActorView node not found. Expected child node named 'Actors'");
                }

                if (_healthView == null)
                {
                    throw new InvalidOperationException("HealthView node not found. Expected child node named 'HealthBars'");
                }

                _logger?.Information("Views found successfully - Grid: {GridView}, Actor: {ActorView}, Health: {HealthView}",
                    _gridView?.Name ?? "null", _actorView?.Name ?? "null", _healthView?.Name ?? "null");

                // Inject logger into views for proper architectural logging
                if (_logger != null)
                {
                    _gridView?.SetLogger(_logger);
                    _actorView?.SetLogger(_logger);
                    _healthView?.SetLogger(_logger);
                }

                // Create presenters manually (they need view interfaces which are Godot-specific)
                var mediator = _serviceProvider.GetRequiredService<IMediator>();
                var gridStateService = _serviceProvider.GetRequiredService<Darklands.Core.Application.Grid.Services.IGridStateService>();
                var actorStateService = _serviceProvider.GetRequiredService<Darklands.Core.Application.Actor.Services.IActorStateService>();
                var combatQueryService = _serviceProvider.GetRequiredService<Darklands.Core.Application.Combat.Services.ICombatQueryService>();
                var actorFactory = _serviceProvider.GetRequiredService<Darklands.Core.Application.Common.IActorFactory>();

                _gridPresenter = new GridPresenter(_gridView!, mediator, _logger!, gridStateService, combatQueryService, actorFactory);
                _actorPresenter = new ActorPresenter(_actorView!, mediator, _logger!, actorFactory, actorStateService);
                _healthPresenter = new HealthPresenter(_healthView!, mediator, _logger!, actorStateService, combatQueryService);

                // Connect views to presenters
                _gridView!.SetPresenter(_gridPresenter);
                _actorView!.SetPresenter(_actorPresenter);
                _healthView!.SetPresenter(_healthPresenter);

                // CRITICAL: Connect presenters to each other for coordinated updates
                // This was missing and caused the visual movement bug!
                _gridPresenter.SetActorPresenter(_actorPresenter);

                // CRITICAL: Connect ActorPresenter to HealthPresenter for health bar creation
                // This connection ensures health bars are created when actors are spawned
                _actorPresenter.SetHealthPresenter(_healthPresenter);

                // CRITICAL: Connect ActorPresenter to GridPresenter for initial vision update
                // This ensures player vision is applied after player creation
                _actorPresenter.SetGridPresenter(_gridPresenter);

                // CRITICAL: Connect HealthPresenter to ActorPresenter for health bar updates (BR_003 fix)
                // This ensures health changes reach the health bars displayed in ActorView
                _healthPresenter.SetActorPresenter(_actorPresenter);

                // Event subscription is now handled automatically by EventAwareNode base class
                // SubscribeToEvents() will be called after EventBus is initialized
                _logger?.Information("GameManager will subscribe to domain events via UI Event Bus - modern architecture replaces static router");

                _logger?.Information("Presenters created and connected - GridPresenter, ActorPresenter, and HealthPresenter initialized with cross-presenter coordination");

                // Initialize presenters (this will set up initial state)
                _gridPresenter.Initialize();
                _actorPresenter.Initialize();
                _healthPresenter.Initialize();

                _logger?.Information("MVP architecture setup completed - application ready for interaction");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "MVP setup error - failed to initialize presenters and views");
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
            _logger?.Fatal(ex, "Unhandled exception in GameManager - application may be unstable");
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
                _logger?.Error("[GameManager] Cannot subscribe to events - EventBus is null");
                return;
            }

            try
            {
                // Subscribe to combat events for UI updates
                EventBus.Subscribe<ActorDiedEvent>(this, HandleActorDiedEvent);
                EventBus.Subscribe<ActorDamagedEvent>(this, HandleActorDamagedEvent);

                _logger?.Information("[GameManager] Successfully subscribed to domain events via UI Event Bus");
                _logger?.Information("Modern event architecture active - static router fully replaced");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "[GameManager] Failed to subscribe to domain events");
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
                _logger?.Information("[GameManager] UpdateHealthBarDeferred called for {ActorIdStr}: {OldCurrent}/{OldMax} → {NewCurrent}/{NewMax}",
                    actorIdStr, oldCurrent, oldMaximum, newCurrent, newMaximum);

                var actorId = Darklands.Core.Domain.Grid.ActorId.FromGuid(Guid.Parse(actorIdStr));

                // Recreate Health objects from the data
                var oldHealthResult = Darklands.Core.Domain.Actor.Health.Create(oldCurrent, oldMaximum);
                var newHealthResult = Darklands.Core.Domain.Actor.Health.Create(newCurrent, newMaximum);

                await oldHealthResult.Match(
                    Succ: async oldHealth => await newHealthResult.Match(
                        Succ: async newHealth =>
                        {
                            if (_healthPresenter != null)
                            {
                                _logger?.Information("[GameManager] Calling HealthPresenter.HandleHealthChangedAsync");
                                await _healthPresenter.HandleHealthChangedAsync(actorId, oldHealth, newHealth);
                                _logger?.Information("Updated health bar for {ActorId}", actorId);
                            }
                            else
                            {
                                _logger?.Error("[GameManager] HealthPresenter is NULL in deferred health update!");
                            }
                        },
                        Fail: error =>
                        {
                            _logger?.Error("Failed to create new health object: {Error}", error.Message);
                            return Task.CompletedTask;
                        }
                    ),
                    Fail: error =>
                    {
                        _logger?.Error("Failed to create old health object: {Error}", error.Message);
                        return Task.CompletedTask;
                    }
                );
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "💥 Error in deferred health bar update for {ActorIdStr}", actorIdStr);
            }
        }

        /// <summary>
        /// Deferred method to remove actor and health bar on main thread.
        /// </summary>
        private async void RemoveActorDeferred(string actorIdStr, int x, int y)
        {
            try
            {
                _logger?.Information("[GameManager] RemoveActorDeferred called for {ActorIdStr} at ({X},{Y})", actorIdStr, x, y);

                var actorId = Darklands.Core.Domain.Grid.ActorId.FromGuid(Guid.Parse(actorIdStr));
                var position = new Darklands.Core.Domain.Grid.Position(x, y);

                // Remove actor sprite
                if (_actorPresenter != null)
                {
                    _logger?.Information("[GameManager] Removing actor sprite via presenter");
                    await _actorPresenter.RemoveActorAsync(actorId, position);
                    _logger?.Information("Removed dead actor {ActorId} sprite", actorId);
                }
                else
                {
                    _logger?.Error("[GameManager] ActorPresenter is NULL in deferred removal!");
                }

                // Remove health bar
                if (_healthPresenter != null)
                {
                    _logger?.Information("[GameManager] Removing health bar via presenter");
                    await _healthPresenter.HandleActorRemovedAsync(actorId, position);
                    _logger?.Information("Removed dead actor {ActorId} health bar", actorId);
                }
                else
                {
                    _logger?.Warning("[GameManager] HealthPresenter is NULL - no health bar to remove");
                }

                _logger?.Information("Visual cleanup complete for dead actor {ActorId} at {Position}", actorId, position);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "💥 Error in deferred actor removal for {ActorIdStr}", actorIdStr);
            }
        }
    }
}
