using Darklands.Core.Domain.Common;

namespace Darklands.Core.Domain.Grid;

/// <summary>
/// Unique identifier for combat grids in the tactical system.
/// Immutable value object that ensures type safety for grid references.
/// Designed for save/load compatibility per ADR-005.
/// </summary>
public readonly record struct GridId(Guid Value) : IEntityId
{
    /// <summary>
    /// Creates a new unique GridId using the provided ID generator.
    /// Supports both deterministic (testing/replay) and non-deterministic (production) generation.
    /// </summary>
    /// <param name="ids">Stable ID generator instance</param>
    /// <returns>New unique GridId</returns>
    public static GridId NewId(IStableIdGenerator ids) => new(ids.NewGuid());

    /// <summary>
    /// Creates a GridId from an existing Guid value.
    /// Used for deserialization, testing, and migration scenarios.
    /// </summary>
    /// <param name="guid">Existing GUID value</param>
    /// <returns>GridId wrapping the provided GUID</returns>
    public static GridId FromGuid(Guid guid) => new(guid);

    /// <summary>
    /// Empty GridId for error states or uninitialized references.
    /// </summary>
    public static readonly GridId Empty = new(Guid.Empty);

    /// <summary>
    /// Checks if this GridId represents an empty/null state.
    /// </summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>
    /// Provides a short, human-readable representation for logging and debugging.
    /// Shows first 8 characters of GUID for uniqueness without overwhelming logs.
    /// </summary>
    public override string ToString() => $"Grid_{Value.ToString()[..8]}";

    /// <summary>
    /// Gets the full GUID string for serialization or exact matching needs.
    /// </summary>
    public string ToFullString() => Value.ToString();

    /// <summary>
    /// Provides implicit conversion to Guid for interoperability.
    /// </summary>
    public static implicit operator Guid(GridId gridId) => gridId.Value;

    /// <summary>
    /// Provides explicit conversion from Guid for creation.
    /// </summary>
    public static explicit operator GridId(Guid guid) => FromGuid(guid);
}
