using LanguageExt;
using Darklands.Core.Domain.GameState;

namespace Darklands.Core.Domain.SaveSystem;

/// <summary>
/// Validates save data integrity and compliance with save system requirements.
/// Ensures save files are valid before writing and after reading.
/// </summary>
public interface ISaveValidator
{
    /// <summary>
    /// Validates a save container before writing to disk.
    /// Checks data integrity, version compatibility, and required fields.
    /// </summary>
    /// <param name="container">Save container to validate</param>
    /// <returns>Success if valid, or error with validation failure details</returns>
    Fin<Unit> ValidateForSave(SaveContainer container);

    /// <summary>
    /// Validates a save container after reading from disk.
    /// Checks checksum, version compatibility, and data corruption.
    /// </summary>
    /// <param name="container">Save container to validate</param>
    /// <returns>Success if valid, or error with validation failure details</returns>
    Fin<Unit> ValidateAfterLoad(SaveContainer container);

    /// <summary>
    /// Validates that game state is save-ready according to ADR-005.
    /// Ensures all entities implement IPersistentEntity and follow save patterns.
    /// </summary>
    /// <param name="state">Game state to validate</param>
    /// <returns>Success if save-ready, or error with compliance issues</returns>
    Fin<Unit> ValidateGameState(Darklands.Core.Domain.GameState.GameState state);

    /// <summary>
    /// Calculates integrity checksum for save data.
    /// Used to detect corruption and tampering.
    /// </summary>
    /// <param name="data">Binary save data</param>
    /// <returns>Base64-encoded checksum string</returns>
    string CalculateChecksum(byte[] data);

    /// <summary>
    /// Verifies checksum matches the provided data.
    /// </summary>
    /// <param name="data">Binary save data</param>
    /// <param name="expectedChecksum">Expected checksum to verify against</param>
    /// <returns>True if checksum matches, false otherwise</returns>
    bool VerifyChecksum(byte[] data, string expectedChecksum);
}
