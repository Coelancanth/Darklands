using System;
using Darklands.Domain.Common;

namespace Darklands.Domain.Grid
{
    /// <summary>
    /// Unique identifier for combat actors (players, NPCs, creatures) on the grid.
    /// Immutable value object that ensures type safety for actor references.
    /// Designed for save/load compatibility per ADR-005.
    /// </summary>
    public readonly record struct ActorId(Guid Value) : IEntityId
    {
        /// <summary>
        /// Creates a new unique ActorId using the provided ID generator.
        /// Supports both deterministic (testing/replay) and non-deterministic (production) generation.
        /// </summary>
        /// <param name="ids">Stable ID generator instance</param>
        /// <returns>New unique ActorId</returns>
        public static ActorId NewId(IStableIdGenerator ids) => new(ids.NewGuid());

        /// <summary>
        /// Creates a new unique ActorId using Guid.NewGuid().
        /// DEPRECATED: Use NewId(IStableIdGenerator) for save-ready entities.
        /// Kept for backwards compatibility with existing code.
        /// </summary>
        [Obsolete("Use NewId(IStableIdGenerator) for save-ready entities. This method will be removed after migration.")]
        public static ActorId NewId() => new(Guid.NewGuid());

        /// <summary>
        /// Creates an ActorId from an existing Guid value.
        /// Used for deserialization, testing, and migration scenarios.
        /// </summary>
        /// <param name="guid">Existing GUID value</param>
        /// <returns>ActorId wrapping the provided GUID</returns>
        public static ActorId FromGuid(Guid guid) => new(guid);

        /// <summary>
        /// Empty ActorId for unoccupied tiles or error states.
        /// </summary>
        public static readonly ActorId Empty = new(Guid.Empty);

        /// <summary>
        /// Checks if this ActorId represents an empty/null state.
        /// </summary>
        public bool IsEmpty => Value == Guid.Empty;

        /// <summary>
        /// Provides a short, human-readable representation for logging and debugging.
        /// Shows first 8 characters of GUID for uniqueness without overwhelming logs.
        /// </summary>
        public override string ToString() => $"Actor_{Value.ToString()[..8]}";

        /// <summary>
        /// Gets the full GUID string for serialization or exact matching needs.
        /// </summary>
        public string ToFullString() => Value.ToString();

        /// <summary>
        /// Provides implicit conversion to Guid for interoperability.
        /// </summary>
        public static implicit operator Guid(ActorId actorId) => actorId.Value;

        /// <summary>
        /// Provides explicit conversion from Guid for creation.
        /// </summary>
        public static explicit operator ActorId(Guid guid) => FromGuid(guid);
    }
}
