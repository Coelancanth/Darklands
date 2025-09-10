namespace Darklands.Core.Domain.Common;

/// <summary>
/// Provides stable ID creation without leaking framework specifics into domain layer.
/// Enables deterministic ID generation for testing and replay scenarios (ADR-004 compliance).
/// 
/// Infrastructure layer can implement via:
/// - GUIDv7/ULID for globally unique IDs in non-deterministic contexts
/// - Deterministic RNG-derived IDs for in-simulation deterministic contexts
/// - Sequential IDs for testing scenarios
/// </summary>
public interface IStableIdGenerator
{
    /// <summary>
    /// Generates a new unique identifier.
    /// Implementation determines whether this is deterministic or non-deterministic.
    /// </summary>
    /// <returns>A unique GUID suitable for entity identification</returns>
    Guid NewGuid();

    /// <summary>
    /// Generates a new string-based unique identifier.
    /// Useful for human-readable IDs or when GUID overhead is undesirable.
    /// </summary>
    /// <param name="length">Length of the generated string (default: 26 for ULID compatibility)</param>
    /// <returns>A unique string identifier</returns>
    string NewStringId(int length = 26);
}
