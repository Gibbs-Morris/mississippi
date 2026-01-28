using System;
using System.Threading;
using System.Threading.Tasks;


using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Unit tests for <see cref="UxProjectionCursorGrain" /> covering the grain implementation directly.
/// </summary>
public sealed class UxProjectionCursorGrainUnitTests
{
    private const string InvalidPrimaryKey = "invalid-key-without-pipe";

    private const string ValidPrimaryKey = "TestProjection|TEST.MODULE.STREAM|entity-123";

    private static UxProjectionCursorGrain CreateGrain(
        string primaryKey
    )
    {
        (UxProjectionCursorGrain sut, Mock<IGrainContext> _, Mock<ILogger<UxProjectionCursorGrain>> _,
                IOptions<BrookProviderOptions> _, Mock<IStreamIdFactory> _, Mock<IBrookStorageReader> _) =
            CreateGrainWithMocks(primaryKey);
        return sut;
    }

    private static (UxProjectionCursorGrain Grain, Mock<IGrainContext> Context, Mock<ILogger<UxProjectionCursorGrain>>
        Logger, IOptions<BrookProviderOptions> Options, Mock<IStreamIdFactory> StreamIdFactory,
        Mock<IBrookStorageReader> BrookStorageReader) CreateGrainWithMocks(
            string primaryKey
        )
    {
        Mock<IGrainContext> context = new();
        Mock<ILogger<UxProjectionCursorGrain>> logger = new();
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        Mock<IBrookStorageReader> brookStorageReader = new();
        GrainId grainId = GrainId.Create("ux-projection-cursor", primaryKey);
        context.SetupGet(c => c.GrainId).Returns(grainId);
        brookStorageReader.Setup(r => r.ReadCursorPositionAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(-1));
        UxProjectionCursorGrain sut = new(
            context.Object,
            options,
            streamIdFactory.Object,
            brookStorageReader.Object,
            logger.Object);
        return (sut, context, logger, options, streamIdFactory, brookStorageReader);
    }

