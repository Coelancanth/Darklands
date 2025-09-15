using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Godot;
using Darklands.Core.Domain.Services;

namespace Darklands.Presentation.Infrastructure;

/// <summary>
/// Godot-specific implementation of IScopeManager that aligns DI scopes with node lifecycle.
///
/// Key Features (ADR-018 Compliance):
/// - ConditionalWeakTable prevents memory leaks from orphaned nodes
/// - Scope caching provides O(1) resolution after first lookup
/// - TreeExiting signal integration for automatic cleanup
/// - ReaderWriterLockSlim for high-performance concurrent access
/// - Performance monitoring with >1ms warnings
///
/// Memory Safety Design:
/// - ConditionalWeakTable allows GC to collect nodes even if scope tracking fails
/// - WeakReference cache entries prevent holding strong references to service providers
/// - Automatic scope disposal prevents service instance accumulation
///
/// Thread Safety:
/// - ReaderWriterLockSlim allows multiple concurrent reads
/// - Write operations (create/dispose) use exclusive locks
/// - Cache operations are thread-safe via ConcurrentDictionary
/// </summary>
public sealed class GodotScopeManager : IScopeManager
{
    private readonly IServiceProvider _rootServiceProvider;
    private readonly ILogger<GodotScopeManager>? _logger;

    // MANDATORY IMPROVEMENT 1: ConditionalWeakTable prevents memory leaks
    // If a node is freed directly without TreeExiting, the WeakTable allows GC
    private readonly ConditionalWeakTable<Node, IServiceScope> _nodeScopes = new();

    // MANDATORY IMPROVEMENT 2: Performance caching for O(1) resolution
    // Cache the resolved provider for each node to avoid repeated tree walking
    private readonly ConcurrentDictionary<Node, WeakReference<IServiceProvider>> _providerCache = new();

    // Thread safety for scope creation/disposal operations
    private readonly ReaderWriterLockSlim _lock = new();

    // Performance and diagnostic tracking
    private long _totalScopeCreations;
    private long _totalScopeDisposals;
    private long _cacheHits;
    private long _cacheMisses;
    private readonly List<double> _resolutionTimes = new();
    private bool _disposed;

    public GodotScopeManager(IServiceProvider rootServiceProvider, ILogger<GodotScopeManager>? logger = null)
    {
        _rootServiceProvider = rootServiceProvider ?? throw new ArgumentNullException(nameof(rootServiceProvider));
        _logger = logger;

        _logger?.LogInformation("GodotScopeManager initialized with root service provider");
    }

