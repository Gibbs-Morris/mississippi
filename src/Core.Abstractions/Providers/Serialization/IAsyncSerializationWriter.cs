namespace Mississippi.Core.Abstractions.Providers.Serialization;

public interface IAsyncSerializationWriter
{
    /// May write directly to sockets, files, pipes, etc.
    ValueTask WriteAsync<T>(
        T value,
        Stream destination,
        CancellationToken cancellationToken = default);
}