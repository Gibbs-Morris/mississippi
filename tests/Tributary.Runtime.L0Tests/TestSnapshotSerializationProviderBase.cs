using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Serialization.Abstractions;


namespace Mississippi.Tributary.Runtime.L0Tests;

/// <summary>
///     Base test serializer used to verify snapshot serializer selection.
/// </summary>
internal abstract class TestSnapshotSerializationProviderBase : ISerializationProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestSnapshotSerializationProviderBase" /> class.
    /// </summary>
    /// <param name="format">The serializer content type.</param>
    protected TestSnapshotSerializationProviderBase(
        string format
    ) =>
        Format = format;

    /// <summary>
    ///     Gets or sets the value returned from deserialize calls.
    /// </summary>
    public object? DeserializedValue { get; set; }

    /// <summary>
    ///     Gets the number of deserialize calls.
    /// </summary>
    public int DeserializeCallCount { get; private set; }

    /// <inheritdoc />
    public string Format { get; }

    /// <summary>
    ///     Gets the persisted serializer identity for this provider instance.
    /// </summary>
    public string SerializerId => GetType().FullName ?? GetType().Name;

    /// <inheritdoc />
    public object Deserialize(
        Type type,
        ReadOnlyMemory<byte> payload
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        DeserializeCallCount++;
        return DeserializedValue ?? throw new InvalidOperationException("No deserialized value was configured for the test provider.");
    }

    /// <inheritdoc />
    public T Deserialize<T>(
        ReadOnlyMemory<byte> payload
    )
    {
        DeserializeCallCount++;
        return DeserializedValue is T typedValue
            ? typedValue
            : throw new InvalidOperationException("No deserialized value was configured for the test provider.");
    }

    /// <inheritdoc />
    public ValueTask<T> DeserializeAsync<T>(
        Stream source,
        CancellationToken cancellationToken = default
    ) => ValueTask.FromResult(Deserialize<T>(ReadOnlyMemory<byte>.Empty));

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