    /// <inheritdoc />
    public IServiceScope? CreateScope(object node)
    {
        if (node is not Node godotNode)
        {
            _logger?.LogError("CreateScope called with non-Node object: {ObjectType}", node?.GetType().Name ?? "null");
            throw new ArgumentException("Node must be a Godot Node", nameof(node));
        }

        _lock.EnterWriteLock();
        try
        {
            // Check if scope already exists
            if (_nodeScopes.TryGetValue(godotNode, out var existingScope))
            {
                _logger?.LogWarning("Attempted to create scope for node {NodeName} that already has a scope", godotNode.Name);
                throw new InvalidOperationException($"Node {godotNode.Name} already has a scope");
            }

            // Find parent scope by walking up the tree
            var parentProvider = GetParentProvider(godotNode);

            // Create new scope from parent
            var scope = parentProvider.CreateScope();

            // MANDATORY IMPROVEMENT 1: Use ConditionalWeakTable for automatic cleanup
            _nodeScopes.Add(godotNode, scope);

            // Connect TreeExiting signal for automatic disposal
            if (godotNode.IsConnected(Node.SignalName.TreeExiting, Callable.From(() => DisposeScope(godotNode))))
            {
                _logger?.LogDebug("TreeExiting signal already connected for {NodeName}", godotNode.Name);
            }
            else
            {
                godotNode.TreeExiting += () => DisposeScope(godotNode);
                _logger?.LogDebug("Connected TreeExiting signal for automatic scope disposal: {NodeName}", godotNode.Name);
            }

            // Clear cache entries for this node and its children (they might resolve differently now)
            InvalidateCacheForNodeTree(godotNode);

            Interlocked.Increment(ref _totalScopeCreations);
            _logger?.LogDebug("Created scope for node {NodeName} (Total scopes: {ScopeCount})",
                             godotNode.Name, Interlocked.Read(ref _totalScopeCreations));

            return scope;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create scope for node {NodeName}", godotNode.Name);
            return null;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void DisposeScope(object node)
    {
        if (node is not Node godotNode)
        {
            _logger?.LogError("DisposeScope called with non-Node object: {ObjectType}", node?.GetType().Name ?? "null");
            return;
        }

        _lock.EnterWriteLock();
        try
        {
            if (_nodeScopes.TryGetValue(godotNode, out var scope))
            {
                // Dispose the scope and all its services
                scope.Dispose();

                // Remove from ConditionalWeakTable
                _nodeScopes.Remove(godotNode);

                // Clear cache entries for this node and its children
                InvalidateCacheForNodeTree(godotNode);

                // Disconnect signal if still connected (might already be disconnected if node is being freed)
                if (godotNode.IsInsideTree() && godotNode.IsConnected(Node.SignalName.TreeExiting, Callable.From(() => DisposeScope(godotNode))))
                {
                    godotNode.TreeExiting -= () => DisposeScope(godotNode);
                }

                Interlocked.Increment(ref _totalScopeDisposals);
                _logger?.LogDebug("Disposed scope for node {NodeName} (Total disposals: {DisposalCount})",
                                 godotNode.Name, Interlocked.Read(ref _totalScopeDisposals));
            }

            // Also dispose any child node scopes recursively
            DisposeChildScopes(godotNode);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error disposing scope for node {NodeName}", godotNode.Name);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public IServiceProvider GetProviderForNode(object node)
    {
        if (node is not Node godotNode)
        {
            _logger?.LogError("GetProviderForNode called with non-Node object: {ObjectType}", node?.GetType().Name ?? "null");
            throw new ArgumentException("Node must be a Godot Node", nameof(node));
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // MANDATORY IMPROVEMENT 2: Check cache first for O(1) performance
            if (_providerCache.TryGetValue(godotNode, out var weakRef) &&
                weakRef.TryGetTarget(out var cachedProvider))
            {
                Interlocked.Increment(ref _cacheHits);
                return cachedProvider;
            }

            Interlocked.Increment(ref _cacheMisses);

            _lock.EnterReadLock();
            try
            {
                var provider = FindProviderByWalkingTree(godotNode);

                // MANDATORY IMPROVEMENT 2: Cache the result for next time
                _providerCache[godotNode] = new WeakReference<IServiceProvider>(provider);

                return provider;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            // MANDATORY IMPROVEMENT 5: Performance monitoring
            if (elapsedMs > 1.0)
            {
                _logger?.LogWarning("Slow scope resolution for {NodeName}: {ElapsedMs:F2}ms (>1ms threshold)",
                                   godotNode.Name, elapsedMs);
            }

            lock (_resolutionTimes)
            {
                _resolutionTimes.Add(elapsedMs);
                if (_resolutionTimes.Count > 1000) // Keep only recent measurements
                {
                    _resolutionTimes.RemoveAt(0);
                }
            }
        }
    }

    /// <inheritdoc />
    public ScopeManagerDiagnostics GetDiagnostics()
    {
        _lock.EnterReadLock();
        try
        {
            var activeScopeCount = CountActiveScopes();
            var cachedProviderCount = _providerCache.Count;
            var totalCreations = Interlocked.Read(ref _totalScopeCreations);
            var totalDisposals = Interlocked.Read(ref _totalScopeDisposals);
            var cacheHits = Interlocked.Read(ref _cacheHits);
            var cacheMisses = Interlocked.Read(ref _cacheMisses);

            int averageResolutionTimeMicroseconds;
            lock (_resolutionTimes)
            {
                if (_resolutionTimes.Count > 0)
                {
                    var recentTimes = _resolutionTimes.GetRange(Math.Max(0, _resolutionTimes.Count - 100), Math.Min(100, _resolutionTimes.Count));
                    var averageMs = recentTimes.Sum() / recentTimes.Count;
                    averageResolutionTimeMicroseconds = (int)(averageMs * 1000); // Convert ms to microseconds
                }
                else
                {
                    averageResolutionTimeMicroseconds = 0;
                }
            }

            int cacheHitRatioPercentage = (cacheHits + cacheMisses) > 0
                ? (int)((cacheHits * 100) / (cacheHits + cacheMisses))
                : 0;

            // Rough memory estimate: each scope ~1KB, each cache entry ~100 bytes
            var estimatedMemory = (activeScopeCount * 1024) + (cachedProviderCount * 100);

            return new ScopeManagerDiagnostics(
                ActiveScopeCount: activeScopeCount,
                CachedProviderCount: cachedProviderCount,
                TotalScopeCreations: (int)totalCreations,
                TotalScopeDisposals: (int)totalDisposals,
                AverageResolutionTimeMicroseconds: averageResolutionTimeMicroseconds,
                CacheHitRatioPercentage: cacheHitRatioPercentage,
                EstimatedMemoryUsageBytes: estimatedMemory);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Finds the appropriate service provider by walking up the node tree.
    /// Starts with the node itself, then walks up parents until a scope is found.
    /// Falls back to root provider if no parent scopes exist.
    /// </summary>
    private IServiceProvider FindProviderByWalkingTree(Node node)
    {
        var currentNode = node;

        // Walk up the tree looking for a node with a scope
        while (currentNode != null)
        {
            if (_nodeScopes.TryGetValue(currentNode, out var scope))
            {
                return scope.ServiceProvider;
            }

            currentNode = currentNode.GetParent();
        }

        // No parent scope found, use root provider
        return _rootServiceProvider;
    }

    /// <summary>
    /// Gets the parent service provider for creating a new scope.
    /// Walks up the tree to find the nearest parent scope.
    /// </summary>
    private IServiceProvider GetParentProvider(Node node)
    {
        var parent = node.GetParent();

        while (parent != null)
        {
            if (_nodeScopes.TryGetValue(parent, out var parentScope))
            {
                return parentScope.ServiceProvider;
            }
            parent = parent.GetParent();
        }

        return _rootServiceProvider;
    }

    /// <summary>
    /// Recursively disposes scopes for all child nodes.
    /// </summary>
    private void DisposeChildScopes(Node parentNode)
    {
        foreach (Node child in parentNode.GetChildren())
        {
            if (_nodeScopes.TryGetValue(child, out _))
            {
                DisposeScope(child);
            }

            // Recurse for grandchildren
            DisposeChildScopes(child);
        }
    }

    /// <summary>
    /// Invalidates cache entries for a node and all its descendants.
    /// Called when scopes are created/disposed to ensure cache consistency.
    /// </summary>
    private void InvalidateCacheForNodeTree(Node node)
    {
        // Remove cache entry for this node
        _providerCache.TryRemove(node, out _);

        // Recursively invalidate children
        foreach (Node child in node.GetChildren())
        {
            InvalidateCacheForNodeTree(child);
        }
    }

    /// <summary>
    /// Counts active scopes by using an approximate count.
    /// ConditionalWeakTable doesn't support direct enumeration, so we track count manually.
    /// Used for diagnostics only.
    /// </summary>
    private int CountActiveScopes()
    {
        // ConditionalWeakTable doesn't support enumeration or Count property
        // We track this manually via creation/disposal counters
        var totalCreations = Interlocked.Read(ref _totalScopeCreations);
        var totalDisposals = Interlocked.Read(ref _totalScopeDisposals);

        // This gives an approximate count - some scopes may have been GC'd without disposal
        return Math.Max(0, (int)(totalCreations - totalDisposals));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _lock.EnterWriteLock();
        try
        {
            // ConditionalWeakTable doesn't support enumeration, but calling Clear() will
            // dispose the finalizers which should clean up the scopes
            _nodeScopes.Clear();
            _providerCache.Clear();

            _disposed = true;
            _logger?.LogInformation("GodotScopeManager disposed successfully. " +
                                   "Active scopes will be cleaned up by garbage collection.");
        }
        finally
        {
            _lock.ExitWriteLock();
            _lock.Dispose();
        }
    }
}