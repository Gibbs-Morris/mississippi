using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime.Cursor;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Hosting.Runtime;

using Moq;

using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Brooks.Runtime.L0Tests;

/// <summary>
///     Verifies recovery notifications use the configured stream and refresh an existing cursor cache.
/// </summary>
public sealed class RecoveredCursorPublisherWiringTests
{
    /// <summary>
    ///     Routes committed recovery notifications through the existing stream identity and cursor observer.
    /// </summary>
    /// <param name="cancelled">Whether the notification is cancelled before dispatch.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecoveryPublisherUsesConfiguredStreamAndUpdatesCachedCursorAsync(
        bool cancelled
    )
    {
        BrookKey key = new("test", "recovery-publisher");
        StreamId id = StreamId.Create("recovery-cursors", key.ToString());
        Mock<IStreamIdFactory> ids = new(MockBehavior.Strict);
        ids.Setup(factory => factory.Create(key)).Returns(id);
        Mock<IStreamProvider> streams = new(MockBehavior.Strict);
        Mock<IAsyncStream<BrookCursorMovedEvent>> stream = new(MockBehavior.Strict);
        streams.Setup(provider => provider.GetStream<BrookCursorMovedEvent>(id)).Returns(stream.Object);
        ServiceCollection services = new();
        services.AddSingleton(ids.Object);
        services.AddKeyedSingleton("RecoveryStreams", streams.Object);
        IConfiguration configuration = Mock.Of<IConfiguration>();
        ISiloBuilder silo =
            Mock.Of<ISiloBuilder>(value => (value.Services == services) && (value.Configuration == configuration));
        silo.UseMississippi(runtime =>
            runtime.AddEventSourcing(options => options.OrleansStreamProviderName = "RecoveryStreams"));
        using ServiceProvider provider = services.BuildServiceProvider();
        Mock<IBrookStorageReader> storage = new(MockBehavior.Strict);
        storage.Setup(reader => reader.ReadCursorPositionAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(10));
        BrookCursorGrain cursor = new(
            storage.Object,
            Mock.Of<IGrainContext>(),
            NullLogger<BrookCursorGrain>.Instance,
            provider.GetRequiredService<IOptions<BrookProviderOptions>>(),
            ids.Object);
        Assert.Equal(new(10), await cursor.GetLatestPositionConfirmedAsync());
        stream.Setup(value => value.OnNextAsync(It.IsAny<BrookCursorMovedEvent>(), null))
            .Returns(async (BrookCursorMovedEvent update, StreamSequenceToken? token) =>
                await cursor.OnNextAsync(update, token));
        Func<BrookCursorMovedEvent, CancellationToken, Task> publish =
            provider.GetRequiredService<Func<BrookCursorMovedEvent, CancellationToken, Task>>();
        using CancellationTokenSource cancellation = new();
        BrookCursorMovedEvent committed = new(key.ToString(), new(12));
        if (cancelled)
        {
            await cancellation.CancelAsync();
            OperationCanceledException failure =
                await Assert.ThrowsAsync<OperationCanceledException>(async () => await publish(
                    committed,
                    cancellation.Token));
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(new(10), await cursor.GetLatestPositionAsync());
            stream.Verify(value => value.OnNextAsync(It.IsAny<BrookCursorMovedEvent>(), null), Times.Never);
        }
        else
        {
            await publish(committed, cancellation.Token);
            Assert.Equal(new(12), await cursor.GetLatestPositionAsync());
            stream.Verify(value => value.OnNextAsync(committed, null), Times.Once);
            ids.Verify(factory => factory.Create(key), Times.Once);
        }

        storage.Verify(
            reader => reader.ReadCursorPositionAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}