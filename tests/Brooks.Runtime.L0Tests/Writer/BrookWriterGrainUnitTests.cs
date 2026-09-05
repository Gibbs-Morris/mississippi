using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Abstractions.Writer;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Brooks.Runtime.Writer;

using Moq;

using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Brooks.Runtime.L0Tests.Writer;

/// <summary>
///     Verifies the distinction between durable append and cursor publication failures.
/// </summary>
public sealed class BrookWriterGrainUnitTests
{
    /// <summary>
    ///     Preserves critical failures instead of converting them into recoverable publication errors.
    /// </summary>
    /// <param name="exceptionType">The critical failure type.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData(typeof(OutOfMemoryException))]
    [InlineData(typeof(StackOverflowException))]
    [InlineData(typeof(ThreadInterruptedException))]
    public async Task AppendEventsAsyncPropagatesCriticalPublicationFailure(Type exceptionType)
    {
        Exception failure = Assert.IsType<Exception>(Activator.CreateInstance(exceptionType), exactMatch: false);
        BrookKey key = new("test", "critical-publication");
        Mock<IBrookStorageWriter> storage = new(MockBehavior.Strict);
        Mock<IGrainContext> context = new(MockBehavior.Strict);
        Mock<IStreamProvider> streams = new(MockBehavior.Strict);
        Mock<IAsyncStream<BrookCursorMovedEvent>> stream = new(MockBehavior.Strict);
        BrookProviderOptions options = new();
        ServiceCollection services = new();
        services.AddKeyedSingleton<IStreamProvider>(options.OrleansStreamProviderName, streams.Object);
        using ServiceProvider provider = services.BuildServiceProvider();
        context.SetupGet(value => value.GrainId).Returns(GrainId.Create("brook-writer", key.ToString()));
        context.SetupGet(value => value.ActivationServices).Returns(provider);
        streams.Setup(value => value.GetStream<BrookCursorMovedEvent>(It.IsAny<StreamId>())).Returns(stream.Object);
        stream.Setup(value => value.OnNextAsync(It.IsAny<BrookCursorMovedEvent>(), null)).ThrowsAsync(failure);
        storage.Setup(value => value.AppendEventsAsync(key, It.IsAny<IReadOnlyList<BrookEvent>>(), null, CancellationToken.None))
            .ReturnsAsync(new BrookPosition(0));
        BrookWriterGrain writer = new(storage.Object, NullLogger<BrookWriterGrain>.Instance, context.Object, Options.Create(options));
        ImmutableArray<BrookEvent> events = [new() { Id = "committed-event" }];

        Exception thrown = await Assert.ThrowsAsync(exceptionType, () => writer.AppendEventsAsync(events));

        Assert.Same(failure, thrown);
        storage.VerifyAll();
    }

