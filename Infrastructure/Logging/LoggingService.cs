using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Darklands.Infrastructure.Logging;

/// <summary>
/// Provides runtime control over category-based log filtering.
/// Categories are auto-discovered from Commands/Queries namespaces (e.g., "Combat", "Movement", "AI").
/// This service is Presentation-layer only - Core has no dependency on logging configuration.
/// </summary>
public class LoggingService
{
    private readonly HashSet<string> _enabledCategories;

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
    /// </summary>
    public void EnableCategory(string category)
    {
        _enabledCategories.Add(category);
    }

    /// <summary>
    /// Disable a category. Logs from this category will be filtered out before sinks.
    /// </summary>
    public void DisableCategory(string category)
    {
        _enabledCategories.Remove(category);
    }

    /// <summary>
    /// Toggle a category on/off.
    /// </summary>
    public void ToggleCategory(string category)
    {
        if (_enabledCategories.Contains(category))
            _enabledCategories.Remove(category);
        else
            _enabledCategories.Add(category);
    }

    /// <summary>
    /// Get the currently enabled categories (read-only).
    /// </summary>
    public IReadOnlySet<string> GetEnabledCategories()
    {
        return _enabledCategories;
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
    /// Pattern: "Darklands.Core.Application.Commands.{Category}.Handler" → "Category"
    /// Pattern: "Darklands.Core.Application.Queries.{Category}.Handler" → "Category"
    /// </summary>
    public static string? ExtractCategory(string sourceContext)
    {
        if (string.IsNullOrEmpty(sourceContext))
            return null;

        var parts = sourceContext.Split('.');

        // Look for Commands.{Category}
        var commandsIndex = Array.IndexOf(parts, "Commands");
        if (commandsIndex >= 0 && commandsIndex + 1 < parts.Length)
            return parts[commandsIndex + 1];

        // Look for Queries.{Category}
        var queriesIndex = Array.IndexOf(parts, "Queries");
        if (queriesIndex >= 0 && queriesIndex + 1 < parts.Length)
            return parts[queriesIndex + 1];

        // Fallback: Infrastructure logs (GameStrapper, ServiceLocator, etc.)
        return "Infrastructure";
    }
}