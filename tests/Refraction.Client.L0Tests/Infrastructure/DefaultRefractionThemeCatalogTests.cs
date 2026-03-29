using Mississippi.Refraction.Abstractions.Theme;
using Mississippi.Refraction.Client.Infrastructure;


namespace Mississippi.Refraction.Client.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="DefaultRefractionThemeCatalog" />.
/// </summary>
public sealed class DefaultRefractionThemeCatalogTests
{
    /// <summary>
    ///     DefaultRefractionThemeCatalog exposes the expected built-in theme descriptors.
    /// </summary>
    [Fact]
    public void DefaultRefractionThemeCatalogExposesExpectedDescriptors()
    {
        // Arrange
        DefaultRefractionThemeCatalog catalog = new();

        // Assert
        Assert.Equal("horizon", catalog.DefaultTheme.BrandId.Value);
        Assert.Collection(
            catalog.Themes,
            theme =>
            {
                Assert.Equal("horizon", theme.BrandId.Value);
                Assert.Equal("horizon", theme.CssScopeName);
                Assert.Equal("Horizon", theme.DisplayName);
                Assert.True(theme.IsDefault);
            },
            theme =>
            {
                Assert.Equal("signal", theme.BrandId.Value);
                Assert.Equal("signal", theme.CssScopeName);
                Assert.Equal("Signal", theme.DisplayName);
                Assert.False(theme.IsDefault);
            });
    }

    /// <summary>
    ///     DefaultRefractionThemeCatalog resolves theme identifiers case-insensitively.
    /// </summary>
    [Fact]
    public void DefaultRefractionThemeCatalogResolvesThemeIdentifiersCaseInsensitively()
    {
        // Arrange
        DefaultRefractionThemeCatalog catalog = new();

        // Act
        RefractionThemeDescriptor? descriptor = catalog.GetTheme(new("SIGNAL"));

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("signal", descriptor.BrandId.Value);
        Assert.Equal("Signal", descriptor.DisplayName);
    }

    /// <summary>
    ///     DefaultRefractionThemeCatalog returns null when the supplied brand identifier is default.
    /// </summary>
    [Fact]
    public void DefaultRefractionThemeCatalogReturnsNullForDefaultBrandId()
    {
        // Arrange
        DefaultRefractionThemeCatalog catalog = new();

        // Act
        RefractionThemeDescriptor? descriptor = catalog.GetTheme(default(RefractionBrandId));

        // Assert
        Assert.Null(descriptor);
    }

    /// <summary>
    ///     DefaultRefractionThemeCatalog returns null when the supplied brand identifier is unknown.
    /// </summary>
    [Fact]
    public void DefaultRefractionThemeCatalogReturnsNullForUnknownBrandId()
    {
        // Arrange
        DefaultRefractionThemeCatalog catalog = new();

        // Act
        RefractionThemeDescriptor? descriptor = catalog.GetTheme(new("missing-brand"));

        // Assert
        Assert.Null(descriptor);
    }
}