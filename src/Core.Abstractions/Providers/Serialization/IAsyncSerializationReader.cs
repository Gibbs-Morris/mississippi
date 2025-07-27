namespace Mississippi.Core.Abstractions.Providers.Serialization;

public interface IAsyncSerializationReader
{
    /// May read directly from sockets, files, pipes, etc.
    ValueTask<T> ReadAsync<T>(
        Stream source,
        CancellationToken cancellationToken = default);
}