using Darklands.Core.Domain.Common;
using System.Text;

namespace Darklands.Core.Infrastructure.Identity;

/// <summary>
/// Production ID generator using Guid.NewGuid() for non-deterministic scenarios.
/// Used for entities that don't require deterministic generation.
/// </summary>
public sealed class GuidIdGenerator : IStableIdGenerator
{
    /// <summary>
    /// Singleton instance for convenience.
    /// </summary>
    public static readonly GuidIdGenerator Instance = new();

    /// <summary>
    /// Generates a new unique GUID.
    /// </summary>
    /// <returns>New unique GUID</returns>
    public Guid NewGuid() => Guid.NewGuid();

    /// <summary>
    /// Generates a new string-based unique identifier.
    /// Uses base62 encoding of GUID for readability.
    /// </summary>
    /// <param name="length">Length of the generated string</param>
    /// <returns>Unique string identifier</returns>
    public string NewStringId(int length = 26)
    {
        var guid = NewGuid();
        var guidString = guid.ToString("N"); // Remove hyphens

        // Simple truncation for now - in production might use proper base62 encoding
        return guidString.Length >= length
            ? guidString[..length].ToUpperInvariant()
            : guidString.ToUpperInvariant();
    }
}
