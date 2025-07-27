namespace Mississippi.Core.Abstractions.Providers.Serialization;

/// <summary>
///     Provides synchronous deserialization functionality for reading objects from byte payloads.
/// </summary>
public interface ISerializationReader
{
    /// <summary>
    ///     Deserializes an object from a byte payload. This is a pure, in-memory operation that never blocks on I/O.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize.</typeparam>
    /// <param name="payload">The byte payload containing the serialized data.</param>
    /// <returns>The deserialized object.</returns>
    T Read<T>(
        ReadOnlyMemory<byte> payload
    );
}