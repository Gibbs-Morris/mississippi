using System;
using System.Reflection;

using Allure.Xunit.Attributes;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Organisms;


namespace Mississippi.Refraction.L0Tests.Components.Organisms;

/// <summary>
///     Smoke tests for <see cref="Pane" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Organisms")]
public sealed class PaneTests : BunitContext
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

    /// <summary>
    ///     Pane renders with default state.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneRendersWithDefaultState()
    {
        // Act
        using var cut = Render<Pane>();

        // Assert
        string? dataState = cut.Find(".rf-pane").GetAttribute("data-state");
        Assert.Equal("idle", dataState);
    }

    /// <summary>
    ///     Pane renders default variant.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneRendersDefaultVariant()
    {
        // Act
        using var cut = Render<Pane>();

        // Assert
        string? dataVariant = cut.Find(".rf-pane").GetAttribute("data-variant");
        Assert.Equal("primary", dataVariant);
    }

    /// <summary>
    ///     Pane renders default depth.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneRendersDefaultDepth()
    {
        // Act
        using var cut = Render<Pane>();

        // Assert
        string? dataDepth = cut.Find(".rf-pane").GetAttribute("data-depth");
        Assert.Equal("mid", dataDepth);
    }

    /// <summary>
    ///     Pane renders title when provided.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneRendersTitleWhenProvided()
    {
        // Act
        using var cut = Render<Pane>(p => p
            .Add(c => c.Title, "Test Title"));

        // Assert
        string textContent = cut.Find(".rf-pane__title").TextContent;
        Assert.Contains("Test Title", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Pane does not render header when title is empty.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneDoesNotRenderHeaderWhenTitleIsEmpty()
    {
        // Act
        using var cut = Render<Pane>(p => p
            .Add(c => c.Title, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-pane__header"));
    }

    /// <summary>
    ///     Pane renders additional attributes.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneRendersAdditionalAttributes()
    {
        // Act
        using var cut = Render<Pane>(p => p
            .AddUnmatched("data-testid", "pane-1"));

        // Assert
        Assert.Equal("pane-1", cut.Find(".rf-pane").GetAttribute("data-testid"));
    }
}