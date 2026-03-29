using System;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Organisms;


namespace Mississippi.Refraction.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="SurfacePanel" /> component.
/// </summary>
public sealed class SurfacePanelTests : BunitContext
{
    /// <summary>
    ///     SurfacePanel does not render footer when null.
    /// </summary>
    [Fact]
    public void SurfacePanelDoesNotRenderFooterWhenNull()
    {
        // Act
        using IRenderedComponent<SurfacePanel> cut = Render<SurfacePanel>();

        // Assert
        Assert.Empty(cut.FindAll(".rf-surface-panel__footer"));
    }

    /// <summary>
    ///     SurfacePanel does not render header when title is empty.
    /// </summary>
    [Fact]
    public void SurfacePanelDoesNotRenderHeaderWhenTitleIsEmpty()
    {
        // Act
        using IRenderedComponent<SurfacePanel> cut = Render<SurfacePanel>(p => p.Add(c => c.Title, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-surface-panel__header"));
    }

    /// <summary>
    ///     SurfacePanel has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    public void SurfacePanelHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SurfacePanel).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     SurfacePanel has ChildContent parameter.
    /// </summary>
    [Fact]
    public void SurfacePanelHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SurfacePanel).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(RenderFragment), prop!.PropertyType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     SurfacePanel has Footer parameter.
    /// </summary>
    [Fact]
    public void SurfacePanelHasFooterParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SurfacePanel).GetProperty("Footer");

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(RenderFragment), prop!.PropertyType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     SurfacePanel has State parameter.
    /// </summary>
    [Fact]
    public void SurfacePanelHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SurfacePanel).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     SurfacePanel has Title parameter.
    /// </summary>
    [Fact]
    public void SurfacePanelHasTitleParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SurfacePanel).GetProperty("Title");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     SurfacePanel inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void SurfacePanelInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(SurfacePanel)));
    }

    /// <summary>
    ///     SurfacePanel renders additional attributes.
    /// </summary>
    [Fact]
    public void SurfacePanelRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<SurfacePanel> cut = Render<SurfacePanel>(p => p.AddUnmatched(
            "data-testid",
            "surface-panel-1"));

        // Assert
        Assert.Equal("surface-panel-1", cut.Find(".rf-surface-panel").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     SurfacePanel merges an additional CSS class with the component root class.
    /// </summary>
    [Fact]
    public void SurfacePanelMergesAdditionalCssClassWithRootClass()
    {
        // Act
        using IRenderedComponent<SurfacePanel> cut = Render<SurfacePanel>(p => p.AddUnmatched(
            "class",
            "surface-panel-host"));

        // Assert
        Assert.NotNull(cut.Find(".rf-surface-panel.surface-panel-host"));
    }

    /// <summary>
    ///     SurfacePanel renders child content.
    /// </summary>
    [Fact]
    public void SurfacePanelRendersChildContent()
    {
        // Act
        using IRenderedComponent<SurfacePanel> cut = Render<SurfacePanel>(p => p.AddChildContent(
            "<span class='test-content'>Panel Content</span>"));

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-surface-panel__content .test-content"));
    }

    /// <summary>
    ///     SurfacePanel renders footer when provided.
    /// </summary>
    [Fact]
    public void SurfacePanelRendersFooterWhenProvided()
    {
        // Act
        using IRenderedComponent<SurfacePanel> cut = Render<SurfacePanel>(p => p.Add(
            c => c.Footer,
            builder => builder.AddContent(0, "Footer Content")));

        // Assert
        Assert.Contains("Footer Content", cut.Find(".rf-surface-panel__footer").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     SurfacePanel renders title when provided.
    /// </summary>
    [Fact]
    public void SurfacePanelRendersTitleWhenProvided()
    {
        // Act
        using IRenderedComponent<SurfacePanel> cut = Render<SurfacePanel>(p => p.Add(c => c.Title, "Test Title"));

        // Assert
        Assert.Contains("Test Title", cut.Find(".rf-surface-panel__title").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     SurfacePanel renders custom state.
    /// </summary>
    [Fact]
    public void SurfacePanelRendersCustomState()
    {
        // Act
        using IRenderedComponent<SurfacePanel> cut = Render<SurfacePanel>(p => p.Add(
            c => c.State,
            RefractionStates.Active));

        // Assert
        Assert.Equal("active", cut.Find(".rf-surface-panel").GetAttribute("data-state"));
    }

    /// <summary>
    ///     SurfacePanel renders with default state.
    /// </summary>
    [Fact]
    public void SurfacePanelRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<SurfacePanel> cut = Render<SurfacePanel>();

        // Assert
        Assert.Equal("idle", cut.Find(".rf-surface-panel").GetAttribute("data-state"));
    }

    /// <summary>
    ///     SurfacePanel State defaults to Idle.
    /// </summary>
    [Fact]
    public void SurfacePanelStateDefaultsToIdle()
    {
        // Arrange
        SurfacePanel panel = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, panel.State);
    }
}