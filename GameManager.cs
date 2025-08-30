using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Presentation.Presenters;
using Darklands.Views;
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
    /// </summary>
    public partial class GameManager : Node2D
    {
        private GridView? _gridView;
        private ActorView? _actorView;
        private GridPresenter? _gridPresenter;
        private ActorPresenter? _actorPresenter;
        private ServiceProvider? _serviceProvider;

        /// <summary>
        /// Called when the node is added to the scene tree.
        /// Initializes the dependency injection container and sets up the MVP connections.
        /// </summary>
        public override void _Ready()
        {
            GD.Print("GameManager starting initialization...");

            try
            {
                // Initialize the DI container
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await InitializeApplicationAsync();
                        GD.Print("GameManager initialization completed successfully");
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"GameManager initialization failed: {ex.Message}");
                        GD.PrintErr($"Stack trace: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                GD.PrintErr($"GameManager._Ready error: {ex.Message}");
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
                GD.Print("GameManager cleaning up resources...");

                // Dispose presenters
                _gridPresenter?.Dispose();
                _actorPresenter?.Dispose();

                // Dispose DI container
                GameStrapper.Dispose();

                GD.Print("GameManager cleanup completed");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"GameManager cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes the application core and sets up the MVP architecture.
        /// </summary>
        private async Task InitializeApplicationAsync()
        {
            try
            {
                GD.Print("Initializing DI container...");

                // Initialize the dependency injection container
                var initResult = GameStrapper.Initialize();
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

                GD.Print("DI container initialized successfully");

                // Set up views and presenters on the main thread
                CallDeferred(MethodName.SetupMvpArchitecture);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Application initialization error: {ex.Message}");
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
                GD.Print("Setting up MVP architecture...");

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

                GD.Print("Views found successfully");

                // Create presenters manually (they need view interfaces which are Godot-specific)
                var mediator = _serviceProvider.GetRequiredService<IMediator>();
                var logger = _serviceProvider.GetRequiredService<Serilog.ILogger>();
                
                _gridPresenter = new GridPresenter(_gridView, mediator, logger);
                _actorPresenter = new ActorPresenter(_actorView, mediator, logger);

                // Connect views to presenters
                _gridView.SetPresenter(_gridPresenter);
                _actorView.SetPresenter(_actorPresenter);

                GD.Print("Presenters created and connected to views");

                // Initialize presenters (this will set up initial state)
                _gridPresenter.Initialize();
                _actorPresenter.Initialize();

                GD.Print("MVP architecture setup completed");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"MVP setup error: {ex.Message}");
                GD.PrintErr($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Handles unhandled exceptions in background tasks.
        /// Ensures game doesn't crash silently from async operations.
        /// </summary>
        private void HandleUnhandledException(Exception ex)
        {
            GD.PrintErr($"Unhandled exception in GameManager: {ex.Message}");
            GD.PrintErr($"Stack trace: {ex.StackTrace}");
            
            // For development, we might want to crash to surface the issue
            // For production, we'd log and attempt graceful recovery
        }
    }
}