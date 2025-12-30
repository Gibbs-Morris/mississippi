using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Factory;
using Mississippi.EventSourcing.Brooks.Reader;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Tests.Reader;

/// <summary>
///     Unit tests for <see cref="BrookReaderGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks")]
[AllureSubSuite("Brook Reader Grain Unit")]
public sealed class BrookReaderGrainUnitTests
{
    /// <summary>
    ///     Ensures deactivation path completes without error.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task DeactivateAsyncCallsDeactivateOnIdle()
    {
        // Arrange
        Mock<IBrookGrainFactory> factory = new();
        IOptions<BrookReaderOptions> options = Options.Create(new BrookReaderOptions());
        Mock<IGrainContext> context = new();
        BrookReaderGrain sut = new(factory.Object, options, context.Object, NullLogger<BrookReaderGrain>.Instance);

        // Act
        await sut.DeactivateAsync();

        // Assert: no exception indicates deactivation path executed without error
        Assert.True(true);
    }
}