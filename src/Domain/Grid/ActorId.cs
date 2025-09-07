using System;

namespace Darklands.Core.Domain.Grid
{
    /// <summary>
    /// Unique identifier for combat actors (players, NPCs, creatures) on the grid.
    /// Immutable value object that ensures type safety for actor references.
    /// </summary>
    public readonly record struct ActorId
    {
        /// <summary>
        /// Internal unique identifier for the actor.
        /// </summary>
        public Guid Value { get; }

        /// <summary>
        /// Private constructor to enforce factory method usage.
        /// </summary>
        private ActorId(Guid value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new unique ActorId.
        /// </summary>
        public static ActorId NewId() => new(Guid.NewGuid());

        /// <summary>
        /// Creates an ActorId from an existing Guid value.
        /// Used for deserialization and testing.
        /// </summary>
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
