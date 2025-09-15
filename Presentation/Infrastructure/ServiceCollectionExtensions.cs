using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Darklands.Core.Domain.Services;

namespace Darklands.Presentation.Infrastructure;

/// <summary>
/// Extension methods for registering Godot-specific DI services in the presentation layer.
///
/// This allows the Core project to remain platform-agnostic while providing concrete
/// implementations when Godot is available.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the stub IScopeManager with the actual GodotScopeManager implementation.
    ///
    /// TD_052: This method should be called from the Presentation layer after GameStrapper
    /// has initialized the Core DI container.
    ///
    /// Usage:
    /// ```csharp
    /// // After GameStrapper.Initialize() succeeds:
    /// var serviceProvider = GameStrapper.GetServices().Match(
    ///     Succ: sp => sp,
    ///     Fail: _ => throw new Exception());
    ///
    /// serviceProvider.ReplaceWithGodotScopeManager();
    /// ```
    /// </summary>
    /// <param name="serviceProvider">The initialized service provider from GameStrapper</param>
    /// <returns>A new service provider with GodotScopeManager replacing the stub</returns>
    public static IServiceProvider ReplaceWithGodotScopeManager(this IServiceProvider serviceProvider)
    {
        // Create a new service collection based on the existing container
        var services = new ServiceCollection();

        // Copy all existing services except IScopeManager
        // Note: This is a simplified approach. In a full implementation, you'd want to
        // iterate through the existing services and copy them, but for our case we'll
        // create a new GodotScopeManager that wraps the existing provider

        // Get logger for the new scope manager
        var logger = serviceProvider.GetService<ILogger<GodotScopeManager>>();

        // Create the real implementation
        var godotScopeManager = new GodotScopeManager(serviceProvider, logger);

        // For this approach, we'll return a wrapper provider that delegates to the original
        // but returns our GodotScopeManager for IScopeManager requests
        return new ScopeManagerWrappingProvider(serviceProvider, godotScopeManager);
    }

    /// <summary>
    /// Initializes the ServiceLocator autoload with the proper GodotScopeManager.
    ///
    /// This method should be called from the main Godot project after both GameStrapper
    /// initialization and scope manager replacement are complete.
    /// </summary>
    /// <param name="serviceProvider">The service provider with GodotScopeManager</param>
    /// <returns>True if initialization succeeded, false otherwise</returns>
    public static bool InitializeServiceLocatorAutoload(this IServiceProvider serviceProvider)
    {
        try
        {
            // Try to find the ServiceLocator autoload
            var sceneTree = Godot.Engine.GetMainLoop() as Godot.SceneTree;
            var serviceLocator = sceneTree?.Root?.GetNodeOrNull<ServiceLocator>("/root/ServiceLocator");

            if (serviceLocator == null)
            {
                Godot.GD.PrintErr("[ServiceLocator] Autoload not found at /root/ServiceLocator. " +
                                 "Add ServiceLocator.cs as autoload in project settings.");
                return false;
            }

            // Get the scope manager and logger
            var scopeManager = serviceProvider.GetRequiredService<IScopeManager>();
            var logger = serviceProvider.GetService<ILogger<ServiceLocator>>();

            // Initialize the autoload
            return serviceLocator.Initialize(scopeManager, logger);
        }
        catch (Exception ex)
        {
            Godot.GD.PrintErr($"[ServiceLocator] Error during autoload initialization: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// Service provider wrapper that delegates all requests to the wrapped provider
/// except for IScopeManager, which returns the provided GodotScopeManager instance.
///
/// This allows us to replace the stub IScopeManager without rebuilding the entire DI container.
/// </summary>
internal sealed class ScopeManagerWrappingProvider : IServiceProvider
{
    private readonly IServiceProvider _wrappedProvider;
    private readonly IScopeManager _scopeManager;

    public ScopeManagerWrappingProvider(IServiceProvider wrappedProvider, IScopeManager scopeManager)
    {
        _wrappedProvider = wrappedProvider ?? throw new ArgumentNullException(nameof(wrappedProvider));
        _scopeManager = scopeManager ?? throw new ArgumentNullException(nameof(scopeManager));
    }

    public object? GetService(Type serviceType)
    {
        // Intercept IScopeManager requests
        if (serviceType == typeof(IScopeManager))
        {
            return _scopeManager;
        }

        // Delegate all other requests to the wrapped provider
        return _wrappedProvider.GetService(serviceType);
    }
}