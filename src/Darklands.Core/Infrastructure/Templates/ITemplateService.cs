using CSharpFunctionalExtensions;
using Godot;

namespace Darklands.Core.Infrastructure.Templates;

/// <summary>
/// Service for loading and retrieving Godot Resource-based templates.
/// Generic design supports multiple template types (Actor, Item, Skill, etc.).
/// </summary>
/// <typeparam name="T">Template type (must be Resource and have Id property)</typeparam>
/// <remarks>
/// <para><b>Usage in Application Layer</b>:</para>
/// <code>
/// public class SpawnActorCommandHandler
/// {
///     private readonly ITemplateService&lt;ActorTemplate&gt; _templates;
///
///     public async Task&lt;Result&lt;Actor&gt;&gt; Handle(SpawnActorCommand cmd)
///     {
///         var templateResult = _templates.GetTemplate(cmd.TemplateId);
///         if (templateResult.IsFailure)
///             return Result.Failure&lt;Actor&gt;(templateResult.Error);
///
///         var template = templateResult.Value;
///
///         // Create Domain entity FROM template data (no Godot dependency!)
///         return new Actor(
///             ActorId.NewId(),
///             nameKey: template.NameKey,
///             health: Health.Create(template.MaxHealth, template.MaxHealth).Value
///         );
///     }
/// }
/// </code>
///
/// <para><b>Testability</b>:</para>
/// <para>Application layer can mock ITemplateService for unit tests (no Godot runtime needed).</para>
/// </remarks>
public interface ITemplateService<T> where T : Resource, IIdentifiableResource
{
    /// <summary>
    /// Load all templates from resource directory.
    /// Called ONCE at game startup (GameStrapper initialization).
    /// </summary>
    /// <remarks>
    /// <para><b>Fail-Fast Strategy</b>:</para>
    /// <para>If ANY template is invalid (missing Id, corrupt data, missing translation key),
    /// returns Failure and blocks game startup. Better to fail early (development) than
    /// crash late (production).</para>
    ///
    /// <para><b>Performance</b>:</para>
    /// <para>Templates loaded into memory once, cached in dictionary.
    /// Future calls to GetTemplate() are O(1) dictionary lookups.</para>
    /// </remarks>
    /// <returns>Success if all templates loaded and validated, Failure with error details otherwise</returns>
    Result LoadTemplates();

    /// <summary>
    /// Get template by unique ID.
    /// O(1) dictionary lookup after templates loaded.
    /// </summary>
    /// <param name="id">Template identifier (e.g., "goblin", "player")</param>
    /// <returns>Success with template if found, Failure if template doesn't exist</returns>
    Result<T> GetTemplate(string id);

    /// <summary>
    /// Get all loaded templates (read-only).
    /// Useful for editor tools, debugging, validation scripts.
    /// </summary>
    /// <returns>Dictionary of template ID → template instance</returns>
    IReadOnlyDictionary<string, T> GetAllTemplates();
}
