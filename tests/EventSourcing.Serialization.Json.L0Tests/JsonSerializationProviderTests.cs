using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Serialization.Json.L0Tests;

/// <summary>
///     Tests for <see cref="JsonSerializationProvider" /> behavior.
/// </summary>
public sealed class JsonSerializationProviderTests
{
    private static JsonSerializationProvider CreateProvider() => new();

    private static SampleModel CreateSampleModel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "sample",
            Count = 5,
        };

    private sealed record SampleModel
    {
        public int Count { get; init; }

        public Guid Id { get; init; }

        public string? Name { get; init; }
    }

    /// <summary>
    ///     Ensures asynchronous deserialization surfaces cancellation immediately when requested.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task DeserializeAsyncShouldRespectCancellationTokens()
    {
        JsonSerializationProvider provider = CreateProvider();
        SampleModel model = CreateSampleModel();
        await using MemoryStream stream = new(JsonSerializer.SerializeToUtf8Bytes(model));
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            provider.DeserializeAsync<SampleModel>(stream, cancellationTokenSource.Token).AsTask());
    }

    /// <summary>
    ///     Ensures asynchronous deserialization returns a value when the stream contains valid JSON.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task DeserializeAsyncShouldReturnInstanceWhenStreamContainsPayload()
    {
        JsonSerializationProvider provider = CreateProvider();
        SampleModel model = CreateSampleModel();
        await using MemoryStream stream = new(JsonSerializer.SerializeToUtf8Bytes(model));
        SampleModel result = await provider.DeserializeAsync<SampleModel>(stream, CancellationToken.None);
        Assert.Equal(model, result);
    }

    /// <summary>
    ///     Ensures asynchronous deserialization throws when the stream represents <c>null</c>.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task DeserializeAsyncShouldThrowInvalidOperationExceptionWhenStreamContainsNull()
    {
        JsonSerializationProvider provider = CreateProvider();
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("null"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.DeserializeAsync<SampleModel>(stream).AsTask());
    }

    /// <summary>
    ///     Ensures <see cref="JsonSerializationProvider.Deserialize{T}(ReadOnlyMemory{byte})" /> returns a hydrated instance
    ///     for
    ///     valid payloads.
    /// </summary>
    [Fact]
    public void DeserializeShouldReturnInstanceWhenPayloadIsValid()
    {
        JsonSerializationProvider provider = CreateProvider();
        SampleModel model = CreateSampleModel();
        ReadOnlyMemory<byte> payload = JsonSerializer.SerializeToUtf8Bytes(model);
        SampleModel result = provider.Deserialize<SampleModel>(payload);
        Assert.Equal(model, result);
    }

    /// <summary>
    ///     Ensures deserializing the literal <c>null</c> throws to highlight invalid content.
    /// </summary>
    [Fact]
    public void DeserializeShouldThrowInvalidOperationExceptionWhenPayloadIsNull()
    {
        JsonSerializationProvider provider = CreateProvider();
        ReadOnlyMemory<byte> payload = Encoding.UTF8.GetBytes("null");
        Assert.Throws<InvalidOperationException>(() => provider.Deserialize<SampleModel>(payload));
    }

    /// <summary>
    ///     Ensures the provider surfaces the expected format name for routing and logging.
    /// </summary>
    [Fact]
    public void FormatShouldReturnSystemTextJson()
    {
        JsonSerializationProvider provider = CreateProvider();
        Assert.Equal("System.Text.Json", provider.Format);
    }

    /// <summary>
    ///     Ensures <see cref="JsonSerializationProvider.SerializeAsync{T}(T, Stream, CancellationToken)" /> honors
    ///     cancellation
    ///     tokens.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task SerializeAsyncShouldThrowTaskCanceledExceptionWhenTokenIsCanceled()
    {
        JsonSerializationProvider provider = CreateProvider();
        await using MemoryStream stream = new();
        SampleModel model = CreateSampleModel();
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            provider.SerializeAsync(model, stream, cancellationTokenSource.Token).AsTask());
    }

    /// <summary>
    ///     Ensures <see cref="JsonSerializationProvider.SerializeAsync{T}(T, Stream, CancellationToken)" /> writes the
    ///     serialized
    ///     payload to the provided stream.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task SerializeAsyncShouldWriteSerializedBytesToDestinationStream()
    {
        JsonSerializationProvider provider = CreateProvider();
        await using MemoryStream stream = new();
        SampleModel model = CreateSampleModel();
        await provider.SerializeAsync(model, stream, CancellationToken.None);
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(model), stream.ToArray());
    }

    /// <summary>
    ///     Ensures <see cref="JsonSerializationProvider.Serialize{T}(T)" /> emits the same payload as
    ///     <see cref="JsonSerializer" />.
    /// </summary>
    [Fact]
    public void SerializeShouldSerializeToUtf8BytesWhenValueIsProvided()
    {
        JsonSerializationProvider provider = CreateProvider();
        SampleModel model = CreateSampleModel();
        ReadOnlyMemory<byte> result = provider.Serialize(model);
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(model), result.ToArray());
    }
}