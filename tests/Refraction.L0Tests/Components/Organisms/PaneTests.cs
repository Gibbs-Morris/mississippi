using System;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Organisms;


namespace Mississippi.Refraction.L0Tests.Components.Organisms;

/// <summary>
///     Smoke tests for <see cref="Pane" /> component.
/// </summary>
public sealed class PaneTests : BunitContext
{
    /// <summary>
    ///     Pane Depth defaults to Mid.
    /// </summary>
    [Fact]
    public void PaneDepthDefaultsToMid()
    {
        // Arrange
        Pane pane = new();

        // Assert
        Assert.Equal(RefractionDepthBands.Mid, pane.Depth);
    }

    /// <summary>
    ///     Pane does not render footer when null.
    /// </summary>
    [Fact]
    public void PaneDoesNotRenderFooterWhenNull()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>();

        // Assert
        Assert.Empty(cut.FindAll(".rf-pane__footer"));
    }

    /// <summary>
    ///     Pane does not render header when title is empty.
    /// </summary>
    [Fact]
    public void PaneDoesNotRenderHeaderWhenTitleIsEmpty()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>(p => p.Add(c => c.Title, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-pane__header"));
    }

    /// <summary>
    ///     Pane has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
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
    public void PaneInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(Pane)));
    }

    /// <summary>
    ///     Pane renders additional attributes.
    /// </summary>
    [Fact]
    public void PaneRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>(p => p.AddUnmatched("data-testid", "pane-1"));

        // Assert
        Assert.Equal("pane-1", cut.Find(".rf-pane").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     Pane renders child content.
    /// </summary>
    [Fact]
    public void PaneRendersChildContent()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>(p => p.AddChildContent(
            "<span class='test-content'>Main Content</span>"));

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-pane__content .test-content"));
    }

    /// <summary>
    ///     Pane renders custom depth.
    /// </summary>
    [Fact]
    public void PaneRendersCustomDepth()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>(p => p.Add(c => c.Depth, RefractionDepthBands.Far));

        // Assert
        string? dataDepth = cut.Find(".rf-pane").GetAttribute("data-depth");
        Assert.Equal("far", dataDepth);
    }

    /// <summary>
    ///     Pane renders custom variant.
    /// </summary>
    [Fact]
    public void PaneRendersCustomVariant()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>(p => p.Add(c => c.Variant, RefractionPaneVariants.Orbital));

        // Assert
        string? dataVariant = cut.Find(".rf-pane").GetAttribute("data-variant");
        Assert.Equal("orbital", dataVariant);
    }

    /// <summary>
    ///     Pane renders default depth.
    /// </summary>
    [Fact]
    public void PaneRendersDefaultDepth()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>();

        // Assert
        string? dataDepth = cut.Find(".rf-pane").GetAttribute("data-depth");
        Assert.Equal("mid", dataDepth);
    }

    /// <summary>
    ///     Pane renders default variant.
    /// </summary>
    [Fact]
    public void PaneRendersDefaultVariant()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>();

        // Assert
        string? dataVariant = cut.Find(".rf-pane").GetAttribute("data-variant");
        Assert.Equal("primary", dataVariant);
    }

    /// <summary>
    ///     Pane renders footer when provided.
    /// </summary>
    [Fact]
    public void PaneRendersFooterWhenProvided()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>(p => p.Add(
            c => c.Footer,
            builder => builder.AddContent(0, "Footer Content")));

        // Assert
        string textContent = cut.Find(".rf-pane__footer").TextContent;
        Assert.Contains("Footer Content", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Pane renders title when provided.
    /// </summary>
    [Fact]
    public void PaneRendersTitleWhenProvided()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>(p => p.Add(c => c.Title, "Test Title"));

        // Assert
        string textContent = cut.Find(".rf-pane__title").TextContent;
        Assert.Contains("Test Title", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Pane renders with default state.
    /// </summary>
    [Fact]
    public void PaneRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<Pane> cut = Render<Pane>();

        // Assert
        string? dataState = cut.Find(".rf-pane").GetAttribute("data-state");
        Assert.Equal("idle", dataState);
    }

    /// <summary>
    ///     Pane State defaults to Idle.
    /// </summary>
    [Fact]
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
    public void PaneVariantDefaultsToPrimary()
    {
        // Arrange
        Pane pane = new();

        // Assert
        Assert.Equal(RefractionPaneVariants.Primary, pane.Variant);
    }
}