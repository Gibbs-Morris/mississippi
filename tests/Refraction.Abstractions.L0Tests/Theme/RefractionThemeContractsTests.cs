using System;
using System.Reflection;

using Mississippi.Refraction.Abstractions.Theme;


namespace Mississippi.Refraction.Abstractions.L0Tests.Theme;

/// <summary>
///     Contract verification tests for the Slice 1 Refraction theme abstractions.
/// </summary>
public sealed class RefractionThemeContractsTests
{
    /// <summary>
    ///     IRefractionThemeCatalog exposes default-theme and lookup members.
    /// </summary>
    [Fact]
    public void IRefractionThemeCatalogExposesLookupContract()
    {
        // Arrange
        Type interfaceType = typeof(IRefractionThemeCatalog);

        // Act
        PropertyInfo? defaultTheme = interfaceType.GetProperty(nameof(IRefractionThemeCatalog.DefaultTheme));
        PropertyInfo? themes = interfaceType.GetProperty(nameof(IRefractionThemeCatalog.Themes));
        MethodInfo? getTheme = interfaceType.GetMethod(nameof(IRefractionThemeCatalog.GetTheme));

        // Assert
        Assert.NotNull(defaultTheme);
        Assert.NotNull(themes);
        Assert.NotNull(getTheme);
        Assert.Equal(typeof(RefractionBrandId), getTheme!.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(RefractionThemeDescriptor), getTheme.ReturnType);
    }

    /// <summary>
    ///     RefractionBrandId canonicalizes host-supplied brand identifiers.
    /// </summary>
    [Fact]
    public void RefractionBrandIdCanonicalizesInput()
    {
        // Act
        RefractionBrandId brandId = new("  signal  ");

        // Assert
        Assert.Equal("signal", brandId.Value);
        Assert.Equal("signal", brandId.ToString());
    }

    /// <summary>
    ///     RefractionThemeDescriptor carries the metadata needed by runtime theme selection.
    /// </summary>
    [Fact]
    public void RefractionThemeDescriptorCarriesRequiredMetadata()
    {
        // Act
        RefractionThemeDescriptor descriptor = new()
        {
            BrandId = new("horizon"),
            CssScopeName = "horizon",
            DisplayName = "Horizon",
            IsDefault = true,
        };

        // Assert
        Assert.Equal("horizon", descriptor.BrandId.Value);
        Assert.Equal("horizon", descriptor.CssScopeName);
        Assert.Equal("Horizon", descriptor.DisplayName);
        Assert.True(descriptor.IsDefault);
    }

    /// <summary>
    ///     RefractionThemeSelection defaults to stable Slice 1 runtime modes.
    /// </summary>
    [Fact]
    public void RefractionThemeSelectionDefaultsToStableRuntimeModes()
    {
        // Act
        RefractionThemeSelection selection = new();

        // Assert
        Assert.Null(selection.BrandId);
        Assert.Equal(RefractionDensity.Comfortable, selection.Density);
        Assert.Equal(RefractionContrastMode.System, selection.Contrast);
        Assert.Equal(RefractionMotionMode.System, selection.Motion);
    }
}