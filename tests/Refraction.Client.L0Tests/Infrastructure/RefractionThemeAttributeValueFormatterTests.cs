using System;

using Mississippi.Refraction.Abstractions.Theme;
using Mississippi.Refraction.Client.Infrastructure;


namespace Mississippi.Refraction.Client.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="RefractionThemeAttributeValueFormatter" />.
/// </summary>
public sealed class RefractionThemeAttributeValueFormatterTests
{
    /// <summary>
    ///     Format returns the expected contrast DOM attribute values.
    /// </summary>
    /// <param name="contrastMode">The contrast mode to format.</param>
    /// <param name="expected">The expected DOM attribute value.</param>
    [Theory]
    [InlineData(RefractionContrastMode.System, "system")]
    [InlineData(RefractionContrastMode.Standard, "standard")]
    [InlineData(RefractionContrastMode.High, "high")]
    public void FormatReturnsExpectedContrastValue(
        RefractionContrastMode contrastMode,
        string expected
    )
    {
        // Act
        string actual = RefractionThemeAttributeValueFormatter.Format(contrastMode);

        // Assert
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     Format rejects unsupported contrast modes.
    /// </summary>
    [Fact]
    public void FormatRejectsUnsupportedContrastMode()
    {
        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RefractionThemeAttributeValueFormatter.Format((RefractionContrastMode)99));
    }

    /// <summary>
    ///     Format returns the expected density DOM attribute values.
    /// </summary>
    /// <param name="density">The density to format.</param>
    /// <param name="expected">The expected DOM attribute value.</param>
    [Theory]
    [InlineData(RefractionDensity.Comfortable, "comfortable")]
    [InlineData(RefractionDensity.Compact, "compact")]
    public void FormatReturnsExpectedDensityValue(
        RefractionDensity density,
        string expected
    )
    {
        // Act
        string actual = RefractionThemeAttributeValueFormatter.Format(density);

        // Assert
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     Format rejects unsupported densities.
    /// </summary>
    [Fact]
    public void FormatRejectsUnsupportedDensity()
    {
        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RefractionThemeAttributeValueFormatter.Format((RefractionDensity)99));
    }

    /// <summary>
    ///     Format returns the expected motion DOM attribute values.
    /// </summary>
    /// <param name="motionMode">The motion mode to format.</param>
    /// <param name="expected">The expected DOM attribute value.</param>
    [Theory]
    [InlineData(RefractionMotionMode.System, "system")]
    [InlineData(RefractionMotionMode.Standard, "standard")]
    [InlineData(RefractionMotionMode.Reduced, "reduced")]
    public void FormatReturnsExpectedMotionValue(
        RefractionMotionMode motionMode,
        string expected
    )
    {
        // Act
        string actual = RefractionThemeAttributeValueFormatter.Format(motionMode);

        // Assert
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     Format rejects unsupported motion modes.
    /// </summary>
    [Fact]
    public void FormatRejectsUnsupportedMotionMode()
    {
        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RefractionThemeAttributeValueFormatter.Format((RefractionMotionMode)99));
    }
}