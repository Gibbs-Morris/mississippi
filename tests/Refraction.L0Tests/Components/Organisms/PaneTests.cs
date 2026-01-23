using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Organisms;


namespace Mississippi.Refraction.L0Tests.Components.Organisms;

/// <summary>
///     Smoke tests for <see cref="Pane" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Organisms")]
public sealed class PaneTests
{
    /// <summary>
    ///     Pane Depth defaults to Mid.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneDepthDefaultsToMid()
    {
        // Arrange
        Pane pane = new();

        // Assert
        Assert.Equal(RefractionDepthBands.Mid, pane.Depth);
    }

    /// <summary>
    ///     Pane has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Pane).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     Pane has ChildContent parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Pane).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(RenderFragment), prop!.PropertyType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Pane has Depth parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneHasDepthParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Pane).GetProperty("Depth");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Pane has Footer parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneHasFooterParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Pane).GetProperty("Footer");

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(RenderFragment), prop!.PropertyType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Pane has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Pane).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Pane has Title parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneHasTitleParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Pane).GetProperty("Title");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Pane has Variant parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneHasVariantParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Pane).GetProperty("Variant");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Pane inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(Pane)));
    }

    /// <summary>
    ///     Pane State defaults to Idle.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneStateDefaultsToIdle()
    {
        // Arrange
        Pane pane = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, pane.State);
    }

    /// <summary>
    ///     Pane Variant defaults to Primary.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneVariantDefaultsToPrimary()
    {
        // Arrange
        Pane pane = new();

        // Assert
        Assert.Equal(RefractionPaneVariants.Primary, pane.Variant);
    }
}