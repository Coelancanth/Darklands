using System;
using Microsoft.Extensions.DependencyInjection;

namespace Darklands.Core.Domain.Services;

/// <summary>
/// Platform-agnostic interface for managing dependency injection scopes aligned with UI node lifecycles.
///
/// Core Responsibilities:
/// - Create scoped service providers for UI nodes and their hierarchies
/// - Automatically dispose scopes when nodes are destroyed
/// - Provide efficient scope resolution by walking hierarchies
/// - Prevent memory leaks through proper scope disposal
///
/// Implementation Requirements (ADR-018):
/// - Thread-safe operations for concurrent node creation/disposal
/// - Performance optimization for frequent service resolution
/// - Automatic cleanup via platform-specific lifecycle events
/// - WeakReference usage to prevent orphaned scope references
///
/// Scope Hierarchy Rules:
/// - Each node can have its own scope created explicitly
/// - Child nodes inherit parent scopes if no local scope exists
/// - Root-level nodes use the global application scope
/// - Scene transitions create new scope boundaries
///
/// Platform Abstraction:
/// - Uses generic object type for nodes to avoid platform coupling
/// - Implementations handle platform-specific node hierarchies
/// - Godot implementation uses Node and TreeExiting signals
/// - Other platforms can implement with different UI frameworks
/// </summary>
public interface IScopeManager : IDisposable
{
    /// <summary>
    /// Creates a new dependency injection scope for the specified UI node.
    ///
    /// The scope will:
    /// - Use the nearest parent scope as its parent (walking up the hierarchy)
    /// - Be automatically disposed when the node is destroyed
    /// - Provide scoped service instances for that node and its children
    ///
    /// Performance Note:
    /// - Results are cached for O(1) subsequent lookups
    /// - Thread-safe using ReaderWriterLockSlim for high read concurrency
    ///
    /// Memory Safety:
    /// - Uses ConditionalWeakTable to prevent memory leaks from orphaned nodes
    /// - Platform-specific signals automatically dispose scopes
    /// </summary>
    /// <param name="node">The UI node to create a scope for (platform-specific type)</param>
    /// <returns>The created service scope, or null if creation failed</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when node already has a scope</exception>
    IServiceScope? CreateScope(object node);

    /// <summary>
    /// Disposes the scope associated with the specified node and all its child scopes.
    ///
    /// Automatically called when nodes are destroyed via platform-specific events.
    /// Can be called manually for explicit scope management.
    ///
    /// Disposal Process:
    /// - Disposes the node's scope and all services within it
    /// - Recursively disposes all child node scopes
    /// - Removes scope from cache and tracking collections
    /// - Disconnects platform-specific lifecycle handlers
    /// </summary>
    /// <param name="node">The UI node whose scope should be disposed</param>
    void DisposeScope(object node);

    /// <summary>
    /// Gets the service provider for the specified node by walking up the hierarchy.
    ///
    /// Resolution Strategy:
    /// 1. Check if the node has its own scope (O(1) cache lookup)
    /// 2. Walk up parent hierarchy until a scope is found
    /// 3. Return global application scope if no parent scopes exist
    /// 4. Cache the resolved provider for subsequent O(1) lookups
    ///
    /// Performance Optimization:
    /// - First call: O(log n) tree walking where n is hierarchy depth
    /// - Subsequent calls: O(1) cached lookup
    /// - Cache invalidation when scopes are created/disposed
    /// - Monitoring warns if resolution takes >1ms
    /// </summary>
    /// <param name="node">The UI node to get a service provider for</param>
    /// <returns>The service provider for the node, never null</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null</exception>
    IServiceProvider GetProviderForNode(object node);

    /// <summary>
    /// Gets diagnostic information about active scopes for debugging and monitoring.
    ///
    /// Provides:
    /// - Count of active scopes per hierarchy level
    /// - Memory usage estimates for scope tracking
    /// - Performance metrics for scope resolution
    /// - Cache hit/miss ratios
    /// </summary>
    /// <returns>Diagnostic information about scope management state</returns>
    ScopeManagerDiagnostics GetDiagnostics();
}

/// <summary>
/// Diagnostic information about scope manager state for debugging and monitoring.
/// ADR-004 Compliant: Uses integer types instead of floating-point for deterministic simulation.
/// </summary>
public record ScopeManagerDiagnostics(
    int ActiveScopeCount,
    int CachedProviderCount,
    int TotalScopeCreations,
    int TotalScopeDisposals,
    int AverageResolutionTimeMicroseconds,
    int CacheHitRatioPercentage,
    long EstimatedMemoryUsageBytes);