    /// <summary>
    ///     Leaves a storage failure unclassified because its append outcome may be unknown.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncPreservesUncertainStorageFailure()
    {
        BrookKey key = new("test", "uncertain-append");
        Mock<IBrookStorageWriter> storage = new(MockBehavior.Strict);
        Mock<IGrainContext> context = new(MockBehavior.Strict);
        TimeoutException storageFailure = new("Append acknowledgement was lost.");
        context.SetupGet(value => value.GrainId).Returns(GrainId.Create("brook-writer", key.ToString()));
        storage.Setup(value => value.AppendEventsAsync(
                key,
                It.IsAny<IReadOnlyList<BrookEvent>>(),
                null,
                CancellationToken.None))
            .ThrowsAsync(storageFailure);
        BrookWriterGrain writer = new(
            storage.Object,
            NullLogger<BrookWriterGrain>.Instance,
            context.Object,
            Options.Create(new BrookProviderOptions()));
        ImmutableArray<BrookEvent> events =
        [
            new()
            {
                Id = "uncertain-event",
            },
        ];
        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() => writer.AppendEventsAsync(events));
        Assert.Same(storageFailure, exception);
        context.VerifyGet(value => value.ActivationServices, Times.Never);
    }

    /// <summary>
    ///     Reports committed storage even when cancellation arrives before publication.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncReportsCommittedPositionWhenCancelledAfterAppend()
    {
        BrookKey key = new("test", "cancelled-publication");
        Mock<IBrookStorageWriter> storage = new(MockBehavior.Strict);
        Mock<IGrainContext> context = new(MockBehavior.Strict);
        using CancellationTokenSource cancellation = new();
        context.SetupGet(value => value.GrainId).Returns(GrainId.Create("brook-writer", key.ToString()));
        storage.Setup(value => value.AppendEventsAsync(
                key,
                It.IsAny<IReadOnlyList<BrookEvent>>(),
                null,
                cancellation.Token))
            .Returns(async () =>
            {
                await cancellation.CancelAsync();
                return new(0);
            });
        BrookWriterGrain writer = new(
            storage.Object,
            NullLogger<BrookWriterGrain>.Instance,
            context.Object,
            Options.Create(new BrookProviderOptions()));
        ImmutableArray<BrookEvent> events =
        [
            new()
            {
                Id = "committed-event",
            },
        ];
        BrookCursorPublicationException exception = await Assert.ThrowsAsync<BrookCursorPublicationException>(() =>
            writer.AppendEventsAsync(events, cancellationToken: cancellation.Token));
        Assert.Equal(0, exception.Position.Value);
        Assert.IsType<OperationCanceledException>(exception.InnerException);
        context.VerifyGet(value => value.ActivationServices, Times.Never);
    }

    /// <summary>
    ///     Reports the committed position when publication fails after storage accepts the events.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncReportsCommittedPositionWhenPublicationFails()
    {
        BrookKey key = new("test", "publication-failure");
        Mock<IBrookStorageWriter> storage = new(MockBehavior.Strict);
        Mock<IGrainContext> context = new(MockBehavior.Strict);
        InvalidOperationException publicationFailure = new("Stream provider is unavailable.");
        context.SetupGet(value => value.GrainId).Returns(GrainId.Create("brook-writer", key.ToString()));
        context.SetupGet(value => value.ActivationServices).Throws(publicationFailure);
        storage.Setup(value => value.AppendEventsAsync(
                key,
                It.IsAny<IReadOnlyList<BrookEvent>>(),
                new BrookPosition(4),
                CancellationToken.None))
            .ReturnsAsync(new BrookPosition(5));
        BrookWriterGrain writer = new(
            storage.Object,
            NullLogger<BrookWriterGrain>.Instance,
            context.Object,
            Options.Create(new BrookProviderOptions()));
        ImmutableArray<BrookEvent> events =
        [
            new()
            {
                Id = "committed-event",
            },
        ];
        BrookCursorPublicationException exception = await Assert.ThrowsAsync<BrookCursorPublicationException>(() =>
            writer.AppendEventsAsync(events, new BrookPosition(4)));
        Assert.Equal(5, exception.Position.Value);
        KeyNotFoundException streamFailure = Assert.IsType<KeyNotFoundException>(exception.InnerException);
        Assert.Same(publicationFailure, streamFailure.InnerException);
        storage.Verify(
            value => value.AppendEventsAsync(
                key,
                It.IsAny<IReadOnlyList<BrookEvent>>(),
                new BrookPosition(4),
                CancellationToken.None),
            Times.Once);
    }

    /// <summary>
    ///     Does not append events while retrying publication, including when the stream is still unavailable.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task PublishCursorAsyncDoesNotAppendEvents()
    {
        Mock<IBrookStorageWriter> storage = new(MockBehavior.Strict);
        Mock<IGrainContext> context = new(MockBehavior.Strict);
        InvalidOperationException publicationFailure = new("Stream provider is unavailable.");
        context.SetupGet(value => value.GrainId)
            .Returns(GrainId.Create("brook-writer", new BrookKey("test", "republish").ToString()));
        context.SetupGet(value => value.ActivationServices).Throws(publicationFailure);
        BrookWriterGrain writer = new(
            storage.Object,
            NullLogger<BrookWriterGrain>.Instance,
            context.Object,
            Options.Create(new BrookProviderOptions()));
        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            writer.PublishCursorAsync(new(5)));
        Assert.Same(publicationFailure, exception.InnerException);
        storage.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Publishes the confirmed position without invoking storage again.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task PublishCursorAsyncPublishesConfirmedPositionWithoutAppending()
    {
        BrookKey key = new("test", "successful-republication");
        Mock<IBrookStorageWriter> storage = new(MockBehavior.Strict);
        Mock<IGrainContext> context = new(MockBehavior.Strict);
        Mock<IStreamProvider> streamProvider = new(MockBehavior.Strict);
        Mock<IAsyncStream<BrookCursorMovedEvent>> stream = new(MockBehavior.Strict);
        BrookProviderOptions options = new();
        ServiceCollection services = new();
        services.AddKeyedSingleton<IStreamProvider>(options.OrleansStreamProviderName, streamProvider.Object);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        context.SetupGet(value => value.GrainId).Returns(GrainId.Create("brook-writer", key.ToString()));
        context.SetupGet(value => value.ActivationServices).Returns(serviceProvider);
        StreamId streamId = StreamId.Create(BrooksRuntimeOrleansStreamNames.CursorUpdateStreamName, key.ToString());
        streamProvider.Setup(value => value.GetStream<BrookCursorMovedEvent>(streamId)).Returns(stream.Object);
        stream.Setup(value => value.OnNextAsync(
                It.Is<BrookCursorMovedEvent>(update =>
                    (update.NewPosition.Value == 5) && (update.BrookKey == key.ToString())),
                null))
            .Returns(Task.CompletedTask);
        BrookWriterGrain writer = new(
            storage.Object,
            NullLogger<BrookWriterGrain>.Instance,
            context.Object,
            Options.Create(options));
        await writer.PublishCursorAsync(new(5));
        stream.VerifyAll();
        storage.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Rejects an unset position before accessing the stream or storage.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task PublishCursorAsyncRejectsUnsetPosition()
    {
        Mock<IBrookStorageWriter> storage = new(MockBehavior.Strict);
        Mock<IGrainContext> context = new(MockBehavior.Strict);
        BrookWriterGrain writer = new(
            storage.Object,
            NullLogger<BrookWriterGrain>.Instance,
            context.Object,
            Options.Create(new BrookProviderOptions()));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => writer.PublishCursorAsync(new(-1)));
        storage.VerifyNoOtherCalls();
        context.VerifyNoOtherCalls();
    }
}
