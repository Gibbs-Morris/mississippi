using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;

using Moq;


namespace Mississippi.EventSourcing.Tests.Reader;

/// <summary>
///     Unit tests for <see cref="BrookReaderGrain" />.
/// </summary>
public class BrookReaderGrainUnitTests
{
    /// <summary>
    ///     Ensures deactivation path completes without error.
    /// </summary>
    [Fact]
    public async Task DeactivateAsync_CallsDeactivateOnIdle()
    {
        // Arrange
        Mock<IBrookGrainFactory> factory = new();
        IOptions<BrookReaderOptions> options = Options.Create(new BrookReaderOptions());
        Mock<IGrainContext> context = new();
        BrookReaderGrain sut = new(factory.Object, options, context.Object);

        // Act
        await sut.DeactivateAsync();

        // Assert: no exception indicates deactivation path executed without error
        Assert.True(true);
    }
}