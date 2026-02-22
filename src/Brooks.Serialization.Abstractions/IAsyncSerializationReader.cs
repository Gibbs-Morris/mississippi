using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Serialization.Abstractions;

/// <summary>
///     Provides asynchronous deserialization functionality for reading objects from streams.
/// </summary>
public interface IAsyncSerializationReader
{
    /// <summary>
    ///     Asynchronously deserializes an object from a stream. May read directly from sockets, files, pipes, etc.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize.</typeparam>
    /// <param name="source">The stream containing the serialized data.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the deserialized object.</returns>
    ValueTask<T> DeserializeAsync<T>(
        Stream source,
        CancellationToken cancellationToken = default
    );
}