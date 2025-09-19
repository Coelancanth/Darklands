using Darklands.Domain.Common;
using Darklands.Domain.Determinism;
using System.Text;

namespace Darklands.Application.Infrastructure.Identity;

/// <summary>
/// Deterministic ID generator using IDeterministicRandom for testing and replay scenarios.
/// Ensures consistent ID generation across runs for same seed, enabling reliable testing and save/load functionality.
/// Implements ADR-004 deterministic simulation requirements.
/// </summary>
public sealed class DeterministicIdGenerator : IStableIdGenerator
{
    private readonly IDeterministicRandom _random;

    /// <summary>
    /// Creates a new deterministic ID generator.
    /// </summary>
    /// <param name="random">Deterministic random source for consistent ID generation</param>
    public DeterministicIdGenerator(IDeterministicRandom random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// Generates a deterministic GUID using the random source.
    /// Same seed will always produce the same sequence of GUIDs.
    /// </summary>
    /// <returns>Deterministic GUID</returns>
    public Guid NewGuid()
    {
        // Generate 16 random bytes for GUID
        var bytes = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            var byteValue = _random.Range(0, 256, $"GUID-byte-{i}").Match(
                Succ: value => (byte)value,
                Fail: error => throw new InvalidOperationException($"Random generation failed: {error.Message}")
            );
            bytes[i] = byteValue;
        }

        // Set version (4) and variant bits to make it a valid GUID v4
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40); // Version 4
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // Variant 10

        return new Guid(bytes);
    }

    /// <summary>
    /// Generates a deterministic string-based unique identifier.
    /// Uses deterministic random to create base62-encoded strings for readability.
    /// </summary>
    /// <param name="length">Length of the generated string (default: 26)</param>
    /// <returns>Deterministic string identifier</returns>
    public string NewStringId(int length = 26)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive");

        const string base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var result = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            var index = _random.Range(0, base62Chars.Length, $"StringId-char-{i}").Match(
                Succ: value => value,
                Fail: error => throw new InvalidOperationException($"Random generation failed: {error.Message}")
            );
            result.Append(base62Chars[index]);
        }

        return result.ToString();
    }
}
