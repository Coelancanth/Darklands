using Darklands.Domain.Common;
using System.Security.Cryptography;
using System.Text;

namespace Darklands.Application.Infrastructure.Identity;

/// <summary>
/// Production ID generator using cryptographically strong random numbers for non-deterministic scenarios.
/// Provides globally unique identifiers suitable for production save systems.
/// Thread-safe and suitable for high-throughput scenarios.
/// </summary>
public sealed class GuidIdGenerator : IStableIdGenerator
{
    /// <summary>
    /// Singleton instance for convenience in non-DI scenarios.
    /// </summary>
    public static readonly GuidIdGenerator Instance = new();

    /// <summary>
    /// Generates a new cryptographically unique GUID.
    /// Uses Guid.NewGuid() which provides strong randomness guarantees.
    /// </summary>
    /// <returns>New globally unique GUID</returns>
    public Guid NewGuid() => Guid.NewGuid();

    /// <summary>
    /// Generates a new string-based unique identifier using base62 encoding.
    /// Provides URL-safe, human-readable identifiers with good entropy.
    /// Uses cryptographically strong random bytes for maximum uniqueness.
    /// </summary>
    /// <param name="length">Length of the generated string (default: 26 for ULID-like compatibility)</param>
    /// <returns>Unique base62-encoded string identifier</returns>
    public string NewStringId(int length = 26)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive");

        const string base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        // Calculate bytes needed for desired entropy
        var bytesNeeded = (int)Math.Ceiling(length * Math.Log(62) / Math.Log(256));
        var randomBytes = new byte[bytesNeeded];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var result = new StringBuilder(length);

        // Convert random bytes to base62 string
        var bigInteger = new System.Numerics.BigInteger(randomBytes.Concat(new byte[] { 0 }).ToArray());

        for (int i = 0; i < length; i++)
        {
            bigInteger = System.Numerics.BigInteger.DivRem(bigInteger, 62, out var remainder);
            result.Append(base62Chars[(int)remainder]);
        }

        return result.ToString();
    }
}
