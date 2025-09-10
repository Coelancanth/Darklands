using LanguageExt;

namespace Darklands.Core.Domain.SaveSystem;

/// <summary>
/// Abstraction for serialization operations on save data.
/// Provides pluggable serialization strategy to support different formats and scenarios.
/// 
/// Default Implementation: System.Text.Json for performance
/// Advanced Scenarios: Newtonsoft.Json for polymorphism, JsonExtensionData, custom converters
/// Future: Binary serialization for production builds
/// </summary>
public interface ISerializationProvider
{
    /// <summary>
    /// Serializes an object to JSON string.
    /// </summary>
    /// <typeparam name="T">Type to serialize</typeparam>
    /// <param name="value">Object to serialize</param>
    /// <returns>Success with JSON string or error if serialization failed</returns>
    Fin<string> SerializeToString<T>(T value);

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Success with deserialized object or error if deserialization failed</returns>
    Fin<T> DeserializeFromString<T>(string json);

    /// <summary>
    /// Serializes an object to binary data.
    /// More compact than JSON for large save files.
    /// </summary>
    /// <typeparam name="T">Type to serialize</typeparam>
    /// <param name="value">Object to serialize</param>
    /// <returns>Success with binary data or error if serialization failed</returns>
    Fin<byte[]> SerializeToBytes<T>(T value);

    /// <summary>
    /// Deserializes binary data to an object.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <param name="data">Binary data to deserialize</param>
    /// <returns>Success with deserialized object or error if deserialization failed</returns>
    Fin<T> DeserializeFromBytes<T>(byte[] data);

    /// <summary>
    /// Gets information about this serialization provider.
    /// </summary>
    SerializationInfo Info { get; }
}

/// <summary>
/// Information about a serialization provider's capabilities.
/// </summary>
public sealed record SerializationInfo(
    string Name,
    SerializationFormat Format,
    bool SupportsPolymorphism,
    bool SupportsExtensionData,
    bool SupportsCustomConverters
);

/// <summary>
/// Format used by a serialization provider.
/// </summary>
public enum SerializationFormat
{
    Json,
    Binary,
    MessagePack,
    ProtocolBuffers
}
