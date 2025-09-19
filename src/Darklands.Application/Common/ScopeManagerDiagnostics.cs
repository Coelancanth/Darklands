namespace Darklands.Application.Common;

/// <summary>
/// Diagnostic information for scope manager performance and state.
/// </summary>
/// <param name="ActiveScopeCount">Number of currently active scopes</param>
/// <param name="CachedProviderCount">Number of cached service providers</param>
/// <param name="TotalScopeCreations">Total number of scopes created since startup</param>
/// <param name="TotalScopeDisposals">Total number of scopes disposed since startup</param>
/// <param name="AverageResolutionTimeMicroseconds">Average time to resolve services in microseconds</param>
/// <param name="CacheHitRatioPercentage">Cache hit ratio as a percentage (0-100)</param>
/// <param name="EstimatedMemoryUsageBytes">Estimated memory usage in bytes</param>
public sealed record ScopeManagerDiagnostics(
    int ActiveScopeCount,
    int CachedProviderCount,
    long TotalScopeCreations,
    long TotalScopeDisposals,
    double AverageResolutionTimeMicroseconds,
    double CacheHitRatioPercentage,
    long EstimatedMemoryUsageBytes
);
