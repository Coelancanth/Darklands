using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Darklands.Presentation.Infrastructure.Logging;

/// <summary>
/// Provides runtime control over category-based log filtering.
/// Categories are auto-discovered from Commands/Queries namespaces (e.g., "Combat", "Movement", "AI").
/// This service is Presentation-layer only - Core has no dependency on logging configuration.
/// Thread-safe: All HashSet operations protected by lock to prevent concurrent modification issues.
/// </summary>
public class LoggingService
{
    private readonly HashSet<string> _enabledCategories;
    private readonly object _lock = new();

    /// <summary>
    /// Initialize with a shared set of enabled categories.
    /// This set is referenced by Serilog's filter - changes affect filtering immediately.
    /// </summary>
    public LoggingService(HashSet<string> enabledCategories)
    {
        _enabledCategories = enabledCategories;
    }

    /// <summary>
    /// Enable a category. Logs from this category will appear in all sinks.
    /// Thread-safe: Lock protects HashSet modification.
    /// </summary>
    public void EnableCategory(string category)
    {
        lock (_lock)
        {
            _enabledCategories.Add(category);
        }
    }

    /// <summary>
    /// Disable a category. Logs from this category will be filtered out before sinks.
    /// Thread-safe: Lock protects HashSet modification.
    /// </summary>
    public void DisableCategory(string category)
    {
        lock (_lock)
        {
            _enabledCategories.Remove(category);
        }
    }

    /// <summary>
    /// Toggle a category on/off.
    /// Thread-safe: Lock protects HashSet read and modification.
    /// </summary>
    public void ToggleCategory(string category)
    {
        lock (_lock)
        {
            if (_enabledCategories.Contains(category))
                _enabledCategories.Remove(category);
            else
                _enabledCategories.Add(category);
        }
    }

    /// <summary>
    /// Get the currently enabled categories (read-only snapshot).
    /// Thread-safe: Returns copy to prevent external modification.
    /// </summary>
    public IReadOnlySet<string> GetEnabledCategories()
    {
        lock (_lock)
        {
            return new HashSet<string>(_enabledCategories);
        }
    }

    /// <summary>
    /// Auto-discover available categories from assembly namespaces.
    /// Scans for Commands.{Category} and Queries.{Category} patterns.
    /// Returns sorted, distinct list of categories.
    /// </summary>
    public IReadOnlyList<string> GetAvailableCategories()
    {
        // Scan Core assembly for CQRS namespace patterns
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Darklands.Core");

        if (coreAssembly == null)
            return Array.Empty<string>();

        return coreAssembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(ns => !string.IsNullOrEmpty(ns))
            .Select(ns => ExtractCategory(ns!))
            .Where(category => !string.IsNullOrEmpty(category))
            .Distinct()
            .OrderBy(c => c)
            .ToList()!;
    }

    /// <summary>
    /// Extract category from namespace or SourceContext.
    /// Pattern: "Darklands.Core.Features.{Category}.Application.Commands" → "Category"
    /// Pattern: "Darklands.Core.Features.{Category}.Application.Queries" → "Category"
    /// Fallback: "Infrastructure" for non-feature logs
    /// </summary>
    public static string? ExtractCategory(string sourceContext)
    {
        if (string.IsNullOrEmpty(sourceContext))
            return null;

        var parts = sourceContext.Split('.');

        // Look for Features.{Category} pattern (ADR-004 feature-based architecture)
        // Example: Darklands.Core.Features.Grid.Application.Commands.MoveActorCommandHandler
        var featuresIndex = Array.IndexOf(parts, "Features");
        if (featuresIndex >= 0 && featuresIndex + 1 < parts.Length)
            return parts[featuresIndex + 1];

        // Fallback: Infrastructure logs (GameStrapper, ServiceLocator, etc.)
        return "Infrastructure";
    }
}