    /// <summary>
    ///     Ensures the constructor throws ArgumentNullException when grainContext is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        Mock<IBrookStorageReader> brookStorageReader = new();
        Mock<ILogger<UxProjectionCursorGrain>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionCursorGrain(
            null!,
            options,
            streamIdFactory.Object,
            brookStorageReader.Object,
            logger.Object));
    }

    /// <summary>
    ///     Ensures the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainContext> context = new();
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        Mock<IBrookStorageReader> brookStorageReader = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionCursorGrain(
            context.Object,
            options,
            streamIdFactory.Object,
            brookStorageReader.Object,
            null!));
    }

    /// <summary>
    ///     Ensures the constructor throws ArgumentNullException when streamIdFactory is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenStreamIdFactoryIsNull()
    {
        // Arrange
        Mock<IGrainContext> context = new();
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IBrookStorageReader> brookStorageReader = new();
        Mock<ILogger<UxProjectionCursorGrain>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionCursorGrain(
            context.Object,
            options,
            null!,
            brookStorageReader.Object,
            logger.Object));
    }

    /// <summary>
    ///     Ensures the constructor throws ArgumentNullException when streamProviderOptions is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenStreamProviderOptionsIsNull()
    {
        // Arrange
        Mock<IGrainContext> context = new();
        Mock<IStreamIdFactory> streamIdFactory = new();
        Mock<IBrookStorageReader> brookStorageReader = new();
        Mock<ILogger<UxProjectionCursorGrain>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionCursorGrain(
            context.Object,
            null!,
            streamIdFactory.Object,
            brookStorageReader.Object,
            logger.Object));
    }

    /// <summary>
    ///     Ensures DeactivateAsync completes without error.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task DeactivateAsyncCompletesWithoutError()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);

        // Act
        await sut.DeactivateAsync();

        // Assert - no exception means success
        Assert.True(true);
    }

    /// <summary>
    ///     Ensures GetPositionAsync returns initial position of -1 before any events.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task GetPositionAsyncReturnsInitialMinusOnePosition()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);

        // Act
        BrookPosition position = await sut.GetPositionAsync();

        // Assert
        Assert.Equal(-1, position.Value);
        Assert.True(position.NotSet);
    }

    /// <summary>
    ///     Ensures GrainContext property returns the injected context.
    /// </summary>
    [Fact]
        public void GrainContextReturnsInjectedContext()
    {
        // Arrange
        (UxProjectionCursorGrain sut, Mock<IGrainContext> contextMock, Mock<ILogger<UxProjectionCursorGrain>> _,
                IOptions<BrookProviderOptions> _, Mock<IStreamIdFactory> _, Mock<IBrookStorageReader> _) =
            CreateGrainWithMocks(ValidPrimaryKey);

        // Act
        IGrainContext context = sut.GrainContext;

        // Assert
        Assert.Same(contextMock.Object, context);
    }

    /// <summary>
    ///     Ensures OnActivateAsync logs error when primary key is invalid.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnActivateAsyncLogsErrorWhenPrimaryKeyInvalid()
    {
        // Arrange
        Mock<IGrainContext> context = new();
        Mock<ILogger<UxProjectionCursorGrain>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        Mock<IBrookStorageReader> brookStorageReader = new();
        GrainId grainId = GrainId.Create("ux-projection-cursor", InvalidPrimaryKey);
        context.SetupGet(c => c.GrainId).Returns(grainId);
        UxProjectionCursorGrain sut = new(
            context.Object,
            options,
            streamIdFactory.Object,
            brookStorageReader.Object,
            logger.Object);

        // Act
        try
        {
            await sut.OnActivateAsync(CancellationToken.None);
        }
        catch (FormatException)
        {
            // Expected
        }

        // Assert
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(id =>
                    (id.Id == 2) &&
                    (id.Name == nameof(UxProjectionCursorGrainLoggerExtensions.CursorGrainInvalidPrimaryKey))),
                It.Is<It.IsAnyType>((
                    state,
                    _
                ) => state.ToString()!.Contains(InvalidPrimaryKey)),
                It.Is<Exception>(ex => ex is FormatException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Ensures OnActivateAsync throws when primary key cannot be parsed.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnActivateAsyncThrowsWhenPrimaryKeyInvalid()
    {
        // Arrange
        Mock<IGrainContext> context = new();
        Mock<ILogger<UxProjectionCursorGrain>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        Mock<IBrookStorageReader> brookStorageReader = new();
        GrainId grainId = GrainId.Create("ux-projection-cursor", InvalidPrimaryKey);
        context.SetupGet(c => c.GrainId).Returns(grainId);
        UxProjectionCursorGrain sut = new(
            context.Object,
            options,
            streamIdFactory.Object,
            brookStorageReader.Object,
            logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => sut.OnActivateAsync(CancellationToken.None));
    }

    /// <summary>
    ///     Ensures OnCompletedAsync completes without error.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnCompletedAsyncCompletesWithoutError()
    {
        // Arrange
        (UxProjectionCursorGrain sut, Mock<IGrainContext> _, Mock<ILogger<UxProjectionCursorGrain>> loggerMock,
                IOptions<BrookProviderOptions> _, Mock<IStreamIdFactory> _, Mock<IBrookStorageReader> _) =
            CreateGrainWithMocks(ValidPrimaryKey);
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

        // Act
        await sut.OnCompletedAsync();

        // Assert - no exception means success; verify logging occurred
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(id => id.Id == 5),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Ensures OnErrorAsync completes without throwing.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnErrorAsyncCompletesWithoutThrowing()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);
        InvalidOperationException testException = new("Test stream error");

        // Act
        await sut.OnErrorAsync(testException);

        // Assert - no exception means success
        Assert.True(true);
    }

    /// <summary>
    ///     Ensures OnErrorAsync logs the error.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnErrorAsyncLogsError()
    {
        // Arrange
        (UxProjectionCursorGrain sut, Mock<IGrainContext> _, Mock<ILogger<UxProjectionCursorGrain>> loggerMock,
                IOptions<BrookProviderOptions> _, Mock<IStreamIdFactory> _, Mock<IBrookStorageReader> _) =
            CreateGrainWithMocks(ValidPrimaryKey);
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
        InvalidOperationException testException = new("Test stream error");

        // Act
        await sut.OnErrorAsync(testException);

        // Assert
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.Is<EventId>(id => id.Id == 4),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(ex => ex == testException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Ensures OnNextAsync handles large position values correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnNextAsyncHandlesLargePositionValues()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);
        const long largePosition = 1_000_000_000L;

        // Act
        await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(largePosition)));
        BrookPosition position = await sut.GetPositionAsync();

        // Assert
        Assert.Equal(largePosition, position.Value);
    }

    /// <summary>
    ///     Ensures OnNextAsync ignores events with equal positions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnNextAsyncIgnoresEqualPosition()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);

        // Act
        await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(10)));
        await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(10)));
        BrookPosition position = await sut.GetPositionAsync();

        // Assert
        Assert.Equal(10, position.Value);
    }

    /// <summary>
    ///     Ensures OnNextAsync ignores events with older positions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnNextAsyncIgnoresOlderPosition()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);

        // Act - set position to 10, then try to go backwards to 5
        await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(10)));
        await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(5)));
        BrookPosition position = await sut.GetPositionAsync();

        // Assert - should still be 10
        Assert.Equal(10, position.Value);
    }

    /// <summary>
    ///     Ensures OnNextAsync throws when item is null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnNextAsyncThrowsWhenItemIsNull()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.OnNextAsync(null!));
    }

    /// <summary>
    ///     Ensures OnNextAsync updates position from initial -1 to 0.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnNextAsyncUpdatesFromMinusOneToZero()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);

        // Act
        await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(0)));
        BrookPosition position = await sut.GetPositionAsync();

        // Assert
        Assert.Equal(0, position.Value);
        Assert.False(position.NotSet);
    }

    /// <summary>
    ///     Ensures OnNextAsync updates position multiple times as events arrive.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnNextAsyncUpdatesPositionMultipleTimes()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);

        // Act
        await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(5)));
        BrookPosition position1 = await sut.GetPositionAsync();
        await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(10)));
        BrookPosition position2 = await sut.GetPositionAsync();
        await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(15)));
        BrookPosition position3 = await sut.GetPositionAsync();

        // Assert
        Assert.Equal(5, position1.Value);
        Assert.Equal(10, position2.Value);
        Assert.Equal(15, position3.Value);
    }

    /// <summary>
    ///     Ensures OnNextAsync updates position when event has newer position.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task OnNextAsyncUpdatesPositionWhenNewer()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);
        BrookCursorMovedEvent cursorEvent = new("TEST.BROOK:entity-1", new(10));

        // Act
        await sut.OnNextAsync(cursorEvent);
        BrookPosition position = await sut.GetPositionAsync();

        // Assert
        Assert.Equal(10, position.Value);
    }

    /// <summary>
    ///     Ensures position is tracked correctly after multiple sequential events.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
        public async Task PositionIsTrackedCorrectlyAfterSequentialEvents()
    {
        // Arrange
        UxProjectionCursorGrain sut = CreateGrain(ValidPrimaryKey);

        // Act - simulate a sequence of cursor updates
        for (int i = 0; i < 100; i++)
        {
            await sut.OnNextAsync(new("TEST.BROOK:entity-1", new(i)));
        }

        BrookPosition finalPosition = await sut.GetPositionAsync();

        // Assert
        Assert.Equal(99, finalPosition.Value);
    }
}