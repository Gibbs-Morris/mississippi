using System.Text.Json;

namespace Mississippi.Core.Abstractions.Providers.Serialization;

public class JsonSerializationProvider : ISerializationProvider
{
    public T Read<T>(ReadOnlyMemory<byte> payload)
    {
        return JsonSerializer.Deserialize<T>(payload.Span) ?? throw new InvalidOperationException();
    }

    public async ValueTask<T> ReadAsync<T>(Stream source, CancellationToken cancellationToken = default)
    {
        var result = await JsonSerializer.DeserializeAsync<T>(source, cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException();
    }

    public ReadOnlyMemory<byte> Write<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value);
    }

    public async ValueTask WriteAsync<T>(T value, Stream destination, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await destination.WriteAsync(bytes, cancellationToken);
    }

    public string Format { get; } = "System.Text.Json";
}