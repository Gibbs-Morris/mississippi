using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Serialization.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Test serializer descriptor used to exercise configured format selection.
/// </summary>
internal sealed class TestSerializationProvider : ISerializationProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestSerializationProvider" /> class.
    /// </summary>
    /// <param name="format">The serializer format exposed for test matching.</param>
    public TestSerializationProvider(
        string format
    ) =>
        Format = format;

    /// <inheritdoc />
    public string Format { get; }

    /// <inheritdoc />
    public object Deserialize(
        Type type,
        ReadOnlyMemory<byte> payload
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public T Deserialize<T>(
        ReadOnlyMemory<byte> payload
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<T> DeserializeAsync<T>(
        Stream source,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Serialize<T>(
        T value
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask SerializeAsync<T>(
        T value,
        Stream destination,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();
}