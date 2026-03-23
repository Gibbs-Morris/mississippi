using System;
using System.Collections.Generic;
using System.Linq;

using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime;

/// <summary>
///     Default implementation of <see cref="ISnapshotStateConverter{TSnapshot}" /> that uses
///     <see cref="ISerializationProvider" /> for payload encoding.
/// </summary>
/// <typeparam name="TSnapshot">The type of state to convert.</typeparam>
internal sealed class SnapshotStateConverter<TSnapshot> : ISnapshotStateConverter<TSnapshot>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotStateConverter{TSnapshot}" /> class.
    /// </summary>
    /// <param name="serializationProvider">The provider for state serialization and deserialization.</param>
    public SnapshotStateConverter(
        ISerializationProvider serializationProvider
    )
        : this(serializationProvider, [serializationProvider])
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotStateConverter{TSnapshot}" /> class.
    /// </summary>
    /// <param name="serializationProvider">The default provider for state serialization and deserialization.</param>
    /// <param name="serializationProviders">All registered serialization providers available for restore.</param>
    public SnapshotStateConverter(
        ISerializationProvider serializationProvider,
        IEnumerable<ISerializationProvider> serializationProviders
    )
    {
        DefaultSerializationProvider =
            serializationProvider ?? throw new ArgumentNullException(nameof(serializationProvider));
        SerializationProviders = serializationProviders?.ToArray() ??
                                 throw new ArgumentNullException(nameof(serializationProviders));
    }

    /// <summary>
    ///     Gets the serialization provider for state encoding and decoding.
    /// </summary>
    private ISerializationProvider DefaultSerializationProvider { get; }

    /// <summary>
    ///     Gets the registered serialization providers available for restore.
    /// </summary>
    private IReadOnlyList<ISerializationProvider> SerializationProviders { get; }

    private static string GetSerializerId(
        ISerializationProvider serializationProvider
    )
    {
        ArgumentNullException.ThrowIfNull(serializationProvider);
        Type providerType = serializationProvider.GetType();
        return providerType.FullName ?? providerType.Name;
    }

    /// <inheritdoc />
    public TSnapshot FromEnvelope(
        SnapshotEnvelope envelope
    )
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ReadOnlyMemory<byte> data = envelope.Data.AsMemory();
        return ResolveDeserializationProvider(envelope).Deserialize<TSnapshot>(data);
    }

    /// <inheritdoc />
    public SnapshotEnvelope ToEnvelope(
        TSnapshot state,
        string reducerHash
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(reducerHash);
        ReadOnlyMemory<byte> data = DefaultSerializationProvider.Serialize(state);
        return new()
        {
            Data = [.. data.Span],
            DataContentType = DefaultSerializationProvider.Format,
            ReducerHash = reducerHash,
            DataSizeBytes = data.Length,
        };
    }

    private ISerializationProvider ResolveDeserializationProvider(
        SnapshotEnvelope envelope
    )
    {
        if (string.IsNullOrWhiteSpace(envelope.PayloadSerializerId))
        {
            return DefaultSerializationProvider;
        }

        List<ISerializationProvider> matches = SerializationProviders.Where(provider => string.Equals(
                GetSerializerId(provider),
                envelope.PayloadSerializerId,
                StringComparison.Ordinal))
            .ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException(
                $"No ISerializationProvider is registered for persisted snapshot serializer id '{envelope.PayloadSerializerId}'."),
            var _ => throw new InvalidOperationException(
                $"Multiple ISerializationProvider registrations share persisted snapshot serializer id '{envelope.PayloadSerializerId}'. Register a single concrete provider for that identity."),
        };
    }
}