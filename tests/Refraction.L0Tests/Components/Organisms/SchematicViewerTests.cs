using System;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Organisms;


namespace Mississippi.Refraction.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="SchematicViewer" /> component.
/// </summary>
public sealed class SchematicViewerTests : BunitContext
{
    /// <summary>
    ///     SchematicViewer does not render caption when empty.
    /// </summary>
    [Fact]
    public void SchematicViewerDoesNotRenderCaptionWhenEmpty()
    {
        // Act
        using IRenderedComponent<SchematicViewer> cut =
            Render<SchematicViewer>(p => p.Add(c => c.Caption, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-schematic-viewer__caption"));
    }

    /// <summary>
    ///     SchematicViewer has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    public void SchematicViewerHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SchematicViewer).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     SchematicViewer has Caption parameter.
    /// </summary>
    [Fact]
    public void SchematicViewerHasCaptionParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SchematicViewer).GetProperty("Caption");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    /// <summary>
    ///     SchematicViewer has ChildContent parameter.
    /// </summary>
    [Fact]
    public void SchematicViewerHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SchematicViewer).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(RenderFragment), prop!.PropertyType);
    }

    /// <summary>
    ///     SchematicViewer has State parameter.
    /// </summary>
    [Fact]
    public void SchematicViewerHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SchematicViewer).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     SchematicViewer inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void SchematicViewerInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(SchematicViewer)));
    }

    /// <summary>
    ///     SchematicViewer renders additional attributes.
    /// </summary>
    [Fact]
    public void SchematicViewerRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<SchematicViewer> cut =
            Render<SchematicViewer>(p => p.AddUnmatched("data-testid", "viewer-1"));

        // Assert
        Assert.Equal("viewer-1", cut.Find(".rf-schematic-viewer").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     SchematicViewer renders caption when provided.
    /// </summary>
    [Fact]
    public void SchematicViewerRendersCaptionWhenProvided()
    {
        // Act
        using IRenderedComponent<SchematicViewer> cut = Render<SchematicViewer>(p => p.Add(
            c => c.Caption,
            "Test Caption"));

        // Assert
        string textContent = cut.Find(".rf-schematic-viewer__caption").TextContent;
        Assert.Contains("Test Caption", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     SchematicViewer renders child content in viewport.
    /// </summary>
    [Fact]
    public void SchematicViewerRendersChildContentInViewport()
    {
        // Act
        using IRenderedComponent<SchematicViewer> cut = Render<SchematicViewer>(p => p.AddChildContent(
            "<svg class='test-schematic'></svg>"));

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-schematic-viewer__viewport .test-schematic"));
    }

    /// <summary>
    ///     SchematicViewer renders custom state.
    /// </summary>
    [Fact]
    public void SchematicViewerRendersCustomState()
    {
        // Act
        using IRenderedComponent<SchematicViewer> cut = Render<SchematicViewer>(p => p.Add(
            c => c.State,
            RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-schematic-viewer").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     SchematicViewer renders viewport.
    /// </summary>
    [Fact]
    public void SchematicViewerRendersViewport()
    {
        // Act
        using IRenderedComponent<SchematicViewer> cut = Render<SchematicViewer>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-schematic-viewer__viewport"));
    }

    /// <summary>
    ///     SchematicViewer renders with default state.
    /// </summary>
    [Fact]
    public void SchematicViewerRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<SchematicViewer> cut = Render<SchematicViewer>();

        // Assert
        string? dataState = cut.Find(".rf-schematic-viewer").GetAttribute("data-state");
        Assert.Equal("idle", dataState);
    }

    /// <summary>
    ///     SchematicViewer State defaults to Idle.
    /// </summary>
    [Fact]
    public void SchematicViewerStateDefaultsToIdle()
    {
        // Arrange
        SchematicViewer component = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, component.State);
    }
}