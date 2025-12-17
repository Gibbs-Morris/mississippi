using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Serialization.Abstractions;

/// <summary>
///     Provides asynchronous serialization functionality for writing objects to streams.
/// </summary>
public interface IAsyncSerializationWriter
{
    /// <summary>
    ///     Asynchronously serializes an object to a stream. May write directly to sockets, files, pipes, etc.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="destination">The stream to write the serialized data to.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask SerializeAsync<T>(
        T value,
        Stream destination,
        CancellationToken cancellationToken = default
    );
}