namespace Darklands.Core.Infrastructure.Templates;

/// <summary>
/// Marker interface for Godot Resource-based templates that require an ID property.
/// Enforces compile-time safety - prevents reflection-based ID extraction fragility.
/// </summary>
/// <remarks>
/// <para><b>Why this exists</b>: Without this interface, we'd need reflection to extract
/// template IDs ("Id" property name hardcoded, fragile). Interface makes it explicit
/// and compile-time checked.</para>
///
/// <para><b>Usage</b>: All templates MUST implement this interface:</para>
/// <code>
/// [GlobalClass]
/// public partial class ActorTemplate : Resource, IIdentifiableResource
/// {
///     [Export] public string Id { get; set; } = "";  // ← Enforced by interface
/// }
/// </code>
///
/// <para><b>Benefit</b>: If someone forgets Id property → compile error (not runtime crash)</para>
/// </remarks>
public interface IIdentifiableResource
{
    /// <summary>
    /// Unique identifier for this template (e.g., "goblin", "player", "iron_sword").
    /// Used as dictionary key in template service for O(1) lookup.
    /// </summary>
    string Id { get; }
}
