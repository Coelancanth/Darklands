using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Darklands.Application.Services;
using Darklands.Application.Common;

// Alias to resolve LogLevel namespace collision
using DomainLogLevel = Darklands.Application.Common.LogLevel;

namespace Darklands.Application.Infrastructure.DependencyInjection;

/// <summary>
/// Stub implementation of IScopeManager that provides fallback behavior when the actual implementation
/// (GodotScopeManager) is not available or not properly initialized.
///
/// This allows the Core project to remain platform-agnostic while providing a working fallback
/// that doesn't crash when Godot-specific implementations are not available.
///
/// Behavior:
/// - CreateScope: Always returns null and logs a warning
/// - DisposeScope: No-op with warning log
/// - GetProviderForNode: Returns the root service provider with warning
/// - GetDiagnostics: Returns empty diagnostics with appropriate warnings
///
/// This stub is replaced by the actual GodotScopeManager when the presentation layer initializes.
/// </summary>
internal sealed class StubScopeManager : IScopeManager
{
    private readonly ICategoryLogger? _logger;
    private bool _disposed;

    public StubScopeManager(ICategoryLogger? logger = null)
    {
        _logger = logger;
    }

    public IServiceScope? CreateScope(object node)
    {
        _logger?.Log(DomainLogLevel.Warning, LogCategory.System, "CreateScope called on StubScopeManager. " +
                           "GodotScopeManager may not be properly initialized. " +
                           "Node service extensions will fall back to GameStrapper pattern.");
        return null;
    }

    public void DisposeScope(object node)
    {
        _logger?.Log(DomainLogLevel.Warning, LogCategory.System, "DisposeScope called on StubScopeManager. " +
                           "This is a no-op. Ensure GodotScopeManager is properly initialized.");
    }

    public IServiceProvider GetProviderForNode(object node)
    {
        _logger?.Log(DomainLogLevel.Warning, LogCategory.System, "GetProviderForNode called on StubScopeManager. " +
                           "Returning fallback behavior. " +
                           "Node service extensions will use GameStrapper fallback.");

        // We can't provide a meaningful service provider here without access to the root container
        // This will cause the extension methods to fall back to GameStrapper.GetServices()
        throw new InvalidOperationException(
            "StubScopeManager cannot provide service providers. " +
            "Extension methods will fall back to GameStrapper pattern.");
    }

    public ScopeManagerDiagnostics GetDiagnostics()
    {
        _logger?.Log(DomainLogLevel.Information, LogCategory.System, "GetDiagnostics called on StubScopeManager - returning empty diagnostics");

        return new ScopeManagerDiagnostics(
            ActiveScopeCount: 0,
            CachedProviderCount: 0,
            TotalScopeCreations: 0,
            TotalScopeDisposals: 0,
            AverageResolutionTimeMicroseconds: 0,
            CacheHitRatioPercentage: 0,
            EstimatedMemoryUsageBytes: 0);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger?.Log(DomainLogLevel.Information, LogCategory.System, "StubScopeManager disposed");
            _disposed = true;
        }
    }
}
