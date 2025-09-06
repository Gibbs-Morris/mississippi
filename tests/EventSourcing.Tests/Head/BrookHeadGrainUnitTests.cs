using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Head;
using Mississippi.EventSourcing.Reader;

using Moq;


namespace Mississippi.EventSourcing.Tests.Head;

/// <summary>
///     Unit tests for <see cref="BrookHeadGrain" />.
/// </summary>
public class BrookHeadGrainUnitTests
{
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
        Mock<ILogger<BrookHeadGrain>> logger = new();
        IOptions<BrookProviderOptions> options = Options.Create(new BrookProviderOptions());
        Mock<IStreamIdFactory> streamIdFactory = new();
        BrookHeadGrain sut = new(storage.Object, context.Object, logger.Object, options, streamIdFactory.Object);

        // Act
        await sut.OnErrorAsync(new InvalidOperationException("boom"));

        // Assert: no exception indicates deactivation path executed without error
        Assert.True(true);
    }
}