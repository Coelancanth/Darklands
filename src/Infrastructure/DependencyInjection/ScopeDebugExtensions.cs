using Darklands.Core.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Darklands.Core.Infrastructure.DependencyInjection;

/// <summary>
/// TD_052 Phase 3: Debug extensions for monitoring scope hierarchy and service lifecycle.
/// Provides diagnostic information for troubleshooting DI lifecycle issues.
/// </summary>
public static class ScopeDebugExtensions
{
    private static readonly ConcurrentDictionary<object, ScopeDebugInfo> _scopeRegistry = new();
    private static int _nextScopeId = 1;

    /// <summary>
    /// Registers a scope for debug tracking.
    /// Called automatically by GodotScopeManager when debug monitoring is enabled.
    /// </summary>
    public static void RegisterScope(object node, IServiceScope scope, string? parentScopeId = null)
    {
        var scopeId = $"scope-{_nextScopeId++}";
        var debugInfo = new ScopeDebugInfo
        {
            ScopeId = scopeId,
            Node = node,
            Scope = scope,
            ParentScopeId = parentScopeId,
            CreatedAt = DateTime.UtcNow,
            ServiceCount = 0 // Will be updated as services are resolved
        };

        _scopeRegistry.TryAdd(node, debugInfo);
    }

    /// <summary>
    /// Unregisters a scope from debug tracking.
    /// Called automatically by GodotScopeManager when scopes are disposed.
    /// </summary>
    public static void UnregisterScope(object node)
    {
        if (_scopeRegistry.TryRemove(node, out var debugInfo))
        {
            debugInfo.DisposedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Increments service resolution count for debugging.
    /// </summary>
    public static void IncrementServiceResolution(object node)
    {
        if (_scopeRegistry.TryGetValue(node, out var debugInfo))
        {
            debugInfo.ServiceCount++;
        }
    }

    /// <summary>
    /// Gets a formatted debug report showing active scopes and their hierarchy.
    /// Useful for F12 debug console or diagnostic logging.
    /// </summary>
    public static string GetScopeHierarchyReport()
    {
        var activeScopes = _scopeRegistry.Values
            .Where(s => s.DisposedAt == null)
            .OrderBy(s => s.CreatedAt)
            .ToList();

        if (!activeScopes.Any())
        {
            return "No active scopes found.";
        }

        var report = new StringBuilder();
        report.AppendLine("=== ACTIVE SCOPE HIERARCHY ===");
        report.AppendLine($"Total Active Scopes: {activeScopes.Count}");
        report.AppendLine();

        // Build hierarchy tree
        var rootScopes = activeScopes.Where(s => s.ParentScopeId == null).ToList();

        foreach (var rootScope in rootScopes)
        {
            AppendScopeTree(report, rootScope, activeScopes, 0);
        }

        // Memory usage summary
        report.AppendLine();
        report.AppendLine("=== MEMORY USAGE SUMMARY ===");
        var totalServices = activeScopes.Sum(s => s.ServiceCount);
        report.AppendLine($"Total Service Resolutions: {totalServices}");
        report.AppendLine($"Average Services per Scope: {(activeScopes.Count > 0 ? totalServices / (double)activeScopes.Count : 0):F1}");

        return report.ToString();
    }

    /// <summary>
    /// Gets performance metrics for scope operations.
    /// </summary>
    public static ScopePerformanceMetrics GetPerformanceMetrics()
    {
        var allScopes = _scopeRegistry.Values.ToList();
        var activeScopes = allScopes.Where(s => s.DisposedAt == null).ToList();
        var disposedScopes = allScopes.Where(s => s.DisposedAt != null).ToList();

        return new ScopePerformanceMetrics(
            ActiveScopeCount: activeScopes.Count,
            TotalScopesCreated: allScopes.Count,
            TotalServiceResolutions: allScopes.Sum(s => s.ServiceCount),
            AverageServicesPerScope: allScopes.Count > 0 ? allScopes.Sum(s => s.ServiceCount) / (double)allScopes.Count : 0,
            MemoryUsageKB: GC.GetTotalMemory(false) / 1024.0
        );
    }

    private static void AppendScopeTree(StringBuilder report, ScopeDebugInfo scope, List<ScopeDebugInfo> allScopes, int depth)
    {
        var indent = new string(' ', depth * 2);
        var nodeType = scope.Node?.GetType().Name ?? "Unknown";
        var lifespan = scope.DisposedAt?.Subtract(scope.CreatedAt).TotalMilliseconds ??
                       DateTime.UtcNow.Subtract(scope.CreatedAt).TotalMilliseconds;

        report.AppendLine($"{indent}├─ {scope.ScopeId} ({nodeType})");
        report.AppendLine($"{indent}│  Services: {scope.ServiceCount}, Lifespan: {lifespan:F0}ms");

        // Find and display child scopes
        var children = allScopes.Where(s => s.ParentScopeId == scope.ScopeId).ToList();
        foreach (var child in children)
        {
            AppendScopeTree(report, child, allScopes, depth + 1);
        }
    }

    /// <summary>
    /// Clears all debug tracking data. Use for memory cleanup in long-running applications.
    /// </summary>
    public static void ClearDebugRegistry()
    {
        _scopeRegistry.Clear();
        _nextScopeId = 1;
    }
}

/// <summary>
/// Debug information tracked per scope.
/// </summary>
public class ScopeDebugInfo
{
    public required string ScopeId { get; init; }
    public required object Node { get; init; }
    public required IServiceScope Scope { get; init; }
    public string? ParentScopeId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DisposedAt { get; set; }
    public int ServiceCount { get; set; }
}

/// <summary>
/// Performance metrics for scope operations.
/// </summary>
public record ScopePerformanceMetrics(
    int ActiveScopeCount,
    int TotalScopesCreated,
    int TotalServiceResolutions,
    double AverageServicesPerScope,
    double MemoryUsageKB
);
