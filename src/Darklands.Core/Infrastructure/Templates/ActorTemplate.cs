using Godot;

namespace Darklands.Core.Infrastructure.Templates;

/// <summary>
/// Actor entity template - designer-editable configuration for spawning actors.
/// Created in Godot Editor (Inspector), saved as .tres file, loaded at startup.
/// </summary>
/// <remarks>
/// <para><b>Designer Workflow</b>:</para>
/// <list type="number">
/// <item>Godot Editor → Right-click data/entities/ → Create Resource → ActorTemplate</item>
/// <item>Inspector: Set properties (Id, NameKey, MaxHealth, Sprite, etc.)</item>
/// <item>Save as {id}.tres (e.g., goblin.tres)</item>
/// <item>Hot-reload works: Edit → Ctrl+S → instant update (no recompile!)</item>
/// </list>
///
/// <para><b>Integration with i18n (ADR-005)</b>:</para>
/// <para>NameKey/DescriptionKey are translation keys (ACTOR_GOBLIN), NOT English text.
/// Flow: template.NameKey → actor.NameKey → tr("ACTOR_GOBLIN") → "Goblin" or "哥布林"</para>
///
/// <para><b>Architecture Note (ADR-001, ADR-006)</b>:</para>
/// <para>Templates live in Infrastructure (can use Godot). Domain entities created FROM
/// template data (no Godot dependency). This is Cookie Cutter pattern:
/// - Template = cookie cutter (reusable configuration)
/// - Entity = cookie (independent instance with copied data)</para>
/// </remarks>
[GlobalClass]
public partial class ActorTemplate : Resource, IIdentifiableResource
{
    // ========== IDENTITY ==========

    /// <summary>
    /// Unique template identifier (e.g., "goblin", "player", "skeleton").
    /// Used as dictionary key in TemplateService for O(1) lookup.
    /// MUST be unique across all actor templates (validated at load time).
    /// </summary>
    [Export]
    public string Id { get; set; } = "";

    // ========== INTERNATIONALIZATION (ADR-005) ==========

    /// <summary>
    /// Translation key for actor name (e.g., "ACTOR_GOBLIN").
    /// NEVER store English text here - use translation keys!
    /// Presentation layer calls tr(NameKey) to get localized name.
    /// </summary>
    [Export]
    public string NameKey { get; set; } = "";

    /// <summary>
    /// Translation key for actor description (e.g., "DESC_ACTOR_GOBLIN").
    /// Used for tooltips, bestiary entries, quest dialogs.
    /// </summary>
    [Export]
    public string DescriptionKey { get; set; } = "";

    // ========== STATS ==========

    /// <summary>
    /// Maximum health points. Must be > 0 (validated at load time).
    /// Domain will create Health value object from this value.
    /// </summary>
    [Export]
    public float MaxHealth { get; set; } = 100f;

    /// <summary>
    /// Base damage for attacks. Can be 0 (non-combatant actors).
    /// Used by combat system when creating Weapon value object.
    /// </summary>
    [Export]
    public float Damage { get; set; } = 10f;

    /// <summary>
    /// Movement speed in grid cells per turn.
    /// Default 5 = typical dungeon crawler speed.
    /// </summary>
    [Export]
    public float MoveSpeed { get; set; } = 5f;

    // ========== VISUALS ==========

    /// <summary>
    /// Actor sprite texture. REQUIRED - validation fails if null.
    /// Godot Inspector provides visual picker (drag PNG file).
    /// </summary>
    [Export]
    public Texture2D? Sprite { get; set; }

    /// <summary>
    /// Sprite tint color (for visual variety without new sprites).
    /// Default: White (no tint). Example: Red tint for "blood goblin" variant.
    /// </summary>
    [Export]
    public Color Tint { get; set; } = Colors.White;

    // ========== FUTURE: BEHAVIOR (Out of Scope for VS_021) ==========

    /// <summary>
    /// Path to AI behavior tree resource (future: VS_011 Enemy AI).
    /// Empty string = no AI (player-controlled or static actor).
    /// Example: "res://data/ai/goblin_aggressive.tres"
    /// </summary>
    [Export]
    public string BehaviorTree { get; set; } = "";
}
