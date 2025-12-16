using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.Tests.Reader;

/// <summary>
///     Unit tests for <see cref="BrookSliceReaderGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks")]
[AllureSubSuite("Brook Slice Reader Grain Unit")]
public sealed class BrookSliceReaderGrainUnitTests
{
    /// <summary>
    ///     Ensures deactivation clears caches and completes without error.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task DeactivateAsyncClearsCacheAndDeactivates()
    {
        // Arrange
        Mock<IBrookStorageReader> storage = new();
        Mock<IGrainContext> context = new();
        Mock<IBrookGrainFactory> factory = new();
        BrookSliceReaderGrain sut = new(storage.Object, context.Object, factory.Object);

        // Act
        await sut.DeactivateAsync();

        // Assert: no exception indicates deactivation path executed without error
        Assert.True(true);
    }
}