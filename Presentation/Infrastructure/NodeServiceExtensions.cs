using System;
using Microsoft.Extensions.DependencyInjection;
using Godot;
using LanguageExt;
using static LanguageExt.Prelude;
using Darklands.Application.Infrastructure.DependencyInjection;

namespace Darklands.Presentation.Infrastructure;

/// <summary>
/// Extension methods for Godot nodes to access dependency injection services.
///
/// Replaces the static GameStrapper.GetServices() pattern with scope-aware service resolution.
/// Provides automatic fallback to legacy pattern if ServiceLocator autoload fails.
///
/// MANDATORY IMPROVEMENT 3: Graceful fallback strategy
/// - Primary: Use ServiceLocator autoload with IScopeManager
/// - Fallback: Use GameStrapper.GetServices() if autoload unavailable
/// - Error Handling: Log issues but never crash node initialization
/// - Performance: Cache ServiceLocator instance per node where possible
///
/// Usage Examples:
/// ```csharp
/// // Replace this legacy pattern:
/// var mediator = GameStrapper.GetServices().Match(
///     Succ: sp => sp.GetRequiredService<IMediator>(),
///     Fail: _ => throw new Exception());
///
/// // With this scope-aware pattern:
/// var mediator = this.GetService<IMediator>();
/// ```
///
/// Scope Resolution Strategy:
/// 1. Find ServiceLocator autoload at /root/ServiceLocator
/// 2. Use IScopeManager to get appropriate provider for node
/// 3. Resolve service from scope-aware provider
/// 4. Fall back to GameStrapper if any step fails
/// </summary>
public static class NodeServiceExtensions
{
    /// <summary>
    /// Gets a required service for the specified node using scope-aware resolution.
    ///
    /// MANDATORY IMPROVEMENT 3: Fallback to GameStrapper if autoload fails
    ///
    /// Resolution Process:
    /// 1. Try to get ServiceLocator autoload
    /// 2. Get scope-appropriate service provider for this node
    /// 3. Resolve service from provider
    /// 4. If any step fails, fall back to GameStrapper.GetServices()
    /// 5. Log fallback usage for diagnostic purposes
    /// </summary>
    /// <typeparam name="T">The service type to resolve</typeparam>
    /// <param name="node">The node requesting the service</param>
    /// <returns>The resolved service instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if service cannot be resolved from any source</exception>
    public static T GetService<T>(this Node node) where T : class
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        try
        {
            // Primary strategy: Use ServiceLocator autoload with scope management
            var serviceLocator = ServiceLocator.GetInstance(node);
            if (serviceLocator?.ScopeManager != null)
            {
                var provider = serviceLocator.ScopeManager.GetProviderForNode(node);
                var service = provider.GetService<T>();

                if (service != null)
                {
                    return service;
                }

                // Service not found in scope, try fallback
                GD.PrintRich($"[color=yellow][ServiceExtensions] Service {typeof(T).Name} not found in scope for {node.Name}, falling back to GameStrapper[/color]");
            }
            else
            {
                // ServiceLocator not available, use fallback immediately
                GD.PrintRich($"[color=yellow][ServiceExtensions] ServiceLocator not available for {node.Name}, using GameStrapper fallback[/color]");
            }

            // MANDATORY IMPROVEMENT 3: Fallback to GameStrapper
            return GetServiceFromGameStrapperFallback<T>(node);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ServiceExtensions] Error resolving {typeof(T).Name} for {node.Name}: {ex.Message}");

