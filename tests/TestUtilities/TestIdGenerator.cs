using Darklands.Domain.Common;

namespace Darklands.Core.Tests.TestUtilities;

/// <summary>
/// Test-specific ID generator for predictable testing scenarios.
/// Uses Guid.NewGuid() for simplicity but could be extended for deterministic testing.
/// </summary>
public sealed class TestIdGenerator : IStableIdGenerator
{
    /// <summary>
    /// Singleton instance for convenience in tests.
    /// </summary>
    public static readonly TestIdGenerator Instance = new();

    /// <summary>
    /// Generates a new unique GUID for testing.
    /// </summary>
    /// <returns>New unique GUID</returns>
    public Guid NewGuid() => Guid.NewGuid();

    /// <summary>
    /// Generates a new string-based unique identifier for testing.
    /// Uses simple GUID-based approach for test consistency.
    /// </summary>
    /// <param name="length">Length of the generated string</param>
    /// <returns>Unique string identifier</returns>
    public string NewStringId(int length = 26)
    {
        var guid = NewGuid();
        var guidString = guid.ToString("N"); // Remove hyphens

        return guidString.Length >= length
            ? guidString[..length].ToUpperInvariant()
            : guidString.ToUpperInvariant();
    }
}
