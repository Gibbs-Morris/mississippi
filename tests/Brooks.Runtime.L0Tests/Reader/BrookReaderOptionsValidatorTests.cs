using System;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Runtime.Reader;


namespace MississippiTests.Brooks.Runtime.L0Tests.Reader;

/// <summary>
///     Tests for <see cref="BrookReaderOptionsValidator" />.
/// </summary>
public sealed class BrookReaderOptionsValidatorTests
{
    private BrookReaderOptionsValidator Sut { get; } = new();

    /// <summary>
    ///     Negative slice size should return failure.
    /// </summary>
    [Fact]
    public void NegativeSliceSizeReturnsFail()
    {
        // Arrange
        BrookReaderOptions options = new()
        {
            BrookSliceSize = -1,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("-1", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Slice size of one should return success (boundary).
    /// </summary>
    [Fact]
    public void SliceSizeOneReturnsSuccess()
    {
        // Arrange
        BrookReaderOptions options = new()
        {
            BrookSliceSize = 1,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    /// <summary>
    ///     Valid slice size should return success.
    /// </summary>
    [Fact]
    public void ValidSliceSizeReturnsSuccess()
    {
        // Arrange
        BrookReaderOptions options = new()
        {
            BrookSliceSize = 100,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Succeeded);
    }

    /// <summary>
    ///     Zero slice size should return failure.
    /// </summary>
    [Fact]
    public void ZeroSliceSizeReturnsFail()
    {
        // Arrange
        BrookReaderOptions options = new()
        {
            BrookSliceSize = 0,
        };

        // Act
        ValidateOptionsResult result = Sut.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("BrookSliceSize", result.FailureMessage, StringComparison.Ordinal);
    }
}