            // Last resort: try GameStrapper fallback
            try
            {
                return GetServiceFromGameStrapperFallback<T>(node);
            }
            catch (Exception fallbackEx)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve service {typeof(T).Name} for node {node.Name}. " +
                    $"Primary error: {ex.Message}. Fallback error: {fallbackEx.Message}",
                    ex);
            }
        }
    }

    /// <summary>
    /// Gets an optional service for the specified node using scope-aware resolution.
    /// Returns null if the service is not available from any source.
    ///
    /// Safer alternative to GetService<T>() when the service might not be registered.
    /// </summary>
    /// <typeparam name="T">The service type to resolve</typeparam>
    /// <param name="node">The node requesting the service</param>
    /// <returns>The resolved service instance, or null if not available</returns>
    public static T? GetOptionalService<T>(this Node node) where T : class
    {
        try
        {
            return node.GetService<T>();
        }
        catch (Exception ex)
        {
            GD.PrintRich($"[color=orange][ServiceExtensions] Optional service {typeof(T).Name} not available for {node.Name}: {ex.Message}[/color]");
            return null;
        }
    }

    /// <summary>
    /// Creates a new dependency injection scope for the specified node.
    ///
    /// Scope Creation Rules:
    /// - Scopes are typically created for scene root nodes
    /// - Child nodes inherit parent scopes automatically
    /// - Overlay/modal nodes may create child scopes
    /// - Scopes are automatically disposed when nodes exit the tree
    ///
    /// Usage:
    /// ```csharp
    /// // In scene root node _Ready():
    /// this.CreateScope();
    ///
    /// // Child nodes automatically inherit the scope:
    /// var mediator = this.GetService<IMediator>(); // Gets from parent scope
    /// ```
    /// </summary>
    /// <param name="node">The node to create a scope for</param>
    /// <returns>True if scope was created successfully, false otherwise</returns>
    public static bool CreateScope(this Node node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        try
        {
            var serviceLocator = ServiceLocator.GetInstance(node);
            if (serviceLocator?.ScopeManager == null)
            {
                GD.PrintErr($"[ServiceExtensions] Cannot create scope for {node.Name} - ServiceLocator not available");
                return false;
            }

            var scope = serviceLocator.ScopeManager.CreateScope(node);
            if (scope != null)
            {
                GD.Print($"[ServiceExtensions] Created scope for {node.Name}");
                return true;
            }

            GD.PrintErr($"[ServiceExtensions] Failed to create scope for {node.Name}");
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ServiceExtensions] Error creating scope for {node.Name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets scope diagnostic information for debugging purposes.
    /// Useful for understanding scope hierarchy and performance.
    /// </summary>
    /// <param name="node">The node to get diagnostics for</param>
    /// <returns>Diagnostic information string</returns>
    public static string GetScopeDiagnostics(this Node node)
    {
        if (node == null)
            return "Node is null";

        try
        {
            var serviceLocator = ServiceLocator.GetInstance(node);
            if (serviceLocator?.ScopeManager == null)
            {
                return $"ServiceLocator not available for {node.Name}";
            }

            var diagnostics = serviceLocator.ScopeManager.GetDiagnostics();
            return $"Node: {node.Name}, Path: {node.GetPath()}, " +
                   $"Active Scopes: {diagnostics.ActiveScopeCount}, " +
                   $"Cache Entries: {diagnostics.CachedProviderCount}, " +
                   $"Cache Hit Ratio: {diagnostics.CacheHitRatioPercentage}%, " +
                   $"Avg Resolution: {diagnostics.AverageResolutionTimeMicroseconds}μs";
        }
        catch (Exception ex)
        {
            return $"Error getting diagnostics for {node.Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// Fallback method that uses the legacy GameStrapper pattern.
    /// MANDATORY IMPROVEMENT 3: Provides graceful degradation when ServiceLocator fails.
    /// </summary>
    private static T GetServiceFromGameStrapperFallback<T>(Node node) where T : class
    {
        var gameStrapperResult = GameStrapper.GetServices();
        return gameStrapperResult.Match(
            Succ: serviceProvider =>
            {
                var service = serviceProvider.GetService<T>();
                if (service == null)
                {
                    throw new InvalidOperationException(
                        $"Service {typeof(T).Name} is not registered in GameStrapper for node {node.Name}");
                }
                return service;
            },
            Fail: error => throw new InvalidOperationException(
                $"GameStrapper fallback failed for {typeof(T).Name} on node {node.Name}: {error.Message}")
        );
    }
}

/// <summary>
/// Scene management extension methods for creating scopes during scene transitions.
/// </summary>
public static class SceneManagerExtensions
{
    /// <summary>
    /// Loads a scene with automatic scope creation for the root node.
    /// Ensures proper DI lifecycle management during scene transitions.
    /// </summary>
    /// <param name="sceneTree">The scene tree to load the scene into</param>
    /// <param name="scenePath">The path to the scene file</param>
    /// <returns>The loaded scene root node with scope created</returns>
    public static Node? LoadSceneWithScope(this SceneTree sceneTree, string scenePath)
    {
        try
        {
            var packedScene = GD.Load<PackedScene>(scenePath);
            if (packedScene == null)
            {
                GD.PrintErr($"[SceneManager] Failed to load scene: {scenePath}");
                return null;
            }

            var sceneInstance = packedScene.Instantiate();
            if (sceneInstance == null)
            {
                GD.PrintErr($"[SceneManager] Failed to instantiate scene: {scenePath}");
                return null;
            }

            // Create scope for the new scene root
            if (sceneInstance.CreateScope())
            {
                GD.Print($"[SceneManager] Loaded scene {scenePath} with scope");
            }
            else
            {
                GD.PrintRich($"[color=yellow][SceneManager] Loaded scene {scenePath} but scope creation failed[/color]");
            }

            return sceneInstance;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SceneManager] Error loading scene {scenePath}: {ex.Message}");
            return null;
        }
    }
}