using System;

using Allure.Xunit.Attributes;

using Mississippi.Refraction.Abstractions.Theme;


namespace Mississippi.Refraction.Abstractions.L0Tests.Theme;

/// <summary>
///     Contract verification tests for <see cref="IRefractionTheme" />.
/// </summary>
[AllureSuite("Refraction.Abstractions")]
[AllureSubSuite("Theme")]
public sealed class IRefractionThemeTests
{
    /// <summary>
    ///     Verifies the interface defines required color properties.
    /// </summary>
    [Fact]
    [AllureFeature("IRefractionTheme")]
    public void IRefractionThemeDefinesRequiredColorProperties()
    {
        // Arrange
        Type interfaceType = typeof(IRefractionTheme);

        // Assert
        Assert.NotNull(interfaceType.GetProperty(nameof(IRefractionTheme.PrimaryAccent)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IRefractionTheme.SecondaryAccent)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IRefractionTheme.PanelBackground)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IRefractionTheme.TextPrimary)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IRefractionTheme.TextSecondary)));
    }

    /// <summary>
    ///     Verifies the interface defines required spacing and timing properties.
    /// </summary>
    [Fact]
    [AllureFeature("IRefractionTheme")]
    public void IRefractionThemeDefinesSpacingAndTimingProperties()
    {
        // Arrange
        Type interfaceType = typeof(IRefractionTheme);

        // Assert
        Assert.NotNull(interfaceType.GetProperty(nameof(IRefractionTheme.SpacingUnit)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IRefractionTheme.BorderRadius)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IRefractionTheme.TransitionDurationMs)));
    }
}