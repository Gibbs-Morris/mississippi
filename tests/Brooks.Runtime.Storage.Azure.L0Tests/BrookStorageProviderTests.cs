using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Tests for the Increment 1 Brooks Azure provider shell behavior.
/// </summary>
public sealed class BrookStorageProviderTests
{
    /// <summary>
    ///     Verifies the provider reports its frozen Increment 1 storage format value.
    /// </summary>
    [Fact]
    public void FormatReturnsAzureBlob()
    {
        BrookStorageProvider provider = new(NullLogger<BrookStorageProvider>.Instance);

        Assert.Equal("azure-blob", provider.Format);
    }

    /// <summary>
    ///     Verifies append attempts fail with the Increment 1 not-yet-implemented guidance.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsNotSupportedException()
    {
        BrookStorageProvider provider = new(NullLogger<BrookStorageProvider>.Instance);

        NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(() => provider.AppendEventsAsync(
            new BrookKey("type", "id"),
            [
                new BrookEvent
                {
                    Id = "event-1",
                    Source = "tests",
                    EventType = "created",
                    Data = ImmutableArray<byte>.Empty,
                },
            ]));

        Assert.Contains("Increment 2", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies cursor reads fail with the Increment 1 not-yet-implemented guidance.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ReadCursorPositionAsyncThrowsNotSupportedException()
    {
        BrookStorageProvider provider = new(NullLogger<BrookStorageProvider>.Instance);

        NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => provider.ReadCursorPositionAsync(new BrookKey("type", "id")));

        Assert.Contains("Increment 2", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies event enumeration fails when the Increment 2 reader implementation is not yet available.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ReadEventsAsyncThrowsWhenEnumerationStarts()
    {
        BrookStorageProvider provider = new(NullLogger<BrookStorageProvider>.Instance);
        IAsyncEnumerable<BrookEvent> events = provider.ReadEventsAsync(new BrookRangeKey("type", "id", 0, 1));

        NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await foreach (BrookEvent item in events)
            {
                GC.KeepAlive(item);
            }
        });

        Assert.Contains("Increment 2", exception.Message, StringComparison.Ordinal);
    }
}