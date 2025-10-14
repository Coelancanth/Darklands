using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Godot;
using Microsoft.Extensions.Logging;

namespace Darklands.Presentation.Infrastructure.Templates;

/// <summary>
/// Godot-based implementation of template loading service.
/// Loads .tres Resource files from specified directory, validates, and caches in memory.
/// </summary>
/// <typeparam name="T">Template type (must inherit Resource and IIdentifiableResource)</typeparam>
/// <remarks>
/// <para><b>Loading Strategy</b>:</para>
/// <list type="number">
/// <item><b>Startup</b>: LoadTemplates() called once in GameStrapper</item>
/// <item><b>Discovery</b>: Scan resource directory for .tres files</item>
/// <item><b>Load</b>: GD.Load&lt;T&gt;() deserializes each file</item>
/// <item><b>Validate</b>: Check Id exists, is unique, template data valid</item>
/// <item><b>Cache</b>: Store in dictionary for O(1) lookup</item>
/// </list>
///
/// <para><b>Fail-Fast Philosophy</b>:</para>
/// <para>Invalid template = startup failure (visible error), not runtime crash (silent corruption).
/// This forces developers to fix data issues immediately.</para>
/// </remarks>
public sealed class GodotTemplateService<T> : Darklands.Core.Infrastructure.Templates.ITemplateService<T>
    where T : Resource, Darklands.Core.Infrastructure.Templates.IIdentifiableResource
{
    private readonly Dictionary<string, T> _templates = new();
    private readonly ILogger<GodotTemplateService<T>> _logger;
    private readonly string _resourcePath;

    /// <summary>
    /// Creates a new template service for the specified resource directory.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages</param>
    /// <param name="resourcePath">Godot resource path (e.g., "res://data/entities/")</param>
    public GodotTemplateService(
        ILogger<GodotTemplateService<T>> logger,
        string resourcePath)
    {
        _logger = logger;
        _resourcePath = resourcePath;
    }

    /// <inheritdoc/>
    public Result LoadTemplates()
    {
        try
        {
            // Open resource directory
            var dir = DirAccess.Open(_resourcePath);
            if (dir == null)
            {
                var error = $"Template directory not found: {_resourcePath}";
                _logger.LogError(error);
                return Result.Failure(error);
            }

            // Scan for .tres files
            var files = dir.GetFiles();
            _logger.LogInformation("Scanning {Path} for templates (found {Count} files)",
                _resourcePath, files.Length);

            foreach (var file in files)
            {
                // Only load .tres Resource files
                if (!file.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Skipping non-template file: {File}", file);
                    continue;
                }

                var fullPath = $"{_resourcePath}/{file}";

                // Load template from disk
                var template = GD.Load<T>(fullPath);

                // Fail-fast: NULL template = corrupt file
                if (template == null)
                {
                    var error = $"Failed to load template (GD.Load returned null): {fullPath}";
                    _logger.LogError(error);
                    return Result.Failure(error);
                }

                // Fail-fast: Missing or empty ID
                var id = template.Id;  // Compile-time safe (IIdentifiableResource constraint)
                if (string.IsNullOrWhiteSpace(id))
                {
                    var error = $"Template missing Id property: {fullPath}";
                    _logger.LogError(error);
                    return Result.Failure(error);
                }

                // Fail-fast: Duplicate ID
                if (_templates.ContainsKey(id))
                {
                    var error = $"Duplicate template ID '{id}': {fullPath} (already loaded from another file)";
                    _logger.LogError(error);
                    return Result.Failure(error);
                }

                // Cache template
                _templates[id] = template;
                _logger.LogInformation("Loaded template: {Id} from {File}", id, file);
            }

            // Validate ALL templates (business rules)
            var validationResult = ValidateTemplates();
            if (validationResult.IsFailure)
                return validationResult;

            _logger.LogInformation(
                "Successfully loaded {Count} {Type} templates from {Path}",
                _templates.Count,
                typeof(T).Name,
                _resourcePath);

            return Result.Success();
        }
        catch (Exception ex)
        {
            var error = $"Template loading failed with exception: {ex.Message}";
            _logger.LogError(ex, error);
            return Result.Failure(error);
        }
    }

    /// <inheritdoc/>
    public Result<T> GetTemplate(string id)
    {
        if (_templates.TryGetValue(id, out var template))
        {
            return Result.Success(template);
        }

        var error = $"Template not found: '{id}' (available: {string.Join(", ", _templates.Keys)})";
        _logger.LogWarning(error);
        return Result.Failure<T>(error);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, T> GetAllTemplates() => _templates;

    /// <summary>
    /// Validate all loaded templates against business rules.
    /// Called after ALL templates loaded (allows cross-template validation if needed).
    /// </summary>
    /// <remarks>
    /// <para><b>Type-Specific Validation</b>:</para>
    /// <para>Uses pattern matching to validate different template types.
    /// ActorTemplate has different rules than ItemTemplate.</para>
    /// </remarks>
    private Result ValidateTemplates()
    {
        var errors = new List<string>();

        foreach (var (id, template) in _templates)
        {
            // Type-specific validation (C# pattern matching)
            if (template is ActorTemplate actor)
            {
                // Validate actor-specific rules
                if (string.IsNullOrWhiteSpace(actor.NameKey))
                    errors.Add($"Actor template '{id}' missing NameKey");

                if (actor.MaxHealth <= 0)
                    errors.Add($"Actor template '{id}' has invalid MaxHealth: {actor.MaxHealth} (must be > 0)");

                if (actor.Sprite == null)
                    errors.Add($"Actor template '{id}' missing Sprite texture");

                // Future: Add translation key validation
                // if (!TranslationKeyExists(actor.NameKey))
                //     errors.Add($"Actor template '{id}' has missing translation key: {actor.NameKey}");
            }

            // Future: Add ItemTemplate, SkillTemplate validation here
        }

        if (errors.Any())
        {
            var errorMessage = $"Template validation failed:\n{string.Join("\n", errors)}";
            _logger.LogError(errorMessage);
            return Result.Failure(errorMessage);
        }

        _logger.LogDebug("All templates validated successfully");
        return Result.Success();
    }
}
