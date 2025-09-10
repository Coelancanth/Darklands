using LanguageExt;

namespace Darklands.Core.Application.SaveSystem;

/// <summary>
/// Abstraction for save file storage operations.
/// Provides platform-independent access to the filesystem for save game operations.
/// 
/// Implementations handle platform-specific concerns:
/// - Windows: %APPDATA%/Darklands/saves
/// - Linux: ~/.local/share/Darklands/saves  
/// - Mac: ~/Library/Application Support/Darklands/saves
/// - Godot: user://saves
/// </summary>
public interface ISaveStorage
{
    /// <summary>
    /// Gets the platform-appropriate directory for save files.
    /// </summary>
    /// <returns>Absolute path to save directory</returns>
    string GetSaveDirectory();

    /// <summary>
    /// Combines path segments using platform-appropriate separators.
    /// </summary>
    /// <param name="parts">Path segments to combine</param>
    /// <returns>Combined path</returns>
    string CombinePath(params string[] parts);

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">File path to check</param>
    /// <returns>Success with true if exists, false if not, or error</returns>
    Task<Fin<bool>> ExistsAsync(string path);

    /// <summary>
    /// Ensures the directory exists, creating it if necessary.
    /// </summary>
    /// <param name="directoryPath">Directory path to ensure</param>
    /// <returns>Success or error with failure reason</returns>
    Task<Fin<Unit>> EnsureDirectoryAsync(string directoryPath);

    /// <summary>
    /// Writes binary data to a file atomically (temp file + rename).
    /// Prevents corruption if write is interrupted.
    /// </summary>
    /// <param name="path">Target file path</param>
    /// <param name="data">Binary data to write</param>
    /// <returns>Success or error with failure reason</returns>
    Task<Fin<Unit>> WriteAsync(string path, byte[] data);

    /// <summary>
    /// Reads binary data from a file.
    /// </summary>
    /// <param name="path">File path to read</param>
    /// <returns>Success with file data or error if read failed</returns>
    Task<Fin<byte[]>> ReadAsync(string path);

    /// <summary>
    /// Deletes a file if it exists.
    /// </summary>
    /// <param name="path">File path to delete</param>
    /// <returns>Success or error with failure reason</returns>
    Task<Fin<Unit>> DeleteAsync(string path);

    /// <summary>
    /// Gets file metadata without reading content.
    /// </summary>
    /// <param name="path">File path to query</param>
    /// <returns>Success with metadata or error if file doesn't exist</returns>
    Task<Fin<SaveFileInfo>> GetFileInfoAsync(string path);
}

/// <summary>
/// Metadata about a save file without reading its contents.
/// </summary>
public sealed record SaveFileInfo(
    string Path,
    long SizeInBytes,
    DateTimeOffset LastModified,
    DateTimeOffset Created
);
