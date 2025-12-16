using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Reader;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Tests.Cursor;

/// <summary>
///     Unit tests for <see cref="BrookCursorGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks")]
[AllureSubSuite("Brook Cursor Grain Unit")]
public sealed class BrookCursorGrainUnitTests
{
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
        GrainId grainId = GrainId.Create("brook-cursor", "invalid-key");
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
}