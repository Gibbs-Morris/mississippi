using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Serialization.Json.Tests;

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
    ///     Ensures the provider surfaces the expected format name for routing and logging.
    /// </summary>
    [Fact]
    public void FormatShouldReturnSystemTextJson()
    {
        JsonSerializationProvider provider = CreateProvider();
        Assert.Equal("System.Text.Json", provider.Format);
    }

    /// <summary>
    ///     Ensures asynchronous deserialization surfaces cancellation immediately when requested.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task ReadAsyncShouldRespectCancellationTokens()
    {
        JsonSerializationProvider provider = CreateProvider();
        SampleModel model = CreateSampleModel();
        await using MemoryStream stream = new(JsonSerializer.SerializeToUtf8Bytes(model));
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            provider.ReadAsync<SampleModel>(stream, cancellationTokenSource.Token).AsTask());
    }

    /// <summary>
    ///     Ensures asynchronous deserialization returns a value when the stream contains valid JSON.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task ReadAsyncShouldReturnInstanceWhenStreamContainsPayload()
    {
        JsonSerializationProvider provider = CreateProvider();
        SampleModel model = CreateSampleModel();
        await using MemoryStream stream = new(JsonSerializer.SerializeToUtf8Bytes(model));
        SampleModel result = await provider.ReadAsync<SampleModel>(stream, CancellationToken.None);
        Assert.Equal(model, result);
    }

    /// <summary>
    ///     Ensures asynchronous deserialization throws when the stream represents <c>null</c>.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task ReadAsyncShouldThrowInvalidOperationExceptionWhenStreamContainsNull()
    {
        JsonSerializationProvider provider = CreateProvider();
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("null"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.ReadAsync<SampleModel>(stream).AsTask());
    }

    /// <summary>
    ///     Ensures <see cref="JsonSerializationProvider.Read{T}(ReadOnlyMemory{byte})" /> returns a hydrated instance for
    ///     valid payloads.
    /// </summary>
    [Fact]
    public void ReadShouldReturnInstanceWhenPayloadIsValid()
    {
        JsonSerializationProvider provider = CreateProvider();
        SampleModel model = CreateSampleModel();
        ReadOnlyMemory<byte> payload = JsonSerializer.SerializeToUtf8Bytes(model);
        SampleModel result = provider.Read<SampleModel>(payload);
        Assert.Equal(model, result);
    }

    /// <summary>
    ///     Ensures deserializing the literal <c>null</c> throws to highlight invalid content.
    /// </summary>
    [Fact]
    public void ReadShouldThrowInvalidOperationExceptionWhenPayloadIsNull()
    {
        JsonSerializationProvider provider = CreateProvider();
        ReadOnlyMemory<byte> payload = Encoding.UTF8.GetBytes("null");
        Assert.Throws<InvalidOperationException>(() => provider.Read<SampleModel>(payload));
    }

    /// <summary>
    ///     Ensures <see cref="JsonSerializationProvider.WriteAsync{T}(T, Stream, CancellationToken)" /> honors cancellation
    ///     tokens.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task WriteAsyncShouldThrowTaskCanceledExceptionWhenTokenIsCanceled()
    {
        JsonSerializationProvider provider = CreateProvider();
        await using MemoryStream stream = new();
        SampleModel model = CreateSampleModel();
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            provider.WriteAsync(model, stream, cancellationTokenSource.Token).AsTask());
    }

    /// <summary>
    ///     Ensures <see cref="JsonSerializationProvider.WriteAsync{T}(T, Stream, CancellationToken)" /> writes the serialized
    ///     payload to the provided stream.
    /// </summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    [Fact]
    public async Task WriteAsyncShouldWriteSerializedBytesToDestinationStream()
    {
        JsonSerializationProvider provider = CreateProvider();
        await using MemoryStream stream = new();
        SampleModel model = CreateSampleModel();
        await provider.WriteAsync(model, stream, CancellationToken.None);
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(model), stream.ToArray());
    }

    /// <summary>
    ///     Ensures <see cref="JsonSerializationProvider.Write{T}(T)" /> emits the same payload as
    ///     <see cref="JsonSerializer" />.
    /// </summary>
    [Fact]
    public void WriteShouldSerializeToUtf8BytesWhenValueIsProvided()
    {
        JsonSerializationProvider provider = CreateProvider();
        SampleModel model = CreateSampleModel();
        ReadOnlyMemory<byte> result = provider.Write(model);
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(model), result.ToArray());
    }
}