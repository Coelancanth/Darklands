using System;
using Microsoft.Extensions.DependencyInjection;
using Darklands.Presentation.Presenters;
using Darklands.Application.Infrastructure.DependencyInjection;

namespace Darklands.Presentation.DI
{
    /// <summary>
    /// Service configuration for the Presentation layer following MVP pattern.
    /// Registers presenters and view-related services for dependency injection.
    ///
    /// This configuration is called from the Godot project level to wire up
    /// presentation layer dependencies that aren't known to the Core library.
    ///
    /// Per ADR-021: The Presentation project provides the MVP firewall,
    /// ensuring Views can only access Presenters, not Application/Domain directly.
    /// </summary>
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Configures presentation layer services in the DI container.
        /// Called during application startup after Core services are registered.
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        /// <returns>The configured service collection for chaining</returns>
        public static IServiceCollection ConfigurePresentationServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            try
            {
                // Register Presenter interfaces with their implementations
                // Using Transient lifetime as presenters are short-lived and stateless

                // Grid presenter for tactical combat grid view
                services.AddTransient<IGridPresenter, GridPresenter>();

                // Actor presenter for character/entity views
                services.AddTransient<IActorPresenter, ActorPresenter>();

                // Attack presenter for combat action views
                services.AddTransient<IAttackPresenter, AttackPresenter>();

                // Path visualization presenter for A* pathfinding display (VS_014)
                services.AddTransient<IPathVisualizationPresenter, PathVisualizationPresenter>();

                // Note: UIDispatcher must be registered in the Godot main project
                // as it requires Godot Node inheritance

                return services;
            }
            catch (Exception ex)
            {
                // Re-throw with context
                throw new InvalidOperationException($"Error configuring presentation services: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a complete service provider with both Core and Presentation services.
        /// This is typically called from the main Godot project entry point.
        /// </summary>
        /// <returns>Configured service provider or error</returns>
        public static IServiceProvider CreateServiceProvider()
        {
            try
            {
                // Initialize Core services first
                var coreResult = GameStrapper.Initialize();

                if (coreResult.IsFail)
                {
                    var error = coreResult.Match(
                        Succ: _ => "Unexpected success",
                        Fail: err => err.Message);
                    throw new InvalidOperationException($"Failed to initialize Core services: {error}");
                }

                // Get the service provider from GameStrapper
                var provider = coreResult.Match(
                    Succ: sp => sp,
                    Fail: _ => throw new InvalidOperationException("Failed to get service provider"));

                return provider;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create service provider: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Interface for the UIDispatcher to enable testing and mocking.
    /// </summary>
    public interface IUIDispatcher
    {
        void DispatchToMainThread(Action action);
        T DispatchToMainThreadWithResult<T>(Func<T> func);
        int QueuedActionCount { get; }
    }


}
