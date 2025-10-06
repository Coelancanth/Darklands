using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Components;

namespace Darklands.Core.Domain.Entities;

/// <summary>
/// Represents an actor in the game world (player, enemies, NPCs, etc.).
/// Actor is a "component container" - behaviors are added via components.
/// </summary>
/// <remarks>
/// <para><b>Component Pattern</b>:</para>
/// <para>
/// Actor itself is lightweight - just ID and name. All behaviors (health, weapons, equipment)
/// are added as components. This allows flexible composition:
/// - Player: Health + Weapon + Equipment + Inventory
/// - Enemy: Health + Weapon
/// - Boss: Health + Weapon + Phases
/// - NPC: Dialogue (no combat components)
/// </para>
///
/// <para><b>Two-System Tracking</b>:</para>
/// <list type="bullet">
/// <item><description>IActorRepository - Stores Actor entities (WHO: components, name)</description></item>
/// <item><description>IActorPositionService - Stores positions (WHERE: grid coordinates)</description></item>
/// <item><description>Linked by ActorId - both systems use same ID</description></item>
/// </list>
///
/// <para><b>Lifecycle</b>:</para>
/// <code>
/// // 1. Create from template (ActorFactory)
/// var actor = ActorFactory.CreateFromTemplate("goblin");
///
/// // 2. Register in both systems
/// await _actors.AddActorAsync(actor);
/// await _positions.RegisterActorAsync(actor.Id, spawnPosition);
///
/// // 3. Use components
/// var health = actor.GetComponent&lt;IHealthComponent&gt;();
/// health.TakeDamage(10);
///
/// // 4. Remove when dead
/// await _actors.RemoveActorAsync(actor.Id);
/// await _positions.RemoveActorAsync(actor.Id);
/// </code>
/// </remarks>
public class Actor
{
    private readonly Dictionary<Type, IComponent> _components = new();

    /// <summary>
    /// Unique identifier for this actor.
    /// Links Actor entity (IActorRepository) with position (IActorPositionService).
    /// </summary>
    public ActorId Id { get; }

    /// <summary>
    /// Translation key for actor's name (e.g., "ACTOR_GOBLIN", "ACTOR_PLAYER").
    /// Follows ADR-005 i18n discipline - Domain returns keys, Presentation translates.
    /// </summary>
    public string NameKey { get; }

    /// <summary>
    /// Creates a new actor with ID and name.
    /// Components must be added separately via AddComponent().
    /// </summary>
    /// <param name="id">Unique identifier</param>
    /// <param name="nameKey">Translation key for actor name</param>
    public Actor(ActorId id, string nameKey)
    {
        Id = id;
        NameKey = nameKey;
    }

    /// <summary>
    /// Adds a component to this actor.
    /// </summary>
    /// <typeparam name="T">Component interface type (IHealthComponent, IWeaponComponent, etc.)</typeparam>
    /// <param name="component">Component instance to add</param>
    /// <returns>Success if added, Failure if component of this type already exists</returns>
    public Result AddComponent<T>(T component) where T : IComponent
    {
        var type = typeof(T);

        if (_components.ContainsKey(type))
        {
            return Result.Failure($"Actor {Id} already has component of type {type.Name}");
        }

        _components[type] = component;
        return Result.Success();
    }

    /// <summary>
    /// Gets a component of the specified type.
    /// </summary>
    /// <typeparam name="T">Component interface type</typeparam>
    /// <returns>Success with component if exists, Failure if not found</returns>
    public Result<T> GetComponent<T>() where T : IComponent
    {
        var type = typeof(T);

        if (!_components.TryGetValue(type, out var component))
        {
            return Result.Failure<T>($"Actor {Id} does not have component of type {type.Name}");
        }

        return Result.Success((T)component);
    }

    /// <summary>
    /// Checks if actor has a component of the specified type.
    /// </summary>
    /// <typeparam name="T">Component interface type</typeparam>
    /// <returns>True if component exists, false otherwise</returns>
    public bool HasComponent<T>() where T : IComponent
    {
        return _components.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Removes a component from this actor.
    /// </summary>
    /// <typeparam name="T">Component interface type</typeparam>
    /// <returns>Success if removed, Failure if component doesn't exist</returns>
    public Result RemoveComponent<T>() where T : IComponent
    {
        var type = typeof(T);

        if (!_components.Remove(type))
        {
            return Result.Failure($"Actor {Id} does not have component of type {type.Name}");
        }

        return Result.Success();
    }
}
