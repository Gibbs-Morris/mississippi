using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Serialization.Abstractions;


namespace Mississippi.EventSourcing.Serialization.Json;

/// <summary>
///     Provides JSON serialization functionality using System.Text.Json.
/// </summary>
public class JsonSerializationProvider : ISerializationProvider
{
    /// <summary>
    ///     Gets the format identifier for this serialization provider.
    /// </summary>
    public string Format { get; } = "System.Text.Json";

    /// <summary>
    ///     Deserializes an object from a byte payload.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize.</typeparam>
    /// <param name="payload">The byte payload containing the serialized data.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
    public T Read<T>(
        ReadOnlyMemory<byte> payload
    ) =>
        JsonSerializer.Deserialize<T>(payload.Span) ?? throw new InvalidOperationException();

    /// <summary>
    ///     Asynchronously deserializes an object from a stream.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize.</typeparam>
    /// <param name="source">The stream containing the serialized data.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization results in null.</exception>
    public async ValueTask<T> ReadAsync<T>(
        Stream source,
        CancellationToken cancellationToken = default
    )
    {
        T? result = await JsonSerializer.DeserializeAsync<T>(source, cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException();
    }

    /// <summary>
    ///     Serializes an object to a byte array.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A byte array containing the serialized data.</returns>
    public ReadOnlyMemory<byte> Write<T>(
        T value
    ) =>
        JsonSerializer.SerializeToUtf8Bytes(value);

    /// <summary>
    ///     Asynchronously serializes an object to a stream.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="destination">The stream to write the serialized data to.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when destination is null.</exception>
    public async ValueTask WriteAsync<T>(
        T value,
        Stream destination,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(destination);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await destination.WriteAsync(bytes, cancellationToken);
    }
}