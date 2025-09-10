using System.Text.Json.Serialization;

namespace Darklands.Core.Domain.Common;

/// <summary>
/// Marker interface for entities that can be persisted to save files.
/// All entities implementing this interface must be designed according to ADR-005.
/// 
/// Requirements:
/// - Use records or immutable classes
/// - Reference other entities by ID, not object references
/// - No framework types (Godot nodes, Unity objects) in domain
/// - No delegates or events in persistent state
/// - Separate persistent vs transient state
/// </summary>
public interface IPersistentEntity
{
    /// <summary>
    /// Unique identifier for this entity instance.
    /// Must be stable across save/load cycles.
    /// </summary>
    IEntityId Id { get; }
}

/// <summary>
/// Base interface for all entity identifiers.
/// Provides type safety and consistent behavior for entity references.
/// </summary>
public interface IEntityId
{
    /// <summary>
    /// The underlying unique identifier value.
    /// </summary>
    Guid Value { get; }
}

/// <summary>
/// Interface for transient state that should not be saved.
/// Used for animations, cached data, temporary UI state, etc.
/// 
/// Transient state is kept separate from persistent entities
/// and reconstructed after loading from save files.
/// </summary>
public interface ITransientState
{
    /// <summary>
    /// Resets transient state to default values.
    /// Called when loading from save or when entity state changes.
    /// </summary>
    void Reset();
}
