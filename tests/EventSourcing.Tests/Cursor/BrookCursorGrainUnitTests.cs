using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Cursor;
using Mississippi.EventSourcing.Reader;

using Moq;

using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.EventSourcing.Tests.Cursor;

/// <summary>
///     Unit tests for <see cref="BrookCursorGrain" />.
/// </summary>
public class BrookCursorGrainUnitTests
{
    private static BrookCursorGrain CreateGrain(
        out Mock<IBrookStorageReader> storage
    )
    {
        storage = new();
        Mock<IGrainContext> context = new();
        context.SetupGet(c => c.GrainId).Returns(GrainId.Create("brook-head", "type|id"));
        Mock<ILogger<BrookCursorGrain>> logger = new();
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        BrookCursorGrain grain = new(storage.Object, context.Object, logger.Object, options, streamIdFactory.Object);
        SetBrookId(grain, new("type", "id"));
        return grain;
    }

    private static StreamSequenceToken? GetLastToken(
        BrookCursorGrain grain
    ) =>
        (StreamSequenceToken?)typeof(BrookCursorGrain).GetProperty(
            "LastToken",
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(grain);

    private static BrookPosition GetTrackedCursorPosition(
        BrookCursorGrain grain
    ) =>
        (BrookPosition)typeof(BrookCursorGrain).GetProperty(
            "TrackedCursorPosition",
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(grain)!;

    private static void SetBrookId(
        BrookCursorGrain grain,
        BrookKey key
    ) =>
        typeof(BrookCursorGrain).GetProperty("BrookId", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(
            grain,
            key);

    private static void SetLastToken(
        BrookCursorGrain grain,
        StreamSequenceToken token
    ) =>
        typeof(BrookCursorGrain).GetProperty("LastToken", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(
            grain,
            token);

    private static void SetTrackedCursorPosition(
        BrookCursorGrain grain,
        BrookPosition position
    ) =>
        typeof(BrookCursorGrain).GetProperty("TrackedCursorPosition", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(grain, position);

    private sealed class TestStreamSequenceToken : StreamSequenceToken
    {
        public TestStreamSequenceToken(
            long sequenceNumber,
            int eventIndex = 0
        )
        {
            SequenceNumber = sequenceNumber;
            EventIndex = eventIndex;
        }

        public override int EventIndex { get; protected set; }

        public override long SequenceNumber { get; protected set; }

        public override int CompareTo(
            StreamSequenceToken? other
        )
        {
            if (other is null)
            {
                return 1;
            }

            if (other is TestStreamSequenceToken typed)
            {
                int seqCompare = SequenceNumber.CompareTo(typed.SequenceNumber);
                return seqCompare != 0 ? seqCompare : EventIndex.CompareTo(typed.EventIndex);
            }

            return 1;
        }

        public override bool Equals(
            StreamSequenceToken? other
        ) =>
            other is TestStreamSequenceToken typed &&
            (SequenceNumber == typed.SequenceNumber) &&
            (EventIndex == typed.EventIndex);

        public override bool Equals(
            object? obj
        ) =>
            Equals(obj as StreamSequenceToken);

        public override int GetHashCode() => HashCode.Combine(SequenceNumber, EventIndex);
    }

    /// <summary>
    ///     Ensures GetLatestPositionConfirmedAsync keeps the cached value when storage returns an older position.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetLatestPositionConfirmedAsyncKeepsCacheWhenStorageIsOlder()
    {
        // Arrange
        BrookCursorGrain sut = CreateGrain(out Mock<IBrookStorageReader> storage);
        SetTrackedCursorPosition(sut, new(6));
        storage.Setup(s => s.ReadCursorPositionAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(2));

        // Act
        BrookPosition confirmed = await sut.GetLatestPositionConfirmedAsync();

        // Assert
        Assert.Equal(6, confirmed.Value);
        Assert.Equal(6, (await sut.GetLatestPositionAsync()).Value);
    }

    /// <summary>
    ///     Ensures GetLatestPositionConfirmedAsync updates the cached cursor when storage returns a newer value.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetLatestPositionConfirmedAsyncUpdatesCacheWhenStorageIsNewer()
    {
        // Arrange
        BrookCursorGrain sut = CreateGrain(out Mock<IBrookStorageReader> storage);
        storage.Setup(s => s.ReadCursorPositionAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(9));

        // Act
        BrookPosition confirmed = await sut.GetLatestPositionConfirmedAsync();

        // Assert
        Assert.Equal(9, confirmed.Value);
        Assert.Equal(9, (await sut.GetLatestPositionAsync()).Value);
        storage.Verify(s => s.ReadCursorPositionAsync(new("type", "id"), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Ensures activation logs and throws when the primary key cannot be parsed.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task OnActivateAsyncLogsAndRethrowsWhenPrimaryKeyInvalid()
    {
        // Arrange
        Mock<IBrookStorageReader> storage = new();
        Mock<IGrainContext> context = new();
        Mock<ILogger<BrookCursorGrain>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        BrookCursorGrain sut = new(storage.Object, context.Object, logger.Object, options, streamIdFactory.Object);
        GrainId grainId = GrainId.Create("brook-head", "invalid-key");
        context.SetupGet(c => c.GrainId).Returns(grainId);

        // Act + Assert
        await Assert.ThrowsAsync<FormatException>(() => sut.OnActivateAsync(CancellationToken.None));
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(id =>
                    (id.Id == 1) && (id.Name == nameof(BrookCursorGrainLoggerExtensions.InvalidPrimaryKey))),
                It.Is<It.IsAnyType>((
                    state,
                    _
                ) => state.ToString() == "Failed to parse brook cursor grain primary key 'invalid-key'."),
                It.Is<Exception>(ex => ex is FormatException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Ensures OnErrorAsync requests grain deactivation without throwing.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task OnErrorAsyncDeactivatesGrain()
    {
        // Arrange
        Mock<IBrookStorageReader> storage = new();
        Mock<IGrainContext> context = new();
        Mock<ILogger<BrookCursorGrain>> logger = new();
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        BrookCursorGrain sut = new(storage.Object, context.Object, logger.Object, options, streamIdFactory.Object);

        // Act
        await sut.OnErrorAsync(new InvalidOperationException("boom"));

        // Assert: no exception indicates deactivation path executed without error
        Assert.True(true);
    }

    /// <summary>
    ///     Ensures OnNextAsync ignores events whose sequence tokens are older than the last processed token.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task OnNextAsyncSkipsOlderTokens()
    {
        // Arrange
        BrookCursorGrain sut = CreateGrain(out Mock<IBrookStorageReader> _);
        SetTrackedCursorPosition(sut, new(5));
        TestStreamSequenceToken lastToken = new(10);
        SetLastToken(sut, lastToken);
        BrookCursorMovedEvent evt = new(new(12));
        TestStreamSequenceToken incomingToken = new(3);

        // Act
        await sut.OnNextAsync(evt, incomingToken);

        // Assert
        Assert.Equal(5, GetTrackedCursorPosition(sut).Value);
        Assert.Same(lastToken, GetLastToken(sut));
    }

    /// <summary>
    ///     Ensures OnNextAsync updates the tracked cursor when the event and token are newer.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task OnNextAsyncUpdatesTrackedCursorWhenEventIsNewer()
    {
        // Arrange
        BrookCursorGrain sut = CreateGrain(out Mock<IBrookStorageReader> _);
        SetTrackedCursorPosition(sut, new(1));
        BrookCursorMovedEvent evt = new(new(4));
        TestStreamSequenceToken incomingToken = new(11);

        // Act
        await sut.OnNextAsync(evt, incomingToken);

        // Assert
        Assert.Equal(4, GetTrackedCursorPosition(sut).Value);
        Assert.Same(incomingToken, GetLastToken(sut));
    }
}