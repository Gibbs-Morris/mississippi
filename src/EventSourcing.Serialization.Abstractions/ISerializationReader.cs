using System;


namespace Mississippi.EventSourcing.Serialization.Abstractions;

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
    T Deserialize<T>(
        ReadOnlyMemory<byte> payload
    );

    /// <summary>
    ///     Deserializes an object from a byte payload using a runtime type.
    ///     This is a pure, in-memory operation that never blocks on I/O.
    /// </summary>
    /// <param name="type">The type of object to deserialize.</param>
    /// <param name="payload">The byte payload containing the serialized data.</param>
    /// <returns>The deserialized object.</returns>
    object Deserialize(
        Type type,
        ReadOnlyMemory<byte> payload
    );
}