using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Reader;


namespace Mississippi.EventSourcing.Brooks.L0Tests.Reader;

/// <summary>
///     Tests for <see cref="BrookReaderOptions" />.
/// </summary>
[AllureParentSuite("Mississippi.EventSourcing.Brooks")]
[AllureSuite("Reader")]
[AllureSubSuite("BrookReaderOptions")]
public sealed class BrookReaderOptionsTests
{
    /// <summary>
    ///     BrookSliceSize should be initializable to a custom value.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void BrookSliceSizeCanBeInitialized()
    {
        // Arrange & Act
        BrookReaderOptions sut = new()
        {
            BrookSliceSize = 500,
        };

        // Assert
        Assert.Equal(500, sut.BrookSliceSize);
    }

    /// <summary>
    ///     BrookSliceSize should default to 100.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void BrookSliceSizeDefaultsTo100()
    {
        // Arrange & Act
        BrookReaderOptions sut = new();

        // Assert
        Assert.Equal(100, sut.BrookSliceSize);
    }

    /// <summary>
    ///     BrookSliceSize should support various valid values.
    /// </summary>
    /// <param name="sliceSize">The slice size to test.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1000)]
    [InlineData(10000)]
    [AllureFeature("Configuration")]
    public void BrookSliceSizeSupportsVariousValues(
        long sliceSize
    )
    {
        // Arrange & Act
        BrookReaderOptions sut = new()
        {
            BrookSliceSize = sliceSize,
        };

        // Assert
        Assert.Equal(sliceSize, sut.BrookSliceSize);
    }
}