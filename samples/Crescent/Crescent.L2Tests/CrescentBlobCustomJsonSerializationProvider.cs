using System.Text.Json;

using Mississippi.Brooks.Serialization.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     JSON-compatible serialization provider used to prove Blob snapshot restart survival with a non-default format.
/// </summary>
internal sealed class CrescentBlobCustomJsonSerializationProvider : ISerializationProvider
{
    /// <summary>
    ///     Stable non-default format identifier used by the Blob snapshot trust slice.
    /// </summary>
    public const string SerializerFormat = "Crescent.CustomJson";

    /// <inheritdoc />
    public string Format { get; } = SerializerFormat;

    /// <inheritdoc />
    public T Deserialize<T>(
        ReadOnlyMemory<byte> payload
    ) =>
        JsonSerializer.Deserialize<T>(payload.Span) ?? throw new InvalidOperationException();

    /// <inheritdoc />
    public object Deserialize(
        Type type,
        ReadOnlyMemory<byte> payload
    ) =>
        JsonSerializer.Deserialize(payload.Span, type) ?? throw new InvalidOperationException();

    /// <inheritdoc />
    public async ValueTask<T> DeserializeAsync<T>(
        Stream source,
        CancellationToken cancellationToken = default
    )
    {
        T? result = await JsonSerializer.DeserializeAsync<T>(source, cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Serialize<T>(
        T value
    ) =>
        JsonSerializer.SerializeToUtf8Bytes(value);

    /// <inheritdoc />
    public async ValueTask SerializeAsync<T>(
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